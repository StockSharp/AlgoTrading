
//Three grades take profit Grid System, use it on weak market and fluctuating market, may need close all positions at some time.

//+------------------------------------------------------------------+
//|                                                     204060.mq4 |
//|                                                     Robbie Ruan  |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|History:                                                           
//|Ver 0.01 2009.08.01                                                
//|Ver 0.02 2012.02.18
//|   1. Modified CloseAllWithEquityIncrease's EquityIncreasePercentage Control                                                                                      
//+------------------------------------------------------------------+
#property copyright "Robbie Ruan ver0.02 2012.02.18"
#property link      "robbie.ruan@gmail.com"

string        GridName = "AIGrid";       // identifies the grid. allows for several co-existing grids
extern int    MagicNumberGrid = 10001;   // Magic number of the trades. must be unique to identify the trades of one grid                                                                               
extern double Lots = 0.01;              // 
extern bool   ConsiderSpread = false;
extern double GridSize = 20;            // pips between orders - grid or mesh size
extern double BaseTakeProfit = 50;        // number of ticks to take profit. normally is = grid size but u can override
extern double Slippage = 2.5;
extern double GridSteps = 10;          // total number of orders to place
extern double GridMaxOpenEachSide = 0;         // maximum number of open positions either long or short side, not the sum of both
extern double StopLoss = 0;            // if u want to add a stop loss. normal grids dont use stop losses
extern double Grid_High = 0;           //define a regione that the price wave in 
extern double Grid_Low = 0;            //
extern int    TrailStop = 0;

extern double FixedAllLongStopLoss = 0;     //All long orders use the same StopLoss
extern double FixedAllShortStopLoss = 0;    //All Short orders use the same StopLoss
extern double UpdateInterval = 0;      // update orders every x minutes

extern bool   wantLongs = true;        //  do we want long positions
extern bool   wantShorts = true;      //  do we want short positions
extern bool   wantBreakout = true;     // do we want longs above price, shorts below price
extern bool   wantCounter = true;      // do we want longs below price, shorts above price

extern bool   ProfitRank1 = true;        // Control Profit Rank, if ProfitRank2,3 both false, then it's equals to 20 program
extern bool   ProfitRank2 = false;
extern bool   ProfitRank3 = false;

extern bool   unEquLongShortControlLots = false;
extern bool   EquityControlLots = false;

extern double BaseLots = 0.01;
extern double BaseEquity = 2000;

double        Equity_Old;
extern bool   CloseAllWithEquityIncrease = false;
extern bool   CloseAllWithEquityDecrease = false;
extern double EquityIncreasePercentage = 10.0;
extern double EquityDecreasePercentage = 10.0;
extern string Note1 = "Equity Increase Decrease Percentage xx%";

extern bool   StopTradeAfterEquityIncreased = false;
extern bool   StopTradeAfterEquityDecreased = false;
bool          StopTradeFlag = false;

extern bool   LimitEMA60 = false;       // do we want longs above ema only, shorts below ema only
extern bool   UseMACD = false;          // if true, will use macd >0 for longs only, macd >0 for shorts only
                                       // on crossover, will cancel all pending orders. This will override any
                                       // wantLongs and wantShort settings - at least for now.
                                      
extern bool   UseMAGroup=false;         // if all fast ma > slow ma,do not make short;
                                       // if all fast ma < slow ma, do not make long;    
extern bool   UseStochastic=false; 

extern bool   UseSAR = false;
extern double SAR_Step = 0.02;
extern double SAR_Maximum = 0.2;
                                                                                                
extern bool   CloseOpenPositions = false;// if UseMACD or UseMAGroup, do we also close open positions with a loss?

extern bool   ClosePendingPositions = false; // Close Pending Positions Far Away From Present Price

double LastUpdate = 0;          // counter used to note time of last update


//int LisenceStartTime = D'31.12.2009 11:30';
//int LisenceEndTime = D'31.12.2011 11:30';

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//---- 
#property show_inputs                  // shows the parameters - thanks Slawa...    

   Equity_Old = AccountEquity();
   
   if (MarketInfo("EURUSD",MODE_DIGITS)==5)
      {
         GridSize *= 10;
         BaseTakeProfit *= 10;
         Slippage *= 10;
         StopLoss *= 10;
         TrailStop *= 10;
      }

//double spread = MarketInfo(Symbol(),MODE_SPREAD);
//if ( BaseTakeProfit <= 0 )                 // 
//   { BaseTakeProfit = GridSize-spread; }
//   Print("BaseTakeProfit......................",BaseTakeProfit);

