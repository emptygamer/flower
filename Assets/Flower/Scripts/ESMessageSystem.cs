using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace RemptyTool.Flowser
{
    /// <summary>The Flowser core by Rempty @ EmptyStudio.
    /// UserFunction
    ///     SetText         : Make the system to print or execute the commands.
    ///     Next            : If the system is WaitingForNext, then it will continue the remaining contents.
    ///     AddSpecialCharToFuncMap : You can add your customized special-characters into the function map.
    ///     ReadTextFromResource    : Load the .txt file from Resources folder and execute.
    ///     Stop                    : Stop the system.
    ///     Resume                  : Resume the system.
    ///     SetDisplayVariable      : Set the value of the variable to display in the text. 
    ///     SetupDialogUIPrefab     : Setup the UI objects of the dialog panel.
    ///     SetupButtonUIPrefab     : Setup the UI objects of the button panel.
    ///     SetupButtonItem         : Setup the UI objects of the button item to the button panel.
    ///     RemoveButtonUI          : Remove the button panel.
    /// Parameters
    ///     isCompleted     : Is the input text parsing completely by the system.
    ///     text            : The result, witch you can show on your interface as a dialog.
    ///     isWaitingForNext: Waiting for user input -> The Next() function.
    ///     textSpeed       : Setting the updating period of text.
    ///     isStop          : Is the system stop.
    /// </summary> 
    public class ESMessageSystem : MonoBehaviour
    {
        public bool isCompleted { get { return isMsgCompleted; } }
        public string text { get { return msgText; } }
        public bool isWaitingForNext { get { return isWaitingForNextToGo; } }
        public float textSpeed = 0.01f; // Updating period of text. The actual period may not less than deltaTime.

        private const char SPECIAL_CHAR_STAR = '[';
        private const char SPECIAL_CHAR_END = ']';
        private enum SpecialCharType { StartChar, CmdChar, EndChar, NormalChar, Variable}
        private bool isMsgCompleted = true;
        private bool isMsgPreCompleted = true; 
        private bool isOnSpecialChar = false;
        private bool isWaitingForNextToGo = false;
        private bool isOnCmdEvent = false;
        private string specialCmd = "";
        private string msgText;
        private char lastChar = ' ';
        private Dictionary<string, Action> specialCharFuncMap = new Dictionary<string, Action>();
        private bool isStop = false;
        private string currentTextListResource = "";
        private List<string> currentTextList = new List<string>();
        private int currentTextListIndex = 0;
        private GameObject uiDialogCanvasObject;
        private Text uiText;
        private Image uiPanel;
        private Color uiTextColor;
        private Color uiPanelColor;
        private List<string> animatingList = new List<string>();
        private const char VAR_CHAR = '#';
        private Dictionary<string, string> variableDisplayMap = new Dictionary<string, string>();
        private string variableString = "";
        private GameObject uiButtonCanvasObject;

        void Start()
        {
            //Register the Keywords Function.
            specialCharFuncMap.Add("w", () => StartCoroutine(CmdFun_w_Task()));
            specialCharFuncMap.Add("r", () => StartCoroutine(CmdFun_r_Task()));
            specialCharFuncMap.Add("l", () => StartCoroutine(CmdFun_l_Task()));
            specialCharFuncMap.Add("lr", () => StartCoroutine(CmdFun_lr_Task()));
            specialCharFuncMap.Add("c", () => StartCoroutine(CmdFun_c_Task()));

            specialCharFuncMap.Add("stop", () => StartCoroutine(CmdFun_stop_Task()));
            specialCharFuncMap.Add("resume", () => StartCoroutine(CmdFun_resume_Task()));
            specialCharFuncMap.Add("hide", () => StartCoroutine(CmdFun_hide_Task()));
            specialCharFuncMap.Add("show", () => StartCoroutine(CmdFun_show_Task()));

            SetupDialogUIPrefab("DefaultDialogUIPrefab");
        }

        #region Public Function
        public void AddSpecialCharToFuncMap(string _str, Action _act)
        {
            specialCharFuncMap.Add(_str, _act);
        }
        #endregion

        #region User Function
        public void Next()
        {
            isWaitingForNextToGo = false;
        }
        public void SetText(string _text)
        {
            StartCoroutine(SetTextTask(_text));
        }    
        public void ReadTextFromResource(string filePath, int index=0){
            this.currentTextList.Clear();
            this.currentTextListIndex = index;
            TextAsset textAsset = Resources.Load(filePath) as TextAsset;
            if(textAsset == null){
                print($"Text resource {filePath} not exists.");
                return;
            }
            var lineTextData = textAsset.text.Split('\n');
            foreach (string line in lineTextData)
            {
                this.currentTextList.Add(line);
            }
            this.currentTextListResource = filePath;
        }
        public void AssignUIText(Text uiText){
            this.uiText = uiText;
        }
        public void AssignUIPanel(Image uiPanel){
            this.uiPanel = uiPanel;
        }
        public void SetupDialogUIPrefab(string resourcePath){
            var dialogCanvas = Resources.Load(resourcePath) as GameObject;
            if(dialogCanvas != null){
                if(this.uiDialogCanvasObject){
                    Destroy(this.uiDialogCanvasObject);
                }
                this.uiDialogCanvasObject = Instantiate(dialogCanvas, Vector3.zero, Quaternion.identity);
                this.uiText = this.uiDialogCanvasObject.transform.Find("DialogPanel/DialogText").GetComponent<Text>();
                this.uiPanel = this.uiDialogCanvasObject.transform.Find("DialogPanel").GetComponent<Image>();
                if(this.uiText == null){
                    print("DialogText not in the UI prefab.");
                }
                if(this.uiPanel == null){
                    print("DialogPanel not in the UI prefab.");
                }
                this.uiPanelColor = uiPanel.color;
                this.uiTextColor = uiText.color;

                var _initTextColor = uiText.color;
                _initTextColor.a = 0;
                uiText.color = _initTextColor;
                var _initPanelColor = uiPanel.color;
                _initPanelColor.a = 0;
                uiPanel.color = _initPanelColor;
            }else{
                print($"Dialog UI prefab {resourcePath} not exists in Resources.");
            }
        }
        public void SetDisplayVariable(string key, string value){
            this.variableDisplayMap[key] = value;
        }
        public void SetupButtonUIPrefab(string resourcePath){
            var buttonCanvas = Resources.Load(resourcePath) as GameObject;
            if(buttonCanvas != null){
                this.uiButtonCanvasObject = Instantiate(buttonCanvas, Vector3.zero, Quaternion.identity);
            }else{
                print($"Button UI prefab {resourcePath} not exists in Resources.");
            }
        }
        public void RemoveButtonUI(){
            if(this.uiButtonCanvasObject != null){
                Destroy(this.uiButtonCanvasObject);
            }
        }
        public void SetupButtonItem(string resourcePath, string info, Action triggerFunction){
            var buttonItem = Resources.Load(resourcePath) as GameObject;
            if(buttonItem != null){
                if(this.uiButtonCanvasObject != null){
                    var _panel = this.uiButtonCanvasObject.transform.Find("ButtonPanel");
                    if(_panel != null){
                        var _button = Instantiate(buttonItem, Vector3.zero, Quaternion.identity);
                        _button.GetComponent<Button>().onClick.AddListener(()=>{triggerFunction();});
                        var _text = _button.transform.Find("Text");
                        if (_text != null){
                            _text.GetComponent<Text>().text = info;
                        }else{
                            print($"\"Text\" not found in buttonItem prefab.");

                        }
                        _button.transform.SetParent(_panel);
                        _button.transform.localScale = Vector3.one;
                    }else{
                        print($"\"ButtonPanel\" not found in Button UI prefab.");
                    }
                }else{
                    print($"Please call SetupButtonUIPrefab to setup the button panel.");
                }
                
            }else{
                print($"Button Item prefab {resourcePath} not exists in Resources.");
            }
        }
        public void Stop(){
            this.isStop = true;
        }
        public void Resume(){
            this.isStop = false;
        }
        #endregion

        #region Keywords Function
        private IEnumerator CmdFun_l_Task()
        {
            isOnCmdEvent = true;
            isWaitingForNextToGo = true;
            yield return new WaitUntil(() => isWaitingForNextToGo == false);
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFun_r_Task()
        {
            isOnCmdEvent = true;
            msgText += '\n';
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFun_w_Task()
        {
            isOnCmdEvent = true;
            isWaitingForNextToGo = true;
            yield return new WaitUntil(() => isWaitingForNextToGo == false);
            msgText = "";   //Erase the messages.
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFun_lr_Task()
        {
            isOnCmdEvent = true;
            isWaitingForNextToGo = true;
            yield return new WaitUntil(() => isWaitingForNextToGo == false);
            msgText += '\n';
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFun_c_Task()
        {
            isOnCmdEvent = true;
            msgText = "";
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFun_stop_Task()
        {
            Stop();
            yield return new WaitUntil(() => isStop == false);
            yield return null;
        }
        private IEnumerator CmdFun_resume_Task()
        {
            Resume();
            yield return null;
        }
        private IEnumerator CmdFun_hide_Task()
        {
            isOnCmdEvent = true;
            var textEndColor = uiText.color;
            textEndColor.a = 0;
            var panelEndColor = uiPanel.color;
            panelEndColor.a = 0;

            StartCoroutine( ChangeColorTask("text", uiText, uiText.color, textEndColor));
            StartCoroutine( ChangeColorTask("panel", uiPanel, uiPanel.color, panelEndColor));
            yield return new WaitUntil(() => this.animatingList.Count == 0);
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFun_show_Task()
        {
            var textEndColor = this.uiTextColor;
            var panelEndColor = this.uiPanelColor;

            StartCoroutine( ChangeColorTask("text", uiText, uiText.color, textEndColor));
            StartCoroutine( ChangeColorTask("panel", uiPanel, uiPanel.color, panelEndColor));
            yield return new WaitUntil(() => this.animatingList.Count == 0);

            yield return null;
        }
        #endregion

        #region Messages Core
        private void AddChar(char _char)
        {
            msgText += _char;
            lastChar = _char;
        }
        private SpecialCharType CheckSpecialChar(char _char)
        {
            if (_char == SPECIAL_CHAR_STAR)
            {
                if (lastChar == SPECIAL_CHAR_STAR)
                {
                    specialCmd = "";
                    isOnSpecialChar = false;
                    return SpecialCharType.NormalChar;
                }
                isOnSpecialChar = true;
                return SpecialCharType.CmdChar;
            }
            else if (_char == SPECIAL_CHAR_END && isOnSpecialChar)
            {
                if(specialCmd[0] == VAR_CHAR){
                    this.variableString = "";
                    var varName = specialCmd.Substring(1,specialCmd.Length-1);
                    if(this.variableDisplayMap.ContainsKey(varName)){
                        this.variableString = this.variableDisplayMap[varName];
                        specialCmd = "";
                        isOnSpecialChar = false;
                        return SpecialCharType.Variable;
                    }
                }
                //exe cmd!
                if (specialCharFuncMap.ContainsKey(specialCmd))
                {
                    specialCharFuncMap[specialCmd]();
                    //Debug.Log("The keyword : [" + specialCmd + "] execute!");
                }
                else
                    Debug.LogError("The keyword : [" + specialCmd + "] is not exist!");

                
                specialCmd = "";
                isOnSpecialChar = false;
                return SpecialCharType.EndChar;
            }
            else if (isOnSpecialChar)
            {
                specialCmd += _char;
                return SpecialCharType.CmdChar;
            }
            return SpecialCharType.NormalChar;
        }
        private IEnumerator SetTextTask(string _text)
        {
            isOnSpecialChar = false;
            isMsgCompleted = false;
            isMsgPreCompleted = false;
            specialCmd = "";
            for (int i = 0; i < _text.Length; i++)
            {
                switch (CheckSpecialChar(_text[i]))
                {
                    case SpecialCharType.NormalChar:
                        AddChar(_text[i]);
                        lastChar = _text[i];
                        yield return new WaitForSeconds(textSpeed);
                        break;
                    case SpecialCharType.Variable:
                        int _index = 0;
                        while(_index < this.variableString.Length){
                            AddChar(this.variableString[_index]);
                            lastChar = this.variableString[_index];
                            _index ++;
                            yield return new WaitForSeconds(textSpeed);
                        }
                        break;
                }
                lastChar = _text[i];
                yield return new WaitUntil(() => isOnCmdEvent == false);
            }
            isMsgPreCompleted = true;
            yield return null;
        }
        private IEnumerator ChangeColorTask(string key, MaskableGraphic graphic ,Color startColor, Color endColor, float step=0.01f){
            animatingList.Add(key);
            float val = 0;
            while(val != 1){
                val += step;
                val = val>1 ? 1:val;
                graphic.color = Color.Lerp(startColor, endColor, val);
                yield return null;
            }
            int _keyIndex = animatingList.IndexOf(key);
            animatingList.RemoveAt(_keyIndex);
            yield return null;
        }
        #endregion
    
       
        void Update() {
            if(this.uiText){
                this.uiText.text = this.text;
            }
            // Make sure will not get isMsgCompleted when the list still have the next line.
            if((!isMsgCompleted) && isMsgPreCompleted){
                isMsgCompleted=true;
            }
            if (this.isCompleted == true && this.currentTextListIndex < this.currentTextList.Count && !this.isStop)
            {
                SetText(this.currentTextList[this.currentTextListIndex]);
                this.currentTextListIndex++;
            }
            
        }
    }
}

