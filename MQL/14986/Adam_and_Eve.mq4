//+------------------------------------------------------------------+
//|                                                 Adam and Eve.mq4 |
//|                                        Copyright 2016, onesoubra |
//|                                       https://www.onesoubra.com/ |
//+------------------------------------------------------------------+

#property copyright   "Copyright 2016, onesoubra"
#property link        "https://www.mql5.com/en/job/new?prefered=soubra2003"
#property version     "1.00"
#property description "Skype: onesoubra"
#property description "https://www.onesoubra.com/"
#property description " "
#property description "- Do not use only one currency pair"
#property strict


extern double calculated_amount = 1000;     //Amount for AUTO Lot
extern double calculated_lot = 0.01;        //Auto Lot Size each Amount
extern double protected_free_margin = 90.9; //Protected % of Free Margin
extern int    magic = 1982;                 //EA ID Number (Magic Number)

   string trade_comment;
   int    slippage = 5;
   
   double auto_lot;
///double atr14_2;
   double atr14_1;
///double atr14_0;
   double haLowHigh_1;
///double haHighLow_1;
   double haOpen_1;
   double haClose_1;
   double sma20_2, sma20_1, sma20_0,
          sma14_2, sma14_1, sma14_0,
          sma12_2, sma12_1, sma12_0,
          sma10_2, sma10_1, sma10_0,
          sma9_2, sma9_1, sma9_0,
          sma7_2, sma7_1, sma7_0,
          sma5_2, sma5_1, sma5_0;
   int    ticketBid;
   int    ticketAsk;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{   
   trade_comment = "FROM TIME FRAME: "+IntegerToString(Period())+" Min";

//---
   return(INIT_SUCCEEDED);
}//END OnInit()


