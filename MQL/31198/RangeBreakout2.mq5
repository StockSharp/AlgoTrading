#property copyright           "Mokara"
#property link                "https://www.mql5.com/en/users/mokara"
#property description         "Periodic Range Breakout"
#property version             "2.0"
#property script_show_inputs  true

#define MACIT 1248

enum WEEK_DAY{Monday=1, Tuesday, Wednesday, Thursday, Friday};
enum HOUR{H0, H1, H2, H3, H4, H5, H6, H7, H8, H9, H10, H11, H12, H13, H14, H15, H16, H17, H18, H19, H20, H21, H22, H23};
enum PHASE{StandBy, Setup, Trade, Deal};
enum MODE_PERIOD{Weekly, Daily, NonStop};
enum MODE_RANGE{ATR, Percent, Fixed};
enum MODE_TRADE{Stop, Limit, Random};
enum MODE_ORDER{Buy, Sell};
enum MODE_LOTMANAGEMENT{Constant, Linear, Martingale, Fibonacci};

input group "[PERIODICITY]";
input MODE_PERIOD Periodicity = Weekly;   //Periodicity Mode
input WEEK_DAY Day = Monday;              //Day of Week (Weekly Mode)
input HOUR Hour = H0;                     //Hour of Day (Weekly/Daily Modes)

input group "[RANGE CALCULATION]";
input MODE_RANGE RangeMode = ATR;   //Range Calculation Mode
input double perATR = 50;           //ATR Percentage (ATR Mode, 10-500)
input double perPrice = 1;          //Price Percentage (Percent Mode, 0.1-10)
input int rangeFixed = 1000;        //Range in Points (Fixed Mode, 10-5000)

input group "[TRADE PARAMETERS]";
input MODE_TRADE TradeMode = Stop;  //Trade Mode
input double perRange = 100;        //Range Percentage (10, 100)
input double perProfit = 100;       //Take-Profit Percentage (10, 100)
input double perLoss = 100;         //Stop-Loss Percentage (10, 100)

input group "[MONEY MANAGEMENT]";
input MODE_LOTMANAGEMENT lotMode = Martingale;  //Lot Management Mode
input double perMargin = 10;                    //Margin Percentage (0.1-20)
input double lotMultiplier = 2;                 //Lot Multiplier (1-2)
input double rangeMultiplier = 1;               //Range Multiplier (1:disable, 1-2)

bool isStandby, isSetup, isTrade, isDeal;
int hATR;
double baseLot, varLot, fibL1, fibL2;
PHASE Phase;
MODE_TRADE tradeMode;

struct sTick
{
   double Bid;
   double Ask;
   datetime Time;
}TICK;

struct sSetup
{
   double ATR[2];
   double Range;
   double Center;
   double High;
   double Low;
}SETUP;

struct sDeal
{
   ulong Ticket;
   double Volume;
   double Result;
}DEAL;

bool CheckInputs()
{
   if((RangeMode == ATR) && (perATR < 10 || perATR > 500))
   {
      Alert("ERROR: ATR Percentage should be between 10 and 500.");
      return(false);
   }
   if((RangeMode == Percent) && (perPrice < 0.1 || perPrice > 10))
   {
      Alert("ERROR: Price Percentage should be between 0.1 and 10.");
      return(false);
   }
   if((RangeMode == Fixed) && (rangeFixed < 10 || rangeFixed > 5000))
   {
      Alert("ERROR: Fixed Range should be between 10 and 5000.");
      return(false);
   }
   if(perRange < 10 || perRange > 100)
   {
      Alert("ERROR: Range percentage should be between 10 and 100.");
      return(false);
   }
   if(perProfit < 10 || perProfit > 100)
   {
      Alert("ERROR: Profit percentage should be between 10 and 100.");
      return(false);
   }
   if(perLoss < 10 || perLoss > 100)
   {
      Alert("ERROR: Loss percentage should be between 10 and 100.");
      return(false);
   }
   if(perMargin < 0.1 || perMargin > 20)
   {
      Alert("ERROR: Margin percentage should be between 0.1 and 20.");
      return(false);
   }
   if(lotMultiplier < 1 || lotMultiplier > 2)
   {
      Alert("ERROR: Lot multiplier should be between 1 and 2.");
      return(false);
   }
   if(rangeMultiplier < 1 || rangeMultiplier > 2)
   {
      Alert("ERROR: Range multiplier should be between 1 and 2.");
      return(false);
   }
   return(true);
}

