//+-------------------------------------------------------------------+
//|           MMA_Breakout_strategy_Volume I - coded by WhooDoo22.mq4 |
//|                        Copyright 2012, MetaQuotes Software Corp.  |
//|                                        http://www.metaquotes.net  |
//+-------------------------------------------------------------------+
#property copyright "Copyright 2012, WhooDoo22."
#property link      "http://www.mql4.com/users/WhooDoo22"

extern string CodedByWhooDoo22.="";
extern string ThanksToMQL4Comunity.="";


// run strategy on eurusd currency pair using m30 timeframe from dates 2003.01.01-2012.06.01.
// in the strategy tester, use Open prices only (fastest method to analyze the bar just completed, only for EAs that explicitly control bar opening.

// advice for improving strategy is to create a icustom moving average or 
//    use a custom moving average that will look sixty or so bars back in history and write
//    a moving average that will spear throught the ranges, essentially creating a probable 
//    center of support and resistance. Remember to maximize profit and minimize risk. Thank you.  

int MaxOrders = 1; 
int TradePerBar = 0; 
int BarCount = -1; 
int MaxTradePerBar = 1; 
int BarsCount = 0;

bool MaxTradePerBarUSE = true;

int ticket;
int TakeProfit;
bool BuyTicket_12345_InUse1;
bool BuyTicket_12345_InUse2;
bool BuyTicket_12345_InUse3;
bool BuyTicket_12345_InUse4;

bool SellTicket_77777_InUse1;
bool SellTicket_77777_InUse2;
bool SellTicket_77777_InUse3;
bool SellTicket_77777_InUse4;

bool win;

int i;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
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

//***************************
// BUY ORDER OPEN SECTION 1 ;
//***************************

   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(OrdersHistoryTotal()==0)                                                                 {
   if(OrdersTotal()<MaxOrders)                                                                 {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,2)<(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,2)))     { 
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)>(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,1)))     {
   if(Bars>BarsCount)                                                                          {
   if(MaxTradePerBarUSE)                                                                       { 
     {
      ticket=OrderSend(Symbol(),OP_BUY,0.04,Ask,30,Ask-5000*Point,Ask+99999*Point,"",12345,0,Blue);    
      BarsCount=Bars;
      BuyTicket_12345_InUse1=true;
      BuyTicket_12345_InUse2=true;
      BuyTicket_12345_InUse3=true;
      BuyTicket_12345_InUse4=true;
      i++;
     }
     }}}}}}
     
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(OrdersHistoryTotal()>0)                                                                  {
   if(OrdersTotal()<MaxOrders)                                                                 {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,2)<(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,2)))     { 
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)>(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,1)))     {
   if(Bars>BarsCount)                                                                          {
   if(MaxTradePerBarUSE)                                                                       { 
     {
      ticket=OrderSend(Symbol(),OP_BUY,0.04,Ask,30,Ask-5000*Point,Ask+99999*Point,"",12345,0,Blue);    
      BarsCount=Bars;
      BuyTicket_12345_InUse1=true;
      BuyTicket_12345_InUse2=true;
      BuyTicket_12345_InUse3=true;
      BuyTicket_12345_InUse4=true;
      i++;
     }
     }}}}}}
     
//****************************
// BUY ORDER CLOSE SECTION 1 ;
//****************************          
    
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse1==true)                                                            {
   if(OrderLots()==0.04)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)<(iMA(NULL,0,5,0,MODE_EMA,PRICE_CLOSE,0)))        { 
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE);
      BuyTicket_12345_InUse1=false;
      i++;
     }
     }}}}}     

   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse2==true)                                                            {
   if(OrderLots()==0.03)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)<(iMA(NULL,0,25,0,MODE_SMMA,PRICE_CLOSE,0)))      { 
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE); 
      BuyTicket_12345_InUse2=false;
      i++;
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse3==true)                                                            {
   if(OrderLots()==0.02)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)<(iMA(NULL,0,50,0,MODE_SMMA,PRICE_CLOSE,0)))      { 
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE); 
      BuyTicket_12345_InUse3=false;
      i++;
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse4==true)                                                            {
   if(OrderLots()==0.01)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)<(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,0)))     { 
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE); 
      BuyTicket_12345_InUse4=false;
     }
     }}}}}     
          
//****************************
// BUY ORDER CLOSE SECTION 2 ;
//****************************      
     
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse1 == true)                                                          {
   if(OrderLots()==0.04)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)<(iMA(NULL,0,5,0,MODE_EMA,PRICE_CLOSE,1)))        { 
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE);
      BuyTicket_12345_InUse1=false;
      i++;
     }
     }}}}}     

   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse2==true)                                                            {
   if(OrderLots()==0.03)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)<(iMA(NULL,0,25,0,MODE_SMMA,PRICE_CLOSE,1)))      {  
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE); 
      BuyTicket_12345_InUse2=false;
      i++;
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse3==true)                                                            {
   if(OrderLots()==0.02)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)<(iMA(NULL,0,50,0,MODE_SMMA,PRICE_CLOSE,1)))      {  
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE); 
      BuyTicket_12345_InUse3=false;
      i++;
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(BuyTicket_12345_InUse4==true)                                                            {
   if(OrderLots()==0.01)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)<(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,1)))     { 
     {
      OrderClose(i,0.01,Ask,30,CLR_NONE); 
      BuyTicket_12345_InUse4=false;
      
      ticket=OrderSend(Symbol(),OP_SELL,0.04,Bid,30,Bid+5000*Point,Bid-99999*Point,"",77777,0,Red);
      BarsCount=Bars;
      SellTicket_77777_InUse1=true;
      SellTicket_77777_InUse2=true;
      SellTicket_77777_InUse3=true;
      SellTicket_77777_InUse4=true;
      i++;
     }
     }}}}}            
    
