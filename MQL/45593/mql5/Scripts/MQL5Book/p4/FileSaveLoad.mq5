//+------------------------------------------------------------------+
//|                                                 FileSaveLoad.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A)  Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Struct for compound field in Simple struct                       |
//+------------------------------------------------------------------+
struct Pair
{
   short x, y;
};

//+------------------------------------------------------------------+
//| Example struct: structs with simple fields only are allowed for  |
//| functions FileSave/FileLoad; this can't be a class               |
//+------------------------------------------------------------------+
struct Simple
{
   double d;
   int i;
   datetime t;
   color c;
   uchar a[10]; // fixed size array is ok
   bool b;
   Pair p;      // compound fields (nested simple structs) are allowed as well
   
   // string field or dynamic array will cause the following compile error:
   // structures or classes containing objects are not allowed
   // string s;
   // uchar a[];
   
   // pointers are also prohibited for FileSave/FileLoad
   // void *ptr;
};

//+------------------------------------------------------------------+
//| Custom equivalent of FileSave function                           |
//+------------------------------------------------------------------+
template<typename T>
bool MyFileSave(const string name, const T &array[], const int flags = 0)
{
   const int h = FileOpen(name, FILE_BIN | FILE_WRITE | flags);
   if(h == INVALID_HANDLE) return false;
   FileWriteArray(h, array);
   FileClose(h);
   return true;
}

//+------------------------------------------------------------------+
//| Custom equivalent of FileLoad function                           |
//+------------------------------------------------------------------+
template<typename T>
long MyFileLoad(const string name, T &array[], const int flags = 0)
{
   const int h = FileOpen(name, FILE_BIN | FILE_READ | flags);
   if(h == INVALID_HANDLE) return -1;
   const uint n = FileReadArray(h, array, 0, (int)(FileSize(h) / sizeof(T)));
   // the next check is our custom improvement over standard FileLoad:
   // we show the warning if file size is not a multiple of struct size
   const ulong leftover = FileSize(h) - FileTell(h);
   if(leftover != 0)
   {
      PrintFormat("Warning from %s: Some data left unread: %d bytes",
         __FUNCTION__, leftover);
      SetUserError((ushort)leftover);
   }
   FileClose(h);
   return n;
}

//+------------------------------------------------------------------+
//| Output byte array in hex                                         |
//+------------------------------------------------------------------+
void ByteArrayPrint(const uchar &bytes[],
                    const int row = 16, const string separator = " | ",
                    const uint start = 0, const uint count = WHOLE_ARRAY)
{
   string hex = "";
   const int n = (int)MathCeil(MathLog10(ArraySize(bytes) + 1));
   for(uint i = start; i < MathMin(start + count, ArraySize(bytes)); ++i)
   {
      if(i % row == 0 || i == start)
      {
         if(hex != "") Print(hex);
         hex = StringFormat("[%0*d]", n, i) + " ";
      }
      hex += StringFormat("%02X", bytes[i]) + separator;
   }
   if(hex != "") Print(hex);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Simple write[] =
   {
      {+1.0, -1, D'2021.01.01', clrBlue, {'a'}, true, {1000, 16000}},
      {-1.0, -2, D'2021.01.01', clrRed,  {'b'}, true, {1000, 16000}},
   };
   const string filename = "MQL5Book/rawdata";
   
   // PART I. Normal execution
   
   PRT(FileSave(filename, write/*, FILE_COMMON*/)); // true
   // PRT(MyFileSave(filename, simple/*, FILE_COMMON*/)); // does the same
   
   Simple read[];
   PRT(FileLoad(filename, read/*, FILE_COMMON*/)); // 2
   // PRT(MyFileLoad(filename, read/*, FILE_COMMON*/)); // does the same

   // make sure that the read data is equal to what has been written
   PRT(ArrayCompare(write, read)); // 0

   // convert struct array to byte array to show its raw presentation
   uchar bytes[];
   for(int i = 0; i < ArraySize(read); ++i)
   {
      uchar temp[];
      PRT(StructToCharArray(read[i], temp));
      ArrayCopy(bytes, temp, ArraySize(bytes));
   }
   ByteArrayPrint(bytes);
   /*
   Output:
   
   [00] 00 | 00 | 00 | 00 | 00 | 00 | F0 | 3F | FF | FF | FF | FF | 00 | 66 | EE | 5F | 
   [16] 00 | 00 | 00 | 00 | 00 | 00 | FF | 00 | 61 | 00 | 00 | 00 | 00 | 00 | 00 | 00 | 
   [32] 00 | 00 | 01 | E8 | 03 | 80 | 3E | 00 | 00 | 00 | 00 | 00 | 00 | F0 | BF | FE | 
   [48] FF | FF | FF | 00 | 66 | EE | 5F | 00 | 00 | 00 | 00 | FF | 00 | 00 | 00 | 62 | 
   [64] 00 | 00 | 00 | 00 | 00 | 00 | 00 | 00 | 00 | 01 | E8 | 03 | 80 | 3E | 
   */
   
   uchar bytes2[];
   // we can read directly in byte array
   PRT(FileLoad(filename, bytes2/*, FILE_COMMON*/)); // 78,  39 * 2
   PRT(ArrayCompare(bytes, bytes2)); // 0, equality

   // PART II. Various problematic stuff
   
   // example of error-free yet incorrect usage:
   // the receiving struct type does not correspond to actual stored data
   MqlDateTime mdt[];
   PRT(sizeof(MqlDateTime)); // 32
   // Warning from MyFileLoad<MqlDateTime>: Some data left unread: 14 bytes
   PRT(MyFileLoad(filename, mdt)); // 2
   ArrayPrint(mdt);
   /*
   Output is a mess:
   
           [year]      [mon] [day]     [hour]    [min]    [sec] [day_of_week] [day_of_year]
   [0]          0 1072693248    -1 1609459200        0 16711680            97             0
   [1] -402587648    4096003     0  -20975616 16777215  6286950     -16777216    1644167168
   */
   
   /*
   // compile error, because type string is not supported here
   string texts[];
   FileSave("any", texts); // parameter conversion not allowed
   */
   
   double data[];
   PRT(FileLoad("any", data)); // -1, no such file
   PRT(_LastError); // 5004, ERR_CANNOT_OPEN_FILE
}
//+------------------------------------------------------------------+
