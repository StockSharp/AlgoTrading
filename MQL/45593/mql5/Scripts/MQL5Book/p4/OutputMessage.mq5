//+------------------------------------------------------------------+
//|                                                OutputMessage.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

//+------------------------------------------------------------------+
//| Message box buttons                                              |
//|                                                                  |
//| There can be up to 3 buttons, which close the dialog             |
//| with specific result code (see ENUM_MB_RESULT below).            |
//| There is also an option to show Help button with special         |
//| MB_HELP constant (which could count as 4-th button).             |
//| But it does not close the dialog and clicks on it are not        |
//| yet forwarded to MQL-programs.                                   |
//+------------------------------------------------------------------+
enum ENUM_MB_BUTTONS
{
   _OK = MB_OK,                                      // Ok
   _OK_CANCEL = MB_OKCANCEL,                         // Ok | Cancel
   _ABORT_RETRY_IGNORE = MB_ABORTRETRYIGNORE,        // Abort | Retry | Ignore
   _YES_NO_CANCEL = MB_YESNOCANCEL,                  // Yes | No | Cancel
   _YES_NO = MB_YESNO,                               // Yes | No
   _RETRY_CANCEL = MB_RETRYCANCEL,                   // Retry | Cancel
   _CANCEL_TRYAGAIN_CONTINUE = MB_CANCELTRYCONTINUE, // Cancel | Try Again | Continue
};

//+------------------------------------------------------------------+
//| Message box icons                                                |
//+------------------------------------------------------------------+
enum ENUM_MB_ICONS
{
   _ICON_NONE = 0,                                  // None
   _ICON_QUESTION = MB_ICONQUESTION,                // Question
   _ICON_INFORMATION_ASTERISK = MB_ICONINFORMATION, // Information (Asterisk)
   _ICON_WARNING_EXCLAMATION = MB_ICONWARNING,      // Warning (Exclamation)
   _ICON_ERROR_STOP_HAND = MB_ICONERROR,            // Error (Stop, Hand)
};

//+------------------------------------------------------------------+
//| Message box defaults (initially focused button)                  |
//+------------------------------------------------------------------+
enum ENUM_MB_DEFAULT
{
   _DEF_BUTTON1 = MB_DEFBUTTON1,  // Default button 1-st
   _DEF_BUTTON2 = MB_DEFBUTTON2,  // Default button 2-nd
   _DEF_BUTTON3 = MB_DEFBUTTON3,  // Default button 3-rd
   _DEF_BUTTON4 = MB_DEFBUTTON4,  // Default button 4-th
};

//+------------------------------------------------------------------+
//| Message box results                                              |
//+------------------------------------------------------------------+
enum ENUM_MB_RESULT
{
   _ID_UNDEFINED = 0,
   _ID_OK = IDOK,
   _ID_CANCEL = IDCANCEL,
   _ID_ABORT = IDABORT,
   _ID_RETRY = IDRETRY,
   _ID_IGNORE = IDIGNORE,
   _ID_YES = IDYES,
   _ID_NO = IDNO,
   _ID_TRYAGAIN = IDTRYAGAIN,
   _ID_CONTINUE = IDCONTINUE,
};

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string Message = "Message";
input string Caption = "";
input ENUM_MB_BUTTONS Buttons = _OK;
input ENUM_MB_ICONS Icon = _ICON_NONE;
input ENUM_MB_DEFAULT Default = _DEF_BUTTON1;
input bool ShowHelpButton = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string text = Message + "\n"
      + EnumToString(Buttons) + ", "
      + EnumToString(Icon) + ","
      + EnumToString(Default);
   ENUM_MB_RESULT result = (ENUM_MB_RESULT)
      MessageBox(text, StringLen(Caption) ? Caption : NULL, Buttons | Icon | Default | (ShowHelpButton ? MB_HELP : 0));
   Print(EnumToString(result));
   
   /*
   // example of result processing
   // you can use 'if' as well
   switch(result)
   {
   case IDYES:
     // ...
     break;
   case IDNO:
     // ...
     break;
   case IDCANCEL:
     // ...
     break;
   }
   */   
}
//+------------------------------------------------------------------+
