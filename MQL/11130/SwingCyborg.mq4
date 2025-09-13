//+------------------------------------------------------------------+
//|                                                  SwingCyborg.mq4 |
//|                                   Copyright © 2014, laplacianlab |
//|                     https://login.mql5.com/en/users/laplacianlab |
//+------------------------------------------------------------------+
#property copyright     "Author: laplacianlab, Copyright © 2014"
#property link          "https://login.mql5.com/en/users/laplacianlab"
#property version       "1.00"
#property description   "Expert Advisor based on your human ability to predict medium and long term trends."
//+------------------------------------------------------------------+
//| Var block                                                        |
//+------------------------------------------------------------------+
//--- custom ENUMs
enum ENUM_STATUS_EA
  {
   BUY,
   SELL,
   DO_NOTHING
  };  
 //--- the following enumeration is for capturing three different money management policies which will be defined in OnInit()
 enum ENUM_AGGRESSIVENESS
  {
   LOW,
   MEDIUM,
   HIGH
  };   
enum ENUM_TYPE_TREND
  {
   UPTREND,
   DOWNTREND
  };  
enum ENUM_VALID_TIMEFRAMES 
  {
   M30,
   H1,
   H4
  };  
  
//--- EA's inputs
input double                  Lots=0.1;                              // Size of lots
input ENUM_TYPE_TREND         YourTrendPrediction;                   // The trader's trend prediction
input ENUM_VALID_TIMEFRAMES   YourTrendTimeframe;                    // Timeframe of the trader's trend prediction
input datetime                TheTrendBeginsOn=__DATETIME__;         // Starting date of the trend
input datetime                TheTrendFinishesOn=__DATETIME__;       // End date of the trend
input ENUM_AGGRESSIVENESS     EAsAggressiveness;                     // EAs aggressiveness (money management)

//--- EA's global vars
int stopLoss;
int takeProfit;
datetime currentTime;
ENUM_STATUS_EA EAStatus;
MqlTick tick;

//--- EA's global vars for RSI
double rsi;
ENUM_TIMEFRAMES rsiPeriod;

//--- EA's global vars for bar management
static datetime previousBarTime;
datetime currentBarTime[1];
bool isNewBar = false;

//+------------------------------------------------------------------+
//| Checks if there is already an open position                      |
//+------------------------------------------------------------------+
bool PositionOpened()
{
   bool isOpened=false;
   for(int i=0;i<OrdersTotal();i++)
   {
      if(OrderSelect(i,SELECT_BY_POS))
      {
         isOpened=true;
      }
   }   
   return isOpened;   
}
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   // --- let's define the EA's money management policies
   switch(EAsAggressiveness)
   {
      case LOW:      
         takeProfit=300;
         stopLoss=200;      
         break;         
      case MEDIUM:      
         takeProfit=500;
         stopLoss=250;      
         break;         
      case HIGH:      
         takeProfit=600;
         stopLoss=300;      
         break;
   }   
   switch(YourTrendTimeframe)
   {
      case M30:      
         rsiPeriod=PERIOD_M30;            
         break;         
      case H1:      
         rsiPeriod=PERIOD_H1;     
         break;         
      case H4:      
         rsiPeriod=PERIOD_H4;      
         break;
   }
   currentTime=TimeLocal();  
//---
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
//--- let's first check if a new bar has come!        
   if(CopyTime(_Symbol,_Period,0,1,currentBarTime) > 0)
     {
      if(previousBarTime!=currentBarTime[0])
        {
         isNewBar=true;
         previousBarTime=currentBarTime[0];
        }
     }
   else
     {
      Alert("Error copying historical data, error: ", GetLastError());
      return;
     }    
   if(!isNewBar) return;   
//--- We run the instructions below only on new bars (once per bar)  
   double tp;
   double sl;   
   SymbolInfoTick(_Symbol,tick);
   //--- is it time to trade?
   currentTime=TimeLocal();
   if(TheTrendBeginsOn<=currentTime && currentTime<=TheTrendFinishesOn)
   {            
      rsi=iRSI(_Symbol,rsiPeriod,14,PRICE_CLOSE,0);      
      //--- if so, is there any open position?      
      if(!PositionOpened())
      {
         if(YourTrendPrediction==UPTREND && rsi<=65)
         {
            EAStatus=BUY;
         }
         else if (YourTrendPrediction==DOWNTREND && rsi>=35)
         {
            EAStatus=SELL;
         }  
      }
      else EAStatus=DO_NOTHING;
      switch(EAStatus)
      {
         case BUY:         
            tp=tick.ask+takeProfit*_Point;
            sl=tick.bid-stopLoss*_Point;            
            OrderSend(_Symbol,OP_BUY,Lots,Ask,3,sl,tp,"SwingCyborg",11384,0,Green);         
            break;            
         case SELL:         
            sl=tick.ask+stopLoss*_Point;
            tp=tick.bid-takeProfit*_Point;            
            OrderSend(_Symbol,OP_SELL,Lots,Bid,3,sl,tp,"SwingCyborg",11384,0,Green);            
            break;            
         case DO_NOTHING:         
            //--- nothing...            
            break;            
      }      
   }  
  }
//+------------------------------------------------------------------+
