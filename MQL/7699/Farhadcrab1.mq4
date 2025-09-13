//+-------------------------------------------------------------------------------------+
//|     FarhadCrab1.mq4                                                                 |
//|     Copyright © 2006, Farhad Farshad                                                |
//|     http://www.fxperz.com                                                           |
//|     http://farhadfarshad.com                                                        |
//|     This EA is optimized to work on                                                 |
//|     GBP/JPY & GBP/USD & EUR/USD M1  TimeFrame ... if you want the optimized         |
//|     EA s for any currency pair please                                               | 
//|     mail me at: info@farhadfarshad.com                                              |
//|     This is the first version of this EA. If                                        |
//|     you want the second edition ('FarhadCrab2.mq4')                                 |
//|     with considerably better performance mail me. 'FarhadMagic.mq4' and             |
//|     'Farhad2.mq4' are also available from this series.                              |
//|     (They are not free and they don't have trial version!)                          |
//|     Enjoy a better automatic investment:) with at least 100% a month.               |
//|     If you get money from this EA please donate some to poor people of your country.|
//+-------------------------------------------------------------------------------------+
#property copyright "Copyright © 2006, Farhad Farshad"
#property link      "http://www.fxperz.com"
//----
#include <stdlib.mqh>
//----
extern double lTakeProfit   =10;   // recomended  no more than 20
extern double sTakeProfit   =10;   // recomended  no more than 20
extern double takeProfit    =10;            // recomended  no more than 20
extern double stopLoss      =10;             // do not use s/l at all. Take it easy man. I'll guarantee your profit :)
extern int magicEA          =114;        // Magic EA identifier. Allows for several co-existing EA with different input values
extern double lTrailingStop =8;   // trail stop in points
extern double sTrailingStop =8;   // trail stop in points
extern color clOpenBuy      =Blue;  //Different colors for different positions
extern color clCloseBuy     =Aqua;  //Different colors for different positions
extern color clOpenSell     =Red;  //Different colors for different positions
extern color clCloseSell    =Violet;  //Different colors for different positions
extern color clModiBuy      =Blue;   //Different colors for different positions
extern color clModiSell     =Red;   //Different colors for different positions
extern int Slippage         =2;
extern double Lots          =0.1;// you can change the lot but be aware of margin. Its better to trade with 1/4 of your capital. 
extern string nameEA        ="FarhadCrab1.mq4";// To "easy read" which EA place an specific order and remember me forever :)
extern double vVolume;
//----
double macdHistCurrent, macdHistPrevious, macdSignalCurrent, macdSignalPrevious, highCurrent, lowCurrent;
double stochHistCurrent, stochHistPrevious, stochSignalCurrent, stochSignalPrevious;
double sarCurrent, sarPrevious,  momCurrent, momPrevious;
double maLongCurrent, maShortCurrent, maLongPrevious, maShortPrevious, faRSICurrent;
double realTP, realSL, faMiddle, faHighest, faLowest, closeCurrent, closeCurrentD, closePreviousD;
int cnt, ticket;
bool isBuying=false, isSelling=false, isBuyClosing=false, isSellClosing=false;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void deinit() 
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   // *****This line is for some reason very important. you'd better settle all your account at the end of day.*****
/*if (TimeHour(CurTime())==23 && MathAbs(faMiddle-faHighest)<MathAbs(faMiddle-faLowest))
{
 CloseBuyPositions();
 return(0);
 }
 if (TimeHour(CurTime())==23 && MathAbs(faMiddle-faHighest)>MathAbs(faMiddle-faLowest) )
{
 CloseSellPositions();
 return(0);
 }
*/
   //System Stoploss based on LongTerm Moving Average (Fibo  55 day MA)
   //StopLoss For Buy Positions (Optional)
     if ((maLongCurrent>closeCurrentD) && (maLongPrevious<closePreviousD)) 
     {
      CloseBuyPositions();
      return(0);
     }
   //StopLoss For Sell Positions (Optional)
     if ((maLongCurrent<closeCurrentD) && (maLongPrevious>closePreviousD)) 
     {
      CloseSellPositions();
      return(0);
     }
   // Check for invalid bars and takeprofit
     if(Bars < 200) 
     {
      Print("Not enough bars for this strategy - ", nameEA);
      return(0);
     }
      /*
       if(isBuying && !isSelling && !isBuyClosing && !isSellClosing) 
       {  // Check for BUY entry signal
         if(stopLoss > 0)
            realSL = Ask - stopLoss * Point;
         if(takeProfit > 0)
            realTP = Ask + takeProfit * Point;
         ticket = OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,realSL,realTP,nameEA+" - Magic: "+magicEA+" ",magicEA,0,Red);  // Buy
         if(ticket < 0) 
         {
            Print("OrderSend (" + nameEA + ") failed with error #" + GetLastError() + " --> " + ErrorDescription(GetLastError()));
         }
          else 
         {
             
         }
      }
      if(isSelling && !isBuying && !isBuyClosing && !isSellClosing) 
      {  // Check for SELL entry signal
         if(stopLoss > 0)
            realSL = Bid + stopLoss * Point;
         if(takeProfit > 0)
            realTP = Bid - takeProfit * Point;
         ticket = OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,realSL,realTP,nameEA+" - Magic: "+magicEA+" ",magicEA,0,Red); // Sell
         if(ticket < 0) 
         {
            Print("OrderSend (" + nameEA + ") failed with error #" + GetLastError() + " --> " + ErrorDescription(GetLastError()));
         }
          else 
         {         
        }
   }
   return(0);
   */
   calculateIndicators();                      // Calculate indicators' value 
   //Check for TakeProfit Conditions  
     if(lTakeProfit<10)
     {
      Print("TakeProfit less than 10 on this EA with Magic -", magicEA );
      return(0);
     }
     if(sTakeProfit<10)
     {
      Print("TakeProfit less than 10 on this EA with Magic -", magicEA);
      return(0);
     }
   //Introducing new expressions
   double faClose0           =iLow(NULL,0,0);
   double faMA1              =iMA(NULL,0,9,0,MODE_EMA,PRICE_TYPICAL,0);
   //double faMA2               = iMAOnArray(faMA1,0,9,0,MODE_EMA,0);
   //double faMA4               = iMAOnArray(faMA2,0,9,0,MODE_EMA,0);
   double faClose2           =iHigh(NULL,0,0);
   double faMA3              =iMA(NULL,0,9,0,MODE_SMA,PRICE_OPEN,0);
   double stochHistCurrent   =iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,0);
   double sarCurrent         =iSAR(NULL,0,0.002,0.2,0);           // Parabolic Sar Current
   double sarPrevious        =iSAR(NULL,0,0.002,0.2,1);  //Parabolic Sar Previous
   double vVolume            =iVolume(NULL,0,0);   // Current Volume
   //double faMAvVolume         = iMAOnArray(vVolume,0,9,0,MODE_SMA,0); //Simple Moving Average
   double faHighest          =Highest(NULL,PERIOD_H4,MODE_HIGH,30,0); // Highest High in an interval of time
   double faLowest           =Lowest(NULL,PERIOD_H4,MODE_LOW,30,0); //Lowest Low in an interval of time
   double faMiddle           =(faHighest+faLowest)/2; //...
   //Check Margin Requirement
     if(AccountFreeMargin()<(1000*Lots))
     {
      Print("We have no money. Free Margin = ", AccountFreeMargin());
      return(0);
     }
   //Buy Condition
     if (!takeBuyPositions())
     {
      //if ((faClose0>faMA1 && faBandWidth<faMABandWidth)){
        if ((faClose0>faMA1))
        {
         OpenBuy();
         //if (OrdersTotal()==1 && (faClose2<faMA3)) {OpenSell();}
         //if (OrdersTotal()==2 && (faClose2<faMA3)) {OpenSell();}
         //if (OrdersTotal()==3 && (faClose2<faMA3)) {OpenSell();}
         return(0);
        }
     }
   //Sell Condition
   //if ((faClose2<faMA3 && faBandWidth<faMABandWidth)){
     if (!takeSellPositions())
     {
        if ((faClose2<faMA3))
        {
         OpenSell();
         //if (OrdersTotal()==1 && (faClose0>faMA1)) {OpenBuy();}
         //if (OrdersTotal()==2 && (faClose0>faMA1)) {OpenBuy();}
         //if (OrdersTotal()==3 && (faClose0>faMA1)) {OpenBuy();}
         return(0);
        }
      //Close Buy Condition
/*      if ((faClose2<faMA3))
      {
      CloseBuy();
      return(0);
      }
      //Close Sell Condition
      if ((faClose2<faMA3))
      {
      CloseSell();
      return(0);
      }
*/
     }
   //Trailing Expressions
   TrailingPositionsBuy(lTrailingStop);
   TrailingPositionsSell(sTrailingStop);
   return(0);
  }
