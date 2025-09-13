//+------------------------------------------------------------------+
//|                                                  2pbIdeal3MA.mq5 |
//|                             Copyright � 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2011, Nikolay Kositsin"
//---- ������ �� ���� ������
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 1
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ����� ����
#property indicator_color1  Yellow
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "2pbIdeal3MA"

//---- ������� ��������� ����������
input int PeriodX1 = 10; //������ ������ ����������
input int PeriodX2 = 10; //������ ���������� ����������
input int PeriodY1 = 10; //������ ������ ����������
input int PeriodY2 = 10; //������ ���������� ����������
input int PeriodZ1 = 10; //������ ������ ����������
input int PeriodZ2 = 10; //������ ���������� ����������
input int MAShift=0; //����� ������� �� ����������� � ����� 

//---- ���������� ������������� �������, ������� ����� � 
// ���������� ����������� � �������� ������������� ������
double ExtLineBuffer[];
//---- ���������� ���������� ��� ������������ ��������
double wX1,wX2,wY1,wY2,wZ1,wZ2;
//---- ���������� ���������� ��� �������� ����������� ����������
double Moving01_,Moving11_,Moving21_;
//+------------------------------------------------------------------+
//|  ���������� �� Neutron                                           |
//+------------------------------------------------------------------+
double GetIdealMASmooth
(
 double W1_,//������ ������������ ���������
 double W2_,//������ ������������ ���������
 double Series1,//�������� �������� � �������� ���� 
 double Series0,//�������� �������� � ����������� ���� 
 double Resalt1 //�������� ������� � ����������� ����
 )
  {
//----
   double Resalt0,dSeries,dSeries2;
   dSeries=Series0-Series1;
   dSeries2=dSeries*dSeries-1.0;

   Resalt0=(W1_ *(Series0-Resalt1)+
            Resalt1+W2_*Resalt1*dSeries2)
   /(1.0+W2_*dSeries2);
//----
   return(Resalt0);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ����������
   wX1=1.0/PeriodX1;
   wX2=1.0/PeriodX2;
   wY1=1.0/PeriodY1;
   wY2=1.0/PeriodY2;
   wZ1=1.0/PeriodZ1;
   wZ2=1.0/PeriodZ2;
//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� ��MAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,MAShift);
//---- ��������� �������, � ������� ���������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,1);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="2pbIdeal3MA";
//---- �������� ����� ��� ����������� � ���� ������
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,     // ���������� ������� � ����� �� ������� ����
                const int prev_calculated, // ���������� ������� � ����� �� ���������� ����
                const int begin,           // ����� ������ ������������ ������� �����
                const double &price[]      // ������� ������ ��� ������� ����������
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<1+begin) return(0);

//---- ���������� ��������� ���������� 
   int first,bar;
   double Moving00,Moving10,Moving20;
   double Moving01,Moving11,Moving21;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1+begin;  // ��������� ����� ��� ������� ���� �����
      //---- �������� ������� ������ ������ �� begin �����, ���������� �������� �� ������ ������� ����������
      if(begin>0)
         PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin+1);

      //---- ��������� �������������  
      ExtLineBuffer[begin]=price[begin];
      Moving01_=price[begin];
      Moving11_=price[begin];
      Moving21_=price[begin];
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- ��������������� �������� ����������
   Moving01=Moving01_;
   Moving11=Moving11_;
   Moving21=Moving21_;

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==rates_total-1)
        {
         Moving01_=Moving01;
         Moving11_=Moving11;
         Moving21_=Moving21;
        }

      Moving00=GetIdealMASmooth(wX1,wX2,price[bar-1],price[bar],Moving01);                    
      Moving10=GetIdealMASmooth(wY1,wY2,Moving01,    Moving00,  Moving11);
      Moving20=GetIdealMASmooth(wZ1,wZ2,Moving11,    Moving10,  Moving21);
      //----                       
      Moving01 = Moving00;
      Moving11 = Moving10;
      Moving21 = Moving20;
      //---- 
      ExtLineBuffer[bar]=Moving20;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
