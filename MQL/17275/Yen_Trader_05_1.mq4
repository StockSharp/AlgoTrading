//+------------------------------------------------------------------+
//|                                              Yen_Trader_05_1.mq4 |
//|                                  Copyright 2016, Khalil Abokwaik |
//|                             http://www.forexfactory.com/abokwaik |
//|                           https://www.mql5.com/en/users/abokwaik |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2016, Khalil Abokwaik"
#property link          "http://www.forexfactory.com/abokwaik"
#property description   "Yen Trader Expert Advisor"
#property description   "by Khalil Abokwaik ( abokwaik@yahoo.com )"
#property version       "5.10"
#property strict
#include <stderror.mqh> 
#include <stdlib.mqh> 
//--- custom types -----------------------------------------------------------
enum enum_entries
  {
   ENTRY_PYRAMID=1,//Pyramiding
   ENTRY_AVERAGE=2,//Averaging
   ENTRY_BOTH=3//Both
  };
//--
enum enum_price_type
  {
   TPRICE_CLOSE=1,//Close Price of Previous Bars 
   TPRICE_HL=2//High/Low Price of Previous Bars 
  };
//--- input parameters -------------------------------------------------------
input int      Magic_Number         =160704;          //Magic Number
input ENUM_TIMEFRAMES time_frame    =PERIOD_H1;       //Signal Time Frame
input string   pair_setup           ="--------------------";           //Pair Setup
input string   Major_Code           ="GBPUSD";        //Major Pair Code
input string   UJ_Code              ="USDJPY";        //DollarYen Pair Code
input string   JPY_Cross            ="GBPJPY";        //Yen Cross Pair Code
input string   major_pos            ="L";             //Major Direction Left/Right 
input string   trade_setup           ="--------------------";           //Trade Setup
input double   Fixed_Lot_Size       =0;               //Fixed Lots (set to 0 enable variable lots)
input double   Bal_Perc_Lot_Size    =1;               //Variable Lots as % of Balance
input int      SL_Pips              =1000;            //Stop Loss (Pips or Points)
input int      TP_Pips              =5000;            //Take Profit (Pips or Points)
input int      BE_Pips              =200;             //Break Even , 0 to disable (Pips or Points)
input int      PL_Pips              =200;             //Profit Lock , 0 to disable (Pips or Points)
input int      Trail_Stop_Pips      =200;             //Trailing Stop, 0 to disable (Pips or Points)
input int      Trail_Stop_Jump_Pips =10;              //Trail Stop Shift (Pips or Points)
input enum_entries entry_type       =ENTRY_BOTH;      //Entry Type when Entry TF < Signal TF
input string   Indicators           ="--------------------";           //Signal Multiple Indicators
input int      loop_back_bars       =2;               //Loop Back Bars (0 to disable)
input enum_price_type price_type    = TPRICE_HL;      //Price Type of Loop Back Bars
input bool     RSI                  =true;            //Relative String Index (RSI)
input bool     RVI                  =true;            //Relative Vigor Index (RVI)
input bool     CCI                  =true;            //Commodity Channel Index (CCI)
input int      MA_Period            =34;              //Moving Average Period (0 to disable)
input ENUM_MA_METHOD MA_Method      =MODE_SMMA;       //Moving Average Method
input string   trade_conditions     ="--------------------";           //Trade Conditions
input int      max_spread           =100;             //Max Spread
input int      max_slippage         =10;              //Max Slippage
input int      max_orders           =10;              //Max Open Trades
input bool     ECN                  =false;           //ECN Account
input bool     close_on_opposite    =false;           //Close on Opposite Signal
input bool     hedge_trades         =true;            //Hedge on Opposite Signal
input string   ATR_Levels           ="--------------------";           //ATR Setup
input bool     enable_atr           =false;           //Enable ATR-based levels (disabling pip levels)
input ENUM_TIMEFRAMES atr_tf        =PERIOD_D1;       //ATR Time Frame
input int      atr_period           =21;              //ATR Period
input double   ATR_SL               =2;               //Stop Loss ATR Multiplier
input double   ATR_TP               =4;               //Take Profit ATR Multiplier
input double   ATR_TS               =1;               //Trailing Stop ATR Multiplier
input double   ATR_BE               =0.5;             //Break Even ATR Multiplier
input double   ATR_PL               =2;               //Profit Lock ATR Multiplier

