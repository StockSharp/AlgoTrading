//+------------------------------------------------------------------+
//|                                                    Parazitos.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int coef=1;
extern   double Size=0.1;
int Bar_Count=0;
int ticket_buy;
int ticket_sell;
double StopLoss=50;
double TakeProfit=50;
double take_profit=50;
int Prev_Order=-1;
int Counter_buy=0;
int Counter_sell=0;
datetime Check_period=0;
datetime Checked_period=0;
int current_year=0;

double max_profit=0, max_loss=-50;
//1-???????, 0-???????

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
   //int handle=FileOpen("OrdersReport.csv",FILE_WRITE|FILE_CSV,"\t");
   //FileWrite(handle,"max_profit","max_loss");
   //FileWrite(handle,max_profit,max_loss);
   //FileClose(handle);

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
//int OrderSend(string symbol, 
//int cmd, double volume, 
//double price, int slippage, 
//double stoploss, double takeprofit, 
//string comment=NULL, int magic=0, 
//datetime expiration=0, color arrow_color=CLR_NONE)
int start()
  {
  int Insider, Insider2;
  int Black_insider, White_insider, White_bird, Black_bird;
  int Buy_signal=0;
  int Confirm_buy;
  int Confirm_sell;
  int Sell_signal=0;
  int Downer=0;
  int Upper=0;
  double Zig_level_buy;
  double Zig_level_sell;
  string i_n;

string loss_filename, win_filename, month, day, ticket_id;  

  double max_price;
  double min_price;
  int i=0;
  int k=0;

 
Checked_period=Time[0];

if (Checked_period==Check_period)
{
   if (Counter_sell+Counter_buy==2)
   {
   return(0);
   }
}
else
{
Check_period=Checked_period;
Counter_sell=0;
Counter_buy=0;
}
     if (High[1]<=High[2] && Low[1]>=Low[2])
       {Insider=1;}else {Insider=0;}
       
     if (High[2]<=High[3] && Low[2]>=Low[3])
       {Insider2=1;}else {Insider2=0;}
       
     if (High[1]<=High[2] && Low[1]>=Low[2] && Close[1]<=Open[1])
       {Black_insider=1;}else {Black_insider=0;}
       
     if (High[1]<=High[2] && Low[1]>=Low[2] && Close[1]>Open[1])
       {White_insider=1;}else {White_insider=0;}

     if (White_insider==1 && Close[2]>Open[2])
       {White_bird=1;}else {White_bird=0;}       
      
     if (Black_insider==1 && Close[2]<Open[2])
       {Black_bird=1;}else {Black_bird=0;}              
       

if (Open[1]<Close[1])
{Zig_level_buy=(Close[1]-(Close[1]-Open[1])/3);}//White
else
{Zig_level_buy=(Close[1]-(Close[1]-Low[1])/3);}//Black

if (Open[1]>Close[1])
{Zig_level_sell=(Close[1]+(Open[1]-Close[1])/3);}//Black
else
{Zig_level_sell=(Close[1]+(High[1]-Close[1])/3);}//White


if ( ((Low[0]<=Zig_level_buy) || (Hour()>=8)) 
&&(Black_bird==0) && (White_insider==0))
{Confirm_buy=1;}else{Confirm_buy=0;}

if ( ((High[0]>=Zig_level_sell) || (Hour()>=8))
&& (White_bird==0) && (Black_insider==0))
{Confirm_sell=1;}else{Confirm_sell=0;}


if (High[0]>High[1])
{Buy_signal=1;}else {Buy_signal=0;}

if (Buy_signal==1)
{
i++;
max_price=High[i];
while (max_price<High[i+1])
   {
      max_price=High[i+1];
      i++;
   }
}

  
if (Low[0]<Low[1])
{Sell_signal=1;}else {Sell_signal=0;}

if (Sell_signal==1)
{
k++;
min_price=Low[k];
while (min_price>Low[k+1])
   {
      min_price=Low[k+1];
      k++;
   }
}


//----

//BUY
if (Buy_signal==1)
{
//      if (max_price>=Bid)
      if (Confirm_buy==1)
      if (Low[0]>Low[1] && Low[1]<High[2])
      if(OrdersTotal()==0 && Counter_buy==0)
      {//Ask-100*Point
      ticket_buy=OrderSend(Symbol(),OP_BUY,Size*coef,Ask,5, 0, 0,"My order #",16384,0,Green);
      if (StringLen(DoubleToStr(ticket_buy,0))==1)
      ticket_id="0"+ticket_buy;
      else
      ticket_id=ticket_buy;
      //   Alert("Ticket="+ticket_id+"=", i);
         Prev_Order=1;
         Counter_buy++;
      }
}

if(Sell_signal==1)
{
   if(Confirm_sell==1)
   if (High[0]<High[1])
   if(OrdersTotal()!=0)
   if(OrderSelect(ticket_buy, SELECT_BY_TICKET)==true)
   if(OrderType()==OP_BUY)
   if(OrderCloseTime()==0)
   if(Confirm_sell==1)
   OrderClose(ticket_buy,OrderLots( ) ,Bid,5,Gray);
}


if (Sell_signal==1)
{
   if(Confirm_sell==1)
   if (High[0]<High[1])
   if(OrdersTotal()==0 && Counter_sell==0)
   {
      ticket_sell=OrderSend(Symbol(),OP_SELL,Size*coef,Bid,5, 0, 0,"My order #",16384,0,Red);
      Prev_Order=0;
      Counter_sell++;
   }
}

if(Buy_signal==1)
{
   if(Confirm_buy==1)
   if (Low[0]>Low[1] && Low[1]<High[2])
   if(OrdersTotal()!=0)
   if(OrderSelect(ticket_sell, SELECT_BY_TICKET)==true)
   if(OrderType()==OP_SELL)
   if(OrderCloseTime()==0)
   OrderClose(ticket_sell,OrderLots( ) ,Ask,5,Gray);
}


//----
   return(0);
  }
//+------------------------------------------------------------------+