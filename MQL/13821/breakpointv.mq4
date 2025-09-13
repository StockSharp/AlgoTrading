//+------------------------------------------------------------------+
//|                                                   BreakPoint.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                             http://confident-trader.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015. Prepared by @TraderConfident"
#property link      "http://confident-trader.blogspot.com"
#property version   "1.1"

input bool   LotAuto=True;
input double LotManual=0.01;
input double StopLoss=0;
input double TakeProfit=30;
input bool   CloseBySignal=true;
input int    Slippage=3;
input int    MaxOpenOrder=0;

extern string _Strategy_1_=" --- Daily Break ---";
input bool Strategy_1_Enable=true;
input int  Strategy_1_BreakPoint=20;
input int  Strategy_1_LastBarSizeMin = 5;
input int  Strategy_1_LastBarSizeMax = 50;
input int  Strategy_1_TrailingStart= 5;
input int  Strategy_1_TrailingStop = 2;
input int  Strategy_1_TrailingStep = 2;
input int  Strategy_1_Magic=900001;

extern string _Averaging_=" --- Averaging ---";
input bool  Averaging_Enable=true;
input int   Averaging_FloatSize=30;
input int   Averaging_TrailingStart= 15;
input int   Averaging_TrailingStop = 10;
input int   Averaging_TrailingStep = 5;
input int   Averaging_Magic=999999;

int ticket;
int LotDigits;
double Trail,iTrailingStop;
int _lotAutoDefense;
double _pip;
string _remark="Confident";

double _lastHigh;
double _lastLow;
double _lastOpen;
double _lastClose;

bool _isBullBar,_isNewBar;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   _lotAutoDefense=1000;
   _pip=Point;
   if(Digits==3 || Digits==5) 
     {
      _lotAutoDefense=1000*10;
      _pip=_pip*10;
     }

   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   static datetime NewTime=0;
   _isNewBar=false;
   if(NewTime!=Time[0])
     {
      NewTime=Time[0];
      _isNewBar=true;
     }

   double DayOpen=iOpen(Symbol(),PERIOD_D1,0);
// Prev Bar
   _lastOpen   = iOpen(Symbol(),PERIOD_CURRENT,1);
   _lastClose  = iClose(Symbol(),PERIOD_CURRENT,1);
   _lastHigh   = iHigh(Symbol(),PERIOD_CURRENT,1);
   _lastLow=iLow(Symbol(),PERIOD_CURRENT,1);
   if(_lastClose>_lastOpen) _isBullBar=true; else _isBullBar=false;

// Strategy 1 (Daily Break Point)
   if(Strategy_1_Enable && _isNewBar)
     {
      double BreakBuy = DayOpen+Strategy_1_BreakPoint*_pip;
      double BreakSell= DayOpen-Strategy_1_BreakPoint*_pip;
      if(_isBullBar && Bid-DayOpen>=Strategy_1_BreakPoint*_pip && 
         _lastClose-_lastOpen<=Strategy_1_LastBarSizeMax*_pip &&
         _lastClose-_lastOpen>=Strategy_1_LastBarSizeMin*_pip &&
         BreakBuy>=_lastOpen &&
         BreakBuy<=_lastClose)
        {
         Buy(Strategy_1_Magic,"S1"+_remark);
        }
      if(!_isBullBar && DayOpen-Ask>=Strategy_1_BreakPoint*_pip && 
         _lastOpen-_lastClose<=Strategy_1_LastBarSizeMax*_pip &&
         _lastOpen-_lastClose>=Strategy_1_LastBarSizeMin*_pip &&
         BreakSell<=_lastOpen &&
         BreakSell>=_lastClose)
        {
         Sell(Strategy_1_Magic,"S1"+_remark);
        }
     }

   Trailing();