//----
//   GridName = StringConcatenate( "AIGrid", Symbol() );
   return(0);
  }
//+------------------------------------------------------------------------+
//| tests if there is an open position or order in the region of atRate    |
//|     will check for longs if checkLongs is true, else will check        |
//|     for shorts                                                         |
//+------------------------------------------------------------------------+

int IsPosition(double atRate, double inRange, bool checkLongs )
  {
     int CheckPositionRank = 0;                       // check how many rank profit is made in the specified rate and return to the function
     int totalorders = OrdersTotal();
     int type;
     double CurrentOrderTakeProfit;
     double point = MarketInfo(Symbol(),MODE_POINT);
        
     for(int j=0;j<totalorders;j++)                                // scan all orders and positions...
      {
        OrderSelect(j, SELECT_BY_POS);

        if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
         {  
             ///////////////////////////////////////////////////////////////////////////////////////////////////////
            // don't reset order near the present price
            if( wantBreakout != wantCounter)                // if only long order up, short order down, or only short order up, long order down
              {
                  if ( ( checkLongs && (MathAbs(Ask-atRate) < (inRange*0.9999)) ) || ( !checkLongs && (MathAbs(Bid-atRate) < (inRange*0.9999)) ) ) 
                  {
                     CheckPositionRank |= 7;
                     return(CheckPositionRank);          // if price near present price, direct return true
                  }
              }      
             
            if(MathAbs( OrderOpenPrice() - atRate ) < (inRange*0.9999 - point*Slippage))// dont look for exact price but price proximity (less than gridsize)
              { 
                 type = OrderType();
                 if ( ( checkLongs && ( type == OP_BUY || type == OP_BUYLIMIT  || type == OP_BUYSTOP ) )  || (!checkLongs && ( type == OP_SELL || type == OP_SELLLIMIT  || type == OP_SELLSTOP ) ) )
                 { 
                    
                    CurrentOrderTakeProfit = NormalizeDouble( ( OrderTakeProfit() - OrderOpenPrice() ),Digits );
                    CurrentOrderTakeProfit = MathAbs( CurrentOrderTakeProfit );
                    
                    if( ProfitRank1 && ( MathAbs( CurrentOrderTakeProfit - point*BaseTakeProfit ) < point* Slippage ) )
                     {
                        CheckPositionRank |= 1;
                        //Print("CheckPositionRank",CheckPositionRank);
                        
                     }
                    else if( ProfitRank2 && ( MathAbs( CurrentOrderTakeProfit - point*(GridSize+BaseTakeProfit) ) < point* Slippage ) )
                     {                      
                        CheckPositionRank |= 2;
                        //Print("CheckPositionRank",CheckPositionRank);
                     }
                    else if( ProfitRank3 && ( MathAbs( CurrentOrderTakeProfit - point*(GridSize*2+BaseTakeProfit) ) < point* Slippage ) )
                     {
                        CheckPositionRank |= 4;
                        //Print("CheckPositionRank",CheckPositionRank);
                     }
                 }
              } // if MathAbs end here    
         } // if loop end here
      } // for loop end here 
   // Print("CheckPositionRank::::::::::::::::::::::",CheckPositionRank);
   return(CheckPositionRank);
  }
  
  
 
//+------------------------------------------------------------------------+
//| close all open orders               
//+------------------------------------------------------------------------+

void CloseAllOpenOrders()
{
  int total = OrdersTotal();
  for(int i=total-1;i>=0;i--)
  {
    OrderSelect(i, SELECT_BY_POS);
    int type    = OrderType();
    bool result = false;

    if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
     {
        //Print("Closing 2 ",type);
        switch(type)
         {
           //Close opened long positions
           case OP_BUY       : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), Slippage, Red );
                               break;
           //Close opened short positions
           case OP_SELL      : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), Slippage, Red );
                               break;
           //Close pending orders
           case OP_BUYLIMIT  : result = true ;
           case OP_BUYSTOP   : result = true ;
           case OP_SELLLIMIT : result = true ;
           case OP_SELLSTOP  : result = true ;
         }
      }
    if(result == false)
    {
      //Alert("Order " , OrderTicket() , " failed to close. Error:" , GetLastError() );
      //Sleep(3000);
    }  
  }
  return;
}


//+------------------------------------------------------------------------+
//| cancel all pending orders                                      |
//+------------------------------------------------------------------------+

