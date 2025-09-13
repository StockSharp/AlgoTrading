
#property copyright           "Mokara"
#property link                "https://www.mql5.com/en/users/mokara"
#property description         "Range Follower"
#property version             "1.0"
#property script_show_inputs  true

input int perTrigger = 60; //Trigger Percent (10%-90%)
input double iLots = 0.1; //Lots

//ATR Parameters
ENUM_TIMEFRAMES atr_timeframe = PERIOD_D1;
int atr_handle = 0;
int atr_period = 20;
int atr_bars = 5;
double atr_trigger = 0;
double atr_range = 0;
double atr_rest = 0;
double atr_array[];

//Intraday Price Information
double day_high = 0;
double day_low = 0;
double day_curask = 0;
double day_curbid = 0;
double day_range = 0;
double day_tohigh = 0;
double day_tolow = 0;

//Flags
bool first_tick = true;
bool range_reached = false;
bool trade_opened = false;

//Data Structures
MqlTradeRequest tReq = {0};
MqlTradeResult tRes = {0};
MqlDateTime dt_begin = {0};
MqlDateTime dt_end = {0};

//Other Variables
long sec_left = 0;
ulong order_ticket = 0;
double lots = 0;
string comment = "";
string dt_end_s = "";

bool UpdateLevels()
{  
   atr_range = atr_array[1] / _Point;
   atr_trigger = atr_range * perTrigger / 100;
   atr_rest = atr_range - atr_trigger;
   day_high = iHigh(_Symbol, PERIOD_D1, 0);
   day_low = iLow(_Symbol, PERIOD_D1, 0);
   day_curask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   day_curbid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   day_range = (day_high - day_low) / _Point;
   day_tohigh = (day_high - day_curask) / _Point;
   day_tolow = (day_curbid - day_low) / _Point;
   if(day_range > atr_trigger) range_reached = true;
   return(true);
}

void UpdateTime()
{
   ZeroMemory(dt_begin);
   ZeroMemory(dt_end);
   TimeToStruct(TimeCurrent(), dt_begin);
   dt_begin.hour = 0;
   dt_begin.min = 0;
   dt_begin.sec = 0;  
   TimeToStruct(StructToTime(dt_begin) + 86400, dt_end);
   sec_left = StructToTime(dt_end) - TimeCurrent();
   dt_end_s = TimeToString(StructToTime(dt_end));
}

void SetVolume()
{
   double min_volume = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int ratio = (int)MathRound(iLots/volume_step);
  
   lots = iLots;
   if(iLots < min_volume) lots = min_volume;
   if(iLots > max_volume) lots = max_volume;
   if(MathAbs(ratio * volume_step - iLots) > 0.0000001) lots = min_volume; //lots = ratio * volume_step;
}

void Show()
{
   Comment("");
   comment = "Trend = " + ((day_tohigh < day_tolow) ? "UP" : "DOWN") + "\n";
   comment += "Average: " + DoubleToString(atr_range, 0) + "\n";
   comment += "Trigger: " + DoubleToString(atr_trigger, 0) + "\n";
   comment += "Rest: " + DoubleToString(atr_rest, 0) + "\n";
   comment += "Daily: " + DoubleToString(day_range, 0) + "\n";
   comment += "To High: " + DoubleToString(day_tohigh, 0) + "\n";
   comment += "To Low: " + DoubleToString(day_tolow, 0) + "\n";  
   comment += "Trade Opened: " + (string)trade_opened + "\n";
   comment += "Range Reached: " + (string)range_reached + "\n";
   comment += "Seconds Left: " + (string)sec_left + "\n";
   comment += "End Time: " + (string)dt_end_s;
   Comment(comment);
}

void OpenBuy()
{
   Alert("Buy Order.");
   ZeroMemory(tReq);
   ZeroMemory(tRes);
   tReq.symbol = _Symbol;
   tReq.volume = lots;
   tReq.action = TRADE_ACTION_DEAL;
   tReq.deviation = 20;
   tReq.type = ORDER_TYPE_BUY;
   tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   tReq.tp = tReq.price + NormalizeDouble(atr_rest * _Point, _Digits);
   tReq.sl = tReq.price - NormalizeDouble(atr_trigger * _Point, _Digits);
   if(!OrderSend(tReq, tRes))
   {
      Alert("ERROR: buy order cannot be sent. ");
   }
   order_ticket = tRes.order;
}

