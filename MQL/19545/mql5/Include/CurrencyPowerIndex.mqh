//+------------------------------------------------------------------+
//|                                           CurrencyPowerIndex.mqh |
//|                        Copyright 2017, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
//+------------------------------------------------------------------+
//| SymbolWeight                                                     |
//+------------------------------------------------------------------+
struct SymbolWeight
  {
   string            symbol;
   double            weight;
  };
//+------------------------------------------------------------------+
//| CCurrencyPowerIndex                                              |
//+------------------------------------------------------------------+
class CCurrencyPowerIndex
  {
protected:
   bool              m_initialized;
   bool              m_logs;
   string            m_symbol;
   string            m_symbol_path;
   double            m_point;
   int               m_digits;
   uint              m_basket_size;
   SymbolWeight      m_symbol_weights[];
   int               m_last_error;
   int               m_minute_bars_count;
   double            m_symbol_powers[];
   //--- current prices
   double            m_current_price_average;
   double            m_current_price_bid;
   double            m_current_price_ask;
   bool              CalculatePower(const MqlRates &m_rates[],double &symbol_power,const bool inverse=false);
public:
                     CCurrencyPowerIndex();
                    ~CCurrencyPowerIndex();
   //--- Current values
   double            GetCurrentPriceAverage()                               { return(m_current_price_average);     }
   double            GetCurrentPriceBid()                                   { return(m_current_price_bid);         }
   double            GetCurrentPriceAsk()                                   { return(m_current_price_ask);         }
   //--- Settings
   void              SetCustomSymbol(const string symbol,const string path) { m_symbol=symbol; m_symbol_path=path; }
   void              SetPointDigits(const double point,const int digits)    { m_point=point; m_digits=digits;      }
   void              SetBasketSize(const uint size);
   void              SetSymbolWeight(const int index,const string symbol,const double weight);
   //--- Initializing
   bool              Initialize(int minute_bars_count,const bool print_logs,const bool chart_open);
   //--- Main calculations
   void              TickCalculate(void);
  };
//+------------------------------------------------------------------+
//| Class constructor                                                |
//+------------------------------------------------------------------+
CCurrencyPowerIndex::CCurrencyPowerIndex() : m_initialized(false),m_logs(true),m_symbol(""),m_symbol_path(""),
                                             m_point(0.01),m_digits(2),m_basket_size(0),m_last_error(0)
  {
  }
//+------------------------------------------------------------------+
//| Class destructor                                                 |
//+------------------------------------------------------------------+
CCurrencyPowerIndex::~CCurrencyPowerIndex()
  {
  }
//+------------------------------------------------------------------+
//| SetBasketSize                                                    |
//+------------------------------------------------------------------+
void CCurrencyPowerIndex::SetBasketSize(const uint size)
  {
   if(size>0 && ArrayResize(m_symbol_weights,size)==size && ArrayResize(m_symbol_powers,size)==size)
      m_basket_size=size;
  }
//+------------------------------------------------------------------+
//| SetSymbolWeight                                                  |
//+------------------------------------------------------------------+
void CCurrencyPowerIndex::SetSymbolWeight(const int index,const string symbol,const double weight)
  {
   if(index>=0 && index<ArraySize(m_symbol_weights))
     {
      m_symbol_weights[index].symbol=symbol;
      m_symbol_weights[index].weight=weight;
     }
  }
