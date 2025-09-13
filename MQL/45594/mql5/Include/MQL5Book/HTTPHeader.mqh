//+------------------------------------------------------------------+
//|                                                   HTTPHeader.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| HTTP header parser                                               |
//+------------------------------------------------------------------+
class HttpHeader
{
   string lines[];
public:
   HttpHeader(string response, const ushort delimiter, const ushort glue)
   {
      if(response == NULL) return;
      StringTrimRight(response);
      const int n = StringSplit(response, delimiter, lines);
      ArrayResize(lines, n * 2);
      for(int i = 0; i < n; ++i)
      {
         StringTrimLeft(lines[i]);
         StringTrimRight(lines[i]);
         string pair[];
         const int m = StringSplit(lines[i], glue, pair);
         if(m > 0)
         {
            StringToLower(pair[0]);
            StringTrimRight(pair[0]);
            lines[i] = pair[0];         // name
            if(m > 1)
            {
               StringTrimLeft(pair[1]);
               lines[i + n] = pair[1];  // value
            }
            else
            {
               lines[i + n] = NULL;
            }
         }
      }
   }
   
   int size() const
   {
      return ArraySize(lines);
   }
   
   string operator[](const int i) const
   {
      return lines[i];
   }
   
   string operator[](string name) const
   {
      const int m = StringLen(name); 
      if(m == 0) return NULL;
      StringToLower(name);
      const int n = ArraySize(lines) / 2;
      for(int i = 0; i < n; ++i)
      {
         if(lines[i] == name)
         {
            return lines[i + n];
         }
      }
      return NULL;
   }
   
   void printRaw() const
   {
      ArrayPrint(lines);
   }
   
   static string unquote(const string text)
   {
      const int n = StringLen(text);
      if(n >= 2 && text[0] == '"' && text[n - 1] == '"')
      {
         return StringSubstr(text, 1, n - 2);
      }
      return text;
   }
   
   static string hash(const string text, const ENUM_CRYPT_METHOD method = CRYPT_HASH_MD5)
   {
      uchar data[], result[], empty[];
      StringToCharArray(text, data, 0, StringLen(text), CP_UTF8);
      CryptEncode(method, data, empty, result);
      if(method != CRYPT_BASE64)
      {
         string str = "";
         for(int i = 0; i < ArraySize(result); ++i)
         {
            str += StringFormat("%02x", result[i]);
         }
         return str;
      }
      return CharArrayToString(result);
   }

   string combine(const ushort delimiter, const ushort glue)
   {
      const int n = ArraySize(lines);
      string result = "";
      for(int i = 0; i < n / 2; ++i)
      {
         result += (i > 0 ? ShortToString(delimiter) : "") + lines[i] + ShortToString(glue) + lines[i + n / 2];
      }
      return result;
   }
};
//+------------------------------------------------------------------+
