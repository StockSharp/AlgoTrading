//+---------------------------------------------------------------------------+
    Donation are accepted to get the access to strategy tester: contact on PM 
    on MQL 4 website
//+---------------------------------------------------------------------------+
#property copyright "Copyright © 2010"
#property link      ""
#include <stdlib.mqh>
//----
//+---------------------------------------------------+
//|Money Management                                   |
//+---------------------------------------------------+
extern bool AccountIsMini=true;         // Change to true if trading mini account
extern bool MoneyManagement=true;       // Change to false to shutdown money management controls.
extern bool UseTrailingStop=true;
//----                                  // Lots = 1 will be in effect and only 1 lot will be open regardless of equity.
extern double TradeSizePercent  = 30;   // Change to whatever percent of equity you wish to risk.
extern double Lots              = 0.1; // you can change the lot but be aware of margin. Its better to trade with 1/4 of your capital. 
extern double MaxLots=200;
extern double StopLoss          = 1000;  // Maximum pips willing to lose per position.


extern int TakeProfit           = 5;   // Maximum profit level achieved. recomended  no more than 20
extern double MarginCutoff      = 300;  // Expert will stop trading if equity level decreases to that level.
extern int Slippage             = 3;   // Possible fix for not getting closed Could be higher with some brokers    
//----
int cnt, ticket;
int MagicNumber;        // Magic EA identifier. Allows for several co-existing EA with different input values
string ExpertName;      // To "easy read" which EA place an specific order and remember me forever :)
double lotMM;
int TradesInThisSymbol;

 

 
double BAL, RAVI0_2_24_H1, LastBid, RAVI0_2_24_D1, RAVI0_2_24_D1_1, RAVI0_2_24_D1_2, RAVI0_2_24_D1_3, Pnt;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
 int init() 
  {
   MagicNumber=3000 + func_Symbol2Val(Symbol())*100 + func_TimeFrame_Const2Val(Period());
   ExpertName="DVD 100 cent: " + MagicNumber + " : " + Symbol() + "_" + func_TimeFrame_Val2String(func_TimeFrame_Const2Val(Period()));
   Pnt = Point*MathPow(10,Digits-5);
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
 int deinit() 
  {
   return(0);
  }

//+------------------------------------------------------------------+
//| CheckExitCondition                                               |
//| Check if any rules are met for close of trade                    |
//| This EA closes trades by hitting StopLoss or TrailingStop        |
//| No Exit rules so always return false                             |
//+------------------------------------------------------------------+
 bool CheckExitCondition(string TradeType,double OpenPrice, datetime OpenTime)
  {
   return(false);
  }
   
 bool calcTrend()
  {
      RAVI0_2_24_H1      = Get_RAVI(PERIOD_H1,2,24, MODE_SMA,PRICE_OPEN,0);
      RAVI0_2_24_D1      = Get_RAVI(PERIOD_D1,2,24, MODE_SMA,PRICE_OPEN,0);
      RAVI0_2_24_D1_1    = Get_RAVI(PERIOD_D1,2,24, MODE_SMA,PRICE_OPEN,1);
      RAVI0_2_24_D1_2    = Get_RAVI(PERIOD_D1,2,24, MODE_SMA,PRICE_OPEN,2);
      RAVI0_2_24_D1_3    = Get_RAVI(PERIOD_D1,2,24, MODE_SMA,PRICE_OPEN,3);
  }
//+------------------------------------------------------------------+
//| CheckEntryCondition                                              |
//| Check if rules are met for Buy trade                             |
//+------------------------------------------------------------------+
bool CheckEntryConditionBUY()
  {
   BAL = 0;      
                                       
   if (RAVI0_2_24_H1 <  -0.00) BAL = BAL + 10; 
   
   double Level100;
   int PointFromLevelGo = 50, PtFrRise = 700;
   Level100 = StrToDouble(DoubleToStr(Bid,2)) + PointFromLevelGo*Pnt;
   
   if (iHigh(NULL,PERIOD_H1,1) > Level100 + PtFrRise*Pnt || iHigh(NULL,PERIOD_H1,2) > Level100 + PtFrRise*Pnt) BAL = BAL + 7;
   
   if (Bid < Level100  && iClose(NULL,PERIOD_M1,1) > Level100 && iLow(NULL,PERIOD_H1,0) > Level100 - PointFromLevelGo*Pnt + 30*Pnt
                                                              && iLow(NULL,PERIOD_H1,1) > Level100 - PointFromLevelGo*Pnt + 30*Pnt 
                                                              && iLow(NULL,PERIOD_H1,2) > Level100 - PointFromLevelGo*Pnt ) BAL = BAL + 45;
                                               
   int HiLevel = 600, LoLevel = 250, x, LoLevel2 = 450;
   for (x=0;x<=11;x++)  {if (iHigh(NULL,PERIOD_M1,x) > Level100 + HiLevel*Pnt) BAL = BAL - 50;}
   
   for (x=0;x<=30;x++) {if (iHigh(NULL,PERIOD_M1,x + 3) - iLow(NULL,PERIOD_M1,x) > 300*Pnt  && iOpen(NULL,PERIOD_M1,x + 3) > iClose(NULL,PERIOD_M1,x) 
                                                                                          && RAVI0_2_24_D1 < -2) BAL = BAL - 50;}
     
   bool IsCrossLowLevel2 = false; 
   for (x=0;x<=14;x++)  {if (iHigh(NULL,PERIOD_H1,x) > Level100 + LoLevel2*Pnt) IsCrossLowLevel2 = true;}
   if (IsCrossLowLevel2 == false) BAL = BAL - 50;

   if (iHigh(NULL,PERIOD_M30,0) < Level100 + LoLevel*Pnt   && iHigh(NULL,PERIOD_M30,1) < Level100 + LoLevel*Pnt
                                                           && iHigh(NULL,PERIOD_M30,2) < Level100 + LoLevel*Pnt
                                                           && iHigh(NULL,PERIOD_M30,3) < Level100 + LoLevel*Pnt
                                                           && iHigh(NULL,PERIOD_M30,4) < Level100 + LoLevel*Pnt
                                                           && iHigh(NULL,PERIOD_M30,5) < Level100 + LoLevel*Pnt
                                                           && iHigh(NULL,PERIOD_M30,6) < Level100 + LoLevel*Pnt
                                                           && iHigh(NULL,PERIOD_M30,7) < Level100 + LoLevel*Pnt) BAL = BAL - 50;
   if (BAL >= 50) return(true); 
   
   return(false);
  }
//+------------------------------------------------------------------+
//| CheckEntryCondition                                              |
//| Check if rules are met for open of trade                         |
//+------------------------------------------------------------------+
int MyLevel=100;

bool CheckEntryConditionSELL()
  {
   BAL = 0;                                          
                   
   if (RAVI0_2_24_H1 >  0.00) BAL = BAL + 10; 
   
   double Level100;
   int PointFromLevelGo = 50, PtFrRise = 700;
   Level100 = StrToDouble(DoubleToStr(Bid,2)) - PointFromLevelGo*Pnt;
   
   if (iLow(NULL,PERIOD_H1,1) < Level100 - PtFrRise*Pnt || iLow(NULL,PERIOD_H1,2) < Level100 - PtFrRise*Pnt) BAL = BAL + 7;
   
   if (Bid > Level100  && iClose(NULL,PERIOD_M1,1) < Level100 && iHigh(NULL,PERIOD_H1,0) < Level100 + PointFromLevelGo*Pnt - 30*Pnt 
                                                              && iHigh(NULL,PERIOD_H1,1) < Level100 + PointFromLevelGo*Pnt - 30*Pnt 
                                                              && iHigh(NULL,PERIOD_H1,2) < Level100 + PointFromLevelGo*Pnt ) BAL = BAL + 45;
                                               
   int HiLevel = 600, LoLevel = 250, x, LoLevel2 = 450;
   for (x=0;x<=11;x++) {if (iLow(NULL,PERIOD_M1,x) < Level100 - HiLevel*Pnt) BAL = BAL - 50;}
   
   for (x=0;x<=30;x++) {if (iHigh(NULL,PERIOD_M1,x) - iLow(NULL,PERIOD_M1,x + 3) > 300*Pnt && iClose(NULL,PERIOD_M1,x) > iOpen(NULL,PERIOD_M1,x + 3) 
                                                                                         && RAVI0_2_24_D1 > 2) BAL = BAL - 50;}

   bool IsCrossLowLevel2 = false; 
   for (x=0;x<=14;x++) {if (iLow(NULL,PERIOD_H1,x) < Level100 - LoLevel2*Pnt) IsCrossLowLevel2 = true;}
   if (IsCrossLowLevel2 == false) BAL = BAL - 50;

   if (iLow(NULL,PERIOD_M30,0) > Level100 - LoLevel*Pnt && iLow(NULL,PERIOD_M30,1) > Level100 - LoLevel*Pnt
                                                          && iLow(NULL,PERIOD_M30,2) > Level100 - LoLevel*Pnt
                                                          && iLow(NULL,PERIOD_M30,3) > Level100 - LoLevel*Pnt
                                                          && iLow(NULL,PERIOD_M30,4) > Level100 - LoLevel*Pnt
                                                          && iLow(NULL,PERIOD_M30,5) > Level100 - LoLevel*Pnt
                                                          && iLow(NULL,PERIOD_M30,6) > Level100 - LoLevel*Pnt
                                                          && iLow(NULL,PERIOD_M30,7) > Level100 - LoLevel*Pnt) BAL = BAL - 50;
   if (BAL >= 50) return(true); 
   
   return(false);
  }
 
 //+------------------------------------------------------------------+
//| Проверка времени торгов                                           |
//+------------------------------------------------------------------+
  bool ValidTime() 
  {
   if (DayOfWeek()==1 && Hour()<=6) return(false);  
  //if (IsNewsDVD(29,65)) return(false);  
    return(true);
   }

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
 int OpPozBUYpred, OpPozSELLpred;
 
  int start() 
  {
   calcTrend();            
   HandleOpenPositions();
 
   if(!ValidTime()) return(0);
 
   TradesInThisSymbol=openPositions();
   int OpPozBUY = openPositionsBUY();
   int OpPozSELL = openPositionsSELL();
   //+------------------------------------------------------------------+
   //| Check if OK to make new trades                                   |
   //+------------------------------------------------------------------+
   // Only allow 1 trade per Symbol
   int KolPozOpen = 1;
    
   // If there is no open trade for this pair and this EA
   if(AccountFreeMargin() < MarginCutoff) 
     {
      Print("Not enough money to trade Strategy:", ExpertName);
      return(0);
     }
   lotMM=GetLots();
   
   if (OpPozBUYpred > OpPozBUY) { SendMail("DVD 100 cent: Close BUY at " + Bid,"");}
   OpPozBUYpred = OpPozBUY;
   
   if(CheckEntryConditionBUY() && OpPozBUY < KolPozOpen)
     {
      OpenBuyOrder();
     }

   if (OpPozSELLpred > OpPozSELL) { SendMail("DVD 100 cent: Close SELL at " + Bid,"");}
   OpPozSELLpred = OpPozSELL;

   if (CheckEntryConditionSELL() && OpPozSELL < KolPozOpen)
     {
      OpenSellOrder();
     }
     
   return(0);
  }


//--------------------------------
double Get_RAVI(int timeframe, int Period1, int Period2, int MA_Metod,  int  PRICE, int shift)
   {
    double MA1, MA2, result; 
   
    MA1 = iMA(NULL, timeframe, Period1 ,0, MA_Metod, PRICE, shift); 
    MA2 = iMA(NULL, timeframe, Period2, 0, MA_Metod, PRICE, shift); 
    result = ((MA1 - MA2) / MA2)*100; 
    return(result);
   
   }

//+------------------------------------------------------------------+
//| OpenBuyOrder                                                     |
//| If Stop Loss or TakeProfit are used the values are calculated    |
//| for each trade                                                   |
//+------------------------------------------------------------------+
void OpenBuyOrder()
  {

   int err, ticket;
   color myColor = Green;
   
   double myPrice      = Bid - 10*Pnt*10;
   double myTakeProfit = myPrice + TakeProfit * Pnt*10;                                                     //
   if (RAVI0_2_24_D1 > 1 && RAVI0_2_24_D1 < 5 && RAVI0_2_24_D1_1 < RAVI0_2_24_D1 && RAVI0_2_24_D1_2 < RAVI0_2_24_D1_1 && RAVI0_2_24_D1_3 < RAVI0_2_24_D1_2) myTakeProfit = myTakeProfit + 25 * Pnt*10;
   double myStopLoss   = myPrice - StopLoss * Pnt*10;
   datetime myTimeEnd  = TimeCurrent() + 1200;
  
   ticket=OrderSend(Symbol(),OP_BUYLIMIT,lotMM,myPrice,Slippage,myStopLoss,myTakeProfit,ExpertName, MagicNumber,myTimeEnd,myColor);
 
   string MyTxt, subject;
   MyTxt = " for " + DoubleToStr(myPrice,4) 
         + " BAL :" + BAL 
         + " RAVI0_2_24_H1 :" + RAVI0_2_24_H1; 
         
   subject = "DVD 100 cent: OpenBuy for " + DoubleToStr(myPrice,4) + " " + Symbol() + " lot " + DoubleToStr(lotMM,2);
   if(ticket<=0)
     {
      err=GetLastError();
      Print("DVD 100 cent: Error opening BUY order [" + ExpertName + "]: (" + err + ") " + ErrorDescription(err) + " /// " + MyTxt);
      SendMail("DVD 100 cent: Error OpenBuy ","[" + ExpertName + "]: (" + err + ") " + ErrorDescription(err) + " /// " + MyTxt);
      return(0);
     }
   Print("DVD 100 cent: OpenBuy" + MyTxt); 
   SendMail(subject,MyTxt);


  }
//+------------------------------------------------------------------+
//| OpenSellOrder                                                    |
//| If Stop Loss or TakeProfit are used the values are calculated    |
//| for each trade                                                   |
//+------------------------------------------------------------------+
void OpenSellOrder()
  {
   int err, ticket;
   color myColor = Green;
   
   double myPrice = Bid + 7*Pnt*10;         
   double myTakeProfit = myPrice - TakeProfit * Pnt*10;                                                           //
   if (RAVI0_2_24_D1 < -1 && RAVI0_2_24_D1 > -5 && RAVI0_2_24_D1_1 > RAVI0_2_24_D1 && RAVI0_2_24_D1_2 > RAVI0_2_24_D1_1 && RAVI0_2_24_D1_3 > RAVI0_2_24_D1_2) myTakeProfit = myTakeProfit - 25 * Pnt*10;
   //if (openPositionsSELLreal() > 0)  myTakeProfit = myTakeProfit - 101 * Pnt*10;
   double myStopLoss   = myPrice + StopLoss * Pnt*10;
   //if (openPositionsSELLreal() > 0)  myStopLoss = myStopLoss - 100 * Pnt*10;
   datetime myTimeEnd  = TimeCurrent() + 1200;
  
   ticket=OrderSend(Symbol(),OP_SELLLIMIT,lotMM,myPrice,Slippage,myStopLoss,myTakeProfit,ExpertName, MagicNumber,myTimeEnd,myColor);
   
   string MyTxt, subject;
   MyTxt = " for " + DoubleToStr(myPrice,4) 
         + " BAL :" + BAL 
         + " RAVI0_2_24_H1 :" + RAVI0_2_24_H1; 
         
   subject = "DVD 100 cent: OpenSell for " + DoubleToStr(myPrice,4) + " " + Symbol() + " lot " + DoubleToStr(lotMM,2);
   if(ticket<=0)
     {
      err=GetLastError();
      Print("DVD 100 cent: Error opening Sell order [" + ExpertName + "]: (" + err + ") " + ErrorDescription(err) + " /// " + MyTxt);
      SendMail("DVD 100 cent: Error OpenSell ","[" + ExpertName + "]: (" + err + ") " + ErrorDescription(err) + " /// " + MyTxt);
      return(0);
     }
   Print("DVD 100 cent: OpenSell" + MyTxt); 
   SendMail(subject,MyTxt);
  }
//+------------------------------------------------------------------------+
//| counts the number of open positions                                    |
//+------------------------------------------------------------------------+
int openPositions(  )
  {  int op =0;
   for(int i=OrdersTotal()-1;i>=0;i--)                                // scan all orders and positions...
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      //if (OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_BUY)op++;
         if(OrderType()==OP_SELL)op++;
         if(OrderType()==OP_BUYLIMIT)op++;
         if(OrderType()==OP_SELLLIMIT)op++;
        }
     }
   return(op);
  }
