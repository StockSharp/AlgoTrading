//+------------------------------------------------------------------+
//|                                                     Laguerre.mq5 |
//|                             Copyright � 2010,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 1
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//--- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� Magenta ����
#property indicator_color1  Magenta
//--- �������� �������������� ������� ����������
#property indicator_level2 0.75
#property indicator_level3 0.45
#property indicator_level4 0.15
//--- � �������� ����� ����� ��������������� ������ ����������� ����� ����
#property indicator_levelcolor Blue
//--- � ����� ��������������� ������ ����������� �������� �����-�������
#property indicator_levelstyle STYLE_DASHDOTDOT
//--- ������� ��������� ����������
input double gamma=0.7;
//--- ���������� ������������� �������, ������� � ����������
//--- ����� ����������� � �������� ������������� ������
double ExtLineBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Laguerre(",gamma,")");
//--- �������� ����� ��� ����������� � ���� ������
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,     // ���������� ������� � ����� �� ������� ����
                const int prev_calculated, // ���������� ������� � ����� �� ���������� ����
                const int begin,           // ����� ������ ������������ ������� �����
                const double &price[])     // ������� ������ ��� ������� ����������
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<begin) return(0);
//--- ���������� ��������� ���������� 
   int first,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,LRSI=0,CU,CD;
//--- ���������� ����������� ���������� ��� �������� �������������� �������� ������������
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=begin; // ��������� ����� ��� ������� ���� �����
      //--- ��������� ������������� ��������� �������������
      L0_ = price[first];
      L1_ = price[first];
      L2_ = price[first];
      L3_ = price[first];
      L0A_ = price[first];
      L1A_ = price[first];
      L2A_ = price[first];
      L3A_ = price[first];
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
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
   for(bar=first; bar<rates_total; bar++)
     {
      //--- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==rates_total-1)
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

      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //---
      L0 = (1 - gamma) * price[bar] + gamma * L0A;
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
      if(CU+CD!=0) LRSI=CU/(CU+CD);
      //--- ������������� ������ ������������� ������ ���������� ��������� LRSI
      ExtLineBuffer[bar]=LRSI;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
