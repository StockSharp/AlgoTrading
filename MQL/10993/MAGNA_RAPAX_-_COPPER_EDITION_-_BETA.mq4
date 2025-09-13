//-------------------------------------------------------------------------//                                                                 
//                                                                         //
//  _ __ ___   __ _  __ _ _ __   __ _   _ __ __ _ _ __   __ ___  __        // 
// | '_ ` _ \ / _` |/ _` | '_ \ / _` | | '__/ _` | '_ \ / _` \ \/ /        //
// | | | | | | (_| | (_| | | | | (_| | | | | (_| | |_) | (_| |>  <         //
// |_| |_| |_|\__,_|\__, |_| |_|\__,_| |_|  \__,_| .__/ \__,_/_/\_\        //
//                   __/ |                       | |                       //
//                  |___/                        |_|                       //
//                             COPPER EDITION                              //
//----------------AUTO TRADER ROBOT FOR METATRADER 4-----------------------//
//----------------------------BY PROXIMUS © 2013---------------------------//

//The author of this EA does not asume any responsibility for material or any other types 
//of losses occuring by using or during the usage of this EA! So use this EA at your own risk!

#property copyright "PROXIMUS © 2013"
#property link      ""
extern double start_lot=0.01; //order lot size
extern int TAKEPROFITPIPS=99; /// take profit in pips
extern int STOPLOSSPIPS=99;  // stop loss in pips
extern int MAX_ACCOUNT_RISK=25; //in percent,closes all losing trades if AccountEquity goes below AccountBalance by this amount
extern string  X________________1="------MARTINGALE------";
extern bool MARTINGALE_ACTIVATOR=false;   /// wanna use martingale system?
extern int MARTINGALE_LEVELS=5; // what levels to go,ex level 5= 2^5 multiplicator for lot
extern bool MARTINGALE_PROGRESSION=false; //false= doubles only losing trades, true=doubles all trades
extern string  X________________2="------ORDER SETTINGS------";
extern int TOTAL_BUY_ORDERS=1;      ///how many buy orders to place,maximum
extern int TOTAL_SELL_ORDERS=1;       //how many sell orders to place,maximum
extern int MAX_SLIPPAGE=3;  //for placing orders only
extern string  X________________3="------MACD SETTINGS------";
extern int FAST_MACD_P=5;
extern int SLOW_MACD_P=35;
extern int SIGNAL_PERIOD=5;
extern string  X________________4="------TREND FILTER SETTINGS------";
//extern int ATR_SLOW_PERIOD=24;
//extern int ATR_QUICK_PERIOD=5;
extern int ADX_PERIOD=14;
extern double ADX_LIMIT_TRIGGER=50;

/////////////////////////OPTIMALIZED////////////----------------------------------<<<<<

///////////////////////////////////-----------------------END EXTERNAL VARIABLES----------------//////////////////
////////////////////////////----------------INIT VARIABLES-------------////////////////////////////////////////////
int ticket=0;
int i=1;
string via="X";
string viaID="X";
int LEVEL=1;
int magic=666;   ///magic number
double martingale_lot=0.01;     
///correction
int PositionIndex; 
int TotalNumberOfOrders;
int slippagex;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   martingale_lot=start_lot;
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


/////////////////////////////////////////-----------START SIGNALS-------/////////////////////////////////////////////////////////
//////////////////////$ nr 1 the moving average rainbow detector

double FMA1=iMA(Symbol(),0,2,0,MODE_EMA,PRICE_CLOSE,0);
double FMA2=iMA(Symbol(),0,3,0,MODE_EMA,PRICE_CLOSE,0);
double FMA3=iMA(Symbol(),0,5,0,MODE_EMA,PRICE_CLOSE,0);
double FMA4=iMA(Symbol(),0,8,0,MODE_EMA,PRICE_CLOSE,0);
double FMA5=iMA(Symbol(),0,13,0,MODE_EMA,PRICE_CLOSE,0);
double FMA6=iMA(Symbol(),0,21,0,MODE_EMA,PRICE_CLOSE,0);
double FMA7=iMA(Symbol(),0,34,0,MODE_EMA,PRICE_CLOSE,0);
double FMA8=iMA(Symbol(),0,55,0,MODE_EMA,PRICE_CLOSE,0);
double FMA9=iMA(Symbol(),0,89,0,MODE_EMA,PRICE_CLOSE,0);
double FMA10=iMA(Symbol(),0,144,0,MODE_EMA,PRICE_CLOSE,0);
double FMA11=iMA(Symbol(),0,233,0,MODE_EMA,PRICE_CLOSE,0);
   //slope             