//+------------------------------------------------------------------------+
//| counts the number of open positions BUY                                    |
//+------------------------------------------------------------------------+
int openPositionsBUYreal(  )
  {  int op =0;
   for(int i=OrdersTotal()-1;i>=0;i--)                                // scan all orders and positions...
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      //if (OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_BUY )op++;
         if(OrderType()==OP_BUYLIMIT)op++;
        }
     }
   return(op);
  }//+------------------------------------------------------------------------+
//| counts the number of open positions BUY                                    |
//+------------------------------------------------------------------------+
int openPositionsBUY(  )
  {  int op =0;
   for(int i=OrdersTotal()-1;i>=0;i--)                                // scan all orders and positions...
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      //if (OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_BUY      )op++;//&& OrderOpenPrice() - Bid < -600*Pnt 
         if(OrderType()==OP_BUYLIMIT )op++;//&& OrderOpenPrice() - Bid < -600*Pnt 
        }
     }
   return(op);
  }
//+------------------------------------------------------------------------+
//| counts the number of open positions SELL                                   |
//+------------------------------------------------------------------------+
int openPositionsSELLreal(  )
  {  int op =0;
   for(int i=OrdersTotal()-1;i>=0;i--)                                // scan all orders and positions...
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      //if (OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_SELL)op++;
         if(OrderType()==OP_SELLLIMIT)op++;
        }
     }
   return(op);
  }//+------------------------------------------------------------------------+
