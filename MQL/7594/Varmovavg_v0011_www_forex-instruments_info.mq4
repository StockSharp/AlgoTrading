//+-----------------------------------------------------------------------------------+
//|                                               VarMovAvg V001.mq4                  |
//|       A VMA is an EMA that automatically adjusts its smoothing                    |
//| percentage based on market volatility. Sensitivity is increased                   |
//| by giving more weight given to the current data thus making it                    |
//| a better signal indicator for short and long term markets.                        |
//|Formula :  VMA = [0.0788 * (VR)] * Close]] + [[1- 0.078 * (VR)] * yesterday's VMA]]|                                                             |
//+-----------------------------------------------------------------------------------+
#include  <WinUser32.mqh>
#property copyright "Balidev for Darma"
#property link      "www.balidev.com"
//---- input parameters
extern int       prm.vma.periodAMA  =52;
extern int       prm.vma.nfast      =5;
extern int       prm.vma.nslow      =20;
extern double    prm.vma.G          =1.0;
extern double    prm.vma.dK         =2.0;
extern int       prm.vma.calc_period=100;
extern double    prm.magic          =997;
extern int       prm.sig.pipsBarA   =2;
extern int       prm.sig.pipsBarB   =1;
extern int       prm.sig.pipsTrade  =2;
extern int       prm.mastop.period  =52;
extern int       prm.mastop.mashift =0;
extern int       prm.mastop.method  =MODE_EMA;
extern int       prm.mastop.shift   =0;
extern int       prm.stop.diff      =34;
extern int       prm.entry.diff     =2;
extern int       prm.slippage       =2;
//----
#define TRADE_NONE   -1
#define TRADE_BUY    OP_BUY
#define TRADE_SELL   OP_SELL
//----
#define STATUS_NONE       1
#define STATUS_CROSS      2
#define STATUS_BAR_A      3
#define STATUS_BAR_B      4
#define STATUS_SIGNAL     5
#define STATUS_TRADE      6
#define STATUS_LATE       8
//----
#define MSG_TRADE         1
#define MSG_STATUS        2
#define MSG_ERROR         3
//----
int m_cross =false;
int m_bara  =false;
int m_barb  =false;
int m_status=STATUS_NONE;
int m_trade =TRADE_NONE;
int m_spread=0;
//----
double m_vma[500];
double m_close_a;
double m_high_b;
double m_order_price;
//----
datetime m_ts_m1 =0;
datetime m_ts_m5 =0;
datetime m_ts_m15=0;
datetime m_ts_m30=0;
datetime m_ts_h1 =0;
datetime m_ts_h4 =0;
datetime m_ts_d1 =0;
datetime m_ts_w1 =0;
datetime m_ts_mn1=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_m1  (datetime ts) {return(m_ts_m1  < ts - (ts%PERIOD_M1));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_m5  (datetime ts) {return(m_ts_m5  < ts - (ts%PERIOD_M5));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_m15 (datetime ts) {return(m_ts_m15 < ts - (ts%PERIOD_M15));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_m30 (datetime ts) {return(m_ts_m30 < ts - (ts%PERIOD_M30));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_h1  (datetime ts) {return(m_ts_h1  < ts - (ts%PERIOD_H1));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_h4  (datetime ts) {return(m_ts_h4  < ts - (ts%PERIOD_H4));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_d1  (datetime ts) {return(m_ts_d1  < ts - (ts%PERIOD_D1));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_w1  (datetime ts) {return(m_ts_w1  < ts - (ts%PERIOD_W1));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool is_open_mn1 (datetime ts) {return(m_ts_mn1 < ts - (ts%PERIOD_MN1));}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int get_used_timeframe () 
  {
   return(Period());
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  bool is_open_tick(datetime ts, int timeframe) 
  {
   datetime tick;
     if (timeframe==0)
     {
      tick=get_used_timeframe();
      }
       else 
      {
      tick=timeframe;
     }
   if (tick==PERIOD_M1)      {return(is_open_m1(ts));}
   else if (tick==PERIOD_M5)  {return(is_open_m5(ts));}
      else if (tick==PERIOD_M15) {return(is_open_m15(ts));}
         else if (tick==PERIOD_M30) {return(is_open_m30(ts));}
            else if (tick==PERIOD_H1)  {return(is_open_h1(ts));}
               else if (tick==PERIOD_H4)  {return(is_open_h4(ts));}
                  else if (tick==PERIOD_D1)  {return(is_open_d1(ts));}
                     else if (tick==PERIOD_W1)  {return(is_open_w1(ts));}
                        else if (tick==PERIOD_MN1) {return(is_open_mn1(ts));}
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void set_ts(datetime ts) 
  {
   m_ts_m1 =ts - (ts%(60 * PERIOD_M1));
   m_ts_m5 =ts - (ts%(60 * PERIOD_M5));
   m_ts_m15=ts - (ts%(60 * PERIOD_M15));
   m_ts_m30=ts - (ts%(60 * PERIOD_M30));
   m_ts_h1 =ts - (ts%(60 * PERIOD_H1));
   m_ts_h4 =ts - (ts%(60 * PERIOD_H4));
   m_ts_d1 =ts - (ts%(60 * PERIOD_D1));
   m_ts_w1 =ts - (ts%(60 * PERIOD_W1));
   m_ts_mn1=ts - (ts%(60 * PERIOD_MN1));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int init() 
  {
   m_spread=Ask - Bid;
   m_trade=TRADE_NONE;
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int deinit() 
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void show_message(int type, string msg) 
  {
   Print(msg);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  double get_lots() 
  {
   return(1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void iVMA (string symbol, int tf, int periodAMA, int nfast, int nslow, double G, double dK, int calc_period) 
  {
   int    i,pos=0;
   double noise=0.000000001,AMA,AMA0,signal,ER;
   double dSC,ERSC,SSC,ddK;
   double slowSC,fastSC;
//----
   pos =calc_period - 1;
   AMA0=Close[pos + 1];
   slowSC=(2.0 /(prm.vma.nslow+1));
   fastSC=(2.0 /(prm.vma.nfast+1));
//----
     while(pos>=0) 
     {
        if(pos==Bars - periodAMA - 2) 
        {
         AMA0=Close[pos + 1];
        }
      signal=MathAbs(Close[pos] - Close[pos + periodAMA]);
      noise=0.000000001;
        for(i=0; i < periodAMA; i++) 
        {
         noise =noise + MathAbs(Close[pos+i] - Close[pos+i+1]);
        }
      ER  =signal/noise;
      dSC =(fastSC - slowSC);
      ERSC=ER * dSC;
      SSC =ERSC + slowSC;
      AMA =AMA0 + (MathPow(SSC,G) * (Close[pos] - AMA0));
      m_vma[pos]=AMA;
      //
      AMA0=AMA;
      pos--;
     }
  }
int stop=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   int i;
   int order_count=0;
   string symbol=Symbol();
   int period=Period();
   int ts=CurTime();
   double stoploss;
   /* determine number of opened order */
     for(i=0; i < OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if ((OrderMagicNumber()==prm.magic) && (OrderSymbol()==symbol)) 
           {
              if ((OrderType()==OP_BUY) || (OrderType()==OP_SELL)) 
              {
               order_count ++;
                 if (m_trade==TRADE_NONE) 
                 {
                  m_trade=OrderType();
                 }
              }
           }
        }
     }
   iVMA(symbol, period, prm.vma.periodAMA, prm.vma.nfast, prm.vma.nslow, prm.vma.G, prm.vma.dK, prm.vma.calc_period);
   /* decide what to do */
   /* check current position */
     if (order_count==0) 
     {
      int prev_trade;
      int fwd_trade;
      int fwd_bar;
      int prev_bar;
      int fwd_status;
      double fwd_close_a;
      double fwd_high_b;
      double fwd_order_price;
//----     
        if (m_trade==TRADE_NONE) 
        {
         /* we may catch for bar A and Bar B, and stay at bar B as long 
            the order price didn't hit */
           if (Close[1] < m_vma[1]) 
           {
            prev_trade=TRADE_BUY;
              for(i=1; i<=Bars; i++) 
              {
                 if (Low[i] > m_vma[i]) 
                 {
                  prev_bar=i;
                  break;
                 }
              }
            }
             else if (Close[1] > m_vma[1]) 
            {
               prev_trade=TRADE_SELL;
                 for(i=1; i<=Bars; i++) 
                 {
                    if (High[i] < m_vma[i]) 
                    {
                     prev_bar=i;
                     break;
                    }
                 }
               }
                else 
               {
            /* we cannot determine the previous trade, but we need to search until 
               we found one of the above condition */
              for(i=1; i<=Bars; i++) 
              {
                 if (High[i] < m_vma[i]) 
                 {
                  prev_trade=TRADE_SELL;
                  prev_bar=i;
                  break;
                  }
                   else if (Low[i] > m_vma[i]) 
                  {
                     prev_trade=TRADE_BUY;
                     prev_bar=i;
                     break;
                    }
              }
           }
         fwd_status=STATUS_TRADE;
           if (prev_trade==TRADE_BUY) 
           {
            /* we trace forward for bar A, Bar B and signal */
              for(fwd_bar=prev_bar; ((fwd_bar > 0) && (fwd_status!=STATUS_LATE)); fwd_bar --) 
              {
                 if (fwd_status==STATUS_TRADE) 
                 {
                    if (Close[fwd_bar] < NormalizeDouble(m_vma[fwd_bar] -(prm.sig.pipsBarA * Point), Digits)) 
                    {
                     fwd_status =STATUS_BAR_A;
                     fwd_close_a=Close[fwd_bar];
                     Print("Trade Buy, found Bar A at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                    }
                  }
                   else if (fwd_status==STATUS_BAR_A) 
                  {
                       if (Close[fwd_bar] < NormalizeDouble(fwd_close_a - (prm.sig.pipsBarB * Point), Digits)) 
                       {
                        fwd_status=STATUS_BAR_B;
                        fwd_high_b=Low[fwd_bar];
                        fwd_order_price=NormalizeDouble(fwd_high_b - (prm.sig.pipsTrade * Point), Digits);
                        Print("Trade Buy, found Bar B at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                        } else if (Close[fwd_bar]>=NormalizeDouble(m_vma[fwd_bar] + (prm.sig.pipsBarA * Point), Digits)) {
                           // reset the state
                           fwd_status=STATUS_TRADE;
                           Print("Trade Buy, A Reset at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                          }
                     }
                      else if (fwd_status==STATUS_BAR_B) 
                     {
                  /* check if it is time to entry or it is too late to send an order */
                  /* we found the last bar, and close[0] is the highest price and it is hit the entry position line */
                       if (Low[fwd_bar] < NormalizeDouble(fwd_order_price - (prm.entry.diff * Point), Digits)) 
                       {
                        fwd_status=STATUS_LATE;
                        Print("Too Late to entry ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                        break;
                        }
                         else if (Close[fwd_bar]>=NormalizeDouble(m_vma[fwd_bar] + (prm.sig.pipsBarA * Point), Digits)) 
                        {
                           // reset the state
                           fwd_status=STATUS_TRADE;
                           Print("Trade Buy, B Reset at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                          }
                    }
              }
              if (fwd_status==STATUS_BAR_B) 
              {
               Print("Waiting for signal ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS), "price at ", Close[0], " price range ", fwd_order_price, " - " , NormalizeDouble(fwd_order_price - (prm.entry.diff * Point), Digits));
                 if ((Close[0]<=fwd_order_price) && (Close[0]>=(fwd_order_price - (prm.entry.diff * Point)))) 
                 {
                  // it is  time to entry 
                  Print("SIGNAL at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                  stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_HIGH, prm.mastop.shift);
                  stoploss=NormalizeDouble(stoploss + (prm.stop.diff * Point), Digits);
                  //open the new order
                  if (OrderSend(
                         symbol,
                         OP_SELL,
                         get_lots(),
                         Bid,
                         prm.slippage,
                         stoploss,
                         0,
                         "",
                         prm.magic,
                         0,
                             Red)!= -1) 
                             {
                     m_trade=TRADE_SELL;
                     m_status=STATUS_TRADE;
                     }
                      else 
                     {
                     // error sending order
                    }
                 }
              }
            }
             else if (prev_trade==TRADE_SELL) 
            {
               // we trace forward for bar A, Bar B and signal 
                 for(fwd_bar=prev_bar; ((fwd_bar > 0) && (fwd_status!=STATUS_LATE)); fwd_bar --) 
                 {
                    if (fwd_status==STATUS_TRADE) 
                    {
                       if (Close[fwd_bar] > NormalizeDouble(m_vma[fwd_bar] +(prm.sig.pipsBarA * Point), Digits)) 
                       {
                        fwd_status =STATUS_BAR_A;
                        fwd_close_a=Close[fwd_bar];
                        Print("Trade Sell, found Bar A at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                       }
                     }
                      else if (fwd_status==STATUS_BAR_A) 
                     {
                          if (Close[fwd_bar] > NormalizeDouble(fwd_close_a + (prm.sig.pipsBarB * Point), Digits)) 
                          {
                           fwd_status=STATUS_BAR_B;
                           fwd_high_b=High[fwd_bar];
                           fwd_order_price=NormalizeDouble(fwd_high_b + (prm.sig.pipsTrade * Point), Digits);
                           Print("Trade Sell, found Bar B at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                           }
                            else if (Close[fwd_bar]<=NormalizeDouble(m_vma[fwd_bar] - (prm.sig.pipsBarA * Point), Digits)) 
                           {
                              // reset the state
                              fwd_status=STATUS_TRADE;
                              Print("Trade Buy, A Reset at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                             }
                        }
                         else if (fwd_status==STATUS_BAR_B) 
                        {
                        // check if it is time to entry or it is too late to send an order
                        // we found the last bar, and close[0] is the highest price and it is hit the entry position line
                          if (High[fwd_bar] > NormalizeDouble(fwd_order_price + (prm.entry.diff * Point), Digits))
                          {
                           fwd_status=STATUS_LATE;
                           Print("Too Late to entry ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                           break;
                           }
                            else if (Close[fwd_bar]<=NormalizeDouble(m_vma[fwd_bar] - (prm.sig.pipsBarA * Point), Digits)) 
                           {                           
                              // reset the state
                              fwd_status=STATUS_TRADE;
                              Print("Trade Buy, B Reset at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                             }
                       }
                 }
                 if (fwd_status==STATUS_BAR_B) 
                 {
                  Print("Waiting for signal ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS), " price at ", Close[0], " price range ", fwd_order_price, " - " , NormalizeDouble(fwd_order_price + (prm.entry.diff * Point), Digits));
                    if ((Close[0]>=fwd_order_price) && (Close[0]<=NormalizeDouble(fwd_order_price + (prm.entry.diff * Point), Digits))) 
                    {
                     Print("SIGNAL at ", TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS));
                     // it is  time to entry
                     stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_LOW, prm.mastop.shift);
                     stoploss=NormalizeDouble(stoploss + (prm.stop.diff * Point), Digits);
                     // open the new order
                     if (OrderSend(
                            symbol,
                            OP_BUY,
                            get_lots(),
                            Ask,
                            prm.slippage,
                            stoploss,
                            0,
                            "",
                            prm.magic,
                            0,
                                Blue)!= -1) 
                                {
                        m_trade=TRADE_BUY;
                        m_status=STATUS_TRADE;
                        }
                         else 
                        {
                        // error sending order
                       }
                    }
                 }
              }
         }
          else if (m_trade==TRADE_BUY) 
         {
            // Hit SL when perform the BUY order.
            stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_HIGH, prm.mastop.shift);
            stoploss=NormalizeDouble(stoploss + (prm.stop.diff * Point), Digits);
            /// open the new order 
            if (OrderSend(
                  symbol,
                  OP_SELL,
                  get_lots(),
                  Bid,
                  prm.slippage,
                  stoploss,
                  0,
                  "",
                  prm.magic,
                  0,
                      Red)!= -1) 
                      {
               m_trade=TRADE_SELL;
               m_status=STATUS_TRADE;
               }
                else 
               {
               // error sending order
              }
            }
             else if (m_trade==TRADE_SELL) 
            {
            // Hit SL when perform the BUY order.
            stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_LOW, prm.mastop.shift);
            stoploss=NormalizeDouble(stoploss - (prm.stop.diff * Point), Digits);
            // open the new order
            if (OrderSend(
                  symbol,
                  OP_BUY,
                  get_lots(),
                  Ask,
                  prm.slippage,
                  stoploss,
                  0,
                  "",
                  prm.magic,
                  0,
                      Red)!= -1) 
                      {
               m_trade=TRADE_BUY;
               m_status=STATUS_TRADE;
               }
                else 
               {
               // error sending order
              }
           }
      }
       else 
      {
      bool order_sent=false;
        if (m_trade==TRADE_NONE) 
        {
         // it is too late to perform a trade action
         // manual intervention is required.
        }
        if (m_trade==TRADE_BUY) 
        {
         // perform this action every open bar
           if (m_status==STATUS_BAR_B) 
           {
            /* real time check for the rade position */
              if (Close[0]<=m_order_price) 
              {
               // close current buy order and open new sell order
                 for(i=OrdersTotal() - 1; i>=0; i --) 
                 {
                    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
                    {
                       if ((OrderSymbol()==symbol) && (OrderMagicNumber()==prm.magic)) 
                       {
                        OrderClose(OrderTicket(), OrderLots(), Close[0], prm.slippage, Blue);
                       }
                    }
                 }
               stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_HIGH, prm.mastop.shift);
               stoploss=NormalizeDouble(stoploss + (prm.stop.diff * Point), Digits);
               /* open the new order */
               if (OrderSend(
                     symbol,
                     OP_SELL,
                     get_lots(),
                     Bid,
                     prm.slippage,
                     stoploss,
                     0,
                     "",
                     prm.magic,
                     0,
                         Red)!=-1) 
                         {
                  m_trade=TRADE_SELL;
                  m_status=STATUS_TRADE;
                  }
                   else 
                  {
                  m_trade=TRADE_NONE;
                  m_status=STATUS_NONE;
                 }
               order_sent=true;
              }
           }
           if ((m_trade==TRADE_BUY) && (!order_sent) && (is_open_tick(ts, period))) 
           {
            // modify stop lost every open tick 
            stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_LOW, prm.mastop.shift);
            stoploss=NormalizeDouble(stoploss - (prm.stop.diff * Point), Digits);
              for(i=OrdersTotal() - 1; i>=0; i--) 
              {
                 if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
                 {
                    if ((OrderSymbol()==symbol) && (OrderMagicNumber()==prm.magic)) 
                    {
                     OrderModify(OrderTicket(), OrderOpenPrice(), stoploss, OrderTakeProfit(), OrderExpiration(), Aqua);
                    }
                 }
              }
           }
           if ((m_trade==TRADE_BUY) && (!order_sent) && (is_open_tick(ts, period))) 
           {
            // check if the SELL signal is trigerred
              if (Close[1] > m_vma[1]) 
              {
               m_status=STATUS_TRADE;
               show_message(MSG_TRADE,  "TRADE BUY");
               show_message(MSG_STATUS, "Bar on top of VMA line");
               show_message(MSG_ERROR,  "");
               }
                else 
               {
                 if (m_status==STATUS_TRADE) 
                 {
                    if (Close[1]<=NormalizeDouble(m_vma[1] - (prm.sig.pipsBarA * Point), Digits)) 
                    {
                     m_status=STATUS_BAR_A;
                     m_close_a=Close[1];
                     show_message(MSG_TRADE,  "TRADE BUY");
                     show_message(MSG_STATUS, "Bar A accepted, Waiting for Bar B");
                     show_message(MSG_ERROR,  "");
                    }
                  }
                   else if (m_status== STATUS_BAR_A) 
                  {
                       if (Close[1]>=NormalizeDouble(m_vma[1] + (prm.sig.pipsBarA * Point), Digits)) 
                       {
                        m_status=STATUS_TRADE;
                        m_close_a=0;
                        show_message(MSG_TRADE,  "TRADE BUY");
                        show_message(MSG_STATUS, "Sell Signal on Bar A was reset");
                        show_message(MSG_ERROR,  "");
                        }
                         else if (Close[1]<=m_close_a - (prm.sig.pipsBarB * Point)) 
                        {
                           m_status=STATUS_BAR_B;
                           m_high_b=Low[1];
                           m_order_price=NormalizeDouble(m_high_b - (prm.sig.pipsTrade  * Point), Digits);
                           show_message(MSG_TRADE,  "TRADE BUY");
                           show_message(MSG_STATUS, "Bar B accepted, Waiting the Sell position at");
                           show_message(MSG_ERROR,  "");
                          }
                     }
                      else if (m_status== STATUS_BAR_B) 
                     {
                       if (Close[1]>=NormalizeDouble(m_vma[1] + (prm.sig.pipsBarA * Point), Digits)) 
                       {
                        m_status=STATUS_TRADE;
                        show_message(MSG_TRADE,  "TRADE BUY");
                        show_message(MSG_STATUS, "Sell Signal on Bar B was reset");
                        show_message(MSG_ERROR,  "");
                       }
                    }
              }
           }
         }
          else if (m_trade==TRADE_SELL) 
         {
            // perform this action every open bar
              if (m_status==STATUS_BAR_B) 
              {
            /* real time check for the rade position */
                 if (Close[0]>=m_order_price) 
                 {
                  // close current buy order and open new sell order
                    for(i=OrdersTotal() - 1; i>=0; i --) 
                    {
                       if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
                       {
                          if ((OrderSymbol()==symbol) && (OrderMagicNumber()==prm.magic)) 
                          {
                           OrderClose(OrderTicket(), OrderLots(), Ask, prm.slippage, Red);
                          }
                       }
                    }
                  stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_LOW, prm.mastop.shift);
                  stoploss=NormalizeDouble(stoploss - (prm.stop.diff * Point), Digits);
               /* open the new order */
                  if (OrderSend(
                        symbol,
                        OP_BUY,
                        get_lots(),
                        Ask,
                        prm.slippage,
                        stoploss,
                        0,
                        "",
                        prm.magic,
                        0,
                            Blue)!=-1) 
                            {
                     m_trade=TRADE_BUY;
                     m_status=STATUS_TRADE;
                     }
                      else 
                     {
                     m_trade=TRADE_NONE;
                     m_status=STATUS_NONE;
                    }
                  order_sent=true;
                 }
              }
              if ((m_trade==TRADE_SELL) && (!order_sent) && (is_open_tick(ts, period))) 
              {
               // modify stop lost every open tick 
               stoploss=iMA(symbol, period, prm.mastop.period, prm.mastop.mashift, prm.mastop.method, PRICE_HIGH, prm.mastop.shift);
               stoploss=NormalizeDouble(stoploss + (prm.stop.diff * Point), Digits);
                 for(i=OrdersTotal() - 1; i>=0; i--) 
                 {
                    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
                    {
                       if ((OrderSymbol()==symbol) && (OrderMagicNumber()==prm.magic)) 
                       {
                        OrderModify(OrderTicket(), OrderOpenPrice(), stoploss, OrderTakeProfit(), OrderExpiration(), Aqua);
                       }
                    }
                 }
              }
              if ((m_trade==TRADE_SELL) && (!order_sent) && (is_open_tick(ts, period))) 
              {
               // check if the SELL signal is trigerred
                 if (Close[1] < m_vma[1]) 
                 {
                  m_status=STATUS_TRADE;
                  show_message(MSG_TRADE,  "TRADE SELL");
                  show_message(MSG_STATUS, "Bar below VMA line");
                  show_message(MSG_ERROR,  "");
                  }
                   else 
                  {
                    if (m_status==STATUS_TRADE) 
                    {
                       if (Close[1]>=NormalizeDouble(m_vma[1] + (prm.sig.pipsBarA * Point), Digits)) 
                       {
                        m_status=STATUS_BAR_A;
                        m_close_a=Close[1];
                        show_message(MSG_TRADE,  "TRADE SELL");
                        show_message(MSG_STATUS, "Bar A accepted, Waiting for Bar B");
                        show_message(MSG_ERROR,  "");
                       }
                     }
                      else if (m_status== STATUS_BAR_A) 
                     {
                          if (Close[1]<=NormalizeDouble(m_vma[1] - (prm.sig.pipsBarA * Point), Digits)) 
                          {
                           m_status=STATUS_TRADE;
                           m_close_a=0;
                           show_message(MSG_TRADE,  "TRADE SELL");
                           show_message(MSG_STATUS, "Buy Signal on Bar A was reset");
                           show_message(MSG_ERROR,  "");
                           }
                            else if (Close[1]>=m_close_a + (prm.sig.pipsBarB * Point)) 
                           {
                              m_status=STATUS_BAR_B;
                              m_high_b=High[1];
                              m_order_price=NormalizeDouble(m_high_b + (prm.sig.pipsTrade  * Point), Digits);
                              show_message(MSG_TRADE,  "TRADE SELL");
                              show_message(MSG_STATUS, "Bar B accepted, Waiting the buy position at");
                              show_message(MSG_ERROR,  "");
                             }
                        }
                         else if (m_status== STATUS_BAR_B) 
                        {
                          if (Close[1] < NormalizeDouble(m_vma[1] - (prm.sig.pipsBarA * Point), Digits)) 
                          {
                           m_status=STATUS_TRADE;
                           show_message(MSG_TRADE,  "TRADE SELL");
                           show_message(MSG_STATUS, "Buy Signal on Bar B was reset");
                           show_message(MSG_ERROR,  "");
                          }
                       }
                 }
              }
           }
     }
   set_ts(ts);
   return(0);
  }
//+------------------------------------------------------------------+