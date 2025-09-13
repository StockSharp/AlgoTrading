//+------------------------------------------------------------------+
//|                                                    Virtual Robot |
//|                                           Copyright 2019, Stiopa |
//|                           "https://www.mql5.com/en/users/stiopa" |
//+------------------------------------------------------------------+
#property      copyright "Stiopa"
#property      link      "https://www.mql5.com/en/users/stiopa"
#property      version   "1.00"
#property      strict
#include       <arrays/list.mqh>
#include       <chartobjects/chartobjectslines.mqh>                   
input          ENUM_TIMEFRAMES TF=60;//TF  
input int      slippage=1;//Slippage
input int      MagicNumber=1;//Magic Number
input string   comment="Virtual Robot";//Comment
input ushort   SL=400;//Stop Loss
input ushort   TP=100;//Take Profit
input ushort   min_tp=15;//Min Take Profit
extern ushort  TP_1=10;//Average + Take Profit  
extern double  Lots=0.01;//Lots
extern double  MaxLots=2;//Max Lots
extern double  Multiplier=1.5;//Multiplier  
input uchar    Stepper=2;//Stepper for real
input uchar    Stepper_v=2;//Stepper for virtual
extern double  Pipstep=22;//PipStep
extern uchar   MaxTrades=16;//MaxTrades
input uchar    starting_real_orders=6;//Start opening real trades
input uchar    real_average=2;//Start average orders price for real orders
input bool     visual=true;//Visual
int            Tick,di_2,pr_60,n_virt_buy=0,n_virt_sell=0;
double         gpoint_320,
mn_digits,
pips_buy[100],pips_sell[100],lots_buy[100],lots_sell[100],
virt_orders_0[102],
virt_orders_1[102],
virtual_close_buy[2],
virtual_close_sell[2],
virt_lots_0[102],
virt_lots_1[102],
PipStep;
datetime       tim_0=0;
bool           buy_start=false,sell_start=false;
string         Error;
//+------------------------------------------------------------------+
//|   For Broker 4 or 5 digits                                       |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(Digits==3 || Digits==5)
     {
      di_2=10;
      gpoint_320=Point*10;
      mn_digits=0.1;
        }else{
      di_2=1;
      gpoint_320=Point;
      mn_digits=0.1;
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Horizontal Line                                                  |
//+------------------------------------------------------------------+
class HLine : public CChartObjectHLine
  {
public:
                     HLine(string st,double price1,color clr,ENUM_LINE_STYLE style_0=0)
     {
      Create(0,st,0,price1);
      Color(clr);
      Style(style_0);
     }
  };
CList list;
//+------------------------------------------------------------------+
//|   On Tick start expert                                           |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(Bars<100)
      return;
//------------------------------------------------
   virt_average_tp("buy");
   virt_average_tp("sell");
   if(timenex()==1)
     {
      PipStep=Pipstep*gpoint_320;
      virtual_orders();
      virtual_tp();
      if(count_45("buy")==0)
        {
         ObjectDelete("Vtp");
         if(buy_start)
           {
            virtual_del("buy");
            buy_start=false;
           }
        }
      if((n_virt_buy>=starting_real_orders && count_45("buy")==0) || (count_45("buy")>0 && nx_open("buy")))
        {
         if(CheckMoney(l_ots("buy"),OP_BUY) && OrderAllowed())
           {
            if(!volume_check(l_ots("buy"))){Print(Symbol()," volume_check buy ",Error);}else
            if(AccountFreeMarginCheck(Symbol(),OP_BUY,l_ots("buy"))<=0.0 || _LastError==ERR_NOT_ENOUGH_MONEY)
              {
               Print("Check sell: ",GetLastError());}else{
               Tick=0;
               while(Tick<1)
                 {
                  Tick=OrderSend(Symbol(),OP_BUY,l_ots("buy"),no_5(Ask),slippage,0,0,comment,MagicNumber,0,Blue);
                  if(Tick<1)
                    {
                     Print("Error: ",GetLastError());Sleep(1000);RefreshRates();}else{
                     modify("buy");
                     buy_start=true;
                    }
                 }
              }
           }
        }
      if(count_45("sell")==0)
        {
         ObjectDelete("Vtp2");
         if(sell_start)
           {
            virtual_del("sell");
            sell_start=false;
           }
        }
      if((n_virt_sell>=starting_real_orders && count_45("sell")==0) || (count_45("sell")>0 && nx_open("sell")))
        {
         if(CheckMoney(l_ots("sell"),OP_SELL) && OrderAllowed())
           {
            if(!volume_check(l_ots("sell"))){Print(Symbol()," volume_check sell ",Error);}else
            if(AccountFreeMarginCheck(Symbol(),OP_SELL,l_ots("sell"))<=0.0 || _LastError==ERR_NOT_ENOUGH_MONEY)
              {
               Print("Check sell: ",GetLastError());}else{
               Tick=0;
               while(Tick<1)
                 {

                  Tick=OrderSend(Symbol(),OP_SELL,l_ots("sell"),no_5(Bid),slippage,0,0,comment,MagicNumber,0,Red);
                  if(Tick<1)
                    {
                     Print("Error: ",GetLastError());Sleep(1000);RefreshRates();}else{
                     modify("sell");
                     sell_start=true;
                    }
                 }
              }
           }
        }
      tim_0=iTime(Symbol(),pr_60,0);
     }
  }
