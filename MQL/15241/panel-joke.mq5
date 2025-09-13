//****** Project (expert): panel_joke.mq5
//+------------------------------------------------------------------+
//|                The code generated MasterWindows Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
#property version       "1.00"
#property description   "Panel-joke or a game system"
//--- Connect class files
#include <Trade\Trade.mqh>
#include <ClassWin.mqh>
#include <Arrays\ArrayString.mqh>
MqlTick     last_tick;
CTrade      trade;
double      lot=0.01;
bool        on_trade=false;
string      strBuy,strSell,prcod;
input ENUM_TIMEFRAMES   tf=PERIOD_M5;// Период графика для расчёта сигнала
//+------------------------------------------------------------------+
//| Структура сигнала                                                |
//+------------------------------------------------------------------+
struct Signal
  {
   int               buy;  // вероятность BUY
   int               sell; // вероятность SELL
  };
//---
Signal   s;
//---
int Mint[][3]=
  {
     {1,0,0},
     {5,176,0},
     {5,176,0},
     {1,100,0},
     {6,75,100},
     {5,125,0},
     {}
  };
string Mstr[][3]=
  {
     {"    Panel-joke or a game system","",""},
     {"possibility Buy","Buy",""},
     {"possibility Sell","Sell",""},
     {"     BUY                                     SELL","",""},
     {"BUY","CLOSE","SELL"},
     {"autopilot","OFF",""},
     {}
  };
//+------------------------------------------------------------------+
//| class CMasterWindows (The master module)                         |
//+------------------------------------------------------------------+
class CMasterWindows:public CWin
  {
private:
   long              Y_hide;          // the window shift value
   long              Y_obj;           // the window shift value
   long              H_obj;           // the window shift value
public:
   bool              on_hide;         // flag HIDE mode
   CArrayString      units;           // elements of the main window
   void              CMasterWindows() {on_event=false; on_hide=false;}
   void              Run();           // run master module method
   void              Hide();          // Method: minimize the window
   void              Deinit() {ObjectsDeleteAll(0,0,-1); Comment("");}
   virtual void      OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Run Method Class CMasterWindows                                  |
//+------------------------------------------------------------------+
void CMasterWindows::Run()
  {
   ObjectsDeleteAll(0,0,-1);
   Comment("The code generated MasterWindows for MQL5 © DC2008");
//--- create a main window and run the executable
   SetWin("panel_joker.Exp",10,30,250,CORNER_LEFT_UPPER);
   Draw(Mint,Mstr,6);
  }
//+------------------------------------------------------------------+
//| Hide Method Class CMasterWindows                                 |
//+------------------------------------------------------------------+
void CMasterWindows::Hide()
  {
   Y_obj=w_ydelta;
   H_obj=Property.H;
   Y_hide=ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS,0)-Y_obj-H_obj;;
//---
   if(on_hide==false)
     {
      int n_str=units.Total();
      for(int i=0; i<n_str; i++)
        {
         long y_obj=ObjectGetInteger(0,units.At(i),OBJPROP_YDISTANCE);
         ObjectSetInteger(0,units.At(i),OBJPROP_YDISTANCE,(int)y_obj+(int)Y_hide);
         if(StringFind(units.At(i),".Button0",0)>0)
            ObjectSetString(0,units.At(i),OBJPROP_TEXT,CharToString(MAX_WIN));
        }
     }
   else
     {
      int n_str=units.Total();
      for(int i=0; i<n_str; i++)
        {
         long y_obj=ObjectGetInteger(0,units.At(i),OBJPROP_YDISTANCE);
         ObjectSetInteger(0,units.At(i),OBJPROP_YDISTANCE,(int)y_obj-(int)Y_hide);
         if(StringFind(units.At(i),".Button0",0)>0)
            ObjectSetString(0,units.At(i),OBJPROP_TEXT,CharToString(MIN_WIN));
        }
     }
//---
   ChartRedraw();
   on_hide=!on_hide;
  }
