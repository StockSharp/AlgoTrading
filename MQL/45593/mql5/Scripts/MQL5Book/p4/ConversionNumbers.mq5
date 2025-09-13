//+------------------------------------------------------------------+
//|                                            ConversionNumbers.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))
#define PRT2(A) Print(#A, "='", (A), "'")

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string text = "123.4567890123456789";
   const string message = "-123e-5 buckazoid";
   const double number = 123.4567890123456789;
   const double exponent = 1.234567890123456789e-5;

   // typecasts
   PRT((double)text);    // 123.4567890123457
   PRT((double)message); // -0.00123
   PRT((string)number);  // 123.4567890123457
   PRT((string)exponent);// 1.234567890123457e-05
   PRT((long)text);      // 123
   PRT((long)message);   // -123
   
   // conversions
   PRT(StringToDouble(text)); // 123.4567890123457
   PRT(StringToDouble(message)); // -0.00123
   
   // by default, 8 digits in fractional part
   PRT(DoubleToString(number)); // 123.45678901

   // customized accuracy
   PRT(DoubleToString(number, 5));  // 123.45679
   PRT(DoubleToString(number, -5)); // 1.23457e+02
   PRT(DoubleToString(number, -16));// 1.2345678901234568e+02
   PRT(DoubleToString(number, 16)); // 123.4567890123456807
   //                                  last 2 digits are unreliable!
   PRT(MathSqrt(-1.0));                 // -nan(ind)
   PRT(DoubleToString(MathSqrt(-1.0))); // 9223372129088496176.54775808
   
   PRT(StringToInteger(text));      // 123
   PRT(StringToInteger(message));   // -123

   PRT2(IntegerToString(INT_MAX));         // '2147483647'
   PRT2(IntegerToString(INT_MAX, 5));      // '2147483647'
   PRT2(IntegerToString(INT_MAX, 16));     // '      2147483647'
   PRT2(IntegerToString(INT_MAX, 16, '0'));// '0000002147483647'
}
//+------------------------------------------------------------------+