//+------------------------------------------------------------------+
//|    Next open position                                            |
//+------------------------------------------------------------------+
bool nx_open(string ot_456)
  {
   bool nx_77=false;
   if(ot_456=="buy")
     {
      if(control_price(ot_456)>=PipStep && count_45(ot_456)<MaxTrades)
        {
         nx_77=TRUE;
        }
     }
   if(ot_456=="sell")
     {
      if(control_price(ot_456)>=PipStep && count_45(ot_456)<MaxTrades)
        {
         nx_77=TRUE;
        }
     }
   return(nx_77);
  }
//+------------------------------------------------------------------+
//|   Control price                                                  |
//+------------------------------------------------------------------+
double control_price(string ot_456)
  {
   double a_0=0;
   if(ot_456=="buy")
     {
      a_0=no_5(select(ot_456,"lastopen")-Ask);
     }
   if(ot_456=="sell")
     {
      a_0=no_5(Bid-select(ot_456,"lastopen"));
     }
   return(a_0);
  }
//+------------------------------------------------------------------+
//|  Multiplier                                                      |
//+------------------------------------------------------------------+
double l_ots(string ot_456)
  {
   double close_mn,
   lo_45=0,
   mi_122=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN),
   Lotstep=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int    LotsDigits=(int)MathCeil(MathAbs(MathLog(Lotstep)/MathLog(10)));
   if(ot_456=="buy")
     {
      close_mn=control_price(ot_456)/PipStep;
      if(count_45(ot_456)>=Stepper)
         lo_45=(double)DoubleToStr(select(ot_456,"lastlots")*(Multiplier*close_mn),LotsDigits);
      else
         lo_45=NormalizeDouble(Lots,LotsDigits);
     }
   if(ot_456=="sell")
     {
      close_mn=control_price(ot_456)/PipStep;
      if(count_45(ot_456)>=Stepper)
         lo_45=(double)DoubleToStr(select(ot_456,"lastlots")*(Multiplier*close_mn),LotsDigits);
      else
         lo_45=NormalizeDouble(Lots,LotsDigits);
     }
   if(lo_45>MaxLots) lo_45=MaxLots;
   if(lo_45<mi_122) lo_45=mi_122;
   if(lo_45>CalcLots() && CalcLots()>0)lo_45=NormalizeDouble(CalcLots(),LotsDigits);
   return(lo_45);
  }
