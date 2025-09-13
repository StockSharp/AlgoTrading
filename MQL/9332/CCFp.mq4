//+------------------------------------------------------------------+
//|                                                         CCFp.mq4 |
//|                                              SemSemFX@rambler.ru |
//|              http://onix-trade.net/forum/index.php?showtopic=107 |
//+------------------------------------------------------------------+
#property copyright "SemSemFX@rambler.ru"
#property link      "http://onix-trade.net/forum/index.php?showtopic=107"
//----
string Indicator_Name = "CCFp:";
int Objs = 0;
//----
#property indicator_separate_window
#property indicator_buffers 8
extern int MA_Method = 1;
extern int Price = 0;
extern int Fast = 5;
extern int Slow = 3;
extern bool USD = 1;
extern bool EUR = 1;
extern bool GBP = 1;
extern bool CHF = 1;
extern bool JPY = 1;
extern bool AUD = 1;
extern bool CAD = 1;
extern bool NZD = 1;
extern color Color_USD = Green;
extern color Color_EUR = DarkBlue;
extern color Color_GBP = Red;
extern color Color_CHF = Chocolate;
extern color Color_JPY = Maroon;
extern color Color_AUD = DarkOrange;
extern color Color_CAD = Purple;
extern color Color_NZD = Teal;
extern int Line_Thickness = 2;
extern int Bars.Count = 0;
//----
double arrUSD[];
double arrEUR[];
double arrGBP[];
double arrCHF[];
double arrJPY[];
double arrAUD[];
double arrCAD[];
double arrNZD[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   if(USD)
       Indicator_Name = StringConcatenate(Indicator_Name, " USD");
   if(EUR)
       Indicator_Name = StringConcatenate(Indicator_Name, " EUR");
   if(GBP)
       Indicator_Name = StringConcatenate(Indicator_Name, " GBP");
   if(CHF)
       Indicator_Name = StringConcatenate(Indicator_Name, " CHF");
   if(AUD)
       Indicator_Name = StringConcatenate(Indicator_Name, " AUD");
   if(CAD)
       Indicator_Name = StringConcatenate(Indicator_Name, " CAD");
   if(JPY)
       Indicator_Name = StringConcatenate(Indicator_Name, " JPY");
   if(NZD)
       Indicator_Name = StringConcatenate(Indicator_Name, " NZD");
   IndicatorShortName(Indicator_Name);
   int cur = 0; 
   int st = 23; 
   if(USD)
     {
       sl("~", cur, Color_USD);
       cur += st;
     }
   if(EUR)
     {
       sl("~", cur, Color_EUR);
       cur += st;
     }
   if(GBP)
     {
       sl("~", cur, Color_GBP);
       cur += st;
     }
   if(CHF)
     {
       sl("~", cur, Color_CHF);
       cur += st;
     }
   if(AUD)
     {
       sl("~", cur, Color_AUD);
       cur += st;
     }
   if(CAD)
     {
       sl("~", cur, Color_CAD);
       cur += st;
     }
   if(JPY)
     {
       sl("~", cur, Color_JPY);
       cur += st;
     }
   if(NZD)
     {
       sl("~", cur, Color_NZD);
       cur += st;
     }
   int width = 0;
   if(0 > StringFind(Symbol(), "USD", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(0, DRAW_LINE, DRAW_LINE, width, Color_USD);
   SetIndexBuffer(0, arrUSD);
   SetIndexLabel(0, "USD"); 
   if(0 > StringFind(Symbol(), "EUR", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(1, DRAW_LINE, DRAW_LINE, width, Color_EUR);
   SetIndexBuffer(1, arrEUR);
   SetIndexLabel(1, "EUR"); 
   if(0 > StringFind(Symbol(), "GBP", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(2, DRAW_LINE, DRAW_LINE, width, Color_GBP);
   SetIndexBuffer(2, arrGBP);
   SetIndexLabel(2, "GBP"); 
   if(0 > StringFind(Symbol(), "CHF", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(3, DRAW_LINE, DRAW_LINE, width, Color_CHF);
   SetIndexBuffer(3, arrCHF);
   SetIndexLabel(3, "CHF"); 
   if(0 > StringFind(Symbol(), "JPY", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(4, DRAW_LINE, DRAW_LINE, width, Color_JPY);
   SetIndexBuffer(4, arrJPY);
   SetIndexLabel(4, "JPY"); 
   if(0 > StringFind(Symbol(), "AUD", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(5, DRAW_LINE, DRAW_LINE, width, Color_AUD);
   SetIndexBuffer(5, arrAUD);
   SetIndexLabel(5, "AUD"); 
   if(0 > StringFind(Symbol(), "CAD", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(6, DRAW_LINE, DRAW_LINE, width, Color_CAD);
   SetIndexBuffer(6, arrCAD);
   SetIndexLabel(6, "CAD"); 
   if(0 > StringFind(Symbol(), "NZD", 0))
       width = 2;
   else 
       width = Line_Thickness;
   SetIndexStyle(7, DRAW_LINE, DRAW_LINE, width, Color_NZD);
   SetIndexBuffer(7, arrNZD);
   SetIndexLabel(7, "NZD"); 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   for(int i = 0; i < Objs; i++)
     {
      if(!ObjectDelete(Indicator_Name + i))
          Print("error: code #", GetLastError());
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   int limit;
   if (IndicatorCounted()<0) return(-1);
  limit=Bars-IndicatorCounted();
  if (Bars.Count>0 && limit>Bars.Count) limit=Bars.Count;
//---- основной цикл
   for(int i = 0; i < limit; i++)
     {
       // Предварительный рассчет
       if(EUR)
         {
           double EURUSD_Fast = ma("EURUSD", Fast, MA_Method, Price, i);
           double EURUSD_Slow = ma("EURUSD", Slow, MA_Method, Price, i);
           if(!EURUSD_Fast || !EURUSD_Slow)
               break;
         }
       if(GBP)
         {
           double GBPUSD_Fast = ma("GBPUSD", Fast, MA_Method, Price, i);
           double GBPUSD_Slow = ma("GBPUSD", Slow, MA_Method, Price, i);
           if(!GBPUSD_Fast || !GBPUSD_Slow)
               break;
         }
       if(AUD)
         {
           double AUDUSD_Fast = ma("AUDUSD", Fast, MA_Method, Price, i);
           double AUDUSD_Slow = ma("AUDUSD", Slow, MA_Method, Price, i);
           if(!AUDUSD_Fast || !AUDUSD_Slow)
               break;
         }
       if(NZD)
         {
           double NZDUSD_Fast = ma("NZDUSD", Fast, MA_Method, Price, i);
           double NZDUSD_Slow = ma("NZDUSD", Slow, MA_Method, Price, i);
           if(!NZDUSD_Fast || !NZDUSD_Slow)
               break;
         }
       if(CAD)
         {
           double USDCAD_Fast = ma("USDCAD", Fast, MA_Method, Price, i);
           double USDCAD_Slow = ma("USDCAD", Slow, MA_Method, Price, i);
           if(!USDCAD_Fast || !USDCAD_Slow)
               break;
         }
       if(CHF)
         {
           double USDCHF_Fast = ma("USDCHF", Fast, MA_Method, Price, i);
           double USDCHF_Slow = ma("USDCHF", Slow, MA_Method, Price, i);
           if(!USDCHF_Fast || !USDCHF_Slow)
               break;
         }
       if(JPY)
         {
           double USDJPY_Fast = ma("USDJPY", Fast, MA_Method, Price, i);
           double USDJPY_Slow = ma("USDJPY", Slow, MA_Method, Price, i);
           if(!USDJPY_Fast || !USDJPY_Slow)
               break;
         }
       // рассчет валют
       if(USD)
         {
           arrUSD[i] = 0;
           if(EUR) 
               arrUSD[i] += EURUSD_Slow / EURUSD_Fast - 1;
           if(GBP) 
               arrUSD[i] += GBPUSD_Slow / GBPUSD_Fast - 1;
           if(AUD) 
               arrUSD[i] += AUDUSD_Slow / AUDUSD_Fast - 1;
           if(NZD) 
               arrUSD[i] += NZDUSD_Slow / NZDUSD_Fast - 1;
           if(CHF) 
               arrUSD[i] += USDCHF_Fast / USDCHF_Slow - 1;
           if(CAD) 
               arrUSD[i] += USDCAD_Fast / USDCAD_Slow - 1;
           if(JPY) 
               arrUSD[i] += USDJPY_Fast / USDJPY_Slow - 1;
         }// end if USD
       if(EUR)
         {
           arrEUR[i] = 0;
           if(USD) 
               arrEUR[i] += EURUSD_Fast / EURUSD_Slow - 1;
           if(GBP) 
               arrEUR[i] += (EURUSD_Fast / GBPUSD_Fast) / 
                            (EURUSD_Slow/GBPUSD_Slow) - 1;
           if(AUD) 
               arrEUR[i] += (EURUSD_Fast / AUDUSD_Fast) / 
                            (EURUSD_Slow/AUDUSD_Slow) - 1;
           if(NZD) 
               arrEUR[i] += (EURUSD_Fast / NZDUSD_Fast) / 
                            (EURUSD_Slow/NZDUSD_Slow) - 1;
           if(CHF) 
               arrEUR[i] += (EURUSD_Fast*USDCHF_Fast) / 
                            (EURUSD_Slow*USDCHF_Slow) - 1;
           if(CAD) 
               arrEUR[i] += (EURUSD_Fast*USDCAD_Fast) / 
                            (EURUSD_Slow*USDCAD_Slow) - 1;
           if(JPY) 
               arrEUR[i] += (EURUSD_Fast*USDJPY_Fast) / 
                            (EURUSD_Slow*USDJPY_Slow) - 1;
         }// end if EUR
       if(GBP)
         {
           arrGBP[i] = 0;
           if(USD) 
               arrGBP[i] += GBPUSD_Fast / GBPUSD_Slow - 1;
           if(EUR) 
               arrGBP[i] += (EURUSD_Slow / GBPUSD_Slow) / 
                            (EURUSD_Fast / GBPUSD_Fast) - 1;
           if(AUD) 
               arrGBP[i] += (GBPUSD_Fast / AUDUSD_Fast) / 
                            (GBPUSD_Slow / AUDUSD_Slow) - 1;
           if(NZD) 
               arrGBP[i] += (GBPUSD_Fast / NZDUSD_Fast) / 
                            (GBPUSD_Slow / NZDUSD_Slow) - 1;
           if(CHF) 
               arrGBP[i] += (GBPUSD_Fast*USDCHF_Fast) / 
                            (GBPUSD_Slow*USDCHF_Slow) - 1;
           if(CAD) 
               arrGBP[i] += (GBPUSD_Fast*USDCAD_Fast) / 
                            (GBPUSD_Slow*USDCAD_Slow) - 1;
           if(JPY) 
               arrGBP[i] += (GBPUSD_Fast*USDJPY_Fast) / 
                            (GBPUSD_Slow*USDJPY_Slow) - 1;
          }// end if GBP
       if(AUD)
         {
           arrAUD[i] = 0;
           if(USD) 
               arrAUD[i] += AUDUSD_Fast / AUDUSD_Slow - 1;
           if(EUR) 
               arrAUD[i] += (EURUSD_Slow / AUDUSD_Slow) / 
                            (EURUSD_Fast / AUDUSD_Fast) - 1;
           if(GBP) 
               arrAUD[i] += (GBPUSD_Slow / AUDUSD_Slow) / 
                            (GBPUSD_Fast / AUDUSD_Fast) - 1;
           if(NZD) 
               arrAUD[i] += (AUDUSD_Fast/NZDUSD_Fast) / 
                            (AUDUSD_Slow / NZDUSD_Slow) - 1;
           if(CHF) 
               arrAUD[i] += (AUDUSD_Fast*USDCHF_Fast) / 
                            (AUDUSD_Slow*USDCHF_Slow) - 1;
           if(CAD) 
               arrAUD[i] += (AUDUSD_Fast*USDCAD_Fast) / 
                            (AUDUSD_Slow*USDCAD_Slow) - 1;
           if(JPY) 
               arrAUD[i] += (AUDUSD_Fast*USDJPY_Fast) / 
                            (AUDUSD_Slow*USDJPY_Slow) - 1;
         }// end if AUD
       if(NZD)
         {
           arrNZD[i] = 0;
           if(USD) 
               arrNZD[i] += NZDUSD_Fast / NZDUSD_Slow - 1;
           if(EUR) 
               arrNZD[i] += (EURUSD_Slow / NZDUSD_Slow) / 
                            (EURUSD_Fast/NZDUSD_Fast) - 1;
           if(GBP) 
               arrNZD[i] += (GBPUSD_Slow / NZDUSD_Slow) / 
                            (GBPUSD_Fast / NZDUSD_Fast) - 1;
           if(AUD) 
               arrNZD[i] += (AUDUSD_Slow / NZDUSD_Slow) / 
                            (AUDUSD_Fast / NZDUSD_Fast) - 1;
           if(CHF) 
               arrNZD[i] += (NZDUSD_Fast*USDCHF_Fast) / 
                            (NZDUSD_Slow*USDCHF_Slow) - 1;
           if(CAD) 
               arrNZD[i] += (NZDUSD_Fast*USDCAD_Fast) / 
                            (NZDUSD_Slow*USDCAD_Slow) - 1;
           if(JPY) 
               arrNZD[i] += (NZDUSD_Fast*USDJPY_Fast) / 
                            (NZDUSD_Slow*USDJPY_Slow) - 1;
         }// end if NZD
       if(CAD)
         {
           arrCAD[i] = 0;
           if(USD) 
               arrCAD[i] += USDCAD_Slow / USDCAD_Fast - 1;
           if(EUR) 
               arrCAD[i] += (EURUSD_Slow*USDCAD_Slow) / 
                            (EURUSD_Fast*USDCAD_Fast) - 1;
           if(GBP) 
               arrCAD[i] += (GBPUSD_Slow*USDCAD_Slow) / 
                            (GBPUSD_Fast*USDCAD_Fast) - 1;
           if(AUD) 
               arrCAD[i] += (AUDUSD_Slow*USDCAD_Slow) / 
                            (AUDUSD_Fast*USDCAD_Fast) - 1;
           if(NZD) 
               arrCAD[i] += (NZDUSD_Slow*USDCAD_Slow) / 
                            (NZDUSD_Fast*USDCAD_Fast) - 1;
           if(CHF) 
               arrCAD[i] += (USDCHF_Fast / USDCAD_Fast) / 
                            (USDCHF_Slow / USDCAD_Slow) - 1;
           if(JPY) 
               arrCAD[i] += (USDJPY_Fast / USDCAD_Fast) / 
                            (USDJPY_Slow / USDCAD_Slow) - 1;
         }// end if CAD
       if(CHF)
         {
           arrCHF[i] = 0;
           if(USD) 
               arrCHF[i] += USDCHF_Slow / USDCHF_Fast - 1;
           if(EUR) 
               arrCHF[i] += (EURUSD_Slow*USDCHF_Slow) / 
                            (EURUSD_Fast*USDCHF_Fast) - 1;
           if(GBP) 
               arrCHF[i] += (GBPUSD_Slow*USDCHF_Slow) / 
                            (GBPUSD_Fast*USDCHF_Fast) - 1;
           if(AUD) 
               arrCHF[i] += (AUDUSD_Slow*USDCHF_Slow) / 
                            (AUDUSD_Fast*USDCHF_Fast) - 1;
           if(NZD) 
               arrCHF[i] += (NZDUSD_Slow*USDCHF_Slow) / 
                            (NZDUSD_Fast*USDCHF_Fast) - 1;
           if(CAD) 
               arrCHF[i] += (USDCHF_Slow / USDCAD_Slow) / 
                            (USDCHF_Fast / USDCAD_Fast) - 1;
           if(JPY) 
               arrCHF[i] += (USDJPY_Fast / USDCHF_Fast) / 
                            (USDJPY_Slow / USDCHF_Slow) - 1;
         }// end if CHF
       if(JPY)
         {
           arrJPY[i] = 0;
           if(USD) 
               arrJPY[i] += USDJPY_Slow / USDJPY_Fast - 1;
           if(EUR) 
               arrJPY[i] += (EURUSD_Slow*USDJPY_Slow) / 
                            (EURUSD_Fast*USDJPY_Fast) - 1;
           if(GBP) 
               arrJPY[i] += (GBPUSD_Slow*USDJPY_Slow) / 
                            (GBPUSD_Fast*USDJPY_Fast) - 1;
           if(AUD) 
               arrJPY[i] += (AUDUSD_Slow*USDJPY_Slow) / 
                            (AUDUSD_Fast*USDJPY_Fast) - 1;
           if(NZD) 
               arrJPY[i] += (NZDUSD_Slow*USDJPY_Slow) / 
                            (NZDUSD_Fast*USDJPY_Fast) - 1;
           if(CAD) 
               arrJPY[i] += (USDJPY_Slow/USDCAD_Slow) / 
                            (USDJPY_Fast/USDCAD_Fast) - 1;
           if(CHF) 
               arrJPY[i] += (USDJPY_Slow/USDCHF_Slow) / 
                            (USDJPY_Fast/USDCHF_Fast) - 1;
         }// end if JPY
     }//end block for(int i=0; i<limit; i++)
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|  Subroutines                                                     |
//+------------------------------------------------------------------+
double ma(string sym, int per, int Mode, int Price, int i)
  {
   double res = 0;
   int k = 1;
   int ma_shift = 0;
   int tf = 0;
   switch(Period())
     {
       case 1:     res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 5;
       case 5:     res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 3;
       case 15:    res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 2;
       case 30:    res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 2;
       case 60:    res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 4;
       case 240:   res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 6;
       case 1440:  res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k += 4;
       case 10080: res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
                   k +=4;
       case 43200: res += iMA(sym, tf, per*k, ma_shift, Mode, Price, i); 
     } 
   return(res);
  }   
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void sl(string sym, int y, color col)
  {
   int window = WindowFind(Indicator_Name);
   string ID = Indicator_Name+Objs;
   int tmp = 10 + y;
   Objs++;
   if(ObjectCreate(ID, OBJ_LABEL, window, 0, 0))
     {
       //ObjectSet(ID, OBJPROP_CORNER, 1);
       ObjectSet(ID, OBJPROP_XDISTANCE, y + 35);
       ObjectSet(ID, OBJPROP_YDISTANCE, 0);
       ObjectSetText(ID, sym, 18, "Arial Black", col);
     }
  }   
//+------------------------------------------------------------------+

