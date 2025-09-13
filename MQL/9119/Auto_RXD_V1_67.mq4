//+------------------------------------------------------------------+
//|                                              Auto_RXD_V1.67.mq4  |
//|                                            Copyright Robbie Ruan |
//|                                                                  |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|History:                                                           
//|Ver 0.01 2008.10.01                                                
//|...................
//|Ver 1.67 2012.07.01
//|   1. Added USD index.
//|   2. Changed all indicators' default status to be false     
//+------------------------------------------------------------------+
#property copyright "Copyright 2008 Robbie Ruan Ver 1.67"
#property link      "robbie.ruan@gmail.com"
//---- input parameters
extern string       AuthorContact = "Robbie.Ruan@gmail.com";

extern int          MagicNumberAI = 888;
extern int          MagicNumberAIReverse = 222;
extern int          MagicNumberGrid = 999;

extern string       NoteMode = "0 Indicator mode, 1 Grid Mode, 2 AI Short, 3 AI Long, 4 AI Filter and Normal";
extern int          Mode = 0;

extern bool         NewOrderAllowed = true;

extern bool         EnableHourTrade = false;
extern int          HourTradeStartTime  = 18;
extern int          HourTradeStopTime   = 23;

extern double       Lots = 0.1;
extern double       MaxLots = 0;

extern bool         FreeMarginControlLots = false;
extern double       BaseLots = 0.1;
extern double       BaseFreeMargin = 2000;   // example, evry 2000 USD make 0.1 Lot
extern double       InvestPercentage = 100;
extern int          Slippage = 1;

extern bool         EnableIndicatorManager  = true;

extern bool         EnableOrderCloseManager = true;
extern int          OrderCloseRSIPeriod     = 14;
extern int          OrderCloseCCIPeriod     = 14;

extern bool         EnableTrailingStopManager = false;
extern double       ATRLongStopRatio = 100;
extern double       ATRShortStopRatio = 100;

extern bool         ADXControl      = false;             // when adx0 > adx1, and above 20, can orders
extern int          ADXPeriod       = 14;
extern double       ADXThreshold    = 21;  
 
extern bool         SARControl      = false;
extern bool         SARCrossNeeded  = false;            //need enter when just SAR crossing
extern bool         MTF_SARControl  = false;            // multi time fram SAR cotrol
extern double       SAR_Step        = 0.02;
extern double       SAR_Maximum     = 0.2;

extern bool         MACDControl     = false;            // macd0,1,2 above 0 make long, vice visa
extern bool         MTF_MACDControl = false;
extern bool         MACDCrossNeeded = false;
extern double       MACD_FastEMA    = 12;
extern double       MACD_SlowEMA    = 26;
extern double       MACD_SMA        = 9;

extern bool         OsMAControl     = false;
extern bool         MTF_OsMAControl = false;
extern double       OsMA_FastEMA    = 12;
extern double       OsMA_SlowEMA    = 26;
extern double       OsMA_SMA        = 9;


extern bool         PriceFiboControl = false;
extern int          PriceFiboPeriod  = 9;
extern double       PriceFiboRetrace = 0.30;
static int          PriceFiboOld     = 0;

extern bool         MAFiboControl    = false;   
extern int          MAFiboPeriod     = 21;
extern int          MAFiboShift      = 9;
//extern int        MAFiboMA_Mothod  = 3;          //"MODE_LWMA";
//extern int        MAFiboApplied_Price  = 6;      //"PRICE_WEIGHTED";
extern double       MAFiboRetrace    = 0.30;
static int          MAFiboOld        = 0;

extern bool         MASlowControl   = false;
extern bool         MASlowIncreaseDetect = false;
extern bool         MASlowAccelerateDetect = false;
extern int          MASlowPeriod    = 89;
extern int          MASlowThreshold = 1;

extern bool         MAFastControl   = false;
extern bool         MAFastIncreaseDetect = false;
extern bool         MAFastAccelerateDetect = false;
extern int          MAFastPeriod    = 21;
extern int          MAFastThreshold = 1;

extern bool         MAGroupControl  = false;
extern bool         MAGroupLongMAControl = false;
extern int          MAGroupPeriod   = 34;

extern bool         ATR_TpSl_Enable = false;
extern int          ATR_Period      = 14;

extern bool         CandleControl   = false;           // make long only after new high, short after new low

extern bool         FractalsControl = false;

extern bool         RSIControl      = false;
extern int          RSIPeriod       = 14;

extern bool         CCIControl      = false;
extern int          CCIPeriod       = 14;

extern bool         AOControl       = false;

extern bool         ACControl       = false;

extern bool         XOControl       = false;
extern bool         XOCrossNeeded   = false;
extern double       XOBoxSize       = 20;

extern bool         OsEURControl    = false;
extern int          MA_Period       = 12;
extern int          MA_Shift        = 5;
extern int          Price           = 6;              //Weighted close price, (high+low+close+close)/4
extern int          MAMode          = 3;              //Linear weighted moving average.

extern bool         IchimokuControl = false;
extern int          Tenkan_Sen       = 9;
extern int          Kijun_Sen        = 26;
extern int          SenkouSpan_B     = 52;

extern string       NoteAI = "The Following are AI parameters";
extern double       BTS_TP = 50;             //TP = 30 - 150
extern double       BTS_SL = 50;             //SL = 10 - 100
extern int          BTS_p  = 5;              //p  = 1  - 5
//Mode = 2
extern int          Short_x1 = 100;          //x  = 1  - 200
extern int          Short_x2 = 100;          //x  = 1  - 200
extern int          Short_x3 = 100;          //x  = 1  - 200
extern int          Short_x4 = 100;          //x  = 1  - 200
extern int          Short_MA = 1;            //MA = 1  - 5     
extern double       Short_TP = 1000;         //TP = 30 - 150
extern double       Short_SL = 1000;         //SL = 10 - 100
extern int          Short_p  = 5;            //p  = 1  - 5
extern int          Short_Threshold = 100;   //Threshold = 80 - 120
//Mode = 3
extern int          Long_x1 = 100;           //x  = 1  - 200 
extern int          Long_x2 = 100;           //x  = 1  - 200
extern int          Long_x3 = 100;           //x  = 1  - 200
extern int          Long_x4 = 100;           //x  = 1  - 200
extern int          Long_MA = 1;             //MA = 1  - 5        
extern double       Long_TP = 1000;          //TP = 30 - 150
extern double       Long_SL = 1000;          //SL = 10 - 100
extern int          Long_p  = 5;             //p  = 1  - 5
extern int          Long_Threshold = 100;    //Threshold = 80 - 120
//Mode = 4
extern int          Supervisor_x1 = 100;     //x  = 1 - 200
extern int          Supervisor_x2 = 100;     //x  = 1 - 200
extern int          Supervisor_x3 = 100;     //x  = 1 - 200
extern int          Supervisor_x4 = 100;     //x  = 1 - 200
extern int          Supervisor_MA = 1;       //MA = 1 - 5
extern int          Supervisor_p  = 5;       //p  = 1 - 5
extern int          Supervisor_Threshold = 100;  //Threshold = 80 - 120
 
