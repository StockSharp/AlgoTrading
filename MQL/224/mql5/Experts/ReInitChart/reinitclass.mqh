//+------------------------------------------------------------------+
//|                                                  ReinitClass.mqh |
//|                             Copyright (c) 2009, Vladimir Gomonov |
//|                                            MetaDriver@rambler.ru |
//+------------------------------------------------------------------+
#property copyright "(c) 2009, Vladimir Gomonov"
#property link      "MetaDriver@rambler.ru"
//+------------------------------------------------------------------+
//| cChartReInit class                                               |
//+------------------------------------------------------------------+
class cChartReInit
  {
private:
   string            pButtonName;
   string            pText;
   color             pTextColor;
   color             pBackColor;
   void              CreateReinitButton(long wh);
public:
                     cChartReInit() {}
                    ~cChartReInit() { Deinit(); }
   void              Init(string,string,color,color);
   void              Deinit();
   void              Run();
  };
//+------------------------------------------------------------------+
//| Init                                                             |
//+------------------------------------------------------------------+
void cChartReInit::Init(string BName,string Text,color TextColor,color BackColor)
  {
   pButtonName=BName;
   pText=Text;
   pTextColor=TextColor;
   pBackColor=BackColor;
   EventSetTimer(1);
  }
//+------------------------------------------------------------------+
//| Deinit                                                           |
//+------------------------------------------------------------------+
void cChartReInit::Deinit()
  {
   EventKillTimer();
   for(long i=ChartNext(0);i>0;i=ChartNext(i)) ObjectDelete(i,pButtonName);
  }
//+------------------------------------------------------------------+
//| Scanning all charts and reinit if button is pushed               |
//+------------------------------------------------------------------+
void cChartReInit::Run()
  {
   for(long i=ChartNext(0);i>0;i=ChartNext(i))
     {
      long wc = ChartGetInteger(i,CHART_WINDOWS_TOTAL);
      long wi = ObjectFind(i, pButtonName);
      if(--wc!=wi) { CreateReinitButton(i); ChartRedraw(i); continue;}
      if(!ChartGetInteger(i,CHART_WINDOW_IS_VISIBLE)) continue;
      if(ObjectGetInteger(i,pButtonName,OBJPROP_STATE))
        {
         ObjectSetInteger(i,pButtonName,OBJPROP_STATE,false);
         ENUM_TIMEFRAMES cp=ChartPeriod(i);
         ChartSetSymbolPeriod(i,ChartSymbol(i),(cp==PERIOD_M1 ? PERIOD_M5 : PERIOD_M1));
         ChartSetSymbolPeriod(i,ChartSymbol(i),cp);
        }
      ChartRedraw(i);
     } // for(Charts)
  }
//+------------------------------------------------------------------+
//| CreateReinitButton                                               |
//+------------------------------------------------------------------+
void cChartReInit::CreateReinitButton(long wh)
  {
   ObjectDelete(wh,pButtonName); // delete if exist, but not relevant subwindow 
   long wc=ChartGetInteger(wh,CHART_WINDOWS_TOTAL);
// Creation and design button
   ObjectCreate(wh,pButtonName,OBJ_BUTTON,int(--wc),0,0);
   ObjectSetInteger(wh,pButtonName,OBJPROP_CORNER,CORNER_RIGHT_LOWER);
   ObjectSetInteger(wh,pButtonName,OBJPROP_ANCHOR,ANCHOR_RIGHT);
   ObjectSetInteger(wh,pButtonName,OBJPROP_XDISTANCE,62);
   ObjectSetInteger(wh,pButtonName,OBJPROP_YDISTANCE,15);
   ObjectSetInteger(wh,pButtonName,OBJPROP_XSIZE,61);
   ObjectSetInteger(wh,pButtonName,OBJPROP_YSIZE,14);
   ObjectSetInteger(wh,pButtonName,OBJPROP_COLOR,pTextColor);
   ObjectSetInteger(wh,pButtonName,OBJPROP_BGCOLOR,pBackColor);
   ObjectSetInteger(wh,pButtonName,OBJPROP_SELECTABLE,false);
//   ObjectSetInteger(wh,FR,OBJPROP_BACK,true);
   ObjectSetString(wh,pButtonName,OBJPROP_TEXT,pText);
   ObjectSetString(wh,pButtonName,OBJPROP_FONT,"Verdana");//"Calibri");
   ObjectSetInteger(wh,pButtonName,OBJPROP_FONTSIZE,5);
   ObjectSetDouble(wh,pButtonName,OBJPROP_ANGLE,90);
  }
//+------------------------------------------------------------------+
