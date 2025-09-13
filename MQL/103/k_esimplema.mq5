//+------------------------------------------------------------------+
//|                                                  K_eSimpleMA.mq5 |
//|                                                Copyright tsaktuo |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "tsaktuo"
#property link      "http://www.mql5.com"
#property version   "1.00"

//--- input parameters
input int Periods=17;  // Period for MA indicator
input int SL=31;       // Stop Loss
input int TP=69;       // Take Profit
input int MAGIC=999;   // MAGIC number

MqlTradeRequest trReq;
MqlTradeResult trRez;
int handle1;
int handle2;
double SmoothedBuffer1[];
double SmoothedBuffer2[];

int sl;
int tp;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   ZeroMemory(trReq);
   ZeroMemory(trRez);
//---- set default vaules for all new order requests
   trReq.action=TRADE_ACTION_DEAL;
   trReq.magic=MAGIC;
   trReq.symbol=Symbol();                 // Trade symbol
   trReq.volume=0.1;                      // Requested volume for a deal in lots
   trReq.deviation=1;                     // Maximal possible deviation from the requested price
   trReq.type_filling=ORDER_FILLING_FOK;  // Order execution type
   trReq.type_time=ORDER_TIME_GTC;        // Order execution time
   trReq.comment="MA Sample";

//---- Create handle for 2 MA indicators
   handle1=iMA(Symbol(),PERIOD_CURRENT,Periods,0,MODE_EMA,PRICE_CLOSE);
   handle2=iMA(Symbol(),PERIOD_CURRENT,Periods+2,0,MODE_EMA,PRICE_CLOSE);

//---- input parameters are ReadOnly
   tp=TP;
   sl=SL;

//---- Suppoprt for acount with 5 decimals
   if(_Digits==5)
     {
      sl*=10;
      tp*=10;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {

   MqlTick tick; //variable for tick info
   if(!SymbolInfoTick(Symbol(),tick))
     {
      Print("Failed to get Symbol info!");
      return;
     }

//---- Copy latest MA indicator values into a buffer
   int copied=CopyBuffer(handle1,0,0,4,SmoothedBuffer1);
   if(copied>0)
      copied=CopyBuffer(handle2,0,0,4,SmoothedBuffer2);

   if(copied>0)
     {
      //---- If MAPeriod > MAPeriod+2 -> BUY
      if(SmoothedBuffer1[1]>SmoothedBuffer2[1] && SmoothedBuffer1[2]<SmoothedBuffer2[2])
        {
         trReq.price=tick.ask;                   // SymbolInfoDouble(NULL,SYMBOL_ASK);
         trReq.sl=tick.ask-_Point*sl;            // Stop Loss level of the order
         trReq.tp=tick.ask+_Point*tp;            // Take Profit level of the order
         trReq.type=ORDER_TYPE_BUY;              // Order type
         OrderSend(trReq,trRez);
        }
      //---- If MAPeriod < MAPeriod+2 -> SELL
      else if(SmoothedBuffer1[1]<SmoothedBuffer2[1] && SmoothedBuffer1[2]>SmoothedBuffer2[2])
        {
         trReq.price=tick.bid;
         trReq.sl=tick.bid+_Point*sl;            // Stop Loss level of the order
         trReq.tp=tick.bid-_Point*tp;            // Take Profit level of the order
         trReq.type=ORDER_TYPE_SELL;             // Order type
         OrderSend(trReq,trRez);
        }
     }

  }
//+------------------------------------------------------------------+