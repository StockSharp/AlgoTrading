//+------------------------------------------------------------------+
//|                                                    FileArray.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

const string raw = "MQL5Book/array.raw";
const string txt = "MQL5Book/array.txt";

//+------------------------------------------------------------------+
//| Struct with string fields to produce compile-time error          |
//+------------------------------------------------------------------+
struct TT
{
   string s1;
   string s2;
};

//+------------------------------------------------------------------+
//| Base struct to be inherited                                      |
//+------------------------------------------------------------------+
struct B
{
private:
   int b;
public:
   void setB(const int v) { b = v; }
};

//+------------------------------------------------------------------+
//| Demo derived struct                                              |
//+------------------------------------------------------------------+
struct XYZ : public B
{
   color x, y, z;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // test arrays of different types
   long numbers1[][2] = {{1, 4}, {2, 5}, {3, 6}};
   long numbers2[][2];
   long numbers3[][2];
   
   string text1[][2] = {{"1.0", "abc"}, {"2.0", "def"}, {"3.0", "ghi"}};
   string text2[][2];
   
   TT tt[];
   XYZ xyz[1];
   xyz[0].setB(-1);
   xyz[0].x = xyz[0].y = xyz[0].z = clrRed;

   // Part I. Numbers and structs
   // create new file or rewrite existing file from very beginning, in binary mode
   int writer = PRTF(FileOpen(raw, FILE_BIN | FILE_WRITE)); // 1 / ok
   PRTF(FileWriteArray(writer, numbers1)); // 6 / ok
   PRTF(FileWriteArray(writer, text1)); // 0 / FILE_NOTTXT(5012)
   PRTF(FileWriteArray(writer, xyz)); // 1 / ok
   FileClose(writer);
   ArrayPrint(numbers1);
/*
   original array
       [,0][,1]
   [0,]   1   4
   [1,]   2   5
   [2,]   3   6
*/   
   
   int reader = PRTF(FileOpen(raw, FILE_BIN | FILE_READ)); // 1 / ok
   PRTF(FileReadArray(reader, numbers2)); // 8 / ok
   ArrayPrint(numbers2);
/*
   since we read out the file completely,
   the receiving array 'numbers2' contains now not only 'numbers1',
   but also 'xyz', "reformatted" into 'numbers2' geometry:
                 [,0]          [,1]
   [0,]             1             4
   [1,]             2             5
   [2,]             3             6
   [3,] 1099511627775 1095216660735   // this is 'xyz' element
   // 4 ints in 'xyz' have the same size as 2 longs in a single row of 'numbers2'
*/

   // compare original array and what has been read (xyz tail is eliminated)
   PRTF(ArrayCompare(numbers1, numbers2, 0, 0, ArraySize(numbers1))); // 0 / ok, equality

   // now rewind the file to beginning for reading anew
   PRTF(FileSeek(reader, 0, SEEK_SET)); // true
   // this time we do partial reading of 3 elements to place them at index 10
   PRTF(FileReadArray(reader, numbers3, 10, 3));
   FileClose(reader);
   ArrayPrint(numbers3);
/*
   restored array numbers3 holds a part of original, 3 elements: 1, 4, 2;
   they placed at index 10[5,0] in the receiving array,
   elements marked by asterisk (') are random
       [,0][,1]
   [0,]  '1  '4
   [1,]  '1  '4
   [2,]  '2  '6
   [3,]  '0  '0
   [4,]  '0  '0
   [5,]   1   4
   [6,]   2  '0
*/   

   // Part II. Strings
   // create new file or rewrite existing file from very beginning, in text mode
   writer = PRTF(FileOpen(txt, FILE_TXT | FILE_ANSI | FILE_WRITE)); // 1 / ok
   // compile-time error:
   // FileWriteArray(writer, tt); // structures or classes containing objects are not allowed
   PRTF(FileWriteArray(writer, text1)); // 6 / ok
   PRTF(FileWriteArray(writer, numbers1)); // 0 / FILE_NOTBIN(5011)
   FileClose(writer);
   
   reader = PRTF(FileOpen(txt, FILE_TXT | FILE_ANSI | FILE_READ)); // 1 / ok
   PRTF(FileReadArray(reader, text2)); // 6 / ok
   FileClose(reader);
   
   PRTF(ArrayCompare(text1, text2)); // 0 / ok, equality
}
//+------------------------------------------------------------------+