//+------------------------------------------------------------------+
//| The method of processing events OnChartEvent Class CMasterWindows|
//+------------------------------------------------------------------+
void CMasterWindows::OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam)
  {
   if(on_event // Event processing is enabled
      && StringFind(sparam,"panel_joker.Exp",0)>=0)
     {
      //--- event broadcast OnChartEvent
      STR1.OnEvent(id,lparam,dparam,sparam);
      STR2.OnEvent(id,lparam,dparam,sparam);
      STR3.OnEvent(id,lparam,dparam,sparam);
      STR4.OnEvent(id,lparam,dparam,sparam);
      STR5.OnEvent(id,lparam,dparam,sparam);
      STR6.OnEvent(id,lparam,dparam,sparam);
      //--- the creation of a graphic object
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CREATE)
        {
         if(StringFind(sparam,"panel_joker.Exp",0)>=0) units.Add(sparam);
        }
      //--- button press [Buy] STR1
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR1",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"panel: new position");
         return;
        }
      //--- button press [Sell] STR2
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR2",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"panel: new position");
         return;
        }
      //--- button press [BUY] STR4
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR4",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"panel: new position");
         return;
        }
      //--- button press [CLOSE] STR4
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR4",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         trade.PositionClose(_Symbol);
         return;
        }
      //--- button press [SELL] STR4
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR4",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"panel: new position");
         return;
        }
      //--- button press [OFF/ON] STR5
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR5",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         if(on_trade)
            ObjectSetString(0,sparam,OBJPROP_TEXT,"OFF");
         else
            ObjectSetString(0,sparam,OBJPROP_TEXT,"ON");
         on_trade=!on_trade;
         ChartRedraw(); return;
        }
      //--- button press Close in the main window
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".Button1",0)>0)
        {
         ExpertRemove();
        }
      //--- button press Hide in the main window
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".Button0",0)>0)
        {
         Hide();
        }
      ChartRedraw();
     }
  }
//---
//+------------------------------------------------------------------+
//| Функция определения направления торговли                         |
//+------------------------------------------------------------------+
Signal BUYorSELL()
  {
   Signal   option;
   double price[],type[2];
   option.buy=0;
   option.sell=0;
   ArraySetAsSeries(price,true);
   CopyOpen(_Symbol,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Open
   CopyHigh(_Symbol,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // High
   type[0]=price[0];
   type[1]=price[1];
   CopyLow(_Symbol,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Low
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]/2>type[1]/2) option.buy++;else option.sell++; // (High+Low)/2
   CopyClose(_Symbol,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Close
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]/3>type[1]/3) option.buy++;else option.sell++; // (High+Low+Close)/2
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]/4>type[1]/4) option.buy++;else option.sell++; // (High+Low+Close+Close)/2
   return(option);
  }
//--- classified master module
CMasterWindows MasterWin;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- run master module
   ChartSetInteger(0,CHART_EVENT_OBJECT_CREATE,0,true);
   MasterWin.Run();
   ushort cod=0x25A0;
   prcod=ShortToString(cod);
   prcod=prcod+prcod+prcod;
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- deinitialization of master module (remove all the garbage)
   MasterWin.Deinit();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   strBuy="";
   strSell="";
   s=BUYorSELL();
   ObjectSetString(0,"panel_joker.Exp.STR4.RowType6(1).Button",OBJPROP_TEXT,(string)(s.buy*100/7)+" %");
   ObjectSetString(0,"panel_joker.Exp.STR4.RowType6(3).Button",OBJPROP_TEXT,(string)(s.sell*100/7)+" %");
   for(int i=0;i<s.buy;i++)
      strBuy+=prcod;
   for(int i=0;i<s.sell;i++)
      strSell+=prcod;
   ObjectSetString(0,"panel_joker.Exp.STR1.RowType5.Text",OBJPROP_TEXT,strBuy);
   ObjectSetString(0,"panel_joker.Exp.STR2.RowType5.Text",OBJPROP_TEXT,strSell);
   ChartRedraw();
   if(on_trade)
     {
      //--- BUY
      if(s.buy>s.sell)
        {
         if(!PositionSelect(_Symbol))
           {
            SymbolInfoTick(_Symbol,last_tick);
            double price=last_tick.ask;
            trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"autopilot Buy: new position");
           }
         else
           {
            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
              {
               trade.PositionClose(_Symbol);
               SymbolInfoTick(_Symbol,last_tick);
               double price=last_tick.ask;
               trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"autopilot Buy: reversal");
              }
           }
        }

      //--- SELL
      if(s.sell>s.buy)
        {
         if(!PositionSelect(_Symbol))
           {
            SymbolInfoTick(_Symbol,last_tick);
            double price=last_tick.bid;
            trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"autopilot SELL: new position");
           }
         else
           {
            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
              {
               trade.PositionClose(_Symbol);
               SymbolInfoTick(_Symbol,last_tick);
               double price=last_tick.bid;
               trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"autopilot SELL: reversal");
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Expert Event function                                            |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//--- event broadcast OnChartEvent the main module
   MasterWin.OnEvent(id,lparam,dparam,sparam);
  }
//+------------------------------------------------------------------+
