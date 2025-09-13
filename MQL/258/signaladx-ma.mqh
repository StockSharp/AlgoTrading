//+------------------------------------------------------------------+
//|                                                 SignalADX-MA.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.10.12 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on price crossover with MA confirmed by ADX  |
//| Type=Signal                                                      |
//| Name=ADX_MA                                                      |
//| Class=CSignalADX_MA                                              |
//| Page=                                                            |
//| Parameter=PeriodADX,int,8                                        |
//| Parameter=MinimumADX,double,22.0                                 |
//| Parameter=PeriodMA,int,8                                         |
//| Parameter=StopLoss,int,30                                        |
//| Parameter=TakeProfit,int,100                                     |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalADX_MA.                                             |
//| Appointment: Class trading signals cross price and MA with ADX.  |
//|              Derives from class CExpertSignal.                   |
//+------------------------------------------------------------------+
class CSignalADX_MA : public CExpertSignal
  {
protected:
   CiADX            *m_ADX;
   CiMA             *m_EMA;
   CiClose          *m_close;
   //--- input parameters
   int               m_period_ADX;
   double            m_minimum_ADX;
   int               m_period_MA;
   int               m_stop_loss;
   int               m_take_profit;

public:
                     CSignalADX_MA();
                    ~CSignalADX_MA();
   //--- methods initialize protected data
   void              PeriodADX(int period)       { m_period_ADX=period;                }
   void              MinimumADX(double minimum)  { m_minimum_ADX=minimum;              }
   void              PeriodMA(int period)        { m_period_MA=period;                 }
   void              StopLoss(int stop_loss)     { m_stop_loss=stop_loss;              }
   void              TakeProfit(int take_profit) { m_take_profit=take_profit;          }
   virtual bool      InitIndicators(CIndicators* indicators);
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);

protected:
   bool              InitADX(CIndicators* indicators);
   bool              InitEMA(CIndicators* indicators);
   bool              InitClose(CIndicators* indicators);
   //---
   double            PlusADX(int ind)            { return(m_ADX.Plus(ind));            }
   double            MainADX(int ind)            { return(m_ADX.Main(ind));            }
   double            MinusADX(int ind)           { return(m_ADX.Minus(ind));           }
   double            EMA(int ind)                { return(m_EMA.Main(ind));            }
   double            Close(int ind)              { return(m_close.GetData(ind));       }
   double            StateADX(int ind)           { return(PlusADX(ind)-MinusADX(ind)); }
   double            StateEMA(int ind)           { return(EMA(ind)-EMA(ind+1));        }
   double            StateClose(int ind)         { return(Close(ind)-EMA(ind));        }
  };
//+------------------------------------------------------------------+
//| Constructor CSignalADX_MA.                                       |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalADX_MA::CSignalADX_MA()
  {
//--- initialize protected data
   m_EMA        =NULL;
   m_ADX        =NULL;
   m_close      =NULL;
//--- set default inputs
   m_period_ADX =8;
   m_minimum_ADX=22.0;
   m_period_MA  =8;
   m_stop_loss  =30;
   m_take_profit=100;
  }
//+------------------------------------------------------------------+
//| Destructor CSignalADX_MA.                                        |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalADX_MA::~CSignalADX_MA()
  {
//---
  }
//+------------------------------------------------------------------+
//| Create indicators.                                               |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalADX_MA::InitIndicators(CIndicators* indicators)
  {
//--- check
   if(indicators==NULL)       return(false);
//--- create and initialize ADX indicator
   if(!InitADX(indicators))   return(false);
//--- create and initialize EMA indicator
   if(!InitEMA(indicators))   return(false);
//--- create and initialize close series
   if(!InitClose(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create ADX indicator.                                            |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalADX_MA::InitADX(CIndicators* indicators)
  {
//--- create ADX indicator
   if(m_ADX==NULL)
      if((m_ADX=new CiADX)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add ADX indicator to collection
   if(!indicators.Add(m_ADX))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_ADX;
      return(false);
     }
//--- initialize ADX indicator
   if(!m_ADX.Create(m_symbol.Name(),m_period,m_period_ADX))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create EMA indicators.                                           |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalADX_MA::InitEMA(CIndicators* indicators)
  {
//--- create EMA indicator
   if(m_EMA==NULL)
      if((m_EMA=new CiMA)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add EMA indicator to collection
   if(!indicators.Add(m_EMA))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_EMA;
      return(false);
     }
//--- initialize EMA indicator
   if(!m_EMA.Create(m_symbol.Name(),m_period,m_period_MA,0,MODE_EMA,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create Close series.                                             |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalADX_MA::InitClose(CIndicators* indicators)
  {
//--- create Close series
   if(m_close==NULL)
      if((m_close=new CiClose)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add Close series to collection
   if(!indicators.Add(m_close))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_close;
      return(false);
     }
//--- initialize Close series
   if(!m_close.Create(m_symbol.Name(),m_period))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
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
bool CSignalADX_MA::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   bool Buy_Condition_1=(StateEMA(0)>0 && StateEMA(1)>0);
   bool Buy_Condition_2=(StateClose(1)>0);
   bool Buy_Condition_3=(MainADX(0)>m_minimum_ADX);
   bool Buy_Condition_4=(StateADX(0)>0);
//---
   price=0.0;
   sl   =m_symbol.Ask()-m_stop_loss*m_adjusted_point;
   tp   =m_symbol.Ask()+m_take_profit*m_adjusted_point;
//---
   return(Buy_Condition_1 && Buy_Condition_2 && Buy_Condition_3 && Buy_Condition_4);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalADX_MA::CheckCloseLong(double& price)
  {
   bool Sell_Condition_1=(StateEMA(0)<0 && StateEMA(1)<0);
   bool Sell_Condition_2=(StateClose(1)<0);
   bool Sell_Condition_3=(MainADX(0)>m_minimum_ADX);
   bool Sell_Condition_4=(StateADX(0)<0);
//---
   price=0.0;
//---
   return(Sell_Condition_1 && Sell_Condition_2 && Sell_Condition_3 && Sell_Condition_4);
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
bool CSignalADX_MA::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   bool Sell_Condition_1=(StateEMA(0)<0 && StateEMA(1)<0);
   bool Sell_Condition_2=(StateClose(1)<0);
   bool Sell_Condition_3=(MainADX(0)>m_minimum_ADX);
   bool Sell_Condition_4=(StateADX(0)<0);
//---
   price=0.0;
   sl   =m_symbol.Bid()+m_stop_loss*m_adjusted_point;
   tp   =m_symbol.Bid()-m_take_profit*m_adjusted_point;
//---
   return(Sell_Condition_1 && Sell_Condition_2 && Sell_Condition_3 && Sell_Condition_4);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalADX_MA::CheckCloseShort(double& price)
  {
   bool Buy_Condition_1=(StateEMA(0)>0 && StateEMA(1)>0);
   bool Buy_Condition_2=(StateClose(1)>0);
   bool Buy_Condition_3=(MainADX(0)>m_minimum_ADX);
   bool Buy_Condition_4=(StateADX(0)>0);
//---
   price=0.0;
//---
   return(Buy_Condition_1 && Buy_Condition_2 && Buy_Condition_3 && Buy_Condition_4);
  }
//+------------------------------------------------------------------+
