//+------------------------------------------------------------------+
//|                                EA_PotentialEntriesCommercial.mq5 |
//|                                    Copyright 2020, Mario Gharib. |
//|                                         mario.gharib@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, Mario Gharib. mario.gharib@hotmail.com"
#property link      "https://www.mql5.com"
#property version   "1.00"

#include <Candlestick.mqh>    // Candlesticks OHLC, charateristics & type
#include <Trade\Trade.mqh>

CTrade trade;

// INPUT TYPE TREND
enum iTrend{
   A1 = 1, //Bullish Trend
   A2 = 2, //Bearish Trend
};
input iTrend PA_TYPE;
input double dVol;

string sArrowBuy1 = "";
string sArrowBuy2 = "";
string sArrowSell1 = "";
string sArrowSell2 = "";
string sTradeIdentification = "";

static int BARS;

//+------------------------------------------------------------------+
//| NewBar function                                                  |
//+------------------------------------------------------------------+
bool IsNewBar()
   {
      if(BARS!=Bars(_Symbol,_Period))
        {
            BARS=Bars(_Symbol,_Period);
            return(true);
        }
      return(false);
   }

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {

   return(INIT_SUCCEEDED);
  }
  
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {

   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(IsNewBar()) {

      cCandlestick cCS1, cCS2;
  
      // OHLC, Characteristics and type of Candle 1
      cCS1.mvGetCandleStickCharateristics(_Symbol,1);
      
      // OHLC, Characteristics and type of Candle 2
      cCS2.mvGetCandleStickCharateristics(_Symbol,2);
   
      // =====================================
      // BULLISH REVERSAL CANDLESTICK PATTERNS 
      // =====================================
      
      double dAsk = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
      double dBid = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);

   
      if (PA_TYPE==1) {            
         // TRADE IDENTIFICATION 1: BULLISH HAMMER
         if (cCS1.bBullCandle && cCS2.bBearCandle && 2*cCS1.dBodyCandle<cCS1.dLowerWickCandle && cCS1.dLowerWickCandle>3*cCS1.dUpperWickCandle) {
            StringConcatenate(sTradeIdentification, "sTradeIdentification: ",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,1)));
            ObjectCreateMQL4(sTradeIdentification,OBJ_TEXT,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,1),cCS1.dLowPrice);
            ObjectSetTextMQL4(sTradeIdentification,"HAM",10,"");
            ObjectSetMQL4(sTradeIdentification,OBJPROP_COLOR, clrGreen);
            trade.Buy(dVol,_Symbol, dAsk, MathMin(cCS1.dLowPrice,cCS2.dLowPrice),0 ,"");         
         }
      
         // TRADE IDENTIFICATION 2: BULLISH INVERTED HAMMER
         else if (cCS1.bBullCandle && cCS2.bBearCandle && 2*cCS1.dBodyCandle<cCS1.dUpperWickCandle && 3*cCS1.dLowerWickCandle<cCS1.dUpperWickCandle) {
            StringConcatenate(sTradeIdentification, "sTradeIdentification: ",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,1)));
            ObjectCreateMQL4(sTradeIdentification,OBJ_TEXT,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,1),cCS1.dLowPrice);
            ObjectSetTextMQL4(sTradeIdentification,"IVH",10,"");
            ObjectSetMQL4(sTradeIdentification,OBJPROP_COLOR, clrGreen);
            trade.Buy(dVol,_Symbol, dAsk, MathMin(cCS1.dLowPrice,cCS2.dLowPrice),0 ,"");         
        }
              
         // TRADE IDENTIFICATION 6: BUILDING MOMEMTUM:
         else if (cCS1.bBullCandle && cCS2.bBullCandle && cCS1.dRangeCandle>cCS2.dRangeCandle && cCS1.dBodyCandle>=2*cCS2.dBodyCandle) {
            StringConcatenate(sArrowBuy1,"sArrowBuy1",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,1)));
            ObjectCreate(0,sArrowBuy1,OBJ_ARROW_BUY,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,1),cCS1.dOpenPrice);
            StringConcatenate(sArrowBuy2,"sArrowBuy2",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,2)));
            ObjectCreate(0,sArrowBuy2,OBJ_ARROW_BUY,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,2),cCS2.dOpenPrice);
            trade.Buy(dVol,_Symbol, dAsk, MathMin(cCS1.dLowPrice,cCS2.dLowPrice),0 ,"");         
         } 

      }
      
      // =====================================
      // BEARISH REVERSAL CANDLESTICK PATTERNS 
      // =====================================
      
      if (PA_TYPE==2) {     
         // TRADE IDENTIFICAITON 1: SHOOTING STAR
         if (cCS1.bBearCandle && cCS2.bBullCandle && 2*cCS1.dBodyCandle<cCS1.dUpperWickCandle && 3*cCS1.dLowerWickCandle<cCS1.dUpperWickCandle) {
            StringConcatenate(sTradeIdentification, "sTradeIdentification: ",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,1)));
            ObjectCreateMQL4(sTradeIdentification,OBJ_TEXT,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,1),cCS1.dHighPrice);
            ObjectSetTextMQL4(sTradeIdentification,"SHS",10,"");
            ObjectSetMQL4(sTradeIdentification,OBJPROP_COLOR, clrRed);
            trade.Sell(dVol,_Symbol, dBid, MathMax(cCS1.dHighPrice,cCS2.dHighPrice),0 ,""); 
         }
      
         // TRADE IDENTIFICAITON 2: HANGING MAN
         else if (cCS1.bBearCandle && cCS2.bBullCandle && 2*cCS1.dBodyCandle<cCS1.dLowerWickCandle && cCS1.dLowerWickCandle>3*cCS1.dUpperWickCandle) {
            StringConcatenate(sTradeIdentification, "sTradeIdentification: ",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,1)));
            ObjectCreateMQL4(sTradeIdentification,OBJ_TEXT,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,1),cCS1.dHighPrice);
            ObjectSetTextMQL4(sTradeIdentification,"HGM",10,"");
            ObjectSetMQL4(sTradeIdentification,OBJPROP_COLOR, clrRed);
            trade.Sell(dVol,_Symbol, dBid, MathMax(cCS1.dHighPrice,cCS2.dHighPrice),0 ,""); 
        }
         
         // TRADE IDENTIFICATION 6: BUILDING MOMEMTUM
         else if (cCS1.bBearCandle && cCS2.bBearCandle && cCS1.dBodyCandle>cCS2.dBodyCandle && cCS1.dRangeCandle>=2*cCS2.dRangeCandle) {
            StringConcatenate(sArrowSell1,"sArrowSell1",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,1)));
            ObjectCreate(0,sArrowSell1,OBJ_ARROW_SELL,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,1),cCS1.dOpenPrice);
            StringConcatenate(sArrowSell2,"sArrowSell2",string(iTimeMQL4(_Symbol,PERIOD_CURRENT,2)));
            ObjectCreate(0,sArrowSell2,OBJ_ARROW_SELL,0,iTimeMQL4(_Symbol,PERIOD_CURRENT,2),cCS2.dOpenPrice);
            trade.Sell(dVol,_Symbol, dBid, MathMax(cCS1.dHighPrice,cCS2.dHighPrice),0 ,""); 
        }
         
      }
   }
}