//****** Project (expert): panel.mq5
//+------------------------------------------------------------------+
//|                The code generated MasterWindows Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010-2016, DC2008"
//--- Connect class files
#include <Trade\Trade.mqh>
#include <ClassWin.mqh>
#include <Arrays\ArrayString.mqh>
MqlTick     last_tick;
CTrade      trade;
double      lot=0.01;
//---
int Mint[][3]=
  {
     {1,0,0},
     {3,100,0},
     {6,100,60},
     {6,130,0},
     {}
  };
string Mstr[][3]=
  {
     {"             Торговая панель","",""},
     {"        Лот","0,01",""},
     {"BUY","CLOSE","SELL"},
     {"price BUY","","price SELL"},
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
   SetWin("panel.Exp",10,30,250,CORNER_LEFT_UPPER);
   Draw(Mint,Mstr,4);
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
      && StringFind(sparam,"panel.Exp",0)>=0)
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
         if(StringFind(sparam,"panel.Exp",0)>=0) units.Add(sparam);
        }
      //--- editing variables [Лот] : button Plus STR1 panel.Exp.STR1.RowType3.Edit
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR1",0)>0
         && StringFind(sparam,".Button3",0)>0)
        {
         //--- reaction to the planned event
         lot=lot+0.01;
         ObjectSetString(0,"panel.Exp.STR1.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot,2));
        }
      //--- editing variables [Лот] : button Minus STR1
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR1",0)>0
         && StringFind(sparam,".Button4",0)>0)
        {
         //--- reaction to the planned event
         lot=lot-0.01;
         if(lot<0.01) lot=0.01;
         ObjectSetString(0,"panel.Exp.STR1.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot,2));
        }
      //--- button press [BUY] STR2
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR2",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"panel: new position");
        }
      //--- button press [CLOSE] STR2
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR2",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         trade.PositionClose(_Symbol);
        }
      //--- button press [SELL] STR2
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR2",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"panel: new position");
        }
      //--- button press [price BUY] STR3
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR3",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         ObjectSetString(0,"panel.Exp.STR3.RowType6(1).Button",OBJPROP_TEXT,(string)last_tick.ask);
         double price=last_tick.ask;
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"panel: new position");
        }
      //--- button press [price SELL] STR3
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR3",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         ObjectSetString(0,"panel.Exp.STR3.RowType6(3).Button",OBJPROP_TEXT,(string)last_tick.bid);
         double price=last_tick.bid;
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"panel: new position");
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
   SymbolInfoTick(_Symbol,last_tick);
   ObjectSetString(0,"panel.Exp.STR3.RowType6(1).Button",OBJPROP_TEXT,(string)last_tick.ask);
   ObjectSetString(0,"panel.Exp.STR3.RowType6(3).Button",OBJPROP_TEXT,(string)last_tick.bid);
   ObjectSetString(0,"panel.Exp.STR1.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot,2));
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
   SymbolInfoTick(_Symbol,last_tick);
   ObjectSetString(0,"panel.Exp.STR3.RowType6(1).Button",OBJPROP_TEXT,(string)last_tick.ask);
   ObjectSetString(0,"panel.Exp.STR3.RowType6(3).Button",OBJPROP_TEXT,(string)last_tick.bid);
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
