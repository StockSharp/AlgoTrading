//+------------------------------------------------------------------+
//|                                                  ACDC_PL_MFI.mqh |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2011.11.24 |
//+------------------------------------------------------------------+
#include "aCandlePatterns.mqh"
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on Dark Cloud Cover/Piercing Line            |
//| confirmed by MFI                                                 |
//| Type=SignalAdvanced                                              |
//| Name=CDC_PL_MFI                                                  |
//| Class=CDC_PL_MFI                                                 |
//| Page=                                                            |
//| Parameter=PeriodMFI,int,49,Period of MFI                         |
//| Parameter=PeriodMA,int,11, Period of MA                          |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| CDC_PL_MFI Class.                                                |
//| Purpose: Trading signals class, based on                         |
//| the "Dark Cloud Cover/Piercing Line"                             |
//| Japanese Candlestick Patterns                                    |
//| with confirmation by MFI indicator                               |
//| Derived from CCandlePattern class.                               |
//+------------------------------------------------------------------+
class CDC_PL_MFI : public CCandlePattern
  {
protected:
   CiMFI               m_MFI;            // object-MFI
   //--- adjusted parameters
   int                 m_periodMFI;      // the "period of calculation" parameter of the oscillator
   ENUM_APPLIED_VOLUME m_applied;        // the "volume" parameter of the oscillator

public:
                     CDC_PL_MFI();
   //--- methods of setting adjustable parameters
   void              PeriodMFI(int value)               { m_periodMFI=value;           }
   void              PeriodMA(int value)                { m_ma_period=value;           }
   void              Applied(ENUM_APPLIED_VOLUME value) { m_applied=value;             }
   //--- method of verification of settings
   virtual bool      ValidationSettings();
   //--- method of creating the indicator and timeseries
   virtual bool      InitIndicators(CIndicators *indicators);
   //--- methods of checking if the market models are formed
   virtual int       LongCondition();
   virtual int       ShortCondition();

protected:
   //--- method of initialization of the oscillator
   bool              InitMFI(CIndicators *indicators);
   //--- methods of getting data
   double            MFI(int ind)                        { return(m_MFI.Main(ind));     }
  };
//+------------------------------------------------------------------+
//| Constructor CDC_PL_MFI.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CDC_PL_MFI::CDC_PL_MFI()
  {
//--- initialization of protected data
   m_used_series=USE_SERIES_HIGH+USE_SERIES_LOW;
//--- setting default values for the oscillator parameters
   m_periodMFI=14;
   m_applied=VOLUME_TICK;
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CDC_PL_MFI::ValidationSettings()
  {
//--- validation settings of additional filters
   if(!CCandlePattern::ValidationSettings()) return(false);
//--- initial data checks
   if(m_periodMFI<=0)
     {
      printf(__FUNCTION__+": period of the MFI oscillator must be greater than 0");
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
bool CDC_PL_MFI::InitIndicators(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- initialization of indicators and timeseries of additional filters
   if(!CCandlePattern::InitIndicators(indicators)) return(false);
//--- create and initialize MFI oscillator
   if(!InitMFI(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize MFI oscillators.                                      |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CDC_PL_MFI::InitMFI(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- add object to collection
   if(!indicators.Add(GetPointer(m_MFI)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize object
   if(!m_MFI.Create(m_symbol.Name(),m_period,m_periodMFI,m_applied))
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
int CDC_PL_MFI::LongCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Piercing Line and MFI<40
   if(CheckCandlestickPattern(CANDLE_PATTERN_PIERCING_LINE) && (MFI(1)<40))
      result=80;
//--- check conditions of short position closing
   if(((MFI(1)>30) && (MFI(2)<30)) || ((MFI(1)>70) && (MFI(2)<70)))
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
int CDC_PL_MFI::ShortCondition()
  {
   int result=0;
   int idx   =StartIndex();    
//--- check formation of Dark Cloud Cover pattern and MFI>60
   if(CheckCandlestickPattern(CANDLE_PATTERN_DARK_CLOUD_COVER) && (MFI(1)>60))
      result=80;
//--- check conditions of long position closing
   if(((MFI(1)>70) && (MFI(2)<70)) || ((MFI(1)<30) && (MFI(2)>30)))
      result=40;       
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+
