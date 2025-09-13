//+------------------------------------------------------------------+
//|                                            Adaptive Grid Mt4.mq4 |
//|                                              Copyright 2022, BkT |
//|                                          https://www.google.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2022, BkT"
#property link      "https://www.google.com/"
#property version   "1.00"
#property strict
//---
sinput string  InpA            =">>> Magic id, volume & tf <<<";//.
sinput int     Inp_MagicNumber = 98765;//Magic number
sinput double  Inp_Lot         = 0.01;//Fixed volume
sinput ENUM_TIMEFRAMES Wtf     = PERIOD_M15;// Working tf
sinput string  InpB            =">>> Grid max orders & timer <<<";//.
input int      Inp_nGrid       = 10,// n grid
               Inp_nBars       = 15;// n bar timer, working tf
sinput string  InpC            =">>> Grid level ATR multipliers <<<<";//.
input double   Inp_Poffset     = 0.8,//Price deviation
               Inp_Pstep       = 0.5,// Grid step
               Inp_StopLoss    = 2.4,//Stop loss
               Inp_TakeProfit  = 2.8;//Take profit
//---
int
tkt=0,
acc;
bool
demo,
dcomm,
tester;
string
mode,
server,
currsym,
tfcomm,
acccurr;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   dcomm=(!MQLInfoInteger(MQL_TESTER)||MQLInfoInteger(MQL_VISUAL_MODE))?true:false;
   tester=(MQLInfoInteger(MQL_TESTER))?true:false;
   demo=(AccountInfoInteger(ACCOUNT_TRADE_MODE) == ACCOUNT_TRADE_MODE_DEMO)?true:false;
   if(!tester)
      mode=(demo)?"\n\nDemo account trading mode":"\n\nReal account trading mode";
   else
      mode=(demo)?"\n\nDemo account tester mode":"\n\nReal account tester mode";
   acc=AccountNumber();
   server=AccountServer();
   acccurr=AccountCurrency();
   currsym=_Symbol;
   if(dcomm)
      SetComment();
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Comment("");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(NewBar())
     {
      PenDel();
      if(What()==0)
         GridModule();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void GridModule()
  {
   for(int i=0; i<Inp_nGrid; i++)
     {
      double
      STEP=Atr()*(Inp_Pstep/PointValue()),
      OFFSET=Atr()*(Inp_Poffset/PointValue()),
      TP=Atr()*(Inp_TakeProfit/PointValue()),
      SL=Atr()*(Inp_StopLoss/PointValue()),
      Bprice=NormalizeDouble(Ask+(OFFSET*PointValue())+(i*STEP*PointValue()),Digits),
      Btp=Bprice+TP*PointValue(),
      Bsl=Bprice-SL*PointValue(),
      Sprice=NormalizeDouble(Bid-(OFFSET*PointValue())-(i*STEP*PointValue()),Digits),
      Stp=Sprice-TP*PointValue(),
      Ssl=Sprice+SL*PointValue(),
      Contract=CheckVolumeValue(Inp_Lot);
      if(CheckMoneyForTrade(Contract,OP_BUY))
         tkt=OrderSend(_Symbol,OP_BUYSTOP,Contract,Bprice,10,SC(Bsl),SC(Btp),"",Inp_MagicNumber,0,clrGreen);
      if(CheckMoneyForTrade(Contract,OP_SELL))
         tkt=OrderSend(_Symbol,OP_SELLSTOP,Contract,Sprice,10,SC(Ssl),SC(Stp),"",Inp_MagicNumber,0,clrRed);
     }
   if(tkt < (Inp_nGrid*2))
     {
      Print(GetLastError());
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double PointValue()
  {
//---
   int
   digits=(int)MarketInfo(_Symbol,MODE_DIGITS);
   double
   point=MarketInfo(_Symbol,MODE_POINT),
   Conversion=(digits == 3 || digits == 5)?point*10:point;
   return(Conversion);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SC(double param)
  {
//---
   RefreshRates();
   double
   SPREAD=MarketInfo(_Symbol,MODE_SPREAD),
   Slevel=MathMax(MarketInfo(_Symbol, MODE_FREEZELEVEL), MarketInfo(_Symbol, MODE_STOPLEVEL)),
   NewParam=(param<(Slevel+SPREAD)*PointValue())?(Slevel+SPREAD)*PointValue():param;
   return(NewParam);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Atr()
  {
//---
   return(iATR(_Symbol,Wtf,14,1));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int What()
  {
//---
   for(int x = OrdersTotal() - 1; x >= 0; x--)
     {
      if(!OrderSelect(x, SELECT_BY_POS))
         break;
      if(OrderCloseTime() == 0 && OrderSymbol() == _Symbol && OrderMagicNumber() == Inp_MagicNumber)
        {
         if(OrderType() == OP_BUY || OrderType() == OP_SELL)
            return(1);
         if(!(OrderType() == OP_BUY || OrderType() == OP_SELL))
            return(-1);
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PenDel()
  {
//--
   for(int x=OrdersTotal() - 1; x >=0; x--)
     {
      if(!OrderSelect(x,SELECT_BY_POS))
         break;
      if(OrderSymbol() == _Symbol && OrderMagicNumber() == Inp_MagicNumber)
        {
         double
         Dur = iBarShift(_Symbol,Wtf,OrderOpenTime());
         if(OrderOpenTime() > 0 && Dur > Inp_nBars)
            break;
         if(Dur < Inp_nBars)
            continue;
         if(!(OrderType()==OP_BUY||OrderType()==OP_SELL))
           {
            if(!OrderDelete(OrderTicket(),clrNONE))
              {
               Print(GetLastError());
               return;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool NewBar()
  {
   static datetime LB;
   datetime CB=iTime(_Symbol,Wtf,0);
   if(LB!=CB)
     {
      LB=CB;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetComment()
  {
   Comment(mode,"\nTraded symbol : ",currsym,"\nWorking time frame : ",EnumToString(Wtf),
           "\nAcc # : ",acc,"\nAcc currency : ",acccurr,"\nServer : ",server);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CheckVolumeValue(double vol)
  {
   double min_volume=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   if(vol<min_volume)
      return(min_volume);
   double max_volume=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
   if(vol>max_volume)
      return(max_volume);
   double volume_step=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(vol/volume_step);
   if(MathAbs(ratio*volume_step-vol)>0.0000001)
      return(ratio*volume_step);
   return(vol);
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
      Print("Not enough money for ", oper," ",lots, " ", _Symbol, " errmsg",GetLastError());
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
