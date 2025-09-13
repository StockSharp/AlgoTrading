#property copyright           "Mokara"
#property link                "https://www.mql5.com/en/users/mokara"
#property description         "Periodic Range Breakout"
#property version             "1.0"
#property script_show_inputs  true

#define ATR_BUFFER 2

enum ENUM_HOUR
{
   H00 = 0, H01, H02, H03, H04, H05, H06, H07, H08, H09, H10, H11, H12, 
   H13, H14, H15, H16, H17, H18, H19, H20, H21, H22, H23
};

enum ENUM_PHASE
{
   STANDBY,
   SETUP,
   TRADE
};

input ENUM_DAY_OF_WEEK inDay = MONDAY; //Day of Week
input ENUM_HOUR inHour = H00;          //Starting Hour
input double inPerPrice = 1;           //Price Percentage (0.3%-3%)
input double inPerATR = 100;           //ATR Percentage (10%-200%)
input double inPerProfit = 100;        //Take Profit Percentage (10%-200%)
input double inPerLoss = 100;          //Stop Loss Percentage (10%-200%)
input double inLot = 0.1;              //Lot

ENUM_DAY_OF_WEEK Day;
ENUM_HOUR Hour;
ENUM_PHASE Phase;
MqlTradeRequest tReq;
MqlTradeResult tRes;
MqlDateTime dtCurrent;

double perPrice;
double perATR;
double perProfit;
double perLoss;
double Range;
double Profit;
double Loss;
double cumLoss; //cumulative loss
double comLoss; //loss compensation
double pCurAsk;
double pCurBid;
double pCenter;
double pHighStop;
double pLowStop;
double pHighProfit;
double pLowProfit;
double pHighLoss;
double pLowLoss;
double Lot;
double arrayATR[];

ulong orderTicket;
uint countSetup = 0;
int handleATR = 0;
int kLot = 1;

bool isSet = false;
bool doCalc = false;
bool tradeOpen = false;

void CheckInputs()
{
   if(inDay == SATURDAY || inDay == SUNDAY)
   {
      Alert("WARNING: weekend day. taking Monday as default.");
      Day = MONDAY;
   }
   else
   {
      Day = inDay;
   }
   if(inPerPrice < 0.3 || inPerPrice > 3)
   {
      Alert("WARNING: invalid price percentage. taking 1 as default.");
      perPrice = 1;
   }
   else
   {
      perPrice = inPerPrice;
   }
   if(inPerATR < 10 || inPerATR > 200)
   {
      Alert("WARNING: invalid atr percentage. taking 100 as default.");
      perATR = 100;
   }
   else
   {
      perATR = inPerATR;
   }
   if(inPerProfit < 10 || inPerProfit > 200)
   {
      Alert("WARNING: invalid profit percentage. taking 100 as default.");
      perProfit = 100;
   }
   else
   {
      perProfit = inPerProfit;
   }
   if(inPerLoss < 10 || inPerLoss > 200)
   {
      Alert("WARNING: invalid loss percentage. taking 100 as default.");
      perLoss = 100;
   }
   else
   {
      perLoss = inPerLoss;
   }
}

bool Initialization()
{
   Phase = STANDBY;
   isSet = false;
   handleATR = iATR(_Symbol, PERIOD_D1, 20);
   if(handleATR == INVALID_HANDLE)
   {
      Alert("ERROR: unable to get handle for atr.");
      return(false);
   }
   return(true);
}

void SetVolume()
{
   double volMin = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double volMax = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double volStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
   int ratio = (int)MathRound(inLot / volStep);
   if(inLot < volMin) Lot = volMin;
   else if(inLot > volMax) Lot = volMax;
   else if(MathAbs(ratio * volStep - inLot) > 0.0000001) Lot = ratio * volStep;
   else Lot = inLot;
}

void Calculate()
{
   if(CopyBuffer(handleATR, 0, 0, ATR_BUFFER, arrayATR) != ATR_BUFFER)
   {
      Alert("ERROR: unable to copy atr buffer. using price percentage instead.");
      Range = NormalizeDouble(pCurAsk * perPrice / 100, _Digits);
   }
   else
   {
      Range = NormalizeDouble(arrayATR[1] * perATR / 100, _Digits);
   }   
   Profit = NormalizeDouble((Range * perProfit / 100), _Digits);
   Loss = NormalizeDouble(Range * perLoss / 100, _Digits);
   if(comLoss != 0) {Profit = comLoss; Loss += comLoss;}
   pHighStop = pCenter + Range;
   pHighProfit = pHighStop + Profit;
   pHighLoss = pHighStop - Loss;
   pLowStop = pCenter - Range;
   pLowProfit = pLowStop - Profit; 
   pLowLoss = pLowStop + Loss;
}

