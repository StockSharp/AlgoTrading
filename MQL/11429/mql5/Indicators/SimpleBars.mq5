//+------------------------------------------------------------------+
//|                                                   SimpleBars.mq5 |
//|                                  Copyright � 2012, Ivan Kornilov |
//|                                                 excelf@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2012, Ivan Kornilov"
#property link "excelf@gmail.com"
#property description "SimpleBars"
//--- ����� ������ ����������
#property version   "1.01"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//--- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  clrSeaGreen,clrRed
//--- ����������� ����� ����������
#property indicator_label1  "Upper;lower"
//+----------------------------------------------+
#define SIGNAL_NONE        0  // ������ ������
#define SIGNAL_BUY         1  // ������ �� ������� 
#define SIGNAL_SELL       -1  // ������ �� ������� 
#define SIGNAL_TRADE_ALLOW 3  // ������, ����������� ��������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint period=6;
input bool useclose=true;
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� �����
//--- � ���������� ������������ � �������� ������������ �������
double ExtopenBuffer[];
double ExthighBuffer[];
double ExtlowBuffer[];
double ExtcloseBuffer[];
double ExtColorBuffer[];
//---
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//--- ������������� ���������� ���������� 
   min_rates_total=int(period)+1;
//--- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtopenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExthighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtlowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtcloseBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="SimpleBars";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//--- ���������� ��������� ���������� 
   int first,bar,trend=0;
   static int prev_trend;
   double buyPrice,sellPrice;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total; // ��������� ����� ��� ������� ���� �����
      prev_trend=SIGNAL_NONE;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(useclose)
        {
         buyPrice=close[bar];
         sellPrice=close[bar];
        }
      else
        {
         buyPrice=low[bar];
         sellPrice=high[bar];
        }

      if(prev_trend==SIGNAL_NONE)
        {
         if(close[bar]>open[bar]) trend=SIGNAL_BUY;
         else trend=SIGNAL_SELL;
        }
      else
        {
         if(prev_trend==SIGNAL_BUY)
           {
            if(buyPrice>low[bar-1]) trend=SIGNAL_BUY;
            else
              {
               for(int j=2; j<=int(period); j++)
                 {
                  if(buyPrice>low[bar-j])
                    {
                     trend=SIGNAL_BUY;
                     break;
                    }
                  else trend=SIGNAL_SELL;
                 }
              }
           }

         if(prev_trend==SIGNAL_SELL)
           {
            if(sellPrice<high[bar-1]) trend=SIGNAL_SELL;
            else
              {
               for(int j=2; j<=int(period); j++)
                 {
                  if(sellPrice<high[bar-j])
                    {
                     trend=SIGNAL_SELL;
                     break;
                    }
                  else trend=SIGNAL_BUY;
                 }
              }
           }
        }
      //--- ������������� ������
      if(trend==SIGNAL_SELL) ExtColorBuffer[bar]=1.0;
      if(trend==SIGNAL_BUY) ExtColorBuffer[bar]=0.0;
      //---
      ExtopenBuffer[bar]=open[bar];
      ExtcloseBuffer[bar]=close[bar];
      ExthighBuffer[bar]=high[bar];
      ExtlowBuffer[bar]=low[bar];
      //---
      if(bar<rates_total-1) prev_trend=trend;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
