//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2021, CompanyName |
//|                           "https://www.mql5.com/en/users/stiopa" |
//+------------------------------------------------------------------+
#property copyright "Stiopa"
#property link      "https://www.mql5.com/en/users/stiopa"
#property version   "1.20"
#property strict
#include <Controls\Dialog.mqh>
#include <Controls\BmpButton.mqh>
#include <Controls\Button.mqh>
#include <Controls\Label.mqh>
#include <Controls\SpinEdit.mqh>
enum ENUM_START
  {
   Short_Only=1,//Short Only
   Long_Only=2,//Long Only
   Short_Long=3,//Short Long
   Off=4
  };
input string          comment="Smart Forex System";//Comment
//extern int            spread=5;//Max Spread
input int             slippage=1;//Slippage
input int             MagicNumber=1;//Magic Number
input ENUM_START      Start=3;
input string          a2a=" ";//--------> STRATEGIES <---------------------------------------
input ENUM_TIMEFRAMES TimeFrames=PERIOD_D1;//Time Frames
input double          percent=1;//Percent
//input string          a3=" ";//--------> FILTER <---------------------------------------
//input ushort          atr1=200;//Max ATR (Open position if iATR(1440) < Max ATR)
//input ushort          atr_n_open=8;//Min ATR (H1)
//input string          a4=" ";//--------> TREND <---------------------------------------
//extern ushort         period_trend=230;//Trend in pips. 0=Off
input string          a5=" ";//--------> MAIN <---------------------------------------
input ushort          SL=400;//Stop Loss
//input ushort          min_tp=15;//Min Take Profit (first position)
input ushort          max_tp=30;//Max Take Profit (first position)
//extern ushort         max_grid_tp=150;//Max Grid Profit (0=off)
extern double         tp_chart=7;//Take Profit
//input ushort          max_next_tp=40;//Max Take Profit (next positions)
//input bool            swap_true=true;//TP+Swap+
input string          a6=" ";//--------> MM <---------------------------------------
extern double         Lots=0.01;//Lots
extern double         MaxLots=2;//Max Lots
input double          power=1.5;//Power
input string          a7=" ";//--------> GRID MANAGMENT <---------------------------------------
extern ushort         PipsStep=26;//Min PipsStep for grid
extern int            MaxTrades=12;//MaxTrades
uchar                 tp_pips=1;
int Tick;
int pr_60;
double gpoint_320;

//+------------------------------------------------------------------+
//| For Broker 4 or 5 digits                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   EventSetTimer(1);
   if(Digits==3 ||Digits==5)
     {
      gpoint_320=Point*10;
     }
   else
     {
      gpoint_320=Point;
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| Main function                                                    |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(Bars<100 || !IsConnected()|| IsTradeAllowed()==false)
      return;
//-----------------------------------------------
   HideTestIndicators(true);
   Buy_main("buy");
   Sell_main("sell");
  }
//bool Spread()
// {
//}
void different()
  {
  }
