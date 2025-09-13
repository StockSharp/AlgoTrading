//+------------------------------------------------------------------+
//|                                         stochastic-automated.mq4 |
//|                                  Copyright 2016, Mohammad Soubra |
//|                         https://www.mql5.com/en/users/soubra2003 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Mohammad Soubra"
#property link      "https://www.mql5.com/en/job/new?prefered=soubra2003"
#property version   "1.00"
#property strict

input int            MagicNumber  = 1982;     //Magic Number
input double         Lots         = 0.01;     //Fixed Lots
input double         StopLoss     = 100;      //Fixed Stop Loss (in Points)
input double         TakeProfit   = 150;      //Fixed Take Profit (in Points)
input int            TrailingStop = 30;       //Trailing Stop (in Points)
input int            k_period     = 5;        //Stochastic K Period
input int            d_period     = 3;        //Stochastic D Period
input int            slowing      = 3;        //Stochastic Slowing
input ENUM_MA_METHOD ma_method    = MODE_SMA; //Stochastic Moving Average Type
input int            price_field  = 0;        //Price field parameter. 0=Low/High or 1=Close/Close
input double         over_bought  = 80;       //Stochastic Over-Bought
input double         over_sold    = 20;       //Stochastic Over-Sold
//---
int    Slippage=5;
double sto_main_curr,sto_sign_curr,sto_main_prev1,sto_sign_prev1,sto_main_prev2,sto_sign_prev2;
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Print("Thank you for  using this example. Take Care!");
  }
//+------------------------------------------------------------------+
//|   expert OnTick function                                         |
//+------------------------------------------------------------------+
void OnTick()
  {
   double TheStopLoss=0;
   double TheTakeProfit=0;
   double MyPoint=Point;

   if(Digits==3 || Digits==5) MyPoint=Point*10;

   sto_main_curr  = iStochastic (Symbol() ,PERIOD_CURRENT ,k_period ,d_period ,slowing ,ma_method ,price_field ,MODE_MAIN   ,0);
   sto_sign_curr  = iStochastic (Symbol() ,PERIOD_CURRENT ,k_period ,d_period ,slowing ,ma_method ,price_field ,MODE_SIGNAL ,0);
   sto_main_prev1 = iStochastic (Symbol() ,PERIOD_CURRENT ,k_period ,d_period ,slowing ,ma_method ,price_field ,MODE_MAIN   ,1);
   sto_sign_prev1 = iStochastic (Symbol() ,PERIOD_CURRENT ,k_period ,d_period ,slowing ,ma_method ,price_field ,MODE_SIGNAL ,1);
   sto_main_prev2 = iStochastic (Symbol() ,PERIOD_CURRENT ,k_period ,d_period ,slowing ,ma_method ,price_field ,MODE_MAIN   ,2);
   sto_sign_prev2 = iStochastic (Symbol() ,PERIOD_CURRENT ,k_period ,d_period ,slowing ,ma_method ,price_field ,MODE_SIGNAL ,2);

   if(TotalOrdersCount()==0)
     {
      int result=0;
      //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~BUY~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
      if((sto_sign_prev2<over_sold) && (sto_main_prev2<over_sold))
        {
         if((sto_sign_prev2>sto_main_prev2) && (sto_sign_prev1<sto_main_prev1))
           {
            if(sto_sign_prev1<sto_sign_curr)
              {
               result=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"EXAMPLE OF STOCHASTIC AUTOMATED",MagicNumber,0,Blue);

               if(result>0)
                 {
                  TheStopLoss=0;
                  TheTakeProfit=0;
                  if(TakeProfit > 0) TheTakeProfit = Ask + TakeProfit * MyPoint;
                  if(StopLoss > 0)   TheStopLoss   = Ask - StopLoss * MyPoint;
                  int MyOrderSelect = OrderSelect(result,SELECT_BY_TICKET);
                  int MyOrderModify = OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),
                                                  NormalizeDouble(TheTakeProfit,Digits),0,Green);
                 }
              }
           }
        }
      //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~SELL~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
      if((sto_sign_prev2>over_bought) && (sto_main_prev2>over_bought))
        {
         if((sto_sign_prev2<sto_main_prev2) && (sto_sign_prev1>sto_main_prev1))
           {
            if(sto_sign_prev1>sto_sign_curr)
              {

               result=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"EXAMPLE OF STOCHASTIC AUTOMATED",MagicNumber,0,Red);

               if(result>0)
                 {
                  TheStopLoss=0;
                  TheTakeProfit=0;
                  if(TakeProfit > 0) TheTakeProfit = Bid - TakeProfit * MyPoint;
                  if(StopLoss > 0)   TheStopLoss   = Bid + StopLoss * MyPoint;
                  int MyOrderSelect = OrderSelect(result,SELECT_BY_TICKET);
                  int MyOrderModify = OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),
                                                  NormalizeDouble(TheTakeProfit,Digits),0,Green);
                 }

              }
           }
        }
     }

   for(int cnt=0; cnt<OrdersTotal(); cnt++)
     {
      int MyOrderSelect=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(OrderType()==OP_BUY)
           {

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~CLOSING BUY~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if((sto_main_curr>over_bought) || (sto_sign_curr<sto_sign_prev1)) //here is the close buy condition
              {
               int MyOrderClose=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,Red);
              }

            //------------------------------+
            // TRAILING STOP CODE HERE:     +
            //------------------------------+
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>MyPoint*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-MyPoint*TrailingStop)
                    {
                     int MyOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),
                                                   OrderStopLoss()+TrailingStop*MyPoint,OrderTakeProfit(),0,Green);
                    }
                 }
              }
           }
         else
           {

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~CLOSING SELL~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if((sto_main_curr<over_sold) || (sto_sign_curr>sto_sign_prev1)) // here is the close sell condition
              {
               int MyOrderClose=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,Red);
              }

            //------------------------------+
            // TRAILING STOP CODE HERE:     +
            //------------------------------+
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(MyPoint*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+MyPoint*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     int MyOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),
                                                   OrderStopLoss()-MyPoint*TrailingStop,OrderTakeProfit(),0,Red);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|   expert TotalOrdersCount function                               |
//+------------------------------------------------------------------+
int TotalOrdersCount()
  {
   int result=0;

   for(int i=0; i<OrdersTotal(); i++)
     {
      int MyOrderSelect=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber) result++;
     }

//---
   return (result);
  }
//-------------------------------------------------------------------+
//Bye
