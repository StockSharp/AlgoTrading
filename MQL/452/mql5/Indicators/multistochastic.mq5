//+------------------------------------------------------------------+
//|                                              MultiStochastic.mq5 |
//|                               Copyright © 2010, Nikolay Kositsin | 
//|                                Khabarovsk, farria@mail.redcom.ru | 
//+------------------------------------------------------------------+  
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version
#property version   "1.00"
//---- drawing the indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers
#property indicator_buffers 15 
//---- 15 plots are used in total
#property indicator_plots   15
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator 1 as a line
#property indicator_type1 DRAW_LINE
//---- lime color is used as the color of the indicator line
#property indicator_color1 Lime
//---- the indicator line is a continuous curve
#property indicator_style1 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator line label
#property indicator_label1  "MultiStochastic"
//+-----------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2 DRAW_LINE
//---- red color is used for the indicator line
#property indicator_color2 Red
//---- the indicator line is a continuous curve
#property indicator_style2  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width2  1
//---- displaying the indicator line label
#property indicator_label2  "MultiStochastic"
//+-----------------------------------+
//---- drawing the indicator 3 as a line
#property indicator_type3 DRAW_LINE
//---- lime color is used as the color of the indicator line
#property indicator_color3 Lime
//---- the indicator line is a continuous curve
#property indicator_style3 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width3  1
//---- displaying the indicator line label
#property indicator_label3  "MultiStochastic"
//+-----------------------------------+
//---- drawing the indicator 4 as a line
#property indicator_type4 DRAW_LINE
//---- red color is used for the indicator line
#property indicator_color4 Red
//---- the indicator line is a continuous curve
#property indicator_style4  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width4  1
//---- displaying the indicator line label
#property indicator_label4  "MultiStochastic"
//+-----------------------------------+
//---- drawing the indicator 5 as a line
#property indicator_type5 DRAW_LINE
//---- lime color is used as the color of the indicator line
#property indicator_color5 Lime
//---- the indicator line is a continuous curve
#property indicator_style5 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width5  1
//---- displaying the indicator line label
#property indicator_label5  "MultiStochastic"
//+-----------------------------------+
//---- drawing the indicator 6 as a line
#property indicator_type6 DRAW_LINE
//---- red color is used for the indicator line
#property indicator_color6 Red
//---- the indicator line is a continuous curve
#property indicator_style6  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width6  1
//---- displaying the indicator line label
#property indicator_label6  "MultiStochastic"
//+-----------------------------------+
//---- drawing the indicator 7 as a line
#property indicator_type7 DRAW_LINE
//---- use blue violet color for the indicator line
#property indicator_color7 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style7 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width7  1
//---- displaying the indicator line label
#property indicator_label7  "Level"
//+-----------------------------------+ 
//---- drawing the indicator 8 as a line
#property indicator_type8 DRAW_LINE
//---- use blue violet color for the indicator line
#property indicator_color8 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style8 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width8  1
//---- displaying the indicator line label
#property indicator_label8  "Level"
//+-----------------------------------+
//---- drawing the indicator 9 as a line
#property indicator_type9 DRAW_LINE
//---- use blue violet color for the indicator line
#property indicator_color9 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style9 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width9  1
//---- displaying the indicator line label
#property indicator_label9  "Level"
//+-----------------------------------+
//---- drawing the indicator 10 as a line
#property indicator_type10 DRAW_LINE
//---- use blue violet color for the indicator line
#property indicator_color10 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style10 STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width10  1
//---- displaying the indicator line label
#property indicator_label10  "Level"
//+-----------------------------------+
//---- Trend line background highlighting drawing parameters
#property indicator_type11 DRAW_HISTOGRAM
#property indicator_color11 Gray
//---- indicator line width is equal to 1
#property indicator_width11 1
//+-----------------------------------+
//---- Trend line background highlighting drawing parameters
#property indicator_type12 DRAW_HISTOGRAM
#property indicator_color12 Red
//---- indicator line width is equal to 1
#property indicator_width12 1
//+-----------------------------------+
//---- Trend line background highlighting drawing parameters
#property indicator_type13 DRAW_HISTOGRAM
#property indicator_color13 Lime
//---- indicator line width is equal to 1
#property indicator_width13 1
//+-----------------------------------+
//---- Trend line background highlighting drawing parameters
#property indicator_type14 DRAW_ARROW
#property indicator_color14 Aqua
//---- indicator line width is equal to 2
#property indicator_width14 5
//+-----------------------------------+
//---- Trend line background highlighting drawing parameters
#property indicator_type15 DRAW_ARROW
#property indicator_color15 Magenta
//---- indicator line width is equal to 2
#property indicator_width15 5
//+-----------------------------------+  
//---- values of the indicator horizontal levels
#property indicator_level1 280
#property indicator_level2 250
#property indicator_level3 220
#property indicator_level4 180
#property indicator_level5 150
#property indicator_level6 120
#property indicator_level7 80
#property indicator_level8 50
#property indicator_level9 20
//---- plotting style of indicator's horizontal levels
#property indicator_levelcolor Purple
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//|  Indicator input parameters       |
//+-----------------------------------+
input int Kperiod = 5;                        // K-period (number of bars for calculations)
input int Dperiod = 3;                        // D-period (primary smoothing period)
input int slowing = 3;                        // Final smoothing
input ENUM_MA_METHOD ma_method=MODE_SMA;      // Smoothing type
input ENUM_STO_PRICE price_field=STO_LOWHIGH; // Stochastic calculation method
input string SymbolA = "EURJPY";
input string SymbolB = "EURJPY";
input string SymbolC = "USDJPY";
//+-----------------------------------+
//---- indicator buffers
double Sto0_Buffer[],Sto1_Buffer[],Sto2_Buffer[];
double Sig0_Buffer[],Sig1_Buffer[],Sig2_Buffer[];
double Lev1_Buffer[],Lev2_Buffer[];
double Lev3_Buffer[],Lev4_Buffer[];
double Fl_Buffer[],Dn_Buffer[],Up_Buffer[];
double UpEnd_Buffer[],DnEnd_Buffer[];
//---- integer variables 
int MinBars,CrossA,CrossB,CrossC;
//+------------------------------------------------------------------+
//| MultiStochastic indicator initialization function                | 
//+------------------------------------------------------------------+   
void OnInit()
  {
//---- obtaining handles of used technical indicators
   CrossA = iStochastic(SymbolA, 0, Kperiod, Dperiod, slowing, ma_method, price_field);
   CrossB = iStochastic(SymbolB, 0, Kperiod, Dperiod, slowing, ma_method, price_field);
   CrossC = iStochastic(SymbolC, 0, Kperiod, Dperiod, slowing, ma_method, price_field);

//---- set dynamic arrays as indicator buffers
   InitIndexBuffer1(0,Sto0_Buffer,"Stochastic "+SymbolA);
   InitIndexBuffer1(1,Sig0_Buffer,"Signal Stochastic "+SymbolA);
   InitIndexBuffer1(2,Sto1_Buffer,"Stochastic "+SymbolB);
   InitIndexBuffer1(3,Sig1_Buffer,"Signal Stochastic "+SymbolB);
   InitIndexBuffer1(4,Sto2_Buffer,"Stochastic "+SymbolC);
   InitIndexBuffer1(5,Sig2_Buffer,"Signal Stochastic "+SymbolC);
//---- set dynamic arrays as indicator buffers
   InitIndexBuffer2(6,Lev1_Buffer,"separator 0",100);
   InitIndexBuffer2(7,Lev2_Buffer,"separator 100",100);
   InitIndexBuffer2(8,Lev3_Buffer,"separator 200",100);
   InitIndexBuffer2(9,Lev4_Buffer,"separator 300",100);
//---- set dynamic arrays as indicator buffers
   InitIndexBuffer1(10,Fl_Buffer,"Flat");
   InitIndexBuffer1(11,Dn_Buffer,"DownTrend");
   InitIndexBuffer1(12,Up_Buffer,"UpTrend");
   InitIndexBuffer1(13,DnEnd_Buffer,"StopDown");
   InitIndexBuffer1(14,UpEnd_Buffer,"StopUp");
   PlotIndexSetInteger(13,PLOT_ARROW,159);
   PlotIndexSetInteger(14,PLOT_ARROW,159);

//---- initializations of a variable for the indicator short name
   string shortname;
   StringConcatenate
   (shortname,"MultiStochastic( Kperiod = ",Kperiod,
    ", Dperiod = ",Dperiod,", slowing = ",slowing,")");
//---- creating a name for displaying in a separate sub-window and in a tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);

//---- initialization of constants
   MinBars=Kperiod+Dperiod+slowing;

//---- indexing elements in indicator buffers as timeseries
   ArraySetAsSeries(Sto0_Buffer,true);
   ArraySetAsSeries(Sto1_Buffer,true);
   ArraySetAsSeries(Sto2_Buffer,true);
   ArraySetAsSeries(Sig0_Buffer,true);
   ArraySetAsSeries(Sig1_Buffer,true);
   ArraySetAsSeries(Sig2_Buffer,true);
   ArraySetAsSeries(Lev1_Buffer,true);
   ArraySetAsSeries(Lev2_Buffer,true);
   ArraySetAsSeries(Lev3_Buffer,true);
   ArraySetAsSeries(Lev4_Buffer,true);
   ArraySetAsSeries(Fl_Buffer,true);
   ArraySetAsSeries(Dn_Buffer,true);
   ArraySetAsSeries(Up_Buffer,true);
   ArraySetAsSeries(DnEnd_Buffer,true);
   ArraySetAsSeries(UpEnd_Buffer,true);
//---- initialization end
  }
