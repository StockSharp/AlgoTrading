/*=============================================================
 Info:    OrderLib Settings
 Name:    OrderLibE.mqh
 Author:  Eric Pribitzer
 Version: 0.7
 Update:  2010-02-11 
 Notes:   ---
  
 Copyright (C) 2010 Erich Pribitzer 
 Email: seizu@gmx.at
=============================================================*/

#property copyright "Copyright © 2011, Erich Pribitzer"
#property link      "http://www.wartris.com"

extern string OL_SETTINGS              = "==== ORDERLIB SETTINGS ====";      //
extern bool   OL_ALLOW_ORDER           = true;
extern double OL_RISK_PERC             = 0;                                  // in %
extern int    OL_RISK_PIPS             = 0;                                  // 0=auto (calc price-stoploss)
extern int    OL_PROFIT_PIPS           = 0;
extern int    OL_TRAILING_STOP         = 0;
extern double OL_LOT_SIZE              = 1;                                  // 0=auto lot depends on risk pips and risk percent per trade
extern double OL_INITIAL_LOT           = 0;
extern double OL_CUSTOM_TICKVALUE      = 0;                                  // 0=no custom tickvalue (for testing mode only)
extern int    OL_SLIP_PAGE             = 2;
extern bool   OL_STOPSBYMODIFY         = true;                               // true = sl,tp will set by modify order
extern double OL_MAX_LOT               = 2;
extern int    OL_MAX_ORDERS            = 3;
extern int    OL_MAGIC                 = 0xA1200;
extern bool   OL_ORDER_DUPCHECK        = false;
extern bool   OL_OPPOSITE_CLOSE        = false;                             // oder will only closed when TP lies between SL and Price of an opposit order    
extern bool   OL_ORDER_COLOR           = false;
extern bool   OL_MYSQL_LOG             = false;    