bool SetATR()
{
   hATR = iATR(_Symbol, PERIOD_D1, 20);
   if(hATR == INVALID_HANDLE)
   {
      Alert("ERROR: invalid atr handle.");
      return(false);
   }
   return(true);
}

void SetPhase(PHASE p)
{
   Phase = p;
   switch(p)
   {
      case StandBy: isStandby = true; isSetup = false; isTrade = false; isDeal = false; break;
      case Setup: isStandby = false; isSetup = true; isTrade = false; isDeal = false; break;
      case Trade: isStandby = false; isSetup = false; isTrade = true; isDeal = false; break;
      case Deal: isStandby = false; isSetup = false; isTrade = false; isDeal = true; break;
   }
}

PHASE GetPhase()
{
   return(Phase);
}

void Initialize()
{
   SetPhase(StandBy);
}

void SetBaseLot()
{
   double freeMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   double reserveMargin = freeMargin * perMargin / 100;
   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double stepLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
   double buyMargin, sellMargin;
   if(!OrderCalcMargin(ORDER_TYPE_BUY, _Symbol, 1, SymbolInfoDouble(_Symbol, SYMBOL_ASK), buyMargin))
   {
      baseLot = minLot;
      return;
   }
   if(!OrderCalcMargin(ORDER_TYPE_SELL, _Symbol, 1, SymbolInfoDouble(_Symbol, SYMBOL_BID), sellMargin))
   {
      baseLot = minLot;
      return;
   }
   if(buyMargin < sellMargin)
   {
      baseLot = reserveMargin/buyMargin;
   }
   else
   {
      baseLot = reserveMargin/sellMargin;
   }
   baseLot = ((int)MathRound(baseLot/stepLot))*stepLot;
   if(baseLot < minLot) baseLot = minLot;
   if(baseLot > maxLot) baseLot = maxLot;
}

void SetVarLot()
{
   static int c = 1;
   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double stepLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
   
   if(lotMode == Constant)
   {
      SetBaseLot();
      varLot = baseLot;
   }
   if(lotMode == Linear && DEAL.Result >= 0)
   {
      SetBaseLot();
      varLot = baseLot;
      c = 1;
   }
   if(lotMode == Linear && DEAL.Result < 0)
   {
      c++;
      varLot = baseLot * c;
   }
   if(lotMode == Martingale && DEAL.Result >= 0)
   {
      SetBaseLot();
      varLot = baseLot;
   }
   if(lotMode == Martingale && DEAL.Result < 0)
   {
      varLot = varLot * lotMultiplier;
   }
   if(lotMode == Fibonacci && DEAL.Result >= 0)
   {
      SetBaseLot();
      varLot = baseLot;
      fibL1 = baseLot;
      fibL2 = baseLot;
   }
   if(lotMode == Fibonacci && DEAL.Result < 0)
   {      
      varLot = fibL1 + fibL2;
      fibL1 = fibL2;
      fibL2 = varLot;
   }   
   
   varLot = ((int)MathRound(varLot/stepLot))*stepLot;
   if(varLot < minLot) varLot = minLot;
   if(varLot > maxLot) varLot = maxLot;
}

int OnInit()
{
   if(!CheckInputs()) return(INIT_FAILED);
   if(!SetATR()) return(INIT_FAILED);
   SetBaseLot();
   Initialize();
   return(INIT_SUCCEEDED);
}