//+------------------------------------------------------------------+
//| InitIndexBuffer1() function                                      |
//+------------------------------------------------------------------+
void InitIndexBuffer1(int Number,double &Array[],string Label)
  {
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(Number,Array,INDICATOR_DATA);
//--- create a label to display in DataWindow
   PlotIndexSetString(Number,PLOT_LABEL,Label);
//---- 
  }
//+------------------------------------------------------------------+
//| InitIndexBuffer2() function                                      |
//+------------------------------------------------------------------+
void InitIndexBuffer2(int Number,double &Array[],string Label,int shift)
  {
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(Number,Array,INDICATOR_DATA);
//--- create a label to display in DataWindow
   PlotIndexSetString(Number,PLOT_LABEL,Label);
//---- horizontal shift of the indicator
   PlotIndexSetInteger(Number,PLOT_SHIFT,shift);
//---- 
  }
//+------------------------------------------------------------------+
//| Rates_Total() function                                           |
//+------------------------------------------------------------------+
int Rates_Total(string SymbolA_,string SymbolB_,string SymbolC_,int BarMinimum)
  {
//----
   int Bars0 = Bars(SymbolA_, 0);
   int Bars1 = Bars(SymbolB_, 0);
   int Bars2 = Bars(SymbolC_, 0);
//----
   int error=GetLastError();
   ResetLastError();
//----
   if(error==4401)return(0);

   if(BarsCalculated(CrossA)<=BarMinimum
      || BarsCalculated(CrossB)<= BarMinimum
      || BarsCalculated(CrossC)<= BarMinimum)
      return(0);
//----
   return(MathMin(Bars0,MathMin(Bars1,Bars2)));
  }
