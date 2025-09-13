//+------------------------------------------------------------------+
//|                                                     TickEnum.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Tick mode for calling CopyTicks/Range                            |
//+------------------------------------------------------------------+
enum COPY_TICKS
{
   ALL_TICKS = /* -1 */ COPY_TICKS_ALL,    // all ticks
   INFO_TICKS = /* 1 */ COPY_TICKS_INFO,   // info ticks
   TRADE_TICKS = /* 2 */ COPY_TICKS_TRADE, // trade ticks
};

//+------------------------------------------------------------------+
//| Tick flags and some most useful combinations (bitmasks)          |
//+------------------------------------------------------------------+
enum TICK_FLAGS
{
   TF_BID = /* 2 */ TICK_FLAG_BID,
   TF_ASK = /* 4 */ TICK_FLAG_ASK,
   TF_BID_ASK = TICK_FLAG_BID | TICK_FLAG_ASK,
   
   TF_LAST = /* 8 */ TICK_FLAG_LAST,
   TF_BID_LAST = TICK_FLAG_BID | TICK_FLAG_LAST,
   TF_ASK_LAST = TICK_FLAG_ASK | TICK_FLAG_LAST,
   TF_BID_ASK_LAST = TF_BID_ASK | TICK_FLAG_LAST,
   
   TF_VOLUME = /* 16 */ TICK_FLAG_VOLUME,
   TF_LAST_VOLUME = TICK_FLAG_LAST | TICK_FLAG_VOLUME,
   TF_BID_VOLUME = TICK_FLAG_BID | TICK_FLAG_VOLUME,
   TF_BID_ASK_VOLUME = TF_BID_ASK | TICK_FLAG_VOLUME,
   TF_BID_ASK_LAST_VOLUME = TF_BID_ASK | TF_LAST_VOLUME,
   
   TF_BUY = /* 32 */ TICK_FLAG_BUY,
   TF_SELL = /* 64 */ TICK_FLAG_SELL,
   TF_BUY_SELL = TICK_FLAG_BUY | TICK_FLAG_SELL,
   TF_LAST_VOLUME_BUY = TF_LAST_VOLUME | TICK_FLAG_BUY,
   TF_LAST_VOLUME_SELL = TF_LAST_VOLUME | TICK_FLAG_SELL,
   TF_LAST_VOLUME_BUY_SELL = TF_BUY_SELL | TF_LAST_VOLUME,
   
   // undocumented (not supported here)
   TF_RESERVED = 128,
   TF_BID_RES = TICK_FLAG_BID | TF_RESERVED,
   TF_ASK_RES = TICK_FLAG_ASK | TF_RESERVED,
   TF_BID_ASK_RES = TF_BID_ASK | TF_RESERVED,
   
   // undocumented (not supported here)
   TF_INTERNAL = 256,
   TF_LAST_VOLUME_BUY_INT = TF_LAST_VOLUME_BUY | TF_INTERNAL,
   TF_LAST_VOLUME_SELL_INT = TF_LAST_VOLUME_SELL | TF_INTERNAL,
   TF_LAST_VOLUME_BUY_SELL_INT = TF_LAST_VOLUME_BUY_SELL | TF_INTERNAL,
};
//+------------------------------------------------------------------+
