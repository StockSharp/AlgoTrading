//+------------------------------------------------------------------+
//|                                                 X3MA EA V2.0.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""

extern double order_lot=0.1;
extern bool money_management=true;
extern double risk_percent=10;
extern double take_profit=0;         				//Fixed takeprofit in pips (0=no takeprofit)
extern double stop_loss=0;           				//Fixed stoploss in pips (0=no stoploss)

extern double equity_stop_loss_percent=20;
extern double equity_take_profit_percent=20;

extern double range_gap_pips=0.2;

extern bool enable_entry_medium_slow_cross=true;
extern bool enable_exit_fast_slow_cross=true;


extern int fast_ma_period=2;
extern int fast_ma_shift=0;
extern int fast_ma_method=0;
extern int fast_ma_apply_price=0;

extern int medium_ma_period=8;
extern int medium_ma_shift=0;
extern int medium_ma_method=0;
extern int medium_ma_apply_price=0;

extern int slow_ma_period=16;
extern int slow_ma_shift=0;
extern int slow_ma_method=0;
extern int slow_ma_apply_price=0;

extern int start_trading_hour=0;
extern int start_trading_minute=0;
extern int end_trading_hour=24;
extern int end_trading_minute=0;

extern int max_trades=2;
extern int signal_bar=0;
extern int magic_number=112413;

extern double slippage=5;              			//Allowed slippage of open/close order

int bar=1;
int timeframe=0;
int number_retry_open_trade=10;

double     myPoint, mySpread, myStopLevel,myTickValue,myTickSize,myLotValue;
int        myDigits;
datetime   TradeBarTime;
bool       enableopen;
bool       enable_ea;
double     my_lots;
int        digit_lot;
int ratio=1000000;
int signal;
int signalclose;
int myBars;
double last_history_check;
string TradeCode="X3MA_EA2.0";