void CalculateRange()
{
   if(RangeMode == ATR)
   {
      int count = ArraySize(SETUP.ATR);
      if(CopyBuffer(hATR, 0, 0, count, SETUP.ATR) != count)
      {
         Alert("ERROR: unable to copy atr buffer. taking %1 of current ask as range.");
         SETUP.Range = NormalizeDouble(TICK.Ask / 100, _Digits);
         return;
      }
      else
      {
         SETUP.Range = NormalizeDouble(SETUP.ATR[1] * perATR / 100, _Digits);
         return;
      }
   }
   if(RangeMode == Percent)
   {
      SETUP.Range = NormalizeDouble(TICK.Ask * perPrice / 100, _Digits);
      return;
   }
   if(RangeMode == Fixed)
   {
      SETUP.Range = rangeFixed * _Point;
      return;
   }   
}

void UpdateTick()
{
   TICK.Bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   TICK.Ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   TICK.Time = TimeCurrent();
}

bool SetupCondition()
{
   MqlDateTime dt;
   TimeToStruct(TICK.Time, dt);
   int hour = (Hour + 1);
   if(hour >= 23) hour = 0;
   
   if(Periodicity == Weekly && dt.day_of_week == Day && dt.hour == hour)
   {
      return(true);
   }
   if(Periodicity == Daily && dt.hour == hour)
   {
      return(true);
   }
   if(Periodicity == NonStop)
   {
      return(true);
   }
   return(false);
}

void CalculateSetupLevels()
{
   CalculateRange();
   SETUP.Center = TICK.Ask;
   SETUP.High = SETUP.Center + SETUP.Range * perRange / 100;
   SETUP.Low = SETUP.Center - SETUP.Range * perRange / 100;
}

void DrawVLine(string n, int c, int s, datetime x)
{
   if(ObjectFind(0, n) == 0) ObjectDelete(0, n);
   ObjectCreate(0, n, OBJ_VLINE, 0, x, 0);
   ObjectSetInteger(0, n, OBJPROP_COLOR, c);
   ObjectSetInteger(0, n, OBJPROP_STYLE, s);
}

void DrawHLine(string n, int c, int s, double y)
{
   if(ObjectFind(0, n) == 0) ObjectDelete(0, n);
   ObjectCreate(0, n, OBJ_HLINE, 0, 0, y);
   ObjectSetInteger(0, n, OBJPROP_COLOR, c);
   ObjectSetInteger(0, n, OBJPROP_STYLE, s);
}

void DrawSetup()
{
   DrawVLine("START", clrAqua, STYLE_DOT, TICK.Time);
   DrawHLine("CENTER", clrAqua, STYLE_DOT, TICK.Ask);
   DrawHLine("HIGH", clrLimeGreen, STYLE_SOLID, SETUP.High);
   DrawHLine("LOW", clrRed, STYLE_SOLID, SETUP.Low);
}

void ClearSetup()
{
   if(ObjectFind(0, "START") == 0) ObjectDelete(0, "START");
   if(ObjectFind(0, "CENTER") == 0) ObjectDelete(0, "CENTER");
   if(ObjectFind(0, "HIGH") == 0) ObjectDelete(0, "HIGH");
   if(ObjectFind(0, "LOW") == 0) ObjectDelete(0, "LOW");
}

bool OpenTrade(ENUM_ORDER_TYPE type)
{
   MqlTradeRequest tReq = {0};
   MqlTradeResult tRes = {0};
   double range;
   
   tReq.symbol = _Symbol;
   tReq.action = TRADE_ACTION_DEAL;
   tReq.deviation = 5;
   tReq.magic = MACIT;
   
   SetVarLot();
   tReq.volume = varLot;

   if(DEAL.Result < 0 && rangeMultiplier != 0)
   {
      range = SETUP.Range * rangeMultiplier;
   }
   else
   {
      range = SETUP.Range;
   }
   if(type == ORDER_TYPE_BUY)
   {
      tReq.type = ORDER_TYPE_BUY;
      tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      tReq.tp = tReq.price + range * perProfit / 100;
      tReq.sl = tReq.price - SETUP.Range * perLoss / 100;
   }
   if(type == ORDER_TYPE_SELL)
   {
      tReq.type = ORDER_TYPE_SELL;
      tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      tReq.tp = tReq.price - range * perProfit / 100;
      tReq.sl = tReq.price + SETUP.Range * perLoss / 100;
   }
   if(!CheckMoneyForTrade(_Symbol, tReq.volume, tReq.type))
   {
      Alert("ERROR: not enough money." + (string)tRes.retcode);
      SetBaseLot();
      varLot = baseLot;
      return(false);
   }
   if(!OrderSend(tReq, tRes))
   {
      Alert("ERROR: order cannot be opened. code: " + (string)tRes.retcode);
      if(tRes.retcode == TRADE_RETCODE_NO_MONEY)
      {
         SetBaseLot();
         varLot = baseLot;
      }
      return(false);
   }
   return(true);
}

