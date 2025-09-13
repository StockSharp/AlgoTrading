//+------------------------------------------------------------------+
//|                                            Petes Party trick.mq4 |
//|                                                     Peter  Byrom |
//|                                                  pete@byroms.net |
//+------------------------------------------------------------------+
#property copyright "Peter  Byrom"
#property link      "pete@byroms.net"

//---- input parameters
int ticketer;
 int magic;//The magic number   
 int ordertype;
extern int cciselllevel = 101;
extern int ccibuylevel = 99;
extern int cci = 14;
extern   int   donchian_length_buy =  100;
 extern int max_trades = 1;
extern double    big_win=15.0;
extern double    win_increase = 0.01;
extern double desired_gap = 0.005;
extern double    WPCR=50.0;
extern int whigh = 80;
extern  int wlow  = 20;
int counter = 1;
extern int fast = 40;
extern int slow = 80;
extern int slippage = 10;
extern int stoploss = 50;
extern int takeprofit = 50;
extern double tradesize = 0.01;
extern int recogniser = 444544;
string pair;
int ticket;
double actual_gap;
bool sell;
bool buy;
//+--------------------int ticket;----------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   whigh= 0-whigh; wlow = 0-wlow;Print(whigh);
  bool t = ObjectCreate("charles",OBJ_LABEL,0,TimeCurrent(),Bid,TimeCurrent()+30,Ask);Print(t);
   ObjectSet("charles",OBJPROP_CORNER,1);
   ObjectSet("charles",OBJPROP_XDISTANCE,15);
   ObjectSet("charles",OBJPROP_YDISTANCE,15);
   ObjectSetText("charles","Market Closed",12,"Ariel",Orange);

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----







  double CCI = iCCI(Symbol(),0,cci,PRICE_CLOSE,0);
   double  ema_fast=iMA(Symbol(),PERIOD_H1,fast,0,MODE_EMA,PRICE_CLOSE,0);
