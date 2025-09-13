//+--------------------------------------------------------------------------+
//|                                                         Laguerre_ADX.mq5 |
//|                         Copyright � 2005, Emerald King / transport_david | 
//| http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/ | 
//+--------------------------------------------------------------------------+
#property copyright "Copyright � 2007, Emerald King / transport_david"
#property link "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1 clrDodgerBlue,clrDeepPink
//--- ����������� ����� ����� ����������
#property indicator_label1  "Laguerre_ADX"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level2 0.75
#property indicator_level3 0.45
#property indicator_level4 0.15
//---- � �������� ����� ����� ��������������� ������ ����������� ������� ����
#property indicator_levelcolor clrMagenta
//---- � ����� ��������������� ������ ����������� �������� �����-�������
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input uint ADXPeriod=14;
input double gamma=0.764;
input int Shift=0;                                 // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double UpIndBuffer[],DnIndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ����� ���������� ��� ������� �����������
int ADX_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(ADXPeriod);

//---- ��������� ������ ���������� iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iADX");
      return(INIT_FAILED);
     }
//--- ������������� ������������ �������
   IndInit(0,UpIndBuffer,INDICATOR_DATA);
   IndInit(1,DnIndBuffer,INDICATOR_DATA);
//--- ������������� �����������
   PlotInit(0,EMPTY_VALUE,0,Shift);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| ������������� ������������� ������                               |
//+------------------------------------------------------------------+    
void IndInit(int Number,double &Buffer[],ENUM_INDEXBUFFER_TYPE Type)
  {
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(Number,Buffer,Type);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Buffer,true);
//---
  }
//+------------------------------------------------------------------+
//| ������������� ����������                                         |
//+------------------------------------------------------------------+    
void PlotInit(int Number,double Empty_Value,int Draw_Begin,int nShift)
  {
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(Number,PLOT_DRAW_BEGIN,Draw_Begin);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(Number,PLOT_EMPTY_VALUE,Empty_Value);
//--- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(Number,PLOT_SHIFT,nShift);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
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
   if(BarsCalculated(ADX_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,LDMIP=0,LDMIM=0,CU,CD,DMIP[],DMIM[];
//---- ���������� ����������� ���������� ��� �������� �������������� �������� ������������
   static double pL0_,pL1_,pL2_,pL3_,pL0A_,pL1A_,pL2A_,pL3A_;
   static double mL0_,mL1_,mL2_,mL3_,mL0A_,mL1A_,mL2A_,mL3A_;

//---- ������ ���������� ������ ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      //---- ��������� ������������� ��������� �������������
      pL0_ = 0.0;
      pL1_ = 0.0;
      pL2_ = 0.0;
      pL3_ = 0.0;
      pL0A_ = 0.0;
      pL1A_ = 0.0;
      pL2A_ = 0.0;
      pL3A_ = 0.0;
      //---- ��������� ������������� ��������� �������������
      mL0_ = 0.0;
      mL1_ = 0.0;
      mL2_ = 0.0;
      mL3_ = 0.0;
      mL0A_ = 0.0;
      mL1A_ = 0.0;
      mL2A_ = 0.0;
      mL3A_ = 0.0;
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+1;

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ADX_Handle,PLUSDI_LINE,0,to_copy,DMIP)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(DMIP,true);

//---- ��������������� �������� ����������
   L0 = pL0_;
   L1 = pL1_;
   L2 = pL2_;
   L3 = pL3_;
   L0A = pL0A_;
   L1A = pL1A_;
   L2A = pL2A_;
   L3A = pL3A_;

//---- �������� ���� ������� ���������� Laguerre_PlusDi
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //----
      L0 = (1 - gamma) * DMIP[bar] + gamma * L0A;
      L1 = - gamma * L0 + L0A + gamma * L1A;
      L2 = - gamma * L1 + L1A + gamma * L2A;
      L3 = - gamma * L2 + L2A + gamma * L3A;
      //----
      CU = 0;
      CD = 0;
      //---- 
      if(L0 >= L1) CU  = L0 - L1; else CD  = L1 - L0;
      if(L1 >= L2) CU += L1 - L2; else CD += L2 - L1;
      if(L2 >= L3) CU += L2 - L3; else CD += L3 - L2;
      //----
      if(CU+CD!=0) LDMIP=CU/(CU+CD);

      //---- ������������� ������ ������������� ������ ���������� ��������� LRSI
      UpIndBuffer[bar]=LDMIP;

      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(bar==1)
        {
         pL0_ = L0;
         pL1_ = L1;
         pL2_ = L2;
         pL3_ = L3;
         pL0A_ = L0A;
         pL1A_ = L1A;
         pL2A_ = L2A;
         pL3A_ = L3A;
        }
     }
     
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ADX_Handle,MINUSDI_LINE,0,to_copy,DMIM)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(DMIM,true);

     
//---- ��������������� �������� ����������
   L0 = mL0_;
   L1 = mL1_;
   L2 = mL2_;
   L3 = mL3_;
   L0A = mL0A_;
   L1A = mL1A_;
   L2A = mL2A_;
   L3A = mL3A_;

//---- �������� ���� ������� ���������� Laguerre_MinusDi
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //----
      L0 = (1 - gamma) * DMIM[bar] + gamma * L0A;
      L1 = - gamma * L0 + L0A + gamma * L1A;
      L2 = - gamma * L1 + L1A + gamma * L2A;
      L3 = - gamma * L2 + L2A + gamma * L3A;
      //----
      CU = 0;
      CD = 0;
      //---- 
      if(L0 >= L1) CU  = L0 - L1; else CD  = L1 - L0;
      if(L1 >= L2) CU += L1 - L2; else CD += L2 - L1;
      if(L2 >= L3) CU += L2 - L3; else CD += L3 - L2;
      //----
      if(CU+CD!=0) LDMIM=CU/(CU+CD);

      //---- ������������� ������ ������������� ������ ���������� ��������� LRSI
      DnIndBuffer[bar]=LDMIM;

      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(bar==1)
        {
         mL0_ = L0;
         mL1_ = L1;
         mL2_ = L2;
         mL3_ = L3;
         mL0A_ = L0A;
         mL1A_ = L1A;
         mL2A_ = L2A;
         mL3A_ = L3A;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