void CloseAllPendingOrders()
  {
     int totalorders = OrdersTotal();
     for(int j=totalorders-1;j>=0;j--)                                // scan all orders and positions...
      {
        OrderSelect(j, SELECT_BY_POS);

        if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
         {  
          int type = OrderType();
          bool result = false;
          switch(type)
          {
            case OP_BUYLIMIT  : result = OrderDelete( OrderTicket() ); break;
            case OP_BUYSTOP   : result = OrderDelete( OrderTicket() ); break;
            case OP_SELLLIMIT : result = OrderDelete( OrderTicket() ); break;
            case OP_SELLSTOP  : result = OrderDelete( OrderTicket() ); break;

            case OP_BUY       : result = true ;
            case OP_SELL      : result = true ;
            //Close pending orders
          }
          
         }
      } 
      return;
  }

//+------------------------------------------------------------------------+
//| cancel pending long orders                                           |
//+------------------------------------------------------------------------+

void ClosePendingLongOrders( )
  {
     int totalorders = OrdersTotal();
     for(int j=totalorders-1;j>=0;j--)                                // scan all orders and positions...
      {
        OrderSelect(j, SELECT_BY_POS);

        if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
         {  
          int type = OrderType();
          bool result = false;
          switch(type)
          {
            case OP_BUYLIMIT  : result = OrderDelete( OrderTicket() ); break;
            case OP_BUYSTOP   : result = OrderDelete( OrderTicket() ); break;
            case OP_BUY       : result = true;
            case OP_SELL      : result = true;
            //Close pending orders

            case OP_SELLLIMIT : result = true;
            case OP_SELLSTOP  : result = true;
          }
         }
      } 
   return;
  }


//+------------------------------------------------------------------------+
//| cancel pending short orders                                           |
//+------------------------------------------------------------------------+

void ClosePendingShortOrders( )
  {
     int totalorders = OrdersTotal();
     for(int j=totalorders-1;j>=0;j--)                                // scan all orders and positions...
      {
        OrderSelect(j, SELECT_BY_POS);

        if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
         {  
          int type = OrderType();
          bool result = false;
          switch(type)
          {
            case OP_SELLLIMIT : result = OrderDelete( OrderTicket() ); break;
            case OP_SELLSTOP  : result = OrderDelete( OrderTicket() ); break;
            case OP_BUY       : result = true;
            case OP_SELL      : result = true;
            //Close pending orders
            case OP_BUYLIMIT  : result = true; 
            case OP_BUYSTOP   : result = true; 

          }
         }
      } 
   return;
  }

//+------------------------------------------------------------------------+
//| cancel all pending orders    and close open positions               |
//+------------------------------------------------------------------------+

void CloseAllOrders()
{
  int total = OrdersTotal();
  for(int i=total-1;i>=0;i--)
{
    OrderSelect(i, SELECT_BY_POS);
    int type    = OrderType();
    bool result = false;

    if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
     {
//    Print("Closing 2 ",type);
        switch(type)
         {
           //Close opened long positions
           case OP_BUY       : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), Slippage, Red );
                               break;
           //Close opened short positions
           case OP_SELL      : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), Slippage, Red );
                               break;
           //Close pending orders
           case OP_BUYLIMIT  : result = OrderDelete( OrderTicket() ); break;
           case OP_BUYSTOP   : result = OrderDelete( OrderTicket() ); break;
           case OP_SELLLIMIT : result = OrderDelete( OrderTicket() ); break;
           case OP_SELLSTOP  : result = OrderDelete( OrderTicket() ); break;
         }
      }
    if(result == false)
    {
//     Alert("Order " , OrderTicket() , " failed to close. Error:" , GetLastError() );
//     Sleep(3000);
    }  
  }
  return;
}

//+------------------------------------------------------------------------+
//| cancel pending long orders    and close open long positions               |
//+------------------------------------------------------------------------+

void CloseLongOrders()
{
  int total = OrdersTotal();
  for(int i=total-1;i>=0;i--)
{
    OrderSelect(i, SELECT_BY_POS);
    int type    = OrderType();
    bool result = false;

    if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
     {
//    Print("Closing 2 ",type);
        switch(type)
         {
           //Close opened long positions
           case OP_BUY       : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), Slippage, Red ); break;
           case OP_BUYLIMIT  : result = OrderDelete( OrderTicket() ); break;
           case OP_BUYSTOP   : result = OrderDelete( OrderTicket() ); break;
//Reserve opened short positions
           case OP_SELL      : result = true;
           case OP_SELLLIMIT : result = true;
           case OP_SELLSTOP  : result = true;
         }
      }
    if(result == false)
    {
//     Alert("Order " , OrderTicket() , " failed to close. Error:" , GetLastError() );
//     Sleep(3000);
    }  
  }
  return;
}


