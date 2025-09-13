//****** Project (expert): TPA.mq5
//+------------------------------------------------------------------+
//|                The code generated MasterWindows Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
#property version       "1.00"
#property description   "Trade panel with autopilot."
#property description   "Example of MasterWindows library."
//--- input parameters
input bool     inp_on_trade=false;  // Autopilot (On/Off)
input double   inp_open=85;         // Threshold values for the opening position
input double   inp_close=55;        // Threshold values for the closing position
input double   inp_lot_fix=0.01;    // lot fixed
input double   inp_lot_perc=0.01;   // lot as a percentage of equity
input bool     inp_on_lot=false;    // if "false" to % of the equity
input bool     inp_on_SL=false;     // Stop loss (On/Off)
//--- Connect class files
#include <Trade\Trade.mqh>
#include <ClassWin.mqh>
#include <Arrays\ArrayString.mqh>
//---
MqlTick     last_tick;
CTrade      trade;
double      lot=0.01;
double      open_position=85;
double      close_position=55;
bool        on_trade=false;
bool        on_SL=false;
string      strBuy,strSell,prcod;
int         intBuy,intSell;
double      Balans,Equity;
double      s_buy,s_sell;
int         size_tf;
//---- handles for indicators
int               Fractals_handle;
ENUM_TIMEFRAMES   Fractals_period=PERIOD_M10;
//---- indicator buffers
double      Upper[];
double      Lower[];
//--- You can make an independent choice to comment out unnecessary!
ENUM_TIMEFRAMES tfs[]=
  {
   PERIOD_M1,
   PERIOD_M2,
   PERIOD_M3,
   PERIOD_M4,
   PERIOD_M5,
   PERIOD_M6,
   PERIOD_M10,
   PERIOD_M12,
   PERIOD_M15,
   PERIOD_M20,
   PERIOD_M30,
   PERIOD_H1,
   PERIOD_H2,
   PERIOD_H3,
   PERIOD_H4,
   PERIOD_H6,
   PERIOD_H8,
   PERIOD_H12,
   PERIOD_D1,
   PERIOD_W1,
   PERIOD_MN1
  };
//+------------------------------------------------------------------+
//| The structure of the signal                                      |
//+------------------------------------------------------------------+
struct Signal
  {
   int               buy;  // ����������� BUY
   int               sell; // ����������� SELL
  };
//---
Signal   s;
//---
int Mint[][3]=
  {
     {1,0,0},
     {5,220,0},
     {5,220,0},
     {6,109,110},
     {}
  };
string Mstr[][3]=
  {
     {"             Trade panel with autopilot","",""},
     {"","BUY",""},
     {"","SELL",""},
     {"Autopilot  OFF","Stop loss  OFF","CLOSE"},
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
   Comment("The code generated MasterWindows for MQL5 � DC2008");
//--- create a main window and run the executable
   SetWin("TPA.Exp",10,30,300,CORNER_LEFT_UPPER);
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
      && StringFind(sparam,"TPA.Exp",0)>=0)
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
         if(StringFind(sparam,"TPA.Exp",0)>=0) units.Add(sparam);
        }
      //--- button press [BUY] STR1
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR1",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.ask;
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"BUY: new position");
        }
      //--- button press [SELL] STR2
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR2",0)>0
         && StringFind(sparam,".Button",0)>0)
        {
         //--- reaction to the planned event
         SymbolInfoTick(_Symbol,last_tick);
         double price=last_tick.bid;
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"SELL: new position");
        }
      //--- button press [AUTOPILOT] STR3
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR3",0)>0
         && StringFind(sparam,"(1)",0)>0)
        {
         //--- reaction to the planned event
         if(on_trade)
            ObjectSetString(0,sparam,OBJPROP_TEXT,"Autopilot  OFF");
         else
            ObjectSetString(0,sparam,OBJPROP_TEXT,"Autopilot  ON");
         on_trade=!on_trade;
         ChartRedraw(); return;
        }
      //--- button press [SL] STR3
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR3",0)>0
         && StringFind(sparam,"(2)",0)>0)
        {
         //--- reaction to the planned event
         if(on_SL)
            ObjectSetString(0,sparam,OBJPROP_TEXT,"Stop loss  OFF");
         else
            ObjectSetString(0,sparam,OBJPROP_TEXT,"Stop loss  ON");
         on_SL=!on_SL;
         OnTrade();
         ChartRedraw(); return;
        }
      //--- button press [CLOSE] STR3
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK
         && StringFind(sparam,".STR3",0)>0
         && StringFind(sparam,"(3)",0)>0)
        {
         //--- reaction to the planned event
         trade.PositionClose(_Symbol); return;
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
//+------------------------------------------------------------------+
//| ������� ����������� ����������� ��������                         |
//| determining the direction of trade function                      |
//+------------------------------------------------------------------+
Signal BUYorSELL(ENUM_TIMEFRAMES tf)
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
   if(type[0]>type[1]) option.buy++;else option.sell++; // (High+Low)/2
   CopyClose(_Symbol,tf,0,2,price);
   if(price[0]>price[1]) option.buy++;else option.sell++;   // Close
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]>type[1]) option.buy++;else option.sell++; // (High+Low+Close)/3
   type[0]+=price[0];
   type[1]+=price[1];
   if(type[0]>type[1]) option.buy++;else option.sell++; // (High+Low+Close+Close)/4
   return(option);
  }