//Number of Buy Positions
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  bool takeBuyPositions() 
  {
   int j ;
   if ((OrdersTotal()==0)) {j=0;}
     if (maLongCurrent<closeCurrent) 
     {
      if ((CurTime()-OrderOpenTime()>300) && (sarCurrent<lowCurrent)) {j=1;}
      if ((CurTime()-OrderOpenTime()>600) && (sarCurrent<lowCurrent)) {j=2;}
      if ((CurTime()-OrderOpenTime()>900) && (sarCurrent<lowCurrent)) {j=3;}
      if ((CurTime()-OrderOpenTime()>1200) && (sarCurrent<lowCurrent)) {j=4;}
      if ((CurTime()-OrderOpenTime()>1500) && (sarCurrent<lowCurrent)) {j=5;}
      if ((CurTime()-OrderOpenTime()>1800) && (sarCurrent<lowCurrent)) {j=6;}
      if ((CurTime()-OrderOpenTime()>2100) && (sarCurrent<lowCurrent)) {j=7;}
      if ((CurTime()-OrderOpenTime()>2400) && (sarCurrent<lowCurrent)) {j=8;}
      if ((CurTime()-OrderOpenTime()>2700) && (sarCurrent<lowCurrent)) {j=9;}
      if ((CurTime()-OrderOpenTime()>3000) && (sarCurrent<lowCurrent)) {j=10;}
      if ((CurTime()-OrderOpenTime()>3300) && (sarCurrent<lowCurrent)) {j=11;}
      if ((CurTime()-OrderOpenTime()>3600) && (sarCurrent<lowCurrent)) {j=12;}
      if ((CurTime()-OrderOpenTime()>3900) && (sarCurrent<lowCurrent)) {j=13;}
      if ((CurTime()-OrderOpenTime()>4200) && (sarCurrent<lowCurrent)) {j=14;}
      if ((CurTime()-OrderOpenTime()>4500) && (sarCurrent<lowCurrent)) {j=15;}
      if ((CurTime()-OrderOpenTime()>4800) && (sarCurrent<lowCurrent)) {j=16;}
      if ((CurTime()-OrderOpenTime()>5100) && (sarCurrent<lowCurrent)) {j=17;}
      if ((CurTime()-OrderOpenTime()>5400) && (sarCurrent<lowCurrent)) {j=18;}
      if ((CurTime()-OrderOpenTime()>5700) && (sarCurrent<lowCurrent)) {j=19;}
        for(int i=j; i<OrdersTotal(); i++) 
        {
           if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
           {
              if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA) 
              {
               return(True);
              }
           }
        }
     }
   return(false);
  }
