//+------------------------------------------------------------------+
//|                                                      wstools.mqh |
//|                             Copyright 2020-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/URL.mqh>

//+------------------------------------------------------------------+
//| Various helper functions                                         |
//+------------------------------------------------------------------+
namespace WsTools
{
   // Parse HTTP headers (multiline) to an array of key[][0]/value[][1]
   int parseHeaders(const string header, string &retVal[][2])
   {
      string fields[];
      const int n = StringSplit(header, '\n', fields);
    
      ArrayResize(retVal, 0);
    
      for(int i = 0; i < n; i++)
      {
         StringReplace(fields[i], "\t", " ");
         string match[];
         
         if(StringSplit(URL::trim(fields[i]), ':', match) >= 2)
         {
            const int m = ArrayRange(retVal, 0);
            ArrayResize(retVal, m + 1);
            StringToLower(match[0]);  // make key case-insensitive
            retVal[m][0] = match[0];  // NB: if the same header key occured many times, it'll be added many times
            StringTrimLeft(match[1]); // skip leading blank after ':'
            retVal[m][1] = match[1];
            for(int j = 2; j < ArraySize(match); j++)
            {
               retVal[m][1] += ":" + match[j];
            }
         }
      }

      if(StringFind(header, "GET ") == 0)
      {
         const int p = StringFind(header, " HTTP/");
         if(p > 0)
         {
           const int m = ArrayRange(retVal, 0);
           ArrayResize(retVal, m + 1);
           retVal[m][0] = "GET";
           retVal[m][1] = StringSubstr(header, 4, p - 4);
         }
      }

      return ArrayRange(retVal, 0);
   }
  
   string arrayToHex(const uchar &array[], const int max = -1)
   { 
      string res = "";
      const int count = max == -1 ? ArraySize(array) : fmin(max, ArraySize(array));
      for(int i = 0; i < count; i++)
      {
         res += StringFormat("%.02X ", array[i]);
      }
      if(count < ArraySize(array)) res += "...";
      return res;
   }
    
   int charCount(const string data, const uchar c)
   {
      int count = 0;
      for(int i = 0; i < StringLen(data); i++)
      {
         if(data[i] == c) count++;
      }
      return count;
   }
    
   union BYTES4
   {
      uchar chars[4];
      uint  num;
      BYTES4(): num(0) { }
      BYTES4(uint n): num(n) { }
      BYTES4(const string &data) { WsTools::StringToByteArray(data, chars, 0, 4); }
      BYTES4(const uchar &data[], const int offset = 0) { ArrayCopy(chars, data, 0, offset, 4); }
      uchar operator[](int i) { return chars[i]; }
   };

   void pack4(uint number, uchar &data[], const int offset = 0)
   {
      BYTES4 b;
      b.num = MathSwap(number);
      ArrayCopy(data, b.chars, offset);
   }

   uint unpack4(const string data, const int offset = 0)
   {
      BYTES4 b;
      b.chars[0] = (uchar)data[0 + offset];
      b.chars[1] = (uchar)data[1 + offset];
      b.chars[2] = (uchar)data[2 + offset];
      b.chars[3] = (uchar)data[3 + offset];
      return MathSwap(b.num); // TODO: consider redo without swap
   }

   uint unpack4(const uchar &data[], const int offset = 0)
   {
      BYTES4 b;
      b.chars[0] = (uchar)data[0 + offset];
      b.chars[1] = (uchar)data[1 + offset];
      b.chars[2] = (uchar)data[2 + offset];
      b.chars[3] = (uchar)data[3 + offset];
      return MathSwap(b.num); // TODO: consider redo without swap
   }

   union BYTES2
   {
      uchar chars[2];
      ushort  num;
      BYTES2(): num(0) { }
      BYTES2(ushort n): num(n) { }
      uchar operator[](int i) { return chars[i]; }
   };
    
   void pack2(ushort number, uchar &data[], const int offset = 0)
   {
      BYTES2 b;
      b.num = MathSwap(number);
      ArrayCopy(data, b.chars, offset);
   }
    
   ushort unpack2(const string data, const int offset = 0)
   {
      BYTES2 b;
      b.chars[0] = (uchar)data[0 + offset];
      b.chars[1] = (uchar)data[1 + offset];
      return MathSwap(b.num);
   }

   ushort unpack2(const uchar &data[], const int offset = 0)
   {
      BYTES2 b;
      b.chars[0] = (uchar)data[0 + offset];
      b.chars[1] = (uchar)data[1 + offset];
      return MathSwap(b.num);
   }

   template<typename T>
   void push(T *&array[], T *ptr)
   {
      const int n = ArraySize(array);
      ArrayResize(array, n + 1);
      array[n] = ptr;
   }

   template<typename T>
   void push(T &array[][2], T key, T value)
   {
      const int n = ArrayRange(array, 0);
      ArrayResize(array, n + 1);
      array[n][0] = key;
      array[n][1] = value;
   }
   
   // NB: StringToCharArray copies to specific part (start/count) of the receiving array (which is confusing)
   // StringToByteArray is intended to copy specific part of the given string
   int StringToByteArray(const string text, uchar &array[], const int start = 0, const int count = -1, const uint cp = CP_ACP)
   {
      if(cp == CP_ACP)
      {
         const int n = count == -1 ? StringLen(text) - start : count;
         ArrayResize(array, n);
         for(int i = 0; i < n; i++)
         {
            array[i] = (uchar)StringGetCharacter(text, i + start);
         }
         return n;
      }
      else
      {
         int n = StringToCharArray((start || count != -1) ?
            StringSubstr(text, start, count) : text, array, 0, -1, cp);
         if(n > 0 && array[n - 1] == 0)
         {
            ArrayResize(array, --n);
         }
         return n;
      }
      return 0;
   }

   string stringify(const uchar &data[], const int limit = -1)
   {
      string result = "";
      const int count = limit == -1 ? ArraySize(data) : MathMin(limit, ArraySize(data));
      StringReserve(result, count);
      for(int i = 0; i < count; i++)
      {
         result += StringFormat("%C", data[i]);
      }
      return result;
   }
};
//+------------------------------------------------------------------+