//--- variables ---------------------------------------------------------------------------------------
int      last_type=-1;
datetime bar_time=0,p_time=0;
int      ord_arr[100];
double   sell_price=0,buy_price=0,stop_loss=0,profit_target=0;
bool     OrderSelected=false,OrderDeleted=false,order_mod=false;
int      NewOrder=0;
int      oper_max_tries=3,tries=0;
double   sl_price=0,tp_price=0,Lot_Size=0,ord_price=0;
int      trail_stop_pips;
int      tkt_num=0;
int      trend_tf=0;
bool     buy=false,sell=false;
double   sma,macd,rsi,min_lot,atr;
int      lot_decimals=2;
double   maj_pips,uj_pips,maj_atr,uj_atr,ma;
string   maj_dir,uj_dir;
double   ind1,ind2,sig1,sig2;
int      err_num=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   min_lot=MarketInfo(Symbol(),MODE_MINLOT);
   if(min_lot==0.01) lot_decimals=2;
   if(min_lot==0.1) lot_decimals=1;
   if(min_lot==1) lot_decimals=0;

   if(invalid_pair(Major_Code))
     {
      Print("First Pair Code ("+Major_Code+") is invalid");
      return(INIT_PARAMETERS_INCORRECT);
     }
   if(invalid_pair(UJ_Code))
     {
      Print("Second Pair Code ("+UJ_Code+") is invalid");
      return(INIT_PARAMETERS_INCORRECT);
     }
   if(invalid_pair(JPY_Cross))
     {
      Print("Second Pair Code ("+JPY_Cross+") is invalid");
      return(INIT_PARAMETERS_INCORRECT);
     }
   if(time_frame<_Period)
     {
      Print("Invalid Input Signal Time Frame ("+IntegerToString(time_frame)+") is less than Trading Time Frame ("+IntegerToString(_Period)+")");
      return(INIT_PARAMETERS_INCORRECT);
     }
   if(BE_Pips>Trail_Stop_Pips && Trail_Stop_Pips>0)
     {
      Print("Break Even Pips ("+IntegerToString(BE_Pips)+") is greater than Trailing Stop ("+IntegerToString(Trail_Stop_Pips)+")");
      return(INIT_PARAMETERS_INCORRECT);
     }
   if(loop_back_bars==0 && MA_Period==0 && !RSI && !RVI && !CCI)
     {
      Print("Error : No Signal Triggers/Indicators Selected !");
      return(INIT_PARAMETERS_INCORRECT);
     }

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(BE_Pips>0) move_to_BE();
   if(PL_Pips>0) move_to_PL();
   if(Trail_Stop_Pips>0) trail_stop();
   if(Time[0]>bar_time)
     {
      if(total_orders()<max_orders)
        {
         if(loop_back_bars>1)
           {
            if(price_type==TPRICE_HL)
              {
               buy=
                   (
                   (major_pos=="L"
                   && iClose(Major_Code,time_frame,1)>iHigh(Major_Code,time_frame,iHighest(Major_Code,time_frame,MODE_HIGH,loop_back_bars,2))
                   && iClose(UJ_Code,time_frame,1)>iHigh(UJ_Code,time_frame,iHighest(UJ_Code,time_frame,MODE_HIGH,loop_back_bars,2))
                   )
                   || 
                   (major_pos=="R"
                   && iClose(Major_Code,time_frame,1)<iLow(Major_Code,time_frame,iLowest(Major_Code,time_frame,MODE_LOW,loop_back_bars,2))
                   && iClose(UJ_Code,time_frame,1)>iHigh(UJ_Code,time_frame,iHighest(UJ_Code,time_frame,MODE_HIGH,loop_back_bars,2))
                   )
                   );
               sell=
                   (
                   (major_pos=="L"
                   && iClose(Major_Code,time_frame,1)<iLow(Major_Code,time_frame,iLowest(Major_Code,time_frame,MODE_LOW,loop_back_bars,2))
                   && iClose(UJ_Code,time_frame,1)<iLow(UJ_Code,time_frame,iLowest(UJ_Code,time_frame,MODE_LOW,loop_back_bars,2))
                   )
                   || 
                   (major_pos=="R"
                   && iClose(Major_Code,time_frame,1)>iHigh(Major_Code,time_frame,iHighest(Major_Code,time_frame,MODE_HIGH,loop_back_bars,2))
                   && iClose(UJ_Code,time_frame,1)<iLow(UJ_Code,time_frame,iLowest(UJ_Code,time_frame,MODE_LOW,loop_back_bars,2))
                   )
                   );
                   }
            else
              {
               buy=
                   (
                   (major_pos=="L"
                   && iClose(Major_Code,time_frame,1)>iClose(Major_Code,time_frame,loop_back_bars)
                   && iClose(UJ_Code,time_frame,1)>iClose(UJ_Code,time_frame,loop_back_bars)
                   )
                   || 
                   (major_pos=="R"
                   && iClose(Major_Code,time_frame,1)<iClose(Major_Code,time_frame,loop_back_bars)
                   && iClose(UJ_Code,time_frame,1)>iClose(UJ_Code,time_frame,loop_back_bars)
                   )
                   );
               sell=
                   (
                   (major_pos=="L"
                   && iClose(Major_Code,time_frame,1)<iClose(Major_Code,time_frame,loop_back_bars)
                   && iClose(UJ_Code,time_frame,1)<iClose(UJ_Code,time_frame,loop_back_bars)
                   )
                   || 
                   (major_pos=="R"
                   && iClose(Major_Code,time_frame,1)>iClose(Major_Code,time_frame,loop_back_bars)
                   && iClose(UJ_Code,time_frame,1)<iClose(UJ_Code,time_frame,loop_back_bars)
                   )
                   );

                   }
           }
         else
           {
            buy=true;
            sell=true;
           }
         if(enable_atr) atr=iATR(Symbol(),atr_tf,atr_period,1);
         buy=buy && 
             (
             (
             (
             (entry_type==ENTRY_BOTH)
             || 
             (entry_type==ENTRY_AVERAGE && Close[1]<Open[1])
             || 
             (entry_type==ENTRY_PYRAMID && Close[1]>Open[1])
             )
             )
             );
         sell=sell && 
             (
             (
             (
             (entry_type==ENTRY_BOTH)
             || 
             (entry_type==ENTRY_AVERAGE && Close[1]>Open[1])
             || 
             (entry_type==ENTRY_PYRAMID && Close[1]<Open[1])
             )
             )
             );
             if(RSI)
           {
            ind1=iRSI(Major_Code,time_frame,14,PRICE_CLOSE,1);
            ind2=iRSI(UJ_Code,time_frame,14,PRICE_CLOSE,1);
            buy=buy && 
                (
                (ind1>50 && major_pos=="L")
                || 
                (ind1<50 && major_pos=="R")
                )
                && ind2>50;
            sell=sell && 
                 (
                 (ind1<50 && major_pos=="L")
                 || 
                 (ind1>50 && major_pos=="R")
                 )

                 && ind2<50;
           }
         if(CCI)
           {
            ind1=iCCI(Major_Code,time_frame,14,PRICE_TYPICAL,1);
            ind2=iCCI(UJ_Code,time_frame,14,PRICE_TYPICAL,1);
            buy=buy && 
                (
                (ind1>0 && major_pos=="L")
                || 
                (ind1<0 && major_pos=="R")
                )
                && ind2>0;
            sell=sell && 
                 (
                 (ind1<0 && major_pos=="L")
                 || 
                 (ind1>0 && major_pos=="R")
                 )

                 && ind2<0;
           }
         if(RVI)
           {
            ind1=iRVI(Major_Code,time_frame,10,MODE_MAIN,1);
            ind2=iRVI(UJ_Code,time_frame,10,MODE_MAIN,1);
            sig1=iRVI(Major_Code,time_frame,10,MODE_SIGNAL,1);
            sig2=iRVI(UJ_Code,time_frame,10,MODE_SIGNAL,1);
            buy=buy && 
                (
                (ind1>sig1 && major_pos=="L")
                || 
                (ind1<sig1 && major_pos=="R")
                )
                && ind2>sig2;
            sell=sell && 
                 (
                 (ind1<sig1 && major_pos=="L")
                 || 
                 (ind1>sig1 && major_pos=="R")
                 )
                 && ind2<sig2;
           }

         if(MA_Period>0)
           {
            ind1=iMA(Major_Code,time_frame,MA_Period,0,MA_Method,PRICE_CLOSE,1);
            ind2=iMA(UJ_Code,time_frame,MA_Period,0,MA_Method,PRICE_CLOSE,1);
            buy=buy && 
                (
                (iClose(Major_Code,time_frame,1)>ind1 && major_pos=="L")
                || 
                (iClose(Major_Code,time_frame,1)<ind1 && major_pos=="R")
                )
                && iClose(UJ_Code,time_frame,1)>ind2;
            sell=sell && 
                 (
                 (iClose(Major_Code,time_frame,1)<ind1 && major_pos=="L")
                 || 
                 (iClose(Major_Code,time_frame,1)>ind1 && major_pos=="R")
                 )

                 && iClose(UJ_Code,time_frame,1)<ind2;
           }
         if(buy)
           {
            if(close_on_opposite) close_current_orders(OP_SELL);
            if(hedge_trades || (!hedge_trades && !exist_order(OP_SELL))) market_buy_order();
           }

         if(sell)
           {
            if(close_on_opposite) close_current_orders(OP_BUY);
            if(hedge_trades || (!hedge_trades && !exist_order(OP_BUY))) market_sell_order();
           }

        }
      bar_time=Time[0];
     }
  }
