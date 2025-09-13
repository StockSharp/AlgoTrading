//+------------------------------------------------------------------+
//|                                                       madelta_ea |
//|                                           Copyright 2013 Winston |
//+------------------------------------------------------------------+
#property copyright "Winston 2013"
#property link      "    "
#property version   "1.00"
#property description "madelta_ea"
//---
input int Delta=195;                        //Hi-lo pips
input int M=392;                            //Multiplier
//---
input int F=26;                             //Fast moving average
input ENUM_MA_METHOD FM=MODE_SMA;           //Fast average mode
input ENUM_APPLIED_PRICE FP=PRICE_WEIGHTED; //Fast price mode
//---
input int S=51;                             //Slow moving average
input ENUM_MA_METHOD SM=MODE_EMA;           //Slow average mode
input ENUM_APPLIED_PRICE SP=PRICE_MEDIAN;   //Slow price mode
//---
int Ms,Mf,GI,trade,flg=0;
double px,hi,lo,d=Delta*0.00001,m=M*0.1;
double ms[1],mf[1];
//---
MqlTradeRequest req;
MqlTradeResult result;
MqlTradeCheckResult check;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   GI=iCustom(NULL,0,"madelta_inc",d,m,F,FM,FP,S,SM,SP); //Optional custom indicator
   Mf=iMA(NULL,0,F,0,FM,FP);
   Ms=iMA(NULL,0,S,0,SM,SP);
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| Expert new tick handling function                                |
//+------------------------------------------------------------------+
void OnTick()
  {
   CopyBuffer(Mf,0,0,1,mf);   //get slow moving average value
   CopyBuffer(Ms,0,0,1,ms);   //get fast moving average value
//---
   px=pow(m*(mf[0]-ms[0]),3); //cubic amplifier transfer of the difference
//--
   if(flg==0){hi=0; lo=0; trade=0; flg=1;} //high low threshold discriminator
   if(px>hi){hi=px; lo=hi-d; trade=1;}
   if(px<lo){lo=px; hi=lo+d; trade=-1;}
//---
   if(PositionSelect(_Symbol) && trade==1 && PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL) BUY(PositionGetDouble(POSITION_VOLUME));
   if(PositionSelect(_Symbol) && trade==-1 && PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY) SELL(PositionGetDouble(POSITION_VOLUME));
//---
   double lots=MathMin(15.0,NormalizeDouble(AccountInfoDouble(ACCOUNT_FREEMARGIN)/2000,1));
//---
   if(PositionsTotal()==0 && trade==1) BUY(lots);
   if(PositionsTotal()==0 && trade==-1) SELL(lots);
//---
   return;
  }
//+------------------------------------------------------------------+
//| BUY                                                              |
//+------------------------------------------------------------------+
void BUY(double lot)
  {
   req.type=ORDER_TYPE_BUY;
   req.action=TRADE_ACTION_DEAL;
   req.price=SymbolInfoDouble(_Symbol,SYMBOL_ASK);
   req.symbol = _Symbol;
   req.volume = lot;
   req.deviation=SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
   req.type_filling=ORDER_FILLING_IOC;
//--
   if(OrderCheck(req,check))
     {
      if(!OrderSend(req,result) || result.deal==0) Print("OrderSend Code: ",result.retcode);
     }
  }
//+------------------------------------------------------------------+
//| SELL                                                             |
//+------------------------------------------------------------------+
void SELL(double lot)
  {
   req.type=ORDER_TYPE_SELL;
   req.action=TRADE_ACTION_DEAL;
   req.price=SymbolInfoDouble(_Symbol,SYMBOL_BID);
   req.symbol = _Symbol;
   req.volume = lot;
   req.deviation=SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
   req.type_filling=ORDER_FILLING_IOC;
//--
   if(OrderCheck(req,check))
     {
      if(!OrderSend(req,result) || result.deal==0) Print("Sell Code: ",result.retcode);
     }
  }
//+------------------------------------------------------------------+
