//+------------------------------------------------------------------+
//|                                                StringSymbols.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "='", (A), "'")

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string numbers = "0123456789";
   PRT(numbers);
   PRT(StringSetCharacter(numbers, 7, 0));   // truncate at 7
   PRT(numbers);                             // 0123456
   PRT(StringSetCharacter(numbers, StringLen(numbers), '*')); // add '*'
   PRT(numbers);                             // 0123456*
   
   PRT(StringGetCharacter(numbers, 5));      // 53 = code of '5'
   PRT(numbers[5]);                          // 53 as well
   
   PRT(CharToString(0xA9));   // "©"
   PRT(CharToString(0xE6));   // "æ", "ж", or other glyph
                              //           depending from Windows locale
   PRT(ShortToString(0x3A3)); // "Σ"
   PRT(ShortToString('Σ'));   // "Σ"

   ushort array1[], array2[]; // dynamic arrays
   ushort text[5];            // fixed array
   string alphabet = "ABCDEАБВГД";
   // copy including terminal '0'
   PRT(StringToShortArray(alphabet, array1)); // 11
   ArrayPrint(array1); // 65   66   67   68   69 1040 1041 1042 1043 1044    0
   // copy excluding terminal '0'
   PRT(StringToShortArray(alphabet, array2, 0, StringLen(alphabet))); // 10
   ArrayPrint(array2); // 65   66   67   68   69 1040 1041 1042 1043 1044
   // copy to a fixed size array
   PRT(StringToShortArray(alphabet, text)); // 5
   ArrayPrint(text); // 65 66 67 68 69
   // copy to a position beyond previous array size
   // (elements [11-19] will contain random data)
   PRT(StringToShortArray(alphabet, array2, 20)); // 11
   ArrayPrint(array2);
   /*
   [ 0]    65    66    67    68    69  1040  1041  1042  1043  1044     0     0     0     0     0 14245
   [16] 15102 37754 48617 54228    65    66    67    68    69  1040  1041  1042  1043  1044     0
   */

   string s = ShortArrayToString(array2, 0, 30);
   PRT(s); // 'ABCDEАБВГД', can have trailing random symbols
}
//+------------------------------------------------------------------+