//+------------------------------------------------------------------+
//|                                         Virtual_Profit_Close.mq4 |
//|                             Copyright 2020, DKP Sweden,CS Robots |
//|                             https://www.mql5.com/en/users/kenpar |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, cs software"
#property link      "https://cs-robots5.webnode.se/"
#property version   "1.01"
#property strict
//////////////////////////////////////////////////////////////////////
//This expert adviser monitor your Buy/Sell positions and closes them
//when profit level reached (virtual)
//WARNING - Adviser DO NOT monitor stoploss!!!!!!!
//
//Version updates
//
//v1.01 - 1. Trailing stop added
//        2. Selectable strategy tester order type
//////////////////////////////////////////////////////////////////////
enum tdir {Sell,Buy,};
sinput string       EAN ="-Virtual Profit Close-";
input tdir          TestDirection = Sell;//Strategy tester order type ( Buy or Sell )
extern int          MagicNumber   = 1234;//Set magic number to monitor
extern double       Profit        = 30.;//Set virtual take profit pips
sinput string       EA1 ="| -> Trailing stop";
extern bool         Utr           = true;//Use trailing stop?
extern double       Start         = 5.0;//Trailing start pips
extern double       Step          = 2.0;//Trailing step pips
//---
double
teststop=20.0,
testlot=0.01,
NewPoint,
MPP,
StopLev;
//---
bool
TradeNow=false,
_comm=false;
//---
int
ticket=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   MPP=1;
   if((MarketInfo(Symbol(),MODE_DIGITS)==3)||(MarketInfo(Symbol(),MODE_DIGITS)==5))
      MPP=10;
   NewPoint=MarketInfo(Symbol(),MODE_TICKSIZE)*MPP;
   StopLev=MarketInfo(Symbol(),MODE_STOPLEVEL)*NewPoint;
   if(IsTesting())
      TradeNow=true;
   if(!IsTesting()||IsVisualMode())
      _comm=true;
//---
   MessageBox("Thanks for using Virtual Profit close!\nFor more ea's,indicators and order management tools\nvisit CS Robots website https://cs-robots5.webnode.se/\nComplete Product listing https://www.mql5.com/en/users/kenpar/seller");
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
   Alert("Thanks for using Virtual Profit close!\nFor more ea's,indicators and order management tools\nvisit CS Robots website https://cs-robots5.webnode.se/\nComplete Product listing https://www.mql5.com/en/users/kenpar/seller");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(_comm)
      Comm();
//---
   if(Utr&&GetPosition()!=0)//Position trailing (Not virtual!)
      TStop(Start,Step);
//---
   if(GetPosition()!=0)//Monitor your desired open position by symbol&magic(Trading mode)
      CloseModule();
//---
   if(TradeNow&&GetPosition()==0)//Only used in strategy tester (Demonstration mode)
      SendTest();
//---
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SendTest()//Only used in strategy tester (Demonstration mode)
  {
   int
   dir=0;
   double
   _SL=0,
   Price=0;
   color
   Col=0;
   switch(TestDirection)
     {
      case Sell:
         dir=OP_SELL;
         _SL=Ask+CS(teststop*NewPoint);
         Price=Bid;
         Col=Red;
         break;
      case Buy:
         dir=OP_BUY;
         _SL=Bid-CS(teststop*NewPoint);
         Price=Ask;
         Col=Green;
         break;
     }
   double
   Contract=CheckVolumeValue(Symbol(),testlot);
   if(CheckMoneyForTrade(Symbol(),dir,Contract))
      ticket = OrderSend(Symbol(),dir,Contract,Price,5,_SL,0,NULL,MagicNumber,0,Col);
   if(ticket<0)
     {
      Print("error ",GetLastError());
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CS(double val)
  {
   if(val<StopLev)
      val=StopLev;
   return(val);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseModule()
  {
//---
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
         break;
      if(OrderSymbol() != Symbol() && OrderMagicNumber() != MagicNumber)
         continue;
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber)
        {
         if(OrderType() == OP_BUY)
           {
            if(Ask - OrderOpenPrice() >= NewPoint * Profit)
               if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 5, Yellow))
                 {
                  Print("Failed to close BUY position - errcode : ",GetLastError());
                  return;
                 }
               else
                  Print("Position BUY virtually closed successfully at price : ",DoubleToStr(OrderClosePrice(),Digits));
           }
         if(OrderType() == OP_SELL)
           {
            if(OrderOpenPrice() - Bid >= NewPoint * Profit)
               if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 5, Yellow))
                 {
                  Print("Failed to close SELL position - errcode : ",GetLastError());
                  return;
                 }
               else
                  Print("Position SELL virtually closed successfully at price : ",DoubleToStr(OrderClosePrice(),Digits));
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetPosition()
  {
   int posval=0;
   for(int e = OrdersTotal() - 1; e >= 0; e--)
     {
      if(!OrderSelect(e, SELECT_BY_POS))
         break;
      if(OrderSymbol()!=Symbol() && OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderCloseTime() == 0 && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(OrderType() == OP_BUY || OrderType() == OP_SELL)
            posval = 1;
         if(!(OrderType() == OP_BUY || OrderType() == OP_SELL))
            posval = -1;
        }
     }
   return(posval);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TStop(double _start,double _step)
  {
//---
   for(int x=0; x<OrdersTotal(); x++)
     {
      if(!OrderSelect(x,SELECT_BY_POS,MODE_TRADES))
         break;
      if(OrderSymbol()!=Symbol() && OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(OrderType()==OP_BUY && Bid-_step * NewPoint>OrderOpenPrice())
           {
            double
            nsb=NormalizeDouble(Bid -_start * NewPoint,Digits);
            if(nsb>OrderStopLoss() || OrderStopLoss()==0)
              {
               if(nsb<Bid - StopLev * NewPoint)
                 {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),nsb,OrderTakeProfit(),0,clrGreen))
                    {
                     Print("Modify error BUY : ",GetLastError());
                     return;
                    }
                 }
              }
           }
         if(OrderType()==OP_SELL && Ask+_step*NewPoint<OrderOpenPrice())
           {
            double
            nss=NormalizeDouble(Ask +_start * NewPoint,Digits);
            if(nss<OrderStopLoss() || OrderStopLoss()==0)
              {
               if(Ask + StopLev * NewPoint<nss)
                 {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),nss,OrderTakeProfit(),0,clrRed))
                    {
                     Print("Modify error SELL : ",GetLastError());
                     return;
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Comm()
  {
   Comment("\nSymbol monitored : ",Symbol(),"\nMagic monitored : ",MagicNumber);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CheckVolumeValue(string sym,double vol)
  {
   double min_volume=SymbolInfoDouble(sym,SYMBOL_VOLUME_MIN);
   if(vol<min_volume)
      return(min_volume);
   double max_volume=SymbolInfoDouble(sym,SYMBOL_VOLUME_MAX);
   if(vol>max_volume)
      return(max_volume);
   double volume_step=SymbolInfoDouble(sym,SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(vol/volume_step);
   if(MathAbs(ratio*volume_step-vol)>0.0000001)
      return(ratio*volume_step);
   return(vol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
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