//-----------------------------------------------------------------------------------------------------
int total_orders()
  {
   int tot_orders=0;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()==Magic_Number
         && OrderSymbol()==Symbol()
         && (OrderType()==OP_BUY || OrderType()==OP_SELL)
         ) tot_orders=tot_orders+1;
     }
   return(tot_orders);
  }
//-----------------------------------------------------------------------------------------------------
int market_buy_order()
  {
   double rem=0; bool x=false;
   NewOrder=0;
   tries=0;
   double x_lots=0;
   if(Fixed_Lot_Size>0) Lot_Size=Fixed_Lot_Size;
   else
     {
      Lot_Size=NormalizeDouble((AccountBalance()*Bal_Perc_Lot_Size/100)/MarketInfo(Symbol(),MODE_MARGINREQUIRED),lot_decimals);
     }
   if(SL_Pips==0) sl_price=0;
   else sl_price=MarketInfo(Symbol(),MODE_ASK)-SL_Pips*Point;
   if(TP_Pips==0) tp_price=0;
   else tp_price=MarketInfo(Symbol(),MODE_ASK)+TP_Pips*Point;

   if(enable_atr)
     {
      if(ATR_SL>0) sl_price=NormalizeDouble(MarketInfo(Symbol(),MODE_ASK)-atr*ATR_SL,Digits);
      else sl_price=0;
      if(ATR_TP>0) tp_price=NormalizeDouble(MarketInfo(Symbol(),MODE_ASK)+atr*ATR_TP,Digits);
      else tp_price=0;
     }
   while(NewOrder<=0 && tries<oper_max_tries && MarketInfo(Symbol(),MODE_ASK)-MarketInfo(Symbol(),MODE_BID)<=max_spread*Point)
     {
      if(ECN)
        {
         NewOrder=OrderSend(Symbol(),OP_BUY,Lot_Size,MarketInfo(Symbol(),MODE_ASK),max_slippage,
                            0,
                            0,
                            "YT5",Magic_Number,0,Blue);
         if(NewOrder>0 && OrderSelect(NewOrder,SELECT_BY_TICKET,MODE_TRADES))
           {
            if(sl_price>0 || tp_price>0) order_mod=OrderModify(NewOrder,OrderOpenPrice(),sl_price,tp_price,0,clrNONE);
           }
         else
           {
            err_num=GetLastError();
            if(err_num!=ERR_NO_ERROR) Print("Error in Sending a Buy Order : ",ErrorDescription(err_num));
           }

        }
      else
        {
         NewOrder=OrderSend(Symbol(),OP_BUY,Lot_Size,MarketInfo(Symbol(),MODE_ASK),max_slippage,
                            sl_price,
                            tp_price,
                            "YT5",Magic_Number,0,Blue);
         if(NewOrder<0)
           {
            err_num=GetLastError();
            if(err_num!=ERR_NO_ERROR) Print("Error in Sending a Buy Order : ",ErrorDescription(err_num));
           }

        }
      tries=tries+1;
     }

   return(NewOrder);
  }