//+------------------------------------------------------------------+
//|   Virtual Orders                                                 |
//+------------------------------------------------------------------+
void virtual_orders()
  {
   double Lotstep=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int    LotsDigits=(int)MathCeil(MathAbs(MathLog(Lotstep)/MathLog(10)));
   double Close_1=iClose(Symbol(),pr_60,1),
   Open_1=iOpen(Symbol(),pr_60,1);
   if((Close_1>Open_1 && n_virt_buy==0) || (n_virt_buy>0 && virt_orders_0[n_virt_buy]-Ask>PipStep))
     {
      n_virt_buy++;
      ObjectDelete("buy_virtual_tp");
      ObjectDelete("buy_virtual_tp2");
      virt_orders_0[n_virt_buy]=Ask;
      if(n_virt_buy<=Stepper_v)
         virt_lots_0[n_virt_buy]=Lots;
      else
         virt_lots_0[n_virt_buy]=NormalizeDouble(virt_lots_0[n_virt_buy-1]*Multiplier,LotsDigits);
      if(visual)
        {
         list.Add(new HLine("buy_virtual_line #"+(string)n_virt_buy+"/"+(string)virt_lots_0[n_virt_buy],virt_orders_0[n_virt_buy],Green,3));
         if(n_virt_buy==1 && TP>0)
            list.Add(new HLine("buy_virtual_tp",virt_orders_0[1]+TP*gpoint_320,Red,3));
        }
      Print("#Buy ",virt_lots_0[n_virt_buy]);
     }
   if((Close_1<Open_1 && n_virt_sell==0) || (n_virt_sell>0 && Bid-virt_orders_1[n_virt_sell]>PipStep && Bid>pips_sell[n_virt_sell]))
     {
      n_virt_sell++;
      ObjectDelete("sell_virtual_tp");
      ObjectDelete("sell_virtual_tp2");
      virt_orders_1[n_virt_sell]=Bid;
      if(n_virt_sell<=Stepper_v)
         virt_lots_1[n_virt_sell]=Lots;
      else
         virt_lots_1[n_virt_sell]=NormalizeDouble(virt_lots_1[n_virt_sell-1]*Multiplier,LotsDigits);
      if(visual)
        {
         list.Add(new HLine("sell_virtual_line#"+(string)n_virt_sell+"/"+(string)virt_lots_1[n_virt_sell],virt_orders_1[n_virt_sell],Green,3));
         if(n_virt_sell==1 && TP>0)
            list.Add(new HLine("sell_virtual_tp",virt_orders_1[1]-TP*gpoint_320,Red,3));
        }
      Print("#Sell ",virt_lots_1[n_virt_sell]);
     }
  }
