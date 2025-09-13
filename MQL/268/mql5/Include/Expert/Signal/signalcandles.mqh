//+------------------------------------------------------------------+
//|                                                SignalCandles.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.10.12 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on reversal candlestick patterns             |
//| Type=Signal                                                      |
//| Name=Candles                                                     |
//| Class=CSignalCandles                                             |
//| Page=                                                            |
//| Parameter=Range,int,6                                            |
//| Parameter=Minimum,int,25                                         |
//| Parameter=ShadowBig,double,0.5                                   |
//| Parameter=ShadowSmall,double,0.2                                 |
//| Parameter=Limit,double,0.0                                       |
//| Parameter=StopLoss,double,2.0                                    |
//| Parameter=TakeProfit,double,1.0                                  |
//| Parameter=Expiration,int,4                                       |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalCandles.                                            |
//| Appointment: Class trading signals with candlestick patterns.    |
//|              Derives from class CExpertSignal.                   |
//+------------------------------------------------------------------+
class CSignalCandles : public CExpertSignal
  {
protected:
   CiOpen           *m_open;
   CiHigh           *m_high;
   CiLow            *m_low;
   CiClose          *m_close;
   //---
   double            m_open_composite;
   double            m_high_composite;
   double            m_low_composite;
   double            m_close_composite;
   //--- input parameters
   int               m_range;
   int               m_minimum;
   double            m_shadow_big;
   double            m_shadow_small;
   double            m_limit;
   double            m_stop_loss;
   double            m_take_profit;
   int               m_expiration;

public:
                     CSignalCandles();
                    ~CSignalCandles();
   //--- methods initialize protected data
   void              Range(int range)                 { m_range=range;                }
   void              Minimum(int minimum)             { m_minimum=minimum;            }
   void              ShadowBig(double shadow_big)     { m_shadow_big=shadow_big;      }
   void              ShadowSmall(double shadow_small) { m_shadow_small=shadow_small;  }
   void              Limit(double limit)              { m_limit=limit;                }
   void              StopLoss(double stop_loss)       { m_stop_loss=stop_loss;        }
   void              TakeProfit(double take_profit)   { m_take_profit=take_profit;    }
   void              Expiration(int expiration)       { m_expiration=expiration;      }
   virtual bool      InitIndicators(CIndicators* indicators);
   virtual bool      ValidationSettings();
   //---
   double            Open(int ind)              const { return(m_open.GetData(ind));  }
   double            High(int ind)              const { return(m_high.GetData(ind));  }
   double            Low(int ind)               const { return(m_low.GetData(ind));   }
   double            Close(int ind)             const { return(m_close.GetData(ind)); }
   //---
   double            OpenComposite()            const { return(m_open_composite);     }
   double            HighComposite()            const { return(m_high_composite);     }
   double            LowComposite()             const { return(m_low_composite);      }
   double            CloseComposite()           const { return(m_close_composite);    }
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);
   //---
   int               Candle(int ind);

protected:
   bool              InitOpen(CIndicators* indicators);
   bool              InitHigh(CIndicators* indicators);
   bool              InitLow(CIndicators* indicators);
   bool              InitClose(CIndicators* indicators);
   //---
   bool              CandleBull(double open,double high,double low,double close);
   bool              CandleBear(double open,double high,double low,double close);
  };
//+------------------------------------------------------------------+
//| Constructor CSignalCandles.                                      |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalCandles::CSignalCandles()
  {
//--- initialize protected data
   m_open           =NULL;
   m_high           =NULL;
   m_low            =NULL;
   m_close          =NULL;
   m_open_composite =EMPTY_VALUE;
   m_high_composite =EMPTY_VALUE;
   m_low_composite  =EMPTY_VALUE;
   m_close_composite=EMPTY_VALUE;
//--- set default inputs
   m_range          =6;
   m_minimum        =25;
   m_shadow_big     =0.5;
   m_shadow_small   =0.2;
   m_limit          =0.0;
   m_stop_loss      =2.0;
   m_take_profit    =1.0;
   m_expiration     =4;
  }
