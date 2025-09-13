//+------------------------------------------------------------------+ 
//|                                                       ADXDMI.mq5 | 
//|                                  Copyright � 2006, FXTEAM Turkey |
//|                                                                  | 
//+------------------------------------------------------------------+  
//---- ��������� ����������
#property copyright "Copyright � 2006, FXTEAM Turkey"
//---- ��������� ����������
#property link      ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ���������� ADXDMI        |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����jd ���������� ������������
#property indicator_color1  clrBlue,clrRed
//---- ����������� ����� ����� ����������
#property indicator_label1  "ADXDMI"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0       // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint DMIPeriod=14;
input uint Smooth=10;
input int Shift=0;        // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double UpBuffer[];
double DnBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=4;

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);

//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"ADXDMI(",DMIPeriod,", ",Smooth,", ",Shift,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- ���������� �������������
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int first,bar;
   double tr,xx,price_high,price_low,PD,ND,Buff,ADX;
   static double PREADX,PREP,PREN,PRETR;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=2;                   // ��������� ����� ��� ������� ���� �����
      PREP=NULL;
      PREN=NULL;
      PRETR=NULL;
      ADX=NULL;
      PREADX=NULL;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {

      if(high[bar]>high[bar-1] && (high[bar]-high[bar-1])>(low[bar-1]-low[bar]))
        {
         xx=high[bar]-high[bar-1];
        }
      else xx=NULL;
      //----
      PD=(((DMIPeriod-1.0)*PREP)+xx)/(DMIPeriod);
      //----
      if(low[bar]<low[bar-1] && (low[bar-1]-low[bar])>(high[bar]-high[bar-1]))
        {
         xx=low[bar-1]-low[bar];
        }
      else xx=NULL;
      //----
      ND=(((DMIPeriod-1.0)*PREN)+xx)/(DMIPeriod);
      Buff=MathAbs(PD-ND);
      if(!Buff) ADX=(((Smooth-1.0)*PREADX))/Smooth;
      else ADX=(((Smooth-1.0)*PREADX)+(MathAbs(PD-ND)/(PD+ND)))/Smooth;
      //----
      price_high=MathMax(high[bar],close[bar-1]);
      price_low=MathMin(low[bar],close[bar-1]);
      double num1=MathAbs(price_high-price_low);
      double num2=MathAbs(price_high-close[bar-1]);
      double num3=MathAbs(close[bar-1]-price_low);
      //----
      tr=MathMax(num1,num2);
      tr=MathMax(tr,num3);
      //----
      tr=(((DMIPeriod-1.0)*PRETR)+tr)/DMIPeriod;
      //----
      UpBuffer[bar]=100000*(PD/tr);
      DnBuffer[bar]=100000*(ND/tr);
      //----
      if(bar<rates_total-1)
        {
         PREN=ND;
         PREP=PD;
         PREADX=ADX;
         PRETR=tr;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