static int          prevtime = 0;
static double       SL = 10;
static double       TP = 10;


int init()
  {
//----
   if (MarketInfo("EURUSD",MODE_DIGITS) == 5)
   {
      Slippage *= 10;
      BTS_TP   *= 10;
      BTS_SL   *= 10;
      
      Short_TP *= 10;
      Short_SL *= 10;
      
      Long_TP  *= 10;
      Long_SL  *= 10;
      
      SL       *= 10;
      TP       *= 10;
      
   }
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


//#define             LONGDIRECTION 1
//#define             SHORTDIRECTION -1
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   int i,total;
   double ATR;
   
   double USDX;
   USDX = 50.14348112 * MathPow(MarketInfo("EURUSD", MODE_BID),-0.576)
                      * MathPow(MarketInfo("USDJPY", MODE_BID), 0.136)
                      * MathPow(MarketInfo("GBPUSD", MODE_BID),-0.119)
                      * MathPow(MarketInfo("USDCAD", MODE_BID), 0.091)
                      * MathPow(MarketInfo("USDSEK", MODE_BID), 0.042)
                      * MathPow(MarketInfo("USDCHF", MODE_BID), 0.036);
                      
   //Print("USDX = ", USDX);
   
   if ( EnableTrailingStopManager == true )
   {
      TrailingStopManager();
   }
   
   if (Time[0] == prevtime)
   {
      return(0);
   }
   
   prevtime = Time[0];
   
   if (! IsTradeAllowed() )
   {
      again();
      return(0);
   }
   
   if ( NewOrderAllowed == false )
   {
      return(0);
   }
      
   
   if ( EnableHourTrade == true )
   {
      if ( HourTradeStartTime < HourTradeStopTime )
      {
         //example, trade just in 19->20->21->22
         if (Hour() < HourTradeStartTime || Hour() > HourTradeStopTime )
         {
            return(0);
         }
      }
      
      else if ( HourTradeStartTime > HourTradeStopTime )
      {
         //example, trade just in 22->23->0->1
         if ( Hour() < HourTradeStartTime && Hour() > HourTradeStopTime )
         {
            return(0);
         }
         
      }
      
      else if ( HourTradeStartTime == HourTradeStopTime )
      {
         return(0);
      }
   }
   
   
   if ( EnableOrderCloseManager == true )
   {
      OrderCloseManager();
   }
   
   
   
   //initial TP/SL set to BTS_TP/BTS_SL
   
   if(ATR_TpSl_Enable == true)
   {
      ATR = iATR(NULL,0,ATR_Period,0);
      TP  = NormalizeDouble(4.0*ATR*BTS_TP/100.0,Digits);
      SL  = NormalizeDouble(3.0*ATR*BTS_SL/100.0, Digits);
   }
   
   else
   {
      TP = BTS_TP*Point;
      SL = BTS_SL*Point;
   }
   
   int ticket = -1;
   
   RefreshRates();
   
   double NewLots = Lots;
   if(FreeMarginControlLots == true)
   {
     NewLots = GetNewLots();
   }
   
   double Supervisor = Supervisor();
   if (Supervisor > 0)
   {
      
      //close reversed orders when doing Grid
      total = OrdersTotal();
      for (i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberGrid)
         {
            if ( OrderType() == OP_SELL)
            {
               //OrderClose(OrderTicket(),OrderLots(),Ask,1,CLR_NONE);
            }
         }
      }
      
      // if one long positions already exist, do not make new orders
      total = OrdersTotal();
      for (i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberAI)
         {
            if ( OrderType() == OP_BUY)
            return(0);
         }
      }
      
      //run here, it means no open long positions, so create one
      
      ticket = OrderSend(Symbol(), OP_BUY, NewLots, Ask, Slippage, Bid - SL, Bid + TP, WindowExpertName(), MagicNumberAI, 0, Blue); 
      if (ticket < 0)
      {
         again();      
      }
   }
   
   else if (Supervisor < 0)
   {
      //close reversed orders when doing Grid
      total = OrdersTotal();
      for (i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberGrid)
         {
            if ( OrderType() == OP_BUY)
            {
               //OrderClose(OrderTicket(),OrderLots(),Bid,1,CLR_NONE);
            }
         }
      }
      
      // if short positions already exist, do not make new orders
      total = OrdersTotal();
      for (i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberAI)
         {
            if ( OrderType() == OP_SELL)
            return(0);
         }
      }
      
      //run here, it means no open short positions, so create one
      
      ticket = OrderSend(Symbol(), OP_SELL, NewLots, Bid, Slippage, Ask + SL, Ask - TP, WindowExpertName(), MagicNumberAI, 0, Red); 
      if (ticket < 0)
      {
         again();
      }
   }
   
   else if (Supervisor == 0)
   {
      BasicTradingSystem(); // BTS, make both long and short orders at the same time
   }
   
