/*
 * Please note that the availability of fonts in the system depends on the options selected upon installation of the
 * operating system, as well as the software used.

 * There are a few fonts that come with any Windows version.
 * Those fonts are considered to be web safe for web design and e-documents
 * that are supposed to be equally displayed on different computers.

 * Web safe fonts are Arial, Courier, Courier New, MS Sans Serif, MS Serif, Symbol and Times New Roman.

 * There are fonts that do not come with earlier Windows versions but are present on just about all
 * computer systems (installed together with additional software, e.g. Microsoft Office).
 * Web safe fonts are Arial, Courier, Courier New, MS Sans Serif, MS Serif, Symbol and Times New Roman.

 * Almost web safe fonts: Comic Sans MS, Tahoma, Trebuchet MS, Verdana.
 */
//+------------------------------------------------------------------+
//|                                                  GetFontName.mqh |
//|                               Copyright © 2011, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "2011,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"

//+------------------------------------------------------------------+
//|  Enumeration for Windows fonts indices storage                   |
//+------------------------------------------------------------------+
enum type_font // font type
  {
   Font0, //Arial
   Font1, //Arial Black
   Font2, //Arial Bold
   Font3, //Arial Bold Italic
   Font4, //Arial Italic
   Font5, //Comic Sans MS Bold
   Font6, //Courier
   Font7, //Courier New
   Font8, //Courier New Bold
   Font9, //Courier New Bold Italic
   Font10, //Courier New Italic
   Font11, //Estrangelo Edessa
   Font12, //Franklin Gothic Medium
   Font13, //Gautami
   Font14, //Georgia
   Font15, //Georgia Bold
   Font16, //Georgia Bold Italic
   Font17, //Georgia Italic
   Font18, //Georgia Italic Impact
   Font19, //Latha
   Font20, //Lucida Console
   Font21, //Lucida Sans Unicode
   Font22, //Modern MS Sans Serif
   Font23, //MS Sans Serif
   Font24, //Mv Boli
   Font25, //Palatino Linotype
   Font26, //Palatino Linotype Bold
   Font27, //Palatino Linotype Italic
   Font28, //Roman
   Font29, //Script
   Font30, //Small Fonts
   Font31, //Symbol
   Font32, //Tahoma
   Font33, //Tahoma Bold
   Font34, //Times New Roman
   Font35, //Times New Roman Bold
   Font36, //Times New Roman Bold Italic
   Font37, //Times New Roman Italic
   Font38, //Trebuchet MS
   Font39, //Trebuchet MS Bold
   Font40, //Trebuchet MS Bold Italic
   Font41, //Trebuchet MS Italic
   Font42, //Tunga
   Font43, //Verdana
   Font44, //Verdana Bold
   Font45, //Verdana Bold Italic
   Font46, //Verdana Italic
   Font47, //Webdings
   Font48, //Westminster
   Font49, //Wingdings
   Font50, //WST_Czech
   Font51, //WST_Engl
   Font52, //WST_Fren
   Font53, //WST_Germ
   Font54, //WST_Ital
   Font55, //WST_Span
   Font56  //WST_Swed
  };
//+------------------------------------------------------------------+
//|  Font name obtaining class                                       |
//+------------------------------------------------------------------+
class CFontName
  {
public:
   string              GetFontName(type_font FontNumber)
     {
      string FontTypes[]=
        {
         "Arial",
         "Arial Black",
         "Arial Bold",
         "Arial Bold Italic",
         "Arial Italic",
         "Comic Sans MS Bold",
         "Courier",
         "Courier New",
         "Courier New Bold",
         "Courier New Bold Italic",
         "Courier New Italic",
         "Estrangelo Edessa",
         "Franklin Gothic Medium",
         "Gautami",
         "Georgia",
         "Georgia Bold",
         "Georgia Bold Italic",
         "Georgia Italic",
         "Georgia Italic Impact",
         "Latha",
         "Lucida Console",
         "Lucida Sans Unicode",
         "Modern MS Sans Serif",
         "MS Sans Serif",
         "Mv Boli",
         "Palatino Linotype",
         "Palatino Linotype Bold",
         "Palatino Linotype Italic",
         "Roman",
         "Script",
         "Small Fonts",
         "Symbol",
         "Tahoma",
         "Tahoma Bold",
         "Times New Roman",
         "Times New Roman Bold",
         "Times New Roman Bold Italic",
         "Times New Roman Italic",
         "Trebuchet MS",
         "Trebuchet MS Bold",
         "Trebuchet MS Bold Italic",
         "Trebuchet MS Italic",
         "Tunga",
         "Verdana",
         "Verdana Bold",
         "Verdana Bold Italic",
         "Verdana Italic",
         "Webdings",
         "Westminster",
         "Wingdings",
         "WST_Czech",
         "WST_Engl",
         "WST_Fren",
         "WST_Germ",
         "WST_Ital",
         "WST_Span",
         "WST_Swed"
        };

      return(FontTypes[int(FontNumber)]);
     };
  };
//+------------------------------------------------------------------+