int cross_type;

   
int err;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   SetPoint();
   if(Digits==3 || Digits==5) slippage*=10;
   if(MarketInfo(Symbol(),MODE_LOTSTEP)>=0.01) digit_lot=2;   
   if(MarketInfo(Symbol(),MODE_LOTSTEP)>=0.1) digit_lot=1;   
   if(MarketInfo(Symbol(),MODE_LOTSTEP)>=1) digit_lot=0;
   TradeBarTime=Time[0];
   bar=signal_bar;
   DrawLine("last_bid_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,Bid+ratio);
   DrawLine("last_ask_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,Ask+ratio);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   for (int j=0;j<=30;j++) ObjectDelete("bg"+j+"_"+TradeCode);

//----
   return(0);
  }

int signal()
{ 

   double ma11,ma12;
   double ma21,ma22;
   double ma31,ma32;
   
   ma11=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar);
   ma12=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar+1);
   ma21=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar);
   ma22=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar+1);
   ma31=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar);
   ma32=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar+1);   
   
   double lastbid,lastask;
   lastbid=GetHLineValue("last_bid_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
   lastask=GetHLineValue("last_ask_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
   
   if(enable_entry_medium_slow_cross)
   {
      if(ma21>ma31 && ma22<=ma32)    
      {
         DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,123+ratio);
         DrawLine("last_cross_buy_23_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
         DrawLine("cross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
      else if(ma21<ma31 && ma22>=ma32) 
      {
         DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,223+ratio);
         DrawLine("last_cross_sell_23_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
         DrawLine("cross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
   }

   if(ma11>ma21 && ma12<=ma22 && ma11>=ma31 && ma21>=ma31)    
   {
      DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,112+ratio);
      DrawLine("last_cross_buy_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
      DrawLine("cross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
   }
   else if(ma11<ma21 && ma12>=ma22  && ma11<=ma31 && ma21<=ma31) 
   {
      DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,212+ratio);
      DrawLine("last_cross_sell_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
      DrawLine("cross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
   }

   if(ma11>ma31 && ma12<=ma32)    
   {
      DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,113+ratio);
      DrawLine("last_cross_buy_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
      DrawLine("cross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
   }
   else if(ma11<ma31 && ma12>=ma32) 
   {
      DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,213+ratio);
      DrawLine("last_cross_sell_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
      DrawLine("cross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
   }

   
   if(GetHLineValue("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)>0)
   {
      int crosstype;
      crosstype=GetHLineValue("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
      cross_type=crosstype;
      double crosspricebuy=GetHLineValue("cross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
      double crosspricesell=GetHLineValue("cross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
      if(crosstype==113 && GetHLineValue("last_cross_buy_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
      {
         if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
         {
            return(10);
         }
      }
      else if(crosstype==213 && GetHLineValue("last_cross_sell_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
      {
         if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
         {
            return(20);
         }
      }      
      else if(crosstype==112 && GetHLineValue("last_cross_buy_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
      {
         if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
         {
            return(10);
         }
      }
      else if(crosstype==212 && GetHLineValue("last_cross_sell_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
      {
         if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
         {
            return(20);
         }
      }      
      if(enable_entry_medium_slow_cross)
      {
         if(crosstype==123 && GetHLineValue("last_cross_buy_23_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
            {
               return(10);
            }
         }
         else if(crosstype==223 && GetHLineValue("last_cross_sell_23_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
            {
               return(20);
            }
         }
      }
   }
   
   
   return(30);
}

int signal_close_fast()
{
   double ma11,ma12;
   double ma21,ma22;
   double ma31,ma32;
   
   ma11=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar);
   ma12=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar+1);
   ma21=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar);
   ma22=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar+1);
   ma31=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar);
   ma32=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar+1);   

   double lastbid,lastask;
   lastbid=GetHLineValue("last_bid_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
   lastask=GetHLineValue("last_ask_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
   
   if(CountOpenOrders(OP_BUY,"Fast")+CountOpenOrders(OP_SELL,"Fast")>0)
   {
      if(ma11>ma21 && ma12<=ma22)    
      {
         DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,112+ratio);
         DrawLine("xlast_cross_buy_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
         DrawLine("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
      else if(ma11<ma21 && ma12>=ma22) 
      {
         DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,212+ratio);
         DrawLine("xlast_cross_sell_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
         DrawLine("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
      if(GetHLineValue("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)>0)
      {
         int crosstype;
         crosstype=GetHLineValue("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
         double crosspricebuy=GetHLineValue("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
         double crosspricesell=GetHLineValue("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
         if(crosstype==112)// && GetHLineValue("xlast_cross_buy_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
            {
               return(11);
            }
         }
         else if(crosstype==212)// && GetHLineValue("xlast_cross_sell_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
            {
               return(21);
            }
         }
      }
   }
}

int signal_close_medium()
{
   double ma11,ma12;
   double ma21,ma22;
   double ma31,ma32;
   
   ma11=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar);
   ma12=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar+1);
   ma21=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar);
   ma22=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar+1);
   ma31=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar);
   ma32=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar+1);   

   double lastbid,lastask;
   lastbid=GetHLineValue("last_bid_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
   lastask=GetHLineValue("last_ask_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;

   if(CountOpenOrders(OP_BUY,"Med")+CountOpenOrders(OP_SELL,"Med")>0)
   {
      if(ma11>ma31 && ma12<=ma32)    
      {
         DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,113+ratio);
         DrawLine("xlast_cross_buy_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
         DrawLine("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
      else if(ma11<ma31 && ma12>=ma32) 
      {
         DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,213+ratio);
         DrawLine("xlast_cross_sell_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
         DrawLine("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
      if(GetHLineValue("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)>0)
      {
         int crosstype;
         crosstype=GetHLineValue("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
         double crosspricebuy=GetHLineValue("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
         double crosspricesell=GetHLineValue("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
         if(crosstype==113)// && GetHLineValue("xlast_cross_buy_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
            {
               return(12);
            }
         }
         else if(crosstype==213)// && GetHLineValue("xlast_cross_sell_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
            {
               return(22);
            }
         }      
      }
   }

}
   
double sum_all_profit()
{
   double total;
   int i;
   for(i=OrdersTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==True)
      {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() ==magic_number && OrderProfit()>0)
         {
            total=total+OrderProfit();
         }
      }
   }
   /*
   for(i=OrdersHistoryTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==True)
      {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() ==magic_number && OrderProfit()>0)
         {
            total=total+OrderProfit();
         }
      }
   }
   */
   
   return(total);

}

double sum_all_loss()
{
   double total;
   int i;
   for(i=OrdersTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==True)
      {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() ==magic_number && OrderProfit()<0)
         {
            total=total+OrderProfit();
         }
      }
   }
   /*
   for(i=OrdersHistoryTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==True)
      {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() ==magic_number && OrderProfit()<0)
         {
            total=total+OrderProfit();
         }
      }
   }
   */
   return(total);

}


double sum_profit()
{
   double total;
   int i;
   for(i=OrdersTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==True)
      {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() ==magic_number)
         {
            total=total+OrderProfit();
         }
      }
   }
   return(total);
}

double last_close_time()
{  
   int i;
   datetime closetime=0;
   for(i=OrdersHistoryTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==True)
      {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic_number)
         {
            if(closetime==0 || closetime<OrderCloseTime())
            {
               closetime=OrderCloseTime();
            }  
         }
      }
   }
   return(closetime);
}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   double sl,tp;   
   int openticket1;
   int openticket2;
   double openprice1;
   double openprice2;
   double stop;
   int i;
   int x;
   int status;
   
   int signalclosefast;
   int signalclosemed;

   if(signal_bar>=0)
   {
      signal=signal();
      signalclosefast=signal_close_fast();
      if(enable_exit_fast_slow_cross) signalclosemed=signal_close_medium();
   }

   if(CountOpenOrders(OP_BUY,TradeCode)+CountOpenOrders(OP_SELL,TradeCode)>0)
   {
      double total_profit=sum_profit();
      if((total_profit*100/AccountEquity()<=0-equity_stop_loss_percent && equity_stop_loss_percent>0) || (total_profit*100/AccountEquity()>=equity_take_profit_percent && equity_take_profit_percent))
      {
         CloseTrades(OP_SELL,TradeCode,0);
         CloseTrades(OP_BUY,TradeCode,0);
      }
      if(signalclosefast==11) CloseTrades(OP_SELL,"Fast",11);
      if(signalclosefast==21) CloseTrades(OP_BUY,"Fast",21);
      if(enable_exit_fast_slow_cross)
      {
         if(signalclosemed==12) CloseTrades(OP_SELL,"Med",12);
         if(signalclosemed==22) CloseTrades(OP_BUY,"Med",22);
      }
   }


   bool enableopen = true; 

   if(!istradinghours(start_trading_hour,end_trading_hour,start_trading_minute,end_trading_minute)) enableopen=false;
   
   if(signal!=30)
   {
      datetime lastclosetime=last_close_time();
      if(iBarShift(Symbol(),timeframe,lastclosetime)==0 && lastclosetime>0) enableopen=false;
   }
    
   if(enableopen)
      if(TradeBarTime!=Time[0])
         if(signal!=30 )
            if(CountOpenOrders(OP_BUY,TradeCode)+CountOpenOrders(OP_SELL,TradeCode)<max_trades)
            {
               my_lots=order_lot;
               string strType;
               if(cross_type==112 || cross_type==212) strType="FastMedCross||";    
               if(cross_type==113 || cross_type==213) strType="FastSlowCross||";    
               if(cross_type==123 || cross_type==223) strType="MedSlowCross||";               
               if(money_management)
               {
                  double my_lots2;
                  double money2 = (AccountBalance() * AccountLeverage() * risk_percent * 0.01);
                  my_lots2 =money2/MarketInfo(Symbol(), MODE_LOTSIZE);
                  //my_lots2=(AccountBalance()*risk_percent*0.01)/(MarketInfo(Symbol(),MODE_MARGINREQUIRED));  
                  my_lots=NormalizeDouble(my_lots2,digit_lot);
               }
               if(my_lots>MarketInfo(Symbol(),MODE_MAXLOT)) my_lots=MarketInfo(Symbol(),MODE_MAXLOT);
               if(my_lots<MarketInfo(Symbol(),MODE_MINLOT)) my_lots=MarketInfo(Symbol(),MODE_MINLOT);
               if(signal==10)
               {
                  openprice1=Ask;
                  if (stop_loss==0) sl=0;
                  if (stop_loss>0) sl=openprice1-(stop_loss*myPoint);
                  if (take_profit==0) tp=0;      
                  if (take_profit>0) tp=openprice1+(take_profit*myPoint); 
                  openticket1=Open_Trade(Symbol(),OP_BUY,openprice1, my_lots,sl, tp, strType+TradeCode);
                  if(openticket1>0) 
                  {
                     DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,0);
                     Alert("Buy Alert "+ Symbol()+ " " + strtf(Period()) + " Date & Time: " + TimeToStr(TimeCurrent(),TIME_DATE)+" "+TimeToStr(TimeCurrent(),TIME_MINUTES) + " - " + WindowExpertName());
                  }
               }
               if(signal==20)
               {      
                  openprice2=Bid;
                  if (stop_loss==0) sl=0;
                  if (stop_loss>0) sl=openprice2+(stop_loss*myPoint);
                  if (take_profit==0) tp=0;      
                  if (take_profit>0) tp=openprice2-(take_profit*myPoint); 
                  openticket2=Open_Trade(Symbol(),OP_SELL,openprice2, my_lots,sl, tp, strType+TradeCode);
                  if(openticket2>0) 
                  {
                     DrawLine("last_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,0);
                     Alert("Sell Alert "+ Symbol()+ " " + strtf(Period()) + " Date & Time: " + TimeToStr(TimeCurrent(),TIME_DATE)+" "+TimeToStr(TimeCurrent(),TIME_MINUTES) + " - " + WindowExpertName());
                  }
               }
            }


   DrawLine("last_bid_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,Bid+ratio);
   DrawLine("last_ask_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,Ask+ratio);
   
   double totalprofit=sum_all_profit();
   double totalloss=sum_all_loss();
   double grossprofit=totalprofit-totalloss;
   
   string strcomment;
   string lnfeed="\n";
   strcomment=strcomment+WindowExpertName()+lnfeed;
   strcomment=strcomment+lnfeed;
   strcomment=strcomment+"Licence type: Unlimited MT4"+lnfeed;
   strcomment=strcomment+lnfeed;
   strcomment=strcomment+"Copyrighted by www.sbginer.com | www.tfxkenya.com"+lnfeed;
   strcomment=strcomment+"Risk % : "+ DoubleToStr(risk_percent,2)+lnfeed;   
   strcomment=strcomment+"Number of trades : " + (CountOpenOrders(OP_BUY,TradeCode)+CountOpenOrders(OP_SELL,TradeCode))+lnfeed;   
   strcomment=strcomment+"Profits : "+ DoubleToStr(totalprofit,2) + " $ "+ lnfeed;   
   strcomment=strcomment+"Loss : "+DoubleToStr(totalloss,2) + " $ "+lnfeed;   
   strcomment=strcomment+"Gross Profit : "+ DoubleToStr(grossprofit,2)+ " $ "+lnfeed;   
   strcomment=strcomment+"Fast MA : " + fast_ma_period + " | Medium MA : " + medium_ma_period + " | Slow MA : " + slow_ma_period +lnfeed;   
   strcomment=strcomment+"Equity SL % : " + DoubleToStr(equity_stop_loss_percent,2) + " | Equity TP % : " + DoubleToStr(equity_take_profit_percent,2) +lnfeed;   
   strcomment=strcomment+"Balance : " + DoubleToStr(AccountBalance(),2) + " $ "+lnfeed;   
   strcomment=strcomment+"Equity : " + DoubleToStr(AccountEquity(),2)+ " $ "+lnfeed;   
   
   
   int j;
   for (j=0;j<=30;j++)
   {
      drawFixedLbl("bg"+j+"_"+TradeCode, "gggggggggggggggggggggggggggggggggggg", 0, 1, j*6, 5, "Webdings", DeepSkyBlue, true);
   }

   Comment(strcomment);
   
   

 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+-------------------------General Functions------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+


void SetPoint()
{

   myPoint     =  MarketInfo(Symbol(),MODE_POINT);
   mySpread    =  MarketInfo(Symbol(),MODE_SPREAD);
   myDigits    =  MarketInfo(Symbol(),MODE_DIGITS);
   myStopLevel =  MarketInfo(Symbol(),MODE_STOPLEVEL);
   myTickValue = MarketInfo(Symbol(),MODE_TICKVALUE);
   myTickSize = MarketInfo(Symbol(),MODE_TICKSIZE);
   myLotValue = myTickValue/myTickSize;

   if(
         myDigits==3    ||  
         myDigits==5
     )
      {
         myPoint     =  myPoint  *  10;
         mySpread    =  mySpread /  10;
         myStopLevel =  myStopLevel / 10;
         myDigits    =  myDigits -1;
      }    

}

//+------------------------------------------------------------------+
//+----------------------Trading Hours-------------------------------+

bool istradinghours(int starthour, int endhour, int startminute=0, int endminute=0, int shift = 0)
{
   int nowtime = (Hour()+shift)*60 + Minute();
   if (nowtime < 0)
      nowtime += 1440;
   if (nowtime >= 1440)
      nowtime -= 1440;
   int starttime = starthour*60 + startminute;
   int endtime = endhour*60 + endminute;

   if (starttime <= endtime)
   {
      if ((nowtime < starttime) || (nowtime >= endtime))
         return(false);
   }
   else
   {
      if ((nowtime < starttime) && (nowtime >= endtime))
         return(false);
   }
   return(true);
}

//+------------------------------------------------------------------+
//+------------------Count Number of Open Orders---------------------+

int CountOpenOrders(int direction, string strtype)
{
int i,j;
   

   
   j  =  0;
   
   for(i=OrdersTotal()-1;i>=0;i--)
      {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==True)
            {
               if(   OrderType()          == direction   &&
                     OrderSymbol()        == Symbol()          &&
                     OrderMagicNumber()   == magic_number     &&
                     StringFind(OrderComment(),strtype)!=-1
                 )
                     j++;
            }
         else
            Print("Could not SELECT trade");
      }
   
   return(j);
}
//+------------------------------------------------------------------+
//+-------------------Close Trades by Symbol()-------------------------+

int CloseTrades(int direction, string strtype,int signal)
{
int j;
//double ClosePrice;

   j  =  0;
   int x;
   int status;
   
   if(direction==-1)
      return;
      
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)
      {
         if(OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)==True)
            {
               if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic_number && StringFind(OrderComment(),strtype)!=-1)                     
                  if(OrderType()==direction)
                     {
                        if(OrderType()==OP_BUY)
                           {
                              for (x = 5; x!= 0; x--) 
                              {                              
                                 while (IsTradeContextBusy() || !IsTradeAllowed()) Sleep(5000);
                                 status = OrderClose(OrderTicket(),OrderLots(),Bid,0);
                                 if (status == 1) { Print(strtype + " Close by signal " + signal); j++; break; }
                              }
                           }
                        else
                           if(OrderType()==OP_BUYLIMIT)
                              {
                                 for (x = 5; x!= 0; x--) 
                                 {                              
                                    while (IsTradeContextBusy() || !IsTradeAllowed()) Sleep(5000);
                                    status = OrderDelete(OrderTicket());
                                    if (status == 1) { j++; break; }
                                 }
                              }
                        else
                           if(OrderType()==OP_BUYSTOP)
                              {
                                 for (x = 5; x!= 0; x--) 
                                 {                              
                                    while (IsTradeContextBusy() || !IsTradeAllowed()) Sleep(5000);
                                    status = OrderDelete(OrderTicket());
                                    if (status == 1) { j++; break; }
                                 }
                              }
                        
                        if(OrderType()==OP_SELL)
                           {
                              for (x = 5; x!= 0; x--) 
                              {                              
                                 while (IsTradeContextBusy() || !IsTradeAllowed()) Sleep(5000);
                                 status = OrderClose(OrderTicket(),OrderLots(),Ask,0);
                                 if (status == 1) { Print(strtype + " Close by signal " + signal); j++; break; }
                              }
                           }
                        else
                           if(OrderType()==OP_SELLLIMIT)
                              {
                                 for (x = 5; x!= 0; x--) 
                                 {                              
                                    while (IsTradeContextBusy() || !IsTradeAllowed()) Sleep(5000);
                                    status = OrderDelete(OrderTicket());
                                    if (status == 1) { j++; break; }
                                 }
                              }
                        else
                           if(OrderType()==OP_SELLSTOP)
                              {
                                 for (x = 5; x!= 0; x--) 
                                 {                              
                                    while (IsTradeContextBusy() || !IsTradeAllowed()) Sleep(5000);
                                    status = OrderDelete(OrderTicket());
                                    if (status == 1) { j++; break; }
                                 }
                              }
                     }
            }
      }
   
   return(j);
}


//+------------------------------------------------------------------+
//+----------------Modify Profit Target and Stop Loss----------------+

bool ModifyProfitTarget(int myTicket, double ProfitTarget, double StopLoss)
{
   int try;
   if(OrderSelect(myTicket,SELECT_BY_TICKET,MODE_TRADES)==False)
      return(false);
            
      if(
            (
               MathRound(ProfitTarget/myPoint)   != MathRound(OrderTakeProfit()/myPoint)
               ||
               MathRound(StopLoss/myPoint)       != MathRound(OrderStopLoss()/myPoint)
            )                 
        )
            {
               //RefreshRates();
               for(try=1;try<=5;try++)
               {
                  if(OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StopLoss,Digits),NormalizeDouble(ProfitTarget,Digits),OrderExpiration()))
                     return(true);
                  //else{
                  //   err=GetLastError();
                  //   Print("OrderModify Error # " + err + " : ",ErrorDescription(err));
                  //}
               }
            }
   return(false);
}







//+------------------------------------------------------------------+
//+----------------------OPEN TRADE----------------------------------+


int Open_Trade(string curr,int cmd,double price, double lot,double sl, double tp, string comm)
{

   int ticket,retry;
   color colour;
   
   sl=NormalizeDouble(sl,Digits);
   tp=NormalizeDouble(tp,Digits);
   price=NormalizeDouble(price,Digits);
   
   if(cmd==0) colour=Blue;
   if(cmd==1) colour=Red;
   
   if(Digits==3 || Digits==5) 
   {
      for(retry=1;retry<=number_retry_open_trade;retry++)
      {      
         RefreshRates();
         ticket=OrderSend(curr,cmd,lot,price,slippage,0,0,comm,magic_number,0,colour);
         if(ticket>0) break;
         //else {
         //   err=GetLastError();
         //   Print("OrderSend Error # " + err + " : ",ErrorDescription(err));
         //}
      }
   }
   else 
   {
      for(retry=1;retry<=number_retry_open_trade;retry++)
      {
         RefreshRates();
         ticket=OrderSend(curr,cmd,lot,price,slippage,sl,tp,comm,magic_number,0,colour);   
         if(ticket>0) break;  
         //else {
         //   err=GetLastError();
         //   Print("OrderSend Error # " + err + " : ",ErrorDescription(err));
         //}
      }
   }
         
   if(ticket>0)
   {
      if(Digits==3 || Digits==5)
      {
         OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
         for(retry=1;retry<=number_retry_open_trade;retry++)
         {
            if(ModifyProfitTarget(ticket, tp, sl))
            break;
         }
      }
      TradeBarTime=Time[0];
      return(ticket);
   }
}
/*
bool isNewCandle()  
{
   bool res=false;
   if (myBars!=Bars)
   {
      myBars=Bars;
      res=true;
   }

return(res);   

}
*/

string strtf(int tf)
{
   switch(tf)
   {
      case PERIOD_M1: return("M1");
      case PERIOD_M5: return("M5");
      case PERIOD_M15: return("M15");
      case PERIOD_M30: return("M30");
      case PERIOD_H1: return("H1");
      case PERIOD_H4: return("H4");
      case PERIOD_D1: return("D1");
      case PERIOD_W1: return("W1");
      case PERIOD_MN1: return("MN1");
      default:return("Unknown timeframe");
   }
}


//+------------------------------------------------------------------+
//+-----------------------------Draw Line----------------------------+


void DrawLine(string sName, double dPrice,color cLineClr=CLR_NONE)
{
    int iWidth=1;
    string sObjName = sName;

    if(ObjectFind(sObjName) == -1){
        // create object 
        ObjectCreate(sObjName,OBJ_HLINE, 0, 0,0);
    }

    ObjectSet(sObjName,OBJPROP_PRICE1,dPrice);
    ObjectSet(sObjName, OBJPROP_COLOR, cLineClr);
    ObjectSet(sObjName, OBJPROP_WIDTH, iWidth);
}

//+------------------------------------------------------------------+
//+---------------------------Get Line Price-------------------------+


double GetHLineValue(string name)
{

   if (ObjectFind(name) == -1)
      return(-1);
   else
      return(ObjectGet(name,OBJPROP_PRICE1));
}


void drawFixedLbl(string objname, string s, int Corner, int DX, int DY, int FSize, string Font, color c, bool bg)
{
   if (ObjectFind(objname) < 0) {ObjectCreate(objname, OBJ_LABEL, 0, 0, 0);}   
   ObjectSet(objname, OBJPROP_CORNER, Corner);
   ObjectSet(objname, OBJPROP_XDISTANCE, DX);
   ObjectSet(objname, OBJPROP_YDISTANCE, DY);
   ObjectSet(objname,OBJPROP_BACK, bg);      
   ObjectSetText(objname, s, FSize, Font, c);
}

/*
int signal_close()
{ 

   double ma11,ma12;
   double ma21,ma22;
   double ma31,ma32;
   
   ma11=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar);
   ma12=iMA(Symbol(),0,fast_ma_period,fast_ma_shift,fast_ma_method,fast_ma_apply_price,bar+1);
   ma21=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar);
   ma22=iMA(Symbol(),0,medium_ma_period,medium_ma_shift,medium_ma_method,medium_ma_apply_price,bar+1);
   ma31=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar);
   ma32=iMA(Symbol(),0,slow_ma_period,slow_ma_shift,slow_ma_method,slow_ma_apply_price,bar+1);   

   double lastbid,lastask;
   lastbid=GetHLineValue("last_bid_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
   lastask=GetHLineValue("last_ask_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;

   if(enable_exit_fast_slow_cross)
   {
      if(ma11>ma31 && ma12<=ma32)    
      {
         DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,113+ratio);
         DrawLine("xlast_cross_buy_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
         DrawLine("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
      else if(ma11<ma31 && ma12>=ma32) 
      {
         DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,213+ratio);
         DrawLine("xlast_cross_sell_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
         DrawLine("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
      }
   }
   
   if(ma11>ma21 && ma12<=ma22)    
   {
      DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,112+ratio);
      DrawLine("xlast_cross_buy_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));
      DrawLine("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
   }
   else if(ma11<ma21 && ma12>=ma22) 
   {
      DrawLine("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,212+ratio);
      DrawLine("xlast_cross_sell_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iTime(Symbol(),timeframe,bar));   
      DrawLine("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number,iClose(Symbol(),timeframe,bar)+ratio);
   }

   if(GetHLineValue("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)>0)
   {
      int crosstype;
      crosstype=GetHLineValue("xlast_cross_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
      double crosspricebuy=GetHLineValue("xcross_price_buy_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
      double crosspricesell=GetHLineValue("xcross_price_sell_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)-ratio;
      if(crosstype==112)// && GetHLineValue("xlast_cross_buy_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
      {
         if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
         {
            return(11);
         }
      }
      else if(crosstype==212)// && GetHLineValue("xlast_cross_sell_12_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
      {
         if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
         {
            return(21);
         }
      }
      if(enable_exit_fast_slow_cross)
      {
         if(crosstype==113)// && GetHLineValue("xlast_cross_buy_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((Bid-crosspricebuy)/myPoint>=range_gap_pips && (lastbid-crosspricebuy)/myPoint<=range_gap_pips) 
            {
               return(12);
            }
         }
         else if(crosstype==213)// && GetHLineValue("xlast_cross_sell_13_"+TradeCode+"_"+Symbol()+strtf(Period())+"_"+magic_number)!=iTime(Symbol(),timeframe,bar))
         {
            if((crosspricesell-Bid)/myPoint>=range_gap_pips && (crosspricesell-lastbid)/myPoint<=range_gap_pips) 
            {
               return(22);
            }
         }      
      }
   }


   return(30);
}
*/