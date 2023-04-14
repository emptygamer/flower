using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Flower{
    /// <summary>
    /// This is Flowser core by Rempty @ EmptyStudio.
    /// <para type="bold">[UserFunction]</para>
    /// <list type="bullet">
    ///     <item>RegisterCommand         : Register your customized commands.</item>
    ///     <item>RegisterEffect          : Register your customized effects.</item>
    ///     <item>RegisterToSceneObject   : Register GameObject to Flower Scene that you can apply commands to this object.</item>
    ///     <item>GetSceneObject          : Get Flower Scene objects.</item>
    ///     <item>QuerySceneObject        : Query Flower Scene objects by regular expression.</item>
    ///     <item>RemoveSceneObject       : Remove Flower Scene objects.</item>
    ///     <item>Next                    : If the system is WaitingForNext, then it will continue the remaining contents.</item>
    ///     <item>SetTextList             : Set and execute the text list.</item>
    ///     <item>ReadTextFromResource    : Load the plain text file (.txt) from Resources folder and execute.</item>
    ///     <item>SetVariable             : Define Flower variables.</item>
    ///     <item>RemoveVariable          : Remove Flower variables.</item>
    ///     <item>SetupDialog             : Setup the dialog UI objects.</item>
    ///     <item>SetupButtonGroup        : Setup the button group UI objects.</item>
    ///     <item>SetupButton             : Setup the button UI objects.</item>
    ///     <item>RemoveDialog            : Remove the dialog UI objects.</item>
    ///     <item>RemoveButtonGroup       : Remove the button group UI objects.</item>
    ///     <item>Stop                    : Stop the system.</item>
    ///     <item>Resume                  : Resume the system.</item>
    ///     <item>InvokeSpecialCommand    : Execute command directly. 
    ///     (ex:"image,fg1,character_image,0,0,10,spFadeIn_1000")</item>
    /// </list>
    /// <para>[Event Handler]</para>
    /// <list type="bullet">
    ///     <item>logHappened             : Log events.</item>
    ///     <item>textUpdated             : Text updating events.</item>
    /// </list>
    /// <para>[Parameters]</para>
    /// <list type="bullet">
    ///     <item>isCompleted             : Is the input text list executed completely by the system.</item>
    ///     <item>text                    : The current text result.</item>
    ///     <item>isWaitingForNext        : Is waiting for user input -> The Next() function.</item>
    ///     <item>textSpeed               : Set the updating period of the text.</item>
    ///     <item>isRichTextEnable        : Is rich text enable, if it's true, will handle the rich text tags.</item>
    ///     <item>isDefaultLogEnable      : Enalbe the default logging system.</item>
    ///     <item>elementsDestroyOnLoad   : Will the system and elements be destroyed when the scene changes.</item>
    /// </list> 
    /// </summary> 
    public class FlowerSystem : MonoBehaviour
    {
        public bool isCompleted { get { return isTextListCompleted; } }
        public string text { get { return msgText.ToString(); } }
        public bool isWaitingForNext { get { return isWaitingForNextToGo; } }
        public float textSpeed = 0.01f;
        public bool isRichTextEnable = true;
        public bool isDefaultLogEnable
        {
            set {
                if(value){
                    if(!_defaultLogEnable){
                        logHappened += DefaultLogFunction;
                        _defaultLogEnable=true;
                    }
                }else{
                    logHappened -= DefaultLogFunction;
                    _defaultLogEnable=false;
                }
            }
            get { return _defaultLogEnable; }
        }
        public delegate void CharFunction(List<string> properties);
        public delegate void EffectFunction(string key, List<string> _params);
        public event System.EventHandler<LogEventArgs> logHappened;
        public event System.EventHandler<TextUpdateEventArgs> textUpdated;
        public bool elementsDestroyOnLoad = true;

        private const char SPECIAL_CHAR_STAR = '[';
        private const char SPECIAL_CHAR_END = ']';
        private const char SPECIAL_CHAR_PARAMS_SEPARATOR = ',';
        private const char SPECIAL_CHAR_EFFECT_SEPARATOR = '_';
        private enum SpecialCharType { StartChar, CmdChar, EndChar, NormalChar, Variable}
        private bool isTextCompleted = true;
        private bool isTextListCompleted = true;
        private float screenWidthReference = -1;
        private float screenHeightReference = -1;

        private bool isOnSpecialChar = false;
        private bool isWaitingForNextToGo = false;
        private bool isOnCmdEvent = false;
        private string specialCmd = "";
        private StringBuilder msgText = new StringBuilder();
        private char lastChar = ' ';
        private Dictionary<string, CharFunction> specialCharFuncMap = new Dictionary<string, CharFunction>();
        private bool isStop = false;
        private string currentTextListResource = "";
        private List<string> currentTextList = new List<string>();
        private int currentTextListIndex = 0;
        private List<string> animatingList = new List<string>();
        private const char VAR_CHAR = '#';
        private Dictionary<string, string> variableMap = new Dictionary<string, string>();
        private string variableString = "";
        private const string RICH_TEXT_REGEX_PATTERN = @"(<[^><]*>)+[^><]*(</[^><]*>)+";
        private Regex richTextRegex = new Regex(RICH_TEXT_REGEX_PATTERN, RegexOptions.Compiled);
        private Dictionary<string, GameObject> sceneObjectMap = new Dictionary<string, GameObject>();
        private Dictionary<string, EffectFunction> effectMap = new Dictionary<string, EffectFunction>();
        private Dictionary<string, string> prefabPathMap = new Dictionary<string, string>();
        private EventHandler<TextUpdateEventArgs> _defaultTextUpdateHandler;
        private bool _defaultLogEnable=false;
        
        void Start()
        {

        }

        private void Awake() {
            // Register Default Keyword-Functions.
            RegisterCommand("w", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_w_Task(_params))));
            RegisterCommand("r", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_r_Task(_params))));
            RegisterCommand("l", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_l_Task(_params))));
            RegisterCommand("lr", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_lr_Task(_params))));
            RegisterCommand("c", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_c_Task(_params))));
            RegisterCommand("stop", (List<string> _params) => StartCoroutine(CmdFunc_stop_Task(_params)));
            RegisterCommand("resume", (List<string> _params) => StartCoroutine(CmdFunc_resume_Task(_params)));
            RegisterCommand("wait", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_wait_Task(_params))));
            RegisterCommand("show", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_dialogShow_Task(_params))));
            RegisterCommand("async_show", (List<string> _params) => StartCoroutine(CmdFunc_dialogShow_Task(_params)));
            RegisterCommand("hide", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_dialogHide_Task(_params))));
            RegisterCommand("async_hide", (List<string> _params) => StartCoroutine(CmdFunc_dialogHide_Task(_params)));
            RegisterCommand("image", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_image_Task(_params))));
            RegisterCommand("remove", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_remove_Task(_params))));
            RegisterCommand("effect", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_effect_Task(_params))));
            RegisterCommand("audio", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_audio_Task(_params))));
            RegisterCommand("particle", (List<string> _params) => StartCoroutine(ApplyCmdWaiting(CmdFunc_particle_Task(_params))));   
            RegisterCommand("async_image", (List<string> _params) => StartCoroutine(CmdFunc_image_Task(_params)));
            RegisterCommand("async_remove", (List<string> _params) => StartCoroutine(CmdFunc_remove_Task(_params)));
            RegisterCommand("async_effect", (List<string> _params) => StartCoroutine(CmdFunc_effect_Task(_params)));
            RegisterCommand("async_audio", (List<string> _params) => StartCoroutine(CmdFunc_audio_Task(_params)));

            // Register Default Effect-Functions.
            RegisterEffect("spFadeIn", EffectSpriteAlphaFadeIn);
            RegisterEffect("spFadeOut", EffectSpriteAlphaFadeOut);
            RegisterEffect("moveTo", EffectPositionMove);
            RegisterEffect("audioTransit", EffectAudioVolumeTransit);
            RegisterEffect("canvasGroupTransit", EffectCanvasGroupAlphaTransit);
            
            SetPrefabPath("image", "DefaultImagePrefab");
            SetPrefabPath("audio", "DefaultAudioPrefab");

            isDefaultLogEnable=true;
        }

        #region Public Function
        public void RegisterCommand(string _str, CharFunction _charFunction)
        {
            specialCharFuncMap.Add(_str, _charFunction);
        }
        public void RegisterEffect(string effectName, EffectFunction effectFunction){
            effectMap.Add(effectName, effectFunction);
        }
        public void SetScreenReference(float width, float height){
            screenWidthReference = width;
            screenHeightReference = height;
        }
        public void RegisterToSceneObject(string key, GameObject gameObject, bool force=true){
            if(!sceneObjectMap.ContainsKey(key)){
                sceneObjectMap[key] = gameObject;
            }else{
                if(force){
                    sceneObjectMap[key] = gameObject;
                }else{
                    throw new Exception($"Scene object id - {key} already eixsts.");
                }
            }
        }
        public GameObject GetSceneObject(string key, bool raiseException=true){
            if(sceneObjectMap.ContainsKey(key)){
                return sceneObjectMap[key];
            }else{
                if(raiseException){
                    throw new Exception($"Scene object - {key} not exists.");
                }
                return null;
            }
        }
        public List<GameObject> QuerySceneObject(string regexPattern){
            Regex regex = new Regex(regexPattern);
            List<GameObject> queryObjects = new List<GameObject>();
            foreach(KeyValuePair<string, GameObject> kvp in this.sceneObjectMap)
            {
                if(regex.IsMatch(kvp.Key)){
                    queryObjects.Add(kvp.Value);
                }
            }
            return queryObjects;
        }
        public void RemoveSceneObject(string key){
            var _obj = GetSceneObject(key, false);
            if(_obj != null){
                Destroy(_obj);
                sceneObjectMap.Remove(key);
            }
        }
        public void SetPrefabPath(string key, string path){
            prefabPathMap[key] = path;
        }
        public string GetPrefabPath(string key){
            if(prefabPathMap.ContainsKey(key)){
                return prefabPathMap[key];
            }else{
                throw new Exception($"Get prefab path failed, key - {key} not exists.");
            }
        }
        #endregion

        #region Raise Event
        private void Log(string message, LogType logType=LogType.Info){
            if(logHappened != null){
                logHappened(this, new LogEventArgs(logType, message));
            }
        }
        private void UpdateText(string text){
            if(textUpdated !=null){
                textUpdated(this, new TextUpdateEventArgs(text));
            }
        }
        #endregion

        #region Default Event Function
        public void DefaultLogFunction(object sender, LogEventArgs args){
            switch(args.type){
                case LogType.Info:
                    Debug.Log(args.message);
                break;
                case LogType.Warning:
                    Debug.LogWarning(args.message);
                break;
                case LogType.Error:
                    Debug.LogError(args.message);
                break;
            }
        }
        #endregion

        #region User Function
        public void Next()
        {
            isWaitingForNextToGo = false;
        }
        public void SetTextList(List<string> textList, int index=0){
            this.currentTextList.Clear();
            this.currentTextListIndex = index;
            this.isTextListCompleted=false;
            this.currentTextList = new List<string>(textList);
            this.currentTextListResource = "";
        }
        public void ReadTextFromResource(string filePath, int index=0){
            // Log($"Read text from file - {filePath}");
            try{
                TextAsset textAsset = LoadResource<TextAsset>(filePath);
                this.currentTextList.Clear();
                this.isTextListCompleted=false;
                this.currentTextListIndex = index;

                var lineTextData = textAsset.text.Split('\n');
                foreach (string line in lineTextData)
                {
                    this.currentTextList.Add(line);
                }
                this.currentTextListResource = filePath;
            }catch(Exception e){
                Log($"ReadTextFromResource failed.\n{e.ToString()}", LogType.Error);
            }
        }
        public void SetVariable(string key, string value){
            this.variableMap[key] = value;
        }
        public void RemoveVariable(string key){
            if(this.variableMap.ContainsKey(key)){
                this.variableMap.Remove(key);
            }
        }
        public void SetupDialog(string resourcePath="DefaultDialogPrefab", bool applyDefaultTextUpdateEvent=true){
            try{
                var dialogPrefab = LoadResource<GameObject>(resourcePath);
                var dialog = CreateAsSceneObject("_Dialog", dialogPrefab, Vector3.zero, false);
                if(applyDefaultTextUpdateEvent){
                    try{
                        var e = typeof(FlowerSystem).GetEvent("textUpdated");
                        var text = dialog.transform.Find("DialogPanel/DialogText").GetComponent<Text>();
                        text.text = "";
                        _defaultTextUpdateHandler = (object sender, TextUpdateEventArgs args)=>{
                            text.text = args.text;
                        };;
                        textUpdated += _defaultTextUpdateHandler;
                    }catch(Exception){
                        throw new Exception("Apply default text update event failed.\nPlease make sure DialogPanel->DialogText object is exists with UI Text compoment.");
                    }
                }
            }catch(Exception e){
                Log($"Setup dialog UI failed.\n{e}", LogType.Error);
            }
        }
        public void SetupButtonGroup(string resourcePath="DefaultButtonGroupPrefab"){
            try{
                var buttonGroupPrefab = LoadResource<GameObject>(resourcePath);
                CreateAsSceneObject("_ButtonGroup", buttonGroupPrefab, Vector3.zero, false);
            }catch(Exception e){
                Log(e.ToString(), LogType.Error);
            }
        }
        public void SetupButton(string info, Action triggerFunction, string resourcePath="DefaultButtonPrefab"){
            try{
                var buttonPrefab = LoadResource<GameObject>(resourcePath);
                var buttonGroup = GetSceneObject("_ButtonGroup");
                var buttonGroupPanel = buttonGroup.transform.Find("ButtonPanel");
                if(buttonGroupPanel){
                    var button = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity);
                    button.GetComponent<Button>().onClick.AddListener(()=>{triggerFunction();});
                    var text = button.transform.Find("Text");
                    text.GetComponent<Text>().text = info;
                    button.transform.SetParent(buttonGroupPanel);
                    button.transform.localScale = Vector3.one;
                }else{
                    throw new Exception($"\"ButtonPanel\" not found in Button Group UI prefab.");
                }
            }catch(Exception e){
                Log(e.ToString(), LogType.Error);
            }
        }
        public void RemoveDialog(){
            if(_defaultTextUpdateHandler != null){
                textUpdated -= _defaultTextUpdateHandler;
                _defaultTextUpdateHandler = null;
            }
            RemoveSceneObject("_Dialog");
        }
        public void RemoveButtonGroup(){
            RemoveSceneObject("_ButtonGroup");
        }
        public void Stop(){
            this.isStop = true;
        }
        public void Resume(){
            this.isStop = false;
        }
        public void InvokeSpecialCommand(string specialCmd){
            var _words = specialCmd.Split(SPECIAL_CHAR_PARAMS_SEPARATOR);
            List<string> specialCmdWords = new List<string>(_words);
            try{
                string _cmd = specialCmdWords[0].ToString();
                if (specialCharFuncMap.ContainsKey(_cmd))
                {
                    specialCmdWords.RemoveAt(0);
                    for(int i=0;i<specialCmdWords.Count;i++){
                        specialCmdWords[i] = tryParseParameterWithVariable(specialCmdWords[i]);
                    }
                    specialCharFuncMap[_cmd](specialCmdWords);
                    // Log("The keyword : [" + _cmd + "] execute!");
                }
                else
                    Log("The keyword : [" + specialCmd + "] is not exists!", LogType.Error);
            }catch(Exception e){
                Log($"Execute command {specialCmd} Failed.\n{e}", LogType.Error);
            }
        }
        #endregion

        #region Keywords Function
        private IEnumerator ApplyCmdWaiting(IEnumerator task){
            isOnCmdEvent = true;
            yield return task;
            isOnCmdEvent = false;
            yield return null;
        }
        private IEnumerator CmdFunc_l_Task(List<string> _params)
        {
            isWaitingForNextToGo = true;
            yield return new WaitUntil(() => isWaitingForNextToGo == false);
            yield return null;
        }
        private IEnumerator CmdFunc_r_Task(List<string> _params)
        {
            msgText.Append("\n");
            yield return null;
        }
        private IEnumerator CmdFunc_w_Task(List<string> _params)
        {
            isWaitingForNextToGo = true;
            yield return new WaitUntil(() => isWaitingForNextToGo == false);
            msgText.Clear();   //Erase the messages.
            yield return null;
        }
        private IEnumerator CmdFunc_lr_Task(List<string> _params)
        {
            isWaitingForNextToGo = true;
            yield return new WaitUntil(() => isWaitingForNextToGo == false);
            msgText.Append("\n");
            yield return null;
        }
        private IEnumerator CmdFunc_c_Task(List<string> _params)
        {
            msgText.Clear();
            yield return null;
        }
        private IEnumerator CmdFunc_stop_Task(List<string> _params)
        {
            Stop();
            yield return new WaitUntil(() => this.isStop == false);
            yield return null;
        }
        private IEnumerator CmdFunc_resume_Task(List<string> _params)
        {
            Resume();
            yield return null;
        }
        private IEnumerator CmdFunc_dialogHide_Task(List<string> _params)
        {
            float milliSec = 1000;
            try{
                milliSec = float.Parse(_params[0].ToString().Trim());
            }catch(Exception){}
            try{
                CanvasGroup canvasGroup = GetSceneObject("_Dialog").GetComponent<CanvasGroup>();
                canvasGroup.alpha = 1;
                EffectCanvasGroupAlphaTransit("_Dialog", new List<string>(){"0",milliSec.ToString()});
            }catch(Exception e){
                throw new Exception($"Hide failed.\n{e}");
            }
            yield return new WaitUntil(() => this.animatingList.Count == 0);
            yield return null;
        }
        private IEnumerator CmdFunc_dialogShow_Task(List<string> _params)
        {
            float milliSec = 1000;
            try{
                milliSec = float.Parse(_params[0].ToString().Trim());
            }catch(Exception){}
            try{
                CanvasGroup canvasGroup = GetSceneObject("_Dialog").GetComponent<CanvasGroup>();
                canvasGroup.alpha = 0;
                EffectCanvasGroupAlphaTransit("_Dialog", new List<string>(){"1",milliSec.ToString()});
            }catch(Exception e){
                throw new Exception($"Show failed.\n{e}");
            }
            yield return new WaitUntil(() => this.animatingList.Count == 0);
            yield return null;
        }
        private IEnumerator CmdFunc_wait_Task(List<string> _params){
            float waitmilliSec;
            try{
                waitmilliSec = float.Parse(_params[0].ToString().Trim());
            }catch(Exception e){
                throw new Exception($"Invalid parameters.\n{e}");
            }
            yield return new WaitForSeconds(waitmilliSec/1000);
            yield return null;
        }
        private IEnumerator CmdFunc_image_Task(List<string> _params)
        {
            string key;
            Sprite sp;
            int x;
            int y;
            int orderInLayer=0;
            string effectName="";
            try{
                key = _params[0].ToString();
                sp = LoadResource<Sprite>(_params[1].ToString());
                x = int.Parse(_params[2].ToString());
                y = int.Parse(_params[3].ToString());
                try{
                    orderInLayer = int.Parse(_params[4].ToString());
                    effectName = _params[5].ToString();
                }catch(Exception){
                }
            }catch(Exception e){
                throw new Exception($"Invalid parameters.\n{e}");
            }

            GameObject sceneObj = GetSceneObject(key, false);
            try{
                var imagePrefab = LoadResource<GameObject>(GetPrefabPath("image"));
                if(!sceneObj){
                    sceneObj = CreateAsSceneObject(key, imagePrefab, new Vector3(ConvertPixelToUnit(x), ConvertPixelToUnit(y), 0));
                }
                sceneObj.name = $"flower-image-{key}";
                SpriteRenderer spr = sceneObj.GetComponent<SpriteRenderer>();
                spr.sprite = sp;
                spr.sortingOrder = orderInLayer;
                float spScale = GetSpriteScale(sp, Camera.main);
                sceneObj.transform.localScale = new Vector3(spScale, spScale, sceneObj.transform.localScale.z);
            }catch(Exception e){
                throw new Exception($"Set image failed.\n{e}");
            }
            
            ApplyEffect(key, effectName);
            yield return new WaitUntil(() => this.animatingList.Count == 0);
        }
        private IEnumerator CmdFunc_remove_Task(List<string> _params)
        {
            string key;
            string effectName = "";
            try{
                key = _params[0].ToString();
                try{
                    effectName = _params[1].ToString();
                }catch(Exception){
                }
            }catch(Exception e){
                throw new Exception($"Invalid parameters.\n{e}");
            }

            GameObject sceneObj = GetSceneObject(key);
            if(sceneObj){
                if(effectName != ""){
                    ApplyEffect(key, effectName);
                    yield return new WaitUntil(() => this.animatingList.Count == 0);
                }
                RemoveSceneObject(key);
            }else{
                Log($"Remove - {key} failed.", LogType.Error);
            }
        }
        private IEnumerator CmdFunc_effect_Task(List<string> _params)
        {
            string key;
            string effectName;
            try{
                key = _params[0].ToString();
                effectName = _params[1].ToString();
            }catch(Exception e){
                throw new Exception($"Invalid parameters.\n{e}");
            }

            GameObject sceneObj = GetSceneObject(key, false);
            ApplyEffect(key, effectName);
            yield return new WaitUntil(() => this.animatingList.Count == 0);
        }
        private IEnumerator CmdFunc_particle_Task(List<string> _params)
        {
            string key;
            string particlePath;
            int x;
            int y;
            try{
                key = _params[0].ToString();
                particlePath = _params[1].ToString();
                x = int.Parse(_params[2].ToString());
                y = int.Parse(_params[3].ToString());
            }catch(Exception e){
                throw new Exception($"Invalid parameters.\n{e}");
            }

            GameObject sceneObj = GetSceneObject(key, false);
            try{
                if(!sceneObj){
                    var particlePrefab = LoadResource<GameObject>(particlePath);
                    sceneObj = CreateAsSceneObject(key, particlePrefab, new Vector3(ConvertPixelToUnit(x), ConvertPixelToUnit(y), 0));
                }else{
                    throw new Exception($"Particle - {key} already exists.");
                }
                sceneObj.name = $"flower-particle-{key}";
            }catch(Exception e){
                throw new Exception($"Set particle failed.\n{e}");
            }
            yield return null;
        }
        private IEnumerator CmdFunc_audio_Task(List<string> _params)
        {  
            string key;
            AudioClip audio;
            bool loop;
            float volume = 1;
            string effectName = "";

            try{
                key = _params[0].ToString();
                audio = LoadResource<AudioClip>(_params[1].ToString());
                loop = bool.Parse(_params[2].ToString());
                try{
                    volume = float.Parse(_params[3].ToString());
                    effectName = _params[4].ToString();
                }catch(Exception){

                }
            }catch(Exception e){
                throw new Exception($"Invalid parameters.\n{e}");
            }

            GameObject sceneObj = GetSceneObject(key, false);
            try{
                if(!sceneObj){
                    var audioPrefab = LoadResource<GameObject>(GetPrefabPath("audio"));
                    sceneObj = CreateAsSceneObject(key, audioPrefab, new Vector3(0, 0, 0));
                }
                AudioSource audioSource =  sceneObj.GetComponent<AudioSource>();
                audioSource.clip = audio;
                audioSource.loop = loop;
                audioSource.volume = volume;
                audioSource.Play();
                sceneObj.name = $"flower-audio-{key}";
            }catch(Exception e){
                throw new Exception($"Set audio failed.\n{e}");
            }

            if(effectName != ""){
                ApplyEffect(key, effectName);
                yield return new WaitUntil(() => this.animatingList.Count == 0);
            }

            yield return null;
        }
        #endregion

        #region Messages Core
        private void AddChar(char _char)
        {
            msgText.Append(_char);
            lastChar = _char;
            UpdateText(text);
        }
        private void SetText(string _text)
        {
            StartCoroutine(SetTextTask(_text));
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
                    if(this.variableMap.ContainsKey(varName)){
                        this.variableString = this.variableMap[varName];
                        specialCmd = "";
                        isOnSpecialChar = false;
                        return SpecialCharType.Variable;
                    }
                }
                InvokeSpecialCommand(specialCmd);
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
            isTextCompleted = false;
            specialCmd = "";

            Dictionary<int, int> richTextIndexMap = new Dictionary<int, int>();
            if(isRichTextEnable){
                var matches = richTextRegex.Matches(_text);
                foreach (Match match in matches)
                {
                    GroupCollection groups = match.Groups;
                    richTextIndexMap[groups[0].Index] = groups[0].Length;
                }
            }
            
            int i = 0;
            float _deltaTimer = Time.deltaTime;
            int richModeCountDown = 0;
            while(i < _text.Length){ 
                while(true){
                    bool fastForward = false;

                    if (isRichTextEnable && richTextIndexMap.ContainsKey(i)){
                        richModeCountDown = richTextIndexMap[i];
                    }

                    switch (CheckSpecialChar(_text[i]))
                    {
                        case SpecialCharType.NormalChar:
                            AddChar(_text[i]);
                            break;
                        case SpecialCharType.Variable:
                            int _index = 0;
                            while(_index < this.variableString.Length){
                                AddChar(this.variableString[_index]);
                                lastChar = this.variableString[_index];
                                _index ++;
                            }
                            break;
                        case SpecialCharType.CmdChar:
                            // If in command mode, do the fast forward.
                            fastForward = true;
                            break;
                    }
                    if(richModeCountDown > 0){
                        richModeCountDown -=1;
                        fastForward = true;
                    }
                    
                    _deltaTimer -= textSpeed;
                    lastChar = _text[i];
                    i += 1;
                    if(fastForward)break;
                    if(i >= _text.Length){
                        break;
                    }
                    if(this.isStop){
                        yield return new WaitUntil(() => isStop == false);
                    }
                    if(isOnCmdEvent){
                        yield return new WaitUntil(() => isOnCmdEvent == false);
                    }
                    if(_deltaTimer < 0){
                        yield return new WaitForSeconds(-_deltaTimer);
                        break;
                    }
                }
                _deltaTimer = Time.deltaTime;
            }
            if(isOnCmdEvent){
                yield return new WaitUntil(() => isOnCmdEvent == false);
            }
            isTextCompleted = true;
            yield return null;
        }
        private float GetSpritePPU(Sprite sp){
            float pixelPerUnit = sp.rect.width / sp.bounds.size.x;
            return pixelPerUnit;
        }
        private float GetCameraPPU(Camera camera, float height){
            float pixelPerUnit = height / (camera.orthographicSize * 2);
            return pixelPerUnit;
        }
        private float GetSpriteScale(Sprite sp, Camera camera){
            float spPPU = GetSpritePPU(sp);
            float spHeightUnit = sp.rect.height/spPPU;
            float spScale = sp.rect.height / (screenHeightReference<0 ? Screen.height : screenHeightReference) * (camera.orthographicSize*2) / spHeightUnit;
            return spScale;
        }
        private float ConvertPixelToUnit(float val){
            float cameraPPU = GetCameraPPU(Camera.main, screenHeightReference<0 ? Screen.height : screenHeightReference);
            return val/cameraPPU;
        }
        private void ApplyEffect(string key, string effectStatement){
            if(effectStatement == "")return;
            var _words = effectStatement.Split(SPECIAL_CHAR_EFFECT_SEPARATOR);
            List<string> specialEffectWords = new List<string>(_words);
            string effectName = specialEffectWords[0]; 
            if(effectMap.ContainsKey(effectName)){
                specialEffectWords.RemoveAt(0);
                for(int i=0;i<specialEffectWords.Count;i++){
                    specialEffectWords[i] = tryParseParameterWithVariable(specialEffectWords[i]);
                }
                effectMap[effectName](key, specialEffectWords);
            }else{
                Log($"Effect - {effectName} not registered.", LogType.Error);
            }
        }
        private T LoadResource<T>(string path, bool raiseException=true) where T:UnityEngine.Object{
            var obj = Resources.Load<T>(path) as T;
            if(obj!=null){
                return obj;
            }else{
                throw new Exception($"Resource - {path} not exists.");
            }
        }
        private GameObject CreateAsSceneObject(string key, GameObject prefab, Vector3 position, bool force=false){
            if(!force && GetSceneObject(key, false)!=null){
                throw new Exception($"Spawn scene object failed. key - {key} already exists.");
            }
            var sceneObj = Instantiate(prefab, position, Quaternion.identity);
            if(!elementsDestroyOnLoad){
                DontDestroyOnLoad(sceneObj);
            }
            RegisterToSceneObject(key, sceneObj);
            return sceneObj;
        }
        private string tryParseParameterWithVariable(string param, bool raiseException=false){
            if(param[0] == VAR_CHAR){
                var varName = param.Substring(1,param.Length-1);
                if(this.variableMap.ContainsKey(varName)){
                    return this.variableMap[varName];
                }else{
                    if(raiseException){
                        throw new Exception($"Try parse parameter with variable failed. var-name : {varName} not exists.");
                    }else{
                        return param;
                    }
                }
            }else{
                return param;
            }
        }
        #endregion

        #region Effect Tasks
        public IEnumerator EffectTimerTask(string key, float endTime, Action<float> effectTimerFunction){
            animatingList.Add(key);
            float currentTime = 0;
            while(currentTime < endTime){
                currentTime = (currentTime+Time.deltaTime)>endTime ? endTime : (currentTime+Time.deltaTime);
                effectTimerFunction(currentTime/endTime);
                yield return null;
            }
            int _keyIndex = animatingList.IndexOf(key);
            animatingList.RemoveAt(_keyIndex);
            yield return null;
        }
        private IEnumerator ChangeCanvasGroupAlphaTask(string key, CanvasGroup canvasGroup ,float startAlpha, float endAlpha, float endTime=1){
            yield return EffectTimerTask(key, endTime, (percent)=>{
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, percent);
            });
        }
        private IEnumerator ChangeSpriteColorTask(string key, SpriteRenderer spr, Color startColor, Color endColor, float endTime=1){
            yield return EffectTimerTask(key, endTime, (percent)=>{
                spr.color = Color.Lerp(startColor, endColor, percent);
            });
        }
        private IEnumerator ChangePositionTask(string key, GameObject obj, Vector3 fromPos, Vector3 toPos, float endTime=1){
            yield return EffectTimerTask(key, endTime, (percent)=>{
                obj.transform.position = Vector3.Lerp(fromPos, toPos, percent);
            });
        }
        private IEnumerator ChangeAudioVolumeTask(string key, AudioSource audioSource, float startVolume, float endVolume, float endTime=1){
            yield return EffectTimerTask(key, endTime, (percent)=>{
                audioSource.volume = Mathf.Lerp(startVolume, endVolume, percent);
            });
        }
        #endregion

        #region Effects
        private void EffectSpriteAlphaFadeIn(string key, List<string> _params){
            try{
                float endTime;
                try{
                    endTime = float.Parse(_params[0])/1000;
                }catch(Exception e){
                    throw new Exception($"Invalid effect parameters.\n{e}");
                }

                SpriteRenderer spr = GetSceneObject(key).GetComponent<SpriteRenderer>();
                var spriteStartColor = spr.color;
                spriteStartColor.a = 0;
                var spriteEndColor = spr.color;
                StartCoroutine( ChangeSpriteColorTask($"fadeIn-{key}", spr,  spriteStartColor, spriteEndColor, endTime));
            }catch(Exception){
                Log($"Effect - SpriteAlphaFadeIn @ [{key}] failed.", LogType.Error);
            }
        }
        private void EffectSpriteAlphaFadeOut(string key, List<string> _params){
            try{
                float endTime;
                try{
                    endTime = float.Parse(_params[0])/1000;
                }catch(Exception e){
                    throw new Exception($"Invalid effect parameters.\n{e}");
                }

                SpriteRenderer spr = GetSceneObject(key).GetComponent<SpriteRenderer>();
                var spriteStartColor = spr.color;
                var spriteEndColor = spr.color;
                spriteEndColor.a = 0;
                
                StartCoroutine( ChangeSpriteColorTask($"fadeOut-{key}", spr,  spriteStartColor, spriteEndColor, endTime));
            }catch(Exception){
                Log($"Effect - spriteAlphaFadeOut @ [{key}] failed.", LogType.Error);
            }
        }
        private void EffectPositionMove(string key, List<string> _params){
            try{
                float x;
                float y;
                float endTime;
                try{
                    x = float.Parse(_params[0]);
                    y = float.Parse(_params[1]);
                    endTime = float.Parse(_params[2])/1000;
                }catch(Exception e){
                    throw new Exception($"Invalid effect parameters.\n{e}");
                }

                GameObject sceneObj = GetSceneObject(key);
                StartCoroutine(ChangePositionTask(
                    $"position-{key}",
                    sceneObj,
                    sceneObj.transform.position,
                    new Vector3(ConvertPixelToUnit(x), ConvertPixelToUnit(y), sceneObj.transform.position.z),
                    endTime
                ));
            }catch(Exception){
                Log($"Effect - spriteAlphaFadeOut @ [{key}] failed.", LogType.Error);
            }
        }
        private void EffectAudioVolumeTransit(string key, List<string> _params){
            try{
                float toVolume;
                float endTime;
                try{
                    toVolume = float.Parse(_params[0]);
                    endTime = float.Parse(_params[1])/1000;
                }catch(Exception e){
                    throw new Exception($"Invalid effect parameters.\n{e}");
                }
                AudioSource audioSource = GetSceneObject(key).GetComponent<AudioSource>();
                StartCoroutine( ChangeAudioVolumeTask($"AudioVolumeTransit-{key}", audioSource, audioSource.volume, toVolume, endTime));
            }catch(Exception){
                Log($"Effect - AudioVolumeTransit @ [{key}] failed.", LogType.Error);
            }
        }
        private void EffectCanvasGroupAlphaTransit(string key, List<string> _params){
            try{
                float toAlpha;
                float endTime;
                try{
                    toAlpha = float.Parse(_params[0]);
                    endTime = float.Parse(_params[1])/1000;
                }catch(Exception e){
                    throw new Exception($"Invalid effect parameters.\n{e}");
                }
                CanvasGroup canvasGroup = GetSceneObject(key).GetComponent<CanvasGroup>();
                StartCoroutine( ChangeCanvasGroupAlphaTask($"canvasGroupAlpha-{key}", canvasGroup, canvasGroup.alpha, toAlpha , endTime));
            }catch(Exception){
                Log($"Effect - effectCanvasGroupAlphaTransit @ [{key}] failed.",LogType.Error);
            }
        }
        #endregion

        void Update() {
            if(!isTextListCompleted && this.isTextCompleted && !this.isStop){
                if(this.currentTextListIndex < this.currentTextList.Count){
                    SetText(this.currentTextList[this.currentTextListIndex]);
                    this.currentTextListIndex++;
                }else{
                    this.isTextListCompleted = true;
                }
            }
        }
    }

    public class LogEventArgs: System.EventArgs{
        public LogType type;
        public string message;
        public LogEventArgs(LogType _type, string _message){
            type = _type;
            message = _message;
        }
    }
    public class TextUpdateEventArgs: System.EventArgs{
        public string text;
        public TextUpdateEventArgs(string _text){
            text = _text;
        }
    }
    public enum LogType{
        Info,
        Warning,
        Error
    }
}