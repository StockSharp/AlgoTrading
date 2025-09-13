//+------------------------------------------------------------------+
//|                                                  ACDC_PL_RSI.mqh |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2011.11.21 |
//+------------------------------------------------------------------+
#include "aCandlePatterns.mqh"
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on Dark Cloud Cover/Piercing Line            |
//| confirmed by RSI                                                 |
//| Type=SignalAdvanced                                              |
//| Name=CDC_PL_RSI                                                  |
//| Class=CDC_PL_RSI                                                 |
//| Page=                                                            |
//| Parameter=PeriodRSI,int,20,Period of RSI                         |
//| Parameter=PeriodMA,int,14, Period of MA                          |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| CDC_PL_RSI Class.                                                |
//| Purpose: Trading signals class, based on                         |
//| the "Dark Cloud Cover/Piercing Line"                             |
//| Japanese Candlestick Patterns                                    |
//| with confirmation by RSI indicator                               |
//| Derived from CCandlePattern class.                               |
//+------------------------------------------------------------------+
class CDC_PL_RSI : public CCandlePattern
  {
protected:
   CiRSI             m_rsi;            // object-rsi
   //--- adjusted parameters
   int               m_periodRSI;      // the "period of calculation" parameter of the oscillator
   ENUM_APPLIED_PRICE m_applied;       // the "prices series" parameter of the oscillator

public:
                     CDC_PL_RSI();
   //--- methods of setting adjustable parameters
   void              PeriodRSI(int value)              { m_periodRSI=value;           }
   void              PeriodMA(int value)               { m_ma_period=value;           }
   void              Applied(ENUM_APPLIED_PRICE value) { m_applied=value;             }
   //--- method of verification of settings
   virtual bool      ValidationSettings();
   //--- method of creating the indicator and timeseries
   virtual bool      InitIndicators(CIndicators *indicators);
   //--- methods of checking if the market models are formed
   virtual int       LongCondition();
   virtual int       ShortCondition();

protected:
   //--- method of initialization of the oscillator
   bool              InitRSI(CIndicators *indicators);
   //--- methods of getting data
   double            RSI(int ind) { return(m_rsi.Main(ind));     }
  };
//+------------------------------------------------------------------+
//| Constructor CDC_PL_RSI.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CDC_PL_RSI::CDC_PL_RSI()
  {
//--- initialization of protected data
   m_used_series=USE_SERIES_HIGH+USE_SERIES_LOW;
//--- setting default values for the oscillator parameters
   m_periodRSI=14;
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CDC_PL_RSI::ValidationSettings()
  {
//--- validation settings of additional filters
   if(!CCandlePattern::ValidationSettings()) return(false);
//--- initial data checks
   if(m_periodRSI<=0)
     {
      printf(__FUNCTION__+": period of the RSI oscillator must be greater than 0");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create indicators.                                               |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CDC_PL_RSI::InitIndicators(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- initialization of indicators and timeseries of additional filters
   if(!CCandlePattern::InitIndicators(indicators)) return(false);
//--- create and initialize RSI oscillator
   if(!InitRSI(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize RSI oscillators.                                      |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CDC_PL_RSI::InitRSI(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- add object to collection
   if(!indicators.Add(GetPointer(m_rsi)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize object
   if(!m_rsi.Create(m_symbol.Name(),m_period,m_periodRSI,m_applied))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| "Voting" that price will grow.                                   |
//| INPUT:  no.                                                      |
//| OUTPUT: number of "votes" that price will grow.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CDC_PL_RSI::LongCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Piercing Line pattern and RSI<40
   if(CheckCandlestickPattern(CANDLE_PATTERN_PIERCING_LINE) && (RSI(1)<40))
      result=80;
//--- check conditions of short position closing
   if(((RSI(1)>30) && (RSI(2)<30)) || ((RSI(1)>70) && (RSI(2)<70)))
      result=40;
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+
//| "Voting" that price will fall.                                   |
//| INPUT:  no.                                                      |
//| OUTPUT: number of "votes" that price will fall.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CDC_PL_RSI::ShortCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Dark Cloud Cover pattern and RSI>60     
   if(CheckCandlestickPattern(CANDLE_PATTERN_DARK_CLOUD_COVER) && (RSI(1)>60))
      result=80;
//--- check conditions of long position closing
   if(((RSI(1)<70) && (RSI(2)>70)) || ((RSI(1)<30) && (RSI(2)>30)))
      result=40;
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+