//| counts the number of open positions SELL                                   |
//+------------------------------------------------------------------------+
int openPositionsSELL(  )
  {  int op =0;
   for(int i=OrdersTotal()-1;i>=0;i--)                                // scan all orders and positions...
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      //if (OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_SELL       )op++;//&& Bid - OrderOpenPrice()  < 600*Pnt
         if(OrderType()==OP_SELLLIMIT )op++;//&& Bid - OrderOpenPrice()  < 600*Pnt 
        }
     }
   return(op);
  }

//+------------------------------------------------------------------+
//| Close Open Position Controls                                     |
//|  Try to close position 3 times                                   |
//+------------------------------------------------------------------+
void CloseOrder(int ticket,double numLots,double close_price)
  {
   int CloseCnt, err;
   // try to close 3 Times
   CloseCnt=0;
   while(CloseCnt < 3)
     {
      if (OrderClose(ticket,numLots,close_price,Slippage,Violet))
        {
         CloseCnt=3;
        }
      else
        {
         err=GetLastError();
         Print(CloseCnt," Error closing order : (", err , ") " + ErrorDescription(err));
         if (err > 0) CloseCnt++;
        }
     }
  }
//+------------------------------------------------------------------+
//| HandleTrailingStop                                               |
//+------------------------------------------------------------------+

