//+------------------------------------------------------------------+
//|                                            Open Source Forex.mq4 |
//|                               Copyleft © 2007, Open Source Forex |
//|                                   http://www.opensourceforex.com |
//+------------------------------------------------------------------+
// OSF Countertrending version, 0 = long 100 = short 50 = flat
//Overbought/Oversold Oscillator
#property copyright "Copyleft © 2007, Open Source Forex"
#property link      "http://www.opensourceforex.com"

extern double Lots = 1;
extern double bars = 5; //bar smoothing - the amount of bars that need to pass before program loops
extern double TakeProfit = 150;


double arrayval;
double sellots;

//---- Include
#include <stderror.mqh>
#include <stdlib.mqh>

//---- Money Management
extern bool    MoneyManagement   = False;
extern bool    StrictManagement  = False;
extern bool    AcountIsMini      = False;
extern int     TradePercent      = 1;
extern int     SumTradePercent   = 10;
double         Margin, PF, PipValue, SumTrade;
bool           TradeAllowed      = False;
bool           Allowed           = False;

//---- Optimisation des Lots
extern bool    LotsOptimisation  = False;
//extern double  Lots              = 0.1;
extern double  PercentPerTrade   = 2;
extern double  DecreaseFactor    = 3;
double         lot;
int            orders, losses, spread;

//----- Identification
extern int     MagicEA           = 16384;
extern string  NameEA            = "Open Source Forex";

//----Buffers
double osfarray[51];
double positionsize[50];

//---- Variables
double rsival;
double ind1, ind2, ind3, ind4, ind5;
double selllots, lots, buylots, StopLoss, Target;
int    cnt, ticket, total, element_count;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
arrayval=50;
 //---- initializing of all array elements with 50
 //- OSF Array 0 = Long 100 = short 
ArrayInitialize(osfarray,100);
int   element_count=ArrayResize(osfarray, 100);

// indicators
//Calculates the specified custom indicator and returns its value. The custom indicator must be compiled (*.EX4 file) and be in the terminal_directory\experts\indicators directory. 
//ind1 = (iCustom(NULL, 0, "osf-fx5",13,1,0))*485;//485 converts to 1 - 100
ind1 = iRSI(NULL,0,14,PRICE_CLOSE,0);
ind2 = iRSI(NULL,0,14,PRICE_CLOSE,0);
ind3 = iRSI(NULL,0,14,PRICE_CLOSE,0);
ind4 = iRSI(NULL,0,14,PRICE_CLOSE,0);
ind5 = iRSI(NULL,0,14,PRICE_CLOSE,0);


arrayval = ( ind1 + ind2 + ind3 + ind4 + ind5 )/5 ;
// indicator to array conversion
for (int i=0; i<10;i++)
osfarray[i] = arrayval;

   
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
      if(Bars<100)
         {
            Print("bars less than 100");
            return(0);  
         }
      if(TakeProfit<10)
         {
            Print("TakeProfit less than 10");
            return(0);  // check TakeProfit
         }
      total=OrdersTotal();
      if(total<1) 
         {
// no opened orders identified
            if(AccountFreeMargin()<(1000*Lots))
               {
                  Print("We have no money. Free Margin = ", AccountFreeMargin());
                  return(0);  
               }
// Check array value
         }
//---- = 50 FLAT Module
//If array = 50 then do nothing.  
      if (arrayval!=50) 
         {
            Comment("Flat");
            return;
         }
//---- > 50 SELL Module
//if array = 51 then sell .01 lots if array = 52 then sell .02 lots
      if (arrayval>50) 
         {
            Comment("Selling..");
//check for open longs and close them

//calculate amount above 50
//if array = 51 then sell .01 lots if array = 52 then sell .02 lots
            selllots=(arrayval - 50)*lots;
            StopLoss = 0;
            Target = Bid-TakeProfit*Point; 
            /* Mnemonic
            int OrderSend( string symbol, int cmd, double volume, double price, int slippage, double stoploss, double takeprofit, string comment=NULL, int magic=0, datetime expiration=0, color arrow_color=CLR_NONE)
            */
            ticket=OrderSend(Symbol(),OP_SELL,selllots,Bid,3,StopLoss,Target,"OSF SELL",MagicEA,0,Red);
            if(ticket>0)
               {
                  if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                     Print("SELL order opened : ",OrderOpenPrice());
               }
            else 
               Print("Error opening SELL order : ",GetLastError()); 
            return(0); 

            return;
         }
//---- < 50 BUY Module
      if (arrayval<50) 
         {
            Comment("Buying...");
//check for open sells and close them

//calculate amount above 50
//if array = 49 then buy .01 lots if array = 48 then buy .02 lots
            buylots=(50 - arrayval)*lots;
            StopLoss = 0;
            Target = Ask-TakeProfit*Point;
            ticket=OrderSend(Symbol(),OP_SELL,buylots,Ask,3,StopLoss,Target,"OSF BUY",MagicEA,0,Green);
            if(ticket>0)
               {
                  if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                     Print("BUY order opened : ",OrderOpenPrice());
               }
            else 
               Print("Error opening BUY order : ",GetLastError()); 
            return(0);
            
            return;
         }

      //rsival=iRSI( string symbol, int timeframe, int period, int applied_price, int shift);
 
//----
      return(0);
   }
//+------------------------------------------------------------------+


