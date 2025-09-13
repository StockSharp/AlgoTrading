//+------------------------------------------------------------------+
//|                                                  CHOWithFlat.mq5 |
//|                                 Copyright � 2014, Powered byStep | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property description "Money Flow Index With Flat"
//---- ��������� ����������
#property copyright "Copyright � 2014, Powered byStep"
//---- ��������� ����������
#property link      ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 3
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrDodgerBlue,clrOrange
//---- ����������� ����� ����������
#property indicator_label1  "CHO Oscillator"
//+----------------------------------------------+
//| ��������� ��������� ���������� 2             |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� ����� ����
#property indicator_color2  clrSlateGray
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 5
#property indicator_width2  5
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Flat"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Smooth_Method
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
   MODE_AMA    //AMA
  };
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint                BBPeriod=20;                 // ������ ��� ������� �����������
input double              StdDeviation=2.0;            // �������� �����������
input ENUM_APPLIED_PRICE  applied_price=PRICE_CLOSE;   // ��� ���� �����������
input Smooth_Method XMA_Method=MODE_SMA;               // ����� ����������
input uint FastPeriod=3;                               // ������ �������� ����������
input uint SlowPeriod=10;                              // ����� ���������� ����������
input int XPhase=15;                                   // �������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;      // ����� 
input uint                MAPeriod=13;                 // ������ ���������� ���������� �����
input  ENUM_MA_METHOD     MAType=MODE_SMA;             // ��� ���������� ���������� �����
input uint                flat=100;                    // �������� ����� � �������
input int                 Shift=0;                     // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[];
double SignalBuffer[];
double IndBuffer1[];
//---- ���������� ������������� ���������� ��� ������� �����������
int BB_Handle,CHO_Handle,MA_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(MathMax(BBPeriod,FastPeriod),SlowPeriod));
//---- ��������� ������ ���������� iBands
   BB_Handle=iBands(Symbol(),PERIOD_CURRENT,BBPeriod,0,StdDeviation,applied_price);
   if(BB_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iBands");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iCHO
   CHO_Handle=iCustom(Symbol(),PERIOD_CURRENT,"CHO",XMA_Method,FastPeriod,SlowPeriod,XPhase,VolumeType);
   if(CHO_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iCHO");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMA
   MA_Handle=iMA(Symbol(),PERIOD_CURRENT,MAPeriod,0,MAType,CHO_Handle);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SignalBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,IndBuffer1,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer1,true);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//----
   IndicatorSetString(INDICATOR_SHORTNAME,"CHOWithFlat");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
                const double& low[],      // ������� ������ ��������� ���� ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(BB_Handle)<rates_total
      || BarsCalculated(CHO_Handle)<rates_total
      || BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double MainCHO[],SignCHO[],UpBB[],MainBB[];
//---- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(BB_Handle,UPPER_BAND,0,to_copy,UpBB)<=0) return(RESET);
   if(CopyBuffer(BB_Handle,BASE_LINE,0,to_copy,MainBB)<=0) return(RESET);
   if(CopyBuffer(CHO_Handle,MAIN_LINE,0,to_copy,MainCHO)<=0) return(RESET);
   if(CopyBuffer(MA_Handle,MAIN_LINE,0,to_copy,SignCHO)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(UpBB,true);
   ArraySetAsSeries(MainBB,true);
   ArraySetAsSeries(MainCHO,true);
   ArraySetAsSeries(SignCHO,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double res=(UpBB[bar]-MainBB[bar])/_Point;
      if(res<flat)
        {
         if(MainCHO[bar]>SignCHO[bar])
           {
            IndBuffer[bar]=0.00000001;
            SignalBuffer[bar]=0.00000001;
            IndBuffer1[bar]=0;
           }
         //----
         if(MainCHO[bar]<SignCHO[bar])
           {
            IndBuffer[bar]=0.00000001;
            SignalBuffer[bar]=0.00000001;
            IndBuffer1[bar]=0;
           }
        }
      else
        {
         IndBuffer1[bar]=EMPTY_VALUE;
         IndBuffer[bar]=MainCHO[bar];
         SignalBuffer[bar]=SignCHO[bar];
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
