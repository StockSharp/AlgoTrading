//+------------------------------------------------------------------+
//|                                            Basic_MA_Template.mq4 |
//|                                      Copyright 2020, cs software |
//|                                   https://cs-robots5.webnode.se/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, cs software"
#property link      "https://cs-robots5.webnode.se/"
#property version   "1.01"
#property strict
//////////////////////////////////////////////////////////////////////
//Update information
//v1.01 - New improved order send modules
//////////////////////////////////////////////////////////////////////
extern int    MagicNumber = 1234567;
extern int    Slippage    = 5;
extern double TakeProfit  = 38.5;//Take proft in pips
extern double StopLoss    = 48.5;//Stop loss in pips
input ENUM_TIMEFRAMES MovingTf = PERIOD_H4;//Moving time frame
input ENUM_MA_METHOD MovingMode = MODE_SMA;//Moving mode
input int     MovingPeriod  =49;//Moving average period
input int     MovingShift   =0;//Moving average shift
//--
double PT,SL,TP,indi,stops;
int    Ticket = 0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---Set point
   if((Digits==5)||(Digits==3))
      PT = Point*10;
   else
      PT = Point;
   stops=MarketInfo(Symbol(),MODE_STOPLEVEL)*PT;
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
   if(PosSelect()==0)
     {
      if(Signal() == 1)//Buy signal and no current chart positions exists
        {
         BuyOrder(StopLoss,TakeProfit);
        }
      if(Signal() == -1)//Sell signal and no current chart positions exists
        {
         SellOrder(StopLoss,TakeProfit);
        }
     }
  }
//////////////////////////////////////////////////////////////////////
//Moving Average signal function
int Signal()
  {
//---New bar
   if(Volume[0]>1)
      return(0);
//---
   int sig=0;
//---Ma indicator for signal
   indi=iMA(NULL,MovingTf,MovingPeriod,MovingShift,MovingMode,PRICE_CLOSE,0);
//---
   if(Open[1]>indi && Close[1]<indi)//Sell signal
      sig=-1;
//---
   if(Open[1]<indi && Close[1]>indi)//Buy signal
      sig=1;
//---
   return(sig);//Return value of sig
  }
//////////////////////////////////////////////////////////////////////
//Buy order function (ECN style -  stripping out the StopLoss and
//TakeProfit. Next, it modifies the newly opened market order by adding the desired SL and TP)
void BuyOrder(double stop,double take)
  {
   double Contract=CheckVolumeValue(0.01);
   if(CheckMoneyForTrade(Symbol(),OP_BUY,Contract))
      Ticket = OrderSend(Symbol(), OP_BUY, Contract, Ask, Slippage, 0, 0, "", MagicNumber, 0, Blue);
//---
   if(Ticket<0)
     {
      Print("Order send error BUY order - errcode : ",GetLastError());
      return;
     }
   else
      Print("BUY order, Ticket : ",DoubleToStr(Ticket,0),", executed successfully!");
//---
   if(OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
     {
      SL = Bid - sc(stop) * PT;
      TP = Ask + sc(take) * PT;
      if(!OrderModify(OrderTicket(), OrderOpenPrice(), SL, TP, 0))
        {
         Print("Failed setting SL/TP BUY order, Ticket : ",DoubleToStr(Ticket,0));
         return;
        }
      else
         Print("Successfully setting SL/TP BUY order, Ticket : ",DoubleToStr(Ticket,0));
     }
  }
//////////////////////////////////////////////////////////////////////
//Sell order function (ECN style -  stripping out the StopLoss and
//TakeProfit. Next, it modifies the newly opened market order by adding the desired SL and TP)
void SellOrder(double stop,double take)
  {
   double Contract=CheckVolumeValue(0.01);
   if(CheckMoneyForTrade(Symbol(),OP_SELL,Contract))
      Ticket = OrderSend(Symbol(), OP_SELL, Contract, Bid, Slippage, 0, 0, "", MagicNumber, 0, Red);
//---
   if(Ticket<1)
     {
      Print("Order send error SELL order - errcode : ",GetLastError());
      return;
     }
   else
      Print("SELL order, Ticket : ",DoubleToStr(Ticket,0),", executed successfully!");
//---
   if(OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
     {
      SL = Ask + sc(stop) * PT;
      TP = Bid - sc(take) * PT;
      if(!OrderModify(OrderTicket(), OrderOpenPrice(), SL, TP, 0))
        {
         Print("Failed setting SL/TP SELL order, Ticket : ",DoubleToStr(Ticket,0));
         return;
        }
      else
         Print("Successfully setting SL/TP SELL order, Ticket : ",DoubleToStr(Ticket,0));
     }
  }
//////////////////////////////////////////////////////////////////////
//Position selector function
int PosSelect()
  {
   int posi=0;
   for(int k = OrdersTotal() - 1; k >= 0; k--)
     {
      if(!OrderSelect(k, SELECT_BY_POS))
         break;
      if(OrderSymbol()!=Symbol()&&OrderMagicNumber()!= MagicNumber)
         continue;
      if(OrderCloseTime() == 0 && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(OrderType() == OP_BUY||OrderType() == OP_SELL)
            posi = 1;
         if(!(OrderType() == OP_SELL||OrderType() == OP_BUY))
            posi = -1; //Short positon
        }
     }
   return(posi);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double sc(double _param)
  {
   if(_param < stops)
      _param=stops;
   return(_param);
  }
////////////////////////////////////////////////////////////////
//Money check
bool CheckMoneyForTrade(string symb,int type,double lots)
  {
   double free_margin=AccountFreeMarginCheck(symb,type,lots);
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ",oper," ",lots," ",symb," Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CheckVolumeValue(double checkedvol)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(checkedvol<min_volume)
      return(min_volume);

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(checkedvol>max_volume)
      return(max_volume);

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(checkedvol/volume_step);
   if(MathAbs(ratio*volume_step-checkedvol)>0.0000001)
      return(ratio*volume_step);
   return(checkedvol);
  }
//+------------------------------------------------------------------+
