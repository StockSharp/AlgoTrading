//+------------------------------------------------------------------+
//|                                                    simplefx2.mq4 |
//|                             Copyright © 2007, GLENNBACON.COM LLC |
//|                                  http://www.GetForexSoftware.com |
//+------------------------------------------------------------------+
/*
   Simple FX source code, expert advisor and user manual are property of 
   GLENNBACON.COM LLC and may not be altered, used or sold in commercial
   products. The Simple FX source code itself may be altered and used in personal
   projects as long as the name GLENNBACON.COM LLC and 
   http://www.GetForexSoftware.com URL are left entacted and visible.
 
   Programmer: Glenn Bacon
   Note: Every programmer has their own style and format of writing code. I try
   to make the code as readable as possible by using indenting and tab nested code.
   There are comments throughout the source explaining what each block/line of code does.
   
   I always use brackets {} to contain my if else and for conditional statements. Some
   programmers use shortcuts I do not. I think more programmers should always use 
   brackets because it makes the code more readable. (bored reading this yet?)
   
   I also only create and use a couple of my own functions because if you create dozens of functions it
   makes the code more difficult to read.
 
   If you're not a programmer and have questions about the code do not email me about
   the code or what it does. If you are a programmer and have questions about this
   code I can be contacted by going to http://www.GetForexSoftware.com and navigate to our
   Customer Service page, it has a contact form.
   
     
*/
 
#property copyright "Copyright © 2007, GLENNBACON.COM LLC"
#property link      "http://www.GetForexSoftware.com"
 
// Shift of moving average
#define Shift 1
 
// Trend Detection function
#define BULL 111111
#define BEAR 222222
 
// Input variables
extern string  comment2          = "*** Order Options ***";
extern double  Lots              = 0.10;
extern int     Stop_Loss         = 0;
extern int     Take_Profit       = 0;
extern int     Slippage          = 5;
extern string  Order_Comment     = "Simple FX";
extern int     Magic             = 112607;
extern color   Order_Arrow_Color = Green;
 
 
extern string  comment3          = "*** Moving Average Options ***";
extern int     Long_MA_Period    = 200;
extern int     Long_MA_Method    = 0;
extern int     Long_MA_Applied_Price = 4;
 
extern int     Short_MA_Period   = 50;
extern int     Short_MA_Method    = 0;
extern int     Short_MA_Applied_Price = 4;
 
// Global variables
int Total_Open_Orders = 1; // we only want to open one order at a time and only manage the one order
int cnt = 0;               // counter variable, used in for() loops
bool init_variables;       // init variable when program starts
datetime PreviousBar;      // record the candle/bar time
int LastTrendDirection;    // record the previous trends direction
int FileHandle;            // variable for file handling 
 
int init()
  {
   if(IsTesting()==false)
   {
      init_variables = true;  // Allows us to init variables in the start function because 
                           // we cannot do this in the init function because the variables 
                           // use values from the chart 
                           
      //create data file if it does not exist and load variables if file does exist
      FileHandle=FileOpen("simplefx.dat",FILE_BIN | FILE_READ);
      if(FileHandle<1)
      {
         Print("simplefx.dat not found.");
         FileHandle=FileOpen("simplefx.dat",FILE_BIN | FILE_WRITE);
         if(FileHandle>0)
         {
            LastTrendDirection=0;
            FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
            FileClose(FileHandle);
            Print("simplefx.dat has been successfully created.");
         }
         else
         {
            FileClose(FileHandle);
            Print("Failed to create simplefx.dat file.");
         }  
      }
      else
      {
          LastTrendDirection=FileReadInteger(FileHandle,SHORT_VALUE);
          Print("Variables loaded from file.");
          FileClose(FileHandle);
      }
   }      
   return(0);
  }
int deinit()
  {
   return(0);
  }
 