//-----------------------------------------------------------------------------------------------------
int market_sell_order()
  {
   double rem=0; bool x=false;
   NewOrder=0;
   tries=0;
   double x_lots=0;
   if(Fixed_Lot_Size>0) Lot_Size=Fixed_Lot_Size;
   else
     {
      Lot_Size=NormalizeDouble((AccountBalance()*Bal_Perc_Lot_Size/100)/MarketInfo(Symbol(),MODE_MARGINREQUIRED),lot_decimals);
     }

   if(SL_Pips==0) sl_price=0;
   else sl_price=MarketInfo(Symbol(),MODE_BID)+SL_Pips*Point;
   if(TP_Pips==0) tp_price=0;
   else tp_price=MarketInfo(Symbol(),MODE_BID)-TP_Pips*Point;
   if(enable_atr)
     {
      if(ATR_SL>0) sl_price=NormalizeDouble(MarketInfo(Symbol(),MODE_BID)+atr*ATR_SL,Digits);
      else sl_price=0;
      if(ATR_TP>0) tp_price=NormalizeDouble(MarketInfo(Symbol(),MODE_BID)-atr*ATR_TP,Digits);
      else tp_price=0;
     }

   while(NewOrder<=0 && tries<oper_max_tries && MarketInfo(Symbol(),MODE_ASK)-MarketInfo(Symbol(),MODE_BID)<=max_spread*Point)
     {
      if(ECN)
        {

         NewOrder=OrderSend(Symbol(),OP_SELL,Lot_Size,MarketInfo(Symbol(),MODE_BID),max_slippage,
                            0,
                            0,
                            "YT5",Magic_Number,0,Red);

         if(NewOrder>0 && OrderSelect(NewOrder,SELECT_BY_TICKET,MODE_TRADES))
           {
            if(sl_price>0 || tp_price>0) order_mod=OrderModify(NewOrder,OrderOpenPrice(),sl_price,tp_price,0,clrNONE);
           }
         else
           {
            err_num=GetLastError();
            if(err_num!=ERR_NO_ERROR) Print("Error in Sending a Sell Order : ",ErrorDescription(err_num));
           }

        }
      else
        {
         NewOrder=OrderSend(Symbol(),OP_SELL,Lot_Size,MarketInfo(Symbol(),MODE_BID),max_slippage,
                            sl_price,
                            tp_price,
                            "YT5",Magic_Number,0,Red);
         if(NewOrder<0)
           {
            err_num=GetLastError();
            if(err_num!=ERR_NO_ERROR) Print("Error in Sending a Sell Order : ",ErrorDescription(err_num));
           }

        }
      tries=tries+1;
     }

   return(NewOrder);
  }
