//+------------------------------------------------------------------+
//|                                         Smart Trend Follower.mq5 |
//|                                         Copyright 2024, Robotop. |
//|                                        https://www.robotop.my.id |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Robotop."
#property link      "https://www.robotop.my.id"
#property version   "1.00"

#property description   "Mengikuti arah tren pasar secara cerdas menggunakan kombinasi sinyal Moving Average dan Stochastic Oscillator"
#property description   "My Telegram : @AutoBotFX"
#property description   "Code MQL : https://t.me/codeMQL"


enum enumJnsSignal{
   eTypeCrossMA,  //Cross 2 MA
   eTypeTrend,    //Follow Trend
};

enum enumOrderType{
   eBuy,    //BUY
   eSell,   //SELL
   eNone = 99, //None
};

input    int         inMagicNumber  = 778899;   //Magic Number
input    double      inLotSize      = 0.01;     //Initial Lot Size
input    double      inMultiply     = 2.0;      //Multiplier
input    int         inJarakLayer   = 200;       //Jarak Layer (pips)

input    enumJnsSignal  inJnsSignal = eTypeCrossMA;    //Signal Type

input    string      strMA          = "===================";      //Moving Average
input    int         inMAPeriodFast = 14;       //MA Fast
input    int         inMAPeriodSlow = 28;       //MA Slow

input    string      strStochastic  = "===================";      //Stochastic Oscillator
input    int         inSTOKPeriod   = 10;       //Sto %K
input    int         inSTODPeriod   = 3;       //Sto %D
input    int         inSTOSlowing   = 3;       //Slowing

input    string      strExit        = "===================";       //Exit Management
input    int         inTakeProfit   = 500;      //Take Profit
input    int         inStopLoss     = 0;       //Stop Loss


struct dataTrades{
   int ttlPos;
   double hargaTA, hargaTB;
   double ttlValue, ttlLot;
   void initial(){
      ttlPos   = 0;
      hargaTA  = hargaTB = 0.0;
      ttlValue = ttlLot = 0.0;
   }
};

int gMAFastHandle, gMASlowHandle, gSTOHandle;
dataTrades trades[2];

double gLotMin, gLotMax,gLotStep, gLotLimit;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{

   if (inLotSize < 0.01){
      Alert ("Initial Lot Size tidak boleh lebih kecil dari 0.01");
      return (INIT_PARAMETERS_INCORRECT);
   }
   
   if (inMAPeriodFast >= inMAPeriodSlow){
      Alert("WARNING:: Period Fast tidak boleh lebih kecil dari Period Slow");
      return (INIT_PARAMETERS_INCORRECT);
   }
   
   gLotMin  = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
   gLotMax  = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);
   gLotStep = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);
   gLotLimit= SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_LIMIT);
   
   
   gMAFastHandle  = iMA(Symbol(), PERIOD_CURRENT, inMAPeriodFast, 0, MODE_SMA, PRICE_CLOSE);
   gMASlowHandle  = iMA(Symbol(), PERIOD_CURRENT, inMAPeriodSlow, 0, MODE_SMA, PRICE_CLOSE);
   gSTOHandle     = iStochastic(Symbol(), PERIOD_CURRENT, inSTOKPeriod, inSTODPeriod, inSTOSlowing, MODE_SMA, STO_LOWHIGH);
   
   return(INIT_SUCCEEDED);
}
  
  
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print ("EA berhenti. Selamat belajar Code EA di MT5");
}
  
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
//---
   ENUM_ORDER_TYPE signal = -1;
   if (isNewCandle() ){
      signal = (ENUM_ORDER_TYPE)GetSignal();
   }
   
   if (signal >= 0){
      manageTrading(signal);
   }
   
   setTPSL();
}
//+------------------------------------------------------------------+


bool isNewCandle(){
   static datetime wktCandle = TimeCurrent();
   bool isNew = false;
   
   if (wktCandle < iTime(Symbol(), PERIOD_CURRENT, 0) ){
      isNew = true;
      wktCandle   = TimeCurrent();
   }
   return (isNew);
}

