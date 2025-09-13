//+---------------------------------------------------------------------+
//|                                                BinaryWave_StDev.mq5 | 
//|                                             Copyright � 2009, LeMan |
//|                                                    b-market@mail.ru |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2009, LeMan"
#property link      "b-market@mail.ru"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ��� ������� � ��������� ���������� ������������ ����� �������
#property indicator_buffers 6
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   5
//+----------------------------------------------+
//|  ��������� ��������� ����� ����������        |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color1  clrOrange,clrGray,clrDodgerBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "BinaryWave"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color2  clrRed
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ������������� ����
#property indicator_color3  clrAqua
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ����� ����� ����������
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color4  clrRed
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 5 � ���� �������
#property indicator_type5   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ������������� ����
#property indicator_color5  clrAqua
//---- ������� ����� ���������� 5 ����� 4
#property indicator_width5  4
//---- ����������� ����� ����� ����������
#property indicator_label5  "Up_Signal 2"
//+-----------------------------------------------+
//| ��������� ����������� �������������� �������  |
//+-----------------------------------------------+
#property indicator_level1  0
#property indicator_levelcolor clrRed
#property indicator_levelstyle STYLE_SOLID
//+-----------------------------------------------+
//|  ���������� ��������                          |
//+-----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------------------+
//|  �������� ������ CXMA                         |
//+-----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+-----------------------------------------------+
//|  ���������� ������������                      |
//+-----------------------------------------------+
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
//+-----------------------------------------------+
//|  ������� ��������� ����������                 |
//+-----------------------------------------------+
//--- ��� �����������. ���� ����, ��������� �� ��������� � ������� �����
input double WeightMA    = 1.0;
input double WeightMACD  = 1.0;
input double WeightOsMA  = 1.0;
input double WeightCCI   = 1.0;
input double WeightMOM   = 1.0;
input double WeightRSI   = 1.0;
input double WeightADX   = 1.0;
//---- ��������� ����������� ��������
input int   MAPeriod=13;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//---- ��������� MACD
input int   FastMACD     = 12;
input int   SlowMACD     = 26;
input int   SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//---- ��������� OsMA
input int   FastPeriod   = 12;
input int   SlowPeriod   = 26;
input int   SignalPeriod = 9;
input ENUM_APPLIED_PRICE   OsMAPrice=PRICE_CLOSE;
//---- ��������� CCI
input int   CCIPeriod=14;
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_MEDIAN;
//---- ��������� �������
input int   MOMPeriod=14;
input ENUM_APPLIED_PRICE   MOMPrice=PRICE_CLOSE;
//---- ��������� RSI
input int   RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
//---- ��������� ADX
input int   ADXPeriod=14;
//---- ��������� ����������� �����
input Smooth_Method bMA_Method=MODE_JJMA; //����� ����������
input int bLength=5; //������� �����������                    
input int bPhase=100; //�������� �����������,
                      //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input double dK1=1.5;  //����������� 1 ��� ������������� �������
