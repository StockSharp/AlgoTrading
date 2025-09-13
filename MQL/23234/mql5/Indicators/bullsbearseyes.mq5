//+------------------------------------------------------------------+
//|                                               BullsBearsEyes.mq5 | 
//|                   Copyright � 2007, EmeraldKing, transport_david | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2007, EmeraldKing, transport_david"
#property link "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
#property description "BullsBearsEyes"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ���� BlueViolet
#property indicator_color1 BlueViolet
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "BullsBearsEyes"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level2 1.0
#property indicator_level3 0.75
#property indicator_level4 0.50
#property indicator_level5 0.25
#property indicator_level6 0.0
#property indicator_levelcolor LimeGreen
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input double period=13; // ������ ���������� ����������
input double gamma=0.6; // ����������� ����������� ����������
input int    Shift=0;   // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������� �������, ������� � ����������
//---- ����� ����������� � �������� ������������� ������
double BullsBearsEyes[];
//---- ���������� ������������� ���������� ��� ������� �����������
int Bears_Handle,Bulls_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| BullsBearsEyes indicator initialization function                 | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(period+1);

//---- ��������� ������ ���������� iBearsPower
   Bears_Handle=iBearsPower(NULL,0,int(period));
   if(Bears_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iBearsPower");

//---- ��������� ������ ���������� iBullsPower
   Bulls_Handle=iBullsPower(NULL,0,int(period));
   if(Bulls_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iBullsPower");

//---- ����������� ������������� ������� BullsBearsEyes[] � ������������ �����
   SetIndexBuffer(0,BullsBearsEyes,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(BullsBearsEyes,true);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"BullsBearsEyes(",period,", ",gamma,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| BullsBearsEyes iteration function                                | 
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
   if(BarsCalculated(Bears_Handle)<rates_total
      || BarsCalculated(Bulls_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- ���������� ���������� � ��������� ������  
   double result,Bears[],Bulls[],CU,CD;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A;
   static double L0_,L1_,L2_,L3_;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int limit,bar,to_copy;

//---- ������� ������������ ���������� ���������� ������
//---- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+1;

//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(Bears,true);
   ArraySetAsSeries(Bulls,true);

//---- �������� ����� ����������� ������ � ������  
   if(CopyBuffer(Bears_Handle,0,0,to_copy,Bears)<=0) return(RESET);
   if(CopyBuffer(Bulls_Handle,0,0,to_copy,Bulls)<=0) return(RESET);

//---- ��������������� �������� ����������
   L0=L0_;
   L1=L1_;
   L2=L2_;
   L3=L3_;

//---- �������� �������� �����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
        {
         L0_=L0;
         L1_=L1;
         L2_=L2;
         L3_=L3;
        }

      L0A=L0;
      L1A=L1;
      L2A=L2;
      L3A=L3;

      L0=(1.0-gamma)*(Bears[bar]+Bulls[bar])+gamma*L0A;
      L1=-gamma*L0+L0A+gamma*L1A;
      L2=-gamma*L1+L1A+gamma*L2A;
      L3=-gamma*L2+L2A+gamma*L3A;

      CU=0.0;
      CD=0.0;
      result=0.0;
      if(L0>=L1) CU=L0-L1; else CD=L1-L0;
      if(L1>=L2) CU+=L1-L2; else CD+=L2-L1;
      if(L2>=L3) CU+=L2-L3; else CD+=L3-L2;
      if(CU+CD!=0) result=CU/(CU+CD);

      BullsBearsEyes[bar]=result;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
