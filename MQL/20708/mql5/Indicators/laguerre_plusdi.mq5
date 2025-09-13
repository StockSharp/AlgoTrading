//+--------------------------------------------------------------------------+
//|                                                      Laguerre_PlusDi.mq5 |
//|                         Copyright � 2005, Emerald King / transport_david | 
//| http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/ | 
//+--------------------------------------------------------------------------+
#property copyright "Copyright � 2007, Emerald King / transport_david"
#property link "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 1
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� Teal ����
#property indicator_color1  clrTeal
//--- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1 2
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level2 0.75
#property indicator_level3 0.45
#property indicator_level4 0.15
//--- � �������� ����� ����� ��������������� ������ ����������� ������� ����
#property indicator_levelcolor clrMagenta
//--- � ����� ��������������� ������ ����������� �������� �����-�������
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint ADXPeriod=14;
input double gamma=0.764;
//+----------------------------------------------+
//--- ���������� ������������� �������, ������� � ����������
//--- ����� ����������� � �������� ������������� ������
double ExtLineBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������� ���������� ��� ������� �����������
int ADX_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(ADXPeriod);
//--- ��������� ������ ���������� iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iADX");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtLineBuffer,true);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Laguerre_PlusDi(",ADXPeriod,", ",gamma,")");
//--- �������� ����� ��� ����������� � ���� ������
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
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
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(ADX_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,LDMIP=0,CU,CD,DMIP[];
//--- ���������� ����������� ���������� ��� �������� �������������� �������� ������������
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//--- ������ ���������� ������ ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      //--- ��������� ������������� ��������� �������������
      L0_ = 0.0;
      L1_ = 0.0;
      L2_ = 0.0;
      L3_ = 0.0;
      L0A_ = 0.0;
      L1A_ = 0.0;
      L2A_ = 0.0;
      L3A_ = 0.0;
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ADX_Handle,PLUSDI_LINE,0,to_copy,DMIP)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(DMIP,true);
//--- ��������������� �������� ����������
   L0 = L0_;
   L1 = L1_;
   L2 = L2_;
   L3 = L3_;
   L0A = L0A_;
   L1A = L1A_;
   L2A = L2A_;
   L3A = L3A_;
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //---
      L0 = (1 - gamma) * DMIP[bar] + gamma * L0A;
      L1 = - gamma * L0 + L0A + gamma * L1A;
      L2 = - gamma * L1 + L1A + gamma * L2A;
      L3 = - gamma * L2 + L2A + gamma * L3A;
      //---
      CU = 0;
      CD = 0;
      //--- 
      if(L0 >= L1) CU  = L0 - L1; else CD  = L1 - L0;
      if(L1 >= L2) CU += L1 - L2; else CD += L2 - L1;
      if(L2 >= L3) CU += L2 - L3; else CD += L3 - L2;
      //---
      if(CU+CD!=0) LDMIP=CU/(CU+CD);
      //--- ������������� ������ ������������� ������ ���������� ��������� LRSI
      ExtLineBuffer[bar]=LDMIP;
      //--- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(bar==1)
        {
         L0_ = L0;
         L1_ = L1;
         L2_ = L2;
         L3_ = L3;
         L0A_ = L0A;
         L1A_ = L1A;
         L2A_ = L2A;
         L3A_ = L3A;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