//--- classified master module
CMasterWindows MasterWin;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- initialization input parameters
   on_trade=inp_on_trade;
   on_SL=inp_on_SL;
   size_tf=ArraySize(tfs);
//--- run master module
   ChartSetInteger(0,CHART_EVENT_OBJECT_CREATE,0,true);
   MasterWin.Run();
//---
   ushort cod=0x25A0;
   prcod=ShortToString(cod);
   prcod=prcod;
//---
   Fractals_handle=iFractals(NULL,Fractals_period);
//--- Display panel initial values
   if(on_trade)
      ObjectSetString(0,"TPA.Exp.STR3.RowType6(1).Button",OBJPROP_TEXT,"Autopilot  ON");
   else
      ObjectSetString(0,"TPA.Exp.STR3.RowType6(1).Button",OBJPROP_TEXT,"Autopilot  OFF");
   if(on_SL)
      ObjectSetString(0,"TPA.Exp.STR3.RowType6(2).Button",OBJPROP_TEXT,"Stop loss  ON");
   else
      ObjectSetString(0,"TPA.Exp.STR3.RowType6(2).Button",OBJPROP_TEXT,"Stop loss  OFF");
   ChartRedraw();
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- calculation of the lot of equity
   Equity=AccountInfoDouble(ACCOUNT_EQUITY);
   if(!inp_on_lot)
      lot=NormalizeDouble(Equity*inp_lot_perc/1000.0,2);
   else
      lot=inp_lot_fix;
   if(lot<0.01) lot=0.01;
//--- ���������� � ����������� �������� / Monitoring and display signals
   s_buy=0;
   s_sell=0;
   strBuy="";
   strSell="";
   intBuy=0;
   intSell=0;
   for(int j=0;j<size_tf-1;j++)
     {
      s=BUYorSELL(tfs[j]);
      intBuy+=s.buy;
      intSell+=s.sell;
     }
   s_buy=(int)((double)intBuy/(double)(intBuy+intSell)*100);
   s_sell=(int)((double)intSell/(double)(intBuy+intSell)*100);
   ObjectSetString(0,"TPA.Exp.STR1.RowType5.Button",OBJPROP_TEXT,"BUY "+(string)(s_buy)+"%");
   ObjectSetString(0,"TPA.Exp.STR2.RowType5.Button",OBJPROP_TEXT,"SELL "+(string)(s_sell)+"%");

   int n_buy=(int)((double)intBuy/(double)(intBuy+intSell)*27);
   for(int i=0;i<n_buy;i++)
      strBuy+=prcod;

   int n_sell=(int)((double)intSell/(double)(intBuy+intSell)*27);
   for(int i=0;i<n_sell;i++)
      strSell+=prcod;
   ObjectSetString(0,"TPA.Exp.STR1.RowType5.Text",OBJPROP_TEXT,strBuy);
   ObjectSetString(0,"TPA.Exp.STR2.RowType5.Text",OBJPROP_TEXT,strSell);