//-----------------------------------------------------------------------------------------------------
int current_order_type()
  {
   int ord_type=-1;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()==Magic_Number
         && OrderSymbol()==Symbol()
         && (OrderType()==OP_BUY || OrderType()==OP_SELL)
         )
        {
         ord_type=OrderType();
        }
     }
   return(ord_type);
  }
//-----------------------------------------------------------------------------------------------------
bool exist_order(int ord_type)
  {
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()==Magic_Number
         && OrderSymbol()==Symbol()
         && OrderType()==ord_type
         )
        {
         return(true);
        }
     }
   return(false);
  }
//-----------------------------------------------------------------------------------------------------
void close_current_orders(int ord_type)
  {
   int k=-1,j=0;
   bool x= false;
   for( j=0;j<100;j++) ord_arr[j]=0;

   int ot=OrdersTotal();
   for(j=0;j<ot;j++)
     {
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==Magic_Number)
        {
         if(OrderType()==ord_type)
           {
            k=k+1;
            ord_arr[k]=OrderTicket();
           }
        }
     }
   for(j=0;j<=k;j++)
     {
      bool OrderClosed=false;
      tries=0;
      while(!OrderClosed && tries<oper_max_tries)
        {
         RefreshRates();
         x=OrderSelect(ord_arr[j],SELECT_BY_TICKET,MODE_TRADES);
         if(OrderType()==OP_SELL) OrderClosed=OrderClose(ord_arr[j],OrderLots(),MarketInfo(Symbol(),MODE_ASK),100,NULL);
         if(OrderType()==OP_BUY) OrderClosed=OrderClose(ord_arr[j],OrderLots(),MarketInfo(Symbol(),MODE_BID),100,NULL);
         tries=tries+1;
        }
     }
  }
