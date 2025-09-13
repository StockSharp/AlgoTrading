//+------------------------------------------------------------------+
//|                                              StringCodepages.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "='", (A), "'")

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("Locales");
   uchar bytes1[], bytes2[];

   string german = "straßenführung";
   string russian = "Русский Текст";

   // copy german text using european ACP,
   // on european Windows this is equivalent of the short form:
   // StringToCharArray(german, bytes1);
   // because CP_ACP = 1252
   StringToCharArray(german, bytes1, 0, WHOLE_ARRAY, 1252);
   ArrayPrint(bytes1);
   
   // restore text back from the array: all is ok
   PRT(CharArrayToString(bytes1, 0, WHOLE_ARRAY, 1252));

   // now copy russian text with european ACP
   // or on Windows where default ACP is 1252 (CP_ACP)
   StringToCharArray(russian, bytes2, 0, WHOLE_ARRAY, 1252);
   ArrayPrint(bytes2);
   // bytes are already corrupted here (see log below),
   // because CP 1252 does not include Cyrillics
   
   // try to restore it and find out: Cyrillic symbols are gone
   PRT(CharArrayToString(bytes2, 0, WHOLE_ARRAY, 1252));
   
   // lets copy russian text using cyrillic ACP,
   // on Russian Windows this is equivalent of the short form:
   // StringToCharArray(russian, bytes2);
   // because CP_ACP = 1251
   StringToCharArray(russian, bytes2, 0, WHOLE_ARRAY, 1251);
   ArrayPrint(bytes2);
   // this time the bytes are meaningful
   
   // restore text back from the array: all is ok
   PRT(CharArrayToString(bytes2, 0, WHOLE_ARRAY, 1251));

   // now suppose we copy german text with cyrillic ACP,
   StringToCharArray(german, bytes1, 0, WHOLE_ARRAY, 1251);
   ArrayPrint(bytes1);
   // you can compare bytes1 with previous bytes1 content
   // a couple of symbols are different
   
   // try to restore it and find out: german specific symbols are damaged
   PRT(CharArrayToString(bytes1, 0, WHOLE_ARRAY, 1251));
   
   // now use UTF-8 both for german and russian text:
   // no matter which language your Windows is using,
   // you'll always get the text correctly restored
   Print("UTF8");
   StringToCharArray(german, bytes1, 0, WHOLE_ARRAY, CP_UTF8);
   ArrayPrint(bytes1);
   // text is ok
   PRT(CharArrayToString(bytes1, 0, WHOLE_ARRAY, CP_UTF8));
   
   StringToCharArray(russian, bytes2, 0, WHOLE_ARRAY, CP_UTF8);
   ArrayPrint(bytes2);
   // text is ok
   PRT(CharArrayToString(bytes2, 0, WHOLE_ARRAY, CP_UTF8));
   
   // note, that both UTF-8 encoded arrays are longer
   // than they were when ANSI codepages were used
   
   // also note, that array with Russian becomes much longer than before,
   // because all letters are now taking 2 bytes each
   
   /*
      output:

   Locales
      
   115 116 114  97 223 101 110 102 252 104 114 117 110 103   0
   CharArrayToString(bytes1,0,WHOLE_ARRAY,1252)='straßenführung'
   63 63 63 63 63 63 63 32 63 63 63 63 63  0
   CharArrayToString(bytes2,0,WHOLE_ARRAY,1252)='??????? ?????'
   208 243 241 241 234 232 233  32 210 229 234 241 242   0
   CharArrayToString(bytes2,0,WHOLE_ARRAY,1251)='Русский Текст'
   115 116 114  97  63 101 110 102 117 104 114 117 110 103   0
   CharArrayToString(bytes1,0,WHOLE_ARRAY,1251)='stra?enfuhrung'
   
   UTF8

   115 116 114  97 195 159 101 110 102 195 188 104 114 117 110 103   0
   CharArrayToString(bytes1,0,WHOLE_ARRAY,CP_UTF8)='straßenführung'
   208 160 209 131 209 129 209 129 208 186 208 184 208 185  32 208 162 208 181 208 186 209 129 209 130   0
   CharArrayToString(bytes2,0,WHOLE_ARRAY,CP_UTF8)='Русский Текст'
   
   */
}
//+------------------------------------------------------------------+