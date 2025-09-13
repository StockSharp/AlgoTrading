//+------------------------------------------------------------------+
//|                                                         _ADX.mq5 |
//|                                                     Yuriy Tokman |
//|                                         http://www.mql-design.ru |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "http://www.mql-design.ru"
#property version   "1.00"

input  string Copyright="Yuriy Tokman";
input  string WritingExpertAdvisors="Indicators_Scripts";
input  string e_mail= "yuriytokman@gmail.com";
input  string Skype = "yuriy.g.t";

input ENUM_TIMEFRAMES TimeFrames=0;
input int shift=1;
//----ADX
int h_adx=INVALID_HANDLE;
double b_adxP[];
double b_adxM[];
input int adx_period=28;
//----+
input int MAGIC=2899;
input double Lots=0.1;
input int SL = 500;
input int TP = 500;
input int dev= 30;
//----+
input int level_p = 5;
input int level_m = 5;
//----+
int OnInit(){return(0);}
void OnDeinit(const int reason){}
//+------------------------------------------------------------------+
void OnTick()
  {
//----+
   if(!ExtPos())
     {
      if(Sig_ADX()>0)OP();
      if(Sig_ADX()<0)OP(0);
     }
//----+  
  }
//+------------------------------------------------------------------+
int Sig_ADX()
  {
   int sig=0;

   if(h_adx==INVALID_HANDLE)
     {
      h_adx=iADXWilder(Symbol(),TimeFrames,adx_period);return(0);
     }
   else
     {
      if(CopyBuffer(h_adx,1,0,3+shift,b_adxP)<3+shift) return(0);
      if(CopyBuffer(h_adx,2,0,3+shift,b_adxM)<3+shift) return(0);
      if(!ArraySetAsSeries(b_adxP,true))return(0);
      if(!ArraySetAsSeries(b_adxM,true))return(0);
     }

   if(b_adxP[0+shift]>level_p && b_adxP[1+shift]<level_p)sig=+1;
   if(b_adxM[0+shift]>level_m && b_adxM[1+shift]<level_m)sig=-1;

   return(sig);
  }
//----+
bool ExtPos()
  {
   for(int i=0;i<PositionsTotal();i++)
     {
      if(Symbol()==PositionGetSymbol(i))
        {
         if(PositionGetInteger(POSITION_MAGIC)==MAGIC)
           {
            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
               return(true);
            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
               return(true);
           }
        }
     }
   return(false);
  }
//----+
void OP(int op=1)
  {
//---
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   MqlTradeRequest request;
   MqlTradeCheckResult check_result;
   MqlTradeResult trade_result;
   ZeroMemory(request);

   request.action=TRADE_ACTION_DEAL;
   request.magic=MAGIC;
   request.symbol=Symbol();
   request.volume=Lots;
   request.deviation=dev;
   request.type_filling=ORDER_FILLING_FOK;
//--- 
   if(op==1)
     {
      request.type=ORDER_TYPE_BUY;
      request.price=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
      request.sl= SymbolInfoDouble(Symbol(),SYMBOL_BID) - SL*Point();
      request.tp=SymbolInfoDouble(Symbol(),SYMBOL_ASK) +TP*Point();
     }
   if(op==0)
     {
      request.type=ORDER_TYPE_SELL;
      request.price=SymbolInfoDouble(Symbol(),SYMBOL_BID);
      request.sl= SymbolInfoDouble(Symbol(),SYMBOL_ASK) + SL*Point();
      request.tp=SymbolInfoDouble(Symbol(),SYMBOL_BID) - TP*Point();
     }
   request.comment="www.mql-design.ru";
//----+
   if(!OrderCheck(request,check_result))return;
   if(OrderSend(request,trade_result))
      Print("Trade request has been successfuly executed, order volume =",trade_result.volume);
   else
      Print("Error in trade request execution. Return code=",trade_result.retcode);
//----+
  }
//+------------------------------------------------------------------+