//+------------------------------------------------------------------+
//| Initialize                                                       |
//+------------------------------------------------------------------+
bool CCurrencyPowerIndex::Initialize(int minute_bars_count,const bool print_logs,const bool chart_open)
  {
   m_minute_bars_count=minute_bars_count;

   m_logs=print_logs;
   m_last_error=0;
//---
   if(!m_initialized)
     {
      if(m_basket_size==0)
        {
         m_last_error=1;
         if(m_logs)
            Print("cannot initialize currency basket");
         return(false);
        }
      //---
      bool is_custom=false;
      bool res=SymbolSelect(m_symbol,true);
      if(res)
         is_custom=(bool)SymbolInfoInteger(m_symbol,SYMBOL_CUSTOM);
      //--- create custom symbol
      if(!res)
        {
         if(!CustomSymbolCreate(m_symbol,m_symbol_path))
           {
            m_last_error=2;
            if(m_logs)
               Print("cannot create custom symbol ",m_symbol);
            return(false);
           }
         //---
         CustomSymbolSetDouble(m_symbol,SYMBOL_POINT,m_point);
         CustomSymbolSetInteger(m_symbol,SYMBOL_DIGITS,m_digits);
         CustomSymbolSetInteger(m_symbol,SYMBOL_SPREAD,0);
         is_custom=true;
         //---
         if(!SymbolSelect(m_symbol,true))
           {
            m_last_error=3;
            if(m_logs)
               Print("cannot select custom symbol ",m_symbol);
            return(false);
           }
        }
      //--- select symbols
      if(is_custom)
        {
         MqlRates rates[100];
         MqlTick  ticks[100];
         //--- select symbols
         for(uint i=0; i<m_basket_size; i++)
           {
            if(!SymbolSelect(m_symbol_weights[i].symbol,true))
              {
               m_last_error=4;
               if(m_logs)
                  Print("cannot select basket symbol ",m_symbol_weights[i].symbol);
               return(false);
              }
            //--- requests to initialize rates and ticks download
            CopyRates(m_symbol_weights[i].symbol,PERIOD_M1,0,100,rates);
            CopyTicks(m_symbol_weights[i].symbol,ticks,COPY_TICKS_ALL,0,100);
           }
         m_initialized=true;
        }
      else
        {
         m_last_error=5;
        }
     }
//--- open custom chart
   if(m_initialized && chart_open)
     {
      long chart_id=ChartFirst();
      bool found=false;
      while(chart_id>=0)
        {
         if(ChartSymbol(chart_id)==m_symbol)
           {
            found=true;
            if(m_logs)
               Print(m_symbol," chart found");
            break;
           }
         chart_id=ChartNext(chart_id);
        }
      if(!found)
        {
         if(m_logs)
            Print("open chart ",m_symbol,",M1");
         chart_id=ChartOpen(m_symbol,PERIOD_M1);
         ChartSetInteger(chart_id,CHART_BRING_TO_TOP,true);
        }
     }
//---
   return(m_initialized);
  }
//+------------------------------------------------------------------+
//| CalculatePower                                                   |
//+------------------------------------------------------------------+
bool CCurrencyPowerIndex::CalculatePower(const MqlRates &m_rates[],double &symbol_power,const bool inverse=false)
  {
   int total_bars=ArraySize(m_rates);
   if(total_bars==0)
      return(false);

   ArraySetAsSeries(m_rates,true);
   double high=m_rates[0].high;
   double low=m_rates[0].low;
   for(int i=0; i<total_bars; i++)
     {
      low=MathMin(low,m_rates[i].low);
      high=MathMax(high,m_rates[i].high);
     }
   double close=m_rates[0].close;
   double range=(high-low);

   if(range<=0)
      return(false);

   double close_pos=(close-low)/range;
   if(inverse==true)
      close_pos=1.0-close_pos;

   symbol_power=100*close_pos;
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Main calculating function                                        |
//+------------------------------------------------------------------+
void CCurrencyPowerIndex::TickCalculate()
  {
   if(m_basket_size==0)
      return;

   MqlRates rates[];
   double value=0.0;
   for(uint i=0; i<m_basket_size; i++)
     {
      int rates_copied=CopyRates(m_symbol_weights[i].symbol,PERIOD_M1,0,m_minute_bars_count,rates);
      if(rates_copied!=m_minute_bars_count)
        {
         PrintFormat("Error %d in CopyRates. Rates_copied=%d",GetLastError(),rates_copied);
         return;
        }
      else
        {
         bool inverse=false;
         if(m_symbol_weights[i].weight<0)
            inverse=true;
         CalculatePower(rates,m_symbol_powers[i],inverse);
         value+=m_symbol_powers[i];
        }
     }
//--- average value
   value/=m_basket_size;
   m_current_price_average=value;
   double spread=2;
   m_current_price_bid=m_current_price_average-spread*0.5;
   m_current_price_ask=m_current_price_average+spread*0.5;

   uint    i,success_cnt=0;
   MqlTick basket_tick[1]={0},tick;
//---
   for(i=0; i<m_basket_size; i++)
     {
      if(SymbolInfoTick(m_symbol_weights[i].symbol,tick))
        {
         success_cnt++;
         //--- get latest time of all ticks
         if(basket_tick[0].time==0)
           {
            basket_tick[0].time=tick.time;
            basket_tick[0].time_msc=tick.time_msc;
           }
         else
           {
            if(basket_tick[0].time_msc<tick.time_msc)
              {
               basket_tick[0].time=tick.time;
               basket_tick[0].time_msc=tick.time_msc;
              }
           }
        }
     }
//--- get all the ticks
   if(success_cnt==m_basket_size && basket_tick[0].time!=0)
     {
      basket_tick[0].bid=m_current_price_bid;
      basket_tick[0].ask=m_current_price_ask;
      int cnt=CustomTicksAdd(m_symbol,basket_tick);
     }
//---
  }
//+------------------------------------------------------------------+
