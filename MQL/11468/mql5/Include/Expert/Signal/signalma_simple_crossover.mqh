//+------------------------------------------------------------------+
//|                                           SignalMA_Crossover.mqh |
//|                         Adapted from SignalMA.mqh from MetaQuotes|
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                              Revision 2012.08.31 |
//|   The basic idea of this simple MA crossover system is the       |
//|   utilization and possible use within the Expert Wizard          |
//|   Onlye the Direction() function is implementated which has a    |
//|   simple buy and sell signaling when the fast is crossing the    |
//|   down moving average signal.                                    |
//|   In addition the timeframe can be adjusted as input parameter.  |
//|   The file servers as a training starting point for              | 
//|   -> programming own signaling which can be used in the Wizard   | 
//|   -> teaching how to adjust the parameters                       | 
//|   see http://www.blackboxtrading.de for more training information|
//|   (under preperation)                                            |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals of indicator 'Moving Average Crossover'            |
//| Type=SignalAdvanced                                              |
//| Name=Moving Average Crossover                                    |
//| ShortName=MACROSS                                                |
//| Class=CSignalMAC                                                 |
//| Page=signal_ma                                                   |
//| Parameter=PeriodMA1,int,12,Period of fast averaging              |
//| Parameter=PeriodMA2,int,240,Period of slow averaging             |
//| Parameter=Shift,int,0,Time shift                                 |
//| Parameter=Method,ENUM_MA_METHOD,MODE_SMA,Method of averaging     |
//| Parameter=Applied,ENUM_APPLIED_PRICE,PRICE_CLOSE,Prices series   |
//| Parameter=Set_MAC_timeframe,ENUM_TIMEFRAMES,PERIOD_H1            |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalMAC.                                                |
//| Purpose: Class of generator of trade signals based on            |
//|          the 'Moving Average' indicator.                         |
//| Is derived from the CExpertSignal class.                         |
//+------------------------------------------------------------------+
class CSignalMAC : public CExpertSignal
  {
protected:
   CiMA              m_ma1;            // object-indicator
   CiMA              m_ma2;            // object-indicator
   //--- adjusted parameters
   int               m_ma_period1;     // the "period of averaging" parameter of the indicator
   int               m_ma_period2;     // the "period of averaging" parameter of the indicator
   int               m_ma_shift;       // the "time shift" parameter of the indicator
   ENUM_MA_METHOD    m_ma_method;      // the "method of averaging" parameter of the indicator
   ENUM_APPLIED_PRICE m_ma_applied;    // the "object of averaging" parameter of the indicator
   ENUM_TIMEFRAMES   m_mac_period;

public:
                     CSignalMAC();
   //--- methods of setting adjustable parameters
   void              PeriodMA1(int value)                { m_ma_period1=value;         }
   void              PeriodMA2(int value)                { m_ma_period2=value;         }
   void              Shift(int value)                    { m_ma_shift=value;           }
   void              Method(ENUM_MA_METHOD value)        { m_ma_method=value;          }
   void              Applied(ENUM_APPLIED_PRICE value)   { m_ma_applied=value;         }
   void              Set_MAC_timeframe(ENUM_TIMEFRAMES value) {m_mac_period=value;}

   //--- method of verification of settings
   virtual bool      ValidationSettings();
   //--- method of creating the indicator and timeseries
   virtual bool      InitIndicators(CIndicators *indicators);
   //--- methods of checking if the market models are formed
   // --- attention these functions are currently not utilized (13.08.12) !!
   virtual int       LongCondition();
   virtual int       ShortCondition();
   // currently only the Direction module is utilized (13.08.12) !!
   virtual double    Direction();
protected:
   //--- method of initialization of the indicator
   bool              InitMA1(CIndicators *indicators);
   bool              InitMA2(CIndicators *indicators);
   //--- methods of getting data
   double            MA1(int ind)                         { return(m_ma1.Main(ind));     }
   double            MA2(int ind)                         { return(m_ma2.Main(ind));     }
   double            DiffMA1(int ind)                     { return(MA1(ind)-MA1(ind+1)); }
   double            DiffOpenMA1(int ind)                 { return(Open(ind)-MA1(ind));  }
   double            DiffHighMA1(int ind)                 { return(High(ind)-MA1(ind));  }
   double            DiffLowMA1(int ind)                  { return(Low(ind)-MA1(ind));   }
   double            DiffCloseMA1(int ind)                { return(Close(ind)-MA1(ind)); }
   double            DiffMA2(int ind)                     { return(MA2(ind)-MA2(ind+1)); }
   double            DiffOpenMA2(int ind)                 { return(Open(ind)-MA2(ind));  }
   double            DiffHighMA2(int ind)                 { return(High(ind)-MA2(ind));  }
   double            DiffLowMA2(int ind)                  { return(Low(ind)-MA2(ind));   }
   double            DiffCloseMA2(int ind)                { return(Close(ind)-MA2(ind)); }
  };