//+------------------------------------------------------------------------+
//| cancel pending short orders    and close open short positions               |
//+------------------------------------------------------------------------+

void CloseShortOrders()
{
  int total = OrdersTotal();
  for(int i=total-1;i>=0;i--)
{
    OrderSelect(i, SELECT_BY_POS);
    int type    = OrderType();
    bool result = false;

    if ( ( OrderSymbol()==Symbol() ) && ( OrderMagicNumber() == MagicNumberGrid ) )  // only look if mygrid and symbol...
     {
//    Print("Closing 2 ",type);
        switch(type)
         {
//Close opened short positions
            case OP_SELL      : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), Slippage, Red ); break;
            case OP_SELLLIMIT : result = OrderDelete( OrderTicket() ); break;
            case OP_SELLSTOP  : result = OrderDelete( OrderTicket() ); break;
 
//Reserve opened long positions
            case OP_BUY       : result = true;
            case OP_BUYLIMIT  : result = true;
            case OP_BUYSTOP   : result = true;
        }
      }
    if(result == false)
    {
//     Alert("Order " , OrderTicket() , " failed to close. Error:" , GetLastError() );
//     Sleep(3000);
    }  
  }
  return;
}

 

//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
int start()
  {
//---- 

   int    i, k, type, ticket, entermode;
   int    totalorders,LongOpenOrders,ShortOpenOrders;
   bool   doit;
   double point, startrate, traderate,spread,traderate_Long;
   double LongLots,ShortLots;
   double Equity_New;
   
//  if (TimeCurrent() >= LisenceEndTime )
//   {
//     Print("Time Expired, please contact robbie.ruan@gmail.com");
//      return(0);
//   }
   
// Parameters defined here are initialized every time, while diefined as extern type initialize just once.

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//if Equity increased EquityIncreasePercentage%, close all

   if(CloseAllWithEquityIncrease)
   {   
      Equity_New = AccountEquity();   
      if( (Equity_New - Equity_Old)/Equity_Old >= EquityIncreasePercentage/100 )
      {
         Print("Close All With Equity Increased: ", EquityIncreasePercentage, "%");
         Equity_Old = Equity_New;
         CloseAllOpenOrders();
         CloseAllPendingOrders();
         if(StopTradeAfterEquityIncreased == true)
         {
            StopTradeFlag = true;
            Print("Stop Trade With Equity Increased, to restart trade, exit and run the EA again");
         }
         
      }
   }
      
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//if Equity Decreased EquityIncreasePercentage%, close all

   if(CloseAllWithEquityDecrease)
   {   
      Equity_New = AccountEquity();   
      if( (Equity_Old - Equity_New)/Equity_Old >= EquityDecreasePercentage/100 )
      {
         Print("Close All With Equity Decreased: ", EquityDecreasePercentage, "%");
         Equity_Old = Equity_New;
         CloseAllOpenOrders();
         CloseAllPendingOrders();
         if(StopTradeAfterEquityDecreased == true)
         {
            StopTradeFlag = true;
            Print("Stop Trade With Equity Decreased, to restart trade, exit and run the EA again");
         }
         
      }
   }
      
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




//   if(StopTradeAfterEquityIncreased == true)
//   {
//      if (StopTradeFlag == true)
//      {
//         Print("EquityIncreased, Stop Trade, to restart trade, exit and reset the EA");
//         return(0);
//      }
//   }



string ScreenString = "DateOfTrade";
ObjectDelete(ScreenString);
ObjectCreate(ScreenString, OBJ_LABEL, 0, 0, 0);
ObjectSetText(ScreenString, ""+TimeToStr(CurTime( ))+" Day"+DayOfWeek(), 12, "Arial Bold", Lime);
ObjectSet(ScreenString, OBJPROP_CORNER, 3);
ObjectSet(ScreenString, OBJPROP_XDISTANCE, 5);
ObjectSet(ScreenString, OBJPROP_YDISTANCE, 5);
 
 
//----
  if ( (MathAbs(CurTime()-LastUpdate)> UpdateInterval*60) && (StopTradeFlag == false) )           // we update the first time it is called and every UpdateInterval minutes
   {
   LastUpdate = CurTime();
   
   point = MarketInfo(Symbol(),MODE_POINT);
   
   if(ConsiderSpread == true)
   {
      spread = MarketInfo(Symbol(),MODE_SPREAD);
      spread = point*spread;
   }
   else
   {
      spread = 0;
   }
   LongLots = Lots;
   ShortLots = Lots;
  
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Equity Control Lots
   
   if(EquityControlLots)
   {
      //Print("Account equity ================== ",AccountEquity());
      double EquityPercentage = AccountEquity()/AccountBalance();
      //Print("===========================EquityPercentage=====",EquityPercentage);
      int EquityTimes = MathFloor( (AccountEquity()/BaseEquity) * EquityPercentage );  // how many time is present equity times BaseEquity
      //Print("EquityTimes===========",EquityTimes);      
      if(EquityTimes >= 1)
      {
         Lots = BaseLots*EquityTimes;                 // total new open Lots
         LongLots = Lots;
         ShortLots = Lots;
         
      }
      //Print("NewLots====================================", Lots);
   }
   
///////////////////////////////////////////////////////////////////////////////////////////////////
//calulate total open order quantiies
 
   totalorders = OrdersTotal();
   LongOpenOrders = 0;
   ShortOpenOrders = 0;
   for(i=0;i<totalorders;i++)
   {
      OrderSelect(i,SELECT_BY_POS);
      type = OrderType();
      if(type == OP_BUY)
         LongOpenOrders++;
      else if(type == OP_SELL)
         ShortOpenOrders++;
   }
////////////////////////////////////////////////////////////////////////////////////////////
//Unbalanced OpenOrders ControlLots, the smaller long short ratio, the more long lots and less short lots, vice visa
   if(unEquLongShortControlLots)
   {  
      
      if( (LongOpenOrders >=10 || ShortOpenOrders >= 10) && Lots > 0.1)
      {
         LongLots = Lots * (2.0*ShortOpenOrders/(LongOpenOrders+ShortOpenOrders));
         LongLots = NormalizeDouble(LongLots,2);
                     
         ShortLots = Lots*2-LongLots;
            
         if(LongLots == 0)
            {
               LongLots += BaseLots;
               ShortLots -= BaseLots;
            }
            
         else if(ShortLots == 0)
            {
               ShortLots += BaseLots;
               LongLots -= BaseLots;
            }
      }
      
      //Print("LongOpenOrders::::::::::::::",LongOpenOrders);
      //Print(":::::::ShortOpenOrders::::::::::::::",ShortOpenOrders);
   }
   

   //Print("LongLots============================================", LongLots);
   //Print("=========ShortLots========================================", ShortLots);



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Trailing stop
   if (TrailStop > 0) TrailingStop(TrailStop);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


// default base value is low price, if Low value is 0, then check if High value also 0, if both 0, then use market price as base value.
  
   if( Grid_Low > 0 )
      startrate=Grid_Low;                        
   else if( (Grid_Low == 0) && Grid_High>0 )
      startrate=Grid_High;
   else
   {
      startrate = ( Bid + point*GridSize/2 ) / point / GridSize;    // round to a number of ticks divisible by GridSize
      k = startrate ;
      k = k * GridSize ;
      startrate = k * point - GridSize*GridSteps/2*point ;          // calculate the lowest entry point
   }
   
//   Print("startrate::::::::::",startrate);
   double EMA60=iMA(NULL,PERIOD_H1,60,0,MODE_EMA,PRICE_CLOSE,0);
   
/////////////////////////////////////////////////////////////////////////////////////////////////////
// if use MACD

   if ( UseMACD )  {
      double Macd0=iMACD(NULL,PERIOD_H4,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
      double Macd1=iMACD(NULL,PERIOD_H4,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
      double Macd2=iMACD(NULL,PERIOD_H4,12,26,9,PRICE_CLOSE,MODE_MAIN,2);
       if( Macd0>0 && Macd1>0  &&  Macd2<0)     // cross up
        {
         CloseAllPendingOrders();
         if ( CloseOpenPositions == true ) { CloseAllOrders(); }
        }
       if( Macd0<0 && Macd1<0  &&  Macd2>0)     // cross down
        {
         CloseAllPendingOrders();
         if ( CloseOpenPositions == true ) { CloseAllOrders(); }
        }
       wantLongs = false;
       wantShorts = false;
       if( Macd0>0 && Macd1>0  &&  Macd2>0)     // is well above zero
        {
         wantLongs = true;
        }
       if( Macd0<0 && Macd1<0  &&  Macd2<0)     // is well below zero
        {
         wantShorts = true;
        }
   }
 
 //////////////////////////////////////////////////////////////////////////////////////////////////////////
 // if Use SAR
  
   if ( UseSAR )
   {
      wantLongs = false;
      wantShorts = false;
      double CLOSE0 = iClose(NULL, PERIOD_W1, 0);
      double SAR0 = iSAR(NULL, PERIOD_W1, SAR_Step, SAR_Maximum, 0);
      
      if(SAR0 < CLOSE0)
      {
         wantLongs = true;
      }
      
      if(SAR0 > CLOSE0)
      {
         wantShorts = true;
      }
      
   }
   
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
// when use MA Group, ADX must be used

   if(UseMAGroup)
   {
      double SMA5=iMA(NULL,PERIOD_M15,5,0,MODE_SMA,PRICE_CLOSE,0);
      double SMA13=iMA(NULL,PERIOD_M15,13,0,MODE_SMA,PRICE_CLOSE,0);
      double SMA21=iMA(NULL,PERIOD_M15,21,0,MODE_SMA,PRICE_CLOSE,0);
      double SMA60=iMA(NULL,PERIOD_M15,60,0,MODE_SMA,PRICE_CLOSE,0);
      double SMA200=iMA(NULL,PERIOD_M15,200,0,MODE_SMA,PRICE_CLOSE,0);
  
// MA Group Divergent Trend Up    
      if(SMA5>SMA13 && SMA13>SMA21 && SMA21>SMA60 && SMA60>SMA200 && (SMA5-SMA21>0.0040))
      {
         //KD Cross Up
         if( (!UseStochastic || ((iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_MAIN,0)-iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_SIGNAL,0))) > 2) )
         {
            //Print("Cross Up MAIN:::::::::::::::::::::::::::",iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_MAIN,0));
            //Print("Signal::::",iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_SIGNAL,0));
            //Sleep(5000);
            ClosePendingShortOrders();
            if(CloseOpenPositions==true)  { CloseShortOrders(); }
            wantLongs=true;
            wantShorts=false;
         }
      }
//MA Group Divergent Trend Down   
      else if(SMA5<SMA13 && SMA13<SMA21 && SMA21<SMA60 && SMA60<SMA200 && (SMA21-SMA5>0.0040))
      {
         //KD Cross Down
         if( (!UseStochastic || ((iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_SIGNAL,0)-iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_MAIN,0))) > 2) )
         {
            //Print("Cross Down MAIN...............................",iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_MAIN,0));
            //Print("Signal::::",iStochastic(NULL,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_SIGNAL,0));
            //Sleep(5000);
            ClosePendingLongOrders();
            if(CloseOpenPositions==true)  { CloseLongOrders(); }
            wantLongs=false; 
            wantShorts=true;
         }
      }
      else
      {
         wantLongs=false;
         wantShorts=false;
         PlaySound("alert2.wav");  
      }
   }
   
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
   for( i=0;i<GridSteps;i++)
   {                    
      if( (Grid_Low == 0) && Grid_High > 0 )
         traderate = startrate - i*point*GridSize;
      else
         traderate = startrate + i*point*GridSize;   // Whether Grid_Low >0 or =0, both put net from bottom up
         traderate_Long = traderate+spread;
      
      if((Grid_Low > 0 && traderate < Grid_Low) || (Grid_High > 0 &&  traderate > Grid_High))
         break;
// all pending orders will not exeed order high low limit

     if ( wantLongs && (!LimitEMA60 || traderate > EMA60) && (LongLots > 0) && (((GridMaxOpenEachSide > 0) && (LongOpenOrders < GridMaxOpenEachSide)) || (GridMaxOpenEachSide == 0)) )
       {
         int CheckPositionRank=255;
         CheckPositionRank=IsPosition(traderate_Long,point*GridSize,true);
         // Print("CheckPositionRank>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>",CheckPositionRank);
         // Print("CheckPositionRank & 1:::::::::::",CheckPositionRank & 1);
         // Print("CheckPositionRank & 2:::::::::::",CheckPositionRank & 2);
         // Print("CheckPositionRank & 4:::::::::::",CheckPositionRank & 4);
         
         //check profit rank 1
         if ( ProfitRank1 && ((CheckPositionRank & 1) == 0) )           // test if i have no open orders profit rank 1 close to my price: if so, put one on
          {
            double myStopLoss = 0;
            
            if(FixedAllLongStopLoss>0)
               myStopLoss=FixedAllLongStopLoss;
            else if( StopLoss > 0 )
               {
                  myStopLoss = traderate_Long-point*StopLoss;
               }
               
            if( traderate_Long > Ask ) 
               { entermode = OP_BUYSTOP; } 
            else 
               { entermode = OP_BUYLIMIT ; } 
            if( ((traderate_Long > Ask ) && (wantBreakout)) || ((traderate_Long <= Ask ) && (wantCounter)) ) 
               {
                  ticket=OrderSend(Symbol(),entermode,LongLots,traderate_Long,0,myStopLoss,traderate_Long+point*BaseTakeProfit,GridName,MagicNumberGrid,0,Green); 
               }
          }
         
         //check profit rank 2
         if ( ProfitRank2 && ((CheckPositionRank & 2) == 0) )           // test if i have no open orders profit rank 2 close to my price: if so, put one on
          {
            myStopLoss = 0;
            
            if(FixedAllLongStopLoss>0)
               myStopLoss=FixedAllLongStopLoss;
            else if( StopLoss > 0 )
               {
                  myStopLoss = traderate_Long-point*StopLoss;
               }
               
            if( traderate_Long > Ask ) 
               { entermode = OP_BUYSTOP; } 
            else 
               { entermode = OP_BUYLIMIT ; } 
            if( ((traderate_Long > Ask ) && (wantBreakout)) || ((traderate_Long <= Ask ) && (wantCounter)) ) 
               { 
                  ticket=OrderSend(Symbol(),entermode,LongLots,traderate_Long,0,myStopLoss,traderate_Long+point*(GridSize+BaseTakeProfit),GridName,MagicNumberGrid,0,Green); 
               }
          }
         
       
         //check profit rank 3
         if ( ProfitRank3 && ((CheckPositionRank & 4) == 0) )          // test if i have no open orders profit rank 3 close to my price: if so, put one on
          {
            myStopLoss = 0;
            
            if(FixedAllLongStopLoss>0)
               myStopLoss=FixedAllLongStopLoss;
            else if( StopLoss > 0 )
               {
                  myStopLoss = traderate_Long-point*StopLoss;
               }
               
            if( traderate_Long > Ask ) 
               { entermode = OP_BUYSTOP; } 
            else 
               { entermode = OP_BUYLIMIT ; } 
            if( ((traderate_Long > Ask ) && (wantBreakout)) || ((traderate_Long <= Ask ) && (wantCounter)) ) 
               { 
                  ticket=OrderSend(Symbol(),entermode,LongLots,traderate_Long,0,myStopLoss,traderate_Long+point*(GridSize*2+BaseTakeProfit),GridName,MagicNumberGrid,0,Green); 
               }
          } 
         
       }   
       
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

     if( wantShorts && (!LimitEMA60 || traderate < EMA60) && (ShortLots > 0) && (((GridMaxOpenEachSide > 0) && (ShortOpenOrders < GridMaxOpenEachSide)) || (GridMaxOpenEachSide == 0)) )
       {
         CheckPositionRank = 255;
         CheckPositionRank = IsPosition(traderate,point*GridSize,false);
         // Print("CheckPositionRank..................................................................",CheckPositionRank);
         // Print("CheckPositionRank & 1:::::::::::",CheckPositionRank & 1);
         // Print("CheckPositionRank & 2:::::::::::",CheckPositionRank & 2);
         // Print("CheckPositionRank & 4:::::::::::",CheckPositionRank & 4);
         
         //check profit rank 1
         if ( ProfitRank1 && ((CheckPositionRank & 1) == 0) )           // test if i have no open orders profit rank 1 close to my price: if so, put one on
          {
             myStopLoss = 0;
             
             if( FixedAllShortStopLoss > 0 )
               myStopLoss=FixedAllShortStopLoss;
             else if ( StopLoss > 0 )
               { 
                  myStopLoss = traderate+point*StopLoss;
               }
             
             if( traderate > Bid ) 
               { entermode = OP_SELLLIMIT; } 
             else 
               { entermode = OP_SELLSTOP ; } 
              
             if( ((traderate < Bid ) && (wantBreakout)) || ((traderate >= Bid ) && (wantCounter)) ) 
               { 
                  ticket=OrderSend(Symbol(),entermode,ShortLots,traderate,0,myStopLoss,traderate-point*BaseTakeProfit,GridName,MagicNumberGrid,0,Red); 
               }
          }
          
           //check profit rank 2
         if ( ProfitRank2 && ((CheckPositionRank & 2) == 0) )           // test if i have no open orders profit rank 2 close to my price: if so, put one on
          {
             myStopLoss = 0;
             
             if( FixedAllShortStopLoss > 0 )
               myStopLoss=FixedAllShortStopLoss;
             else if ( StopLoss > 0 )
               { 
                  myStopLoss = traderate+point*StopLoss;
               }
             
             if( traderate > Bid ) 
               { entermode = OP_SELLLIMIT; } 
             else 
               { entermode = OP_SELLSTOP ; } 
              
             if( ((traderate < Bid ) && (wantBreakout)) || ((traderate >= Bid ) && (wantCounter)) ) 
               { 
                  ticket=OrderSend(Symbol(),entermode,ShortLots,traderate,0,myStopLoss,traderate-point*(GridSize+BaseTakeProfit),GridName,MagicNumberGrid,0,Red); 
               }
          }
  
         //check profit rank 3
         if ( ProfitRank3 && ((CheckPositionRank & 4) == 0) )           // test if i have no open orders profit rank 3 close to my price: if so, put one on
          {
             myStopLoss = 0;
             
             if( FixedAllShortStopLoss > 0 )
               myStopLoss=FixedAllShortStopLoss;
             else if ( StopLoss > 0 )
               { 
                  myStopLoss = traderate+point*StopLoss;
               }
             
             if( traderate > Bid ) 
               { entermode = OP_SELLLIMIT; } 
             else 
               { entermode = OP_SELLSTOP ; } 
              
             if( ((traderate < Bid ) && (wantBreakout)) || ((traderate >= Bid ) && (wantCounter)) ) 
               { 
                  ticket=OrderSend(Symbol(),entermode,ShortLots,traderate,0,myStopLoss,traderate-point*(GridSize*2+BaseTakeProfit),GridName,MagicNumberGrid,0,Red); 
               }
          }           
       } //short "if" end here
     }// Grid "for" end here
   
 
   if(ClosePendingPositions)          // close pending orders far away from present price
   {
      for(i=0;i<OrdersTotal();i++)
      {
         OrderSelect(i, SELECT_BY_POS);
         type=OrderType();
         if( ( (type==OP_BUYLIMIT || type==OP_BUYSTOP) && MathAbs(OrderOpenPrice()-Ask) > point*GridSize*(GridSteps*0.5) ) ||  ( (type==OP_SELLLIMIT || type==OP_SELLSTOP) && MathAbs(OrderOpenPrice()-Bid) > point*GridSize*(GridSteps*0.5) ) )// Orders outside the GridSize
         {
            OrderDelete(OrderTicket());
         }
      }
      
   } //close pending order if end here
   
   }// time check "if" end here
   return(0);
  }
