//+------------------------------------------------------------------+
//|                                              SignalAlligator.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.10.12 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on the Alligator                             |
//| Type=Signal                                                      |
//| Name=Alligator                                                   |
//| Class=CSignalAlligator                                           |
//| Page=                                                            |
//| Parameter=JawPeriod,int,13                                       |
//| Parameter=JawShift,int,8                                         |
//| Parameter=TeethPeriod,int,8                                      |
//| Parameter=TeethShift,int,5                                       |
//| Parameter=LipsPeriod,int,5                                       |
//| Parameter=LipsShift,int,3                                        |
//| Parameter=MaMethod,ENUM_MA_METHOD,MODE_SMMA                      |
//| Parameter=Applied,ENUM_APPLIED_PRICE,PRICE_MEDIAN                |
//| Parameter=CrossMeasure,int,5                                     |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalAlligator.                                          |
//| Appointment: Class trading signals Alligator.                    |
//|              Derives from class CExpertSignal.                   |
//+------------------------------------------------------------------+
class CSignalAlligator : public CExpertSignal
  {
protected:
   CiAlligator      *m_alligator;
   bool              m_crossed;
   double            m_delta;
   //--- input parameters
   int               m_jaw_period;
   int               m_jaw_shift;
   int               m_teeth_period;
   int               m_teeth_shift;
   int               m_lips_period;
   int               m_lips_shift;
   ENUM_MA_METHOD    m_ma_method;
   ENUM_APPLIED_PRICE m_applied;
   int               m_cross_measure;

public:
                     CSignalAlligator();
                    ~CSignalAlligator();
   //--- methods initialize protected data
   void              JawPeriod(int period)               { m_jaw_period=period;            }
   void              JawShift(int shift)                 { m_jaw_shift=shift;              }
   void              TeethPeriod(int period)             { m_teeth_period=period;          }
   void              TeethShift(int shift)               { m_teeth_shift=shift;            }
   void              LipsPeriod(int period)              { m_lips_period=period;           }
   void              LipsShift(int shift)                { m_lips_shift=shift;             }
   void              MaMethod(ENUM_MA_METHOD method)     { m_ma_method=method;             }
   void              Applied(ENUM_APPLIED_PRICE applied) { m_applied=applied;              }
   void              CrossMeasure(int measure);
   virtual bool      InitIndicators(CIndicators* indicators);
   virtual bool      ValidationSettings();
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);

protected:
   double            Jaw(int ind)                        { return(m_alligator.Jaw(ind));   }
   double            Teeth(int ind)                      { return(m_alligator.Teeth(ind)); }
   double            Lips(int ind)                       { return(m_alligator.Lips(ind));  }
   double            LipsTeethDiff(int ind)              { return(Lips(ind)-Teeth(ind));   }
   double            TeethJawDiff(int ind)               { return(Teeth(ind)-Jaw(ind));    }
   double            LipsJawDiff(int ind)                { return(Lips(ind)-Jaw(ind));     }
   bool              CheckCross();
  };
//+------------------------------------------------------------------+
//| Constructor CSignalAlligator.                                    |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalAlligator::CSignalAlligator()
  {
//--- initialize protected data
   m_alligator    =NULL;
   m_crossed      =false;
   m_delta        =0.0;
//--- set default inputs
   m_jaw_period   =13;
   m_jaw_shift    =8;
   m_teeth_period =8;
   m_teeth_shift  =5;
   m_lips_period  =5;
   m_lips_shift   =3;
   m_ma_method    =MODE_SMMA;
   m_applied      =PRICE_MEDIAN;
   m_cross_measure=5;
  }
//+------------------------------------------------------------------+
//| Destructor CSignalAlligator.                                     |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalAlligator::~CSignalAlligator()
  {
   //---
  }
//+------------------------------------------------------------------+
//| Set m_cross_measure member.                                      |
//| INPUT:  measure - new value.                                     |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalAlligator::CrossMeasure(int measure)
  {
   m_cross_measure=measure;
   m_delta        =measure*m_adjusted_point;
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalAlligator::ValidationSettings()
  {
//--- initial data checks
   if(m_jaw_period<=m_teeth_period || m_teeth_period<=m_lips_period)
     {
      printf(__FUNCTION__+": check the periods of the alligator");
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
bool CSignalAlligator::InitIndicators(CIndicators* indicators)
  {
//--- check
   if(indicators==NULL) return(false);
//--- create Alligator indicator
   if(m_alligator==NULL)
      if((m_alligator=new CiAlligator)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add Alligator indicator to collection
   if(!indicators.Add(m_alligator))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_alligator;
      return(false);
     }
//--- initialize Alligator indicator
   if(!m_alligator.Create(m_symbol.Name(),m_period,
      m_jaw_period,  m_jaw_shift, m_teeth_period,m_teeth_shift,
      m_lips_period, m_lips_shift,m_ma_method,   m_applied))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Check the lines crossing the alligator.                          |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if lines crossing, false otherwise.                 |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalAlligator::CheckCross()
  {
   if(!m_crossed) return(m_crossed);
//---
   if(MathAbs(LipsTeethDiff(1))>m_delta || 
      MathAbs(TeethJawDiff(1)) >m_delta ||
      MathAbs(LipsJawDiff(1))  >m_delta)      m_crossed=false;
//---
   return(m_crossed);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position open.                         |
//| INPUT:  price      - refernce for price,                         |
//|         sl         - refernce for stop loss,                     |
//|         tp         - refernce for take profit,                   |
//|         expiration - refernce for expiration.                    |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalAlligator::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(CheckCross()) return(false);
//---
   price=0.0;
   sl   =0.0;
   tp   =0.0;
//---
   if(LipsTeethDiff(-2)>=LipsTeethDiff(-1) && LipsTeethDiff(-1)>=LipsTeethDiff(0) && LipsTeethDiff(0)>=0.0 &&
      TeethJawDiff(-2) >=TeethJawDiff(-1)  && TeethJawDiff(-1) >=TeethJawDiff(0)  && TeethJawDiff(0) >=0.0)
      m_crossed=true;
//---
   return(m_crossed);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalAlligator::CheckCloseLong(double& price)
  {
   price=0.0;
//---
   return(LipsTeethDiff(-1)<0 && LipsTeethDiff(0)>=0 && LipsTeethDiff(1)>0);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position open.                        |
//| INPUT:  price      - refernce for price,                         |
//|         sl         - refernce for stop loss,                     |
//|         tp         - refernce for take profit,                   |
//|         expiration - refernce for expiration.                    |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalAlligator::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(CheckCross()) return(false);
//---
   price=0.0;
   sl   =0.0;
   tp   =0.0;
//---
   if(LipsTeethDiff(-2)<=LipsTeethDiff(-1) && LipsTeethDiff(-1)<=LipsTeethDiff(0) && LipsTeethDiff(0)<=0.0 &&
      TeethJawDiff(-2) <=TeethJawDiff(-1)  && TeethJawDiff(-1) <=TeethJawDiff(0)  && TeethJawDiff(0) <=0.0)
      m_crossed=true;
//---
   return(m_crossed);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalAlligator::CheckCloseShort(double& price)
  {
   price=0.0;
//---
   return(LipsTeethDiff(-1)>0 && LipsTeethDiff(0)<=0 && LipsTeethDiff(1)<0);
  }
//+------------------------------------------------------------------+