//-- Exit --
   return(0);
}
//---------------------------------------------------------------------------------------------------------------
double Supervisor()
{
   double ATR;
   if (Mode == 4)
   {
      if (Supervisor_Perceptron() > 0)
      {
         // Perceptron Long
         if ( Long_Perceptron() > 0 && ( !EnableIndicatorManager || IndicatorManager(1) == true) )
         {
            if(ATR_TpSl_Enable == true)
            {
               ATR = iATR(NULL,0,ATR_Period,0);
               TP  = NormalizeDouble(4.0*ATR*Long_TP/100.0,Digits);
               SL  = NormalizeDouble(3.0*ATR*Long_SL/100.0, Digits);
            }
            
            else
            {
               TP = Long_TP*Point;
               SL = Long_SL*Point;
            }
            
            return(1);
         }
      }
      
      // Perceptron Short
      else if ( Short_Perceptron() < 0 && ( !EnableIndicatorManager || IndicatorManager(-1) == true) )
         {
            if(ATR_TpSl_Enable == true)
            {
               ATR = iATR(NULL,0,ATR_Period,0);
               TP  = NormalizeDouble(4.0*ATR*Short_TP/100.0,Digits);
               SL  = NormalizeDouble(3.0*ATR*Short_SL/100.0, Digits);
            }
            
            else
            {
               TP = Short_TP*Point;
               SL = Short_SL*Point;
            }
            
            return(-1);
         }
      
      else
         return(0);
   }

   if (Mode == 3)
   {
      if ( Long_Perceptron() > 0 && ( !EnableIndicatorManager || IndicatorManager(1) == true) )
      {
         if(ATR_TpSl_Enable == true)
            {
               ATR = iATR(NULL,0,ATR_Period,0);
               TP  = NormalizeDouble(4.0*ATR*Long_TP/100.0,Digits);
               SL  = NormalizeDouble(3.0*ATR*Long_SL/100.0, Digits);
            }
            
            else
            {
               TP = Long_TP*Point;
               SL = Long_SL*Point;
            }
            
            return(1);
      }
      
      else
         return(0);
   }

   if (Mode == 2)
   {
      if ( Short_Perceptron() < 0 && ( !EnableIndicatorManager || IndicatorManager(-1) == true) )
      {
         if(ATR_TpSl_Enable == true)
            {
               ATR = iATR(NULL,0,ATR_Period,0);
               TP  = NormalizeDouble(4.0*ATR*Short_TP/100.0,Digits);
               SL  = NormalizeDouble(3.0*ATR*Short_SL/100.0, Digits);
            }
            
            else
            {
               TP = Short_TP*Point;
               SL = Short_SL*Point;
            }
            
            return(-1);
      }
      
      else
         return(0);
   }
   
   if (Mode == 1)
   {
      return(0);
   }
   //Mode = 0 in none AI mode,that means just use indicators to determine buy or sell
   if (Mode == 0)
   {
      if ( ( EnableIndicatorManager == true && IndicatorManager(1) == true) )
      {
         Print("IndicatorManager(1) = ", IndicatorManager(1),"   Long...........");
         if(ATR_TpSl_Enable == true)
            {
               ATR = iATR(NULL,0,ATR_Period,0);
               TP  = NormalizeDouble(4.0*ATR*Long_TP/100.0,Digits);
               SL  = NormalizeDouble(3.0*ATR*Long_SL/100.0, Digits);
            }
            
            else
            {
               TP = Long_TP*Point;
               SL = Long_SL*Point;
            }
            
            return(1);
      }
      else if ( ( EnableIndicatorManager == true && IndicatorManager(-1) == true) )
         {
            Print("IndicatorManager(-1) = ", IndicatorManager(-1),"   Short...........");
            if(ATR_TpSl_Enable == true)
            {
               ATR = iATR(NULL,0,ATR_Period,0);
               TP  = NormalizeDouble(4.0*ATR*Short_TP/100.0,Digits);
               SL  = NormalizeDouble(3.0*ATR*Short_SL/100.0, Digits);
            }
            
            else
            {
               TP = Short_TP*Point;
               SL = Short_SL*Point;
            }
            
            return(-1);
         }
         
      return(0);  
   }   
   
   return(0);  // Mode nothing, just return 0 by default
   
}
//short traning
double Short_Perceptron()
{
   double   w1 = Short_x1 - 100;
   double   w2 = Short_x2 - 100;
   double   w3 = Short_x3 - 100;
   double   w4 = Short_x4 - 100;
   
   double   a1 = iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_CLOSE,   0         ) - iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p   );
   double   a2 = iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p   ) - iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p*2 );
   double   a3 = iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p*2 ) - iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p*3 );
   double   a4 = iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p*3 ) - iMA( NULL,0,Short_MA,0,MODE_LWMA,PRICE_WEIGHTED,Short_p*4 );
   
   return(w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 + Short_Threshold - 100 );
}
//long training1
double Long_Perceptron()
{
   double   w1 = Long_x1 - 100;
   double   w2 = Long_x2 - 100;
   double   w3 = Long_x3 - 100;
   double   w4 = Long_x4 - 100;
   
   double   a1 = iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_CLOSE,   0        ) - iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p   );
   double   a2 = iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p   ) - iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p*2 );
   double   a3 = iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p*2 ) - iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p*3 );
   double   a4 = iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p*3 ) - iMA( NULL,0,Long_MA,0,MODE_LWMA,PRICE_WEIGHTED,Long_p*4 );
   
   return(w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 + Long_Threshold - 100 );
   
}
//long training2
double Supervisor_Perceptron()
{
   double   w1 = Supervisor_x1 - 100;
   double   w2 = Supervisor_x2 - 100;
   double   w3 = Supervisor_x3 - 100;
   double   w4 = Supervisor_x4 - 100;
   
   double   a1 = iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_CLOSE,   0              ) - iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p   );
   double   a2 = iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p   ) - iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p*2 );
   double   a3 = iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p*2 ) - iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p*3 );
   double   a4 = iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p*3 ) - iMA( NULL,0,Supervisor_MA,0,MODE_LWMA,PRICE_WEIGHTED,Supervisor_p*4 );
   
   return(w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 + Supervisor_Threshold - 100 );
}
double BasicTradingSystem()
{
   //return(iCCI(Symbol(), 0, BTS_p, PRICE_OPEN, 0));
   bool Flag_Long = false;
   bool Flag_Short = false;
   int  total = OrdersTotal();
  
   for (int i = 0; i < total; i++)
   {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberGrid)
      {
         if ( OrderType() == OP_BUY)
         {
            Flag_Long = true;
         }
         
         if (OrderType() == OP_SELL)
         {
            Flag_Short = true;
         }
      }
   }
   
   if (Flag_Long == false)
   {
      //OrderSend(Symbol(), OP_BUY, lots, Ask, Slippage, Bid - SL * Point, Bid + TP * Point, WindowExpertName(), MagicNumberGrid, 0, Blue);
   }
   if (Flag_Short == false)
   {
      //OrderSend(Symbol(), OP_SELL, lots, Bid, Slippage, Ask + SL * Point, Ask - TP * Point, WindowExpertName(), MagicNumberGrid, 0, Red); 
   }
   
}
void again()
{
   prevtime = Time[1];
   Sleep(30000);
}
//Control All Inidicator results
bool IndicatorManager(int Direction)
{

   if (   (     IchimokuControl     || 
                ADXControl          || 
                MACDControl         || 
                SARControl          || 
                PriceFiboControl    || 
                MAFiboControl       || 
                XOControl           || 
                CandleControl       || 
                MAFastControl       || 
                MASlowControl       || 
                MAGroupControl      || 
                FractalsControl     || 
                RSIControl          ||
                CCIControl          ||
                AOControl           || 
                ACControl           || 
                OsMAControl         || 
                OsEURControl        )  == false )
   
   {
      Print("Enabled IndicatorManager, but no indicator Enabled. ");
      return(0);
   }
   
   if (Direction == 1)
   {
      return( (!IchimokuControl     || IchimokuConfirm()    == 1) && 
              (!ADXControl          || ADXConfirm()         == 1) && 
              (!MACDControl         || MACDConfirm()        == 1) && 
              (!SARControl          || SARConfirm()         == 1) &&
              (!PriceFiboControl    || PriceFiboConfirm()   == 1) &&
              (!MAFiboControl       || MAFiboConfirm()      == 1) && 
              (!XOControl           || XOConfirm()          == 1) &&
              (!CandleControl       || CandleConfirm(1)     == 1) &&
              (!MAFastControl       || MAFastConfirm()      == 1) &&
              (!MASlowControl       || MASlowConfirm()      == 1) &&
              (!MAGroupControl      || MAGroupConfirm()     == 1) &&
              (!FractalsControl     || FractalsConfirm(1)   == 1) &&
              (!RSIControl          || RSIConfirm()         == 1) &&
              (!CCIControl          || CCIConfirm()         == 1) &&
              (!AOControl           || AOConfirm()          == 1) &&
              (!ACControl           || ACConfirm()          == 1) &&
              (!OsMAControl         || OsMAConfirm()        == 1) &&
              (!OsEURControl        || OsEURConfirm()       == 1) );
   }
   
   if (Direction == -1)
   {
      return( (!IchimokuControl     || IchimokuConfirm()    == -1) && 
              (!ADXControl          || ADXConfirm()         == -1) && 
              (!MACDControl         || MACDConfirm()        == -1) && 
              (!SARControl          || SARConfirm()         == -1) &&
              (!PriceFiboControl    || PriceFiboConfirm()   == -1) && 
              (!MAFiboControl       || MAFiboConfirm()      == -1) && 
              (!XOControl           || XOConfirm()          == -1) &&
              (!CandleControl       || CandleConfirm(-1)    == -1) &&
              (!MAFastControl       || MAFastConfirm()      == -1) &&
              (!MASlowControl       || MASlowConfirm()      == -1) &&
              (!MAGroupControl      || MAGroupConfirm()     == -1) &&
              (!FractalsControl     || FractalsConfirm(-1)  == -1) &&
              (!RSIControl          || RSIConfirm()         == -1) &&
              (!CCIControl          || CCIConfirm()         == -1) &&
              (!AOControl           || AOConfirm()          == -1) &&
              (!ACControl           || ACConfirm()          == -1) &&
              (!OsMAControl         || OsMAConfirm()        == -1) &&
              (!OsEURControl        || OsEURConfirm()       == -1) );
   }
   
   return(false);
}

