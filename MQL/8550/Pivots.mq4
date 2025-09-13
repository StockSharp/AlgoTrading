//+------------------------------------------------------------------+
//|                                        FerruFx_Multi_info+_2.mq4 |
//|                                        Copyright © 2007, FerruFx |
//|                                                                  |
//+------------------------------------------------------------------+


#property indicator_chart_window

#property indicator_buffers 7
#property indicator_color1 MediumSpringGreen
#property indicator_color2 FireBrick
#property indicator_color3 MediumBlue
#property indicator_color4 DarkOrange
#property indicator_color5 MediumBlue
#property indicator_color6 FireBrick
#property indicator_color7 MediumSpringGreen

extern double TrendStrongLevel = 75.00;
extern int Pivots_Period = 1440;

//---- buffers
double Piv_Buffer[];
double R1_Buffer[];
double R2_Buffer[];
double R3_Buffer[];
double S1_Buffer[];
double S2_Buffer[];
double S3_Buffer[];

// indicators parameters

extern int Ydist_line=20;
extern int Xdist_line=10;


int TimeZone=0;
bool pivots = true;
bool alert = true;

double yesterday_high=0;
double yesterday_open=0;
double yesterday_low=0;
double yesterday_close=0;
double today_open=0;
double today_high=0;
double today_low=0;

double rates_h1[2][6];
double rates_d1[2][6];


//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
     string short_name;
//---- 2 additional buffers are used for counting.
   IndicatorBuffers(7);
   SetIndexStyle(3,DRAW_LINE);
   SetIndexBuffer(3,Piv_Buffer);
   SetIndexStyle(4,DRAW_LINE);
   SetIndexBuffer(4,R1_Buffer);
   SetIndexStyle(5,DRAW_LINE);
   SetIndexBuffer(5,R2_Buffer);
   SetIndexStyle(6,DRAW_LINE);
   SetIndexBuffer(6,R3_Buffer);
   SetIndexStyle(2,DRAW_LINE);
   SetIndexBuffer(2,S1_Buffer);
   SetIndexStyle(1,DRAW_LINE);
   SetIndexBuffer(1,S2_Buffer);
   SetIndexStyle(0,DRAW_LINE);
   SetIndexBuffer(0,S3_Buffer);
//   short_name="Pivots("+S3_Buffer+"|"+S2_Buffer+"|"+S1_Buffer+"|"+Piv_Buffer+"|"+R1_Buffer+"|"+R2_Buffer+"|"+R3_Buffer+"|"+")";
//   IndicatorShortName(short_name);
   SetIndexLabel(0,short_name);
