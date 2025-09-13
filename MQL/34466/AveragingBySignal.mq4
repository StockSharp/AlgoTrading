//+------------------------------------------------------------------+
//|                                            AveragingBySignal.mq4 |
//|                                     Copyright 2021, Signal Forex |
//|                                           https://signalforex.id |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Signal Forex"
#property link      "https://signalforex.id"
#property version   "3.00"
#property strict

/*
   21 April 2021, Versi 3.00 
   EA ini dibuat untuk tujuan belajar code MQL4
   Tidak diperuntukan dalam trading.
   Bila ingin menggunakan dalam trading, harus sesuaikan kembali indicator yang digunakan untuk mencari signal entry
   TP, jarak averaging dan lain sebagainya perlu disesuaikan kembali sesuai dengan strategy trading anda.
   
   
   Group Sharing : t.me/codeMQL
   contact : t.me/AutoBotFX
   
*/

enum  enumONFF {
   eTurnOn,    //Aktifkan
   eTurnOff,   //Non Aktifkan
};
enum enumLotType{
   eFixLot,    //Fix LotSize
   eMulti,     //Multiplier
};

input string strTrading = "___________________";   //---------- Lot Setting ----------------
input double      IN_LotSize  = 0.10;     //Init Lot Size
input enumLotType IN_LotType  = eMulti;   //Lot Type 
input double      IN_LotX     = 2.0;      //Multiplier

input string strIndi = "___________________";   //---------- Indicator ----------------
input int   IN_PeriodFast  = 28;  //Period MA Fast
input ENUM_MA_METHOD IN_MAMethodFast = MODE_LWMA; //MA Method Fast

input int   IN_PeriodSlow  = 50; //Period MA Slow
input ENUM_MA_METHOD IN_MAMethodSlow = MODE_SMMA; //MA Method Slow

input string strTPSL = "___________________";   //---------- Target Profit ----------------
input int  IN_TakeProfit  = 15; //Take Profit (pips) 0: off
//next update: input int  IN_TargetMoney = 0; //Target Money ($) 0: off
//next update: input int  IN_TargetCL    = 0; //Max Loss (CutLoss) ($) 0: off

input string strAveraging = "_________________"; //----------- Averaging -----------
input enumONFF IN_IsBySignal  = eTurnOn;     //Averaging by Signal
input int   IN_JarakLayer     = 10;          //Jarak Layer (pips)
input int   IN_MaxLayer       = 10;          //Max Layer

input string strTrailingStop = "__________________"; //------------ Trailing Stop --------------
input  enumONFF IN_IsTrailingStop = eTurnOff;  //Trailing Stop
input  int  IN_TrailingStart  = 10; //Trailing Start
input  int  IN_TrailingStep   = 1;  //Trailing Step

input int   slippage       = 3;  //Slippage

//Variabel Global
int      GDigit         = 0;
double   GPoint         = 0.0;
int      GMagicNumber   = 0;

int   IDEA  = 10; //ID EA
string arrPairs[][2] = {  
                           {"GBPUSD", "01"} ,  {"EURUSD", "02"} ,  {"USDJPY", "03"} ,  {"USDCAD", "04"} ,  {"USDCHF", "05"} ,
                           {"NZDUSD", "06"} ,  {"AUDUSD", "07"} ,  {"XAUUSD", "08"}
                           //We can add other pairs
                        };


class cOrder{
   protected:
      int   tOrders, type;
      double   harga, hargaTA, hargaTB;
      double   lot, lotTKcl, lotTBsr;
      datetime opTime;
      bool     status;
   public:
      cOrder(void);
      void updateOrder(int orderType, double price, double lotSize, datetime time, bool isInc, bool isUpdate);
      void updateStatus(bool isUpdate){ status=isUpdate;};
      int getTtlOrders(){ return (tOrders); }
      double getHargaTA(){ return (NormalizeDouble(hargaTA, GDigit));}
      double getHargaTB(){ return (NormalizeDouble(hargaTB, GDigit));}
      bool   getStatus(){ return(status); }
   
};

