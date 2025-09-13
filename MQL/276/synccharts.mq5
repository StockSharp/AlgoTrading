//+------------------------------------------------------------------+
//| Include                                                          |
//+------------------------------------------------------------------+
#include <Expert\Expert.mqh>
#include <ChartObjects\ChartObjectsTxtControls.mqh>

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
//--- inputs for expert
input string Inp_Expert_Title              ="SyncCharts";
int          Expert_MagicNumber            =1234;
bool         Expert_EveryTick              =false;

//+------------------------------------------------------------------+
//| Global expert object                                             |
//+------------------------------------------------------------------+
CExpert ExtExpert;
CChartObjectLabel g_label;
bool g_main;

//+------------------------------------------------------------------+
//| Initialization function of the expert                            |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Initializing expert
   if(!ExtExpert.Init(Symbol(),Period(),Expert_EveryTick,Expert_MagicNumber))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing expert");
      ExtExpert.Deinit();
      return(-1);
     }

   int   sy=10;
   int   dy=16;
   color color_label;
   color color_info;

//--- tuning colors
   color_info =(color)(ChartGetInteger(0,CHART_COLOR_BACKGROUND)^0xFFFFFF);
   color_label=(color)(color_info^0x202020);
//---
   if(ChartGetInteger(0,CHART_SHOW_OHLC))
   {
      sy+=16;
   }

//--- creation Labels[]
   g_label.Create(0, "Label" + IntegerToString(0) , 0, 20, sy + dy);
   g_label.Color(color_label);
   g_label.FontSize(8);
   //---

   if(ChartFirst() == ChartID())
   {
      g_label.Description("SyncCharts - Main");
      g_main = true;
   }
   else
   {
      g_label.Description("Synced");
      g_main = false;
   }

   ChartSetInteger(0, CHART_AUTOSCROLL, 0);

   EventSetTimer(1);   

//--- ok
   return(0);
  }
//+------------------------------------------------------------------+
//| Deinitialization function of the expert                          |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   EventKillTimer();
   ExtExpert.Deinit();
  }
//+------------------------------------------------------------------+
//| Timer
//+------------------------------------------------------------------+
void OnTimer()
{
long lFirstChartID, lMyChartID, lFirstMode, lMyMode, lFirstShift, lMyShift, lFirstScale, lMyScale, lFirstBorder, lMyBorder;
ENUM_TIMEFRAMES etFirstPeriod, etMyPeriod;
bool bRedraw;

   bRedraw = false;

   if(g_main == false)
   {
      ChartSetInteger(0, CHART_AUTOSCROLL, 0);
   
      lFirstChartID = ChartFirst();
      lMyChartID = ChartID();

      lFirstMode = ChartGetInteger(lFirstChartID, CHART_MODE);
      lMyMode = ChartGetInteger(lMyChartID, CHART_MODE);                  

      etFirstPeriod = ChartPeriod(lFirstChartID);
      etMyPeriod = ChartPeriod(lMyChartID);         

      lFirstShift = ChartGetInteger(lFirstChartID, CHART_FIRST_VISIBLE_BAR);
      lMyShift = ChartGetInteger(lMyChartID, CHART_FIRST_VISIBLE_BAR);
         
      lFirstScale = ChartGetInteger(lFirstChartID, CHART_SCALE);         
      lMyScale = ChartGetInteger(lMyChartID, CHART_SCALE);

      lFirstBorder = ChartGetInteger(lFirstChartID, CHART_SHIFT);         
      lMyBorder = ChartGetInteger(lMyChartID, CHART_SHIFT);
               
      if(lFirstMode != lMyMode)
      {
         ChartSetInteger(lMyChartID, CHART_MODE, lFirstMode);
         bRedraw = true;
      }

      if(etFirstPeriod != etMyPeriod)
      {
         Print("Change period..." + IntegerToString(etMyPeriod) + " -> " + IntegerToString(etFirstPeriod));
         ChartSetSymbolPeriod(lMyChartID, _Symbol, etFirstPeriod);
         bRedraw = true;
      }
      else
      if(lFirstBorder != lMyBorder)
      {
         ChartSetInteger(lMyChartID, CHART_SHIFT, lFirstBorder);         
         bRedraw = true;
      }
      else
      if(lFirstScale != lMyScale)
      {
         ChartSetInteger(lMyChartID, CHART_SCALE, lFirstScale);         
         bRedraw = true;
      }
      else
      if(lFirstShift != lMyShift)
      {
         Print("Shift..." + IntegerToString(lMyShift) + " -> " + IntegerToString(lFirstShift));
         ChartNavigate(lMyChartID, CHART_CURRENT_POS, int(lFirstShift - lMyShift));
         bRedraw = true;
      }

      if(bRedraw == true)
      {
         ChartRedraw();      
      }
      else
      {
         Print("Idle...");
      }
   }
   
   ExtExpert.OnTimer();
}
