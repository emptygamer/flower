# 🌺 Flower
<img src="./flower_logo.png" alt="flower_logo"/>

### **Flower** is a dialog system which support the keywords format like KrKr engine in **Unity**.
(Flower is the next generation of 
[ES_MsgSystem-for-Unity](https://github.com/emptygamer/ES_MsgSystem-for-Unity))

Flower can integrate with your own game logic easily, setup with default **Dialog** and **Button** features.

- ## **Supported keywords in texts**
    |Keywords|Commands|
    |-----------|--------|
    |l|Wait for press.|
    |r|Change the line.|
    |lr|Wait for press, and change the line.|
    |w|Wait for press, and erase the text.|
    |c|Erase the text.|
    |hide|Hide the text panel.|
    |show|Show the text panel.|
    |stop|Stop the system.|
    |resume|Resume the system.|
    |#VAR_NAME|Display the variable value.|
- ## **Call keywords commands**
    ### **Use brackets (by defult) to set the keywords commands.**
    - The brackets can be changed to another characters.
    - If you want to print the brackets (or the special start command character), just repeat it to make the words not a command. 
        - ex : ( [[w] -> print "[w]" )
- ## **Use cases**
```
    1.  text:   Hello![l]World![w]
        result: Hello!(wait for press)
        result: Hello!World!(wait for press)

    2.  text:   Hello![r]World![w]
        result: Hello!
                World!(wait for press)

    3.  text:   Hello![lr]World![w]
        result: Hello!(wait for press)
        result: Hello!
                World!(wait for press)
    
    4.  text:   Hello![w]World![w]
        result: Hello!(wait for press)
        result: World!(wait for press)
```

- ## **Loading and Executing text files**
Simply load the txt from Resources folder and execute.
```C#
ESMessageSystem msgSys;
// This will load the "start.txt" from Resources folder and execute.
msgSys.ReadTextFromResource("start");
```

- ## **Setup UI**
Setup UI Objects.
- **Dialog UI (Auto Generate)**
    <br>Prefab(Canvas) -> DialogPanel(Image) -> DialogText(Text)
    ```C#
    ESMessageSystem msgSys;
    msgSys.SetupDialogUIPrefab("DefaultDialogUIPrefab");
    ```
- **Button UI**
    <br>Prefab(Canvas) -> ButtonPanel(Image)
    ```C#
    ESMessageSystem msgSys;
    msgSys.SetupDialogUIPrefab("DefaultDialogUIPrefab");
    ```
- **Button Item UI**
    <br>Prefab(Button) -> Text(Text)
    <br>Will put the button in **ButtonPanel**, need to setup **Button UI** first.
    ```C#
    ESMessageSystem msgSys;
    msgSys.SetupDialogUIPrefab("DefaultDialogUIPrefab");
    ```

- ## **Show variable values**
Show variable values in the dialog.
```C#
ESMessageSystem msgSys;
private string myName = "Rempty (｢･ω･)｢";
msgSys.SetDisplayVariable("MyName", myName);
// Call [#MyName] in the text will show the "Rempty (｢･ω･)｢".
```

- ## **Stop and Resume**
```C#
ESMessageSystem msgSys;
msgSys.Stop(); // Stop the system.
// Your gameplay here...
msgSys.Resume(); // Resume the system.

//-----
msgSys.ReadTextFromResource("stop")
// -- stop.txt --
// Stop here.
// [stop] // Invoke stop using text.
// ----------
// Your gameplay here...
msgSys.SetText("[Resume]") // Invoke resume using text.
```

- ## **Integrate and Logic Control**
**Check [UsageCase.cs]() for more detail.**