double  ema_slow=iMA(Symbol(),PERIOD_H1,slow,0,MODE_EMA,PRICE_CLOSE,0);
   double will = iWPR(Symbol(),PERIOD_M1,WPCR,0);
 
   double will_lag = iWPR(Symbol(),PERIOD_M1,WPCR,1);
   
   if (will == 0){will = 0-1;}if (will_lag == 0){will_lag = 0-1; }
  string name =  StringConcatenate("Marker ",counter);
  
  
  
  
  //Buying AND Selling ***************************************************************
  
      if(ema_fast<ema_slow)
         {
         // Stop Buying and satrt selling*********************
         // Step 1 Close all buy Orders *********************
         int buyi;
         int allbuy;//Total Number of orders
         int styepbuy;// string to match orders with pair
         string activeorderpairbuy;   activeorderpairbuy=OrderSymbol();//The symbol being traded
            
         allbuy=OrdersTotal();
          /********************************************************************************
         I want a list of ttcket numbers that match the symbol and the magic number *
         *********************************************************************************
         */
         for (buyi=0;buyi<allbuy;buyi++)
         {
         OrderSelect(buyi,SELECT_BY_POS,MODE_TRADES);
         double  sizebuy = OrderLots();
         ordertype = OrderType();
         ticketer=OrderTicket();
         styepbuy=StringFind(pair,activeorderpairbuy);
         if(ordertype == OP_BUY && styepbuy==0 && recogniser == OrderMagicNumber()) {OrderClose(ticketer,sizebuy,Ask,slippage,Green);
         if(OrderProfit() > big_win) { tradesize = tradesize + win_increase; big_win = big_win * 2;}
         }}
         // Step 2 set booleans buy = false sell = true******
         sell = True;
         buy = False;
         ObjectSetText("charles","Selling",12,"Ariel",Crimson);
         }
       
  
        if(ema_fast>ema_slow)
         {
         // Stop selling and satrt buying*********************
         // Step 1 Close all sell Orders *********************
         int selli;
         int allsell;//Total Number of orders
         int styesell;// string to match orders with pair
         string activeorderpairsell;   activeorderpairsell=OrderSymbol();//The symbol being traded  
         allsell=OrdersTotal();
          /********************************************************************************
         I want a list of ttcket numbers that match the symbol and the magic number *
         *********************************************************************************
         */
         for (selli=0;selli<allsell;selli++)
         {
         OrderSelect(selli,SELECT_BY_POS,MODE_TRADES);
         double  sizesell = OrderLots();
         ordertype = OrderType();
         ticketer=OrderTicket();
         styesell=StringFind(pair,activeorderpairsell);
         if(ordertype == OP_SELL && styesell==0 && recogniser == OrderMagicNumber()) {OrderClose(ticketer,sizesell,Ask,slippage,Green);
         if(OrderProfit() > big_win) { tradesize = tradesize + win_increase; big_win = big_win * 2;}
         }}
         // Step 2 set booleans buy = false sell = true******
         sell = False;
         buy = True; 
         ObjectSetText("charles","Buying",12,"Ariel",SteelBlue);
         }
  
 
 // SELL*************************************************************************************** 
  
   if (will < -20 && will_lag > -20  && will_lag <0 && max_trades > OrdersTotal()&& sell==True && CCI >cciselllevel)
   
      {
     
    ticket=OrderSend(Symbol(),OP_SELL,tradesize,Bid,slippage,Bid+stoploss*Point,Bid-takeprofit*Point,"My order number",recogniser,0,Crimson);
         OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
     pair=OrderSymbol();
      if(ticket<0)
       {
        Print("OrderSend failed with error #",GetLastError());
        return(0);
      }
      }
      
      
 //   BUY********************************************************************************************     
        
   if (will > -80 && will_lag < -80  && will_lag <0 && max_trades > OrdersTotal()&& buy==True && CCI < ccibuylevel)
   
      {
     
    ticket=OrderSend(Symbol(),OP_BUY,tradesize,Ask,slippage,Ask-stoploss*Point,Ask+takeprofit*Point,"My order number",recogniser,0,Crimson);
         OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
     pair=OrderSymbol();
      if(ticket<0)
       {
        Print("OrderSend failed with error #",GetLastError());
        return(0);
      }
      }
      
      
      
      
      
      
      
      
      
      
  // CLOSE**********************************************************************    
      
      //Close sells when dochian is high and close buys when donchian is low
    
  //CLOSE ALL SELLS***********************************************************************   
   double escape = High[iHighest(Symbol(),0,MODE_HIGH,donchian_length_buy,1)];
   if(Ask >escape){
   // find the sells
           activeorderpairsell=OrderSymbol();//The symbol being traded  
         allsell=OrdersTotal();
          /********************************************************************************
         I want a list of ttcket numbers that match the symbol and the magic number *
         *********************************************************************************
         */
         for (selli=0;selli<allsell;selli++)
         {
         OrderSelect(selli,SELECT_BY_POS,MODE_TRADES);
        sizesell = OrderLots();
         ordertype = OrderType();
         ticketer=OrderTicket();
         styesell=StringFind(pair,activeorderpairsell);
         if(ordertype == OP_SELL && styesell==0 && recogniser == OrderMagicNumber()) {OrderClose(ticketer,sizesell,Ask,slippage,Green);
         if(OrderProfit() > big_win) { tradesize = tradesize + win_increase; big_win = big_win * 2;}
         }
   }
   
 }
   
     //CLOSE ALL Bus***********************************************************************   
   double oucha = Low[iLowest(Symbol(),0,MODE_LOW,donchian_length_buy,1)];
   
   if(Bid<oucha)
   {
   
   
    activeorderpairbuy=OrderSymbol();//The symbol being traded
            
         allbuy=OrdersTotal();
          /********************************************************************************
         I want a list of ttcket numbers that match the symbol and the magic number *
         *********************************************************************************
         */
         for (buyi=0;buyi<allbuy;buyi++)
         {
         OrderSelect(buyi,SELECT_BY_POS,MODE_TRADES);
       sizebuy = OrderLots();
         ordertype = OrderType();
         ticketer=OrderTicket();
         styepbuy=StringFind(pair,activeorderpairbuy);
         if(ordertype == OP_BUY && styepbuy==0 && recogniser == OrderMagicNumber()) {OrderClose(ticketer,sizebuy,Ask,slippage,Green);
         if(OrderProfit() > big_win) { tradesize = tradesize + win_increase; big_win = big_win * 2;}
         }}
         
        
   
   
   
   
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+