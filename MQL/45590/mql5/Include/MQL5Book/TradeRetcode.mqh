//+------------------------------------------------------------------+
//|                                                 TradeRetcode.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| All TRADE_RETCODEs in MQL5. "TRADE_RETCODE_" prefixes are        |
//| removed to eliminate name collisions with built-in constants.    |
//+------------------------------------------------------------------+
enum TRADE_RETCODE
{
   OK_0                 = 0,
   REQUOTE              = 10004,
   REJECT               = 10006,
   CANCEL               = 10007,
   PLACED               = 10008,
   DONE                 = 10009,
   DONE_PARTIAL         = 10010,
   ERROR                = 10011,
   TIMEOUT              = 10012,
   INVALID              = 10013,
   INVALID_VOLUME       = 10014,
   INVALID_PRICE        = 10015,
   INVALID_STOPS        = 10016,
   TRADE_DISABLED       = 10017,
   MARKET_CLOSED        = 10018,
   NO_MONEY             = 10019,
   PRICE_CHANGED        = 10020,
   PRICE_OFF            = 10021,
   INVALID_EXPIRATION   = 10022,
   ORDER_CHANGED        = 10023,
   TOO_MANY_REQUESTS    = 10024,
   NO_CHANGES           = 10025,
   SERVER_DISABLES_AT   = 10026,
   CLIENT_DISABLES_AT   = 10027,
   LOCKED               = 10028,
   FROZEN               = 10029,
   INVALID_FILL         = 10030,
   CONNECTION           = 10031,
   ONLY_REAL            = 10032,
   LIMIT_ORDERS         = 10033,
   LIMIT_VOLUME         = 10034,
   INVALID_ORDER        = 10035,
   POSITION_CLOSED      = 10036,
   INVALID_CLOSE_VOLUME = 10038,
   CLOSE_ORDER_EXIST    = 10039,
   LIMIT_POSITIONS      = 10040,
   REJECT_CANCEL        = 10041,
   LONG_ONLY            = 10042,
   SHORT_ONLY           = 10043,
   CLOSE_ONLY           = 10044,
   FIFO_CLOSE           = 10045,
   HEDGE_PROHIBITED     = 10046,
};

#define TRCSTR(X) EnumToString((TRADE_RETCODE)(X))
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Simplified classification of retcodes,                           |
//| for more finegrained classification see severity groups below    |
//+------------------------------------------------------------------+
#define IS_SERVICABLE(T) ((T) < TRADE_RETCODE_ERROR)
#define IS_TANGIBLE(T) ((T) >= TRADE_RETCODE_ERROR)

//+------------------------------------------------------------------+
//| Groups of retcodes by severity (from low to high)                |
//| SEVERITY_INVALID and above require an intervention (user/program)|
//+------------------------------------------------------------------+
enum TRADE_RETCODE_SEVERITY
{
   SEVERITY_UNDEFINED,   // skip, output to log
   SEVERITY_NORMAL,      // keep normal operation
   SEVERITY_RETRY,       // retry (probably several times) on updated environment
   SEVERITY_TRY_LATER,   // wait a bit more and retry
   SEVERITY_REJECT,      // request is not processed, probably keep trying
                         // 
   SEVERITY_INVALID,     // change input data and retry
   SEVERITY_LIMITS,      // check limits and adjust request
   SEVERITY_PERMISSIONS, // notify user or adapt algorithm to settings
   SEVERITY_ERROR,       // output info to user or log
};

//+------------------------------------------------------------------+
//| Detect severity of a given retcode                               |
//| to make a decision on further actions                            |
//+------------------------------------------------------------------+
TRADE_RETCODE_SEVERITY TradeCodeSeverity(const uint retcode)
{
   static const TRADE_RETCODE_SEVERITY severities[] =
   {
      SEVERITY_UNDEFINED,
      SEVERITY_UNDEFINED,
      SEVERITY_UNDEFINED,
      SEVERITY_UNDEFINED,
      SEVERITY_RETRY,       // REQUOTE (10004)
      SEVERITY_UNDEFINED,     
      SEVERITY_REJECT,      // REJECT (10006)
      SEVERITY_NORMAL,      // CANCEL (10007)
      SEVERITY_NORMAL,      // PLACED (10008)
      SEVERITY_NORMAL,      // DONE (10009)
      SEVERITY_NORMAL,      // DONE_PARTIAL (10010)
      SEVERITY_ERROR,       // ERROR (10011)
      SEVERITY_RETRY,       // TIMEOUT (10012)
      SEVERITY_INVALID,     // INVALID (10013)
      SEVERITY_INVALID,     // INVALID_VOLUME (10014)
      SEVERITY_INVALID,     // INVALID_PRICE (10015)
      SEVERITY_INVALID,     // INVALID_STOPS (10016)
      SEVERITY_PERMISSIONS, // TRADE_DISABLED (10017)
      SEVERITY_TRY_LATER,   // MARKET_CLOSED (10018)
      SEVERITY_LIMITS,      // NO_MONEY (10019)
      SEVERITY_RETRY,       // PRICE_CHANGED (10020) ~ REQUOTE
      SEVERITY_RETRY,       // PRICE_OFF (10021) ~ REQUOTE
      SEVERITY_INVALID,     // INVALID_EXPIRATION (10022)
      SEVERITY_INVALID,     // ORDER_CHANGED (10023)
      SEVERITY_LIMITS,      // TOO_MANY_REQUESTS (10024)
      SEVERITY_NORMAL,      // NO_CHANGES (10025)
      SEVERITY_PERMISSIONS, // SERVER_DISABLES_AT (10026)
      SEVERITY_PERMISSIONS, // CLIENT_DISABLES_AT (10027)
      SEVERITY_TRY_LATER,   // LOCKED (10028)
      SEVERITY_TRY_LATER,   // FROZEN (10029)
      SEVERITY_INVALID,     // INVALID_FILL (10030)
      SEVERITY_TRY_LATER,   // CONNECTION (10031)
      SEVERITY_PERMISSIONS, // ONLY_REAL (10032)
      SEVERITY_LIMITS,      // LIMIT_ORDERS (10033)
      SEVERITY_LIMITS,      // LIMIT_VOLUME (10034)
      SEVERITY_INVALID,     // INVALID_ORDER (10035)
      SEVERITY_NORMAL,      // POSITION_CLOSED (10036)
      SEVERITY_UNDEFINED,
      SEVERITY_INVALID,     // INVALID_CLOSE_VOLUME (10038)
      SEVERITY_NORMAL,      // CLOSE_ORDER_EXIST (10039)
      SEVERITY_LIMITS,      // LIMIT_POSITIONS (10040)
      SEVERITY_REJECT,      // REJECT_CANCEL (10041)
      SEVERITY_PERMISSIONS, // LONG_ONLY (10042)
      SEVERITY_PERMISSIONS, // SHORT_ONLY (10043)
      SEVERITY_PERMISSIONS, // CLOSE_ONLY (10044)
      SEVERITY_PERMISSIONS, // FIFO_CLOSE (10045)
      SEVERITY_PERMISSIONS, // HEDGE_PROHIBITED (10046)
   };

   if(retcode == 0) return SEVERITY_NORMAL;
   if(retcode < 10000 || retcode > HEDGE_PROHIBITED) return SEVERITY_UNDEFINED;
   return severities[retcode - 10000];
};
//+------------------------------------------------------------------+
