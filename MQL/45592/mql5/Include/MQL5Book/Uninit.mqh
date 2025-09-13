//+------------------------------------------------------------------+
//|                                                       Uninit.mqh |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Enumeration for getting names of deinitialization codes          |
//+------------------------------------------------------------------+
enum ENUM_DEINIT_REASON
{
   // DEINIT_ prefix is used below
   // to prevent name collisions with built-in constants
   DEINIT_REASON_PROGRAM     = 0,
   DEINIT_REASON_REMOVE      = 1,
   DEINIT_REASON_RECOMPILE   = 2,
   DEINIT_REASON_CHARTCHANGE = 3,
   DEINIT_REASON_CHARTCLOSE  = 4,
   DEINIT_REASON_PARAMETERS  = 5,
   DEINIT_REASON_ACCOUNT     = 6,
   DEINIT_REASON_TEMPLATE    = 7,
   DEINIT_REASON_INITFAILED  = 8,
   DEINIT_REASON_CLOSE       = 9,
};

#define DRSTR(X) EnumToString((ENUM_DEINIT_REASON)(X))
//+------------------------------------------------------------------+
