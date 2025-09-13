//+------------------------------------------------------------------+
//|                                    Change TPSL By Percentage.mq4 |
//|                               Copyright 2023, Sathyam            |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, Sathyam"
#property link      "https://www.mql5.com/en/users/hacsat"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

//Works for Single symbol

//
extern string  TPPer = "<<< TakeProfit Percentage >>>";
extern double Percentage_profit=40;//Profit Percentage
extern string  SLPer = "<<< StopLoss Percentage >>>";
extern double Percentage_stoploss=90;//Stop Loss Percentage
extern string  Lev = "<<< Enter leverage value(1:200-->0.5, 1:100-->1, 1:400-->0.25) >>>";
extern double Symbolleaverage=0.5;//Enter leverage value(1:200-->0.5, 1:100-->1, 1:400-->0.25)
//

// Declare a function to set the profit target for open positions
void setProfitTarget()
  {

// Get current account balance
   double balance = AccountInfoDouble(ACCOUNT_BALANCE);

// Calculate profit target
   double profitTarget = balance * Percentage_profit/100;

// Calculate Stoploss target
   double stoptarget = balance * Percentage_stoploss/100;

//display Profit target right side

   ObjectCreate("Acc_Balance_Label",OBJ_LABEL,0,0,0);
   ObjectSetText("Acc_Balance_Label","Profit Target " +DoubleToStr(Percentage_profit,2) +"%:   " +DoubleToStr(profitTarget,2) +" $",10,"Times New Roman",White);
   ObjectSet("Acc_Balance_Label",OBJPROP_CORNER,3);
   ObjectSet("Acc_Balance_Label",OBJPROP_XDISTANCE,100);
   ObjectSet("Acc_Balance_Label",OBJPROP_YDISTANCE,20);

double profitmultiplier=0;
double stopmultiplier=0;
double average_price=0;

   if(AccountMargin() !=0)
     {
      //profit multiplier
      profitmultiplier = profitTarget/AccountMargin();
      //Use stoploss account close %
      stopmultiplier = stoptarget/AccountMargin();
     }


//Use leverage price %

   double percentage =1+((Symbolleaverage*profitmultiplier)/100);
   double percentage1 =1-((Symbolleaverage*profitmultiplier)/100);



   double stoppercentage1 =1+((Symbolleaverage*stopmultiplier)/100);
   double stoppercentage =1-((Symbolleaverage*stopmultiplier)/100);




//Average price
   double total_volume = 0;
   double total_value = 0;

   for(int t = OrdersTotal() - 1; t >= 0; t--)
     {
      if(OrderSelect(t, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol() == Symbol() && OrderType() <= OP_SELL)
           {
            total_volume += OrderLots();
            total_value += OrderLots() * OrderOpenPrice();
           }
        }
     }

   if(total_volume > 0)
     {
      average_price = total_value / total_volume;

     }

//Take profit calculation
   double takeProfitDistance =NormalizeDouble(average_price*percentage,3);
   double takeProfitDistance1 =NormalizeDouble(average_price*percentage1,3);

//stoploss calculation
   double stopdistance1 =NormalizeDouble(average_price*stoppercentage1,3);
   double stopdistance =NormalizeDouble(average_price*stoppercentage,3);


// Loop through all open positions
   for(int i = 0; i < OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {





         if(OrderType() == OP_BUY)
           {


            if(stopdistance<0)
              {
               stopdistance=0;
              }

            // Set take profit to required distance
            bool result = OrderModify(OrderTicket(), OrderOpenPrice(), stopdistance, takeProfitDistance, 0, Green);
            if(result)
              {
               Print("Profit target set for buy order ", OrderTicket());
              }
            else
              {
               //  Print("Failed to set profit target for buy order ", OrderTicket());
              }


           }
         //If sell
         if(OrderType() == OP_SELL)
           {


            if(takeProfitDistance1<0)
              {
               takeProfitDistance1=0;
              }

            // Set take profit to required distance
            bool result1 = OrderModify(OrderTicket(), OrderOpenPrice(), stopdistance1, takeProfitDistance1, 0, Green);


            if(result1)
              {
               Print("Profit target set for sell order ", OrderTicket());
              }
            else
              {
               // Print("Failed to set profit target for sell order ", OrderTicket());
              }
           }
        }
     }
  }

// Declare an event handler for the OnTick event
void OnTick()
  {

   setProfitTarget();
  }

//+------------------------------------------------------------------+