//-----------------------------------------------------------------------------------------------------
void trail_stop()
  {
   double new_sl=0; bool OrderMod=false;
   trail_stop_pips=Trail_Stop_Pips;
   if(enable_atr)
     {
      trail_stop_pips=(int)((atr*ATR_TS)/Point);
     }
   if(trail_stop_pips==0) return;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()==Magic_Number && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)
           {
            new_sl=0;
            if(MarketInfo(Symbol(),MODE_BID)-OrderOpenPrice()>trail_stop_pips*Point && (OrderOpenPrice()>OrderStopLoss() || OrderStopLoss()==0))
               new_sl=OrderOpenPrice()+Point;
            if(MarketInfo(Symbol(),MODE_BID)-OrderStopLoss()>(trail_stop_pips*Point+Trail_Stop_Jump_Pips*Point) && OrderStopLoss()>OrderOpenPrice())
               new_sl=MarketInfo(Symbol(),MODE_BID)-trail_stop_pips*Point;
            OrderMod=false;
            tries=0;

            while(!OrderMod && tries<oper_max_tries && new_sl>0 && new_sl>OrderStopLoss())
              {
               OrderMod=OrderModify(OrderTicket(),OrderOpenPrice(),new_sl,OrderTakeProfit(),0,White);
               if(!OrderMod) err_num=GetLastError();
               if(err_num!=ERR_NO_ERROR) Print("Order SL Modify Error: ",ErrorDescription(err_num));

               tries=tries+1;

              }

           }
         if(OrderType()==OP_SELL)
           {
            new_sl=0;
            if(OrderOpenPrice()-MarketInfo(Symbol(),MODE_ASK)>trail_stop_pips*Point && (OrderOpenPrice()<OrderStopLoss() || OrderStopLoss()==0))
               new_sl=OrderOpenPrice()-Point;
            if(OrderStopLoss()-MarketInfo(Symbol(),MODE_ASK)>trail_stop_pips*Point+Trail_Stop_Jump_Pips*Point && OrderStopLoss()<OrderOpenPrice())
               new_sl=MarketInfo(Symbol(),MODE_ASK)+trail_stop_pips*Point;
            OrderMod=false;
            tries=0;

            while(!OrderMod && tries<oper_max_tries && new_sl>0 && new_sl<OrderStopLoss())
              {
               OrderMod=OrderModify(OrderTicket(),OrderOpenPrice(),new_sl,OrderTakeProfit(),0,White);
               if(!OrderMod) err_num=GetLastError();
               if(err_num!=ERR_NO_ERROR) Print("Order SL Modify Error: ",ErrorDescription(err_num));
               tries=tries+1;

              }

           }

        }
     }
  }
//-----------------------------------------------------------------------------------------------------
bool invalid_pair(string pair)
  {
   for(int i=0;i<SymbolsTotal(true);i++)
     {
      if(SymbolName(i,true)==pair) return(false);
     }
   return(true);
  }
