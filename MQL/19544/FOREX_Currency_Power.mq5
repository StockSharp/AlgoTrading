//+------------------------------------------------------------------+
//|                                         FOREX_Currency_Power.mq5 |
//|                        Copyright 2017, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define MINITE_BARS_COUNT 5
#include <CurrencyPowerIndex.mqh>
#include <Graphics\Graphic.mqh>
//+------------------------------------------------------------------+
//| IndexData                                                        |
//+------------------------------------------------------------------+
struct IndexData
  {
   string            symbol;
   double            values[];
  };
//+------------------------------------------------------------------+
//| CPowersFOREX                                                     |
//+------------------------------------------------------------------+
class CPowersFOREX
  {
protected:
   CCurrencyPowerIndex m_indices[5];
   IndexData         m_data[5];
   CGraphic          m_graph_powers;
   //--
   double            m_values_50[];
   double            m_values_EUR[];
   double            m_values_USD[];
   double            m_values_GBP[];
   double            m_values_CHF[];
   double            m_values_JPY[];
   //--- internal variables for circular buffer
   int               m_size;
   int               m_head;
   int               m_tail;
   int               m_count;
   //---
   void              AddCurrentData();
   bool              GetDataArray(const int idx,double &values[]);
   bool              m_initialized;
public:
   void              CPowersFOREX::CPowersFOREX();
   bool              Initialize(const int minute_bars_count=5);
   void              Calculate();
   bool              ShowCurrencyPowers();
  };
//+------------------------------------------------------------------+
//| Class constructor                                                |
//+------------------------------------------------------------------+
void CPowersFOREX::CPowersFOREX()
  {
   m_size=400;
   m_head=0;
   m_tail=0;
   m_count=0;
   m_initialized=false;
//---
   for(int i=0; i<5; i++)
      ArrayResize(m_data[i].values,m_size);
  }
//+------------------------------------------------------------------+
//| Calculate                                                        |
//+------------------------------------------------------------------+
void CPowersFOREX::Calculate()
  {
   for(int i=0; i<5; i++)
      m_indices[i].TickCalculate();
//---
   AddCurrentData();
  }
//+------------------------------------------------------------------+
//| AddCurrentData                                                   |
//+------------------------------------------------------------------+
void CPowersFOREX::AddCurrentData()
  {
   for(int i=0; i<5; i++)
     {
      double value=m_indices[i].GetCurrentPriceAverage();
      m_data[i].values[m_tail]=value;
     }
   m_tail=(m_tail+1)%m_size;
   if(m_tail==m_head)
      m_head=(m_head+1)%m_size;
   m_count++;
   if(m_count>m_size-1)
      m_count=m_size-1;
  }