//ADX
int ADXConfirm()
{
// add this if for speed up test
if (ADXControl == true)
{
   double ADX0 = iADX(NULL,PERIOD_H4,ADXPeriod,PRICE_OPEN,MODE_MAIN,0);
// double ADX1 = iADX(NULL,PERIOD_H4,ADXPeriod,PRICE_OPEN,MODE_MAIN,1);
   
   double ADX_PLUSDI0  = iADX(NULL,PERIOD_H4,ADXPeriod,PRICE_OPEN,MODE_PLUSDI,0);
   double ADX_MINUSDI0 = iADX(NULL,PERIOD_H4,ADXPeriod,PRICE_OPEN,MODE_MINUSDI,0);
   
   if ( (ADX0 >= ADXThreshold) && (ADX_PLUSDI0 > ADX_MINUSDI0) )// && (ADX0 >= ADX1) )
   {
      return(1);
   }
   
   if ( (ADX0 >= ADXThreshold) && (ADX_PLUSDI0 < ADX_MINUSDI0) )// && (ADX0 >= ADX1) )
   {
      return(-1);
   }
   
}
   return(0);
}

//MACD/////////////////////////////////
int MACDConfirm()
{
// add this if for speed up test
if (MACDControl == true)
{
   double MACD0, MACD1, SIGNAL0, SIGNAL1, MACD0_H4, SIGNAL0_H4;
   MACD0   = iMACD(NULL,0,MACD_FastEMA,MACD_SlowEMA,MACD_SMA,PRICE_OPEN,MODE_MAIN,0);
   MACD1   = iMACD(NULL,0,MACD_FastEMA,MACD_SlowEMA,MACD_SMA,PRICE_OPEN,MODE_MAIN,1);
   
   SIGNAL0 = iMACD(NULL,0,MACD_FastEMA,MACD_SlowEMA,MACD_SMA,PRICE_OPEN,MODE_SIGNAL,0);
   SIGNAL1 = iMACD(NULL,0,MACD_FastEMA,MACD_SlowEMA,MACD_SMA,PRICE_OPEN,MODE_SIGNAL,1);

   MACD0_H4   = iMACD(NULL,PERIOD_H4,12,26,9,PRICE_OPEN,MODE_MAIN,0);
   SIGNAL0_H4 = iMACD(NULL,PERIOD_H4,12,26,9,PRICE_OPEN,MODE_SIGNAL,0);
   
   if ( MACD0 > MACD1 && MACD0 > SIGNAL0 && (MACDCrossNeeded || MACD0 > 0) && (!MACDCrossNeeded || (MACD0 < 0 && MACD1 < SIGNAL1) ) && (!MTF_MACDControl || MACD0_H4 > SIGNAL0_H4) )
   {
      return(1);
   }
   
   if ( MACD0 < MACD1 && MACD0 < SIGNAL0 && (MACDCrossNeeded || MACD0 < 0) && (!MACDCrossNeeded || (MACD0 > 0 && MACD1 > SIGNAL1) ) && (!MTF_MACDControl || MACD0_H4 < SIGNAL0_H4) )
   {
      return(-1);
   }
}
   return(0);
}