//****************************
// SELL ORDER OPEN SECTION 1 ;
//****************************

   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(OrdersHistoryTotal()==0)                                                                 {
   if(OrdersTotal()<MaxOrders)                                                                 {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,2)>(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,2)))     {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)<(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,1)))     {  
   if(Bars>BarsCount)                                                                          {
   if(MaxTradePerBarUSE)                                                                       { 
     {
      ticket=OrderSend(Symbol(),OP_SELL,0.04,Bid,30,Bid+5000*Point,Bid-99999*Point,"",77777,0,Red);
      BarsCount=Bars;
      SellTicket_77777_InUse1=true;
      SellTicket_77777_InUse2=true;
      SellTicket_77777_InUse3=true;
      SellTicket_77777_InUse4=true;
      i++;
     }
     }}}}}}
     
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(OrdersHistoryTotal()>0)                                                                  {
   if(OrdersTotal()<1)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,2)>(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,2)))     {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)<(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,1)))     { 
   if(Bars>BarsCount)                                                                          {
   if(MaxTradePerBarUSE)                                                                       {   
     {
      ticket=OrderSend(Symbol(),OP_SELL,0.04,Bid,30,Bid+5000*Point,Bid-99999*Point,"",77777,0,Red);    
      BarsCount=Bars;
      SellTicket_77777_InUse1=true;
      SellTicket_77777_InUse2=true;
      SellTicket_77777_InUse3=true;
      SellTicket_77777_InUse4=true;
      i++;
     }
     }}}}}}     

//*****************************
// SELL ORDER CLOSE SECTION 1 ;
//***************************** 
    
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse1==true)                                                           {
   if(OrderLots()==0.04)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)>(iMA(NULL,0,5,0,MODE_EMA,PRICE_CLOSE,0)))        { 
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse1=false;
      i++;
     }
     }}}}}     

   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse2==true)                                                           {
   if(OrderLots()==0.03)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)>(iMA(NULL,0,25,0,MODE_SMMA,PRICE_CLOSE,0)))      {  
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse2=false;
      i++; 
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse3==true)                                                           {
   if(OrderLots()==0.02)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)>(iMA(NULL,0,50,0,MODE_SMMA,PRICE_CLOSE,0)))      {  
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse3=false;
      i++; 
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse4==true)                                                           {
   if(OrderLots()==0.01)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()>=0)                                                                        {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,0)>(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,0)))     { 
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse4=false; 
     }
     }}}}}          
     
//*****************************
// SELL ORDER CLOSE SECTION 2 ;
//*****************************     

   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse1==true)                                                           {
   if(OrderLots()==0.04)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)>(iMA(NULL,0,5,0,MODE_EMA,PRICE_CLOSE,1)))        { 
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse1=false;
      i++;
     }
     }}}}}     

   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse2==true)                                                           {
   if(OrderLots()==0.03)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)>(iMA(NULL,0,25,0,MODE_SMMA,PRICE_CLOSE,1)))      {  
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse2 = false;
      i++;
     }
     }}}}}

   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse3==true)                                                           {
   if(OrderLots()==0.02)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)>(iMA(NULL,0,50,0,MODE_SMMA,PRICE_CLOSE,1)))      { 
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse3 = false;
      i++;
     }
     }}}}}
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(SellTicket_77777_InUse4==true)                                                           {
   if(OrderLots()==0.01)                                                                       {
   if(OrdersTotal()>0)                                                                         {
   if(OrderProfit()<0)                                                                         {
   if(iMA(NULL,0,1,0,MODE_SMMA,PRICE_CLOSE,1)>(iMA(NULL,0,200,0,MODE_SMMA,PRICE_CLOSE,1)))     { 
     {
      OrderClose(i,0.01,Bid,30,CLR_NONE);
      SellTicket_77777_InUse4 = false; 
      
      ticket=OrderSend(Symbol(),OP_BUY,0.04,Ask,30,Ask-5000*Point,Ask+99999*Point,"",12345,0,Blue);    
      BarsCount=Bars;
      BuyTicket_12345_InUse1=true;
      BuyTicket_12345_InUse2=true;
      BuyTicket_12345_InUse3=true;
      BuyTicket_12345_InUse4=true; 
      i++;
     }
     }}}}}     






     
     
     
     
 



     
     
     
     
     
     
   OrderSelect(i,SELECT_BY_TICKET,MODE_TRADES);
   if(OrderLots()==0.01){
    
   Comment("/n OrderTicket() = " ,i, 
           "/n OrderProfit() = ",OrderProfit());
          }  
 
   
     
     
     
     
     
     
     
     




   return(0);
  }
//+------------------------------------------------------------------+