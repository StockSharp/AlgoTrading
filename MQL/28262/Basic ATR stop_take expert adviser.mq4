//+------------------------------------------------------------------+
//|                           Basic ATR stop_take expert adviser.mq4 |
//|                             Copyright 2020, DKP Sweden,CS Robots |
//|                             https://www.mql5.com/en/users/kenpar |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, DKP Sweden,CS Robots"
#property link      "https://www.mql5.com/en/users/kenpar"
#property version   "1.00"
#property strict
//////////////////////////////////////////////////////////////////////
//In this template i am demonstrating the use of ATR based 
//take profit and stop loss function
//Should not be used for trading as there are no enty rules!
//Basic template for further development - NOT TRADING!
//////////////////////////////////////////////////////////////////////
//Update information
//
//////////////////////////////////////////////////////////////////////
//--Enum
enum OT {_buy,_sell,None};
//--Externals
extern int    MagicNumber   = 1234567;//Magic number
input OT      Ordertype     = _buy;//Market order type
extern double FixedLot      = 0.01;//Lots size
input ENUM_TIMEFRAMES atrTf = PERIOD_CURRENT;//ATR time frame
input int     atrPer        = 14;//ATR period
extern double TPfactor      = 2.0;//ATR Take profit factor multiplier
extern double SLfactor      = 1.5;//ATR Stop loss factor multiplier
//--Internals
int    Ticket = 0,Sell,Buy,_mode;
double Lots,_point,pricemode,atr,_SL,_TP,atrTP,atrSL;
color col;
string _type="";
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---Set symbol digits
   if((Digits==5)||(Digits==3))
      _point=Point*10;
   else
      _point=Point;
//---
   if(Ordertype==None)
     {
      MessageBox("You need to select one order type direction - Buy or Sell!");
      return(INIT_FAILED);
     }
   else
      Print("Order type selection check - Trading allowed");
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
   if(Position()==0)//If no open orders on current chart continue
     {
      SendOrder(SLfactor,TPfactor);
     }
//--
   return;
  }
//--
void SendOrder(double _stop,double _take)//Order send module buy/sell
  {
//---
   atr = iATR(Symbol(), atrTf, atrPer, 0);
   atrSL =(atr * _stop / _point);
   atrTP =(atr * _take / _point);
//---
   switch(Ordertype)
     {
      case _buy: //Buy order
         _mode =OP_BUY;
         pricemode = Ask;
         col = Green;
         _type="BUY";
         _SL = Ask - atrSL * _point;
         _TP = Ask + atrTP * _point;
         break;
      case _sell://Sell order
         _mode = OP_SELL;
         pricemode = Bid;
         col = Red;
         _type="SELL";
         _SL = Bid + atrSL * _point;
         _TP = Bid - atrTP * _point;
         break;
     }
   if(CheckMoneyForTrade(Symbol(),_mode,LotSize()))
      Ticket=OrderSend(Symbol(),_mode,LotSize(),pricemode,5,0,0,WindowExpertName(),MagicNumber,0,col);
   if(Ticket<1)
     {
      Print("Order send failed, OrderType : ",(string)_type,", errcode : ",GetLastError());
      return;
     }
   else
      Print("OrderType : ",(string)_type,", executed successfully!");
   if(OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
     {
      if(!OrderModify(OrderTicket(), OrderOpenPrice(), _SL, _TP, 0))
        {
         Print("Failed setting TP/SL OrderType : ",(string)_type,", errcode : ",GetLastError());
         return;
        }
      else
         Print("Successfully setting TP/SL on OrderType : ",(string)_type);
     }
  }
//--
int Position()//This function prevents this adviser from interfere with other experts you may use on same account
  {           // It checks and handle it's own orders.
   int dir=0;
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(!OrderSelect(i, SELECT_BY_POS))
         break;
      if(OrderSymbol()!=Symbol()&&OrderMagicNumber()!= MagicNumber)
         continue;
      if(OrderCloseTime() == 0 && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(OrderType() == OP_SELL)
            dir = -1; //Short positon
         if(OrderType() == OP_BUY)
            dir = 1; //Long positon
        }
     }
   return(dir);
  }
//--
double LotSize()//Fixed lots size calculation
  {
   Lots = MathMin(MathMax((MathRound(FixedLot/MarketInfo(Symbol(),MODE_LOTSTEP))*MarketInfo(Symbol(),MODE_LOTSTEP)),
                          MarketInfo(Symbol(),MODE_MINLOT)),MarketInfo(Symbol(),MODE_MAXLOT));
   return(Lots);
  }
//--Money check
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