double MACD   = iMACD(Symbol(),0, FAST_MACD_P, SLOW_MACD_P, SIGNAL_PERIOD, PRICE_CLOSE, MODE_SIGNAL, 0);  
//double ATRSLOW = iATR(Symbol(),0,ATR_SLOW_PERIOD,0);
//double ATRQUICK = iATR(Symbol(),0,ATR_QUICK_PERIOD,0);
double ADXTREND = iADX(Symbol(),0,ADX_PERIOD,PRICE_CLOSE,MODE_MAIN,0);

 ///////////////////////////////////////////////HOLY SIGNAL/////////////////////////
//here it goes baby
///buy
//HALT
if(MACD>0)
{
viaID="NS";
if(FMA1>FMA2 && FMA2>FMA3 && FMA3>FMA4 && FMA4>FMA5 && FMA5>FMA6 && FMA6>FMA7 && FMA7>FMA8 && FMA8>FMA9 && FMA9>FMA10 && FMA10>FMA11 && ADXTREND>ADX_LIMIT_TRIGGER)
{via="B";}
//else viaID="NB";
}
//halt buy


//sell
if(MACD<0)
{
viaID="NB";
if(FMA1<FMA2 && FMA2<FMA3 && FMA3<FMA4 && FMA4<FMA5 && FMA5<FMA6 && FMA6<FMA7 && FMA7<FMA8 && FMA8<FMA9 && FMA9<FMA10 && FMA10<FMA11 && ADXTREND>ADX_LIMIT_TRIGGER)
{via="S";}
//else viaID="NS";
}


 Print("-------BUY OR SELL?: ", via);
  Print("-------DECISION?: ", viaID);

/////////------------------------------------------------------------------------------------------------//
  /////buy signal

if(via=="B" || viaID=="NS" || (((AccountBalance()-AccountEquity())*100/AccountBalance())>MAX_ACCOUNT_RISK))
{

  
  TotalNumberOfOrders = OrdersTotal();
for(PositionIndex = TotalNumberOfOrders - 1; PositionIndex >= 0 ; PositionIndex --)  //  <-- for loop to loop through all Orders . .   COUNT DOWN TO ZERO !
   {
   if( ! OrderSelect(PositionIndex, SELECT_BY_POS, MODE_TRADES) ) continue;   // <-- if the OrderSelect fails advance the loop to the next PositionIndex
   
   if( OrderMagicNumber() == magic      // <-- does the Order's Magic Number match our EA's magic number ? 
      && OrderSymbol() == Symbol()         // <-- does the Order's Symbol match the Symbol our EA is working on ? 
      && ( OrderType() == OP_SELL ))         // <-- is the Order a SELL Order ? 
         // <-- PROFITABLE OR NOT NO NEED ?
   {   
slippagex=MathAbs(Bid-OrderStopLoss())/Point;
Print("this is the slippage: ",slippagex);
      if ( ! OrderClose( OrderTicket(), OrderLots(), Bid,slippagex ) )               // <-- try to close the order
         Print("Order Close failed, order number: ", OrderTicket(), " Error: ", GetLastError() );  // <-- if the Order Close failed print some helpful information 
      }
   } //  end of For loop
  
  
  
  
  
  
} 
/////---------------------------------------------------------------------------------------------///

