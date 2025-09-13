//+------------------------------------------------------------------+
//|                            EMA 6.12(barabashkakvn's edition).mq5 |
//|                        Copyright 2017, MetaQuotes Software Corp. |
//|                                           http://wmua.ru/slesar/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, MetaQuotes Software Corp."
#property link      "http://wmua.ru/slesar/"
#property version   "1.000"
//---
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
//--- input parameters
input double   InpLots           = 1.0;      // Lots
input ushort   InpTakeProfit     = 10;       // Take Profit (in pips)
input ushort   InpTrailingStop   = 50;       // Trailing Stop (in pips)
input ushort   InpTrailingStep   = 5;        // Trailing Step (in pips)
input int      MA_fast_ma_period = 6;        // MA fast: averaging period 
input int      MA_slow_ma_period = 54;       // MA slow: averaging period 
input ulong    m_magic           = 32995373; // magic number
//---
ulong          m_slippage=30;                // slippage

double         ExtTakeProfit=0.0;
double         ExtTrailingStop=0.0;
double         ExtTrailingStep=0.0;

int            handle_iMA_fast;              // variable for storing the handle of the iMA indicator 
int            handle_iMA_slow;              // variable for storing the handle of the iMA indicator 
double         m_adjusted_point;             // point value adjusted for 3 or 5 points
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(MA_fast_ma_period<=0 || MA_slow_ma_period<=0)
     {
      Print("\"averaging period\" can not be less than or equal to zero");
      return(INIT_PARAMETERS_INCORRECT);
     }
   if(MA_fast_ma_period>=MA_slow_ma_period)
     {
      Print("\"MA fast\" can not be equal to or greater than \"MA slow\"");
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

   ExtTakeProfit=InpTakeProfit*m_adjusted_point;
   ExtTrailingStop=InpTrailingStop*m_adjusted_point;
   ExtTrailingStep=InpTrailingStep*m_adjusted_point;
//--- create handle of the indicator iMA
   handle_iMA_fast=iMA(m_symbol.Name(),Period(),MA_fast_ma_period,0,MODE_SMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMA_fast==INVALID_HANDLE)
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
   handle_iMA_slow=iMA(m_symbol.Name(),Period(),MA_slow_ma_period,0,MODE_SMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMA_slow==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
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

   double Fast[];
   double Slow[];
   ArraySetAsSeries(Fast,true);
   ArraySetAsSeries(Slow,true);
   iMAGet(handle_iMA_fast,2,Fast);
   iMAGet(handle_iMA_slow,2,Slow);
//--- for visual
   string text="Fast[1]="+DoubleToString(Fast[1],m_symbol.Digits()+1)+"\n"+
               "Fast[0]="+DoubleToString(Fast[0],m_symbol.Digits()+1)+"\n"+
               "Slow[1]="+DoubleToString(Slow[1],m_symbol.Digits()+1)+"\n"+
               "Slow[0]="+DoubleToString(Slow[0],m_symbol.Digits()+1);
   Comment(text);

   int count_buys=0;
   int count_sells=0;
   CalculatePositions(count_buys,count_sells);

   if(Slow[1]>Fast[1] && Slow[0]<Fast[0])
      if(count_sells>0)
         ClosePositions(POSITION_TYPE_SELL);

   if(Slow[1]<Fast[1] && Slow[0]>Fast[0])
      if(count_buys>0)
         ClosePositions(POSITION_TYPE_BUY);

   if(count_buys==0 && count_sells==0)
     {
      //--- check condition on BUY
      if(Slow[1]>Fast[1] && Slow[0]<Fast[0])
        {
         if(!RefreshRates())
           {
            PrevBars=iTime(1);
            return;
           }
         double tp=(InpTakeProfit==0)?0.0:m_symbol.Ask()+ExtTakeProfit;
         m_trade.Buy(InpLots,m_symbol.Name(),m_symbol.Ask(),0.0,m_symbol.NormalizePrice(tp));
        }
      //--- check condition on SELL
      else if(Slow[1]<Fast[1] && Slow[0]>Fast[0])
        {
         if(!RefreshRates())
           {
            PrevBars=iTime(1);
            return;
           }
         double tp=(InpTakeProfit==0)?0.0:m_symbol.Bid()-ExtTakeProfit;
         m_trade.Sell(InpLots,m_symbol.Name(),m_symbol.Bid(),0.0,m_symbol.NormalizePrice(tp));
        }
     }
   else
      Trailing();
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &trans,
                        const MqlTradeRequest &request,
                        const MqlTradeResult &result)
  {
//---

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
// double min_volume=m_symbol.LotsMin();
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      error_description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
// double max_volume=m_symbol.LotsMax();
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      error_description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
// double volume_step=m_symbol.LotsStep();
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

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
   if(copied>0) time=Time[0];
   return(time);
  }
//+------------------------------------------------------------------+
//| Calculate positions Buy and Sell                                 |
//+------------------------------------------------------------------+
void CalculatePositions(int &count_buys,int &count_sells)
  {
   count_buys=0.0;
   count_sells=0.0;

   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
               count_buys++;

            if(m_position.PositionType()==POSITION_TYPE_SELL)
               count_sells++;
           }
//---
   return;
  }
//+------------------------------------------------------------------+
//| Close positions                                                  |
//+------------------------------------------------------------------+
void ClosePositions(const ENUM_POSITION_TYPE pos_type)
  {
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of current positions
      if(m_position.SelectByIndex(i))     // selects the position by index for further access to its properties
         if(m_position.Symbol()==Symbol() && m_position.Magic()==m_magic)
            if(m_position.PositionType()==pos_type) // gets the position type
               m_trade.PositionClose(m_position.Ticket()); // close a position by the specified symbol
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMA                                 |
//+------------------------------------------------------------------+
bool iMAGet(int handle_iMA,const int count,double  &array[])
  {
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMABuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iMA,0,1,count,array)!=count)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMA indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//| Trailing                                                         |
//+------------------------------------------------------------------+
void Trailing()
  {
   if(ExtTrailingStop==0)
      return;
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
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
            else
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
                    }
              }

           }
  }
//+------------------------------------------------------------------+