cOrder::cOrder(void){
   tOrders = 0;
   type = -1;
   harga = hargaTA = hargaTB = 0.0;
   lot = lotTKcl = lotTBsr = 0.0;
   opTime = 0;
   status =false;
};

void cOrder::updateOrder(int orderType, double price, double lotSize, datetime time, bool isInc, bool isUpdate){
   if (isInc) tOrders++;
   status   = isUpdate;
   type  = orderType;
   if (opTime < time){
      harga = price;
      lot   = lotSize;
      opTime= time;
   }
   if (price > hargaTA) hargaTA = price;
   if (price < hargaTB || hargaTB==0.0) hargaTB = price;
   if (lotSize > lotTBsr) lotTBsr = lotSize;
   if (lotSize < lotTKcl || lotTKcl==0) lotTKcl = lotSize;
};


class cOrderTPSL{
   private:
      double tLot, tValue;
      bool   isUpdate;
   public:
      cOrderTPSL(){
         tLot = tValue = 0.0;
         isUpdate = false;
      }
      void updateData(double opPrice, double lotSize, bool status);
      double getBEP();
      bool getStatus(){ return(isUpdate); }
      void updateStatus(bool status=false){ isUpdate = status; }
};

void cOrderTPSL::updateData(double opPrice, double lotSize, bool status){
   tLot  += lotSize;
   tValue+= opPrice * lotSize;
   //isUpdate = status;
   if (!isUpdate) isUpdate = status;
}

double cOrderTPSL::getBEP(){
   return(NormalizeDouble(tValue/tLot, GDigit));
}


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  { 
   if(IN_PeriodFast >= IN_PeriodSlow || IN_TakeProfit < 0)
     {
      Alert("invalid input");
      return (INIT_PARAMETERS_INCORRECT);
     }
  
  //Validasi lainnya bisa ditambahkan sesuai kebutuhan.
  //seperti validasi utk trailing stop, dll
   
   
   string pair =  Symbol();
   GDigit = (int) MarketInfo(pair, MODE_DIGITS);
   GPoint = MarketInfo(pair, MODE_POINT);
   if(GDigit%2==1)GPoint *= 10;
   GMagicNumber =   GenerateMagicNumber();
   
   return(INIT_SUCCEEDED);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   bool isNewCS   = newCandle();
   
   //Cari Signal buy/sell
   int signal = -1;
   if (isNewCS==true)signal = getSignal();
   
   cOrder buy, sell;
   //Open Order sesuai dengan signal
   ManageOrders(signal, buy, sell);
   
   setTPSLMarti();
   
   //Set Trailing Stop
   setTrailingStop(buy, sell);
   
   
   //Set Trailing Stop for Floating Orders (Averaging)
      //Next Update
   
   //Daily Close
      //next update
   
   //Target Daily / Max Loss Daily
      //next Update

  }
//+------------------------------------------------------------------+

int getSignal(){
   int signal = -1; 
   
   double emaFast1   = iMA(Symbol(), PERIOD_CURRENT, IN_PeriodFast, 0, IN_MAMethodFast, PRICE_CLOSE, 1);
   double emaFast2   = iMA(Symbol(), PERIOD_CURRENT, IN_PeriodFast, 0, IN_MAMethodFast, PRICE_CLOSE, 2);
   double emaSlow1   = iMA(Symbol(), PERIOD_CURRENT, IN_PeriodSlow, 0, IN_MAMethodSlow, PRICE_CLOSE, 1);
   double emaSlow2   = iMA(Symbol(), PERIOD_CURRENT, IN_PeriodSlow, 0, IN_MAMethodSlow, PRICE_CLOSE, 2);
   //Print (emaFast2 , " < " , emaSlow2 , " && " , emaFast1 , " >= ", emaSlow1);
   if (emaFast2 < emaSlow2 && emaFast1 >= emaSlow1)
      signal = OP_BUY;
   else if (emaFast2 > emaSlow2 && emaFast1 <= emaSlow1)
      signal = OP_SELL;
   
   return (signal);
}