//OsMA/////////////////////////////////
//OsMA is actually iMACD(NULL,0,12,26,9,PRICE_OPEN,MODE_MAIN,0)-iMACD(NULL,0,12,26,9,PRICE_OPEN,MODE_SIGNAL,0);;
int OsMAConfirm()
{
// add this if for speed up test
if (OsMAControl == true)
{

   double OsMA0    = iOsMA(NULL,0,OsMA_FastEMA,OsMA_SlowEMA,OsMA_SMA,PRICE_OPEN,0);
   
   double OsMA0_H4 = iOsMA(NULL,PERIOD_H4,OsMA_FastEMA,OsMA_SlowEMA,OsMA_SMA,PRICE_OPEN,0);
   
   if ( OsMA0 > 0 && (!MTF_OsMAControl || OsMA0_H4 > 0 ) )
   {
      return(1);
   }
   
   if ( OsMA0 < 0 && (!MTF_OsMAControl || OsMA0_H4 < 0 ) )
   {
      return(-1);
   }

}
   
   return(0);
}

//SAR/////////////////////////////////
int SARConfirm()
{ 
// add this if for speed up test
if (SARControl == true)
{
   double SAR0    = iSAR(NULL,0,SAR_Step,SAR_Maximum,0);
   double SAR1    = iSAR(NULL,0,SAR_Step,SAR_Maximum,1);
   double SAR2    = iSAR(NULL,0,SAR_Step,SAR_Maximum,2);
   
   double SAR0_H4 = iSAR(NULL,PERIOD_H4,SAR_Step,SAR_Maximum,0);
   
   //indicate long
   double Low0_H4 = iLow(NULL,PERIOD_H4,0); 
   
   if( SAR0 < Low[0] && SAR1 < Close[1] && (!SARCrossNeeded || SAR2 > High[2]) && (!MTF_SARControl || SAR0_H4 < Low0_H4) )
   {
      return(1);
   }
   
   // indicate short
   double High0_H4 = iHigh(NULL,PERIOD_H4,0);
   
   if( SAR0 > High[0] && SAR1 > Close[1] && (!SARCrossNeeded || SAR2 < Low[2]) && (!MTF_SARControl || SAR0_H4 >High0_H4) )
   {
      return(-1);
   }
   
}
   
   return(0);
   
}

//Candle/////////////////////////////////
int CandleConfirm(int Direction)
{
// add this if for speed up test
if (CandleControl == true)
{
 
  if (Direction == 1 )
   {
      if ( High[1] > High[2] )// && Close[1] > Open[1] )// && Low1 > Low2 )
      {
         return(1);
      }
   }
   
  if (Direction == -1 )
   {
      if ( Low[1] < Low[2] )// && Close[1] < Open[1] )// && High1 < High2 )
      {
         return(-1);
      }
   }
   
}
   
   return(0);
}

int MAFastConfirm()
{
// add this if for speed up test
if (MAFastControl == true)
{
   double LWMA0 = iMA(NULL,0,MAFastPeriod,0,MODE_LWMA,PRICE_WEIGHTED,1);
   double LWMA1 = iMA(NULL,0,MAFastPeriod,0,MODE_LWMA,PRICE_WEIGHTED,2);
   double LWMA2 = iMA(NULL,0,MAFastPeriod,0,MODE_LWMA,PRICE_WEIGHTED,3);
   
   double SMA0  = iMA(NULL,0,MAFastPeriod,0,MODE_SMA,PRICE_CLOSE,1);
   
   double Delta = LWMA0 - SMA0;
   double MAFastIncreasement = LWMA0 - LWMA1;
   double MAFastAcceleration = (LWMA0 - LWMA1) - (LWMA1 - LWMA2);
   
   if ( ( Delta > MAFastThreshold*Point ) && (!MAFastIncreaseDetect || MAFastIncreasement > 0 ) && (!MAFastAccelerateDetect || MAFastAcceleration > 0 ) )
   {
      return(1);
   }
   
   if ( ( Delta < 0 - MAFastThreshold*Point) && (!MAFastIncreaseDetect || MAFastIncreasement < 0 ) && (!MAFastAccelerateDetect || MAFastAcceleration < 0 ) )
   {
      return(-1);
   }
   
}
   
   return(0);
}

int MASlowConfirm()
{
// add this if for speed up test
if (MASlowControl == true)
{
   double LWMA0 = iMA(NULL,0,MASlowPeriod,0,MODE_LWMA,PRICE_WEIGHTED,1);
   double LWMA1 = iMA(NULL,0,MASlowPeriod,0,MODE_LWMA,PRICE_WEIGHTED,2);
   double LWMA2 = iMA(NULL,0,MASlowPeriod,0,MODE_LWMA,PRICE_WEIGHTED,3);
   
   double SMA0  = iMA(NULL,0,MASlowPeriod,0,MODE_SMA,PRICE_CLOSE,1);
   
   double Delta = LWMA0 - SMA0;
   double MASlowIncreasement = LWMA0 - LWMA1;
   double MASlowAcceleration = (LWMA0 - LWMA1) - (LWMA1 - LWMA2);
   
   if ( ( Delta > MASlowThreshold*Point ) && (!MASlowIncreaseDetect || MASlowIncreasement > 0 ) && (!MASlowAccelerateDetect || MASlowAcceleration > 0 ) )
   {
      return(1);
   }
   
   if ( ( Delta < 0 - MASlowThreshold*Point) && (!MASlowIncreaseDetect || MASlowIncreasement < 0 ) && (!MASlowAccelerateDetect || MASlowAcceleration < 0 ) )
   {
      return(-1);
   }
   
}
   
   return(0);
}
 
