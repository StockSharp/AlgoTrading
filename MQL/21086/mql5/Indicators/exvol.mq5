//+------------------------------------------------------------------+
//|                                                        ExVol.mq5 |
//|                           Copyright � 2006, Alex Sidd (Executer) |
//|                                           mailto:work_st@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, Alex Sidd (Executer)"
#property link      "mailto:work_st@mail.ru" 
//--- ����� ������ ����������
#property version   "1.01"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//--- ���������� ������������ ������� 2
#property indicator_buffers 2 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//--- ��������� ���������� � ���� ������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- � �������� ������ ������������� ����������� ������������
#property indicator_color1 clrRed,clrLightSalmon,clrGray,clrSkyBlue,clrBlue
//--- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1 2
//--- ����������� ����� ����������
#property indicator_label1 "ExVol"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint ExPeriod=15;
//+-----------------------------------+
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(ExPeriod);
//--- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"ExVol("+string(ExPeriod)+")");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
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
///--- ���������� ��������� ���������� 
   int first,bar;
   double;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total;  // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double negative=0;
      double positive=0;
      int kkk=int(bar-ExPeriod+1);
      while(kkk<=bar)
        {
         double res=(close[kkk]-open[kkk])/_Point;
         if(res>0) positive+=res;
         if(res<0) negative-=res;
         kkk++;
        }
      IndBuffer[bar]=(positive-negative)/ExPeriod;
     }
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- �������� ���� ��������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=0;

      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
