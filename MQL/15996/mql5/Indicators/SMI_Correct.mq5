//+------------------------------------------------------------------+
//|                                                  SMI_Correct.mq5 |
//|                                Copyright � 2016, transport_david | 
//|                                                                  | 
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2016, transport_david"
//---- ��������� ����������
#property link      ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrAqua,clrMagenta
//--- ����������� ����� ����������
#property indicator_label1  "SMI_Correct"
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0       // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
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
input uint ExtrPeriod=13; //������ ������ �����������                  
input Smooth_Method MA_Method1=MODE_EMA_; //����� ���������� ������� ����������� 
input uint Length1=25; //�������  ������� �����������                    
input int Phase1=15; //�������� ������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_JJMA; //����� ���������� ������� ����������� 
input uint Length2=3; //�������  ������� ����������� 
input int Phase2=15;  //�������� ������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method3=MODE_JJMA; //����� ���������� �������� ����������� 
input uint Length3 = 5; //�������  �������� ����������� 
input int Phase3=15;  //�������� �������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE_;//������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
input int HighLevel=+50;
input int MiddleLevel=0;
input int LowLevel=-50;
input color HighLevelsColor=clrBlue;
input color MiddleLevelsColor=clrGray;
input color LowLevelsColor=clrRed;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double SMIBuffer[];
double TriggerBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=int(ExtrPeriod);
   min_rates_2=min_rates_1+GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_3=min_rates_2+GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=min_rates_3+GetStartBars(MA_Method3,Length3,Phase3);

//---- ����������� ������������� ������� SMIBuffer[] � ������������ �����
   SetIndexBuffer(0,SMIBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� TriggerBuffer[] � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="SMI_Correct";
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ����������  �������������� ������� ���������� 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- � �������� ������ ����� �������������� ������� ������������ ����� � ������� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,HighLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,MiddleLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,LowLevelsColor);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//----
   return(0);
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
   int first,bar,barx;
   double HH,LL,price,SM,HQ,XSM,XHQ,XXSM,XXHQ;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_1;                   // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      HH=-9999999999.0;
      LL=+9999999999.0;
      for(int index=0; index<int(ExtrPeriod); index++)
       {
         barx=bar-index;
         if(high[barx]>HH) HH=high[barx];
         if(low[barx]<LL) LL=low[barx];
       }
      price=PriceSeries(IPC,bar,open,low,high,close);
      SM=price-(HH+LL)/2.0;
      HQ=HH-LL;
      XSM=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method1,Phase1,Length1,SM,bar,false);
      XHQ=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method1,Phase1,Length1,HQ,bar,false);     
      XXSM=XMA3.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,XSM,bar,false);
      XXHQ=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,XHQ,bar,false);
      if(XXHQ) SMIBuffer[bar]=100.0*(XXSM/(XXHQ/2)); 
      else SMIBuffer[bar]=100.0;
      TriggerBuffer[bar]=XMA5.XMASeries(min_rates_3,prev_calculated,rates_total,MA_Method3,Phase3,Length3,SMIBuffer[bar],bar,false);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