input double dK2=2.5;  //����������� 2 ��� ������������� �������
input uint std_period=9; //������ ������������� �������
input int Shift=0; //����� ���������� �� ����������� � �����
//+-----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//----
double dWave[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_total_1;
//---- ���������� ����� ���������� ��� ������� �����������
int MA_Handle,MACD_Handle,OsMA_Handle,CCI_Handle,MOM_Handle,RSI_Handle,ADX_Handle;
//+------------------------------------------------------------------+
//| ���������� ��������� ���� �������� ������������ ��������         |
//+------------------------------------------------------------------+    
double MAClose(int bar,double &MaArray[],const double &Close[])
  {
//----
   if(WeightMA>0)
     {
      if(Close[bar]-MaArray[bar]>0) return(+WeightMA);
      if(Close[bar]-MaArray[bar]<0) return(-WeightMA);
      //if(Close[bar]-MaArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| ���������� ������ MACD                                           |
//+------------------------------------------------------------------+    
double MACD(int bar,double &MacdArray[])
  {
//----
   if(WeightMACD>0)
     {
      if(MacdArray[bar]-MacdArray[bar+1]>0) return(+WeightMACD);
      if(MacdArray[bar]-MacdArray[bar+1]<0) return(-WeightMACD);
      //if(MacdArray[bar]-MacdArray[bar+1]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| ���������� ��������� OsMa ������������ ����                      |
//+------------------------------------------------------------------+    
double OsMA(int bar,double &OsMAArray[])
  {
//----
   if(WeightOsMA>0)
     {
      if(OsMAArray[bar]>0) return(+WeightOsMA);
      if(OsMAArray[bar]<0) return(-WeightOsMA);
      //if(OsMAArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| ���������� ��������� CCI ������������ ����                       |
//+------------------------------------------------------------------+    
double CCI(int bar,double &CCIArray[])
  {
//----
   if(WeightCCI>0)
     {
      if(CCIArray[bar]>0) return(+WeightCCI);
      if(CCIArray[bar]<0) return(-WeightCCI);
      //if(CCIArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| ���������� ��������� Momentum ������������ 100                   |
//+------------------------------------------------------------------+    
double MOM(int bar,double &MOMArray[])
  {
//----
   if(WeightMOM>0)
     {
      if(MOMArray[bar]>100) return(+WeightMOM);
      if(MOMArray[bar]<100) return(-WeightMOM);
      //if(MOMArray[bar]==100) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| ���������� ��������� RSI ������������ 50                         |
//+------------------------------------------------------------------+    
double RSI(int bar,double &RSIArray[])
  {
//----
   if(WeightRSI>0)
     {
      if(RSIArray[bar]>50) return(+WeightRSI);
      if(RSIArray[bar]<50) return(-WeightRSI);
      //if(RSIArray[bar]==100) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| ���������� ��������� DMI                                         |
//+------------------------------------------------------------------+    
double ADX(int bar,double &DMIPArray[],double &DMIMArray[])
  {
//----
   if(WeightADX>0)
     {
      if(DMIPArray[bar]>DMIMArray[bar]) return(+WeightADX);
      if(DMIPArray[bar]<DMIMArray[bar]) return(-WeightADX);
      //if(DMIPArray[bar]==DMIMArray[bar]) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+   
//| BinaryWave indicator initialization function                     | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total_1=MathMax(MAPeriod,MathMax(SlowPeriod,MathMax(CCIPeriod,MathMax(SlowMACD,MOMPeriod))))+1;
   min_rates_total=min_rates_total_1+XMA1.GetStartBars(bMA_Method,bLength,bPhase);
   min_rates_total+=int(std_period);
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(dWave,std_period);
   
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("bLength", bLength);
   XMA1.XMAPhaseCheck("bPhase", bPhase, bMA_Method);

//---- ��������� ������ ���������� iMA
   MA_Handle=iMA(NULL,0,MAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMACD
   MACD_Handle=iMACD(NULL,0,FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(MACD_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iOsMA
   OsMA_Handle=iOsMA(NULL,0,FastPeriod,SlowPeriod,SignalPeriod,OsMAPrice);
   if(OsMA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iOsMA");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iCCI
   CCI_Handle=iCCI(NULL,0,CCIPeriod,CCIPrice);
   if(CCI_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iCCI");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMomentum
   MOM_Handle=iMomentum(NULL,0,MOMPeriod,MOMPrice);
   if(MOM_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMomentum");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iRSI
   RSI_Handle=iRSI(NULL,0,RSIPeriod,RSIPrice);
   if(RSI_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iADX");
      return(INIT_FAILED);
     }

//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtLineBuffer,true);
   
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorExtLineBuffer,true);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BearsBuffer1,true);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BullsBuffer1,true);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BearsBuffer2,true);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BullsBuffer2,true);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(bMA_Method);
   StringConcatenate(shortname,"BinaryWave(",bLength,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| BinaryWave iteration function                                    | 
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
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MACD_Handle)<rates_total
      || BarsCalculated(OsMA_Handle)<rates_total
      || BarsCalculated(CCI_Handle)<rates_total
      || BarsCalculated(MOM_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar,maxbar;
   double tmp,MA_[],MACD_[],OsMA_[],CCI_[],MOM_[],RSI_[],DMIP_[],DMIM_[];
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2,wave;

//---- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      to_copy=rates_total; // ��������� ���������� ���� �����
      limit=rates_total-2; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // ��������� ���������� ������ ����� �����
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }


//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA_)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,0,0,to_copy+1,MACD_)<=0) return(RESET);
   if(CopyBuffer(OsMA_Handle,0,0,to_copy,OsMA_)<=0) return(RESET);
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI_)<=0) return(RESET);
   if(CopyBuffer(MOM_Handle,0,0,to_copy,MOM_)<=0) return(RESET);
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,1,0,to_copy,DMIP_)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,DMIM_)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(MA_,true);
   ArraySetAsSeries(MACD_,true);
   ArraySetAsSeries(OsMA_,true);
   ArraySetAsSeries(CCI_,true);
   ArraySetAsSeries(MOM_,true);
   ArraySetAsSeries(RSI_,true);
   ArraySetAsSeries(DMIP_,true);
   ArraySetAsSeries(DMIM_,true);
   ArraySetAsSeries(close,true);

//----   
   maxbar=rates_total-min_rates_total_1-1;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      tmp=MAClose(bar,MA_,close)+MACD(bar,MACD_)+OsMA(bar,OsMA_)+CCI(bar,CCI_)+MOM(bar,MOM_)+RSI(bar,RSI_)+ADX(bar,DMIP_,DMIM_);
      ExtLineBuffer[bar]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,bMA_Method,bPhase,bLength,tmp,bar,true);
     }
     
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      limit--;

//---- �������� ���� ��������� ���������� �����
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=1;
      if(ExtLineBuffer[bar+1]<ExtLineBuffer[bar]) clr=2;
      if(ExtLineBuffer[bar+1]>ExtLineBuffer[bar]) clr=0;
      ColorExtLineBuffer[bar]=clr;
     }
     
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total+1;
//---- �������� ���� ������� ���������� ����������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- ��������� ���������� ���������� � ������ ��� ������������� ����������
      for(int iii=0; iii<int(std_period); iii++) dWave[iii]=ExtLineBuffer[bar+iii]-ExtLineBuffer[bar+iii+1];

      //---- ������� ������� ������� ���������� ����������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dWave[iii];
      SMAdif=Sum/std_period;

      //---- ������� ����� ��������� ��������� ���������� � ��������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dWave[iii]-SMAdif,2);

      //---- ���������� �������� �������� ������������������� ���������� StDev �� ���������� ����������
      StDev=MathSqrt(Sum/std_period);

      //---- ������������� ����������
      dstd=NormalizeDouble(dWave[0],_Digits+2);
      Filter1=NormalizeDouble(dK1*StDev,_Digits+2);
      Filter2=NormalizeDouble(dK2*StDev,_Digits+2);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      wave=ExtLineBuffer[bar];

      //---- ���������� ������������ ��������
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=wave; //���� ���������� �����
      if(dstd<-Filter2) BEARS2=wave; //���� ���������� �����
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=wave; //���� ���������� �����
      if(dstd>+Filter2) BULLS2=wave; //���� ���������� �����

      //---- ������������� ����� ������������ ������� ����������� ���������� 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
