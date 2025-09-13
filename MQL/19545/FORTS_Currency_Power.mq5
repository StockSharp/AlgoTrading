//+------------------------------------------------------------------+
//|                                         FORTS_Currency_Power.mq5 |
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
//| CPowersFORTS                                                     |
//+------------------------------------------------------------------+
class CPowersFORTS
  {
protected:
   CCurrencyPowerIndex m_indices[3];
   IndexData         m_data[3];
   CGraphic          m_graph_powers;
   //--
   double            m_values_50[];
   double            m_values_RTS[];
   double            m_values_USD[];
   double            m_values_RUB[];
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
   void              CPowersFORTS::CPowersFORTS();
   bool              Initialize(const int minute_bars_count=5);
   void              Calculate();
   bool              ShowCurrencyPowers();
  };
//+------------------------------------------------------------------+
//| Class constructor                                                |
//+------------------------------------------------------------------+
void CPowersFORTS::CPowersFORTS()
  {
   m_size=400;
   m_head=0;
   m_tail=0;
   m_count=0;
   m_initialized=false;
//---
   for(int i=0; i<3; i++)
      ArrayResize(m_data[i].values,m_size);
  }
//+------------------------------------------------------------------+
//| Calculate                                                        |
//+------------------------------------------------------------------+
void CPowersFORTS::Calculate()
  {
   for(int i=0; i<3; i++)
      m_indices[i].TickCalculate();
//---
   AddCurrentData();
  }
//+------------------------------------------------------------------+
//| AddCurrentData                                                   |
//+------------------------------------------------------------------+
void CPowersFORTS::AddCurrentData()
  {
   for(int i=0; i<3; i++)
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
bool CPowersFORTS::GetDataArray(const int idx,double &values[])
  {
//--- check array index
   if(idx<0 || idx>3)
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
bool CPowersFORTS::ShowCurrencyPowers()
  {
   GetDataArray(0,m_values_RTS);
   GetDataArray(1,m_values_USD);
   GetDataArray(2,m_values_RUB);
//---
   m_graph_powers.CurveGetByIndex(0).Update(m_values_50);
   m_graph_powers.CurveGetByIndex(1).Update(m_values_RTS);
   m_graph_powers.CurveGetByIndex(2).Update(m_values_USD);
   m_graph_powers.CurveGetByIndex(3).Update(m_values_RUB);
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
bool CPowersFORTS::Initialize(const int minute_bars_count=5)
  {
   if(m_initialized==true)
      return(true);
//---
   const string symbol_names[]={"FORTS.RTS","FORTS.USD","FORTS.RUB"};
   SymbolWeight weights_RTS[2]={{"MIX-3.18",1.0},{"RTS-3.18", 1.0}};
   SymbolWeight weights_USD[2]={{"Si-3.18", 1.0},{"RTS-3.18",-1.0}};
   SymbolWeight weights_RUB[3]={{"Si-3.18",-1.0},{"MIX-3.18",-1.0}, {"Eu-3.18",-1.0}};
   string symbol_postfix=".M"+IntegerToString(minute_bars_count);
   const string custom_group="Custom\\FORTS\\";
//--- prepare symbols, set weights and initialize
   for(int i=0; i<3; i++)
     {
      string full_name=symbol_names[i]+symbol_postfix;
      string full_group_name=custom_group+full_name;
      m_indices[i].SetCustomSymbol(full_name,full_group_name);
      m_indices[i].SetPointDigits(0.01,2);
      switch(i)
        {
         case 0:
           {
            m_indices[i].SetBasketSize(ArraySize(weights_RTS));
            for(int j=0; j<ArraySize(weights_RTS); j++)
               m_indices[i].SetSymbolWeight(j,weights_RTS[j].symbol,weights_RTS[j].weight);
            break;
           }
         case 1:
           {
            m_indices[i].SetBasketSize(ArraySize(weights_USD));
            for(int j=0; j<ArraySize(weights_USD); j++)
               m_indices[i].SetSymbolWeight(j,weights_USD[j].symbol,weights_USD[j].weight);
            break;
           }
         case 2:
           {
            m_indices[i].SetBasketSize(ArraySize(weights_RUB));
            for(int j=0; j<ArraySize(weights_RUB); j++)
               m_indices[i].SetSymbolWeight(j,weights_RUB[j].symbol,weights_RUB[j].weight);
            break;
           }
        }

      if(!m_indices[i].Initialize(minute_bars_count,true,true))
         return(false);

      m_data[i].symbol=full_name;
     }
//---
   m_graph_powers.Create(0,"FORTS_powers",0,0,0,1000,400);
   m_graph_powers.GridBackgroundColor(ColorToARGB(clrLightGray,255));
   m_graph_powers.GridLineColor(ColorToARGB(clrDarkGray,255));
   m_graph_powers.BackgroundMainSize(16);
   m_graph_powers.BackgroundMain("FORTS Currency Powers Realtime");
   m_graph_powers.XAxis().MaxGrace(0);
   m_graph_powers.HistorySymbolSize(15);
   m_graph_powers.HistoryNameSize(15);
   m_graph_powers.HistoryNameWidth(30);

   int size=m_size-1;
   ArrayResize(m_values_RTS,size);
   ArrayResize(m_values_USD,size);
   ArrayResize(m_values_RUB,size);
   ZeroMemory(m_values_50);
   ZeroMemory(m_values_RTS);
   ZeroMemory(m_values_USD);
   ZeroMemory(m_values_RUB);
   ArrayResize(m_values_50,size);
   for(int i=0; i<size; i++)
      m_values_50[i]=50.0;

   m_graph_powers.CurveAdd(m_values_50,ColorToARGB(clrBlack,255),CURVE_LINES,"50");
   m_graph_powers.CurveAdd(m_values_RTS,ColorToARGB(clrRoyalBlue,255),CURVE_LINES,"RTS");
   m_graph_powers.CurveAdd(m_values_USD,ColorToARGB(clrRed,255),CURVE_LINES,"USD");
   m_graph_powers.CurveAdd(m_values_RUB,ColorToARGB(clrDarkGray,255),CURVE_LINES,"RUB");
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

CPowersFORTS ExtFORTSPowers;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!ExtFORTSPowers.Initialize(MINITE_BARS_COUNT))
      return(INIT_FAILED);
//--- create timer
   EventSetMillisecondTimer(100);
//---
   Print("FORTS Currency Powers datafeed started");
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
   Print("FORTS Currency Powers datafeed stopped");
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   ExtFORTSPowers.Calculate();
   ExtFORTSPowers.ShowCurrencyPowers();
  }
//+------------------------------------------------------------------+