//+------------------------------------------------------------------+
//|  SynchroCheck() function                                         |
//+------------------------------------------------------------------+
bool SynchroCheck(string SymbolA_,string SymbolB_,string SymbolC_)
  {
//----
   datetime Time_[1],Vel0,Vel1,Vel2;
//----
   CopyTime(SymbolA_, 0, 0, 1, Time_); Vel0 = Time_[0];
   CopyTime(SymbolB_, 0, 0, 1, Time_); Vel1 = Time_[0];
   CopyTime(SymbolC_, 0, 0, 1, Time_); Vel2 = Time_[0];

   if(Vel0!=Vel1 || Vel1!=Vel2) return(false);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| MultiStochastic iteration function                               |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// number of bars calculated at previous call
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking the number of bars to be enough for the calculation
   int Bars_=Rates_Total(SymbolA,SymbolA,SymbolC,MinBars*5);
   if(Bars_<MinBars)return(0);

//---- check timeseries synchronization
   if(!SynchroCheck(SymbolA,SymbolA,SymbolC))
      return(prev_calculated);

//---- declaration of local variables
   int limit,limit_,bar;
   double Dif0,Dif1,Dif2;
   bool Up0,Up1,Up2,Dn0,Dn1,Dn2;

//---- calculation of the 'limit' starting index for the bars recalculation loop
//---- initialize indicator buffers
   if(prev_calculated==0)
     {
      limit=Bars_-1-MinBars;
      //----
      for(bar=0; bar<rates_total; bar++)
        {
         Lev1_Buffer[bar] = 100;
         Lev2_Buffer[bar] = 200;
         Lev3_Buffer[bar] = 0;
         Lev4_Buffer[bar] = 300;
         //----
         Sto0_Buffer[bar] = EMPTY_VALUE;
         Sto1_Buffer[bar] = EMPTY_VALUE;
         Sto2_Buffer[bar] = EMPTY_VALUE;
         Sig0_Buffer[bar] = EMPTY_VALUE;
         Sig1_Buffer[bar] = EMPTY_VALUE;
         Sig2_Buffer[bar] = EMPTY_VALUE;
        }
     }
   else limit=rates_total-prev_calculated;

//----
   limit_=limit+1;

//---- using indicators' handles, copy values of indicator's buffers 
//---- into the specially prepared dynamic arrays
   if(CopyBuffer(CrossA, 0, 0, limit_, Sto0_Buffer) < 0){Print("CopyBuffer Sto0_Buffer error =", GetLastError());}
   if(CopyBuffer(CrossA, 1, 0, limit_, Sig0_Buffer) < 0){Print("CopyBuffer Sig0_Buffer error =", GetLastError());}
//----
   if(CopyBuffer(CrossB, 0, 0, limit_, Sto1_Buffer) < 0){Print("CopyBuffer Sto1_Buffer error =", GetLastError());}
   if(CopyBuffer(CrossB, 1, 0, limit_, Sig1_Buffer) < 0){Print("CopyBuffer Sig1_Buffer error =", GetLastError());}
//----
   if(CopyBuffer(CrossC, 0, 0, limit_, Sto2_Buffer) < 0){Print("CopyBuffer Sto2_Buffer error =", GetLastError());}
   if(CopyBuffer(CrossC, 1, 0, limit_, Sig2_Buffer) < 0){Print("CopyBuffer Sig2_Buffer error =", GetLastError());}

//---- main indicator calculation loop
   for(bar=limit; bar>=0; bar--)
     {
      Sto0_Buffer[bar] += 200;
      Sig0_Buffer[bar] += 200;
      //----
      Sto1_Buffer[bar] += 100;
      Sig1_Buffer[bar] += 100;
      //----
      Lev1_Buffer[bar] = 0;
      Lev2_Buffer[bar] = 100;
      Lev3_Buffer[bar] = 200;
      Lev4_Buffer[bar] = 300;
      //----
      Fl_Buffer[bar] = 300;
      Dn_Buffer[bar] = 0;
      Up_Buffer[bar] = 0;

      //---- obtaining signals to enter the market
      Up0 = false;
      Up1 = false;
      Up2 = false;
      Dn0 = false;
      Dn1 = false;
      Dn2 = false;
      //---- 
      Dif0 = NormalizeDouble(Sto0_Buffer[bar] - Sig0_Buffer[bar], _Digits + 2);
      Dif1 = NormalizeDouble(Sto1_Buffer[bar] - Sig1_Buffer[bar], _Digits + 2);
      Dif2 = NormalizeDouble(Sto2_Buffer[bar] - Sig2_Buffer[bar], _Digits + 2);
      //----
      if(Dif0 > 0) Up0 = true;
      if(Dif1 > 0) Up1 = true;
      if(Dif2 > 0) Up2 = true;
      //----
      if(Dif0 < 0) Dn0 = true;
      if(Dif1 < 0) Dn1 = true;
      if(Dif2 < 0) Dn2 = true;
      //----
      if(Up0 && Up1 && Dn2 && MathAbs(Dif1) > MathAbs(Dif2))Up2 = true;
      if(Up0 && Up2 && Dn1 && MathAbs(Dif2) > MathAbs(Dif1))Up1 = true;
      //----
      if(Dn0 && Dn1 && Up2 && MathAbs(Dif1) > MathAbs(Dif2))Dn2 = true;
      if(Dn0 && Dn2 && Up1 && MathAbs(Dif2) > MathAbs(Dif1))Dn1 = true;
      //----
      if(Up0 && Up1 && !Dn2) Up2 = true;
      if(Up0 && Up2 && !Dn1) Up1 = true;
      //----
      if(Dn0 && Dn1 && !Up2) Dn2 = true;
      if(Dn0 && Dn2 && !Up1) Dn1 = true;
      //----
      if(Up1 && Up2 && !Dn0) Up0 = true;
      if(Dn1 && Dn2 && !Up0) Dn0 = true;
      //----
      if(Up0 && Up1 && Up2)
        {
         Up_Buffer[bar] = 300;
         Fl_Buffer[bar] = EMPTY_VALUE;
        }
      //----
      if(Dn0 && Dn1 && Dn2)
        {
         Dn_Buffer[bar] = 300;
         Fl_Buffer[bar] = EMPTY_VALUE;
        }
      //---- obtaining signals to exit the market
      DnEnd_Buffer[bar] = EMPTY_VALUE;
      UpEnd_Buffer[bar] = EMPTY_VALUE;
      //---- 
      if(Up0) DnEnd_Buffer[bar]=300;
      //---- 
      if(Dn0) UpEnd_Buffer[bar]=300;
     }
//---- end of indicator values calculations 
   return(rates_total);
  }
//+------------------------------------------------------------------+
