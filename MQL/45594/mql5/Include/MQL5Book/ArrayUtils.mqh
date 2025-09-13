//+------------------------------------------------------------------+
//|                                                   ArrayUtils.mqh |
//|                         Copyright (c) 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Helper function to remove duplicates from array                  |
//+------------------------------------------------------------------+
template<typename T>
int ArrayUnique(T &array[])
{
   const int n = ArraySize(array);
   if(n <= 1) return 0;
   int w = 0;
   for(int i = 1; i < n; ++i)
   {
      if(array[i] == array[w])
      {
         // skip it
      }
      else if(w < i - 1)
      {
         array[++w] = array[i];
      }
      else
      {
         ++w;
      }
   }
   return n - ArrayResize(array, w + 1);
}

//+------------------------------------------------------------------+
//| Helper function to remove empty elements from array              |
//+------------------------------------------------------------------+
template<typename T>
void ArrayPurge(T &array[], const T empty)
{
   const int count = ArraySize(array);
   int write = 0;
   int i = 0;
   while(i < count)
   {
     while(i < count && array[i] == empty)
     {
       i++;
     }
     
     while(i < count && array[i] != empty)
     {
       if(write < i)
       {
         array[write] = array[i];
       }
       i++;
       write++;
     }
   }
   ArrayResize(array, write);
}

//+------------------------------------------------------------------+
//| Helper class to remove elements from array by predicate          |
//+------------------------------------------------------------------+
template<typename T>
class ArrayPurger
{
   typedef bool (*PURGER)(const T &element);
public:
   ArrayPurger(T &array[], PURGER func)
   {
      const int count = ArraySize(array);
      int write = 0;
      int i = 0;
      while(i < count)
      {
         while(i < count && func(array[i]))
         {
            i++;
         }
        
         while(i < count && !func(array[i]))
         {
            if(write < i)
            {
               array[write] = array[i];
            }
            i++;
            write++;
         }
      }
      ArrayResize(array, write);
   }
};

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
//| Output number array in hex                                       |
//+------------------------------------------------------------------+
template<typename T>
void HexArrayPrint(const T &bytes[],
                    const int row = 16, const string separator = " | ",
                    const uint start = 0, const uint count = WHOLE_ARRAY)
{
   const int size = sizeof(T) / sizeof(uchar) * 2;
   string hex = "";
   const int n = (int)MathCeil(MathLog10(ArraySize(bytes) + 1));
   for(uint i = start; i < MathMin(start + count, ArraySize(bytes)); ++i)
   {
      if(i % row == 0 || i == start)
      {
         if(hex != "") Print(hex);
         hex = StringFormat("[%0*d]", n, i) + " ";
      }
      hex += StringFormat("%0*x", size, bytes[i]) + separator;
   }
   if(hex != "") Print(hex);
}

//+------------------------------------------------------------------+
