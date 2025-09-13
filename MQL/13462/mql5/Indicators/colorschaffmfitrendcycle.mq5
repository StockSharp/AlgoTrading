//+------------------------------------------------------------------+
//|                                     ColorSchaffMFITrendCycle.mq5 |
//|                                  Copyright � 2011, EarnForex.com |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2011, EarnForex.com"
#property link      "http://www.earnforex.com"
#property description "Schaff Trend Cycle - Cyclical Stoch over Stoch over MFI MACD."
#property description "The code adapted Nikolay Kositsin."
//---- ����� ������ ����������
#property version   "2.10"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------------------+
//| ��������� ��������� ����������                |
//+-----------------------------------------------+
//---- ��������� ���������� � ���� ������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ������������
#property indicator_color1 clrDarkOrange,clrMediumOrchid,clrGold,clrPlum,clrLightGreen,clrDodgerBlue,clrGreen,clrMediumSeaGreen
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "Schaff MFI Trend Cycle"
//+-----------------------------------------------+
//| ��������� ����������� �������������� �������  |
//+-----------------------------------------------+
//---- ������������ ������� � ������ ������ ���� ����������
#property indicator_minimum -110
#property indicator_maximum +110
//+-----------------------------------------------+
//| ���������� ��������                           |
//+-----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------------------+
//| ������� ��������� ����������                  |
//+-----------------------------------------------+
input uint Fast_MFI = 23; // ������ �������� MFI
input uint Slow_MFI = 50; // ������ ���������� MFI
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // ����� 
input uint Cycle=10; // ������ ��������������� �����������
input int HighLevel=+60;
input int MiddleLevel=0;
input int LowLevel=-60;
//+-----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double STC_Buffer[];
double ColorSTC_Buffer[];
//----
int Count[];
bool st1_pass,st2_pass;
double MACD[],ST[],Factor;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//---- ���������� ������������� ���������� ��� ������� �����������
int Ind1_Handle,Ind2_Handle;
//+------------------------------------------------------------------+
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Rates_total,
                          int Bar)
  {
//----
   if(!Bar) return;
//----
   int numb;
   static int count=1;
   count--;
//----
   if(count<0) count=int(Cycle)-1;
//----
   for(int iii=0; iii<int(Cycle); iii++)
     {
      numb=iii+count;
      if(numb>int(Cycle)-1) numb-=int(Cycle);
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=int(Fast_MFI);
   min_rates_2=int(Slow_MFI);
   min_rates_total=3*MathMax(min_rates_1,min_rates_2)+int(Cycle)+1;
//--- ��������� ������ ���������� MA 1
   Ind1_Handle=iMFI(Symbol(),PERIOD_CURRENT,Fast_MFI,VolumeType);
   if(Ind1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� MFI 1");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� MA 2
   Ind2_Handle=iMFI(Symbol(),PERIOD_CURRENT,Slow_MFI,VolumeType);
   if(Ind2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� MFI 2");
      return(INIT_FAILED);
     }
//---- ������������� ������ ��� ������� ����������
   if(ArrayResize(ST,Cycle)<int(Cycle))
     {
      Print("�� ������� ������������ ������ ��� ������ ST");
      return(INIT_FAILED);
     }
   if(ArrayResize(MACD,Cycle)<int(Cycle))
     {
      Print("�� ������� ������������ ������ ��� ������ MACD");
      return(INIT_FAILED);
     }
   if(ArrayResize(Count,Cycle)<int(Cycle))
     {
      Print("�� ������� ������������ ������ ��� ������ Count");
      return(INIT_FAILED);
     }
//---- ������������� ��������  
   Factor=0.5;
   st1_pass = false;
   st2_pass = false;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,STC_Buffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(STC_Buffer,true);
//---- ����������� ������������� ������� � �������� �����
   SetIndexBuffer(1,ColorSTC_Buffer,INDICATOR_COLOR_INDEX);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorSTC_Buffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Schaff MFI Trend Cycle( ",Fast_MFI,", ",Slow_MFI,", ",Cycle," )");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ����������  �������������� ������� ���������� 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- � �������� ������ ����� �������������� ������� ������������ ����� � ������� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrMediumSeaGreen);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Schaff MFI Trend Cycle                                           |
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
//---- 
   if(rates_total<min_rates_total) return(RESET);
   if(BarsCalculated(Ind1_Handle)<Bars(Symbol(),PERIOD_CURRENT)) return(prev_calculated);
   if(BarsCalculated(Ind2_Handle)<Bars(Symbol(),PERIOD_CURRENT)) return(prev_calculated);
//---- ���������� ���������� � ��������� ������  
   double fastMFI[],slowMFI[],LLV,HHV;
//---- ���������� ������������� ����������
   int limit,to_copy,bar,Bar0,Bar1;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-2; // ��������� ����� ��� ������� ���� �����
      STC_Buffer[limit+1]=0;
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Ind1_Handle,0,0,to_copy,fastMFI)<=0) return(RESET);
   if(CopyBuffer(Ind2_Handle,0,0,to_copy,slowMFI)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(fastMFI,true);
   ArraySetAsSeries(slowMFI,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Bar0=Count[0];
      Bar1=Count[1];
      //----
      MACD[Bar0]=fastMFI[bar]-slowMFI[bar];
      //----
      LLV=MACD[ArrayMinimum(MACD)];
      HHV=MACD[ArrayMaximum(MACD)];
      //---- ������ ������� ����������
      if(HHV-LLV!=0) ST[Bar0]=((MACD[Bar0]-LLV)/(HHV-LLV))*100;
      else           ST[Bar0]=ST[Bar1];
      //----
      if(st1_pass) ST[Bar0]=Factor *(ST[Bar0]-ST[Bar1])+ST[Bar1];
      st1_pass=true;
      //----
      LLV=ST[ArrayMinimum(ST)];
      HHV=ST[ArrayMaximum(ST)];
      //---- ������ ������� ����������
      if(HHV-LLV!=0) STC_Buffer[bar]=((ST[Bar0]-LLV)/(HHV-LLV))*200-100;
      else           STC_Buffer[bar]=STC_Buffer[bar+1];
      //---- ����������� ������� ����������
      if(st2_pass) STC_Buffer[bar]=Factor *(STC_Buffer[bar]-STC_Buffer[bar+1])+STC_Buffer[bar+1];
      st2_pass=true;
      //---- �������� ������� ��������� � ��������� ������� �� ����� ����
      Recount_ArrayZeroPos(Count,rates_total,bar);
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) limit=rates_total-min_rates_total;
//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Sts=STC_Buffer[bar];
      double dSts=Sts-STC_Buffer[bar+1];
      int clr=4;
      //----
      if(Sts>0)
        {
         if(Sts>HighLevel)
           {
            if(dSts>=0) clr=7;
            else clr=6;
           }
         else
           {
            if(dSts>=0) clr=5;
            else clr=4;
           }
        }
      //----  
      if(Sts<0)
        {
         if(Sts<LowLevel)
           {
            if(dSts<0) clr=0;
            else clr=1;
           }
         else
           {
            if(dSts<0) clr=2;
            else clr=3;
           }
        }
      //----  
      ColorSTC_Buffer[bar]=clr;
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
