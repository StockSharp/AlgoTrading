//+------------------------------------------------------------------+ 
//|                                                 Laguerre_ROC.mq5 | 
//|                           Copyright � 2005, Emerald King , MTE&I | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, Emerald King , MTE&I"
#property link ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 3
#property indicator_buffers 3 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0                       // ��������� ��� �������� ��������� ������� �� �������� ����������
#define INDICATOR_NAME "Laguerre_ROC" // ��������� ��� ����� ����������
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- � �������� ������ ���������� ������������
#property indicator_color1  clrDarkOrange,clrBrown,clrGray,clrBlue,clrDeepSkyBlue
//---- ������� ����� ���������� 1 ����� 5
#property indicator_width1  5
//---- ����������� ����� ����������
#property indicator_label1  INDICATOR_NAME
//+----------------------------------------------+
//| ��������� ������ ���� ����������             |
//+----------------------------------------------+
#property indicator_maximum +1.1
#property indicator_minimum -0.1
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0
#property indicator_levelcolor clrBlueViolet
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+  
enum WIDTH
  {
   Width_1=1, //1
   Width_2,   //2
   Width_3,   //3
   Width_4,   //4
   Width_5    //5
  };
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum STYLE
  {
   SOLID_,       // �������� �����
   DASH_,        // ��������� �����
   DOT_,         // ���������� �����
   DASHDOT_,     // �����-���������� �����
   DASHDOTDOT_   // �����-���������� ����� � �������� �������
  };

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint vPeriod=5;                 // ������
input double gamma=0.500;             // ����������� ����������                
input double UpLevel=0.75;            // ������� ��������������� � %%
input double DnLevel=0.25;            // ������� ��������������� � %%
input color UpLevelsColor=clrMagenta; // ���� ������ ���������������
input color DnLevelsColor=clrMagenta; // ���� ������ ���������������
input STYLE Levelstyle=DASH_;         // ����� �������
input WIDTH  LevelsWidth=Width_1;     // ������� �������
input int  Shift=0;                   // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� 
   min_rates_total=int(vPeriod+1);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpIndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpIndBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnIndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnIndBuffer,true);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(2,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,INDICATOR_NAME);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- ��������� ��������� �����  
   IndicatorSetInteger(INDICATOR_LEVELS,3);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,UpLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,0,LevelsWidth);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,DnLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,DnLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,1,LevelsWidth);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,0.5);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,DASHDOTDOT_);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,2,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const int begin,          // ����� ������ ������������ ������� �����
                const double &price[]) // ������� ������ ��� ������� ����������
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total+begin) return(RESET);
//---- ���������� ����������� ���������� ��� �������� �������������� �������� ������������
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//---- ���������� ��������� ���������� 
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,CU,CD,ROC,LROC=0;
   int limit,bar,vbar,clr;
//---- ������� ������������ ���������� ���������� ������ �
//---- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1-begin; // ��������� ����� ��� ������� ���� �����
      //---- ��������� ������������� ��������� �������������
      bar=limit+1;
      vbar=limit+int(vPeriod)+1;
      ROC=(price[bar]-price[vbar])/price[vbar]+_Point;
      L0_ = ROC;
      L1_ = ROC;
      L2_ = ROC;
      L3_ = ROC;
      L0A_ = ROC;
      L1A_ = ROC;
      L2A_ = ROC;
      L3A_ = ROC;
      //---- ������������� ������ ������ ������� ��������� ���������� 1
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� ����� 
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(price,true);
//---- ��������������� �������� ����������
   L0 = L0_;
   L1 = L1_;
   L2 = L2_;
   L3 = L3_;
   L0A = L0A_;
   L1A = L1A_;
   L2A = L2A_;
   L3A = L3A_;
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      vbar=bar+int(vPeriod);
      ROC=(price[bar]-price[vbar])/price[vbar]+_Point;
      //----
      L0 = (1 - gamma) * ROC + gamma * L0A;
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
      if(CU+CD!=0) LROC=CU/(CU+CD);
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
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
      //----
      UpIndBuffer[bar]=LROC;
      DnIndBuffer[bar]=0.5;
      clr=2;
      if(LROC>UpLevel) clr=4;
      else if(LROC>0.5) clr=3;
      //----
      if(LROC<DnLevel) clr=0;
      else if(LROC<0.5) clr=1;
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
