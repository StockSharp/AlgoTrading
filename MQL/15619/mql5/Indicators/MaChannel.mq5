//+---------------------------------------------------------------------+
//|                                                       MaChannel.mq5 | 
//|                                     Copyright � 2012, Ivan Kornilov | 
//|                                                    excelf@gmail.com | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2012, Ivan Kornilov"
#property link "excelf@gmail.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type1 DRAW_ARROW
//---- � �������� ������� ���������� �����������
#property indicator_color1 clrBlue
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1 1
//---- ����������� ����� ���������� �����
#property indicator_label1  "MaChannel Up"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type2 DRAW_ARROW
//---- � �������� ������� ���������� �����������
#property indicator_color2 clrRed
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width2 1
//---- ����������� ����� ���������� �����
#property indicator_label2  "MaChannel Down"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� ������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� �����������
#property indicator_color3  clrDodgerBlue
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 1
#property indicator_width3  1
//---- ����������� ������ ����� ����������
#property indicator_label3  "Buy MaChannel signal"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� ������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� �����������
#property indicator_color4  clrMagenta
//---- ����� ���������� 2 - ����������� ������
#property indicator_style4  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width4  1
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Sell MaChannel signal"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
/*enum SmoothMethod - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_SMA_;  //����� ����������
input uint XLength=12;                     //������� �����������                    
input int XPhase=15;                       //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input uint Renge=600;                      //������� ������ ������� � �������
input bool oneWay= true;                   //������������� �������� ����������
input int Shift=0;                         //����� ���������� �� �����������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//---- ���������� ���������� �������� ������������� ������ �������
double dRenge;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| MaChannel indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase)+2;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//---- ������������� ������ �� ���������
   dRenge=_Point*Renge;
   
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,119);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,119);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(2,PLOT_ARROW,117);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,117);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"MaChannel");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| MaChannel iteration function                                     | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ���������� � ��������� ������  
   double up,down;
   static double up_prev,down_prev;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar,trend;
   static int trend_prev;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
   {
      first=0; // ��������� ����� ��� ������� ���� �����
      trend_prev=0;
    }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;
      trend=trend_prev;
      up=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,high[bar],bar,false)+dRenge;
      down=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,low[bar],bar,false)+dRenge;

      if(oneWay)
        {
         if(trend_prev==+1)
           {
            if(down<down_prev && down_prev) down=down_prev;
           }
         else if(trend_prev==-1)
           {
            if(up>up_prev && up_prev) up=up_prev;
           }
        }

      if(high[bar]>up)
        {
         trend=+1;
        }
      else if(low[bar]<down)
        {
         trend=-1;
        }

      if(trend==-1.0)
        {
         TrendDown[bar]=up;
        }
      else if(trend==+1.0)
        {
         TrendUp[bar]=down;
        }
        
      if(trend_prev<=0 && trend>0) SignUp[bar]=TrendUp[bar];
      if(trend_prev>=0 && trend<0) SignDown[bar]=TrendDown[bar];

       if(bar<rates_total-1)
         {
          up_prev=up;
          down_prev=down;
          trend_prev=trend;         
         }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