//+------------------------------------------------------------------+
//| Destructor CSignalCandles.                                       |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalCandles::~CSignalCandles()
  {
//---
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::ValidationSettings()
  {
   if(m_range<=0)
     {
      printf(__FUNCTION__+": candles range must be greater than 0");
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
bool CSignalCandles::InitIndicators(CIndicators* indicators)
  {
//--- check
   if(indicators==NULL)       return(false);
//--- create and initialize Open series
   if(!InitOpen(indicators))  return(false);
//--- create and initialize High series
   if(!InitHigh(indicators))  return(false);
//--- create and initialize Low series
   if(!InitLow(indicators))   return(false);
//--- create and initialize Close series
   if(!InitClose(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create Open series.                                              |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::InitOpen(CIndicators* indicators)
  {
//--- create Open series
   if(m_open==NULL)
      if((m_open=new CiOpen)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add Open series to collection
   if(!indicators.Add(m_open))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_open;
      return(false);
     }
//--- initialize Open series
   if(!m_open.Create(m_symbol.Name(),m_period))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create High series.                                              |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::InitHigh(CIndicators* indicators)
  {
//--- create High series
   if(m_high==NULL)
      if((m_high=new CiHigh)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add High series to collection
   if(!indicators.Add(m_high))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_high;
      return(false);
     }
//--- initialize High series
   if(!m_high.Create(m_symbol.Name(),m_period))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create Low series.                                               |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::InitLow(CIndicators* indicators)
  {
//--- create Low series
   if(m_low==NULL)
      if((m_low=new CiLow)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add Low series to collection
   if(!indicators.Add(m_low))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_low;
      return(false);
     }
//--- initialize Low series
   if(!m_low.Create(m_symbol.Name(),m_period))
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
bool CSignalCandles::InitClose(CIndicators* indicators)
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
bool CSignalCandles::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(Candle(1)<=0) return(false);
//---
   double size=m_high_composite-m_low_composite;
//---
   price=m_symbol.NormalizePrice(m_symbol.Ask()-m_limit*size);
   sl   =m_symbol.NormalizePrice(price-m_stop_loss*size);
   tp   =m_symbol.NormalizePrice(price+m_take_profit*size);
   expiration+=m_expiration*PeriodSeconds(m_period);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::CheckCloseLong(double& price)
  {
   if(Candle(1)>=0) return(false);
//---
   price=0.0;
//---
   return(true);
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
bool CSignalCandles::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(Candle(1)>=0) return(false);
//---
   double size=m_high_composite-m_low_composite;
//---
   price=m_symbol.NormalizePrice(m_symbol.Bid()+m_limit*size);
   sl   =m_symbol.NormalizePrice(price+m_stop_loss*size);
   tp   =m_symbol.NormalizePrice(price-m_take_profit*size);
   expiration+=m_expiration*PeriodSeconds(m_period);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::CheckCloseShort(double& price)
  {
   if(Candle(1)<=0) return(false);
//---
   price=0.0;
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Formation of composite candles                                   |
//| and checking for membership of the bulls or bears.               |
//| INPUT:  ind - index of the first candle.                         |
//| OUTPUT: <0-if candles bears, >0-if candles bulls.                |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CSignalCandles::Candle(int ind)
  {
   if(ind<0) return(0);
//---
   int    i    =0;
   double size =m_minimum*PriceLevelUnit();
   double open =Open(ind);
   double high =High(ind);
   double low  =Low(ind);
   double close=Close(ind);
//---
   if(high-low<size)
     {
      for(i=1;i<m_range;i++)
        {
         open=Open(i+ind);
         if(high<High(i+ind)) high=High(i+ind);
         if(low>Low(i+ind))   low =Low(i+ind);
         if(high-low>size)  break;
        }
     }
   if(high-low>size)
     {
      if(CandleBull(open,high,low,close)) return(i);
      if(CandleBear(open,high,low,close)) return(-i);
     }
   m_open_composite =EMPTY_VALUE;
   m_high_composite =EMPTY_VALUE;
   m_low_composite  =EMPTY_VALUE;
   m_close_composite=EMPTY_VALUE;
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Check the candles on her membership bulls.                       |
//| INPUT:  open  - price open candles,                              |
//|         high  - price high candles,                              |
//|         low   - price low candles,                               |
//|         close - price close candles.                             |
//| OUTPUT: true-if candles bulls, false otherwise.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::CandleBull(double open,double high,double low,double close)
  {
   int    shadow_mode=1;
   double size=high-low;
   double shadow_h=0,shadow_l=0;
//---
   switch(shadow_mode)
     {
      case 0:  // classic
         shadow_h=high-((open>close)?open:close);
         shadow_l=((open<close)?open:close)-low;
         break;
      case 1:  // modern 1
         shadow_h=high-((open>close)?open:close);
         shadow_l=close-low;
         break;
      case 2:  // modern 2
         shadow_h=high-open;
         shadow_l=close-low;
         break;
     }
//---
   if(shadow_h<m_shadow_small*size && shadow_l>m_shadow_big*size)
     {
      m_open_composite =open;
      m_high_composite =high;
      m_low_composite  =low;
      m_close_composite=close;
      return(true);
     }
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Check the candles on her membership bears.                       |
//| INPUT:  open  - price open candles,                              |
//|         high  - price high candles,                              |
//|         low   - price low candles,                               |
//|         close - price close candles.                             |
//| OUTPUT: true-if candles bears, false otherwise.                  |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCandles::CandleBear(double open,double high,double low,double close)
  {
   int    shadow_mode=1;
   double size=high-low;
   double shadow_h=0,shadow_l=0;
//---
   switch(shadow_mode)
     {
      case 0:  // classic
         shadow_h=high-((open>close)?open:close);
         shadow_l=((open<close)?open:close)-low;
         break;
      case 1:  // modern 1
         shadow_h=high-close;
         shadow_l=((open<close)?open:close)-low;
         break;
      case 2:  // modern 2
         shadow_h=high-close;
         shadow_l=open-low;
         break;
     }
//---
   if(shadow_l<m_shadow_small*size && shadow_h>m_shadow_big*size)
     {
      m_open_composite =open;
      m_high_composite =high;
      m_low_composite  =low;
      m_close_composite=close;
      return(true);
     }
//---
   return(false);
  }
//+------------------------------------------------------------------+
