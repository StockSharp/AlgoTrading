//+------------------------------------------------------------------+
//|                                                  ACMS_ES_CCI.mqh |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2011.11.22 |
//+------------------------------------------------------------------+
#include "aCandlePatterns.mqh"
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on Morning/Evening Stars                     |
//| confirmed by CCI                                                 |
//| Type=SignalAdvanced                                              |
//| Name=CMS_ES_CCI                                                  |
//| Class=CMS_ES_CCI                                                 |
//| Page=                                                            |
//| Parameter=PeriodCCI,int,25,Period of CCI                         |
//| Parameter=PeriodMA,int,5, Period of MA                           |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| CMS_ES_CCI Class.                                                |
//| Purpose: Trading signals class, based on                         |
//| the "Morning/Evening Stars"                                      |
//| Japanese Candlestick Patterns                                    |
//| with confirmation by CCI indicator                               |
//| Derived from CCandlePattern class.                               |
//+------------------------------------------------------------------+
class CMS_ES_CCI : public CCandlePattern
  {
protected:
   CiCCI             m_CCI;            // object-CCI
   //--- adjusted parameters
   int               m_periodCCI;      // the "period of calculation" parameter of the oscillator
   ENUM_APPLIED_PRICE m_applied;       // the "prices series" parameter of the oscillator

public:
                     CMS_ES_CCI();
   //--- methods of setting adjustable parameters
   void              PeriodCCI(int value)              { m_periodCCI=value;           }
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
   bool              InitCCI(CIndicators *indicators);
   //--- methods of getting data
   double            CCI(int ind)                       { return(m_CCI.Main(ind));     }
  };
//+------------------------------------------------------------------+
//| Constructor CMS_ES_CCI.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CMS_ES_CCI::CMS_ES_CCI()
  {
//--- initialization of protected data
   m_used_series=USE_SERIES_HIGH+USE_SERIES_LOW;
//--- setting default values for the oscillator parameters
   m_periodCCI=14;
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CMS_ES_CCI::ValidationSettings()
  {
//--- validation settings of additional filters
   if(!CCandlePattern::ValidationSettings()) return(false);
//--- initial data checks
   if(m_periodCCI<=0)
     {
      printf(__FUNCTION__+": period of the CCI oscillator must be greater than 0");
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
bool CMS_ES_CCI::InitIndicators(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- initialization of indicators and timeseries of additional filters
   if(!CCandlePattern::InitIndicators(indicators)) return(false);
//--- create and initialize CCI oscillator
   if(!InitCCI(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize CCI oscillators.                                      |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CMS_ES_CCI::InitCCI(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- add object to collection
   if(!indicators.Add(GetPointer(m_CCI)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize object
   if(!m_CCI.Create(m_symbol.Name(),m_period,m_periodCCI,m_applied))
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
int CMS_ES_CCI::LongCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Morning Star and CCI<-50
   if(CheckCandlestickPattern(CANDLE_PATTERN_MORNING_STAR) && (CCI(1)<-50))
      result=80;
//--- check conditions of short position closing
   if(((CCI(1)>-80) && (CCI(2)<-80)) || ((CCI(1)<80) && (CCI(2)>80)))
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
int CMS_ES_CCI::ShortCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Evening Star pattern and CCI>50
   if(CheckCandlestickPattern(CANDLE_PATTERN_EVENING_STAR) && (CCI(1)>50))
      result=80;
//--- check conditions of long position closing
   if(((CCI(1)<80) && (CCI(2)>80)) || ((CCI(1)<-80) && (CCI(2)>-80)))
      result=40;
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+
