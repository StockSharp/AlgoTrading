//+------------------------------------------------------------------+
//|                                             MqlParamStringer.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Convert MqlParam array into string                               |
//+------------------------------------------------------------------+
class MqlParamStringer
{
public:
   static string stringify(const MqlParam &param)
   {
      switch(param.type)
      {
      case TYPE_BOOL:
         // return param.integer_value ? "true" : "false";
      case TYPE_CHAR:
      case TYPE_UCHAR:
         // return CharToString((uchar)param.integer_value);
      case TYPE_SHORT:
      case TYPE_USHORT:
         // return ShortToString((ushort)param.integer_value);
      case TYPE_DATETIME:
         // return TimeToString(param.integer_value);
      case TYPE_COLOR:
      case TYPE_INT:
      case TYPE_UINT:
      case TYPE_LONG:
      case TYPE_ULONG:
         return IntegerToString(param.integer_value);
      case TYPE_FLOAT:
      case TYPE_DOUBLE:
         return (string)(float)param.double_value;
      case TYPE_STRING:
         return param.string_value;
      }
      return NULL;
   }
   
   static string stringify(const MqlParam &params[])
   {
      string result = "";
      const int p = ArraySize(params);
      for(int i = 0; i < p; ++i)
      {
         result += stringify(params[i]) + (i < p - 1 ? "," : "");
      }
      return result;
   }
};
//+------------------------------------------------------------------+