int HandleTrailingStop(string type, int ticket, double op, double os, double tp)
 {
     int x; bool IsGoHi;
    
     if (type== "SELL")
     {
      IsGoHi = false;
      for (x=0;x<=30;x++) {if (iHigh(NULL,PERIOD_M1,x) - iLow(NULL,PERIOD_M1,x + 3) > 500*Pnt && iClose(NULL,PERIOD_M1,x) > iOpen(NULL,PERIOD_M1,x + 3)) IsGoHi = true;}
      if(op < Ask && IsGoHi && tp < op - 50*Pnt) ModifyOrder(ticket,op,os,op  - 10*Pnt );
      }
    
     if (type== "BUY")
     {
      IsGoHi = false;
      for (x=0;x<=30;x++) {if (iHigh(NULL,PERIOD_M1,x + 3) - iLow(NULL,PERIOD_M1,x) > 500*Pnt && iClose(NULL,PERIOD_M1,x) < iOpen(NULL,PERIOD_M1,x + 3)) IsGoHi = true;}
      if(op > Bid && IsGoHi && tp > op + 50*Pnt) ModifyOrder(ticket,op,os,op + 10*Pnt);
      }


   return(0);  // не используется
  }
//+------------------------------------------------------------------+
//|  Modify Open Position Controls                                   |
//|  Try to modify position 3 times                                  |
//+------------------------------------------------------------------+
void ModifyOrder(int ord_ticket,double op, double price,double tp)
  {
   int CloseCnt, err;
   CloseCnt=0;
   while(CloseCnt < 3)
     {
      if (OrderModify(ord_ticket,op,price,tp,0,Aqua))
        {
         CloseCnt=3;
        }
      else
        {
         err=GetLastError();
         Print(CloseCnt," Error modifying order : (", err , ") " + ErrorDescription(err));
         if (err>0) CloseCnt++;
        }
     }
  } 
  