int MAGroupConfirm()
{
// add this if for speed up test
if (MAGroupControl == true)
{
   double LWMA_Close0 = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_CLOSE,1);
   double LWMA_High0  = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_HIGH,1);
   double LWMA_Low0   = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_LOW,1);
   
   double LWMA_Close1 = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_CLOSE,2);
   double LWMA_High1  = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_HIGH,2);
   double LWMA_Low1   = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_LOW,2);   
   
   double LWMA_Close2 = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_CLOSE,3);
   double LWMA_High2  = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_HIGH,3);
   double LWMA_Low2   = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_LOW,3);   
   
   double LWMA_Close3 = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_CLOSE,4);
   double LWMA_High3  = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_HIGH,4);
   double LWMA_Low3   = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_LOW,4); 
   
   double LWMA_Close4 = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_CLOSE,5);
   double LWMA_High4  = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_HIGH,5);
   double LWMA_Low4   = iMA(NULL,0,MAGroupPeriod,0,MODE_LWMA,PRICE_LOW,5);     
   
   double LWMA_Close0_LongMA = iMA(NULL,0,MAGroupPeriod*4,0,MODE_LWMA,PRICE_CLOSE,1);
   double LWMA_Close1_LongMA = iMA(NULL,0,MAGroupPeriod*4,0,MODE_LWMA,PRICE_CLOSE,2);

   
   if ( ( ( LWMA_Close0 > LWMA_Close1 ) && ( LWMA_High0 > LWMA_High1 ) && ( LWMA_Low0 > LWMA_Low1 ) ) && ( ( ( LWMA_Close1 <= LWMA_Close2 ) && ( LWMA_High1 <= LWMA_High2 ) && ( LWMA_Low1 <= LWMA_Low2 ) ) || 
                                                                                                           ( ( LWMA_Close2 <= LWMA_Close3 ) && ( LWMA_High2 <= LWMA_High3 ) && ( LWMA_Low2 <= LWMA_Low3 ) ) ||
                                                                                                           ( ( LWMA_Close3 <= LWMA_Close4 ) && ( LWMA_High3 <= LWMA_High4 ) && ( LWMA_Low3 <= LWMA_Low4 ) )  )
                                                                                                      && ( !MAGroupLongMAControl || (LWMA_Close0_LongMA > LWMA_Close1_LongMA) )
      ) 
   
   {
      return(1);
   }
   
   if ( ( ( LWMA_Close0 < LWMA_Close1 ) && ( LWMA_High0 < LWMA_High1 ) && ( LWMA_Low0 < LWMA_Low1 ) ) && ( ( ( LWMA_Close1 >= LWMA_Close2 ) && ( LWMA_High1 >= LWMA_High2 ) && ( LWMA_Low1 >= LWMA_Low2 ) ) || 
                                                                                                           ( ( LWMA_Close2 >= LWMA_Close3 ) && ( LWMA_High2 >= LWMA_High3 ) && ( LWMA_Low2 >= LWMA_Low3 ) ) ||
                                                                                                           ( ( LWMA_Close3 >= LWMA_Close4 ) && ( LWMA_High3 >= LWMA_High4 ) && ( LWMA_Low3 >= LWMA_Low4 ) )  )
                                                                                                      && ( !MAGroupLongMAControl || (LWMA_Close0_LongMA < LWMA_Close1_LongMA) )     
      )
   
   {
      return(-1);
   }
}
   return(0);
}
 
int FractalsConfirm(int Direction)
{
// add this if for speed up test
if (FractalsControl == true)
{
   if ( Direction == 1 )
   {
      double FractalsUpper = iFractals(NULL,0,MODE_UPPER,3);
      
      if ( FractalsUpper == 0 || High[0] > FractalsUpper )
      {
         return(1);
      }
   }
   
   if ( Direction == -1 )
   {
      double FractalsLower = iFractals(NULL,0,MODE_LOWER,3);
      
      if ( FractalsLower == 0 || Low[0] < FractalsLower )
      {
         return(-1);
      }
   }
   
}
   return(0);
}

//////RSI
int RSIConfirm()
{
// add this if for speed up test
if (RSIControl == true)
{
   double RSI = iRSI(NULL,0,RSIPeriod,PRICE_OPEN,0);
   
   if ( RSI > 50 )
   {
      return(1);
   }
   
   if ( RSI < 50 )
   {
      return(-1);
   }
   
}
   return(0);
}


//////CCI
int CCIConfirm()
{
// add this if for speed up test
if (CCIControl == true)
{
   double CCI = iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL,0);
   
   if ( CCI > 100 )
   {
      return(1);
   }
   
   if ( CCI < -100 )
   {
      return(-1);
   }
   
}
   return(0);
}