//+------------------------------------------------------------------+
//| Expert OnTick function                                           |
//+------------------------------------------------------------------+
void OnTick()
{
   auto_lot = AccountBalance() / calculated_amount * calculated_lot;
   
 //atr14_2 = iATR(Symbol(),PERIOD_CURRENT,14,2);
   atr14_1 = iATR(Symbol(),PERIOD_CURRENT,14,1);
 //atr14_0 = iATR(Symbol(),PERIOD_CURRENT,14,0);
   
     haLowHigh_1 = iCustom(Symbol(),PERIOD_CURRENT,"Heiken Ashi",Red,White,Red,White,0,1);
   //haHighLow_1 = iCustom(Symbol(),PERIOD_CURRENT,"Heiken Ashi",Red,White,Red,White,1,1);
     haOpen_1    = iCustom(Symbol(),PERIOD_CURRENT,"Heiken Ashi",Red,White,Red,White,2,1);
     haClose_1   = iCustom(Symbol(),PERIOD_CURRENT,"Heiken Ashi",Red,White,Red,White,3,1);

   sma20_2 = iMA(Symbol(),PERIOD_CURRENT,20,0,MODE_SMA,PRICE_CLOSE,2);
   sma20_1 = iMA(Symbol(),PERIOD_CURRENT,20,0,MODE_SMA,PRICE_CLOSE,1);
   sma20_0 = iMA(Symbol(),PERIOD_CURRENT,20,0,MODE_SMA,PRICE_CLOSE,0);
   sma14_2 = iMA(Symbol(),PERIOD_CURRENT,14,0,MODE_SMA,PRICE_CLOSE,2);
   sma14_1 = iMA(Symbol(),PERIOD_CURRENT,14,0,MODE_SMA,PRICE_CLOSE,1);
   sma14_0 = iMA(Symbol(),PERIOD_CURRENT,14,0,MODE_SMA,PRICE_CLOSE,0);
   sma12_2 = iMA(Symbol(),PERIOD_CURRENT,12,0,MODE_SMA,PRICE_CLOSE,2);
   sma12_1 = iMA(Symbol(),PERIOD_CURRENT,12,0,MODE_SMA,PRICE_CLOSE,1);
   sma12_0 = iMA(Symbol(),PERIOD_CURRENT,12,0,MODE_SMA,PRICE_CLOSE,0);
   sma10_2 = iMA(Symbol(),PERIOD_CURRENT,10,0,MODE_SMA,PRICE_CLOSE,2);
   sma10_1 = iMA(Symbol(),PERIOD_CURRENT,10,0,MODE_SMA,PRICE_CLOSE,1);
   sma10_0 = iMA(Symbol(),PERIOD_CURRENT,10,0,MODE_SMA,PRICE_CLOSE,0);
   sma9_2  = iMA(Symbol(),PERIOD_CURRENT,9,0,MODE_SMA,PRICE_CLOSE,2);
   sma9_1  = iMA(Symbol(),PERIOD_CURRENT,9,0,MODE_SMA,PRICE_CLOSE,1);
   sma9_0  = iMA(Symbol(),PERIOD_CURRENT,9,0,MODE_SMA,PRICE_CLOSE,0);
   sma7_2  = iMA(Symbol(),PERIOD_CURRENT,7,0,MODE_SMA,PRICE_CLOSE,2);
   sma7_1  = iMA(Symbol(),PERIOD_CURRENT,7,0,MODE_SMA,PRICE_CLOSE,1);
   sma7_0  = iMA(Symbol(),PERIOD_CURRENT,7,0,MODE_SMA,PRICE_CLOSE,0);
   sma5_2  = iMA(Symbol(),PERIOD_CURRENT,5,0,MODE_SMA,PRICE_CLOSE,2);
   sma5_1  = iMA(Symbol(),PERIOD_CURRENT,5,0,MODE_SMA,PRICE_CLOSE,1);
   sma5_0  = iMA(Symbol(),PERIOD_CURRENT,5,0,MODE_SMA,PRICE_CLOSE,0);

//---
   if (AccountFreeMargin() >= (AccountBalance() * (protected_free_margin/100) ))
   {

      //TO SELL
      if (haClose_1 < haOpen_1)
      {
      if (haOpen_1 == haLowHigh_1)
      {
         if (sma5_0 < sma5_1)
         {
         if (sma5_1 < sma5_2)
         {
            if (sma7_0 < sma7_1)
            {
            if (sma7_1 < sma7_2)
            {
               if (sma9_0 < sma9_1)
               {
               if (sma9_1 < sma9_2)
               {
               if (sma10_0 < sma10_1)
               {
               if (sma10_1 < sma10_2)
               {
            if (sma12_0 < sma12_1)
            {
            if (sma12_1 < sma12_2)
            {
         if (sma14_0 < sma14_1)
         {
         if (sma14_1 < sma14_2)
         {
      if (sma20_0 < sma20_1)
      {
      if (sma20_1 < sma20_2)
      {
      
      sell();
      
      }}}}}}}}}}}}}}}}//END SELL CONDITIONS

      //TO BUY
      if (haClose_1 > haOpen_1) 
      {
      if (haOpen_1 == haLowHigh_1)
      {
         if (sma5_0 > sma5_1)
         {
         if (sma5_1 > sma5_2)
         {
            if (sma7_0 > sma7_1)
            {
            if (sma7_1 > sma7_2)
            {
               if (sma9_0 > sma9_1)
               {
               if (sma9_1 > sma9_2)
               {
               if (sma10_0 > sma10_1)
               {
               if (sma10_1 > sma10_2)
               {
            if (sma12_0 > sma12_1)
            {
            if (sma12_1 > sma12_2)
            {
         if (sma14_0 > sma14_1)
         {
         if (sma14_1 > sma14_2)
         {
      if (sma20_0 > sma20_1)
      {
      if (sma20_1 > sma20_2)
      {
      
      buy();
      
      }}}}}}}}}}}}}}}}//END BUY CONDITIONS
      
   }
   
//---
   Comment("    TOTAL OF OPENED POSITIONS = ",IntegerToString(OrdersTotal()),
         "\n    BROKER STOP OUT = ",AccountStopoutLevel());
}//END OnTick()


//+------------------------------------------------------------------+
//| Expert sell function                                             |
//+------------------------------------------------------------------+
void sell()
{   
   ticketBid =
            OrderSend(Symbol(),OP_SELL,auto_lot,Bid,slippage,0,Ask-atr14_1,trade_comment,magic,0,clrNONE);
}//END sell()


//+------------------------------------------------------------------+
//| Expert buy function                                              |
//+------------------------------------------------------------------+
void buy()
{   
   ticketAsk =
            OrderSend(Symbol(),OP_BUY,auto_lot,Ask,slippage,0,Bid+atr14_1,trade_comment,magic,0,clrNONE);
}//END buy()



//+------------------------------------------------------------------+
//END   