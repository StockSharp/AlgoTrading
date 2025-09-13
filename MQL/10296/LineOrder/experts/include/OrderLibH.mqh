/*=============================================================
 Info:    OrderLib Header File
 Name:    OrderLibH.mqh
 Author:  Erich Pribitzer
 Version: 0.7
 Update:  2010-02-11 
 Notes:   ---
  
 Copyright (C) 2010 Erich Pribitzer
=============================================================*/

#property copyright "Copyright © 2011, Erich Pribitzer"
#property link      "http://www.wartris.com"

#import "OrderLib.ex4"
   int     OL_Init(bool ALLOW_ORDER,double RISK_PERC,int RISK_PIPS,int PROFIT_PIPS,int TRAILING_STOP, double LOT_SIZE,double INITIAL_LOT,double CUSTOM_TICKVALUE, int SLIPPAGE, bool STOPSBYMODIFY, double MAX_LOT, int MAX_ORDERS, int MAGIC, bool ORDER_DUPCHECK, bool OPPOSITE_CLOSE, bool ORDER_COLOR, bool MYSQL_LOG);
   int     OL_Deinit();
   int     OL_SyncBuffer();
   int     OL_ReadBuffer();
   int     OL_WriteBuffer();
   void    OL_addOrderProperty(int property,double value);
   void    OL_addOrderDescription(string desc);
   int     OL_registerOrder();

   int     OL_enumOrderList(int binx, int cmd);
   void    OL_setOrderProperty(int binx,int property,double value);
   double  OL_getOrderProperty(int binx,int property);
   int     OL_orderCount(int cmd);
   void    OL_processOrder();
   void    OL_processOppositClose(int cmd, double tp);
   void    OL_processClose();
   double  OL_calcTakeProfit(int cmd, int pips, int mode, double price);
   double  OL_calcStopLoss(int cmd, int pips, int mode, double price);
   
#import


#define OL_FIX "OL_"
#define OL_ALL 100
#define OL_OPEN 1
#define OL_CLOSE 2
#define OL_SCHEDULED -1

#define OL_CL_OPPOSITCLOSE 1
#define OL_CL_TIMEEXPIRE 2
#define OL_CL_STOPLOSS 3
#define OL_CL_TAKEPROFIT 4
#define OL_CL_BYSERVER 5
#define OL_CL_BYEA 6
#define OL_CL_DUPCHECK 7
#define OL_CL_SIZE 8

#define OL_FL_MODIFY 1
#define OL_FL_CLOSE 2


#define OL_ORDER_BUFFER_SIZE 10

// orderInfo buffer
#define	OL_ID 0
#define  OL_OID 1
#define	OL_TYPE 2
#define  OL_FLAG 3
#define	OL_OTIME 4
#define	OL_CTIME 5
#define  OL_PRICE 6
#define	OL_OPRICE 7
#define	OL_CPRICE 8
#define	OL_PROFIT 9

#define  OL_HIPROFITTIME 10
#define	OL_HIPROFIT 11
#define  OL_LOPROFITTIME 12
#define  OL_LOPROFIT 13

#define	OL_OSPREAD 14
#define	OL_CSPREAD 15
#define	OL_OTICKCOUNT 16
#define	OL_CTICKCOUNT 17
#define  OL_STOPLOSS 18
#define  OL_TAKEPROFIT 19
#define  OL_LOTSIZE 20
#define  OL_OBAR 21
#define  OL_CBAR 22
#define  OL_EXPIRATION 23
#define  OL_PERIOD 24
#define  OL_ERRORNO 25
#define  OL_RISKPERC 26
#define  OL_RISKPIPS 27
#define  OL_PROFITPIPS 28
#define  OL_CLOSEREASON 29
#define  OL_SLIPPAGE 30
#define  OL_TRAILINGSTOP 31
#define  OL_TRAILING 32
#define  OL_SIZE 33