//AO is actually iMACD(NULL,0,5,34,5,PRICE_MEDIAN,MODE_MAIN,0);
int AOConfirm()
{
// add this if for speed up test
if (AOControl == true)
{
   double AO0 = iAO(NULL,0,0);
   double AO1 = iAO(NULL,0,1);
   
   if ( AO0 > 0 )// && AO0 > AO1)
   {
      return(1);
   }
   
   if ( AO0 < 0 )// && AO0 < AO1)
   {
      return(-1);
   }
   
}   
   return(0);
}
//AC is actually AO-SMA(AO, 5), so it equals to iMACD(NULL,0,5,34,5,PRICE_MEDIAN,MODE_MAIN,0)-iMACD(NULL,0,5,34,5,PRICE_MEDIAN,MODE_SIGNAL,0);;
int ACConfirm()
{
// add this if for speed up test
if (ACControl == true)
{
   double AC0 = iAC(NULL,0,0);
   //double AC1 = iAC(NULL,0,1);
   
   if (AC0 > 0) //&& AC0 > AC1)
   {
      return(1);
   }
   
   if (AC0 < 0) //&& AC0 < AC1)
   {
      return(-1);
   }
   
}
   
   return(0);
}
int IchimokuConfirm()
{
// add this if for speed up test
if (IchimokuControl == true)
{
      //the BaseLine ( KIJUN SEN ) is a calculation of the ( highest high + lowest low )/2 over 26(Kijun_Sen) periods.
   
   double BaseLine       = iIchimoku(NULL,0,Tenkan_Sen,Kijun_Sen,SenkouSpan_B,MODE_KIJUNSEN,   0);
   
      //the ConversionLine is the same calc, ( highest high + lowest low )/2 but over 9(Tenkan_Sen) periods.
   
   double ConversionLine = iIchimoku(NULL,0,Tenkan_Sen,Kijun_Sen,SenkouSpan_B,MODE_TENKANSEN,  0);
   
      //the DelayLine is the closing price 26(Kijun_Sen) periods behind the current period.
   
   double DelayLine      = iIchimoku(NULL,0,Tenkan_Sen,Kijun_Sen,SenkouSpan_B,MODE_CHINKOUSPAN,Kijun_Sen);
   
      //the LeadingSpanA is the ( ConversionLine + BaseLine )/2 with the results displaced forward by 26(Kijun_Sen) periods
   
   double LeadingSpanA   = iIchimoku(NULL,0,Tenkan_Sen,Kijun_Sen,SenkouSpan_B,MODE_SENKOUSPANA,0);
   
      //the LeadingSpanB is the ( highest high + lowest low )/2 over the past 52(SenkouSpan_B) periods and displaced forward 26(Kijun_Sen) periods.
   
   double LeadingSpanB   = iIchimoku(NULL,0,Tenkan_Sen,Kijun_Sen,SenkouSpan_B,MODE_SENKOUSPANB,0);
   
   if ( (ConversionLine > BaseLine) )// && ( (Close[0] > LeadingSpanA) && (Close[0] > LeadingSpanB) ) && (DelayLine >= Close[Kijun_Sen]) )
   {
      return(1);
   }
   
   if ( (ConversionLine < BaseLine) )// && ( (Close[0] < LeadingSpanA) && (Close[0] < LeadingSpanB) ) && (DelayLine <= Close[Kijun_Sen]) )
   {
      return(-1);
   }
   
}
   return(0);
   
}
int OsEURConfirm()
{
// add this if for speed up test
if (OsEURControl == true)
{
   int i = 0;
   double EURIndex;
   EURIndex = (iMA("EURUSD",0,MA_Period,0,MAMode,Price,i) - iMA("EURUSD",0,MA_Period,0,MAMode,Price,i+MA_Shift)) * MathPow(10,MarketInfo("EURUSD",MODE_DIGITS)) +
              (iMA("EURGBP",0,MA_Period,0,MAMode,Price,i) - iMA("EURGBP",0,MA_Period,0,MAMode,Price,i+MA_Shift)) * MathPow(10,MarketInfo("EURGBP",MODE_DIGITS)) +
              (iMA("EURCHF",0,MA_Period,0,MAMode,Price,i) - iMA("EURCHF",0,MA_Period,0,MAMode,Price,i+MA_Shift)) * MathPow(10,MarketInfo("EURCHF",MODE_DIGITS)) +
              (iMA("EURJPY",0,MA_Period,0,MAMode,Price,i) - iMA("EURJPY",0,MA_Period,0,MAMode,Price,i+MA_Shift)) * MathPow(10,MarketInfo("EURJPY",MODE_DIGITS)) +
              (iMA("EURCAD",0,MA_Period,0,MAMode,Price,i) - iMA("EURCAD",0,MA_Period,0,MAMode,Price,i+MA_Shift)) * MathPow(10,MarketInfo("EURCAD",MODE_DIGITS)) +
              (iMA("EURAUD",0,MA_Period,0,MAMode,Price,i) - iMA("EURAUD",0,MA_Period,0,MAMode,Price,i+MA_Shift)) * MathPow(10,MarketInfo("EURAUD",MODE_DIGITS)) ;
             
   if (EURIndex > 0 )
   {
      return(1);
   }
   
   if (EURIndex < 0 )
   {
      return(-1);
   }
   
}
   
   return(0);   
}

//XO Confirm
int XOConfirm()
{
//for speed up test
if( XOControl == true )
{
   int CurrentBar;
   int Counter = 0;
   double X_High,O_Low,CurrentPrice;
   double XO_UpFlag,XO_DownFlag,XO_UpFlag2,XO_DownFlag2;
   
   XO_UpFlag = 0;
   XO_DownFlag = 0;
   
   XO_UpFlag2 = 0;
   XO_DownFlag2 = 0;
   
   for(int i=200;i>=1;i--)
   {
      XO_UpFlag2 = XO_UpFlag;
      XO_DownFlag2 = XO_DownFlag;
      
      CurrentBar = i;
      
      if (Counter<1)
      {
         X_High=Close[CurrentBar];
         O_Low =Close[CurrentBar];
         Counter=1;
      }
      
      CurrentPrice=Close[CurrentBar];
      
      if (CurrentPrice>(X_High+XOBoxSize*Point))
      {
         Counter +=1;
         X_High = CurrentPrice;
         O_Low  = CurrentPrice-XOBoxSize*Point;
         XO_UpFlag = 1;
         XO_DownFlag = 0;
      }
      
      if (CurrentPrice<(O_Low-XOBoxSize*Point))
      {
         Counter += 1;
         O_Low  = CurrentPrice;
         X_High = CurrentPrice+XOBoxSize*Point;
         XO_UpFlag = 0;
         XO_DownFlag = 1;
      }
      
   }
   
   if((XO_UpFlag == 1) && ( !XOCrossNeeded || XO_DownFlag2 == 1 ) )
   {
      return(1);
   }
   
   if((XO_DownFlag == 1) && ( !XOCrossNeeded || XO_UpFlag2 == 1 ) )
   {
      return(-1);
   }
}
   return(0);
   
}
// Moving Fibonacci
int PriceFiboConfirm()
{  
//for speed up test
if ( PriceFiboControl == true )
{
   int PriceFiboNew;
   
   double MaxHigh = High[iHighest(NULL,0,MODE_HIGH,PriceFiboPeriod,1)];
   double MaxLow  = Low[iLowest(NULL,0,MODE_LOW,PriceFiboPeriod,1)];    
   
   double Risistance = MaxHigh - (MaxHigh-MaxLow)*PriceFiboRetrace;    // = High - 30%
   double Support    = MaxLow  + (MaxHigh-MaxLow)*PriceFiboRetrace;    // = Low  + 30%   
   
   PriceFiboNew = PriceFiboOld;   // if no new assign, PriceFiboNew remains the same with the old one.
   if (Close[1] > Risistance )
   {
      PriceFiboNew = 1;
      PriceFiboOld = PriceFiboNew;   // store PriceFiboNew value to PriceFiboOld.
      //return(PriceFiboNew);
   }
   
   if (Close[1] < Support )
   {
      PriceFiboNew = -1;
      PriceFiboOld = PriceFiboNew;   // store PriceFiboNew value to PriceFiboOld.
      //return(PriceFiboNew);
   }
   
}
   return(PriceFiboNew);
}

