//+------------------------------------------------------------------+
//|                                                aCBC_WS_Stoch.mqh |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//|                                              Revision 2011.11.10 |
//+------------------------------------------------------------------+
#include "ACandlePatterns.mqh"
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on 3 Black Crows/3 White Soldiers            |
//| confirmed by Stochastic                                          |
//| Type=SignalAdvanced                                              |
//| Name=CBC_WS_Stoch                                                |
//| Class=CBC_WS_Stoch                                               |
//| Page=                                                            |
//| Parameter=StochPeriodK,int,47                                    |
//| Parameter=StochPeriodD,int,9                                     |
//| Parameter=StochPeriodSlow,int,13                                 |
//| Parameter=StochApplied,ENUM_STO_PRICE,STO_LOWHIGH                |
//| Parameter=MAPeriod,int,5                                         |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| CBC_WS_Stoch Class.                                              |
//| Purpose: Trading signals class, based on                         |
//| the "3 Black Crows/3 White Soldiers"                             |
//| Japanese Candlestick Patterns                                    |
//| with confirmation by Stochastic indicator                        |
//| Derived from CCandlePattern class.                               |
//+------------------------------------------------------------------+
class CBC_WS_Stoch : public CCandlePattern
  {
protected:
   CiStochastic      m_stoch;
   //--- input parameters
   int               m_periodK;
   int               m_periodD;
   int               m_period_slow;
   ENUM_STO_PRICE    m_applied;

public:
   //--- class constructor
                     CBC_WS_Stoch();
   //--- input parameters initialization methods
   void              StochPeriodK(int period)              { m_periodK=period;            }
   void              StochPeriodD(int period)              { m_periodD=period;            }
   void              StochPeriodSlow(int period)           { m_period_slow=period;        }
   void              StochApplied(ENUM_STO_PRICE applied)  { m_applied=applied;           }
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
//| CBC_WS_Stoch class constructor.                                  |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CBC_WS_Stoch::CBC_WS_Stoch()
  {
//--- set default inputs
   m_periodK    =7;
   m_periodD    =11;
   m_period_slow=15;
   m_applied    =STO_LOWHIGH;
  }
//+------------------------------------------------------------------+
//| Validation settings.                                             |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CBC_WS_Stoch::ValidationSettings()
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
bool CBC_WS_Stoch::InitIndicators(CIndicators *indicators)
  {
//--- check
   if(indicators==NULL) return(false);
   if(!CCandlePattern::InitIndicators(indicators)) return(false);
//--- create and initialize Stochastic indicator
   if(!InitStoch(indicators))                      return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create Stochastic indicators.                                    |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CBC_WS_Stoch::InitStoch(CIndicators *indicators)
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
//| "Voting" that price will grow.                                   |
//| INPUT:  no.                                                      |
//| OUTPUT: number of "votes" that price will grow.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CBC_WS_Stoch::LongCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Three White Soldiers pattern and
//--- check signal line of stochastic indicator
  if (CheckCandlestickPattern(CANDLE_PATTERN_THREE_WHITE_SOLDIERS) && (StochSignal(1)<30))
     result=80;
//--- check conditions of short position closing
   if((((StochSignal(1)>20) && (StochSignal(2)<20)) ||
       ((StochSignal(1)>80) && (StochSignal(2)<80))))
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
int CBC_WS_Stoch::ShortCondition()
  {
   int result=0;
   int idx   =StartIndex();
//--- check formation of Three Black Crows pattern and
//--- signal line of stochastic indicator>70
  if (CheckCandlestickPattern(CANDLE_PATTERN_THREE_BLACK_CROWS) && (StochSignal(1)>70))
     result=80;
//--- check conditions of long position closing
   if((((StochSignal(1)<80) && (StochSignal(2)>80)) ||
       ((StochSignal(1)<20) && (StochSignal(2)>20))))
     result=40;
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+