//+------------------------------------------------------------------+
//|  Virtual Objects Delete All                                      |
//+------------------------------------------------------------------+
void virtual_del(string ot_456)
  {
   for(int x=0;x<=100;x++)
     {
      if(ot_456=="buy")
        {
         if(count_45(ot_456)==0)
           {
            virt_orders_0[x]=0;
            virt_lots_0[x]=0;
            n_virt_buy=0;
            virtual_close_buy[0]=0;
            ObjectsDeleteAll(0,"buy_virtual",0,OBJ_HLINE);
           }
        }
      if(ot_456=="sell")
        {
         if(count_45(ot_456)==0)
           {
            virt_orders_1[x]=0;
            virt_lots_1[x]=0;
            n_virt_sell=0;
            virtual_close_sell[0]=0;
            ObjectsDeleteAll(0,"sell_virtual",0,OBJ_HLINE);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|   Virtual Take Profit                                            |
//+------------------------------------------------------------------+
void virtual_tp()
  {
   if(no_5(Ask-virt_orders_0[1])>min_tp*gpoint_320 && n_virt_buy==1)
     {
      virtual_del("buy");
      CloseAllOrders(0);
     }
   if(no_5(virt_orders_1[1])-Bid>min_tp*gpoint_320 && n_virt_sell==1)
     {
      virtual_del("sell");
      CloseAllOrders(1);
     }
  }
//+------------------------------------------------------------------+
//|  Virtual Average Order Price + Take Profit                       |
//+------------------------------------------------------------------+
void virt_average_tp(string ot_456)
  {
   if(ot_456=="buy")
     {
      if(n_virt_buy>1)
        {
         if(visual)
            list.Add(new HLine("buy_virtual_tp2",virt_average(ot_456)+TP_1*gpoint_320,Red,3));
         if(Bid>virt_average(ot_456)+TP_1*gpoint_320)
           {
            virtual_del(ot_456);
            CloseAllOrders(0);
           }
        }
     }
   if(ot_456=="sell")
     {
      if(n_virt_sell>1)
        {
         if(visual)
            list.Add(new HLine("sell_virtual_tp2",virt_average(ot_456)-TP_1*gpoint_320,Red,3));
         if(Ask<virt_average(ot_456)-TP_1*gpoint_320)
           {
            virtual_del(ot_456);
            CloseAllOrders(1);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Virtual Average Order Price                                      |
//+------------------------------------------------------------------+
double virt_average(string ot_456)
  {
   double avp_2=0,
   Count=0;
   for(int x=n_virt_buy+n_virt_sell;x>=1;x--)
     {
      if(ot_456=="buy")
        {
         avp_2=avp_2+virt_orders_0[x]*virt_lots_0[x];
         Count=Count+virt_lots_0[x];
        }
      if(ot_456=="sell")
        {
         avp_2=avp_2+virt_orders_1[x]*virt_lots_1[x];
         Count=Count+virt_lots_1[x];
        }
     }
   avp_2=NormalizeDouble(avp_2/Count,Digits);
   return(avp_2);
  }
//+------------------------------------------------------------------+
//| Calculation of the size of the lots                              |
//+------------------------------------------------------------------+
double CalcLots()
  {
   double Lotstep=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   double Lots_Max=MathFloor((AccountFreeMargin()*99)/100/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/Lotstep)*Lotstep;
   return (Lots_Max);
  }
//+------------------------------------------------------------------+
//|   Modify Take Profit and Stop Loss                               |
//+------------------------------------------------------------------+
void modify(string ot_456)
  {
   int c_0=count_45(ot_456);
   int TP1=0,TP2=0;
   double last_price=select(ot_456,"lastopen");
   double tp=0,av_0=0,sl=0,av_2=0,tp_0;
   tp=TP_1;
   if(count_45(ot_456)>1)
     {
      if(count_45(ot_456)>=real_average)
         av_2=avp_next(ot_456);
      else
         av_2=virt_average(ot_456);
     }
   for(int tr_45=OrdersTotal()-1;tr_45>=0;tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSelect modify() Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(ot_456=="buy")
           {
            if(OrderType()==OP_BUY)
              {
               if(c_0>1)
                 {
                  tp_0=no_5(av_2+tp*gpoint_320);
                  av_0=av_2;
                    }else{
                  if(TP!=0)
                     tp_0=no_5(OrderOpenPrice()+st_tpsl(TP*gpoint_320));
                  else
                     tp_0=0;
                  av_0=Ask;
                 }
               if(SL!=0)
                  sl=no_5(last_price-st_tpsl(SL*gpoint_320));
               else
                  sl=OrderStopLoss();
               if(OrderTakeProfit()!=tp_0)
                 {
                  if(check_sl_tp(0,sl,tp_0))
                     if(OrderModify(OrderTicket(),av_0,sl,tp_0,0,Yellow)==False)
                        Print("Error in OrderModify buy Average + TP2. Error code=",GetLastError());
                  ObjectDelete("buy_virtual_tp2");
                 }
              }
           }
         if(ot_456=="sell")
           {
            if(OrderType()==OP_SELL)
              {
               if(c_0>1)
                 {
                  tp_0=no_5(av_2-tp*gpoint_320);
                  av_0=av_2;
                    }else{
                  if(TP!=0)
                     tp_0=no_5(OrderOpenPrice()-st_tpsl(TP*gpoint_320));
                  else
                     tp_0=0;
                  av_0=Bid;
                 }
               if(SL!=0)
                  sl=no_5(last_price+st_tpsl(SL*gpoint_320));
               else
                  sl=OrderStopLoss();
               if(OrderTakeProfit()!=tp_0)
                 {
                  if(check_sl_tp(1,sl,tp_0))
                     if(OrderModify(OrderTicket(),av_0,sl,tp_0,0,Yellow)==False)
                        Print("Error in OrderModify sell Average - TP2. Error code=",GetLastError());
                  ObjectDelete("sell_virtual_tp2");
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|   Volume Check                                                   |
//+------------------------------------------------------------------+
bool volume_check(double volume)
  {
   double min,max,step;
   min=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min)
     {
      Error=StringConcatenate("Volume less than the minimum allowed. The minimum volume is ",min);
      return(false);
     }
   max=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max)
     {
      Error=StringConcatenate("Volume greater than the maximum allowed. The maximum volume is ",max);
      return(false);
     }
   step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int r=(int)MathRound(volume/step);
   if(MathAbs(r*step-volume)>0.0000001)
     {
      Error=StringConcatenate("The volume is not multiple of the minimum gradation ",step,". Volume closest to the valid ",r*step);
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//| Check Money                                                      |
//+------------------------------------------------------------------+
bool CheckMoney(double lots,int type)
  {
   string text;
   double margin=AccountFreeMarginCheck(Symbol(),type,lots);
   if(margin<0)
     {
      switch(type)
        {
         case 0:text="Buy";break;
         case 1:text="Sell";break;
         default:text="Order";
        }
      Print("Not enough money for ",text," ",lots," Error code=",GetLastError());
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//| Order Allowed                                                    |
//+------------------------------------------------------------------+
bool OrderAllowed()
  {
   int max_orders=(int)AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);
   if(max_orders==0) return(true);
   int orders_t=OrdersTotal();
   return(orders_t<max_orders);
  }
//+-------------------------------------------------------------------------------+
//|Minimal indention in points from the current close price to place Stop orders  |
//+-------------------------------------------------------------------------------+
bool check_sl_tp(ENUM_ORDER_TYPE type,double sl,double tp)
  {
   int stops_level=(int)SymbolInfoInteger(Symbol(),SYMBOL_TRADE_STOPS_LEVEL);
   bool SL_check=false,TP_check=false;
   if(type==0)
     {
      SL_check=(Bid-sl>stops_level*gpoint_320);
      TP_check=(tp-Bid>stops_level*gpoint_320);
      return(SL_check && TP_check);
     }
   if(type==1)
     {
      SL_check=(sl-Ask>stops_level*gpoint_320);
      TP_check=(Ask-tp>stops_level*gpoint_320);
      return(TP_check && SL_check);
     }
   return false;
  }
//+------------------------------------------------------------------+
//|Normalize Double                                                  |
//+------------------------------------------------------------------+
double no_5(double a_0)
  {
   return (NormalizeDouble(a_0, Digits));
  }
//+------------------------------------------------------------------+
//| Entry  logic operates on Bar Close only                          |
//+------------------------------------------------------------------+
int timenex()
  {
   if(tim_0!=iTime(Symbol(),pr_60,0))return (1);
   return (0);
  }
//+------------------------------------------------------------------+
//| Check MODE STOP LEVEL                                            |
//+------------------------------------------------------------------+
double st_tpsl(double a_0)
  {
   RefreshRates();
   double StopLevel=MarketInfo(Symbol(),MODE_STOPLEVEL);
   if(a_0<StopLevel*gpoint_320)a_0=StopLevel*gpoint_320;
   return(a_0);
  }
//+------------------------------------------------------------------+
//|    Count                                                         |
//+------------------------------------------------------------------+
int count_45(string ot_456)
  {
   int count=0,oldticketnumber=0;
   for(int tr_45=OrdersTotal()-1;tr_45>=0;tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSellect count_45 . Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
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
//| Average Order Price                                              |
//+------------------------------------------------------------------+
double avp_next(string ot_456)
  {
   double
   avp_2=0,
   Count=0;
   for(int tr_45=OrdersTotal()-1;tr_45>=0;tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSelect avp_2. Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
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
   avp_2=NormalizeDouble(avp_2/Count,Digits);
   return(avp_2);
  }
//+------------------------------------------------------------------+
//| Choose what you need                                             |
//+------------------------------------------------------------------+
double select(string ot_456,string inf)
  {
   double last_open=0,last_lots=0;
   int oldticketnumber=0,n=0;
   for(int tr_45=OrdersTotal()-1;tr_45>=0;tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS,MODE_TRADES)==False)
         Print("Error in OrderSelect select(). Error code=",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
        {
         if(ot_456=="buy")
           {
            if(OrderType()==OP_BUY)
              {
               if(OrderTicket()>oldticketnumber)
                 {
                  last_open=OrderOpenPrice();
                  last_lots=OrderLots();
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
                  last_open=OrderOpenPrice();
                  last_lots=OrderLots();
                  oldticketnumber=OrderTicket();
                 }
              }
           }
        }
     }
   if(inf=="lastopen")      return(last_open);
   if(inf=="lastlots")      return(last_lots);
   return(0);
  }
//+------------------------------------------------------------------+
//|Close All Orders                                                  |
//+------------------------------------------------------------------+
bool CloseAllOrders(int a_0)
  {
   int OT;
   bool OC=true;
   for(int tr_45=OrdersTotal()-1; tr_45>=0; tr_45--)
     {
      if(OrderSelect(tr_45,SELECT_BY_POS))
        {
         if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            OT=OrderType();
            if(OT!=a_0) continue;
            if(OT==OP_BUY)
              {
               OC=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),slippage,Blue);
               if(OC){Print(Symbol(),"Closing  transaction",OrderLots());}
              }
            if(OT==OP_SELL)
              {
               OC=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),slippage,Red);
               if(OC){Print(Symbol(),"Closing  transaction",OrderLots());}
              }
           }
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
