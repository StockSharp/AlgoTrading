//+------------------------------------------------------------------+
//|            EMA Cross Contest Hedged(barabashkakvn's edition).mq5 |
//|                                                      Coders Guru |
//|                                         http://www.forex-tsd.com |
//+------------------------------------------------------------------+
#property copyright "Coders Guru"
#property link      "http://www.forex-tsd.com"
#property version   "1.001"
//---
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\OrderInfo.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
COrderInfo     m_order;                      // pending orders object
//+------------------------------------------------------------------+
//| Enum Bar                                                         |
//+------------------------------------------------------------------+
enum EnumBar
  {
   curretn=0,    // bar #0
   previous=1,   // bar #1
  };
//--- input parameters
input double   InpLots           = 0.1;      // Lots
input ushort   InpStopLoss       = 140;      // Stop Loss (in pips)
input ushort   InpTakeProfit     = 120;      // Take Profit (in pips)
input ushort   InpTrailingStop   = 30;       // Trailing Stop (in pips)
input ushort   InpTrailingStep   = 1;        // Trailing Step (in pips)
input ushort   InpHedgeLevel     = 6;        // Hedge level (in pips)
input bool     InpCloseOpposite  = false;    // Close the opposite positions
input bool     InpUseMACD        = false;    // Use MACD
input ushort   InpExpiration     = 65535;    // Expiration pending orders (seconds) 
input int      InpShort_ma_period= 4;        // MA short: averaging period 
input int      InpLong_ma_period = 24;       // MA long: averaging period 
input EnumBar  InpCurrentBar     = previous; // Trade bar
input ulong    m_magic=154246789;            // magic number
//---
ulong          m_slippage=10;                // slippage

double         ExtStopLoss=0.0;
double         ExtTakeProfit=0.0;
double         ExtTrailingStop=0.0;
double         ExtTrailingStep=0.0;
double         ExtHedgeLevel=0.0;

int            handle_iMA_short;             // variable for storing the handle of the iMA indicator 
int            handle_iMA_long;              // variable for storing the handle of the iMA indicator 
int            handle_iMACD;                 // variable for storing the handle of the iMACD indicator 

double         m_adjusted_point;             // point value adjusted for 3 or 5 points
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(InpShort_ma_period>=InpLong_ma_period)
     {
      Print("\"MA short: averaging period\" can not be greater and equal to \"MA long: averaging period\"");
      return(INIT_PARAMETERS_INCORRECT);
     }
//---
   if(!m_symbol.Name(Symbol())) // sets symbol name
      return(INIT_FAILED);
   RefreshRates();

   string err_text="";
   if(!CheckVolumeValue(InpLots,err_text))
     {
      Print(err_text);
      return(INIT_PARAMETERS_INCORRECT);
     }
//---
   m_trade.SetExpertMagicNumber(m_magic);
//---
   if(IsFillingTypeAllowed(SYMBOL_FILLING_FOK))
      m_trade.SetTypeFilling(ORDER_FILLING_FOK);
   else if(IsFillingTypeAllowed(SYMBOL_FILLING_IOC))
      m_trade.SetTypeFilling(ORDER_FILLING_IOC);
   else
      m_trade.SetTypeFilling(ORDER_FILLING_RETURN);
//---
   m_trade.SetDeviationInPoints(m_slippage);
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;
   m_adjusted_point=m_symbol.Point()*digits_adjust;

   ExtStopLoss=InpStopLoss*m_adjusted_point;
   ExtTakeProfit=InpTakeProfit*m_adjusted_point;
   ExtTrailingStop=InpTrailingStop*m_adjusted_point;
   ExtTrailingStep=InpTrailingStep*m_adjusted_point;
   ExtHedgeLevel=InpHedgeLevel*m_adjusted_point;
