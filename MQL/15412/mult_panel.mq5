//****** Project (expert): mult_panel.mq5
//+------------------------------------------------------------------+
//|                The code generated MasterWindows Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
#property version       "1.00"
#property description   "Multicurrency trading panel (program-joke:) or a game system"
//--- Connect class files
#include <Trade\Trade.mqh>
#include <ClassWin.mqh>
#include <Arrays\ArrayString.mqh>
//--- Переменные / variables
MqlTick           last_tick;
CTrade            trade;
double            lot[3]={0.01,0.01,0.01};
ENUM_TIMEFRAMES   tfr[3];
string            strBuy[3],strSell[3],objBuy[3],objSell[3],btBuy[3],btSell[3],prcod;
string            str_vPos[3],str_prPos[3],scBuy[3],scSell[3];
double            vPos[3],prPos[3];
bool              posBuy[3],posSell[3];
double            Balans,Equity;
string            Symb[3]={"EURUSD","USDJPY","GBPUSD"};
bool              on_trade=false;
//--- Входные параметры / Input parameters
input ENUM_TIMEFRAMES   tf1=PERIOD_M5;// Период графика EURUSD для расчёта сигнала
input ENUM_TIMEFRAMES   tf2=PERIOD_M5;// Период графика USDJPY для расчёта сигнала
input ENUM_TIMEFRAMES   tf3=PERIOD_M5;// Период графика GBPUSD для расчёта сигнала
//+------------------------------------------------------------------+
//| Структура сигнала                                                |
//+------------------------------------------------------------------+
struct Signal
  {
   int               buy;  // вероятность BUY
   int               sell; // вероятность SELL
  };
//---
Signal   s[3];
//---
Signal BUYorSELL(string Sym,ENUM_TIMEFRAMES tf)
  {
   Signal   option;
   double price[],type[2];
   option.buy=0;
   option.sell=0;
   ArraySetAsSeries(price,true);
   CopyOpen(Sym,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Open
   CopyHigh(Sym,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // High
   type[0]=price[0];
   type[1]=price[1];
   CopyLow(Sym,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Low
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]>type[1]) option.buy++;else option.sell++;     // (High+Low)/2
   CopyClose(Sym,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Close
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]>type[1]) option.buy++;else option.sell++;     // (High+Low+Close)/3
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]>type[1]) option.buy++;else option.sell++;     // (High+Low+Close+Close)/4
   return(option);
  }
//---
int Mint[][3]=
  {
     {1,0,0},
     {1,100,50},
     {5,200,0},
     {5,200,50},
     {3,200,0},
     {6,100,100},
     {1,100,0},
     {5,200,50},
     {5,200,0},
     {3,200,0},
     {6,100,100},
     {1,100,0},
     {5,200,50},
     {5,200,0},
     {3,200,0},
     {6,100,100},
     {1,100,50},
     {6,150,50},
     {}
  };
