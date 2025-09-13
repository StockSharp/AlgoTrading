//+------------------------------------------------------------------+
//|                                                      toyjson.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/Defines.mqh>

enum JS_TYPE
{
   JS_OBJECT,
   JS_ARRAY,
   JS_PRIMITIVE,
   JS_NULL        // singleton object stub for undefined props
};

enum JS_AFFINITY  // for primitive type
{
   JS_NONE   = 0,
   JS_CONST  = 1, // true, false, null
   JS_INT    = 2,
   JS_FLOAT  = 4,
   JS_STRING = 8
};

//+------------------------------------------------------------------+
//| Main class to store single value (primitive, object, or array)   |
//+------------------------------------------------------------------+
class JsValue
{
   MapArray<string, int> properties;
   AutoPtr<JsValue> objects[];
   
   const static JsValue null;
   static bool selfcall;
   
public:
   const string s;      // value is assigned once at construction
   const JS_TYPE t;     // type and
   const JS_AFFINITY a; //          affinity are detected
   JsValue(const string _s): s(_s), t(JS_PRIMITIVE), a(detect()) { }

   JsValue(const JS_TYPE _t = JS_OBJECT): t(_t), s(NULL), a(JS_NONE) { }
   
   int size() const
   {
      return ArraySize(objects);
   }
   
   static JS_AFFINITY isNumeric(const ushort c)
   {
      if((c >= '0' && c <= '9') || c == '+' || c == '-') return JS_INT;
      else if(c == '.' || c == 'e' || c == 'E') return JS_FLOAT;
      else if(c == ' ' || c == '\r' || c == '\n' || c == '\t') return JS_NONE;
      return JS_STRING;
   }
   
   JS_AFFINITY detect() const
   {
      if(s == "true" || s == "false" || s == "null") return JS_CONST;
      JS_AFFINITY result = 0;
      for(int i = 0; i < StringLen(s) && result < JS_STRING; ++i)
      {
         result |= isNumeric(s[i]);
      }
      return result;
   }
   
   string stringify() const
   {
      if(a >= JS_STRING)
      {
         string ss = s;
         StringReplace(ss, "\"", "\\\"");
         return "\"" + ss + "\"";
      }
      if(a >= JS_FLOAT) return s; // FIXME: apply specific accuracy
      if(a >= JS_INT) return s;
      return s != NULL ? s : "null";
   }

   template<typename T>
   T get(const bool typechecks = true) const
   {
      if(typechecks)
      {
         if(t != JS_PRIMITIVE) return (T)NULL;
         if(a >= JS_STRING && typename(T) != "string") return (T)NULL;
         if(a == JS_CONST && typename(T) != "bool") return (T)NULL;
      }
      return (T)s;
   }
   
   template<typename T>
   bool operator==(const T value)
   {
      if(t != JS_PRIMITIVE) return false;
      if(a >= JS_STRING && typename(T) != "string") return false;
      if(a == JS_CONST && typename(T) != "bool") return false;
      T temp = this.get<T>(false);
      return temp == value;
   }

   // object putters
   
   template<typename T>
   void put(const string key, const T value)
   {
      put(key, new JsValue((string)value));
   }

   void put(const string key, const double value, const int digits = 8)
   {
      put(key, new JsValue(DoubleToString(value, digits)));
   }

   void put(const string key, const JsValue *value)
   {
      if(t != JS_OBJECT && !selfcall)
      {
         PrintFormat("WARNING: Setting property '%s' for non-object", key);
      }
      int p = properties.find(key);
      if(p == -1)
      {
         p = EXPAND(objects);
         properties.put(key, p);
      }
      objects[p] = value;
   }
   
   // array putters
   
   template<typename T>
   void put(const T value)
   {
      put("[" + (string)ArraySize(objects) + "]", new JsValue((string)value));
   }

   void put(const double value, const int digits = 8)
   {
      put("[" + (string)ArraySize(objects) + "]", new JsValue(DoubleToString(value, digits)));
   }

   void put(const JsValue *value)
   {
      if(t != JS_ARRAY)
      {
         Print("WARNING: Setting indexed element for non-array: ", value.stringify());
      }
      selfcall = true;
      put("[" + (string)ArraySize(objects) + "]", value);
      selfcall = false;
   }
   
   // indexed access
   
   JsValue *operator[](const string name)
   {
      const int p = properties.find(name);
      if(p == -1) return (JsValue *)&null;
      return objects[p][];
   }

   JsValue *operator[](const int i)
   {
      if(i < 0 || i >= ArraySize(objects)) return (JsValue *)&null;
      return objects[i][];
   }
   
   // aux stuff
   
   void print() const
   {
      int level = 0;
      print(level);
   }
   
   void print(int &level) const
   {
      const static string open[] = {"{", "[", "", "<null>"};
      const static string close[] = {"}", "]", "", ""};
      Print(StringFormat("%*s", level * 2, ""), open[t], s);
      ++level;
      const string padding = StringFormat("%*s", level * 2, "");
      for(int i = 0; i < properties.getSize(); ++i)
      {
         if(objects[i][].s != NULL)
         {
            Print(padding, properties.getKey(i), " = ", objects[i][].s);
         }
         else
         {
            Print(padding, properties.getKey(i), " = ");
            objects[i][].print(level);
         }
      }
      --level;
      if(t < JS_PRIMITIVE) Print(StringFormat("%*s", level * 2, ""), close[t]);
   }
   