void OpenSell()
{
   Alert("Sell Order.");
   ZeroMemory(tReq);
   ZeroMemory(tRes);
   tReq.symbol = _Symbol;
   tReq.volume = lots;
   tReq.action = TRADE_ACTION_DEAL;
   tReq.deviation = 20;
   tReq.type = ORDER_TYPE_SELL;
   tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   tReq.tp = tReq.price - NormalizeDouble(atr_rest * _Point, _Digits);
   tReq.sl = tReq.price + NormalizeDouble(atr_trigger * _Point, _Digits);
   if(!OrderSend(tReq, tRes))
   {
      Alert("ERROR: sell order cannot be sent. ");
   }
   order_ticket = tRes.order;
}

void CloseByTicket(ulong ticket)
{
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);
  
   if(!PositionSelectByTicket(ticket))
   {
      return;
   }
   if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
   {
      request.type = ORDER_TYPE_SELL;
      request.price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      request.action = TRADE_ACTION_DEAL;
      request.symbol = PositionGetString(POSITION_SYMBOL);
      request.volume = PositionGetDouble(POSITION_VOLUME);
      request.sl = 0.0;
      request.tp = 0.0;
      request.deviation = 20;
      request.position = PositionGetInteger(POSITION_TICKET);
      if(!OrderSend(request, result))
      {
         Alert("Error: buy order cannot be closed.");
         return;
      }
   }
   if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
   {
      request.type = ORDER_TYPE_BUY;
      request.price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      request.action = TRADE_ACTION_DEAL;
      request.symbol = PositionGetString(POSITION_SYMBOL);
      request.volume = PositionGetDouble(POSITION_VOLUME);
      request.sl = 0.0;
      request.tp = 0.0;
      request.deviation = 20;
      request.position = PositionGetInteger(POSITION_TICKET);
      if(!OrderSend(request, result))
      {
         Alert("Error: sell order cannot be closed.");
         return;
      }
   }
}

int OnInit()
{
   EventSetTimer(1);
   if(perTrigger < 10 || perTrigger > 90)
   {
      Alert("Error: trigger rate should be between 20 and 90.");
      return(false);
   }
   SetVolume();
   atr_handle = iATR(Symbol(), atr_timeframe, atr_period);
   ArraySetAsSeries(atr_array, true);
   ArrayResize(atr_array, atr_bars);
   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
   Comment("");
   EventKillTimer();
   Print("Program terminated.");
}

void OnTick()
{
   if(first_tick == true)
   {
      first_tick = false;
      if(CopyBuffer(atr_handle, 0, 0, atr_bars, atr_array) != atr_bars)
      {
         Alert("ERROR: unable to copy atr buffer.");
         ExpertRemove();
         return;
      }
      UpdateLevels();
      UpdateTime();
      if(range_reached == true)
      {
         Alert("ERROR: daily range has already been reached.");
         ExpertRemove();
         return;
      }
   }
  
   if(AccountInfoDouble(ACCOUNT_MARGIN_FREE) < (1000 * lots))
   {
      Print("We have no money. Free Margin = ", AccountInfoDouble(ACCOUNT_MARGIN_FREE));
      return;
   }
  
   UpdateLevels();
   Show();  

   //BUY ORDER
   if(day_tolow > atr_trigger && trade_opened == false)
   {
      OpenBuy();
      trade_opened = true;      
   }
  
   //SELL ORDER
   if(day_tohigh > atr_trigger && trade_opened == false)
   {
      OpenSell();
      trade_opened = true;
   }  
}

void OnTimer()
{
   sec_left = StructToTime(dt_end) - TimeCurrent();
   if(sec_left < 0)
   {
      first_tick = true;
      range_reached = false;
      trade_opened = false;
      if(PositionSelectByTicket(order_ticket))
      {
         CloseByTicket(order_ticket);
      }
   }
}