//+------------------------------------------------------------------+


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Trailing Stop function

void TrailingStop(int TrailStop)
{
   if (TrailStop >= 5)
   {
      int total = OrdersTotal();
      for (int i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if ( OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberGrid )
         {
            if (OrderType() == OP_BUY)
            {
               if ( Bid - OrderOpenPrice() > TrailStop * MarketInfo(OrderSymbol(), MODE_POINT) )
               {
                  if (OrderStopLoss() < Bid - TrailStop * MarketInfo(OrderSymbol(), MODE_POINT) )
                  {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Bid - TrailStop * MarketInfo(OrderSymbol(), MODE_POINT), OrderTakeProfit(), OrderExpiration(),CLR_NONE);
                  }
               }
               
            }
            
            else if (OrderType() == OP_SELL)
            {
               if (OrderOpenPrice() - Ask > TrailStop * MarketInfo(OrderSymbol(), MODE_POINT))
               {
                  if (OrderStopLoss() > Ask + TrailStop * MarketInfo(OrderSymbol(), MODE_POINT) || OrderStopLoss() == 0.0)
                  {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Ask + TrailStop * MarketInfo(OrderSymbol(), MODE_POINT), OrderTakeProfit(), OrderExpiration(),CLR_NONE);
                  }
               }     
            } //else end 
         }// magic end 
      } // for total end
   }// if TrailStop >=5 end
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

