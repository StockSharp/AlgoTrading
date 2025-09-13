//+----------------------------------------------------------------------------------+
//| FractalAMA                                                                       |
//|                                                                                  |
//| Description:  Fractal Adaptive Moving Average - by John Ehlers                   |
//|               Version 1.1 7/17/2006                                              |
//|                                                                                  |
//| Heavily modified and reprogrammed by Matt Kennel (mbkennelfx@gmail.com)          |
//|                                                                                  |
//| Notes:                                                                           |
//|               October 2005 Issue - "FRAMA - Fractal Adaptive Moving Average"     |
//|               Length will be forced to be an even number.                        |
//|               Odd numbers will be bumped up to the                               |
//|               next even number.                                                  |
//| Formula Parameters:     Defaults:                                                |
//| RPeriod                 16                                                       |
//+----------------------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2005, MrPip"
//---- ��������� ����������
#property link      "mbkennelfx@gmail.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� FractalAMA    |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� DarkOrange ����
#property indicator_color1  clrDarkOrange
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "FractalAMA"
//+----------------------------------------------+
//| ��������� ��������� ���������� Trigger       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� SlateBlue ����
#property indicator_color2  clrSlateBlue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ����� ����������
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint RPeriod=16;
input double multiplier=4.6;
input double signal_multiplier=2.5;
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double FrAmaBuffer[];
double TriggerBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,N;
//+------------------------------------------------------------------+
//| Range()                                                          |
//+------------------------------------------------------------------+   
double Range(int index,const double &Low[],const double &High[],int period)
  {
//----
   return(High[ArrayMaximum(High,index,period)]-Low[ArrayMinimum(Low,index,period)]);
  }
//+------------------------------------------------------------------+
//| DEst()                                                           |
//+------------------------------------------------------------------+   
double DEst(int index,const double &Low[],const double &High[],int period)
  {
//----
   double R1,R2,R3;
   int n2=period/2;
//----
   R3=Range(index,Low,High,period)/period;
   R1=Range(index,Low,High,n2)/n2;
   R2=Range(index+n2,Low,High,n2)/n2;
//----
   return((MathLog(R1+R2)-MathLog(R3)) *1.442695);// log_2(e) = 1.442694
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(RPeriod);
   N=int(MathFloor(RPeriod/2)*2);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,FrAmaBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(FrAmaBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(TriggerBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"FractalAMA(",RPeriod,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//---- ���������� ��������� ���������� 
   int limit,bar;
   double dimension_estimate,alpha,alphas;
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      int start=limit+1;
      FrAmaBuffer[start]=close[start];
      TriggerBuffer[start]=close[start];
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      dimension_estimate=DEst(bar,low,high,N);
      alpha=MathExp(-multiplier*(dimension_estimate-1.0));
      alphas=MathExp(-signal_multiplier*(dimension_estimate-1.0));
      //----
      alpha=MathMin(alpha,1.0);
      alpha=MathMax(alpha,0.01);
      //----
      FrAmaBuffer[bar]=alpha*close[bar]+(1.0-alpha) *FrAmaBuffer[bar+1];
      TriggerBuffer[bar]=alphas*FrAmaBuffer[bar]+(1.0-alphas)*TriggerBuffer[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