//Number of Sell Positions
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  bool takeSellPositions() 
  {
   int j ;
   if ((OrdersTotal()==0)) {j=0;}
     if (maLongCurrent>closeCurrent) 
     {
      if ((CurTime()-OrderOpenTime()>300) && (sarCurrent>highCurrent)) {j=1;}
      if ((CurTime()-OrderOpenTime()>600) && (sarCurrent>highCurrent)) {j=2;}
      if ((CurTime()-OrderOpenTime()>900) && (sarCurrent>highCurrent)) {j=3;}
      if ((CurTime()-OrderOpenTime()>1200) && (sarCurrent>highCurrent)) {j=4;}
      if ((CurTime()-OrderOpenTime()>1500) && (sarCurrent>highCurrent)) {j=5;}
      if ((CurTime()-OrderOpenTime()>1800) && (sarCurrent>highCurrent)) {j=6;}
      if ((CurTime()-OrderOpenTime()>2100) && (sarCurrent>highCurrent)) {j=7;}
      if ((CurTime()-OrderOpenTime()>2400) && (sarCurrent>highCurrent)) {j=8;}
      if ((CurTime()-OrderOpenTime()>2700) && (sarCurrent>highCurrent)) {j=9;}
      if ((CurTime()-OrderOpenTime()>3000) && (sarCurrent>highCurrent)) {j=10;}
      if ((CurTime()-OrderOpenTime()>3300) && (sarCurrent>highCurrent)) {j=11;}
      if ((CurTime()-OrderOpenTime()>3600) && (sarCurrent>highCurrent)) {j=12;}
      if ((CurTime()-OrderOpenTime()>3900) && (sarCurrent>highCurrent)) {j=13;}
      if ((CurTime()-OrderOpenTime()>4200) && (sarCurrent>highCurrent)) {j=14;}
      if ((CurTime()-OrderOpenTime()>4500) && (sarCurrent>highCurrent)) {j=15;}
      if ((CurTime()-OrderOpenTime()>4800) && (sarCurrent>highCurrent)) {j=16;}
      if ((CurTime()-OrderOpenTime()>5100) && (sarCurrent>highCurrent)) {j=17;}
      if ((CurTime()-OrderOpenTime()>5400) && (sarCurrent>highCurrent)) {j=18;}
      if ((CurTime()-OrderOpenTime()>5700) && (sarCurrent>highCurrent)) {j=19;}
        for(int i=j; i<OrdersTotal(); i++) 
        {
           if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
           {
              if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA) 
              {
               return(True);
              }
           }
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void TrailingPositionsBuy(int trailingStop) 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA) 
           {
              if (OrderType()==OP_BUY) 
              {
                 if (Bid-OrderOpenPrice()>trailingStop*Point) 
                 {
                  if (OrderStopLoss()<Bid-trailingStop*Point)
                     ModifyStopLoss(Bid-trailingStop*Point);
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void TrailingPositionsSell(int trailingStop) 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA) 
           {
              if (OrderType()==OP_SELL) 
              {
                 if (OrderOpenPrice()-Ask>trailingStop*Point) 
                 {
                  if (OrderStopLoss()>Ask+trailingStop*Point ||
OrderStopLoss()==0)
                     ModifyStopLoss(Ask+trailingStop*Point);
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void ModifyStopLoss(double ldStopLoss) 
  {
   bool fm;
   fm=OrderModify(OrderTicket(),OrderOpenPrice
   (),ldStopLoss,OrderTakeProfit(),0,CLR_NONE);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void OpenBuy() 
  {
   double ldLot, ldStop, ldTake;
   string lsComm;
   ldLot=GetSizeLot();
   ldStop=0;
   ldTake=GetTakeProfitBuy();
   lsComm=GetCommentForOrder();
   OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,ldStop,ldTake,nameEA,magicEA,0,clOpenBuy);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void OpenSell() 
  {
   double ldLot, ldStop, ldTake;
   string lsComm;
//----
   ldLot=GetSizeLot();
   ldStop=0;
   ldTake=GetTakeProfitSell();
   lsComm=GetCommentForOrder();
   OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,ldStop,ldTake,nameEA,magicEA,0,clOpenSell);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetCommentForOrder() { return(nameEA); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSizeLot() { return(Lots); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitBuy() { return(Ask+lTakeProfit*Point); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitSell() { return(Bid-sTakeProfit*Point); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void calculateIndicators() 
  {
   // Calculate indicators' value   
   macdHistCurrent    =iMACD(NULL,0,12,26,9,PRICE_OPEN,MODE_MAIN,0);
   macdHistPrevious   =iMACD(NULL,0,12,26,9,PRICE_OPEN,MODE_MAIN,1);
   macdSignalCurrent  =iMACD(NULL,0,12,26,9,PRICE_OPEN,MODE_SIGNAL,0);
   macdSignalPrevious =iMACD(NULL,0,12,26,9,PRICE_OPEN,MODE_SIGNAL,1);
   stochHistCurrent   =iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,0);
   stochHistPrevious  =iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,1);
   stochSignalCurrent =iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_SIGNAL,0);
   stochSignalPrevious=iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_SIGNAL,1);
   sarCurrent         =iSAR(NULL,0,0.009,0.2,0);           // Parabolic Sar Current
   sarPrevious        =iSAR(NULL,0,0.009,0.2,1);  //Parabolic Sar Previous
   momCurrent         =iMomentum(NULL,0,14,PRICE_OPEN,0); // Momentum Current
   momPrevious        =iMomentum(NULL,0,14,PRICE_OPEN,1); // Momentum Previous
   highCurrent        =iHigh(NULL,0,0);     //High price Current
   lowCurrent         =iLow(NULL,0,0);      //Low Price Current
   closeCurrent       =iClose(NULL,PERIOD_H4,0);  //Close Price Current for H4 TimeFrame
   closeCurrentD      =iClose(NULL,PERIOD_D1,0); //Close Price Current for D1 TimeFrame
   closePreviousD     =iClose(NULL,PERIOD_D1,1); //Close Price Previous for D1 TimeFrame
   maLongCurrent      =iMA(NULL,PERIOD_D1,55,1,MODE_SMMA,PRICE_TYPICAL,0); //Current Long Term Moving Average 
   maLongPrevious     =iMA(NULL,PERIOD_D1,55,1,MODE_SMMA,PRICE_TYPICAL,1); //Previous Long Term Moving Average 
   maShortCurrent     =iMA(NULL,0,2,1,MODE_SMMA,PRICE_TYPICAL,0);  //Current Short Term Moving Average 
   maShortPrevious    =iMA(NULL,0,2,1,MODE_SMMA,PRICE_TYPICAL,1);  //Previous Long Term Moving Average
   faRSICurrent       =iRSI(NULL,0,14,PRICE_TYPICAL,0); //Current RSI 
   // Check for BUY, SELL, and CLOSE signal
   isBuying =false;
   isSelling=false;
   isBuyClosing=false;
   isSellClosing=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void CloseBuyPositions()
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA) 
           {
            if (OrderType()==OP_BUY) OrderClose(OrderTicket(),Lots,Bid,Slippage);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void CloseSellPositions()
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA) 
           {
            if (OrderType()==OP_SELL) OrderClose(OrderTicket(),Lots,Ask,Slippage);
           }
        }
     }
  }
//+------------------------------------------------------------------+