//+------------------------------------------------------------------+
//| Constructor CSignalMAC.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalMAC::CSignalMAC()
  {
//--- initialization of protected data
   m_used_series=USE_SERIES_OPEN+USE_SERIES_HIGH+USE_SERIES_LOW+USE_SERIES_CLOSE;
//--- setting default values for the indicator parameters
   m_ma_period1 =12;
   m_ma_period2 =24;
   m_ma_shift  =0;
   m_ma_method =MODE_SMA;
   m_ma_applied=PRICE_CLOSE;

  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalMAC::ValidationSettings()
  {
//--- validation settings of additional filters
   if(!CExpertSignal::ValidationSettings()) return(false);
//--- initial data checks
   if(m_ma_period1<=0)
     {
      printf(__FUNCTION__+": period MA1 must be greater than 0");
      return(false);
     }
   if(m_ma_period2<=0)
     {
      printf(__FUNCTION__+": period MA2 must be greater than 0");
      return(false);
     }
   if(m_ma_period1>=m_ma_period2)
     {
      printf(__FUNCTION__+": MA1 period has to be smaller than period MA2 ");
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
bool CSignalMAC::InitIndicators(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- initialization of indicators and timeseries of additional filters
   if(!CExpertSignal::InitIndicators(indicators)) return(false);
//--- create and initialize MA indicator
   if(!InitMA1(indicators))                        return(false);
   if(!InitMA2(indicators))                        return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize MA1 indicators.                                       |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalMAC::InitMA1(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- add object to collection
   if(!indicators.Add(GetPointer(m_ma1)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize object
   if(!m_ma1.Create(m_symbol.Name(),m_mac_period,m_ma_period1,m_ma_shift,m_ma_method,m_ma_applied))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize MA2 indicators.                                       |
//| INPUT:  indicators - pointer of indicator collection.            |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalMAC::InitMA2(CIndicators *indicators)
  {
//--- check pointer
   if(indicators==NULL) return(false);
//--- add object to collection
   if(!indicators.Add(GetPointer(m_ma2)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize object
   if(!m_ma2.Create(m_symbol.Name(),m_mac_period,m_ma_period2,m_ma_shift,m_ma_method,m_ma_applied))
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
int CSignalMAC::LongCondition()
  {

//--- return the result
   return(0);
  }
//+------------------------------------------------------------------+
//| "Voting" that price will fall.                                   |
//| INPUT:  no.                                                      |
//| OUTPUT: number of "votes" that price will fall.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CSignalMAC::ShortCondition()
  {
   return(0);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|   Direction +100 for up -100 for down                               
//|   the rules itself are pretty simple and depends on two signals
//|   slow MA: main trend signal line
//|   fast MA: if fast crosses slow a signal is generated depending on:
//+------------------------------------------------------------------+
double CSignalMAC::Direction()
  {

// Print(" Check direction :");
   double direction=0;  //the direction weight is need for activation basis +100 for long and -100 for short
                        //overall weighted size has to be large than Signal_ThresholdOpen or Signal_ThresholdClose


   double K_fast=MA1(0);
   double K_fast_old1=MA1(1);
   double K_fast_old2=MA1(2);

   double K_slow=MA2(0);
   double K_slow_old1=MA2(1);
   double K_slow_old2=MA2(2);




// just look for simple ricing or falling --> move this to  function update fields   
   bool cond1_up=K_fast>K_fast_old1; //up 
   bool cond1_down=K_fast<K_fast_old1; //down

   bool cond2_up=(K_fast>K_slow)&(K_fast_old1<=K_slow); //up crossing
   bool cond2_down=(K_fast<K_slow)&(K_fast_old1>=K_slow); //down crossing

                                                          // condition 3 and 4 not used currently
   bool cond3_up=K_fast>K_fast_old2; //up 
   bool cond3_down=K_fast<K_fast_old2; //down

   bool cond4_up=(K_fast>K_slow)&(K_fast_old2<=K_slow); //up crossing
   bool cond4_down=(K_fast<K_slow)&(K_fast_old2>=K_slow); //down crossing
                                                          //////Print(string(cond1)+"  ----: "+string(!cond1));

   if(cond1_up&cond2_up)
     {
      direction=100; //allow for long realy?
     }
   if((cond1_down)&(cond2_down))
     {
      direction=-100; //allow for short
     }

   direction=direction;
// Print(" Direction value: "+direction);
   return direction;
  }
//+------------------------------------------------------------------+