int GetSignal(){
   int signal = -1;
   
   if (inJnsSignal == eTypeCrossMA){
      //CrossMA
      double maFast[3], maSlow[3];
      CopyBuffer(gMAFastHandle, 0, 0, 3, maFast);
      CopyBuffer(gMASlowHandle, 0, 0, 3, maSlow);
      if (maFast[0] < maSlow[0] && maSlow[1] < maFast[1] ){
         signal   = POSITION_TYPE_BUY;
      }else if (maFast[0] > maSlow[0] && maSlow[1] > maFast[1] ){
         signal   = POSITION_TYPE_SELL;
      }
      
   }else if (inJnsSignal == eTypeTrend){
      //Trending by MA and Confirmed by Stochastic
      double maFast[1], maSlow[1], stoMain[1]; //, stoSignal[1];
      CopyBuffer(gMAFastHandle, 0, 1, 1, maFast);
      CopyBuffer(gMASlowHandle, 0, 1, 1, maSlow);
      CopyBuffer(gSTOHandle, MAIN_LINE, 1, 1, stoMain);
      //CopyBuffer(gSTOHandle, SIGNAL_LINE, 1, 1, stoSignal);
      
      MqlRates rate[];
      CopyRates(Symbol(), PERIOD_CURRENT, 1, 1, rate);
      
      double lvLow = 30.0, lvUp = 70.0;
      if (maFast[0] > maSlow[0] && rate[0].open < rate[0].close && stoMain[0] <= lvLow){
         signal = ORDER_TYPE_BUY;
      }else if (maFast[0] < maSlow[0] && rate[0].open > rate[0].close && stoMain[0] >= lvUp){
         signal = ORDER_TYPE_SELL;
      }
      
   }
   
   return (signal);
}

void manageTrading(ENUM_ORDER_TYPE signal){
   
   if (signal < 0) return;
   
   updateDataTrades();
   double lot  = 0.0;
   if (trades[signal].ttlPos == 0){
      lot   = getLotSize(signal, inLotSize, 0);
      if (lot>=gLotMin) openTrade(signal, lot);
   }else {
      lot   = getLotSize(signal, inLotSize, trades[signal].ttlPos);
      if (signal == ORDER_TYPE_BUY){
         if (trades[signal].hargaTB-(inJarakLayer * Point()) >= SymbolInfoDouble(Symbol(), SYMBOL_ASK) ){
            if (lot>=gLotMin) openTrade(signal, lot);
         }
      }else if (signal == ORDER_TYPE_SELL){
         if (trades[signal].hargaTA+(inJarakLayer * Point()) <= SymbolInfoDouble(Symbol(), SYMBOL_BID) ){
            if (lot>=gLotMin) openTrade(signal, lot);
         }
      }
      
   }
}


void updateDataTrades(){
   int tPos = PositionsTotal();
   trades[eBuy].initial();
   trades[eSell].initial();
   
   for (int i=tPos-1; i>=0; i--){
      if (PositionGetTicket(i) > 0 ){
         if (PositionGetString(POSITION_SYMBOL) == Symbol() && PositionGetInteger(POSITION_MAGIC) == inMagicNumber ){
            ENUM_POSITION_TYPE type = (ENUM_POSITION_TYPE) PositionGetInteger(POSITION_TYPE);
            if (trades[type].hargaTA < PositionGetDouble(POSITION_PRICE_OPEN)){
               trades[type].hargaTA = PositionGetDouble(POSITION_PRICE_OPEN);
            }
            if (trades[type].hargaTB > PositionGetDouble(POSITION_PRICE_OPEN) || trades[type].hargaTB == 0.0){
               trades[type].hargaTB = PositionGetDouble(POSITION_PRICE_OPEN);
            }
            trades[type].ttlPos++;
            trades[type].ttlLot  += PositionGetDouble(POSITION_VOLUME);
            trades[type].ttlValue   += PositionGetDouble(POSITION_PRICE_OPEN) * PositionGetDouble(POSITION_VOLUME);
         }
      }
   }
   
}

void openTrade(ENUM_ORDER_TYPE signal, double lotSize){
   MqlTradeRequest request = {};
   MqlTradeResult  result  = {};
   
   request.action = TRADE_ACTION_DEAL;
   request.symbol = Symbol();
   request.magic  = inMagicNumber;
   request.type   = signal;
   request.volume = lotSize;
   request.price  = (signal==ORDER_TYPE_BUY)?SymbolInfoDouble(Symbol(), SYMBOL_ASK) : SymbolInfoDouble(Symbol(), SYMBOL_BID);
   if (OrderSend(request, result)){
      Print ("Open berhasil");
   }else{
      Print ("Open Gagal, code: ", result.retcode);
   }

}