//+------------------------------------------------------------------+
//| GetDataArray                                                     |
//+------------------------------------------------------------------+
bool CPowersFOREX::GetDataArray(const int idx,double &values[])
  {
//--- check array index
   if(idx<0 || idx>4)
      return(false);
//---
   if(ArraySize(values)<m_count)
      ArrayResize(values,m_count);
//---
   for(int i=0; i<m_count; i++)
     {
      int real_index=(m_head+i)%m_size;
      values[i]=m_data[idx].values[real_index];
     }
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| ShowPowers                                                       |
//+------------------------------------------------------------------+
bool CPowersFOREX::ShowCurrencyPowers()
  {
   GetDataArray(0,m_values_EUR);
   GetDataArray(1,m_values_USD);
   GetDataArray(2,m_values_GBP);
   GetDataArray(3,m_values_CHF);
   GetDataArray(4,m_values_JPY);
//---
   m_graph_powers.CurveGetByIndex(0).Update(m_values_50);
   m_graph_powers.CurveGetByIndex(1).Update(m_values_EUR);
   m_graph_powers.CurveGetByIndex(2).Update(m_values_USD);
   m_graph_powers.CurveGetByIndex(3).Update(m_values_GBP);
   m_graph_powers.CurveGetByIndex(4).Update(m_values_CHF);
   m_graph_powers.CurveGetByIndex(5).Update(m_values_JPY);
//---   
   m_graph_powers.CurvePlotAll();
   m_graph_powers.Redraw(true);
   m_graph_powers.Update();
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialize                                                       |
//+------------------------------------------------------------------+
bool CPowersFOREX::Initialize(const int minute_bars_count=5)
  {
   if(m_initialized==true)
      return(true);
//---
   const string symbol_names[]={"FOREX.EUR","FOREX.USD","FOREX.GBP","FOREX.CHF","FOREX.JPY"};
   SymbolWeight weights[20]=
     {
        {"EURUSD",1.0},{"EURGBP",1.0},{"EURCHF",1.0},{"EURJPY",1.0},
        {"EURUSD",-1.0},{"GBPUSD",-1.0},{"USDCHF", 1.0},{"USDJPY", 1.0},
        {"EURGBP",-1.0},{"GBPUSD", 1.0},{"GBPCHF", 1.0},{"GBPJPY", 1.0},
        {"EURCHF",-1.0},{"USDCHF",-1.0},{"GBPCHF",-1.0},{"CHFJPY", 1.0},
        {"EURJPY",-1.0},{"USDJPY",-1.0},{"GBPJPY",-1.0},{"CHFJPY",-1.0}
     };
   string symbol_postfix=".M"+IntegerToString(minute_bars_count);
   const string custom_group="Custom\\Forex\\";
//--- prepare symbols, set weights and initialize
   for(int i=0; i<5; i++)
     {
      string full_name=symbol_names[i]+symbol_postfix;
      string full_group_name=custom_group+full_name;
      m_indices[i].SetCustomSymbol(full_name,custom_group+full_name);
      m_indices[i].SetPointDigits(0.01,2);
      m_indices[i].SetBasketSize(4);
      for(int j=0; j<4; j++)
        {
         int ind=4*i+j;
         m_indices[i].SetSymbolWeight(j,weights[ind].symbol,weights[ind].weight);
        }
      if(!m_indices[i].Initialize(minute_bars_count,true,true))
         return(false);

      m_data[i].symbol=full_name;
     }
//---
   m_graph_powers.Create(0,"FOREX_powers",0,0,0,1000,400);
   m_graph_powers.GridBackgroundColor(ColorToARGB(clrLightGray,255));
   m_graph_powers.GridLineColor(ColorToARGB(clrDarkGray,255));
   m_graph_powers.BackgroundMainSize(16);
   m_graph_powers.BackgroundMain("FOREX Currency Powers Realtime");
   m_graph_powers.XAxis().MaxGrace(0);
   m_graph_powers.HistorySymbolSize(15);
   m_graph_powers.HistoryNameSize(15);
   m_graph_powers.HistoryNameWidth(30);

   int size=m_size-1;
   ArrayResize(m_values_EUR,size);
   ArrayResize(m_values_USD,size);
   ArrayResize(m_values_GBP,size);
   ArrayResize(m_values_CHF,size);
   ArrayResize(m_values_JPY,size);
   ZeroMemory(m_values_50);
   ZeroMemory(m_values_EUR);
   ZeroMemory(m_values_USD);
   ZeroMemory(m_values_GBP);
   ZeroMemory(m_values_CHF);
   ZeroMemory(m_values_JPY);
   ArrayResize(m_values_50,size);
   for(int i=0; i<size; i++)
      m_values_50[i]=50.0;

   m_graph_powers.CurveAdd(m_values_50,ColorToARGB(clrBlack,255),CURVE_LINES,"50");
   m_graph_powers.CurveAdd(m_values_EUR,ColorToARGB(clrGreen,255),CURVE_LINES,"EUR");
   m_graph_powers.CurveAdd(m_values_USD,ColorToARGB(clrRed,255),CURVE_LINES,"USD");
   m_graph_powers.CurveAdd(m_values_GBP,ColorToARGB(clrMagenta,255),CURVE_LINES,"GBP");
   m_graph_powers.CurveAdd(m_values_CHF,ColorToARGB(clrYellow,255),CURVE_LINES,"CHF");
   m_graph_powers.CurveAdd(m_values_JPY,ColorToARGB(clrBlue,255),CURVE_LINES,"JPY");
   m_graph_powers.YAxis().AutoScale(false);
   m_graph_powers.YAxis().Min(0);
   m_graph_powers.YAxis().Max(100);

   for(int i=0; i<m_graph_powers.CurvesTotal(); i++)
      m_graph_powers.CurveGetByIndex(i).LinesWidth(3);
//---
   m_graph_powers.CurvePlotAll();
   m_graph_powers.Redraw(true);
   m_graph_powers.Update();
//--- 
   m_initialized=true;
   return(true);
  }

CPowersFOREX ExtFOREXPowers;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!ExtFOREXPowers.Initialize(MINITE_BARS_COUNT))
      return(INIT_FAILED);
//--- create timer
   EventSetMillisecondTimer(100);
//---
   Print("FOREX Currency Powers datafeed started");
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();
//---
   Print("FOREX Currency Powers datafeed stopped");
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   ExtFOREXPowers.Calculate();
   ExtFOREXPowers.ShowCurrencyPowers();
  }
//+------------------------------------------------------------------+