/////sell signal
if(via=="S" || viaID=="NB" || (((AccountBalance()-AccountEquity())*100/AccountBalance())>MAX_ACCOUNT_RISK))
{

TotalNumberOfOrders = OrdersTotal();
for(PositionIndex = TotalNumberOfOrders - 1; PositionIndex >= 0 ; PositionIndex --)  //  <-- for loop to loop through all Orders . .   COUNT DOWN TO ZERO !
   {
   if( ! OrderSelect(PositionIndex, SELECT_BY_POS, MODE_TRADES) ) continue;   // <-- if the OrderSelect fails advance the loop to the next PositionIndex
   
   if( OrderMagicNumber() == magic      // <-- does the Order's Magic Number match our EA's magic number ? 
      && OrderSymbol() == Symbol()         // <-- does the Order's Symbol match the Symbol our EA is working on ? 
      && ( OrderType() == OP_BUY  ))         // <-- is the Order a Buy Order ? 
      // <-- PROFITABLE O NOT NO NEED?
      {
slippagex=MathAbs(Ask-OrderStopLoss())/Point;
Print("this is the slippage: ",slippagex);
      if ( ! OrderClose( OrderTicket(), OrderLots(), Ask,slippagex ) )               // <-- try to close the order
         Print("Order Close failed, order number: ", OrderTicket(), " Error: ", GetLastError() );  // <-- if the Order Close failed print some helpful information 
     } 
   } //  end of For loop 
 
 
}



//////////////////////////////MARTINGALE SYSTEM//////////////////////////////////////////////////

//--------------------------------//
    if(MARTINGALE_ACTIVATOR){ /////////////////
    static datetime lastClose;  datetime lastClosePrev = lastClose;
    for(i=0; i < OrdersHistoryTotal(); i++) {///////////////////////////////////////////////

 OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);   // Only orders w/
   if( OrderMagicNumber() == magic      // <-- does the Order's Magic Number match our EA's magic number ? 
      &&  OrderCloseTime()    > lastClosePrev 
      && OrderSymbol() == Symbol()         // <-- does the Order's Symbol match the Symbol our EA is working on ? 
      && (OrderType() == OP_BUY           // <-- is the Order a Buy Order ? 
      ||OrderType() == OP_SELL ) )      // <-- PROFITABLE O NOT ?
   {  /////////// /////////////////////////////////////////////////////////////////////////////////////////////

lastClose = OrderCloseTime();

 
  ////////////////////////////////////////////////////
   if(MARTINGALE_PROGRESSION==false){
if(OrderProfit()<=0) LEVEL++; //LOSS
if(OrderProfit()>0) LEVEL=0; //PROFIT CANCEL OUT
if(LEVEL>MARTINGALE_LEVELS) LEVEL=0;
start_lot=martingale_lot*MathPow(2,LEVEL);
}
///////////////////////////////////////////////////////////////////////////////////////
   if(MARTINGALE_PROGRESSION==true){
LEVEL++;
 if(LEVEL>MARTINGALE_LEVELS) LEVEL=0;
start_lot=martingale_lot*MathPow(2,LEVEL);
}
/////////////////////////////////////////////////////////
break;
}
///////////////////////////////////////
 } }
 ///////////////////////////////////////////-------------ORDERS SEND AND MODIFY--------/////////////////////////////
  
//----

  if(via=="B" && viaID=="NS" && (OrdersTotal()<TOTAL_BUY_ORDERS) && (((AccountBalance()-AccountEquity())*100/AccountBalance())<MAX_ACCOUNT_RISK)) ///////////////BUY
     {
     
          ticket=OrderSend(Symbol(),OP_BUY,start_lot,Ask,MAX_SLIPPAGE*10,0,0,"",magic,0,Blue);
     //   OrderModify(ticket,OrderOpenPrice(),Ask-STOPLOSSPIPS*Point,Ask+(TAKEPROFITPIPS*Point),0,CLR_NONE);  
     }
     
  if(via=="S" && viaID=="NB" && (OrdersTotal()<TOTAL_SELL_ORDERS) && (((AccountBalance()-AccountEquity())*100/AccountBalance())<MAX_ACCOUNT_RISK)) /////////// SELL
     {
    
          ticket=OrderSend(Symbol(),OP_SELL,start_lot,Bid,MAX_SLIPPAGE*10,0,0,"",magic,0,Red);
     //   OrderModify(ticket,OrderOpenPrice(),Bid+STOPLOSSPIPS*Point,Bid-(TAKEPROFITPIPS*Point),0,CLR_NONE); 
   }
//----





//////////////////////////////////////////------------END SIGNALS--------------////////////////////////////////////////////////////////////





   return(0);
  }

///////////////////////////////////-----------------------END PROGRAM-----------////////////////////////////////////////