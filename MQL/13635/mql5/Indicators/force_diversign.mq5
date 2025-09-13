//+------------------------------------------------------------------+
//|                                              Force_DiverSign.mq5 | 
//|                                       Copyright � 2015, olegok83 | 
//|                           https://www.mql5.com/ru/users/olegok83 | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2015, olegok83"
#property link "https://www.mql5.com/ru/users/olegok83"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� Gold ����
#property indicator_color1  clrGold
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ��������� ����� ����������
#property indicator_label1  "Force_DiverSign Sell"
//+----------------------------------------------+
//| ��������� ��������� ������� ����������       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ������ ����� ���������� ����������� Aqua ����
#property indicator_color2  clrAqua
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ������ ����� ����������
#property indicator_label2 "Force_DiverSign Buy"
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input  ENUM_MA_METHOD   MAType1=MODE_EMA;          // ����� ���������� �������� ����������
input uint iPeriod1=3;                             // ������ �������� ����������
input  ENUM_MA_METHOD   MAType2=MODE_EMA;          // ����� ���������� ��������� ����������
input uint iPeriod2=7;                             // ������ ��������� ����������
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // �����
input int Shift=0;                                 // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double SellBuffer[],BuyBuffer[];
//--- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle,Ind_Handle1,Ind_Handle2;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- ������������� ���������� ���������� 
   int ATR_Period=10;
//---- ������������� ���������� ������ ������� ������
   min_rates_=int(MathMax(iPeriod1,iPeriod2));
   min_rates_total=min_rates_+int(MathMax(iPeriod1,iPeriod2))+5;
   min_rates_total=int(MathMax(min_rates_total,ATR_Period));
//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(Symbol(),PERIOD_CURRENT,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� Force1
   Ind_Handle1=iForce(Symbol(),PERIOD_CURRENT,iPeriod1,MAType1,VolumeType);
   if(Ind_Handle1==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Force1");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� Force2
   Ind_Handle2=iForce(Symbol(),PERIOD_CURRENT,iPeriod2,MAType2,VolumeType);
   if(Ind_Handle2==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Force2");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,174);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,174);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="Force_DiverSign";
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
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
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(Ind_Handle1)<rates_total
      || BarsCalculated(Ind_Handle2)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- ���������� ���������� � ��������� ������  
   double Ind1[],Ind2[],ATR[];
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int to_copy,limit,bar;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//----
   to_copy=limit+1;
//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(Ind1,true);
   ArraySetAsSeries(Ind2,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy+=4;
   if(CopyBuffer(Ind_Handle1,0,0,to_copy,Ind1)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle2,0,0,to_copy,Ind2)<=0) return(RESET);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //----
      if(SellCheck(open,close,bar))
         if(Ind1[bar+4]<Ind1[bar+3] && Ind1[bar+3]>Ind1[bar+2] && Ind1[bar+2]<Ind1[bar+1])
            if(Ind2[bar+4]<Ind2[bar+3] && Ind2[bar+3]>Ind2[bar+2] && Ind2[bar+2]<Ind2[bar+1])
              {
               if((Ind1[bar+3]>Ind1[bar+1] && Ind2[bar+3]<Ind2[bar+1])
                  || (Ind1[bar+3]<Ind1[bar+1] && Ind2[bar+3]>Ind2[bar+1])) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
              }
      //----
      if(BuyCheck(open,close,bar))
         if(Ind1[bar+4]>Ind1[bar+3] && Ind1[bar+3]<Ind1[bar+2] && Ind1[bar+2]>Ind1[bar+1])
            if(Ind2[bar+4]>Ind2[bar+3] && Ind2[bar+3]<Ind2[bar+2] && Ind2[bar+2]>Ind2[bar+1])
              {
               if((Ind1[bar+3]>Ind1[bar+1] && Ind2[bar+3]<Ind2[bar+1])
                  || (Ind1[bar+3]<Ind1[bar+1] && Ind2[bar+3]>Ind2[bar+1])) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
              }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| �������� �� ������� ������� ����� ����� �������� ��������        |
//+------------------------------------------------------------------+  
bool SellCheck(const double &Open[],const double &Close[],int index)
  {
//--- ����������� ������������� ������� � �������� ��������� �����
   if(Open[index+3]<Close[index+3] && Open[index+2]>Close[index+2] && Open[index+1]<Close[index+1]) return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| �������� �� ������� ������� ����� ����� �������� ��������        |
//+------------------------------------------------------------------+  
bool BuyCheck(const double &Open[],const double &Close[],int index)
  {
//--- ����������� ������������� ������� � �������� ��������� �����
   if(Open[index+3]>Close[index+3] && Open[index+2]<Close[index+2] && Open[index+1]>Close[index+1]) return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