   void stringify(string &buffer) const
   {
      const static string open[] = {"{", "[", "", "null"};
      const static string close[] = {"}", "]", "", ""};
      StringAdd(buffer, open[t]);
      for(int i = 0; i < ArraySize(objects); ++i)
      {
         if(i > 0) StringAdd(buffer, ", ");
         if(t != JS_ARRAY) StringAdd(buffer, "\"" + properties.getKey(i) + "\" : ");
         if(objects[i][].s != NULL)
         {
            StringAdd(buffer, objects[i][].stringify());
         }
         else
         {
            objects[i][].stringify(buffer);
         }
      }
      StringAdd(buffer, close[t]);
   }
};

/* Can't do this due to MQL5 limitation (pointers vs values mixture)
class JsObject: public JsValue
{
public:
   JsObject() : JsValue(JS_OBJECT) { }
};

class JsArray: public JsValue
{
public:
   JsArray() : JsValue(JS_ARRAY) { }
};
*/

//+------------------------------------------------------------------+
//| JSON parser class                                                |
//+------------------------------------------------------------------+
class JsParser
{
   int cursor;
   string tokens[];
   
   void tokenize(const string &context)
   {
      string copy = context;
      StringReplace(copy, "{", ShortToString(1));
      StringReplace(copy, "}", ShortToString(1));
      StringReplace(copy, "[", ShortToString(1));
      StringReplace(copy, "]", ShortToString(1));
      StringReplace(copy, ShortToString('"'), ShortToString(1));
      StringReplace(copy, ",", ShortToString(1));
      StringReplace(copy, ":", ShortToString(1));
      StringSplit(copy, 1, tokens);
      int position = 0;
      for(int i = 0; i < ArraySize(tokens); ++i)
      {
         int step = i > 0 ? StringLen(tokens[i]) + 1 : StringLen(tokens[i]);
         tokens[i] = (i > 0 ? ShortToString(context[position]) : ShortToString(1)) + tokens[i];
         position += step;
      }
   }
   
   string parse_key()
   {
      if(tokens[cursor][0] == '"')
      {
         const string result = StringSubstr(tokens[cursor], 1);
         if(tokens[++cursor][0] != '"')
         {
            PrintFormat("Closing '\"' expected, got: '%s' @ %d", tokens[cursor], cursor);
            return NULL;
         }
         ++cursor;
         return result;
      }
      else
      {
         PrintFormat("Opening '\"' expected, got: '%s' @ %d", tokens[cursor], cursor);
         return NULL;
      }
   }
   
   JsValue *parse_value()
   {
      if(tokens[cursor][0] == '"')
      {
         string v = StringSubstr(tokens[cursor], 1);
         while(tokens[++cursor][0] != '"'
            || (tokens[cursor - 1][StringLen(tokens[cursor - 1]) - 1] == '\\'))
         {
            v += tokens[cursor];
         }
         if(v != NULL)
         {
            StringReplace(v, "\\\"", "\"");
            ++cursor;
            return new JsValue(v);
         }
         else
         {
            PrintFormat("Value expected, got: '%s' @ %d", tokens[cursor], cursor);
         }
         return NULL;
      }
      else if(tokens[cursor][0] == '{')
      {
         return parse_object();
      }
      else if(tokens[cursor][0] == '[')
      {
         return parse_array();
      }
      else // primitive, should be ":123.456", ":true"
      {
         return new JsValue(StringSubstr(tokens[cursor++], 1));
      }
   }
   
   JsValue *parse_array()
   {
      JsValue *current = NULL;
      if(tokens[cursor][0] == '[')
      {
         current = new JsValue(JS_ARRAY);
         int i = 0;
         do
         {
            ++cursor;
            current.put(parse_value());
            
            if(tokens[cursor][0] != ']' && tokens[cursor][0] != ',')
            {
               PrintFormat("'],' expected, got: '%s' @ %d", tokens[cursor], cursor);
               return current;
            }
         }
         while(tokens[cursor][0] != ']');
         if(cursor < ArraySize(tokens)) ++cursor;
      }
      else
      {
         PrintFormat("'[' expected, got: '%s' @ %d", tokens[cursor], cursor);
         return NULL;
      }
      return current;
   }
   
   JsValue *parse_object()
   {
      JsValue *current = NULL;
      if(tokens[cursor][0] == '{')
      {
         current = new JsValue(JS_OBJECT);
         do
         {
            ++cursor;
            string key = parse_key();
            if(tokens[cursor][0] != ':')
            {
               PrintFormat("':' expected, got: '%s' @ %d", tokens[cursor], cursor);
               return NULL;
            }
            string t = tokens[cursor]; // can be ":<whitespace>" (if a string or object is next) or ":<whitespace>value"
            StringTrimRight(t);
            if(StringLen(t) == 1) ++cursor;
            current.put(key, parse_value());
            
            if(tokens[cursor][0] != '}' && tokens[cursor][0] != ',')
            {
               PrintFormat("'},' expected, got: '%s' @ %d", tokens[cursor], cursor);
               return current;
            }
         }
         while(tokens[cursor][0] != '}');
         if(cursor < ArraySize(tokens)) ++cursor;
      }
      else
      {
         PrintFormat("'{' expected, got: '%s' @ %d", tokens[cursor], cursor);
         return NULL;
      }
      return current;
   }
   
public:
   JsValue *parse(const string &text)
   {
      if(StringLen(text) < 2) return NULL;
      
      cursor = 1; // skip start token (0x1)
      tokenize(text);
      // printTokens();
      return tokens[cursor][0] == '[' ? parse_array() : parse_object();
   }
   
   void printTokens() const
   {
      ArrayPrint(tokens);
   }
   
   static JsValue *jsonify(const string text)
   {
      JsParser parser;
      return parser.parse(text);
   }
};

static bool JsValue::selfcall = false;
const static JsValue JsValue::null(JS_NULL);
//+------------------------------------------------------------------+
