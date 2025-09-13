//+------------------------------------------------------------------+
//|                                     SignalEnvelopes-jpalonso.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2011.03.30 |
//|                                          Modified  2012, alohafx |
//|                                   http://alohafx.blog36.fc2.com/ |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals of indicator 'Envelopes'                           |
//| Type=SignalAdvanced                                              |
//| Name=Envelopes                                                   |
//| ShortName=Envelopes                                              |
//| Class=CSignalEnvelopes                                           |
//| Page=signal_envelopes                                            |
//| Parameter=PeriodMA,int,45,Period of averaging                    |
//| Parameter=Shift,int,0,Time shift                                 |
//| Parameter=Method,ENUM_MA_METHOD,MODE_SMA,Method of averaging     |
//| Parameter=Applied,ENUM_APPLIED_PRICE,PRICE_CLOSE,Prices series   |
//| Parameter=Deviation,double,0.15,Deviation                        |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalEnvelopes.                                          |
//| Purpose: Class of generator of trade signals based on            |
//|          the 'Envelopes' indicator.                              |
//| Is derived from the CExpertSignal class.                         |
//+------------------------------------------------------------------+
class CSignalEnvelopes : public CExpertSignal
  {
protected:
   CiEnvelopes       m_env;            // object-indicator
   //--- adjusted parameters
   int               m_ma_period;      // the "period of averaging" parameter of the indicator
   int               m_ma_shift;       // the "time shift" parameter of the indicator
   ENUM_MA_METHOD    m_ma_method;      // the "method of averaging" parameter of the indicator
   ENUM_APPLIED_PRICE m_ma_applied;    // the "object of averaging" parameter of the indicator
   double            m_deviation;      // the "deviation" parameter of the indicator
   double            m_limit_in;       // threshold sensitivity of the 'rollback zone'
   double            m_limit_out;      // threshold sensitivity of the 'break through zone'
   //--- "weights" of market models (0-100)
   int               m_pattern_0;      // model 0 "price is near the necessary border of the envelope"
   int               m_pattern_1;      // model 1 "price crossed a border of the envelope"

public:
                     CSignalEnvelopes();
   //--- methods of setting adjustable parameters
   void              PeriodMA(int value)                 { m_ma_period=value;        }
   void              Shift(int value)                    { m_ma_shift=value;         }
   void              Method(ENUM_MA_METHOD value)        { m_ma_method=value;        }
   void              Applied(ENUM_APPLIED_PRICE value)   { m_ma_applied=value;       }
   void              Deviation(double value)             { m_deviation=value;        }
   void              LimitIn(double value)               { m_limit_in=value;         }
   void              LimitOut(double value)              { m_limit_out=value;        }
   //--- methods of adjusting "weights" of market models
   void              Pattern_0(int value)                { m_pattern_0=value;        }
   void              Pattern_1(int value)                { m_pattern_1=value;        }
   //--- method of verification of settings
   virtual bool      ValidationSettings();
   //--- method of creating the indicator and timeseries
   virtual bool      InitIndicators(CIndicators* indicators);
   //--- methods of checking if the market models are formed
   virtual int       LongCondition();
   virtual int       ShortCondition();

protected:
   //--- method of initialization of the indicator
   bool              InitMA(CIndicators* indicators);
   //--- methods of getting data
   double            Upper(int ind)                      { return(m_env.Upper(ind)); }
   double            Lower(int ind)                      { return(m_env.Lower(ind)); }
  };
//+------------------------------------------------------------------+
//| Constructor CSignalEnvelopes.                                    |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalEnvelopes::CSignalEnvelopes()
  {
//--- initialization of protected data
   m_used_series=USE_SERIES_OPEN+USE_SERIES_HIGH+USE_SERIES_LOW+USE_SERIES_CLOSE;
//--- setting default values for the indicator parameters
   m_ma_period =45;
   m_ma_shift  =0;
   m_ma_method =MODE_SMA;
   m_ma_applied=PRICE_CLOSE;
   m_deviation =0.15;
//---
/*   m_limit_in  =0.0;
   m_limit_out =10;
//--- setting default "weights" of the market models
   m_pattern_0 =90;          // model 0 "price is near the necessary border of the envelope"
   m_pattern_1 =0;          // model 1 "price crossed a border of the envelope"
*/  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalEnvelopes::ValidationSettings()
  {
//--- validation settings of additional filters
   if(!CExpertSignal::ValidationSettings()) return(false);
//--- initial data checks
   if(m_ma_period<=0)
     {
      printf(__FUNCTION__+": period MA must be greater than 0");
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
bool CSignalEnvelopes::InitIndicators(CIndicators* indicators)
  {
//--- check pointer
   if(indicators==NULL)                           return(false);
//--- initialization of indicators and timeseries of additional filters
   if(!CExpertSignal::InitIndicators(indicators)) return(false);
//--- create and initialize MA indicator
   if(!InitMA(indicators))                        return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize MA indicators.                                        |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalEnvelopes::InitMA(CIndicators* indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- add object to collection
   if(!indicators.Add(GetPointer(m_env)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize object
   if(!m_env.Create(m_symbol.Name(),m_period,m_ma_period,m_ma_shift,m_ma_method,m_ma_applied,m_deviation))
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
int CSignalEnvelopes::LongCondition()
  {
   int result=0;
   int idx   =StartIndex();
   double close=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
   double upper=Upper(idx);
   double lower=Lower(idx);
   double width=upper-lower;
   double ma=lower+width/2;
   if(close<=lower || (close<upper && close>ma))
      result=100;
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+
//| "Voting" that price will fall.                                   |
//| INPUT:  no.                                                      |
//| OUTPUT: number of "votes" that price will fall.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CSignalEnvelopes::ShortCondition()
  {
   int result  =0;
   int idx     =StartIndex();
   double close=SymbolInfoDouble(Symbol(),SYMBOL_BID);
   double upper=Upper(idx);
   double lower=Lower(idx);
   double width=upper-lower;
   double ma=lower+width/2;
   if(close>=upper || (close>lower && close<ma))
      result=100;
//--- return the result
   return(result);
  }
//+------------------------------------------------------------------+