//---
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Trailing()
  {
   int TrailingStart=0,TrailingStop=0,TrailingStep=0;
   for(int cnt=0;cnt<OrdersTotal();cnt++) 
     {
      ticket=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==Strategy_1_Magic)
        {
         TrailingStart              = Strategy_1_TrailingStart;
         TrailingStop               = Strategy_1_TrailingStop;
         TrailingStep               = Strategy_1_TrailingStep;
           }else if(OrderMagicNumber()==Averaging_Magic){
         TrailingStart              = Averaging_TrailingStart;
         TrailingStop               = Averaging_TrailingStop;
         TrailingStep               = Averaging_TrailingStep;
        }

      if(OrderSymbol()==Symbol() && (OrderMagicNumber()==Strategy_1_Magic || OrderMagicNumber()==Averaging_Magic))
        {
         if(OrderType()==OP_BUY)
           {
            //Close
            if((TakeProfit>0 && Bid-OrderOpenPrice()>=TakeProfit*_pip) || (StopLoss>0 && OrderOpenPrice()-Ask>StopLoss*_pip))
              {
               ticket=OrderClose(OrderTicket(),OrderLots(),Bid,0,Violet);
              }
            //Trail  
            if(TrailingStart>0)
              {
               if(OrderStopLoss()==0)
                 {
                  if(Bid-OrderOpenPrice()>TrailingStart*_pip)
                    {
                     ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+TrailingStop*_pip,OrderTakeProfit(),0,Gray);
                    }
                    }else{ // This trailing martingle
                  if(Bid-OrderStopLoss()>TrailingStep*_pip)
                    {
                     ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss()+TrailingStop*_pip,OrderTakeProfit(),0,Gray);
                    }
                 }
              }
           }
         //if(OrderType()==OP_SELL)  
         else
           {
            //Close
            if((TakeProfit>0 && OrderOpenPrice()-Ask>=TakeProfit*_pip) || (StopLoss>0 && Bid-OrderOpenPrice()>StopLoss*_pip))
              {
               ticket=OrderClose(OrderTicket(),OrderLots(),Ask,0,Violet);
              }
            //Trail  
            if(TrailingStart>0)
              {
               if(OrderStopLoss()==0)
                 {
                  if(OrderOpenPrice()-Ask>TrailingStart*_pip)
                    {
                     ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-TrailingStop*_pip,OrderTakeProfit(),0,Gray);
                    }
                    }else{ // This trailing martingle
                  if(OrderStopLoss()-Ask>TrailingStep*_pip)
                    {
                     ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss()-TrailingStop*_pip,OrderTakeProfit(),0,Gray);
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
void Buy(int _magic,string _rmk)
  {
   ticket=OrderSend(Symbol(),OP_BUY,Lot(),Ask,Slippage,0.0,0.0,_rmk,_magic,0,Blue);
   if(CloseBySignal) CloseOrders("BUY");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Sell(int _magic,string _rmk)
  {
   ticket=OrderSend(Symbol(),OP_SELL,Lot(),Bid,Slippage,0.0,0.0,_rmk,_magic,0,Red);
   if(CloseBySignal) CloseOrders("SELL");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseOrders(string _op)
  {
   int  total=OrdersTotal();
   for(int y=OrdersTotal()-1; y>=0; y--)
     {
      if(OrderSelect(y,SELECT_BY_POS,MODE_TRADES))
         if(_op=="BUY")
           {
            if(OrderSymbol()==Symbol() && OrderType()==OP_SELL && OrderComment()!="" && (OrderMagicNumber()==Strategy_1_Magic || OrderMagicNumber()==Averaging_Magic)) ticket=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Black);
              }else{
            if(OrderSymbol()==Symbol() && OrderType()==OP_BUY && OrderComment()!="" && (OrderMagicNumber()==Strategy_1_Magic || OrderMagicNumber()==Averaging_Magic)) ticket=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Black);
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Lot()
  {
   double lot;
   double lot_max =MarketInfo(Symbol(),MODE_MAXLOT);
   double lot_min =MarketInfo(Symbol(),MODE_MINLOT);
   double tick=MarketInfo(Symbol(),MODE_TICKVALUE);
//---
   double  myAccount=AccountBalance();
//---
   if(lot_min==0.01) LotDigits=2;
   if(lot_min==0.1) LotDigits=1;
   if(lot_min==1) LotDigits=0;
//---
   if(LotAuto)
     {
      lot=NormalizeDouble((myAccount/_lotAutoDefense),LotDigits);
        }else{
      lot=LotManual;
     }
//---
   if(lot>lot_max) { lot=lot_max; }
   if(lot<lot_min) { lot=lot_min; }
//---
   return(lot);
  }  
//+------------------------------------------------------------------+