int start()
  {
      
   // moving average only run expert advisor if there is enough candle/bars in history
   if(Bars < Long_MA_Period+1)
   {
      if(IsTesting()==false)
      {
         Comment("Long moving average does not have enough Bars in history to open a trade!\n", 
               "Must be at least ",Long_MA_Period, " bars to perform technical analysis.");
      }
      else
      {
         Comment("Back testing currently Long moving average does not have enough Bars to open a trade.\n", 
               "Must be at least ",Long_MA_Period, " bars to perform technical analysis.\n",
               "Please be patient and wait...\n",
               "Bars in history ",Bars);
      }
      return(0);
   }
            
   // make sure trader has set Lots to at least the minimum lot size of the broker and 
   // we will normalize the Lots variable so we can properly open an order
   if(MarketInfo(Symbol(),MODE_MINLOT) == 0.01)
   {
      Lots = NormalizeDouble(Lots,2);
      if(Lots < 0.01)
      {
         Comment("The variable Lots must be 0.01 or greater to open an order. ");
         return(0);
      }
   }
   if(MarketInfo(Symbol(),MODE_MINLOT) == 0.1)
   {
      Lots = NormalizeDouble(Lots,1);
      if(Lots < 0.1)
      {
         Comment("The variable Lots must be 0.1 or greater to open an order. ");
         return(0);
      }
   }
   if(MarketInfo(Symbol(),MODE_MINLOT) == 1)
   {
      Lots = NormalizeDouble(Lots,0);
      if(Lots < 1)
      {
         Comment("The variable Lots must be 1 or greater to open an order. ");
         return(0);
      }
   }
         
   // init variables when the expert advisor first starts running
   if(init_variables == true)
   {
      PreviousBar = Time[0];     // record the current canle/bar open time
      
      // place code here that you only wnat to run one time
       
      init_variables = false;    // change to false so we only init variable once
   }
   
   // record trends direction
   if(LastTrendDirection==0)
      {
         if(TrendDetection()==BULL)
         {
            LastTrendDirection=BULL;
         }
         if(TrendDetection()==BEAR)
         {
            LastTrendDirection=BEAR;
         }
         // save variables to file
         SaveVariables();
      }
      
   // perform analysis and open orders on new candle/bar 
   if(NewBar() == true)
   {
      // only perform analysis and close order if we only have one order open
      if(TotalOpenOrders() == Total_Open_Orders && SelectTheOrder() == true)
      {
         if(OrderType() == OP_BUY && TrendDetection() == BEAR)
         {
            OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Order_Arrow_Color);
         }
         if(OrderType() == OP_SELL && TrendDetection() == BULL)
         {
            OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Order_Arrow_Color);
         }
      }
      
      // only perform analysis and open new order if we have not reached our Total_Open_Orders max
      if(TotalOpenOrders() < Total_Open_Orders)
      {
         // open buy
         if(TrendDetection() == BULL && LastTrendDirection==BEAR)
         {
            if(Stop_Loss>0 && Take_Profit>0)
            {
               // open order
               OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,Ask-(Stop_Loss*Point),Ask+(Take_Profit*Point),Order_Comment,Magic,0,Order_Arrow_Color);
            }
            if(Stop_Loss>0 && Take_Profit==0)
            {
               // open order
               OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,Ask-(Stop_Loss*Point),0,Order_Comment,Magic,0,Order_Arrow_Color);
            }
            if(Stop_Loss==0 && Take_Profit>0)
            {
               // open order
               OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,Ask+(Take_Profit*Point),Order_Comment,Magic,0,Order_Arrow_Color);
            }
            if(Stop_Loss==0 && Take_Profit==0)
            {
               // open order
               OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,Order_Comment,Magic,0,Order_Arrow_Color);
            }
            LastTrendDirection=BULL;
            // save variables to file
            SaveVariables();
         }
         
         // open sell
         if(TrendDetection() == BEAR && LastTrendDirection==BULL)
         {
            if(Stop_Loss>0 && Take_Profit>0)
            {
               // open order
               OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,Bid+(Stop_Loss*Point),Bid-(Take_Profit*Point),Order_Comment,Magic,0,Order_Arrow_Color);
            }
            if(Stop_Loss>0 && Take_Profit==0)
            {
               // open order
               OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,Bid+(Stop_Loss*Point),0,Order_Comment,Magic,0,Order_Arrow_Color);
            }
            if(Stop_Loss==0 && Take_Profit>0)
            {
               // open order
               OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,Bid-(Take_Profit*Point),Order_Comment,Magic,0,Order_Arrow_Color);
            }
            if(Stop_Loss==0 && Take_Profit==0)
            {
               // open order
               OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,Order_Comment,Magic,0,Order_Arrow_Color);
            }
            LastTrendDirection=BEAR;
            // save variables to file
            SaveVariables();
         }
      }
      
      // when back testing only display chart info every candle/bar so we do not slow down back tests
      if(IsTesting() == true)
      {
         Display_Info();
      }
   }
      
   // when not back testing display chart info every tick
   if(IsTesting() == false)
   {
      Display_Info();
   }
   
   return(0);
  }
 