//+------------------------------------------------------------------+
//| Handle Open Positions                                            |
//| Check if any open positions need to be closed or modified        |
//+------------------------------------------------------------------+
int HandleOpenPositions()
  {
   int cnt;
   bool YesClose;
   double pt;
//----
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect (cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()!=Symbol()) continue;
      if(OrderMagicNumber()!=MagicNumber)  continue;
      if(OrderType()==OP_BUY)
        {
         if (CheckExitCondition("BUY",OrderOpenPrice(),OrderOpenTime() ))
           {
            CloseOrder(OrderTicket(),OrderLots(),Bid);
           }
         else
           {
            if (UseTrailingStop)
              {
               HandleTrailingStop("BUY",OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit());
              }
           }
        }
      if(OrderType()==OP_SELL)
        {
         if (CheckExitCondition("SELL",OrderOpenPrice(),OrderOpenTime()))
           {
            CloseOrder(OrderTicket(),OrderLots(),Ask);
           }
         else
           {
            if(UseTrailingStop)
              {
               HandleTrailingStop("SELL",OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit());
              }
           }
        }
      if(OrderType()==OP_SELLLIMIT)
        {
         if (CheckExitCondition("LIMIT",OrderOpenPrice(),OrderOpenTime()))
           {
            CloseOrder(OrderTicket(),OrderLots(),Ask);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Get number of lots for this trade                                |
//+------------------------------------------------------------------+
double GetLots()
  {
   double lot;
   if(MoneyManagement)
     {
      lot=LotsOptimized();
     }
     else 
     {
      lot=Lots;
      if(AccountIsMini)
        {
         if (lot > 1.0) lot=lot/10;
         if (lot < 0.1) lot=0.1;
        }
     }
     //----
   return(lot);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
//---- select lot size
   lot=NormalizeDouble(MathFloor(AccountFreeMargin()*TradeSizePercent/1000)/100,2);
   // Check if mini or standard Account
   if(AccountIsMini)
     {
      lot=MathFloor(lot*100)/100;
      // Use at least 1 mini lot
      if(lot<0.1) lot=0.1;
      if (lot > MaxLots) lot=MaxLots;
     }
     else
     {
      if (lot < 1.0) lot=1.0;
      if (lot > MaxLots) lot=MaxLots;
     }
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| Time frame interval appropriation  function                      |
//+------------------------------------------------------------------+
  int func_TimeFrame_Const2Val(int Constant)
  {
     switch(Constant) 
     {
         case 1:  // M1
            return(1);
         case 5:  // M5
            return(2);
         case 15:
            return(3);
         case 30:
            return(4);
         case 60:
            return(5);
         case 240:
            return(6);
         case 1440:
            return(7);
         case 10080:
            return(8);
         case 43200:
            return(9);
        }
     }
//+------------------------------------------------------------------+
//| Time frame string appropriation  function                        |
//+------------------------------------------------------------------+
 string func_TimeFrame_Val2String(int Value)
 {
    switch(Value) 
    {
        case 1:  // M1
           return("PERIOD_M1");
        case 2:  // M1
           return("PERIOD_M5");
        case 3:
           return("PERIOD_M15");
        case 4:
           return("PERIOD_M30");
        case 5:
           return("PERIOD_H1");
        case 6:
           return("PERIOD_H4");
        case 7:
           return("PERIOD_D1");
        case 8:
           return("PERIOD_W1");
        case 9:
           return("PERIOD_MN1");
        default:
           return("undefined " + Value);
       }
    }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int func_Symbol2Val(string symbol) 
  {
     if(symbol=="AUDCAD") 
     {
      return(1);
      }
       else if(symbol=="AUDJPY") 
      {
         return(2);
         }
          else if(symbol=="AUDNZD") 
         {
         return(3);
         }
          else if(symbol=="AUDUSD") 
         {
         return(4);
         }
          else if(symbol=="CHFJPY") 
         {
         return(5);
         }
          else if(symbol=="EURAUD") 
         {
         return(6);
         }
          else if(symbol=="EURCAD") 
         {
         return(7);
         }
          else if(symbol=="EURCHF") 
         {
         return(8);
         }
          else if(symbol=="EURGBP") 
         {
         return(9);
         }
          else if(symbol=="EURJPY") 
         {
         return(10);
         }
          else if(symbol=="EURUSD") 
         {
         return(11);
         }
          else if(symbol=="GBPCHF") 
          {
         return(12);
         }
          else if(symbol=="GBPJPY") 
         {
         return(13);
         }
          else if(symbol=="GBPUSD") 
         {
         return(14);
         }
          else if(symbol=="NZDUSD") 
         {
         return(15);
         }
          else if(symbol=="USDCAD") 
         {
         return(16);
         }
          else if(symbol=="USDCHF") 
         {
         return(17);
         }
          else if(symbol=="USDJPY") 
         {
         return(18);
         }
          else 
         {
      Comment("unexpected Symbol");
      return(0);
     }
  }
//+------------------------------------------------------------------+