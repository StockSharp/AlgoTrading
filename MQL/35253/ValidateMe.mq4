//+------------------------------------------------------------------+
//|                                                   ValidateMe.mq4 |
//|                                      Copyright 2021, Frozen Pips |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
//THIS FRAMEWORK DEALS WITH SOME BASIC CHECKS A TRADING ROBOT MUST 
//PASS BEFORE PUBLICATION IN THE MARKET
#property copyright      "Copyright 2021, Frozen Pips"
#property link           "https://www.mql5.com"
#property description    "ValidateMe frameworks"
#property version        "1.00"
#property strict
sinput string ValidateMe="======= Framework inputs ======";
enum market {__BUY__,__SELL__,};
input ushort OrderTakePips = 50;
input ushort OrderStopPips = 50;
extern double Lots = 0.01;
input market Market=__BUY__;
int order=0;
string text="";
double StopLevel,GetPoint=0.0,GetLot=0.0,ExtOrderTake=0.0,
                 ExtOrderStop=0.0,ask=0.0,bid=0.0,sl=0.0,tp=0.0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(OrderStopPips<1||OrderTakePips<1)
      return(INIT_PARAMETERS_INCORRECT);
   GetPoint=SetPoint();
   StopLevel=MarketInfo(_Symbol,MODE_STOPLEVEL)*GetPoint;
   ExtOrderStop=OrderStopPips*GetPoint;
   ExtOrderTake=OrderTakePips*GetPoint;
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(OrdersTotal()<1)
      Orders();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Orders()
  {
   switch(Market)
     {
      case __BUY__:
         ask=MarketInfo(_Symbol,MODE_ASK);
         bid=MarketInfo(_Symbol,MODE_BID);
         sl=(OrderStopPips==0)?0.0:bid-ExtOrderStop;
         if(sl!=0.0 && ExtOrderStop<StopLevel)
            sl=bid-StopLevel;
         tp=(OrderTakePips==0)?0.0:ask+ExtOrderTake;
         if(tp!=0.0 && ExtOrderTake<StopLevel)
            tp=ask+StopLevel;
         GetLot=CheckVolumeValue(Lots);
         if(!CheckStopLoss_Takeprofit(OP_BUY,ExtOrderStop,ExtOrderTake))
            return;
         if(CheckMoneyForTrade(GetLot,OP_BUY))
            order=OrderSend(_Symbol,OP_BUY,GetLot,ask,10,sl,tp,"FrameWork",678,0,Blue);
         break;
      case __SELL__:
         bid=MarketInfo(_Symbol,MODE_BID);
         ask=MarketInfo(_Symbol,MODE_ASK);
         sl=(OrderStopPips==0)?0.0:ask+ExtOrderStop;
         if(sl!=0.0 && ExtOrderStop<StopLevel)
            sl=ask+StopLevel;
         tp=(OrderTakePips==0)?0.0:bid-ExtOrderTake;
         if(tp!=0.0 && ExtOrderTake<StopLevel)
            tp=bid-StopLevel;
         GetLot=CheckVolumeValue(Lots);
         if(!CheckStopLoss_Takeprofit(OP_SELL,ExtOrderStop,ExtOrderTake))
            return;
         if(CheckMoneyForTrade(GetLot,OP_SELL))
            order=OrderSend(_Symbol,OP_SELL,GetLot,bid,10,sl,tp,"FrameWork",678,0,Red);
         break;
     }
   if(order<0)
     {
      Print(GetLastError());
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SetPoint()
  {
   int digits=(int)MarketInfo(_Symbol,MODE_DIGITS);
   double point=MarketInfo(_Symbol,MODE_POINT),
          pt=(digits == 5||digits == 3)?point*10:point;
   return(pt);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CheckVolumeValue(double checkedvol)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   if(checkedvol<min_volume)
      return(min_volume);

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
   if(checkedvol>max_volume)
      return(max_volume);

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(checkedvol/volume_step);
   if(MathAbs(ratio*volume_step-checkedvol)>0.0000001)
      return(ratio*volume_step);
   return(checkedvol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(_Symbol,type,lots);
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ", oper," ",lots, " ", _Symbol, " Error msg=",GetLastError());
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckStopLoss_Takeprofit(ENUM_ORDER_TYPE type,double SL,double TP)
  {
//--- get the SYMBOL_TRADE_STOPS_LEVEL level
   int stops_level=(int)SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL);
   if(stops_level!=0)
     {
      PrintFormat("SYMBOL_TRADE_STOPS_LEVEL=%d: StopLoss and TakeProfit must"+
                  " not be nearer than %d points from the closing price",stops_level,stops_level);
     }
//---
   bool SL_check=false,TP_check=false;
//--- check only two order types
   switch(type)
     {
      //--- Buy operation
      case  ORDER_TYPE_BUY:
        {
         //--- check the StopLoss
         SL_check=(SL>stops_level*GetPoint);
         if(!SL_check)
            PrintFormat("For order %s StopLoss=%.5f must be less than %.5f"+
                        " (Bid=%.5f - SYMBOL_TRADE_STOPS_LEVEL=%d points)",
                        EnumToString(type),SL,Bid-stops_level*GetPoint,Bid,stops_level);
         //--- check the TakeProfit
         TP_check=(TP>stops_level*GetPoint);
         if(!TP_check)
            PrintFormat("For order %s TakeProfit=%.5f must be greater than %.5f"+
                        " (Bid=%.5f + SYMBOL_TRADE_STOPS_LEVEL=%d points)",
                        EnumToString(type),TP,Bid+stops_level*GetPoint,Bid,stops_level);
         //--- return the result of checking
         return(SL_check&&TP_check);
        }
      //--- Sell operation
      case  ORDER_TYPE_SELL:
        {
         //--- check the StopLoss
         SL_check=(SL>stops_level*GetPoint);
         if(!SL_check)
            PrintFormat("For order %s StopLoss=%.5f must be greater than %.5f "+
                        " (Ask=%.5f + SYMBOL_TRADE_STOPS_LEVEL=%d points)",
                        EnumToString(type),SL,Ask+stops_level*GetPoint,Ask,stops_level);
         //--- check the TakeProfit
         TP_check=(TP>stops_level*GetPoint);
         if(!TP_check)
            PrintFormat("For order %s TakeProfit=%.5f must be less than %.5f "+
                        " (Ask=%.5f - SYMBOL_TRADE_STOPS_LEVEL=%d points)",
                        EnumToString(type),TP,Ask-stops_level*GetPoint,Ask,stops_level);
         //--- return the result of checking
         return(TP_check&&SL_check);
        }
      break;
     }
//--- a slightly different function is required for pending orders
   return false;
  }
//+------------------------------------------------------------------+
