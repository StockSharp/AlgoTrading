//+------------------------------------------------------------------+
//|                                         BalanceDrawdownInMT4.mq4 |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input bool ENABLE_MAGIC_NUMBER= true; // Enable Magic Number(For manual trades set false)
input int  MAGIC_NUMBER_INPUT =  20131111; // Magic Number
input double START_BALANCE =1000; // Start Balance
input double LOTS = 0.01;
input double STOPLOSS =300;
input double TAKEPROFIT =400;

int MAGIC_NUMBER;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(ENABLE_MAGIC_NUMBER)
      MAGIC_NUMBER = MAGIC_NUMBER_INPUT;
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
   if(OrdersTotal()==0){
     int res= OrderSend(Symbol(),OP_BUY,LOTS,Ask,3,Ask-STOPLOSS*Point(),Ask+STOPLOSS*Point(),"",MAGIC_NUMBER,0,clrGreen);
   }
   Comment("Current Drawdown "+DoubleToStr(DrawDown(Symbol()),2));
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|              CALCULATES THE DRAWDOWN                             |
//+------------------------------------------------------------------+
double DrawDown(string symbol)
  {
   double maxBalance=START_BALANCE;
   double currentBalance=START_BALANCE;
   // FINDING THE MAXIMUM BALANCE SO FAR AND THE TOTAL BALANCE
   for(int i =0; i <OrdersHistoryTotal(); i++)
     {
      // If the order cannot be selected, throw and log an error.
      if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)  && OrderSymbol()==symbol && ((ENABLE_MAGIC_NUMBER && OrderMagicNumber()==MAGIC_NUMBER) || !ENABLE_MAGIC_NUMBER))
        {
         currentBalance+= OrderProfit();
         if(currentBalance>maxBalance)
            maxBalance=currentBalance;
        }
     }
   double currentProfit =TotalCurrentProfit(symbol);
   currentBalance+= currentProfit;
   double drawdown= ((maxBalance-currentBalance)*100)/(maxBalance);
   return drawdown;
  }

//+------------------------------------------------------------------+
//|                   GET CURRENT TOTAL PROFIT OF A SYMBOL                   |
//+------------------------------------------------------------------+
double TotalCurrentProfit(string symbol)
  {
   double profit=0;
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)&& ((ENABLE_MAGIC_NUMBER && OrderMagicNumber()==MAGIC_NUMBER) || !ENABLE_MAGIC_NUMBER) && OrderSymbol()==symbol)
         profit+=OrderProfit();

     }
   return profit;
  }
//+------------------------------------------------------------------+