/////////////////////////////////////////////////////////////////////////
// Common functions                                                   //
///////////////////////////////////////////////////////////////////////


int SaveVariables()
{
   if(IsTesting()==false)
   {
      //save variables to file
      FileHandle=FileOpen("simplefx.dat",FILE_BIN | FILE_WRITE);
      if(FileHandle<1)
      {
         Print("simplefx.dat not found.");
         FileHandle=FileOpen("simplefx.dat",FILE_BIN | FILE_WRITE);
         if(FileHandle>0)
         {
            FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
            FileClose(FileHandle);
            Print("simplefx.dat has been successfully created.");
         }  
         else
         {
            FileClose(FileHandle);
            Print("Failed to create simplefx.dat file.");
         }
      }
      else
      {
         FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
         FileClose(FileHandle);
         Print("SimpleFX variables successfully saved to file.");
      }  
   }
return(0);
}

 
// This function returns the total amount of orders the expert advisor has open  
int TotalOpenOrders()
{
   cnt=OrdersTotal();
   int TotalOpenOrders = 0;
   
   if(cnt==0)
   {
      return(0);
   }
   else
   {
      for(;cnt>=0;cnt--)
      {
         RefreshRates();
         OrderSelect(cnt,SELECT_BY_POS);
         if(OrderMagicNumber()==Magic)
         {
            TotalOpenOrders++;
         }
      }
   }
   return(TotalOpenOrders);
}
 
// This function finds the open order and selects it 
int SelectTheOrder()
{
   cnt=OrdersTotal();
      
   if(cnt==0)
   {
      return(false);
   }
   else
   {
      for(;cnt>=0;cnt--)
      {
         RefreshRates();
         OrderSelect(cnt,SELECT_BY_POS);
         if(OrderMagicNumber()==Magic)
         {
            return(true);
         }
      }
   }
   return(false);
}
 
// This function return the value true if the current bar/candle was just formed
bool NewBar()
{
   if(PreviousBar<Time[0])
   {
      PreviousBar = Time[0];
      return(true);
   }
   else
   {
      return(false);
   }
   return(false);    // in case if - else statement is not executed
}
 
// is trend up/bullish or is it down/bearish
int TrendDetection()
{
   // BULL trend
   if(iMA(NULL,0,Short_MA_Period,0,Short_MA_Method,Short_MA_Applied_Price,0) > iMA(NULL,0,Long_MA_Period,0,Long_MA_Method,Long_MA_Applied_Price,0) && iMA(NULL,0,Short_MA_Period,0,Short_MA_Method,Short_MA_Applied_Price,1) > iMA(NULL,0,Long_MA_Period,0,Long_MA_Method,Long_MA_Applied_Price,1))
   {
      return(BULL);
   }
   
   // BEAR trend
   if(iMA(NULL,0,Short_MA_Period,0,Short_MA_Method,Short_MA_Applied_Price,0) < iMA(NULL,0,Long_MA_Period,0,Long_MA_Method,Long_MA_Applied_Price,0) && iMA(NULL,0,Short_MA_Period,0,Short_MA_Method,Short_MA_Applied_Price,1) < iMA(NULL,0,Long_MA_Period,0,Long_MA_Method,Long_MA_Applied_Price,1))
   {
      return(BEAR);
   }
      
   // flat no trend return 0
   return(0);
}
 
void Display_Info()
{
   Comment("Simple FX ver 2.0\n",
            "Copyright © 2007, GlennBacon.com, LLC\n",
            "Visit: www.GetForexSoftware.com\n",
            "Forex Account Server:",AccountServer(),"\n",
            "Account Balance:  $",AccountBalance(),"\n",
            "Lots:  ",Lots,"\n",
            "Symbol: ", Symbol(),"\n",
            "Price:  ",NormalizeDouble(Bid,4),"\n",
            "Pip Spread:  ",MarketInfo("EURUSD",MODE_SPREAD),"\n",
            "Date: ",Month(),"-",Day(),"-",Year()," Server Time: ",Hour(),":",Minute(),":",Seconds(),"\n",
            "Minimum Lot Size: ",MarketInfo(Symbol(),MODE_MINLOT));
   return(0);
}