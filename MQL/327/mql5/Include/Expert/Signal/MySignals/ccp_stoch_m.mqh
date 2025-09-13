//+------------------------------------------------------------------+
//|                                                    CCP_Stoch.mqh |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//|                                              Revision 2011.04.19 |
//+------------------------------------------------------------------+
#include "CandlePatterns.mqh"
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on Candlestick Patterns+Stochastic           |
//| Type=SignalAdvanced                                              |
//| Name=CCP_Stoch                                                   |
//| Class=CCP_Stoch                                                  |
//| Page=                                                            |
//| Parameter=StochPeriodK,int,33                                    |
//| Parameter=StochPeriodD,int,37                                    |
//| Parameter=StochPeriodSlow,int,30                                 |
//| Parameter=StochApplied,ENUM_STO_PRICE,STO_LOWHIGH                |
//| Parameter=MAPeriod,int,25                                        |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| CCP_Stoch Class.                                                 |
//| Purpose: Trading signals class, based on                         |
//| Japanese Candlestick Patterns                                    |
//| with confirmation by Stochastic indicator                        |
//| Derived from CCandlePattern class.                               |
//+------------------------------------------------------------------+
class CCP_Stoch : public CCandlePattern
  {
protected:
   CiStochastic      m_stoch;
   CPriceSeries     *m_app_price_high;
   CPriceSeries     *m_app_price_low;
   //--- input parameters
   int               m_periodK;
   int               m_periodD;
   int               m_period_slow;
   ENUM_STO_PRICE    m_applied;
   //--- "weights" of market models (0-100)
   int               m_pattern_0;      // model 0 "market entry"
   int               m_pattern_1;      // model 1 "market exit"

public:
   //--- class constructor
                     CCP_Stoch();
   //--- input parameters initialization methods
   void              StochPeriodK(int period)              { m_periodK=period;            }
   void              StochPeriodD(int period)              { m_periodD=period;            }
   void              StochPeriodSlow(int period)           { m_period_slow=period;        }
   void              StochApplied(ENUM_STO_PRICE applied)  { m_applied=applied;           }
   //--- methods of adjusting "weights" of market models
   void              Pattern_0(int value)                  { m_pattern_0=value;           }
   void              Pattern_1(int value)                  { m_pattern_1=value;           }

   //--- initialization of indicators
   virtual bool      ValidationSettings();
   virtual bool      InitIndicators(CIndicators *indicators);

   //--- methods of checking if the market models are formed
   virtual int       LongCondition();
   virtual int       ShortCondition();

protected:
   //--- Stochastic initialization method
   bool              InitStoch(CIndicators *indicators);
   //--- lines of Stochastic indicator
   double            StochMain(int ind)         const      { return(m_stoch.Main(ind));   }
   double            StochSignal(int ind)       const      { return(m_stoch.Signal(ind)); }
  };
//+------------------------------------------------------------------+
//| CCP_Stoch class constructor.                                  |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CCP_Stoch::CCP_Stoch()
  {
//--- set default inputs
   m_periodK    =7;
   m_periodD    =11;
   m_period_slow=15;
   m_applied    =STO_LOWHIGH;
//--- setting default "weights" of the market models
   m_pattern_0 =70;          // model 0 "market entry"
   m_pattern_1 =40;          // model 1 "market exit" 

  }
//+------------------------------------------------------------------+
//| Validation settings.                                             |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CCP_Stoch::ValidationSettings()
  {
   if(!CCandlePattern::ValidationSettings()) return(false);
//--- initial input parameters
   if(m_periodK<=0)
     {
      printf(__FUNCTION__+": period %K Stochastic must be greater than 0");
      return(false);
     }
   if(m_periodD<=0)
     {
      printf(__FUNCTION__+": period %D Stochastic must be greater than 0");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create indicators.                                               |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CCP_Stoch::InitIndicators(CIndicators *indicators)
  {
//--- check
   if(indicators==NULL) return(false);
   if(!CCandlePattern::InitIndicators(indicators)) return(false);
//--- create and initialize Stochastic indicator
   if(!InitStoch(indicators)) return(false);
   if(m_applied==STO_CLOSECLOSE)
     {
      //--- copy Close series
      m_app_price_high=GetPointer(m_close);
      //--- copy Close series
      m_app_price_low=GetPointer(m_close);
     }
   else
     {
      //--- copy High series
      m_app_price_high=GetPointer(m_high);
      //--- copy Low series
      m_app_price_low=GetPointer(m_low);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create Stochastic indicators.                                    |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CCP_Stoch::InitStoch(CIndicators *indicators)
  {
//--- add Stochastic indicator to collection
   if(!indicators.Add(GetPointer(m_stoch)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize Stochastic indicator
   if(!m_stoch.Create(m_symbol.Name(),m_period,m_periodK,m_periodD,m_period_slow,MODE_SMA,m_applied))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
   m_stoch.BufferResize(50);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Method of checking if the market models are formed               |
//| Checks conditions for                                            | 
//| entry (open long position, m_pattern_0)                          |
//| exit  (close short position, m_pattern_1)                        |
//+------------------------------------------------------------------+
int CCP_Stoch::LongCondition()
  {
   int res=0;
//--- check conditions to open long position
//--- formation of bullish pattern and signal line of Stochastic indicator<30
   if(CheckPatternAllBullish() && (StochSignal(1)<30)) res=m_pattern_0; // signal to open long positon

//--- check conditions of short position closing
//--- formation of bullish pattern or crossover of the signal line (upward 20, upward 80)
   if(CheckPatternAllBullish() ||
      ((StochSignal(1)>20) && (StochSignal(2)<20)) || 
      ((StochSignal(1)>80) && (StochSignal(2)<80)))    res=m_pattern_1; // signal to close short position
//---
   return(res);
  }
//+------------------------------------------------------------------+
//| Method of checking if the market models are formed               |
//| Checks conditions for                                            | 
//| entry (open short position, m_pattern_0)                         |
//| exit  (close long position, m_pattern_1)                         |
//+------------------------------------------------------------------+
int CCP_Stoch::ShortCondition()
  {
   int res=0;
//--- check conditions to open short position
//--- formation of bearish pattern and signal line of Stochastic indicator>70     
   if(CheckPatternAllBearish() && (StochSignal(1)>70)) res=m_pattern_0; // signal to open short positon

//--- check conditions of long position closing
//--- formation of bearish pattern or crossover of the signal line (downward 80, downward 20)
   if(CheckPatternAllBearish() || 
      ((StochSignal(1)<80) && (StochSignal(2)>80)) || 
      ((StochSignal(1)<20) && (StochSignal(2)>20)))    res=m_pattern_1; // signal to close long position
//---
   return(res);
  }
//+------------------------------------------------------------------+