//+------------------------------------------------------------------+
//| Modify Take Profit and Stop Loss                                 |
//+------------------------------------------------------------------+
void modify(string ot_456)
  {
   double tp_chart_0,tp_chart_1;
   int c1=cou_tra_45("buy"),
       c2=cou_tra_45("sell");
   if(c1==1)
      tp_chart_0=max_tp;
   else
      tp_chart_0=tp_chart;
   if(c2==1)
      tp_chart_1=max_tp;
   else
      tp_chart_1=tp_chart;
   double last_price=select(ot_456,"price");
   int tr_45;
   double TP_all=0,SL_all;
   double avp_2;
   bool order;
   avp_2=avp_next2(ot_456);
   for(tr_45=OrdersTotal()-1; tr_45>=0; tr_45--)
     {
      if(!OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES))
         Print("Error in OrderSelect Takeprofit==true. Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(ot_456=="buy")
           {
            if(OrderType()==OP_BUY)
              {
               TP_all=nor_1(avp_2+(tp_chart_0*gpoint_320));
               SL_all=nor_1(last_price-(SL*gpoint_320));
               if(check_sl_tp(OP_BUY,SL_all,TP_all))
                  if(OrderTakeProfit()!=TP_all || OrderStopLoss()!=SL_all)
                     order=OrderModify(OrderTicket(),nor_1(avp_2),SL_all,TP_all,0,Yellow);
               if(order)
                  Print("All TP for buy successfully");
               else
                  Print("Error in OrderModify buy Average + TP. Error code=",GetLastError());
              }
           }
         if(ot_456=="sell")
           {
            if(OrderType()==OP_SELL)
              {
               TP_all=nor_1(avp_2-(tp_chart_1*gpoint_320));
               SL_all=nor_1(last_price+(SL*gpoint_320));
               if(check_sl_tp(OP_SELL,SL_all,TP_all))
                  if(OrderTakeProfit()!=TP_all || OrderStopLoss()!=SL_all)
                     order=OrderModify(OrderTicket(),nor_1(avp_2),SL_all,TP_all,0,Yellow);
               if(order)
                  Print("All TP for sell successfully");
               else
                  Print("Error in OrderModify sell Average - TP. Error code=",GetLastError());
              }
           }

        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Close_Select(string ot_456)
  {
  }
//int close_select(string ot_456)
// {
// }
//double avp_next(string ot_456)
// {
// }

//+------------------------------------------------------------------+
//| Average Order Price                                              |
//+------------------------------------------------------------------+
double avp_next2(string ot_456)
  {
   double Count=0,avp_2=0;
   int tr_45;
   for(tr_45=OrdersTotal()-1; tr_45>=0; tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSelect avp_2. Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(ot_456=="buy")
           {
            if(OrderType()==OP_BUY)
              {
               avp_2=avp_2+OrderOpenPrice()*OrderLots();
               Count=Count+OrderLots();
              }
           }
         if(ot_456=="sell")
           {
            if(OrderType()==OP_SELL)
              {
               avp_2=avp_2+OrderOpenPrice()*OrderLots();
               Count=Count+OrderLots();
              }
           }
        }
     }
   if(avp_2>0 && Count>0)
      avp_2=NormalizeDouble(avp_2/Count,Digits);
   return(avp_2);
  }
//double select_history2(string ot_456,string inf)
// {
//}
void close_first_tp()
  {
  }
//+------------------------------------------------------------------+
//|       Buy                                                           |
//+------------------------------------------------------------------+
void Buy_main(string ot_456)
  {
   if(iVolume(NULL,0,0)>1)
      return;
   double lots;
   if(OrderAllowed())
      if((cou_tra_45(ot_456)==0 && Signal_1()==1 && (Start==2 || Start==3))||(cou_tra_45(ot_456)>0 && nx_77(ot_456)))
        {
         lots=CheckVolumeValue(ne_234(ot_456));
         if(CheckMoney(lots,OP_BUY))
           {
            Tick=0;
            while(Tick<1)
              {
               while(IsTradeContextBusy())
                  Sleep(100);
               RefreshRates();
               Tick=OrderSend(Symbol(),OP_BUY,lots,nor_1(Ask),slippage,0,0,comment,MagicNumber,0,Blue);
               if(Tick<1)
                 {
                  Print("Error: ",GetLastError());
                  Sleep(1000);
                  RefreshRates();
                 }
               else
                 {
                  if(cou_tra_45(ot_456)>=1)
                     modify(ot_456);
                 }
              }
           }
        }
  }
//+------------------------------------------------------------------+
//|   Sell                                                               |
//+------------------------------------------------------------------+
void Sell_main(string ot_456)
  {
   if(iVolume(NULL,0,0)>1)
      return;
   double lots;
   if(OrderAllowed())
      if((cou_tra_45(ot_456)==0 && Signal_1()==2 && (Start==1 || Start==3))||(cou_tra_45(ot_456)>0 && nx_77(ot_456)))
        {
         lots=CheckVolumeValue(ne_234(ot_456));
         if(CheckMoney(lots,OP_SELL))
           {
            Tick=0;
            while(Tick<1)
              {
               while(IsTradeContextBusy())
                  Sleep(100);
               RefreshRates();
               Tick=OrderSend(Symbol(),OP_SELL,lots,nor_1(Bid),slippage,0,0,comment,MagicNumber,0,Red);
               if(Tick<1)
                 {
                  Print("Error: ",GetLastError());
                  Sleep(1000);
                  RefreshRates();
                 }
               else
                 {
                  if(cou_tra_45(ot_456)>=1)
                     modify(ot_456);
                 }
              }
           }
        }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void speed()
  {
  }
//+------------------------------------------------------------------+
//|  Lots_Digits                                                                |
//+------------------------------------------------------------------+
int Lots_Digits()
  {
   double Lotstep=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int    LotsDigits=(int)MathCeil(MathAbs(MathLog(Lotstep)/MathLog(10)));
   return(LotsDigits);
  }
//+------------------------------------------------------------------+
//|  MODE_STOPLEVEL                                                                |
//+------------------------------------------------------------------+
double nd_tpsl(double a_0)
  {
   RefreshRates();
   double StopLevel=MarketInfo(Symbol(),MODE_STOPLEVEL);
   if(a_0<StopLevel*gpoint_320)
      a_0=StopLevel*gpoint_320;
   return(NormalizeDouble(a_0, Digits));
  }
//double swap(string ot_456)
// {
// return;
// }
void button()
  {

  }
//double bal_ance_3(string ot_456,string inf)
// {
// }

//+------------------------------------------------------------------+
//|   Normalize Double                                                               |
//+------------------------------------------------------------------+
double nor_1(double ad_0)
  {
   return (NormalizeDouble(ad_0, Digits));
  }
//+------------------------------------------------------------------+
//| Check Volume Value                                                                 |
//+------------------------------------------------------------------+
double CheckVolumeValue(double check)
  {
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(check<min_volume)
      return(min_volume);
   double max_lots=MaxLots;
   if(check>max_lots)
      return(MaxLots);
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(check>max_volume)
      return(max_volume);
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(check/volume_step);
   if(MathAbs(ratio*volume_step-check)>0.0000001)
      return(ratio*volume_step);
   return(check);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
  }
//+------------------------------------------------------------------+
//|     Signal                                                             |
//+------------------------------------------------------------------+
int Signal_1()
  {
   double Force=0,
          Close_1=iClose(Symbol(),0,1),
          Open_1=iOpen(Symbol(),0,1);
   double Day_1=iClose(NULL,TimeFrames,1);
   if(Day_1!=0)
      Force=((Bid-Day_1)/Day_1)*10000;
   if(Close_1>Open_1 && Force>percent)
      return(2);
   if(Close_1<Open_1 && Force<-percent)
      return(1);
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void trend_main()
  {
  }
//+------------------------------------------------------------------+
//|     Next positions                                                             |
//+------------------------------------------------------------------+
bool nx_77(string ot_456)
  {
   double dl_12=0;
   bool nx_77=false;
   dl_12=PipsStep*gpoint_320;
   if(cou_tra_45(ot_456)>=MaxTrades)
      return false;
   dl_12=PipsStep*gpoint_320;
   if((control_price(ot_456)>=dl_12))
     {
      nx_77=TRUE;
     }
   return(nx_77);
  }
//bool CheckNextOpen(string ot_456)
//{
//return(a_0);
// }

//+------------------------------------------------------------------+
//|   Control price                                                               |
//+------------------------------------------------------------------+
double control_price(string ot_456)
  {
   double a_0=0;
   if(ot_456=="buy")
     {
      a_0=nor_1(select(ot_456,"price")-Ask);
     }
   if(ot_456=="sell")
     {
      a_0=nor_1(Bid-select(ot_456,"price"));
     }
   return(a_0);
  }
//+------------------------------------------------------------------+
//|  Lots for first and next position                                |
//+------------------------------------------------------------------+
double ne_234(string ot_456)
  {
   double lo_45=0;
   if(ot_456=="buy")
     {
      if(cou_tra_45(ot_456)>0)
         lo_45=(double)DoubleToStr(Lots*MathPow(power,cou_tra_45(ot_456)),Lots_Digits());
      else
         lo_45=NormalizeDouble(Lots,Lots_Digits());
     }
   if(ot_456=="sell")
     {
      if(cou_tra_45(ot_456)>0)
         lo_45=(double)DoubleToStr(Lots*MathPow(power,cou_tra_45(ot_456)),Lots_Digits());
      else
         lo_45=NormalizeDouble(Lots,Lots_Digits());
     }
   return(lo_45);
  }
//double multiplier(string ot_456)
// {
// }

//+------------------------------------------------------------------+
//|   Check stop loss and take profit                                                               |
//+------------------------------------------------------------------+
bool check_sl_tp(ENUM_ORDER_TYPE type,double sl,double tp)
  {
   int stops_level=(int)SymbolInfoInteger(Symbol(),SYMBOL_TRADE_STOPS_LEVEL);
   bool SL_check=false,TP_check=false;
   switch(type)
     {
      case  ORDER_TYPE_BUY:
        {
         SL_check=((Bid-sl>stops_level*gpoint_320) || sl==0);
         TP_check=((tp-Bid>stops_level*gpoint_320) || tp==0);
         return(SL_check && TP_check);
        }
      case  ORDER_TYPE_SELL:
        {
         SL_check=((sl-Ask>stops_level*gpoint_320) || sl==0);
         TP_check=((Ask-tp>stops_level*gpoint_320)|| tp==0);
         return(TP_check && SL_check);
        }
      break;
     }
   return false;
  }
//+------------------------------------------------------------------+
//|    Check Money                                                              |
//+------------------------------------------------------------------+
bool CheckMoney(double lots,int type)
  {
   string text;
   double margin=AccountFreeMarginCheck(Symbol(),type,lots);
   if(margin<0)
     {
      switch(type)
        {
         case 0:
            text="Buy";
            break;
         case 1:
            text="Sell";
            break;
         default:
            text="Order";
        }
      Print("Not enough money for ",text," ",lots," Error code=",GetLastError());
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|    Order Allowed                                                              |
//+------------------------------------------------------------------+
bool OrderAllowed()
  {
   int max_orders=(int)AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);
   if(max_orders==0)
      return(true);
   int orders_t=OrdersTotal();
   return(orders_t<max_orders);
  }
//+------------------------------------------------------------------+
//|  Count orders                                                                |
//+------------------------------------------------------------------+
int cou_tra_45(string ot_456)
  {
   int count=0,tr_45,oldticketnumber=0;
   for(tr_45=OrdersTotal()-1; tr_45>=0; tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSellect cou_tra_45 . Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(ot_456=="buy")
           {
            if(OrderType()==OP_BUY)
              {
               count++;
              }
           }
         if(ot_456=="sell")
           {
            if(OrderType()==OP_SELL)
              {
               count++;
              }
           }
        }
     }
   return(count);
  }
//+------------------------------------------------------------------+
//|  Choose what you need                                            |
//+------------------------------------------------------------------+
double select(string ot_456,string inf)
  {
   RefreshRates();
   double gd_58=0.0;
   int tr_45,oldticketnumber=0,n=0;
   for(tr_45=OrdersTotal()-1; tr_45>=0; tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSelect select(). Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(ot_456=="buy")
           {
            if(OrderType()==OP_BUY)
              {
               if(OrderTicket()>oldticketnumber)
                 {
                  gd_58=OrderOpenPrice();
                  oldticketnumber=OrderTicket();
                 }
              }
           }
         if(ot_456=="sell")
           {
            if(OrderType()==OP_SELL)
              {
               if(OrderTicket()>oldticketnumber)
                 {
                  gd_58=OrderOpenPrice();
                  oldticketnumber=OrderTicket();
                 }
              }
           }
        }
     }
   if(inf=="price")
      return(gd_58);
   return(0);
  }
//bool CloseAllOrders(int a_0,int a_1)
//{
// return(true);
// }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
  }





//+------------------------------------------------------------------+
