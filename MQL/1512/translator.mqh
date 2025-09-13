//+------------------------------------------------------------------+
//|                                                   Translator.mqh |
//|                                             Copyright 2013, Rone |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone"
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//| includes                                                         |
//+------------------------------------------------------------------+
#include <LanguagesEnum.mqh>
//+------------------------------------------------------------------+
//| Class CTranslator.                                               |
//| Appointment: Class for translating different messges.            |
//+------------------------------------------------------------------+
class CTranslator {
   private:
      string         filename;
   public:
                     CTranslator();
                    ~CTranslator();
      bool           init(string progName, ENUM_LANGUAGES lang);
      string         tr(string str);
      void           print(string str);
      void           alert(string str);
      void           comment(string str);
      int            messageBox(string text, string caption=NULL, int flags=0);
      
   private:
      string         getTranslate(string str);
      void           stringTrimBoth(string &str);
};
//+------------------------------------------------------------------+
//| Constructor CTranslator.                                         |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
CTranslator::CTranslator() {
}
//+------------------------------------------------------------------+
//| Destructor CTranslator.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
CTranslator::~CTranslator() {
}
//+------------------------------------------------------------------+
//| Initialization of object.                                        |
//| INPUT:  progName - name of the MQL5 program,                     |
//|         lang - language into which we translate                  |
//| OUTPUT: true - if successful, false - if not.                    |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CTranslator::init(string progName,ENUM_LANGUAGES lang) {
   string language = EnumToString(lang);
   StringToLower(language);
   
   filename = "Languages\\" + progName + "." + language + ".ini";
   
   if ( !FileIsExist(filename) ) {
      Print("WARNING(", __FUNCTION__ ,"): the file named \"", filename, "\" doesn't exist! Program will use aliases.");
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//| Translate string.                                                |
//| INPUT:  string to translate.                                     |
//| OUTPUT: translated string.                                       |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
string CTranslator::tr(string str) {
   return(getTranslate(str));
}
//+------------------------------------------------------------------+
//| Print translated string.                                         |
//| INPUT:  string to translate.                                     |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CTranslator::print(string str) {
   Print(getTranslate(str));
}
//+------------------------------------------------------------------+
//| Display translated string in a separate (alert) window.          |
//| INPUT:  string to translate.                                     |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CTranslator::alert(string str) {
   Alert(getTranslate(str));
}
//+------------------------------------------------------------------+
//| Display translated string as a comment in the top left corner    |
//| of a chart.                                                      |
//| INPUT:  string to translate.                                     |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CTranslator::comment(string str) {
   Comment(getTranslate(str));
}
//+------------------------------------------------------------------+
//| Display translated string in a message box window.               |
//| INPUT:  str - string to translate.                               |
//|         caption - optional to be translated and displayed in the |
//|               box header.                                        |
//|         flags - optional MessageBox flags.                       |
//| OUTPUT: one of values of MessageBox return codes.                |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CTranslator::messageBox(string str,string caption=NULL,int flags=0) {
   return(MessageBox(getTranslate(str), getTranslate(caption), flags));
}
//+------------------------------------------------------------------+
//| Get trasnlation.                                                 |
//| INPUT:  str - string to translate.                               |
//| OUTPUT: translation - if successful, input string - if not.      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
string CTranslator::getTranslate(string str) {
//---
   int fileHandle = FileOpen(filename, FILE_READ|FILE_TXT|FILE_ANSI, 0, CP_UTF8);
   
   if ( fileHandle == INVALID_HANDLE ) {
      Print("Can't open file named \"", filename, "\"");
      return(str);
   }
//---
   string temp, alias, traslation;
   int delimiterPos;
   
   stringTrimBoth(str);
   
   for ( ; !FileIsEnding(fileHandle); ) {
      temp = FileReadString(fileHandle);
      delimiterPos = StringFind(temp, "=");
      alias = StringSubstr(temp, 0, delimiterPos);
      stringTrimBoth(alias);
      
      if ( StringCompare(str, alias, false) == 0 ) {
         traslation = StringSubstr(temp, delimiterPos+1);
         stringTrimBoth(traslation);
         FileClose(fileHandle);
         
         return(traslation);
      }
   }
   FileClose(fileHandle);
//---
   return(str);
}
//+------------------------------------------------------------------+
//| Cut line feed characters, spaces and tabs in the both side.      |
//| INPUT:  string to be cut.                                        |
//| OUTPUT: no.                                                      |
//| REMARK: string is modified at place.                             |
//+------------------------------------------------------------------+
void CTranslator::stringTrimBoth(string &str) {
   StringTrimLeft(str);
   StringTrimRight(str);
}
//+------------------------------------------------------------------+