void Martingale()
{
   if(HistoryDealSelect(orderTicket + 1))
   {
      if(HistoryDealGetDouble(orderTicket + 1, DEAL_PROFIT) < 0)
      {
         kLot *= 2;
         cumLoss += HistoryDealGetDouble(orderTicket + 1, DEAL_PROFIT);
         comLoss = MathAbs(NormalizeDouble(cumLoss * _Point * 10 / (kLot * SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_VALUE)), _Digits));
      }
      else
      {
         kLot = 1;
         cumLoss = 0;
         comLoss = 0;
      }
   }
}

bool OpenBuy()
{   
   ZeroMemory(tReq);
   ZeroMemory(tRes);
   tReq.symbol = _Symbol;
   tReq.volume = Lot * kLot;
   tReq.action = TRADE_ACTION_DEAL;
   tReq.deviation = 20;
   tReq.type = ORDER_TYPE_BUY; 
   tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_ASK); 
   tReq.tp = pHighProfit;
   tReq.sl = pHighLoss;
   if(!OrderSend(tReq, tRes)) return(false);
   tradeOpen = true;
   orderTicket = tRes.order;
   return(true);
}

bool OpenSell()
{
   ZeroMemory(tReq);
   ZeroMemory(tRes);
   tReq.symbol = _Symbol;
   tReq.volume = Lot * kLot;
   tReq.action = TRADE_ACTION_DEAL;
   tReq.deviation = 20;
   tReq.type = ORDER_TYPE_SELL; 
   tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_BID); 
   tReq.tp = pLowProfit;
   tReq.sl = pLowLoss;
   if(!OrderSend(tReq, tRes)) return(false);
   tradeOpen = true;
   orderTicket = tRes.order;
   return(true);
}

void DrawVLine(string n, int c, int s, datetime x)
{
   if(ObjectFind(0, n) == 0) ObjectDelete(0, n);
   ObjectCreate(0, n, OBJ_VLINE, 0, x, 0);
   ObjectSetInteger(0, n, OBJPROP_COLOR, c);
   ObjectSetInteger(0, n, OBJPROP_STYLE, s);
}

int OnInit()
{
   CheckInputs();
   SetVolume();
   if(!Initialization())
   {
      return(INIT_FAILED);
   }
   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
}

void OnTick()
{
   pCurAsk = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   pCurBid = SymbolInfoDouble(_Symbol, SYMBOL_BID);   
   if(Phase == STANDBY)
   {
      ZeroMemory(dtCurrent);
      TimeToStruct(TimeCurrent(), dtCurrent);
      if(isSet == false && dtCurrent.day_of_week == Day && dtCurrent.hour == (Hour + 1))
      {
         pCenter = iClose(_Symbol, PERIOD_H1, 1);
         Martingale();
         Calculate();
         isSet = true;
         DrawVLine("Start_" + (string)countSetup, clrAqua, STYLE_DOT, TimeCurrent());
         countSetup++;
         Phase = SETUP;
         return;
      }
   }
   if(Phase == SETUP)
   {
      if(pCurAsk > pHighStop && tradeOpen == false)
      {
         if(AccountInfoDouble(ACCOUNT_MARGIN_FREE) < (1000 * Lot))
         {
            Alert("We have no money. Free Margin = ", AccountInfoDouble(ACCOUNT_MARGIN_FREE));
            return;
         }
         if(OpenBuy())
         {
            Alert("INFO: buy order opened. ");
            Phase = TRADE;
         }
         else
         {
            Alert("ERROR: buy order cannot be opened. ");
            return;
         }
      }
      
      if(pCurBid < pLowStop && tradeOpen == false)
      {
      
         if(AccountInfoDouble(ACCOUNT_MARGIN_FREE) < (1000 * Lot))
         {
            Alert("We have no money. Free Margin = ", AccountInfoDouble(ACCOUNT_MARGIN_FREE));
            return;;
         }
         if(OpenSell())
         {
            Alert("INFO: sell order opened. ");
            Phase = TRADE;
         }
         else
         {
            Alert("ERROR: sell order cannot be opened. ");
            return;
         }
      }
   }
   if(Phase == TRADE)
   {
      if(tradeOpen && !PositionSelectByTicket(orderTicket))
      {
         tradeOpen = false;
         if(!Initialization())
         {
            Alert("ERROR: expert refresh failed.");
            ExpertRemove();
            return;
         }
      }
   }
}