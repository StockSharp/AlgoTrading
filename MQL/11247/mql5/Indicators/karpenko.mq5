//+------------------------------------------------------------------+
//|                                                     Karpenko.mq5 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2014, MetaQuotes Software Corp."
//--- ������ �� ���� ������
#property link "http://www.metaquotes.net" 
#property description "Karpenko"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrPaleGreen,clrLightPink
//--- ����������� ����� ����������
#property indicator_label1  "Karpenko"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint Basic_MA=144;     // ������ MA
input uint History=500;      // ������ �������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double UpBuffer[],DnBuffer[];
//--- ���������� ����� ���������� ��� �������� ������� �����������
int Ind_Handle;
//--- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(Basic_MA,History));
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnBuffer,true);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"Karpenko");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(RESET);
//--- ���������� ��������� ���������� 
   double sum_c,up,dw,base;
   int limit,bar,k;
//--- ������� ������������ ���������� ���������� ������
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      k=0;
      sum_c=0.0;
      while(k<int(Basic_MA)) {sum_c+=close[bar+k]; k++;}
      base=sum_c/Basic_MA;

      k=0;
      sum_c=0.0;
      while(k<int(History)) {sum_c+=high[bar+k]-low[bar+k]; k++;}
      up=sum_c/History;
      dw=up;

      double Up=base;
      while(high[bar]>Up) {up*=1.618; Up=base+up;}

      double Dn=base;
      while(low[bar]<Dn) {dw*=1.618; Dn=base-dw;}

      if(base==Up)
        {
         UpBuffer[bar]=base-dw;
         DnBuffer[bar]=base;
        }
      else
        {
         UpBuffer[bar]=base+up;
         DnBuffer[bar]=base;
        }
     }
//---    

   return(rates_total);
  }
//+------------------------------------------------------------------+