string Mstr[][3]=
  {
     {"Multicurrency trading panel (program-joke:)","",""},
     {"EURUSD","",""},
     {"","BUY",""},
     {"","SELL",""},
     {"Position=0.00                      Lot=","0.01",""},
     {"Ask","CLOSE","Bid"},
     {"USDJPY","",""},
     {"","BUY",""},
     {"","SELL",""},
     {"Position=0.00                      Lot=","0.01",""},
     {"Ask","CLOSE","Bid"},
     {"GBPUSD","",""},
     {"","BUY",""},
     {"","SELL",""},
     {"Position=0.00                      Lot=","0.01",""},
     {"Ask","CLOSE","Bid"},
     {"","","CLOSE ALL"},
     {"AutoTrader ON","ClrLot","CLOSE ALL"},
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
   SetWin("mult_panel.Exp",10,30,300,CORNER_LEFT_UPPER);
   Draw(Mint,Mstr,18);
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
      && StringFind(sparam,"mult_panel.Exp",0)>=0)
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
         if(StringFind(sparam,"mult_panel.Exp",0)>=0) units.Add(sparam);
        }
      //--- button press [BUY] STR2
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR2",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[0],last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(Symb[0],ORDER_TYPE_BUY,NormalizeDouble(lot[0],2),price,0,0,"panel BUY: new position");
         return;
        }
      //--- button press [SELL] STR3
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR3",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[0],last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(Symb[0],ORDER_TYPE_SELL,NormalizeDouble(lot[0],2),price,0,0,"panel SELL: new position");
         return;
        }
      //--- editing variables [Lot] : button Plus STR4
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR4",0)>0
         && StringFind(sparam,".Button3",0)>0)
        {
         //--- reaction to the planned event
         lot[0]+=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR4.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[0],2));
         ChartRedraw(); return;
        }
      //--- editing variables [Lot] : button Minus STR4
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR4",0)>0
         && StringFind(sparam,".Button4",0)>0)
        {
         //--- reaction to the planned event
         lot[0]-=0.01;
         if(lot[0]<0.01) lot[0]=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR4.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[0],2));
         ChartRedraw(); return;
        }
      //--- button press [Ask] STR5
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR5",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[0],last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(Symb[0],ORDER_TYPE_BUY,NormalizeDouble(lot[0],2),price,0,0,"panel BUY: new position");
         return;
        }
      //--- button press [CLOSE] STR5
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR5",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         trade.PositionClose(Symb[0]);
         return;
        }
      //--- button press [Bid] STR5
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR5",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[0],last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(Symb[0],ORDER_TYPE_SELL,NormalizeDouble(lot[0],2),price,0,0,"panel SELL: new position");
         return;
        }
      //--- button press [BUY] STR7
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR7",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[1],last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(Symb[1],ORDER_TYPE_BUY,NormalizeDouble(lot[1],2),price,0,0,"panel BUY: new position");
         return;
        }
      //--- button press [SELL] STR8
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR8",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[1],last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(Symb[1],ORDER_TYPE_SELL,NormalizeDouble(lot[1],2),price,0,0,"panel SELL: new position");
         return;
        }
      //--- editing variables [Lot] : button Plus STR9
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR9",0)>0
         && StringFind(sparam,".Button3",0)>0)
        {
         //--- reaction to the planned event
         lot[1]+=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR9.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[1],2));
         ChartRedraw(); return;
        }
      //--- editing variables [Lot] : button Minus STR9
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR9",0)>0
         && StringFind(sparam,".Button4",0)>0)
        {
         //--- reaction to the planned event
         lot[1]-=0.01;
         if(lot[1]<0.01) lot[1]=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR9.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[1],2));
         ChartRedraw(); return;
        }
      //--- button press [Ask] STR10
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR10",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[1],last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(Symb[1],ORDER_TYPE_BUY,NormalizeDouble(lot[1],2),price,0,0,"panel BUY: new position");
         return;
        }
      //--- button press [CLOSE] STR10
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR10",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         trade.PositionClose(Symb[1]);
         return;
        }
      //--- button press [Bid] STR10
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR10",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[1],last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(Symb[1],ORDER_TYPE_SELL,NormalizeDouble(lot[1],2),price,0,0,"panel SELL: new position");
         return;
        }
      //--- button press [BUY] STR12
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR12",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[2],last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(Symb[2],ORDER_TYPE_BUY,NormalizeDouble(lot[2],2),price,0,0,"panel BUY: new position");
         return;
        }
      //--- button press [SELL] STR13
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR13",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[2],last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(Symb[2],ORDER_TYPE_SELL,NormalizeDouble(lot[2],2),price,0,0,"panel SELL: new position");
         return;
        }
      //--- editing variables [Lot] : button Plus STR14
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR14",0)>0
         && StringFind(sparam,".Button3",0)>0)
        {
         //--- reaction to the planned event
         lot[2]+=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR14.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[2],2));
         ChartRedraw(); return;
        }
      //--- editing variables [Lot] : button Minus STR14
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR14",0)>0
         && StringFind(sparam,".Button4",0)>0)
        {
         //--- reaction to the planned event
         lot[2]-=0.01;
         if(lot[2]<0.01) lot[2]=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR14.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[2],2));
         ChartRedraw(); return;
        }
      //--- button press [Ask] STR15
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR15",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[2],last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(Symb[2],ORDER_TYPE_BUY,NormalizeDouble(lot[2],2),price,0,0,"panel BUY: new position");
         return;
        }
      //--- button press [CLOSE] STR15
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR15",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         trade.PositionClose(Symb[2]);
         return;
        }
      //--- button press [Bid] STR15
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR15",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(Symb[2],last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(Symb[2],ORDER_TYPE_SELL,NormalizeDouble(lot[2],2),price,0,0,"panel SELL: new position");
         return;
        }
      //--- button press [AutoTrader ON] STR17
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR17",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         if(on_trade)
            ObjectSetString(0,sparam,OBJPROP_TEXT,"AutoTrader OFF");
         else
            ObjectSetString(0,sparam,OBJPROP_TEXT,"AutoTrader ON");
         on_trade=!on_trade;
         for(int i=0;i<3; i++)
           {
            ObjectSetInteger(0,scBuy[i],OBJPROP_BGCOLOR,clrSteelBlue);
            ObjectSetInteger(0,scSell[i],OBJPROP_BGCOLOR,clrSteelBlue);
           }
         ChartRedraw(); return;
        }
      //--- button press [ClrLot] STR17
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR17",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         for(int i=0;i<3; i++)
            lot[i]=0.01;
         ObjectSetString(0,"mult_panel.Exp.STR4.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[0],2));
         ObjectSetString(0,"mult_panel.Exp.STR9.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[1],2));
         ObjectSetString(0,"mult_panel.Exp.STR14.RowType3.Edit",OBJPROP_TEXT,DoubleToString(lot[2],2));
         ChartRedraw(); return;
        }
      //--- button press [CLOSE ALL] STR17
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR17",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         for(int i=0;i<3; i++)
           {
            trade.PositionClose(Symb[i]);
            ObjectSetInteger(0,scBuy[i],OBJPROP_BGCOLOR,clrSteelBlue);
            ObjectSetInteger(0,scSell[i],OBJPROP_BGCOLOR,clrSteelBlue);
           }
         ChartRedraw(); return;
         //--- reaction to the planned event
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
//--- замена цветов фона / change the background colors
   ObjectSetInteger(0,"mult_panel.Exp.STR1.RowType1(3).Text",OBJPROP_BGCOLOR,clrRed);
   ObjectSetInteger(0,"mult_panel.Exp.STR6.RowType1(3).Text",OBJPROP_BGCOLOR,clrRed);
   ObjectSetInteger(0,"mult_panel.Exp.STR11.RowType1(3).Text",OBJPROP_BGCOLOR,clrRed);
   ObjectSetInteger(0,"mult_panel.Exp.STR16.RowType1(3).Text",OBJPROP_BGCOLOR,clrMediumBlue);
//---
   ushort cod=0x25A0;
   prcod=ShortToString(cod);
   prcod=prcod+prcod+prcod;
//--- array initialization
   tfr[0]=tf1; tfr[1]=tf2; tfr[2]=tf3;
   objBuy[0]="mult_panel.Exp.STR2.RowType5.Text";
   objSell[0]="mult_panel.Exp.STR3.RowType5.Text";
   objBuy[1]="mult_panel.Exp.STR7.RowType5.Text";
   objSell[1]="mult_panel.Exp.STR8.RowType5.Text";
   objBuy[2]="mult_panel.Exp.STR12.RowType5.Text";
   objSell[2]="mult_panel.Exp.STR13.RowType5.Text";
//---
   btBuy[0]="mult_panel.Exp.STR5.RowType6(1).Button";
   btSell[0]="mult_panel.Exp.STR5.RowType6(3).Button";
   btBuy[1]="mult_panel.Exp.STR10.RowType6(1).Button";
   btSell[1]="mult_panel.Exp.STR10.RowType6(3).Button";
   btBuy[2]="mult_panel.Exp.STR15.RowType6(1).Button";
   btSell[2]="mult_panel.Exp.STR15.RowType6(3).Button";
//---
   scBuy[0]="mult_panel.Exp.STR2.RowType5.Text";
   scSell[0]="mult_panel.Exp.STR3.RowType5.Text";
   scBuy[1]="mult_panel.Exp.STR7.RowType5.Text";
   scSell[1]="mult_panel.Exp.STR8.RowType5.Text";
   scBuy[2]="mult_panel.Exp.STR12.RowType5.Text";
   scSell[2]="mult_panel.Exp.STR13.RowType5.Text";
//---
   str_vPos[0]="mult_panel.Exp.STR4.RowType3.Text";
   str_vPos[1]="mult_panel.Exp.STR9.RowType3.Text";
   str_vPos[2]="mult_panel.Exp.STR14.RowType3.Text";
//---
   str_prPos[0]="mult_panel.Exp.STR1.RowType1(3).Text";
   str_prPos[1]="mult_panel.Exp.STR6.RowType1(3).Text";
   str_prPos[2]="mult_panel.Exp.STR11.RowType1(3).Text";
//---
   return(INIT_SUCCEEDED);
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
   Balans=AccountInfoDouble(ACCOUNT_BALANCE);
   Equity=AccountInfoDouble(ACCOUNT_EQUITY);
   ObjectSetString(0,"mult_panel.Exp.STR16.RowType1(3).Text",OBJPROP_TEXT,
                   "Balance = "+DoubleToString(Balans,2)+
                   "          Profit = "+DoubleToString(Equity-Balans,2));
//--- перебор валютных пар в цикле / bust currency pairs in the cycle
   for(int i=0;i<3; i++)
     {
      strBuy[i]=ShortToString(0x25C7)+ShortToString(0x250A);
      strSell[i]=ShortToString(0x25C7)+ShortToString(0x250A);
      if(PositionSelect(Symb[i]))
        {
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
            strBuy[i]=ShortToString(0x25C6)+ShortToString(0x250A);
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
            strSell[i]=ShortToString(0x25C6)+ShortToString(0x250A);
        }
      s[i]=BUYorSELL(Symb[i],tfr[i]);
      ObjectSetString(0,btBuy[i],OBJPROP_TEXT,"Buy "+(string)(s[i].buy*100/7)+"%");
      ObjectSetString(0,btSell[i],OBJPROP_TEXT,"Sell "+(string)(s[i].sell*100/7)+"%");
      for(int j=0;j<s[i].buy;j++)
         strBuy[i]+=prcod;
      for(int j=0;j<s[i].sell;j++)
         strSell[i]+=prcod;
      ObjectSetString(0,objBuy[i],OBJPROP_TEXT,strBuy[i]);
      ObjectSetString(0,objSell[i],OBJPROP_TEXT,strSell[i]);
      //--- информация по открытым позициям / Information on open positions
      if(PositionSelect(Symb[i]))
        {
         vPos[i]=PositionGetDouble(POSITION_VOLUME);
         prPos[i]=PositionGetDouble(POSITION_PROFIT);
        }
      else
        {
         vPos[i]=0;
         prPos[i]=0;
        }
      ObjectSetString(0,str_vPos[i],OBJPROP_TEXT,"Position="+DoubleToString(vPos[i],2));
      ObjectSetString(0,str_prPos[i],OBJPROP_TEXT,Symb[i]+
                      "                                    profit = "+DoubleToString(prPos[i],2));
      ChartRedraw();
      //---
      if(on_trade)
        {
         //--- BUY
         if(s[i].buy>s[i].sell)
           {
            if(!PositionSelect(Symb[i]))
              {
               SymbolInfoTick(Symb[i],last_tick);
               double price=last_tick.ask;
               trade.PositionOpen(Symb[i],ORDER_TYPE_BUY,NormalizeDouble(lot[i],2),price,0,0,"autotrader BUY: new position");
              }
            else
              {
               if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
                 {
                  trade.PositionClose(Symb[i]);
                  SymbolInfoTick(Symb[i],last_tick);
                  double price=last_tick.ask;
                  trade.PositionOpen(Symb[i],ORDER_TYPE_BUY,NormalizeDouble(lot[i],2),price,0,0,"autotrader BUY: reversal");
                 }
              }
           }

         //--- SELL
         if(s[i].sell>s[i].buy)
           {
            if(!PositionSelect(Symb[i]))
              {
               SymbolInfoTick(Symb[i],last_tick);
               double price=last_tick.bid;
               trade.PositionOpen(Symb[i],ORDER_TYPE_SELL,NormalizeDouble(lot[i],2),price,0,0,"autotrader SELL: new position");
              }
            else
              {
               if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
                 {
                  trade.PositionClose(Symb[i]);
                  SymbolInfoTick(Symb[i],last_tick);
                  double price=last_tick.bid;
                  trade.PositionOpen(Symb[i],ORDER_TYPE_SELL,NormalizeDouble(lot[i],2),price,0,0,"autotrader SELL: reversal");
                 }
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
