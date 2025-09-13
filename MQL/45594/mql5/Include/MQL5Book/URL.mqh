//+------------------------------------------------------------------+
//|                                                          URL.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| URL components                                                   |
//+------------------------------------------------------------------+
enum URL_PARTS
{
   URL_COMPLETE,
   URL_SCHEME,   // protocol
   URL_USER,     // deprecated/not supported/null
   URL_HOST,
   URL_PORT,
   URL_PATH,
   URL_QUERY,
   URL_FRAGMENT, // not extracted/null
   URL_ENUM_LENGTH
};

//+------------------------------------------------------------------+
//| URL parser                                                       |
//+------------------------------------------------------------------+
class URL
{
public:
   static bool isAlpha(const uchar c)
   {
    	return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
   }

   static bool isDigit(const uchar c) 
   {
      return c >= '0' && c <= '9';
   }

   static bool isAlNum(const uchar c)
   {
    	return isAlpha(c) || isDigit(c);
   }
    
   static string encode(const string str)
   {
      string new_str = "";
      uchar c;

      uchar chars[];
      const int len = StringToCharArray(str, chars);
  
      for(int i = 0; i < len; i++)
      {
         c = chars[i];
         if(c == ' ') new_str += "+";
         else if(isAlNum(c) || c == '-' || c == '_' || c == '.' || c == '~') new_str += (string)c;
         else
         {
            new_str += "%" + StringFormat("%%%.2X", c);
         }
      }
      return new_str;
   }
    
   static uchar hex2value(const uchar hex)
   {
      uint result = hex - '0';
      if(result > 9)
      {
         result = hex - 'A' + 10;
         if(result > 15)
         {
            result = hex - 'a' + 10;
         }
      }
      return (uchar)result;
   }
    
    // TODO: 2-byte/3-byte encodings support

   static string decode(const string str)
   {
      string ret;
      uchar chars[];
      const int len = StringToCharArray(str, chars);
  
      for(int i = 0; i < len; i++)
      {
         if(chars[i] != '%')
         {
            if(chars[i] == '+')
               ret += " ";
            else
               ret += (string)chars[i];
         }
         else
         {
            ret += (string)(uchar)(hex2value(chars[i + 1]) * 16 + hex2value(chars[i + 2]));
            i += 2;
         }
      }
      return ret;
   }
    
   static string trim(string &str)
   {
      StringTrimLeft(str);
      StringTrimRight(str);
      return str;
   }
    
   // scheme://example.com:80/path?query#hash
   static void parse(string url, string &parts[])
   {
      const static string start = "://";
      const static string comma = ":";
      const static string slash = "/";
      const static string question = "?";

      ArrayResize(parts, URL_PARTS::URL_ENUM_LENGTH);
      for(int i = 0; i < URL_PARTS::URL_ENUM_LENGTH; i++)
      {
         parts[i] = NULL;
      }
      
      parts[0] = url; // TODO: re-assemble url from parts
      
      int c = 0;
      int p = StringFind(url, start);
      if(p > -1)
      {
         parts[URL_SCHEME] = StringSubstr(url, 0, p);
         p += StringLen(start);
         c = p;
      }
      
      p = StringFind(url, comma, c);
      int path = StringFind(url, slash, c);
      int port = -1;
      if(p > -1 && (p < path || path == -1))
      {
         port = p;
         parts[URL_HOST] = StringSubstr(url, c, p - c);
      }
      
      if(path > -1)
      {
         parts[URL_HOST] = StringSubstr(url, c, (port != -1 ? port : path) - c);
         c = path + 1;
      }
      else
      {
         parts[URL_HOST] = StringSubstr(url, c, (port != -1 ? port : StringLen(url)) - c);
         c = StringLen(url) + 1;
      }
      
      if(port > -1)
      {
         parts[URL_PORT] = StringSubstr(url, port + 1, c - port - 2);
      }
      
      if(path > -1)
      {
         p = StringFind(url, question, path);
         c = p;
         if(p == -1) p = StringLen(url);
         parts[URL_PATH] = StringSubstr(url, path, p - path);
         if(c > -1)
         {
            parts[URL_QUERY] = StringSubstr(url, c + 1);
         }
      }
      else
      {
         parts[URL_PATH] = "/";
      }
   }
};
//+------------------------------------------------------------------+
