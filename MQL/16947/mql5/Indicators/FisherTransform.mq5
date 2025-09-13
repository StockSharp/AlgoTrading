//+------------------------------------------------------------------+
//|                                              FisherTransform.mq5 |
//|                                                                  |
//| Fisher Transform                                                 |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Coded by Witold Wozniak"
//---- ��������� ����������
#property link      "www.mqlsoft.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� Fisher       |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color1  Red
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ����� ����������
#property indicator_label1  "Fisher"
//+----------------------------------------------+
//|  ��������� ��������� ���������� Trigger      |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� ����� ����
#property indicator_color2  Blue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int Length=10; // ������ ���������� 
input int Shift=0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double FisherBuffer[];
double TriggerBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,FisherBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� Length
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,Length);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� Length
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,Length);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"FisherTransform(",Length,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<Length) return(0);

//---- ���������� ��������� ���������� 
   int first,bar,kkk;
   double price,price1,MaxH,MinL,Value;
   static double Value_;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=Length-1; // ��������� ����� ��� ������� ���� �����
      Value_=0.0;
      FisherBuffer[first-1]=0.0;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- ��������������� �������� ����������
   Value=Value_;

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==rates_total-1)
         Value_=Value;

      price=(high[bar]+low[bar])/2.0;
      MaxH = price;
      MinL = price;

      for(int iii=0; iii<Length; iii++)
        {
         kkk=bar-iii;
         price1=(high[kkk]+low[kkk])/2.0;
         if(price1 > MaxH) MaxH = price1;
         if(price1 < MinL) MinL = price1;
        }

      double res=MaxH-MinL;
      if(res) Value=0.5*2.0 *((price-MinL)/res-0.5)+0.5*Value;
      else Value=0.0;

      if(Value>+0.9999) Value=+0.9999;
      if(Value<-0.9999) Value=-0.9999;

      FisherBuffer[bar]=0.25*MathLog((1+Value)/(1-Value))+0.5*FisherBuffer[bar-1];
      TriggerBuffer[bar]=FisherBuffer[bar-1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
