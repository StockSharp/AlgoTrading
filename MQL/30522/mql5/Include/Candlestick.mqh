//+------------------------------------------------------------------+
//|                                                  Candlestick.mqh |
//|                        Copyright 2019, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2019, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
// #define MacrosHello   "Hello, world!"
// #define MacrosYear    2010
//+------------------------------------------------------------------+
//| DLL imports                                                      |
//+------------------------------------------------------------------+
// #import "user32.dll"
//   int      SendMessageA(int hWnd,int Msg,int wParam,int lParam);
// #import "my_expert.dll"
//   int      ExpertRecalculate(int wParam,int lParam);
// #import
//+------------------------------------------------------------------+
//| EX5 imports                                                      |
//+------------------------------------------------------------------+
// #import "stdlib.ex5"
//   string ErrorDescription(int error_code);
// #import
//+------------------------------------------------------------------+

#include <functions.mqh>               // Migrating functions from MQL4 to MQL5

class cCandlestick {

   public:

      // OHLC of Candlestick
      double dOpenPrice;
      double dHighPrice;
      double dLowPrice;
      double dClosePrice;

      // Characteristics of Candlestick
      double dRangeCandle;
      double dBodyCandle;
      double dUpperWickCandle;
      double dLowerWickCandle;
      
      // Type of Candlestick
      bool bBullCandle;
      bool bBearCandle;
      bool bDojiCandle;
      
      void mvGetCandleStickCharateristics (string s, int i) {
         
         dOpenPrice = iOpenMQL4(s, PERIOD_CURRENT,i);
         dHighPrice = iHighMQL4(s, PERIOD_CURRENT,i);
         dLowPrice = iLowMQL4(s, PERIOD_CURRENT,i);
         dClosePrice = iCloseMQL4(s, PERIOD_CURRENT,i);
         
         dRangeCandle = NormalizeDouble(MathAbs(dHighPrice-dLowPrice)/MarketInfoMQL4(s,MODE_POINT),_Digits);
         dBodyCandle = NormalizeDouble(MathAbs(dOpenPrice-dClosePrice)/MarketInfoMQL4(s,MODE_POINT),_Digits);
         dUpperWickCandle = NormalizeDouble(MathAbs(dHighPrice-MathMax(dClosePrice,dOpenPrice))/MarketInfoMQL4(s,MODE_POINT),_Digits);
         dLowerWickCandle = NormalizeDouble(MathAbs(MathMin(dClosePrice,dOpenPrice)-dLowPrice)/MarketInfoMQL4(s,MODE_POINT),_Digits);
         
         if (dBodyCandle<=1.0 && dUpperWickCandle>dBodyCandle && dLowerWickCandle>dBodyCandle) {
            bDojiCandle=true; bBullCandle=false; bBearCandle=false;}
         else if (dOpenPrice<dClosePrice) {
            bDojiCandle=false; bBullCandle=true; bBearCandle=false;}
         else if (dOpenPrice>dClosePrice) {
            bDojiCandle=false; bBullCandle=false; bBearCandle=true;}
      }

};