//----
   SetIndexDrawBegin(0,S3_Buffer);
   SetIndexDrawBegin(1,S2_Buffer);
   SetIndexDrawBegin(2,S1_Buffer);
   SetIndexDrawBegin(3,Piv_Buffer);
   SetIndexDrawBegin(4,R1_Buffer);
   SetIndexDrawBegin(5,R2_Buffer);
   SetIndexDrawBegin(6,R3_Buffer);

  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
   ObjectDelete("pivots");
   ObjectDelete("line5");
   ObjectDelete("R3_Label");
   ObjectDelete("R2_Label");
   ObjectDelete("R1_Label");
   ObjectDelete("Pivot_Label");
   ObjectDelete("S1_Label");
   ObjectDelete("S2_Label");
   ObjectDelete("S3_Label");
   ObjectDelete("R3_Value");
   ObjectDelete("R2_Value");
   ObjectDelete("R1_Value");
   ObjectDelete("Pivot_Value");
   ObjectDelete("S1_Value");
   ObjectDelete("S2_Value");
   ObjectDelete("S3_Value");   
   
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {

   int    i,counted_bars=IndicatorCounted();   
   i=Bars-counted_bars-1;
   while(i>=0)
     {

   color color_common_line = White;
   color color_common_text = White;
   color color_ind = PowderBlue;
   color color_indic;
   color color_pivots_1=DarkOrange;
   double x;
   
   
   
// DAILY PIVOTS AND RANGE

   string Pivots_Label;
   Pivots_Period=Period();
   switch (Pivots_Period)
     {
       case 1     : Pivots_Label="+1 мин Pivots+";break;
       case 5     : Pivots_Label="+5 мин Pivots+";break;
       case 15    : Pivots_Label="+15мин Pivots+";break;
       case 30    : Pivots_Label="+30мин Pivots+";break;
       case 60    : Pivots_Label="+1 час Pivots+";break;
       case 240   : Pivots_Label="+4 час Pivots+";break;
       case 1440  : Pivots_Label="+Дневн Pivots+";break;
       case 10080 : Pivots_Label="+Недел Pivots+";break;
       case 43200 : Pivots_Label="+Месяц Pivots+";break;
     }
   
   
   ObjectCreate("pivots", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("pivots",Pivots_Label,9, "Verdana", color_pivots_1);
   ObjectSet("pivots", OBJPROP_CORNER, 0);
   ObjectSet("pivots", OBJPROP_XDISTANCE, Xdist_line);
   ObjectSet("pivots", OBJPROP_YDISTANCE, Ydist_line+5);
   
   ObjectCreate("line5", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("line5","------------------",8, "Verdana", color_pivots_1);
   ObjectSet("line5", OBJPROP_CORNER, 0);
   ObjectSet("line5", OBJPROP_XDISTANCE, Xdist_line);
   ObjectSet("line5", OBJPROP_YDISTANCE, Ydist_line+16);
   
//---- Get new daily prices

//   ArrayCopyRates(rates_d1, Symbol(), PERIOD_D1);
   ArrayCopyRates(rates_d1, Symbol(), 0);
   
//---- modifs ibfx
   int offset = i;
 //  if(DayOfWeek()==1) offset=1;
//----

   double day50_high = rates_d1[50+offset][3];
   double day50_low = rates_d1[50+offset][2]; 
   double day49_high = rates_d1[49+offset][3];
   double day49_low = rates_d1[49+offset][2]; 
   double day48_high = rates_d1[48+offset][3];
   double day48_low = rates_d1[48+offset][2]; 
   double day47_high = rates_d1[47+offset][3];
   double day47_low = rates_d1[47+offset][2]; 
   double day46_high = rates_d1[46+offset][3];
   double day46_low = rates_d1[46+offset][2]; 
   double day45_high = rates_d1[45+offset][3];
   double day45_low = rates_d1[45+offset][2]; 
   double day44_high = rates_d1[44+offset][3];
   double day44_low = rates_d1[44+offset][2]; 
   double day43_high = rates_d1[43+offset][3];
   double day43_low = rates_d1[43+offset][2]; 
   double day42_high = rates_d1[42+offset][3];
   double day42_low = rates_d1[42+offset][2]; 
   double day41_high = rates_d1[41+offset][3];
   double day41_low = rates_d1[41+offset][2]; 
   double day40_high = rates_d1[40+offset][3];
   double day40_low = rates_d1[40+offset][2]; 
   double day39_high = rates_d1[39+offset][3];
   double day39_low = rates_d1[39+offset][2]; 
   double day38_high = rates_d1[38+offset][3];
   double day38_low = rates_d1[38+offset][2]; 
   double day37_high = rates_d1[37+offset][3];
   double day37_low = rates_d1[37+offset][2]; 
   double day36_high = rates_d1[36+offset][3];
   double day36_low = rates_d1[36+offset][2]; 
   double day35_high = rates_d1[35+offset][3];
   double day35_low = rates_d1[35+offset][2]; 
   double day34_high = rates_d1[34+offset][3];
   double day34_low = rates_d1[34+offset][2]; 
   double day33_high = rates_d1[33+offset][3];
   double day33_low = rates_d1[33+offset][2]; 
   double day32_high = rates_d1[32+offset][3];
   double day32_low = rates_d1[32+offset][2]; 
   double day31_high = rates_d1[31+offset][3];
   double day31_low = rates_d1[31+offset][2]; 
   double day30_high = rates_d1[30+offset][3];
   double day30_low = rates_d1[30+offset][2]; 
   double day29_high = rates_d1[29+offset][3];
   double day29_low = rates_d1[29+offset][2]; 
   double day28_high = rates_d1[28+offset][3];
   double day28_low = rates_d1[28+offset][2]; 
   double day27_high = rates_d1[27+offset][3];
   double day27_low = rates_d1[27+offset][2]; 
   double day26_high = rates_d1[26+offset][3];
   double day26_low = rates_d1[26+offset][2]; 
   double day25_high = rates_d1[25+offset][3];
   double day25_low = rates_d1[25+offset][2]; 
   double day24_high = rates_d1[24+offset][3];
   double day24_low = rates_d1[24+offset][2]; 
   double day23_high = rates_d1[23+offset][3];
   double day23_low = rates_d1[23+offset][2]; 
   double day22_high = rates_d1[22+offset][3];
   double day22_low = rates_d1[22+offset][2]; 
   double day21_high = rates_d1[21+offset][3];
   double day21_low = rates_d1[21+offset][2]; 
   double day20_high = rates_d1[20+offset][3];
   double day20_low = rates_d1[20+offset][2]; 
   double day19_high = rates_d1[19+offset][3];
   double day19_low = rates_d1[19+offset][2]; 
   double day18_high = rates_d1[18+offset][3];
   double day18_low = rates_d1[18+offset][2]; 
   double day17_high = rates_d1[17+offset][3];
   double day17_low = rates_d1[17+offset][2]; 
   double day16_high = rates_d1[16+offset][3];
   double day16_low = rates_d1[16+offset][2]; 
   double day15_high = rates_d1[15+offset][3];
   double day15_low = rates_d1[15+offset][2]; 
   double day14_high = rates_d1[14+offset][3];
   double day14_low = rates_d1[14+offset][2]; 
   double day13_high = rates_d1[13+offset][3];
   double day13_low = rates_d1[13+offset][2]; 
   double day12_high = rates_d1[12+offset][3];
   double day12_low = rates_d1[12+offset][2]; 
   double day11_high = rates_d1[11+offset][3];
   double day11_low = rates_d1[11+offset][2]; 
   double day10_high = rates_d1[10+offset][3];
   double day10_low = rates_d1[10+offset][2]; 
   double day9_high = rates_d1[9+offset][3];
   double day9_low = rates_d1[9+offset][2];
   double day8_high = rates_d1[8+offset][3];
   double day8_low = rates_d1[8+offset][2]; 
   double day7_high = rates_d1[7+offset][3];
   double day7_low = rates_d1[7+offset][2]; 
   double day6_high = rates_d1[6+offset][3];
   double day6_low = rates_d1[6+offset][2]; 
   double day5_high = rates_d1[5+offset][3];
   double day5_low = rates_d1[5+offset][2]; 
   double day4_high = rates_d1[4+offset][3];
   double day4_low = rates_d1[4+offset][2]; 
   double day3_high = rates_d1[3+offset][3];
   double day3_low = rates_d1[3+offset][2]; 
   double day2_high = rates_d1[2+offset][3];
   double day2_low = rates_d1[2+offset][2]; 
   double yesterday_high = rates_d1[1+offset][3];
   double yesterday_low = rates_d1[1+offset][2];
   double yesterday_close = rates_d1[1+offset][4];
   double day_high = rates_d1[0][3];
   double day_low = rates_d1[0][2];
   
/*
   int i=0;

   ArrayCopyRates(rates_h1, Symbol(), PERIOD_H1);
   for (i=0;i<=25;i++)
   {
    if (TimeMinute(rates_h1[i][0])==0 && (TimeHour(rates_h1[i][0])-TimeZone)==0)
    {
     yesterday_close = rates_h1[i+1][4];      
     yesterday_open = rates_h1[i+24][1];
     today_open = rates_h1[i][1];      
     break;
    }
   }
*/

//---- Calculate Pivots et range

   double D = (day_high - day_low);
   double Q = (yesterday_high - yesterday_low);
   double Q2 = (day2_high - day2_low);
   double Q3 = (day3_high - day3_low);
   double Q4 = (day4_high - day4_low);
   double Q5 = (day5_high - day5_low);
   double Q6 = (day6_high - day6_low);
   double Q7 = (day7_high - day7_low);
   double Q8 = (day8_high - day8_low);
   double Q9 = (day9_high - day9_low);
   double Q10 = (day10_high - day10_low);
   double Q11 = (day11_high - day11_low);
   double Q12 = (day12_high - day12_low);
   double Q13 = (day13_high - day13_low);
   double Q14 = (day14_high - day14_low);
   double Q15 = (day15_high - day15_low);
   double Q16 = (day16_high - day16_low);
   double Q17 = (day17_high - day17_low);
   double Q18 = (day18_high - day18_low);
   double Q19 = (day19_high - day19_low);
   double Q20 = (day20_high - day20_low);
   double Q21 = (day21_high - day21_low);
   double Q22 = (day22_high - day22_low);
   double Q23 = (day23_high - day23_low);
   double Q24 = (day24_high - day24_low);
   double Q25 = (day25_high - day25_low);
   double Q26 = (day26_high - day26_low);
   double Q27 = (day27_high - day27_low);
   double Q28 = (day28_high - day28_low);
   double Q29 = (day29_high - day29_low);
   double Q30 = (day30_high - day30_low);
   double Q31 = (day31_high - day31_low);
   double Q32 = (day32_high - day32_low);
   double Q33 = (day33_high - day33_low);
   double Q34 = (day34_high - day34_low);
   double Q35 = (day35_high - day35_low);
   double Q36 = (day36_high - day36_low);
   double Q37 = (day37_high - day37_low);
   double Q38 = (day38_high - day38_low);
   double Q39 = (day39_high - day39_low);
   double Q40 = (day40_high - day40_low);
   double Q41 = (day41_high - day41_low);
   double Q42 = (day42_high - day42_low);
   double Q43 = (day43_high - day43_low);
   double Q44 = (day44_high - day44_low);
   double Q45 = (day45_high - day45_low);
   double Q46 = (day46_high - day46_low);
   double Q47 = (day47_high - day47_low);
   double Q48 = (day48_high - day48_low);
   double Q49 = (day49_high - day49_low);
   double Q50 = (day50_high - day50_low);
   double P = (yesterday_high + yesterday_low + yesterday_close) / 3;
   double R1 = (2*P)-yesterday_low;
   double S1 = (2*P)-yesterday_high;
   double R2 = P+(R1 - S1);
   double S2 = P-(R1 - S1);
	double R3 = (2*P)+(yesterday_high-(2*yesterday_low));
	double S3 = (2*P)-((2* yesterday_high)-yesterday_low);
	
	
	
	int Precision, dig;
{
   if( StringFind( Symbol(), "JPY", 0) != -1 ) { Precision = 100; dig = 2;}
   else                                        { Precision = 10000; dig = 4; }
} 	
	
//---- Set Pivots labels
   
     
   

   
   ObjectCreate("R3_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("R3_Label","R3",9, "Verdana", Gainsboro);
   ObjectSet("R3_Label", OBJPROP_CORNER, 0);
   ObjectSet("R3_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("R3_Label", OBJPROP_YDISTANCE, Ydist_line+30);
   
   ObjectCreate("R3_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("R3_Value"," "+DoubleToStr(R3,dig),9, "Verdana", Gainsboro);
   ObjectSet("R3_Value", OBJPROP_CORNER, 0);
   ObjectSet("R3_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("R3_Value", OBJPROP_YDISTANCE, Ydist_line+30);
   
   ObjectCreate("R2_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("R2_Label","R2",9, "Verdana", Silver);
   ObjectSet("R2_Label", OBJPROP_CORNER, 0);
   ObjectSet("R2_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("R2_Label", OBJPROP_YDISTANCE, Ydist_line+45);
   
   ObjectCreate("R2_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("R2_Value"," "+DoubleToStr(R2,dig),9, "Verdana", Silver);
   ObjectSet("R2_Value", OBJPROP_CORNER, 0);
   ObjectSet("R2_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("R2_Value", OBJPROP_YDISTANCE, Ydist_line+45);
   
   ObjectCreate("R1_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("R1_Label","R1",9, "Verdana", DarkGray);
   ObjectSet("R1_Label", OBJPROP_CORNER, 0);
   ObjectSet("R1_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("R1_Label", OBJPROP_YDISTANCE, Ydist_line+60);
   
   ObjectCreate("R1_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("R1_Value"," "+DoubleToStr(R1,dig),9, "Verdana", DarkGray);
   ObjectSet("R1_Value", OBJPROP_CORNER, 0);
   ObjectSet("R1_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("R1_Value", OBJPROP_YDISTANCE, Ydist_line+60);
   
   ObjectCreate("Pivot_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("Pivot_Label","Pivot",9, "Verdana", Gray);
   ObjectSet("Pivot_Label", OBJPROP_CORNER, 0);
   ObjectSet("Pivot_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("Pivot_Label", OBJPROP_YDISTANCE, Ydist_line+75);
   
   ObjectCreate("Pivot_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("Pivot_Value"," "+DoubleToStr(P,dig),9, "Verdana", Gray);
   ObjectSet("Pivot_Value", OBJPROP_CORNER, 0);
   ObjectSet("Pivot_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("Pivot_Value", OBJPROP_YDISTANCE, Ydist_line+75);
   
   ObjectCreate("S1_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("S1_Label","S1",9, "Verdana", DarkGray);
   ObjectSet("S1_Label", OBJPROP_CORNER, 0);
   ObjectSet("S1_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("S1_Label", OBJPROP_YDISTANCE, Ydist_line+90);
   
   ObjectCreate("S1_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("S1_Value"," "+DoubleToStr(S1,dig),9, "Verdana", DarkGray);
   ObjectSet("S1_Value", OBJPROP_CORNER, 0);
   ObjectSet("S1_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("S1_Value", OBJPROP_YDISTANCE, Ydist_line+90);
   
   ObjectCreate("S2_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("S2_Label","S2",9, "Verdana", Silver);
   ObjectSet("S2_Label", OBJPROP_CORNER, 0);
   ObjectSet("S2_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("S2_Label", OBJPROP_YDISTANCE, Ydist_line+105);
   
   ObjectCreate("S2_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("S2_Value"," "+DoubleToStr(S2,dig),9, "Verdana", Silver);
   ObjectSet("S2_Value", OBJPROP_CORNER, 0);
   ObjectSet("S2_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("S2_Value", OBJPROP_YDISTANCE, Ydist_line+105);
   
   ObjectCreate("S3_Label", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("S3_Label","S3",9, "Verdana", Gainsboro);
   ObjectSet("S3_Label", OBJPROP_CORNER, 0);
   ObjectSet("S3_Label", OBJPROP_XDISTANCE, Xdist_line+3);
   ObjectSet("S3_Label", OBJPROP_YDISTANCE, Ydist_line+120);
   
   ObjectCreate("S3_Value", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("S3_Value"," "+DoubleToStr(S3,dig),9, "Verdana", Gainsboro);
   ObjectSet("S3_Value", OBJPROP_CORNER, 0);
   ObjectSet("S3_Value", OBJPROP_XDISTANCE, Xdist_line+40);
   ObjectSet("S3_Value", OBJPROP_YDISTANCE, Ydist_line+120);
   
   S3_Buffer[i] =S3;
   S2_Buffer[i] =S2;
   S1_Buffer[i] =S1;
   Piv_Buffer[i]=P;
   R1_Buffer[i] =R1;
   R2_Buffer[i] =R2;
   R3_Buffer[i] =R3;     
     
      i--;
     }     

   return(0);
  }