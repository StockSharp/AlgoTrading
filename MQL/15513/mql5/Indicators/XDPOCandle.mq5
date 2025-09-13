//+---------------------------------------------------------------------+
//|                                                      XDPOCandle.mq5 |
//|                                  Copyright � 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "XDPOCandle"
//---- ����� ������ ����������
#property version   "1.00"
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrOrange,clrGray,clrGreenYellow
//---- ����������� ����� ����������
#property indicator_label1  "Open;High;Low;Close"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
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
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input Smooth_Method MA_Method1=MODE_SMA_; //����� ���������� ������� ����������� 
input uint Length1=12; //�������  ������� �����������                    
input int Phase1=15; //�������� ������� �����������,
  //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
  // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_T3; //����� ���������� ������� ����������� 
input uint Length2 = 5; //�������  ������� ����������� 
input int Phase2=15;  //�������� ������� �����������,
  //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
  // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int Ind_Handle[4];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(MA_Method1, Length1, Phase1);
   min_rates_total+=GetStartBars(MA_Method2, Length2, Phase2);
//---- ��������� ������� ���������� iXDPO
   Ind_Handle[0]=iCustom(NULL,0,"XDPO",MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,PRICE_OPEN,0);
   if(Ind_Handle[0]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iXDPO["+string(0)+"]!");
      return(INIT_FAILED);
     }
//---
   Ind_Handle[1]=iCustom(NULL,0,"XDPO",MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,PRICE_HIGH,0);
   if(Ind_Handle[1]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iXDPO["+string(1)+"]!");
      return(INIT_FAILED);
     }
//---
   Ind_Handle[2]=iCustom(NULL,0,"XDPO",MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,PRICE_LOW,0);
   if(Ind_Handle[2]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iXDPO["+string(2)+"]!");
      return(INIT_FAILED);
     }
//---
   Ind_Handle[3]=iCustom(NULL,0,"XDPO",MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,PRICE_CLOSE,0);
   if(Ind_Handle[3]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iXDPO["+string(3)+"]!");
      return(INIT_FAILED);
     }
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������� ��� � ����������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,3);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="XDPOCandle";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
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
   if(BarsCalculated(Ind_Handle[0])<rates_total
      || BarsCalculated(Ind_Handle[1])<rates_total
      || BarsCalculated(Ind_Handle[2])<rates_total
      || BarsCalculated(Ind_Handle[3])<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
//---- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//---
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Ind_Handle[0],1,0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[1],1,0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[2],1,0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[3],1,0,to_copy,ExtCloseBuffer)<=0) return(RESET);
//---- �������� ���� ����������� � ����������� ������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Max=MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      double Min=MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      //---
      ExtHighBuffer[bar]=MathMax(Max,ExtHighBuffer[bar]);
      ExtLowBuffer[bar]=MathMin(Min,ExtLowBuffer[bar]);
      //---
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
