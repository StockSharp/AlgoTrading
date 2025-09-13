//+------------------------------------------------------------------+
//|                                                      MACross.mq4 |
//|                                     Copyright 2020, Signal Forex |
//|                                           https://signalforex.id |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, Signal Forex"
#property link      "https://signalforex.id"
#property version   "1.00"
#property strict

//--- input parameters
input    int      period_ma_fast = 8;  //Period Fast MA
input    int      period_ma_slow = 20; //Period Slow MA

input    double   takeProfit  = 20.0;  //Take Profit (pips)
input    double   stopLoss    = 20.0;  //Stop Loss (pips)

input    double   lotSize     = 1.00;  //Lot Size
input    double   minEquity   = 100.0; //Min. Equity ($)

input    int Slippage = 3;       //Slippage
input    int MagicNumber = 889;  //Magic Number

//Variabel Global
double   myPoint    = 0.0;
int      mySlippage = 0;
int      BuyTicket   = 0;
int      SellTicket  = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   //validasi input, sebaiknya kita selalu melakukan validasi pada initialisasi data input
   if (period_ma_fast >= period_ma_slow || takeProfit < 0.0 || stopLoss < 0.0 || lotSize < 0.01 || minEquity < 10){
      Alert("WARNING - Input data inisial tidak valid");
      return (INIT_PARAMETERS_INCORRECT);
   }
   
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(lotSize<min_volume)
   {
      string pesan =StringFormat("Volume lebih kecil dari batas yang dibolehkan yaitu %.2f",min_volume);
      Alert (pesan);
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   myPoint = GetPipPoint(Symbol());
   mySlippage = GetSlippage(Symbol(),Slippage);

   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print ("EA telah diberhentikan");
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   if (cekMinEquity()){
      
      
      int signal = -1;
      bool isNewCandle = NewCandle(Period(), Symbol());
      
      signal = getSignal(isNewCandle);
      transaction(isNewCandle, signal);
      
      
   }else{
      //Stop trading, karena equity tidak cukup
      Print ("EA akan segera diberhentikan karena equity tidak mencukup");
   }
}

void transaction(bool isNewCandle, int signal){
   if (isNewCandle==false) return;
   
   int   tOrder = 0;
   int   tOrderBuy = 0, tOrderSell = 0;
   string   strMN = "", pair = "";
   int   tiketBuy = 0, tiketSell = 0;
   double   lotBuy = 0.0, lotSell = 0.0;
   
   pair = Symbol();
   
   tOrder = OrdersTotal();
   for (int i=tOrder-1; i>=0; i--){
      bool hrsSelect = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      strMN = IntegerToString(OrderMagicNumber());
      if (StringFind(strMN, IntegerToString(MagicNumber), 0) == 0 && StringFind(OrderSymbol(), pair, 0) == 0 ){
         if (OrderType() == OP_BUY){
            tOrderBuy++;
            tiketBuy = OrderTicket();
            lotBuy   = OrderLots();
         }
         if (OrderType() == OP_SELL){
            tOrderSell++;
            tiketSell = OrderTicket();
            lotSell   = OrderLots();
         }
         
      }//end if magic number && pair
      
   }//end for
   
   double lot = 0.0;
   double hargaOP = 0.0;
   double sl = 0.0, tp = 0.0;
   int    tiket = 0;
   int    orderType = -1;
   
   //Open pertama kali
   if (signal == OP_BUY && tOrderBuy == 0){
      lot = getLotSize();
      orderType = signal;
      hargaOP = Ask;
      tiket = OrderSend(Symbol(), orderType, lot, hargaOP, mySlippage, sl, tp, "OP BUY", MagicNumber, 0, clrBlue);
      if (tiketSell > 0){
         if (OrderClose(tiketSell, lotSell, Ask, mySlippage, clrRed)){
            Print ("Close successful");
         }
      }
   }else if (signal == OP_SELL && tOrderSell == 0){
      lot = getLotSize();
      orderType = signal;
      hargaOP = Bid;
      tiket = OrderSend(Symbol(), orderType, lot, hargaOP, mySlippage, sl, tp, "OP SELL", MagicNumber, 0, clrRed);
      if (tiketBuy > 0){
         if (OrderClose(tiketBuy, lotBuy, Bid, mySlippage, clrRed)){
            Print ("Close successful");
         }
      }
   }
   
}

int getSignal(bool isNewCandle){
   int signal = -1;
   
   if (isNewCandle==true){
      //Moving Averages
      double maFast1 = iMA(NULL, 0, period_ma_fast, 0, MODE_SMA, 0, 1);
      double maSlow1 = iMA(NULL, 0, period_ma_slow, 0, MODE_SMA, 0, 1);
      double maFast2 = iMA(NULL, 0, period_ma_fast, 0, MODE_SMA, 0, 2);
      double maSlow2 = iMA(NULL, 0, period_ma_slow, 0, MODE_SMA, 0, 2);
      
      if(maFast2 <= maSlow2 && maFast1 > maSlow1){
         signal = OP_BUY;
      }else if(maFast2 >= maSlow2 && maFast1 < maSlow1){
         signal = OP_SELL;
      }
   }
   
   return (signal);
}

double getLotSize(){
   double lot = 0.0;
   lot = NormalizeDouble(lotSize, 2);
   return (lot);
}

//fungsi tambahan untuk cek equity minimum
bool cekMinEquity(){
   bool valid = false;
   double equity = 0.0;
   equity = AccountEquity();
   
   if (equity > minEquity){
      valid = true;
   }
   return (valid);
}

// Fungsi GetPipPoint
double GetPipPoint(string pair)
{
   double point= 0.0;
   int digits = (int) MarketInfo(pair, MODE_DIGITS);
   if(digits == 2 || digits== 3) point= 0.01;
   else if(digits== 4 || digits== 5) point= 0.0001;
   return(point);
}

// Fungsi GetSlippage
int GetSlippage(string pair, int SlippagePips)
{
   int slippage = 0;
   int digit = (int) MarketInfo(pair,MODE_DIGITS);
   if(digit == 2 || digit == 4) slippage = SlippagePips;
   else if(digit == 3 || digit == 5) slippage = SlippagePips * 10;
   return(slippage );
}

bool NewCandle(int tf, string pair = "" ){
   bool isNewCS  = false;
   static datetime prevTime   = TimeCurrent();
   if (pair == "") pair = Symbol();
   if (prevTime < iTime(pair, tf, 0)){
      isNewCS  = true;
      prevTime = iTime(pair, tf, 0);
   }
   return isNewCS;
}