void ManageOrders(int orderType, cOrder &buy, cOrder &sell){
      
      int tOrder = OrdersTotal();
      for (int i=tOrder-1; i>=0; i--){
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true){
            if (OrderMagicNumber() == GMagicNumber /* && OrderSymbol() == Symbol() */  ){
               bool status = (OrderTakeProfit() == 0.0);
               if (OrderType() == OP_BUY)buy.updateOrder(OrderType(), OrderOpenPrice(), OrderLots(), OrderOpenTime(), true, status);
               if (OrderType() == OP_SELL)sell.updateOrder(OrderType(), OrderOpenPrice(), OrderLots(), OrderOpenTime(), true, status);
            } //end magicNumber
         }
      } //end for
      
      //open order
      double vol  = getLotSize(0);
      double hargaOP = 0.0, sl = 0.0, tp = 0.0;
      bool isAllowOP = false;
      
      if (orderType == OP_BUY && buy.getTtlOrders()==0) {
         hargaOP = Ask;
         isAllowOP = true;
      }else if (orderType == OP_SELL && sell.getTtlOrders() == 0){
         hargaOP = Bid;
         isAllowOP = true;
      }
      
      if (isAllowOP == true){
         if (!cekMargin(orderType, vol)){
            Print ("Margin tidak cukup, open order dibatalkan");
         }else{
            int ticket = OrderSend(Symbol(), orderType, vol, hargaOP, slippage, sl, tp, "SignalForex.id", GMagicNumber );
            if (ticket > 0) 
               Print ("Open Order Sukses");
            else 
               Print ("Open Order gagal");
         }
      }
      // end open order
      
      
      //MARTINGALE / AVERAGING
      double hargaOpen = 0.0;
      //BUY
      if (buy.getTtlOrders() >0 && buy.getTtlOrders() < IN_MaxLayer && (orderType == OP_BUY || IN_IsBySignal == eTurnOff) ) {
         hargaOpen = buy.getHargaTB() - (IN_JarakLayer * GPoint);
         if (hargaOpen >= Ask){
            vol = getLotSize(buy.getTtlOrders()); //lotSize;
            //cek Free Margin cukup atau tidak?
            if (!cekMargin(OP_BUY, vol))
               Print ("Margin tidak cukup, open order dibatalkan");
            else 
               bool hsl = OrderSend(Symbol(), OP_BUY, vol, Ask, slippage, 0.0, 0.0, "", GMagicNumber);
         }
      }
      
      //SELL
      if (sell.getTtlOrders() >0 && sell.getTtlOrders() < IN_MaxLayer && (orderType == OP_SELL || IN_IsBySignal == eTurnOff) ) {
         hargaOpen = sell.getHargaTA() + (IN_JarakLayer * GPoint);
         if (hargaOpen <= Bid){
            vol = vol = getLotSize(sell.getTtlOrders()); //lotSize;
            if (!cekMargin(OP_SELL, vol))
               Print ("Margin tidak cukup, open order dibatalkan");
            else
               bool hsl = OrderSend(Symbol(), OP_SELL, vol, Bid, slippage, 0.0, 0.0, "", GMagicNumber);
         }
         
      }
      
}


void setTrailingStop(cOrder &buy, cOrder &sell){
   if (IN_IsTrailingStop == eTurnOn && ( buy.getTtlOrders() <= 1 || sell.getTtlOrders() <= 1)){
      double sl = 0.0;
      int tOrders = OrdersTotal();
      for (int i=tOrders-1; i>=0; i--){
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)){
            if (OrderMagicNumber() == GMagicNumber){
               if (OrderType() == OP_BUY && buy.getTtlOrders() == 1){
                  if (  (Bid - (IN_TrailingStart * GPoint)) >= OrderOpenPrice() 
                        && (Bid - ((IN_TrailingStart+IN_TrailingStep)*GPoint)) > OrderStopLoss()
                     )
                  {
                     sl = NormalizeDouble(Bid - (IN_TrailingStart * GPoint), GDigit);
                     if (OrderModify (OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, clrGold))
                        Print ("Trailing sukses");
                  }
               }
               if (OrderType() == OP_SELL && sell.getTtlOrders() == 1){
                  if (  (Ask + (IN_TrailingStart * GPoint)) <= OrderOpenPrice() 
                        && ( (Ask + ((IN_TrailingStart+IN_TrailingStep)*GPoint)) < OrderStopLoss() || OrderStopLoss() == 0)
                     )
                  {
                     sl = NormalizeDouble(Ask + (IN_TrailingStart * GPoint), GDigit);
                     if (OrderModify (OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, clrGold))
                        Print ("Trailing sukses");
                  }
               }
            }  //end magic number
         }
      }
   }
}