//--- buttons for clarity lights
   if(s_buy>inp_open)
      ObjectSetInteger(0,"TPA.Exp.STR1.RowType5.Button",OBJPROP_BGCOLOR,clrFireBrick);
   else
      ObjectSetInteger(0,"TPA.Exp.STR1.RowType5.Button",OBJPROP_BGCOLOR,clrSteelBlue);
   if(s_sell>inp_open)
      ObjectSetInteger(0,"TPA.Exp.STR2.RowType5.Button",OBJPROP_BGCOLOR,clrFireBrick);
   else
      ObjectSetInteger(0,"TPA.Exp.STR2.RowType5.Button",OBJPROP_BGCOLOR,clrSteelBlue);
   if(PositionSelect(_Symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         if(s_sell<inp_close)
            ObjectSetInteger(0,"TPA.Exp.STR3.RowType6(3).Button",OBJPROP_BGCOLOR,clrFireBrick);
         else
            ObjectSetInteger(0,"TPA.Exp.STR3.RowType6(3).Button",OBJPROP_BGCOLOR,clrSteelBlue);
        }
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         if(s_buy<inp_close)
            ObjectSetInteger(0,"TPA.Exp.STR3.RowType6(3).Button",OBJPROP_BGCOLOR,clrFireBrick);
         else
            ObjectSetInteger(0,"TPA.Exp.STR3.RowType6(3).Button",OBJPROP_BGCOLOR,clrSteelBlue);
        }
     }
   else
      ObjectSetInteger(0,"TPA.Exp.STR3.RowType6(3).Button",OBJPROP_BGCOLOR,clrSteelBlue);
//---
   if(on_trade)
     {
      //--- CLOSE
      if(PositionSelect(_Symbol))
        {
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
            if(s_sell<inp_close)
               trade.PositionClose(_Symbol);
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
            if(s_buy<inp_close)
               trade.PositionClose(_Symbol);
        }
      //--- BUY
      if(s_buy>inp_open)
         if(!PositionSelect(_Symbol))
           {
            SymbolInfoTick(_Symbol,last_tick);
            double price=last_tick.ask;
            trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,"autopilot BUY: new position");
           }

      //--- SELL
      if(s_sell>inp_open)
         if(!PositionSelect(_Symbol))
           {
            SymbolInfoTick(_Symbol,last_tick);
            double price=last_tick.bid;
            trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,"autopilot SELL: new position");
           }
     }
//---
   ChartRedraw();
//---
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
//| Expert OnTrade function                                          |
//+------------------------------------------------------------------+
void OnTrade()
  {
   if(on_SL)
     {
      int buf=100;
      CopyBuffer(Fractals_handle,0,0,buf,Upper);
      ArraySetAsSeries(Upper,true);
      CopyBuffer(Fractals_handle,1,0,buf,Lower);
      ArraySetAsSeries(Lower,true);
      //--- ����� �������� ��� ��������� SL
      //--- Search fractal to install SL
      if(PositionSelect(_Symbol))
        {
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
            if(PositionGetDouble(POSITION_SL)==NULL)
               for(int i=0;i<buf-1;i++)
                  if(Lower[i]<DBL_MAX)
                     if(Lower[i]<PositionGetDouble(POSITION_PRICE_CURRENT))
                       {
                        bool res=trade.PositionModify(_Symbol,Lower[i],0);
                        if(res) return;
                       }
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
            if(PositionGetDouble(POSITION_SL)==NULL)
               for(int i=0;i<buf-1;i++)
                  if(Upper[i]<DBL_MAX)
                     if(Upper[i]>PositionGetDouble(POSITION_PRICE_CURRENT))
                       {
                        bool res=trade.PositionModify(_Symbol,Upper[i],0);
                        if(res) return;
                       }
        }
     }
  }
//+------------------------------------------------------------------+