//--- create handle of the indicator iMA
   handle_iMA_short=iMA(m_symbol.Name(),Period(),InpShort_ma_period,0,MODE_EMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMA_short==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMA
   handle_iMA_long=iMA(m_symbol.Name(),Period(),InpLong_ma_period,0,MODE_EMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMA_long==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMACD
   handle_iMACD=iMACD(m_symbol.Name(),Period(),4,24,12,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMACD==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMACD indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
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
//--- we work only at the time of the birth of new bar
   static datetime PrevBars=0;
   datetime time_0=iTime(0);
   if(time_0==PrevBars)
      return;
   PrevBars=time_0;
//---
   double macd=(InpUseMACD)?iMACDGet(MAIN_LINE,InpCurrentBar):0.0;
//---
   if(!RefreshRates())
      return;
   int crossed=Crossed();

   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
     {
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               if(InpCloseOpposite && crossed==2)
                 {
                  m_trade.PositionClose(m_position.Ticket());
                  continue;
                 }
               if(InpTrailingStop!=0)
                 {
                  if(m_position.PriceCurrent()-m_position.PriceOpen()>ExtTrailingStop+ExtTrailingStep)
                     if(m_position.StopLoss()<m_position.PriceCurrent()-(ExtTrailingStop+ExtTrailingStep))
                       {
                        if(!m_trade.PositionModify(m_position.Ticket(),
                           m_symbol.NormalizePrice(m_position.PriceCurrent()-ExtTrailingStop),
                           m_position.TakeProfit()))
                           Print("Modify ",m_position.Ticket(),
                                 " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                                 ", description of result: ",m_trade.ResultRetcodeDescription());
                        continue;
                       }
                 }
              }
            else if(m_position.PositionType()==POSITION_TYPE_SELL)
              {
               if(InpCloseOpposite && crossed==1)
                 {
                  m_trade.PositionClose(m_position.Ticket());
                  continue;
                 }
               if(InpTrailingStop!=0)
                 {
                  if(m_position.PriceOpen()-m_position.PriceCurrent()>ExtTrailingStop+ExtTrailingStep)
                     if((m_position.StopLoss()>(m_position.PriceCurrent()+(ExtTrailingStop+ExtTrailingStep))) || 
                        (m_position.StopLoss()==0))
                       {
                        if(!m_trade.PositionModify(m_position.Ticket(),
                           m_symbol.NormalizePrice(m_position.PriceCurrent()+ExtTrailingStop),
                           m_position.TakeProfit()))
                           Print("Modify ",m_position.Ticket(),
                                 " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                                 ", description of result: ",m_trade.ResultRetcodeDescription());
                        return;
                       }
                 }
              }
           }
      return;
     }
//--- if positions is "0"
   if(crossed==1 && macd>=0)
     {
      double sl=(InpStopLoss==0)?0.0:m_symbol.Ask()-ExtStopLoss;
      double tp=(InpTakeProfit==0)?0.0:m_symbol.Ask()+ExtTakeProfit;
      m_trade.Buy(InpLots,m_symbol.Name(),m_symbol.Ask(),
                  m_symbol.NormalizePrice(sl),
                  m_symbol.NormalizePrice(tp));
      datetime time=TimeCurrent()+InpExpiration;
      for(int i=0;i<4;i++)
        {
         double price=m_symbol.Ask()+ExtHedgeLevel*(i+1);
         sl=(InpStopLoss==0)?0.0:price-ExtStopLoss;
         tp=(InpTakeProfit==0)?0.0:price+ExtTakeProfit;
         m_trade.BuyStop(InpLots,m_symbol.NormalizePrice(price),m_symbol.Name(),
                         m_symbol.NormalizePrice(sl),m_symbol.NormalizePrice(tp),
                         ORDER_TIME_SPECIFIED,time);
        }
     }
   if(crossed==2 && macd<=0)
     {
      double sl=(InpStopLoss==0)?0.0:m_symbol.Bid()+ExtStopLoss;
      double tp=(InpTakeProfit==0)?0.0:m_symbol.Bid()-ExtTakeProfit;
      m_trade.Sell(InpLots,m_symbol.Name(),m_symbol.Bid(),
                   m_symbol.NormalizePrice(sl),
                   m_symbol.NormalizePrice(tp));
      datetime time=TimeCurrent()+InpExpiration;
      for(int i=0;i<4;i++)
        {
         double price=m_symbol.Bid()-ExtHedgeLevel*(i+1);
         sl=(InpStopLoss==0)?0.0:price+ExtStopLoss;
         tp=(InpTakeProfit==0)?0.0:price-ExtTakeProfit;
         m_trade.SellStop(InpLots,m_symbol.NormalizePrice(price),m_symbol.Name(),
                          m_symbol.NormalizePrice(sl),m_symbol.NormalizePrice(tp),
                          ORDER_TIME_SPECIFIED,time);
        }
     }
//---
  }
//+------------------------------------------------------------------+
//| Crossed Moving Average                                           |
//+------------------------------------------------------------------+
int Crossed()
  {
   double EmaLongPrevious=iMAGet(handle_iMA_long,InpCurrentBar+1);
   double EmaLongCurrent=iMAGet(handle_iMA_long,InpCurrentBar);
   double EmaShortPrevious=iMAGet(handle_iMA_short,InpCurrentBar+1);
   double EmaShortCurrent=iMAGet(handle_iMA_short,InpCurrentBar);
//---
   if(EmaShortPrevious<EmaLongPrevious && EmaShortCurrent>EmaLongCurrent)
      return(1); //up trend
   if(EmaShortPrevious>EmaLongPrevious && EmaShortCurrent<EmaLongCurrent)
      return(2); //down trend
//---
   return(0); //elsewhere
  }
//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates(void)
  {
//--- refresh rates
   if(!m_symbol.RefreshRates())
     {
      Print("RefreshRates error");
      return(false);
     }
//--- protection against the return value of "zero"
   if(m_symbol.Ask()==0 || m_symbol.Bid()==0)
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &error_description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=m_symbol.LotsMin();
   if(volume<min_volume)
     {
      error_description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }
//--- maximal allowed volume of trade operations
   double max_volume=m_symbol.LotsMax();
   if(volume>max_volume)
     {
      error_description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }
//--- get minimal step of volume changing
   double volume_step=m_symbol.LotsStep();
   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      error_description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                                     volume_step,ratio*volume_step);
      return(false);
     }
   error_description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+ 
//| Checks if the specified filling mode is allowed                  | 
//+------------------------------------------------------------------+ 
bool IsFillingTypeAllowed(int fill_type)
  {
//--- Obtain the value of the property that describes allowed filling modes 
   int filling=m_symbol.TradeFillFlags();
//--- Return true, if mode fill_type is allowed 
   return((filling & fill_type)==fill_type);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMA                                 |
//+------------------------------------------------------------------+
double iMAGet(int handle_iMA,const int index)
  {
   double MA[1];
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMABuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iMA,0,index,1,MA)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMA indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(MA[0]);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMACD                               |
//|  the buffer numbers are the following:                           |
//|   0 - MAIN_LINE, 1 - SIGNAL_LINE                                 |
//+------------------------------------------------------------------+
double iMACDGet(const int buffer,const int index)
  {
   double MACD[1];
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMACDBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iMACD,buffer,index,1,MACD)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMACD indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(MACD[0]);
  }
//+------------------------------------------------------------------+ 
//| Get Time for specified bar index                                 | 
//+------------------------------------------------------------------+ 
datetime iTime(const int index,string symbol=NULL,ENUM_TIMEFRAMES timeframe=PERIOD_CURRENT)
  {
   if(symbol==NULL)
      symbol=Symbol();
   if(timeframe==0)
      timeframe=Period();
   datetime Time[1];
   datetime time=0;
   int copied=CopyTime(symbol,timeframe,index,1,Time);
   if(copied>0)
      time=Time[0];
   return(time);
  }
//+------------------------------------------------------------------+