void CheckRandom()
{
   if(TradeMode == Random)
   {
      MathSrand(GetTickCount());
      switch(MathRand()%2)
      {
         case 0: tradeMode = Stop;
         case 1: tradeMode = Limit;
      }
   }
   else
   {
      tradeMode = TradeMode;
   }
}

bool CheckMoneyForTrade(string symb, double lots, ENUM_ORDER_TYPE type)
{
   MqlTick mqlTick;
   SymbolInfoTick(symb, mqlTick);
   
   double price = mqlTick.ask;
   double margin, free_margin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   if(type==ORDER_TYPE_SELL) price = mqlTick.bid;
   
   if(!OrderCalcMargin(type, symb, lots, price, margin))
   {
      Print("ERROR: required margin cannot be calculated. Code: " + (string)GetLastError());
      return(false);
   }

   if(margin > free_margin)
   {
      Print("ERROR: Not enough money for trade. Code:" + (string)GetLastError());
      return(false);
   }
   return(true);
}

void OnDeinit(const int reason)
{
   ClearSetup();
}

void OnTick()
{
   UpdateTick();
   
   if(GetPhase() == StandBy)
   {
      if(SetupCondition())
      {
         CalculateSetupLevels();
         DrawSetup();
         SetPhase(Setup);      
      }
   }
   
   if(GetPhase() == Setup)
   {
      if(TICK.Ask > SETUP.High)
      {
         CheckRandom();
         if(tradeMode == Stop)
         {
            if(OpenTrade(ORDER_TYPE_BUY))
               SetPhase(Trade);
            else
               Initialize();
         }
         if(tradeMode == Limit)
         {
            if(OpenTrade(ORDER_TYPE_SELL)) 
               SetPhase(Trade);              
            else
               Initialize();
         }
      }
      
      if(TICK.Bid < SETUP.Low)
      {
         CheckRandom();
         if(tradeMode == Stop)
         {
            if(OpenTrade(ORDER_TYPE_SELL)) 
               SetPhase(Trade);              
            else
               Initialize();
         }
         if(tradeMode == Limit)
         {
            if(OpenTrade(ORDER_TYPE_BUY))
               SetPhase(Trade);
            else
               Initialize();
         }
      }   
   }
   
   if(GetPhase() == Trade)
   {

   }
   
   if(GetPhase() == Deal)
   {
      Initialize();
   }
}

void OnTradeTransaction(const MqlTradeTransaction &tTrn, const MqlTradeRequest &tReq, const MqlTradeResult &tRes)
{
   if(tTrn.type == TRADE_TRANSACTION_DEAL_ADD)
   {
      if(HistoryDealSelect(tTrn.deal) && tTrn.symbol == _Symbol && HistoryDealGetInteger(tTrn.deal, DEAL_MAGIC) == MACIT)
      {     
         DEAL.Ticket = tTrn.deal;
         DEAL.Result = HistoryDealGetDouble(tTrn.deal, DEAL_PROFIT);
         DEAL.Volume = HistoryDealGetDouble(tTrn.deal, DEAL_VOLUME);
         if(DEAL.Result != 0) SetPhase(Deal);
         return;
      }
   }
}