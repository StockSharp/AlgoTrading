//+------------------------------------------------------------------+
//|                                           DarvasBoxes_System.mq5 |
//|                               Copyright � 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2013, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "��������� ������� � �������������� ���������� DarvasBoxes"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 7
//---- ������������ ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������ ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����� ���������� ����������� WhiteSmoke ����
#property indicator_color1  clrWhiteSmoke
//---- ����������� ����� ����������
#property indicator_label1  "DarvasBoxes"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 2            |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� MediumSeaGreen ����
#property indicator_color2  clrMediumSeaGreen
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ����� ����������
#property indicator_label2  "Upper DarvasBoxes"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 3            |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �����
#property indicator_type3   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� Magenta ����
#property indicator_color3  clrMagenta
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ��������� ����� ����������
#property indicator_label3  "Lower DarvasBoxes"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 4            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type4 DRAW_COLOR_HISTOGRAM2
//---- � �������� ������ ������������� ����������� ������������
#property indicator_color4 clrDeepPink,clrPurple,clrGray,clrMediumBlue,clrDodgerBlue
//---- ����� ���������� - ��������
#property indicator_style4 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width4 2
//---- ����������� ����� ����������
#property indicator_label4 "DarvasBoxes_BARS"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input bool symmetry=true;
input uint   Shift=2;   // ����� ������ �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double Up1Buffer[],Dn1Buffer[];
double Up2Buffer[],Dn2Buffer[];
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(2+Shift);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,Up1Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Up1Buffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,Dn1Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Dn1Buffer,true);
   
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Up2Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Up2Buffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,Dn2Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Dn2Buffer,true);
   
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(4,UpIndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpIndBuffer,true);

//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(5,DnIndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnIndBuffer,true);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(6,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);
   
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 3 �� min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,0);
//---- ������������� ������ ������ ������� ��������� ���������� 4 �� min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"DarvasBoxes(",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
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
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- ���������� ����� ����������
   int limit;
//---- ���������� ����������� ����������
   static int state,STATE;
   static double box_top,box_bottom,BOX_TOP,BOX_BUTTOM;

//---- ������� ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
      BOX_TOP=high[limit+1];
      BOX_BUTTOM=low[limit+1];
      STATE=1;
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

//---- ��������������� �������� ����������
   state=STATE;
   box_top=BOX_TOP;
   box_bottom=BOX_BUTTOM;

//---- �������� ���� ������� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      switch(state)
        {
         case 1:  box_top=high[bar]; if(symmetry)box_bottom=low[bar]; break;
         case 2:  if(box_top<=high[bar]) box_top=high[bar]; break;
         case 3:  if(box_top> high[bar]) box_bottom=low[bar]; else box_top=high[bar]; break;
         case 4:  if(box_top > high[bar]) {if(box_bottom >= low[bar]) box_bottom=low[bar];} else box_top=high[bar]; break;
         case 5:  if(box_top > high[bar]) {if(box_bottom >= low[bar]) box_bottom=low[bar];} else box_top=high[bar]; state=0; break;
        }

      Up1Buffer[bar]=box_top;
      Dn1Buffer[bar]=box_bottom;
      Up2Buffer[bar]=box_top;
      Dn2Buffer[bar]=box_bottom;
      state++;
      
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(bar==1)
        {
         STATE=state;
         BOX_TOP=box_top;
         BOX_BUTTOM=box_bottom;
        }
     }

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) limit-=int(Shift);     
//---- �������� ���� ��������� ����� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=2;
      UpIndBuffer[bar]=0.0;
      DnIndBuffer[bar]=0.0;
      
      if(close[bar]>Up1Buffer[bar+Shift])
        {
         if(open[bar]<close[bar]) clr=4;
         else clr=3;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      if(close[bar]<Dn1Buffer[bar+Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