//-----------------------------------------------------------------------------------------------------
void move_to_BE()
  {
   double new_sl=0; bool OrderMod=false;
   trail_stop_pips=BE_Pips;
   if(enable_atr)
     {
      trail_stop_pips=(int)((atr*ATR_BE)/Point);
     }
   if(trail_stop_pips==0) return;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()==Magic_Number && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)
           {
            new_sl=0;
            if(MarketInfo(Symbol(),MODE_BID)-OrderOpenPrice()>trail_stop_pips*Point && (OrderOpenPrice()>OrderStopLoss() || OrderStopLoss()==0))
               new_sl=OrderOpenPrice()+trail_stop_pips*Point;
            OrderMod=false;
            tries=0;

            while(!OrderMod && tries<oper_max_tries && new_sl>0 && new_sl>OrderStopLoss())
              {
               OrderMod=OrderModify(OrderTicket(),OrderOpenPrice(),new_sl,OrderTakeProfit(),0,White);
               if(!OrderMod) err_num=GetLastError();
               if(err_num!=ERR_NO_ERROR) Print("Order move to BE Modify Error: ",ErrorDescription(err_num));

               tries=tries+1;

              }

           }
         if(OrderType()==OP_SELL)
           {
            new_sl=0;
            if(OrderOpenPrice()-MarketInfo(Symbol(),MODE_ASK)>trail_stop_pips*Point && (OrderOpenPrice()<OrderStopLoss() || OrderStopLoss()==0))
               new_sl=OrderOpenPrice()-trail_stop_pips*Point;
            OrderMod=false;
            tries=0;

            while(!OrderMod && tries<oper_max_tries && new_sl>0 && new_sl<OrderStopLoss())
              {
               OrderMod=OrderModify(OrderTicket(),OrderOpenPrice(),new_sl,OrderTakeProfit(),0,White);
               if(!OrderMod) err_num=GetLastError();
               if(err_num!=ERR_NO_ERROR) Print("Order move to BE Modify Error: ",ErrorDescription(err_num));
               tries=tries+1;

              }

           }

        }
     }
  }
//-----------------------------------------------------------------------------------------------------
void move_to_PL()
  {
   double new_sl=0; bool OrderMod=false;
   trail_stop_pips=PL_Pips;
   if(enable_atr)
     {
      trail_stop_pips=(int)((atr*ATR_PL)/Point);
     }
   if(trail_stop_pips==0) return;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()==Magic_Number && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)
           {
            new_sl=0;
            if(MarketInfo(Symbol(),MODE_BID)-OrderOpenPrice()>trail_stop_pips*Point && OrderOpenPrice()<OrderStopLoss())
               new_sl=OrderOpenPrice()+trail_stop_pips*Point;
            OrderMod=false;
            tries=0;

            while(!OrderMod && tries<oper_max_tries && new_sl>0 && new_sl>OrderStopLoss())
              {
               OrderMod=OrderModify(OrderTicket(),OrderOpenPrice(),new_sl,OrderTakeProfit(),0,White);
               if(!OrderMod) err_num=GetLastError();
               if(err_num!=ERR_NO_ERROR) Print("Order move to PL Modify Error: ",ErrorDescription(err_num));

               tries=tries+1;

              }

           }
         if(OrderType()==OP_SELL)
           {
            new_sl=0;
            if(OrderOpenPrice()-MarketInfo(Symbol(),MODE_ASK)>trail_stop_pips*Point && OrderOpenPrice()>OrderStopLoss())
               new_sl=OrderOpenPrice()-trail_stop_pips*Point;
            OrderMod=false;
            tries=0;

            while(!OrderMod && tries<oper_max_tries && new_sl>0 && new_sl<OrderStopLoss())
              {
               OrderMod=OrderModify(OrderTicket(),OrderOpenPrice(),new_sl,OrderTakeProfit(),0,White);
               if(!OrderMod) err_num=GetLastError();
               if(err_num!=ERR_NO_ERROR) Print("Order move to PL Modify Error: ",ErrorDescription(err_num));
               tries=tries+1;

              }

           }

        }
     }
  }
//-----------------------------------------------------------------------------------------------------
