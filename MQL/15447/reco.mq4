//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2012, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
#property copyright "eevviill"
#property link "http://alievtm.blogspot.com/"
#property version "2.2"
#property strict
#property description "M1?"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum po_koefs
  {
   _1=1,/*1*/  _10=10,/*10*/ _100=100,/*100*/ _1000=1000,/*1000*/
  };

extern string oth_set = "///////////////Other settings///////////////////";
extern int Max_orders = 0;
extern int Slippage=0;
extern int Magic=456;
extern string comment="Reco";
extern po_koefs point_multiplier=_10;

extern string sig_set = "///////////////Signal settings///////////////////";
extern int RSI_period = 14;
extern double RSI_sell_zone= 70;
extern double RSI_buy_zone = 30;

extern string lot_set="///////////////Lot settings///////////////////";
extern double Lot=0.01;
extern double Lot_multiplier=2;
extern double max_lot = 0;
extern double min_lot = 0;

extern string dis_set="///////////////Distance settings///////////////////";
extern int start_distance=20;
extern double distance_multiplier=1.5;
extern int max_distance = 0;
extern int min_distance = 0;

extern string prof_set="///////////////Profit/Lose settings///////////////////";
extern bool use_close_profit = true;
extern double profit_1_order = 2;
extern double profit_multiplier=0.7;
extern bool use_close_lose = false;
extern double lose_1_order = 6;
extern double lose_multiplier=1.1;




int Sig_p;
int buys,sells,Orders_Total;
double point=Point*point_multiplier;
int nor_lot=2;
//////////////////////////////////////////////////////////////
int OnInit()
  {

   if(MarketInfo(Symbol(),MODE_LOTSTEP)==0.1) nor_lot=1;



   return(INIT_SUCCEEDED);
  }
////////////////////////////////////
void OnTick()
  {
//close profit
   if(use_close_profit)
     {
      CountOpenedPositions_f();
      if(Profit_f()>=profit_1_order*MathPow(profit_multiplier,Orders_Total-1)) Close_all_f();
     }

//close lose
   if(use_close_lose)
     {
      CountOpenedPositions_f();
      if(Profit_f()<=-(lose_1_order*MathPow(lose_multiplier,Orders_Total-1))) Close_all_f();
     }

//open
   Sig_p=Sig_f();
   if(Sig_p!=0) open_f();

  }
//func
/////////////////////////////////////////////////////////////////////////////////////////////
char Sig_f()
  {
   CountOpenedPositions_f();

   if(Orders_Total==0)
     {
      double rsi=iRSI(Symbol(),0,RSI_period,PRICE_CLOSE,0);
      if(rsi>=RSI_sell_zone) return(-1);
      if(rsi<=RSI_buy_zone) return(1);
     }
   else
     {
      if(Max_orders>0 && Orders_Total>=Max_orders) return(0);

      Select_last_order_f();
      //last buy
      if(OrderType()==OP_BUY)
        {
         double dist=start_distance*point*MathPow(distance_multiplier,Orders_Total-1);
         if(max_distance>0 && dist>max_distance*point) dist=max_distance*point;
         if(min_distance>0 && dist<min_distance*point) dist=min_distance*point;

         if(Bid<=OrderOpenPrice()-dist) return(-1);
        }
      //last sell
      if(OrderType()==OP_SELL)
        {
         double dist=start_distance*point*MathPow(distance_multiplier,Orders_Total-1);
         if(max_distance>0 && dist>max_distance*point) dist=max_distance*point;
         if(min_distance>0 && dist<min_distance*point) dist=min_distance*point;

         if(Ask>=OrderOpenPrice()+dist) return(1);
        }
     }//end else


   return(0);
  }
///////////////////////////////////////////////////////////////////////////////////////////////
void open_f()
  {
///////////// LOT /////////////////
   double Lotss=Lot;
   CountOpenedPositions_f();
   Lotss=Lot*MathPow(Lot_multiplier,Orders_Total);
//user limit
   if(max_lot>0 && Lotss>max_lot) Lotss=max_lot;
   if(min_lot>0 && Lotss<min_lot) Lotss=min_lot;
//broker limit
   double Min_Lot =MarketInfo(Symbol(),MODE_MINLOT);
   double Max_Lot =MarketInfo(Symbol(),MODE_MAXLOT);
   if(Lotss<Min_Lot) Lotss=Min_Lot;
   if(Lotss>Max_Lot) Lotss=Max_Lot;

//chek free margin
   if(MarketInfo(Symbol(),MODE_MARGINREQUIRED)*Lotss>AccountFreeMargin()) {Alert("Not enouth money to open order "+string(Lotss)+" lots!");return;}



///////////// MAIN /////////////  
   int ticket_op=-1;
   for(int j_op = 0; j_op < 64; j_op++)
     {
      while(IsTradeContextBusy()) Sleep(200);
      RefreshRates();

      if(Sig_p>0) ticket_op=OrderSend(Symbol(),OP_BUY,NormalizeDouble(Lotss,nor_lot),Ask,Slippage,0,0,comment,Magic,0,clrNONE);
      if(Sig_p<0) ticket_op=OrderSend(Symbol(),OP_SELL,NormalizeDouble(Lotss,nor_lot),Bid,Slippage,0,0,comment,Magic,0,clrNONE);

      if(ticket_op>-1)break;
     }


  }
////////////////////////////////////////////////////////////////////////////////////
void CountOpenedPositions_f()
  {
   buys=0;
   sells=0;
   Orders_Total=0;

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)      buys++;
               if(OrderType()==OP_SELL)     sells++;
              }
           }
        }
     }

   Orders_Total=buys+sells;
  }
////////////////////////////////////////////////////////////////////
void Select_last_order_f()
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               break;
              }
           }
        }
     }

  }
/////////////////////////////////////////////////////////////////////////////////// 
double Profit_f()
  {
   double prof=0;

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               prof+=OrderProfit()+OrderSwap()+OrderCommission();
              }
           }
        }
     }

   return(prof);
  }
////////////////////////////////////////////////////////////////////////////////
void Close_all_f()
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               bool ticket_ex=false;
               for(int j_ex=0;j_ex<64; j_ex++)
                 {
                  while(IsTradeContextBusy()) Sleep(200);
                  RefreshRates();

                  if(OrderType()==OP_BUY) ticket_ex=OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrYellow);
                  else
                     if(OrderType()==OP_SELL) ticket_ex=OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrYellow);
                  else
                     if(OrderType()==OP_SELLSTOP || OrderType()==OP_BUYSTOP || OrderType()==OP_SELLLIMIT || OrderType()==OP_BUYLIMIT) ticket_ex=OrderDelete(OrderTicket(),clrBrown);
                  else
                     break;
                  if(ticket_ex==true)break;
                 }
              }
           }
        }
     }

  } 
//+------------------------------------------------------------------+
