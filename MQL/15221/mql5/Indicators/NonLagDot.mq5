//+------------------------------------------------------------------+ 
//|                                                    NonLagDot.mq5 | 
//|                                Copyright � 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, TrendLaboratory"
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ ������� 2
#property indicator_buffers 2
//+-----------------------------------+
//|  ���������� ��������              |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
#define PI     3.1415926535 // ����� ��
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- ��������� ���������� � ���� ������� �������
#property indicator_type1   DRAW_COLOR_ARROW
#property indicator_color1  clrGray,clrMagenta,clrGreen
#property indicator_width1  2
#property indicator_label1  "NonLagDot"
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input ENUM_APPLIED_PRICE Price=PRICE_CLOSE;       // ������� ���������
input ENUM_MA_METHOD     Type=MODE_SMA;           // ����� ����������
input int                Length=10;               // ������ ������� ����������
input int                Filter= 0;
input double             Deviation=0;             // ��������
input int                Shift=0;                 // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double MABuffer[];
double ColorMABuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int MA_Handle;
//---- ���������� ���������� ����������
int Phase;
double Coeff,Len,Cycle,dT1,dT2,Kd,Fi;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ��������
   Coeff= 3*PI;
   Phase=Length-1;
   Cycle= 4;
   Len=Length*Cycle + Phase;
   dT1=(2*Cycle-1)/(Cycle*Length-1);
   dT2=1.0/(Phase-1);
   Kd=1.0+Deviation/100;
   Fi=Filter*_Point;

//---- ������������� ���������� ������ ������� ������ 
   min_rates_total=int(Length+Len+1);

//---- ��������� ������ ���������� iMA
   MA_Handle=iMA(NULL,0,Length,0,Type,Price);
   if(MA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");

//---- ����������� ������������� ������� MABuffer[] � ������������ �����
   SetIndexBuffer(0,MABuffer,INDICATOR_CALCULATIONS);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(MABuffer,true);

//---- ����������� ������������� ������� ColorMABuffer[] � ������������ �����
   SetIndexBuffer(1,ColorMABuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� �� �����������  
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������   
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorMABuffer,true);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"NonLagDot( Length = ",Length,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(MA_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar,trend0;
   double MA[],alfa,beta,t,Sum,Weight,g;
   static int trend1;

//---- ������� ������������ ���������� ���������� ������
//---- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      to_copy=rates_total;                 // ��������� ���������� ���� �����
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      to_copy=rates_total-prev_calculated+int(Len); // ��������� ���������� ������ ����� �����
      limit=rates_total-prev_calculated;            // ��������� ����� ��� ������� ����� �����
     }

//---- �������� ����� ����������� ������ � ������
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(MA,true);

   trend0=trend1;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Weight=0;
      Sum=0;
      t=0;

      for(int iii=0; iii<=Len-1; iii++)
        {
         g=1.0/(Coeff*t+1);
         if(t<=0.5) g=1;
         beta=MathCos(PI*t);
         alfa=g*beta;
         Sum+=alfa*MA[bar+iii];
         Weight+=alfa;
         if(t<1) t+=dT2;
         else if(t<Len-1) t+=dT1;
        }

      if(Weight>0) MABuffer[bar]=Kd*Sum/Weight;

      if(Filter>0) if(MathAbs(MABuffer[bar]-MABuffer[bar-1])<Fi) MABuffer[bar]=MABuffer[bar-1];

      if(MABuffer[bar]-MABuffer[bar+1]>Fi) trend0=+1;
      if(MABuffer[bar+1]-MABuffer[bar]>Fi) trend0=-1;

      ColorMABuffer[bar]=0;

      if(trend0>0) ColorMABuffer[bar]=2;
      if(trend0<0) ColorMABuffer[bar]=1;
      if(bar) trend1=trend0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