void setTPSL(){
   updateDataTrades();
   double bepBuy  = 0.0, bepSell = 0.0;
   double tpBuy = 0.0, slBuy = 0.0, tpSell = 0.0, slSell = 0.0;
   if (trades[eBuy].ttlPos > 0){
      bepBuy   = trades[eBuy].ttlValue / trades[eBuy].ttlLot;
      if (inTakeProfit > 0) tpBuy   = NormalizeDouble(bepBuy + (inTakeProfit * Point() ), Digits() );
      if (inStopLoss > 0) slBuy   = NormalizeDouble(bepBuy - (inStopLoss * Point() ), Digits() );
      
   }
   
   if (trades[eSell].ttlPos > 0){
      bepSell  = trades[eSell].ttlValue / trades[eSell].ttlLot;
      if (inTakeProfit > 0) tpSell   = NormalizeDouble(bepSell - (inTakeProfit * Point() ), Digits() );
      if (inStopLoss > 0) slSell   = NormalizeDouble(bepSell + (inStopLoss * Point() ), Digits() );
   }
   
   int tPos = PositionsTotal();
   for (int i=tPos-1; i>=0; i--){
      ulong ticket   = PositionGetTicket(i);
      //double tp = PositionGetDouble(POSITION_TP);
      //double sl = PositionGetDouble(POSITION_SL);
      if (ticket > 0 && PositionGetString(POSITION_SYMBOL) == Symbol() && PositionGetInteger(POSITION_MAGIC) == inMagicNumber ){
         if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY && (PositionGetDouble(POSITION_TP) != tpBuy || PositionGetDouble(POSITION_SL) != slBuy)){
            modifTPSL(ticket, slBuy, tpBuy);
         }else if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL && (PositionGetDouble(POSITION_TP) != tpSell || PositionGetDouble(POSITION_SL) != slSell)){
            modifTPSL(ticket, slSell, tpSell);
         }
      
      }
   }
   
}

void modifTPSL(ulong ticket, double sl, double tp){
   MqlTradeRequest request = {};
   MqlTradeResult  result = {};
   
   request.action = TRADE_ACTION_SLTP;
   request.position  = ticket;
   //request.symbol = Symbol();
   request.sl     = NormalizeDouble(sl, Digits() );
   request.tp     = NormalizeDouble(tp, Digits() );
   if (OrderSend(request, result) ){
      Print ("Modif TP/SL berhasil");
   }else{
      Print ("Modif TP/SL gagal. ErrCode: ", result.retcode);
   }
}

void validateLot(double &lot){
   lot= MathRound(lot / gLotStep) * gLotStep;
   lot   = (gLotMin > lot )? gLotMin : (gLotMax < lot) ? gLotMax : lot;
   lot   = NormalizeDouble(lot, 2);
}

double getLotSize(ENUM_ORDER_TYPE orderType, double initLotSize, int tPos, string pair=""){
   double lot = 0.0;
   pair = (pair == "")?Symbol() : pair;
   
   lot = initLotSize * MathPow(inMultiply, tPos);
   validateLot (lot);
   if (gLotLimit - (getTotalVolume(pair, orderType)+lot) < 0) lot = 0.0;
   
   lot = CheckMoneyForTrade(pair, lot, orderType)?lot : 0.0;
   
   return (lot);
}

double getTotalVolume(string symbol, int orderType)
{
   double tVol = 0.0;
   string pair;
   int type;
   int tPos = PositionsTotal();
   for (int pos=0; pos<tPos; pos++){
      ulong ticket = PositionGetTicket(pos);
      if (ticket > 0){
         pair  = PositionGetString(POSITION_SYMBOL);
         type = (ENUM_POSITION_TYPE) PositionGetInteger(POSITION_TYPE);
         if (pair == symbol && type == orderType){
            tVol  += PositionGetDouble(POSITION_VOLUME);
         }
      }
   }
   
   int tOrder = OrdersTotal();
   for (int ord=0; ord<tOrder; ord++){
      ulong ticket = OrderGetTicket(ord);
      if (ticket > 0){
         pair  = OrderGetString(ORDER_SYMBOL);
         type = (ENUM_ORDER_TYPE) OrderGetInteger(ORDER_TYPE);
         if (pair == symbol && (type % 2 == orderType) ){
            tVol  += PositionGetDouble(POSITION_VOLUME);
         }
      }
   }
   
   return (tVol);
}

bool CheckMoneyForTrade(string symb,double lots,ENUM_ORDER_TYPE type)
{
   //--- Getting the opening price
   MqlTick mqltick;
   SymbolInfoTick(symb,mqltick);
   double price=mqltick.ask;
   if(type==ORDER_TYPE_SELL)
   price=mqltick.bid;
   //--- values of the required and free margin
   double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   //--- call of the checking function
   if(!OrderCalcMargin(type,symb,lots,price,margin))
   {
      //--- something went wrong, report and return false
      Print("Error in ",__FUNCTION__," code=",GetLastError());
      return(false);
   }
   //--- if there are insufficient funds to perform the operation
   if(margin>free_margin)
   {
      //--- report the error and return false
      Print("Not enough money for ",EnumToString(type)," ",lots," ",symb," Error code=",GetLastError());
      return(false);
   }
   //--- checking successful
   return(true);
}