// Moving Average Fibonacci
int MAFiboConfirm()
{  
//for speed up test
if ( MAFiboControl == true )
{
   int i, MAFiboNew;
   double tmp, MAMaxLow, MAMaxHigh;
   
   tmp = iMA(Symbol(),0,MAFiboPeriod,0,MODE_LWMA,PRICE_WEIGHTED,1);
   MAMaxHigh = tmp;
   MAMaxLow  = tmp;
   
   for (i=1; i<=MAFiboShift; i++ )
   {
      tmp = iMA(Symbol(),0,MAFiboPeriod,0,MODE_LWMA,PRICE_WEIGHTED,i);
      
      if ( tmp > MAMaxHigh )
      {
         MAMaxHigh = tmp;
      }
      
      if ( tmp < MAMaxLow )
      {
         MAMaxLow = tmp;
      }
      
   }
    
   double MARisistance = MAMaxHigh - (MAMaxHigh-MAMaxLow)*MAFiboRetrace;    // = MAHigh - 30%
   double MASupport    = MAMaxLow  + (MAMaxHigh-MAMaxLow)*MAFiboRetrace;    // = MALow  + 30%   
   
   MAFiboNew = MAFiboOld;   // if no new assign, MAFiboNew remains the same with the old one.
   if (Close[1] > MARisistance )
   {
      MAFiboNew = 1;
      MAFiboOld = MAFiboNew;   // store MAFiboNew value to MAFiboOld. 
      return(MAFiboNew);
   }
   
   if (Close[1] < MASupport )
   {
      MAFiboNew = -1;
      MAFiboOld = MAFiboNew;   // store MAFiboNew value to MAFiboOld. 
      return(MAFiboNew);
   }
 
}
   return(MAFiboNew);
}
 
double GetNewLots()
{
   double NewLots = BaseLots;   
   double FreeMarginPercentage = AccountFreeMargin()/AccountBalance();
   int FreeMarginTimes = MathFloor( ( ( InvestPercentage /100.0 * AccountFreeMargin() ) / BaseFreeMargin ) * FreeMarginPercentage );  // how many time is present FreeMargin times BaseFreeMargin
   
   if(FreeMarginTimes >= 1)
   {
      NewLots = BaseLots*FreeMarginTimes;
   }
   
   if ( MaxLots > 0 && NewLots > MaxLots )
   {
      NewLots = MaxLots;
   }
         
   return(NewLots);
   
}

int OrderCloseManager()
{
   int i, Flag;
   bool CloseBUYReason = false;
   bool CloseSELLReason = false;
   
   double SAR0 = iSAR(NULL,0,SAR_Step,SAR_Maximum,0);
   //double RSI = iRSI(NULL,0,OrderCloseCCIPeriod,PRICE_OPEN,0);
   //double CCI  = iCCI(NULL,0,OrderCloseCCIPeriod,PRICE_TYPICAL,0);

   if ( SAR0 > High[0]) // || ( RSI < 50 && CCI <= -100 ) ) 
   {
      CloseBUYReason = true;
   }
   
   if ( SAR0 < Low[0] ) // || ( RSI > 50 && CCI >=  100 ) )
   {
      CloseSELLReason = true;
   }  
   

   if ( CloseSELLReason == true)
   {
      int total = OrdersTotal();
      for (i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberAI)
         {
            if ( OrderType() == OP_SELL)
            {
               OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,CLR_NONE);
               Flag = 1;
            }
         }
      }   
   }
   
   if (CloseBUYReason == true )
   {
      total = OrdersTotal();
      for (i = 0; i < total; i++)
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumberAI)
         {
            if ( OrderType() == OP_BUY)
            {
               OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,CLR_NONE);
               Flag = -1;
            }
         }
      }   
   }
   
   return(Flag);
   
}
 
 
//---------------------------------------------------------------------------------------------------------------
// Trailing Stop
int TrailingStopManager()
{
   int Counter = 0;
   
   for(int i=0;i<OrdersTotal();i++)
   {
      OrderSelect(i,SELECT_BY_POS);
      
      // long trailing stop
      if(OrderType() == OP_BUY)
      {
         int ShiftBarCounts = 15;// iBarShift(Symbol(),0,OrderOpenTime(),0);
         Print("LongShiftBarCounts:::::::::::",ShiftBarCounts);
         
         double NewMaxHigh = High[iHighest(NULL,0,MODE_HIGH,ShiftBarCounts,0)];
         double NewLongStopLoss = NewMaxHigh - iATR(NULL,0,14,0)*3.0*ATRLongStopRatio/100.0 - (MarketInfo(OrderSymbol(),MODE_SPREAD) + 3)*Point;
         Print("NewMaxHigh::::::::::::::::",NewMaxHigh);
         Print("NewLongStopLoss::::::::::::::::::::::",NewLongStopLoss);
         Print("NormalizeDouble(NewLongStopLoss - OrderStopLoss(), Digits):::::::::::::::::::",NormalizeDouble(NewLongStopLoss - OrderStopLoss(), Digits));
         
         //if(NormalizeDouble(NewLongStopLoss - OrderOpenPrice(), Digits) > 0)
         //{
            if(NormalizeDouble(NewLongStopLoss - OrderStopLoss(), Digits) > 0)
            {
               Print("................................");
               OrderModify(OrderTicket(),OrderOpenPrice(),NewLongStopLoss,OrderTakeProfit(),OrderExpiration(),CLR_NONE);
               Counter += 1;
            }
         //}
      }
      
      // short trailing stop
       if(OrderType() == OP_SELL)
      {
         ShiftBarCounts = 15;// iBarShift(Symbol(),0,OrderOpenTime(),0);
         Print("ShortShiftBarCounts:::::::::::",ShiftBarCounts);
         
         double NewMaxLow = Low[iLowest(NULL,0,MODE_LOW,ShiftBarCounts,0)];
         double NewShortStopLoss = NewMaxLow + iATR(NULL,0,14,0)*3.0*ATRShortStopRatio/100.0 + (MarketInfo(OrderSymbol(),MODE_SPREAD) + 3)*Point;
         Print("NewMaxLow::::::::::::::::",NewMaxLow);
         Print("NewsShortStopLoss::::::::::::::::::::::",NewShortStopLoss);
         Print("NormalizeDouble(NewShortStopLoss - OrderStopLoss(), Digits):::::::::::::::::::",NormalizeDouble(NewShortStopLoss - OrderStopLoss(), Digits));
         
         //if(NormalizeDouble(NewShortStopLoss - OrderOpenPrice(), Digits) < 0)
         //{
            if(NormalizeDouble(NewShortStopLoss - OrderStopLoss(), Digits) < 0)
            {
               Print("................................");
               OrderModify(OrderTicket(),OrderOpenPrice(),NewShortStopLoss,OrderTakeProfit(),OrderExpiration(),CLR_NONE);
               Counter += 1;
            }
         //}
      }
      
   }// for end here
   
   return(Counter);
}  
//---------------------------------------------------------------------------------------------------------------