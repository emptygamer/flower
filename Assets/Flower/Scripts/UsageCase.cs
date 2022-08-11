using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RemptyTool.Flowser;

public class UsageCase : MonoBehaviour
{
    [SerializeField]
    ESMessageSystem msgSys;
    
    private string myName;
    private int progress = 0;
    private bool pickedUpTheKey = false;
    private bool isGameEnd = false;
    private bool isLocked = false;
    void Start()
    {
        myName = "Rempty (｢･ω･)｢";
        // Define your customized keyword functions.
        msgSys.AddSpecialCharToFuncMap("UsageCase", CustomizedFunction);
    }
    private void CustomizedFunction()
    {
        Debug.Log("Hi! This is called by CustomizedFunction!");
    }

    void Update()
    {
        // ----- Integration DEMO -----
        // Your own logic control.
        if(msgSys.isCompleted && !isGameEnd && !isLocked){
            switch(progress){
                case 0:
                    msgSys.ReadTextFromResource("start");
                    break;
                case 1:
                    msgSys.SetDisplayVariable("MyName", myName);
                    msgSys.ReadTextFromResource("demo_start");
                    break;
                case 2:
                    msgSys.SetupButtonUIPrefab("DefaultButtonUIPrefab");
                    if(!pickedUpTheKey){
                        msgSys.SetupButtonItem("DefaultButtonItemPrefab","Pickup the key.",()=>{
                            pickedUpTheKey = true;
                            msgSys.Resume();
                            msgSys.RemoveButtonUI();
                            msgSys.ReadTextFromResource("demo_key");
                            progress = 2;
                            isLocked=false;
                        });
                    }
                    msgSys.SetupButtonItem("DefaultButtonItemPrefab","Open the door",()=>{
                        if(pickedUpTheKey){
                            msgSys.Resume();
                            msgSys.RemoveButtonUI();
                            msgSys.ReadTextFromResource("demo_door");
                            isLocked=false;
                        }else{
                            msgSys.Resume();
                            msgSys.RemoveButtonUI();
                            msgSys.ReadTextFromResource("demo_locked_door");
                            progress = 2;
                            isLocked=false;
                        }
                    });
                    isLocked=true;
                    break;
                case 3:
                    isGameEnd=true;
                    break;
            }
            progress ++;
        }

        if (!isGameEnd && Input.GetKeyDown(KeyCode.Space))
        {
            //Continue the messages, stoping by [w] or [lr] keywords.
            msgSys.Next();
        }
    }
}
