//+------------------------------------------------------------------+
//|                                                       test12.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, LX Trading"
#property version   "1.00"
#property strict

int TradeType,Ticket,Breakout_Flag,OrderFree;
double SL,TP,TP_Trade,SL_Size;
double Stop_Diff,Lots;
double PointIsPIP=(1/(Point*10000));
extern int Slippage=4;
extern int Percent=5; //percent risked per trade
extern double Breakout_Factor=15;
bool CanTrade;     //flag. true if conditions are true and there are no open orders by this EA. False otherwise
int Slip=int(Slippage*PointIsPIP);
string Symb=Symbol();
double BreakoutFactor;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   Breakout_Flag=1; //only allows one trade per break out. Flag is set so that no late entry trades will be made when terminal opens
   OrderFree=1;      //a flag that is set to 1 if no orders by this EA are open, and 0 if there is an order open that was made by this EA. Determined by order accounting
   BreakoutFactor=NormalizeDouble((Breakout_Factor/10000),4); //width of bollinger bands before a "break out" is identified 
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
// Preliminary processing
   Prelim();
//-------------------------------------------------------------------------------------------------     
   double Middle=NormalizeDouble(iBands(NULL,0,18,2,0,PRICE_CLOSE,MODE_MAIN,0),Digits);
   double MinStop=NormalizeDouble(MarketInfo(Symbol(),MODE_STOPLEVEL),Digits);

   OrderAcc();
   if(OrderFree == 0 || Breakout_Flag == 1) {CanTrade = False;}
   if(OrderFree == 1 && Breakout_Flag == 0) {CanTrade = True;}

//trading criteria + SL and TP calculations. Not opening orders until lot sizes are calculated

   SL=Middle;
// if (SL < MinStop) {Alert("Stop distance too small to trade");}

   TradeType = Trade_Criteria();
   Stop_Diff = MathAbs(NormalizeDouble((NormalizeDouble((Ask - SL), 4)*10000), 2));
   Print("Stop",Stop_Diff);

   if(TradeType==01)
     {
      TP=NormalizeDouble((Bid+(100*Point)),Digits);
     }

   if(TradeType==02)
     {
      TP=NormalizeDouble((Ask -(100*Point)),Digits);
     }

//------------------------------------------------------

   if(CanTrade==True)
     {
      Open_Order();
     }

//Stop loss modify
   if(OrderFree==0)
     {
      SL=Middle;

      if(SL<MinStop){Print("Stop modify failed - Stop distance too small");}

      if(OrderSelect(Ticket,SELECT_BY_TICKET)==True)
        {
         TP_Trade=OrderTakeProfit();
         NormalizeDouble(TP_Trade,Digits);
         bool Modify=OrderModify(Ticket,OrderOpenPrice(),SL,TP_Trade,0);
         if(Modify==False)
           {
            Errors();
           }
        }
     }

  }
//+------------------------------------------------------------------+
//| Trade_Criteria                                                   |
//+------------------------------------------------------------------+
int Trade_Criteria() //returns a 01 for buy and 02 for sell and a -1 if no criteria are met
  {
   double
   Top=NormalizeDouble(iBands(NULL,0,18,2,0,PRICE_CLOSE,MODE_UPPER,1),Digits),
   Bottom = NormalizeDouble(iBands(NULL,0,18,2,0,PRICE_CLOSE,MODE_LOWER,1),Digits),
   Middle = NormalizeDouble(iBands(NULL,0,18,2,0,PRICE_CLOSE,MODE_MAIN,0),Digits),
   Macd= NormalizeDouble(iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0),Digits),
   RSI = NormalizeDouble(iRSI(NULL,0,14,PRICE_CLOSE,0),Digits),
   EMA = NormalizeDouble(iMA(NULL,0,3,0,MODE_EMA,PRICE_CLOSE,0),Digits),
   Diff= NormalizeDouble(Top-Bottom,Digits);
//-------------------------------------------------------------------------------------------
//   The break out flag only allows one trade per position. This resets the breakout flag once the market is ready for a new position 
   if(Breakout_Flag==1 && Diff<BreakoutFactor)
     {
      Breakout_Flag=0;
     }
//-------------------------------------------------------------------------------------------------

//Buy criteria
   if((Diff>=BreakoutFactor) && (Macd>0) && (RSI>50) && (EMA>Middle))
     {
      if(Close[1]>=Top)
        {
         Stop_Diff=MathAbs(NormalizeDouble((NormalizeDouble((Ask-SL),4)*10000),2));
         return(01);
        }
     }

//Sell criteria
   if((Diff>=BreakoutFactor) && (Macd<0) && (RSI<50) && (EMA<Middle))
     {

      if(Close[1]<=Bottom)
        {
         Stop_Diff=MathAbs(NormalizeDouble((NormalizeDouble((Bid-SL),4)*10000),2));
         return(02);
           } else {return(-1);
        }
     }
   else {return(-1);}
  }
//+------------------------------------------------------------------+
//| Lot_Size                                                         |
//| Lot size calculation                                             |
//+------------------------------------------------------------------+
void Lot_Size()
  {
//lot size calculation - 5 percent of Free Margin
//the potential stop loss first needs to be calculated, in order to calculate lot size - Stop_Diff
   RefreshRates();
   double Min_Lot=MarketInfo("EURUSD",MODE_MINLOT);
   double Free = AccountFreeMargin();
   double Risk = ((Free*Percent)/100);
   if(Stop_Diff!=0)
     {
      MathAbs(Lots=NormalizeDouble((Risk/(Stop_Diff*10)),2));
     }
   if(Lots<Min_Lot) {Alert("Lot size too small, please adjust risk percentage, or add funds to your account"); Lots=0;}
   Errors();
  }
//+------------------------------------------------------------------+
//| Open_Order                                                       |
//| Order Opening function                                           |
//+------------------------------------------------------------------+
void Open_Order()
  {
   Lot_Size();
   while(true)
     {
      RefreshRates();

      if(CanTrade==True && TradeType==01)
        {
         Ticket=OrderSend("EURUSD",OP_BUY,Lots,Ask,Slip,SL,TP);
         Breakout_Flag=1;
         Errors();
        }

      if(CanTrade==True && TradeType==02)
        {
         Ticket=OrderSend("EURUSD",OP_SELL,Lots,Bid,Slip,SL,TP);
         Breakout_Flag=1;
         Errors();
        }
      break;
     }
  }
//+------------------------------------------------------------------+
//| Errors                                                           |
//| Error Processing. Simply returns an error code, if there is one  |
//+------------------------------------------------------------------+
void Errors()
  {
   int ErrNo=GetLastError();
   if(GetLastError()!=0)
     {
      (Alert("Error ",ErrNo));
     }
   return;
  }
//+------------------------------------------------------------------+
//| OrderAcc                                                         |
//| order accounting. Magic no = 1                                   |
//+------------------------------------------------------------------+
void OrderAcc()
  {
   int n;

   for(n=0; n<=OrdersTotal(); n++)
     {
      if(OrderSelect(n,SELECT_BY_POS,MODE_TRADES==true))
        {
         if(OrderSymbol()==Symb)
           {
            if(OrderMagicNumber()==1) {OrderFree=0;Ticket=OrderTicket(); break;} else {OrderFree=1;}
           }
           }else{OrderFree=1;
        }
     }
  }
//+------------------------------------------------------------------+
//| Prelim                                                           |
//+------------------------------------------------------------------+
void Prelim()
  {
   if(Bars<18) // Not enough bars     
     {
      Print("Not enough bars in the window. EA doesn't work.");
      return;                                    // Exit start()
     }
  }
//+------------------------------------------------------------------+