void setTPSLMarti(){
   
   cOrderTPSL BUY, SELL;
   double tp = 0.0, sl = 0.0;
   int tOrders = OrdersTotal();
   for (int i=tOrders-1; i>=0; i--){
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)){
         if (OrderMagicNumber() == GMagicNumber){
            if (OrderType() == OP_BUY)BUY.updateData(OrderOpenPrice(), OrderLots(), (OrderTakeProfit() == 0));
            if (OrderType() == OP_SELL)SELL.updateData(OrderOpenPrice(), OrderLots(), (OrderTakeProfit() == 0) );
         } //end magic Number
      }//end select
   } //end for


   if (BUY.getStatus() || SELL.getStatus()){
      double tpBuy =0.0, tpSell = 0.0;
      
      if (BUY.getStatus()) tpBuy    = BUY.getBEP() + (IN_TakeProfit * GPoint);
      if (SELL.getStatus())tpSell   = SELL.getBEP() - (IN_TakeProfit * GPoint);
      
      tOrders = OrdersTotal();
      for (int i=tOrders-1; i>=0; i--){
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)){
            if (OrderMagicNumber() == GMagicNumber){
               if (OrderType() == OP_BUY && BUY.getStatus()){
                  if (!OrderModify(OrderTicket(), OrderOpenPrice(), OrderStopLoss(), tpBuy, 0, clrNONE)){
                     Print ("Warning, update gagal");
                  }
               }
               if (OrderType() == OP_SELL && SELL.getStatus()){
                  if (!OrderModify(OrderTicket(), OrderOpenPrice(), OrderStopLoss(), tpSell, 0, clrNONE)){
                     Print ("Warning, update gagal");
                  }
               } 
            } //end magic Number
         }//end select
      } //end for
      
      BUY.updateStatus();
      SELL.updateStatus();
   }
   
}


int   GenerateMagicNumber(){
   int magicNumber = (int) StringToInteger( IntegerToString(IDEA) + getCodePair(Symbol()));
   return (magicNumber);
}

string getCodePair(string pair){
   string codePair = "00";
   int size = ArrayRange(arrPairs, 0);
   //pair = ".GBPUSDm";
   for (int i=0; i< size; i++){
      if (StringFind(pair, arrPairs[i][0], 0) >= 0 ){
         codePair = arrPairs[i][1];
         break;
      }
   }
   return (codePair);
}


double getLotSize(int tOrderType){
   double lot = 0.0;
   
   
   
   if (IN_LotType == eMulti)
      lot = IN_LotSize * MathPow(IN_LotX, tOrderType);
   else
      lot   = IN_LotSize;
      
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double lotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
   
   lot = MathRound(lot / lotStep + 0.0001) * lotStep;
   lot = MathMin(MathMax(lot, minLot), maxLot);
   
   lot = NormalizeDouble(lot, 2);
   
   return (lot);
}

bool cekMargin(int orderType, double lot){
   if (AccountFreeMarginCheck(Symbol(), orderType, lot) <=0 || GetLastError()==134 ){
      return (false);
   }
   return(true);
}


bool newCandle(){
   bool isNewCS = false;
   static datetime opTime  = TimeCurrent();
   if (iTime(Symbol(), 0, 0) > opTime){
      opTime = iTime(Symbol(), 0, 0);
      isNewCS = true;
   }
   return (isNewCS);
}

