/*=============================================================
 Info:    Basic Include 
 Name:    BasicH.mqh
 Author:  Erich Pribitzer
 Version: 1.1
 Update:  2011-03-29
 Notes:   DIGITS Bug removed
 Version: 1.0
 Update:  2010-02-11 
 Notes:   ---
  
 Copyright (C) 2010 Erich Pribitzer 
 Email: seizu@gmx.at
=============================================================*/

#property copyright "Copyright © 2011, Erich Pribitzer"
#property link      "http://www.wartris.com"

#include <stderror.mqh>
#include <stdlib.mqh>

#define ERR_DOUBLE_ORDER         30001
#define ERR_ORDER_SETTINGS       30002
#define ERR_TIMEFRAME_DISABLED   30003
#define ERR_LIB_NOT_INITALIZED   30004

#define PERIODS 11
#define PERIOD_M10 10
#define PERIOD_H2  120

#define UNDEF 0
#define SHORT 1
#define LONG 2
#define SHORTCORR 3
#define LONGCORR 4

extern string BA_SETTINGS = "===== BASIC SETTINGS ====";
extern bool BA_ENABLE_M1  = true;
extern bool BA_ENABLE_M5  = true;
extern bool BA_ENABLE_M10 = false;
extern bool BA_ENABLE_M15 = true;
extern bool BA_ENABLE_M30 = true;
extern bool BA_ENABLE_H1  = true;
extern bool BA_ENABLE_H2  = false;
extern bool BA_ENABLE_H4  = true;
extern bool BA_ENABLE_D1  = true;
extern bool BA_ENABLE_W1  = true;
extern bool BA_ENABLE_MN  = true;
extern int  BA_INIT_BARS  = 200;


int      timeframe[]={1,5,10,15,30,60,120,240,1440,10080,43200};

string   timeframeString[]={ "M01","M05","M10","M15","M30","H01","H02","H04","D01","W01","MN"};
string   dirString[]={ "-","S","L","s","l"};


bool   BasicInc_Initialized;
int    DIGITS=0;
double POINT =0;
bool   FIVEDIGITS = false;
int    LOTDIGITS = 0;


void BA_Init()
{
   Print("Init BasicInc");   
   if(Digits == 5 || (Digits == 3 && StringFind(Symbol(),"SILVER",0)==-1 ))
   {
      POINT =Point*10;
//      DIGITS=Digits-1;    Bug
      DIGITS=Digits;   
      FIVEDIGITS=true;
   }
   else
   {
      POINT =Point; 
      DIGITS=Digits;
      FIVEDIGITS=false;
   }
   
   if(MarketInfo(Symbol(),MODE_LOTSTEP)==0.01)
   {
      LOTDIGITS=2;
   }
   else
   {
      LOTDIGITS=1;
   }

   BasicInc_Initialized=true;
}

bool IsTimeframeEnabled(int period)
{
   switch(period)
   {
      case 0:
         return (BA_ENABLE_M1);
      case 1:
         return (BA_ENABLE_M5);
      case 2:
         return (BA_ENABLE_M10);
      case 3:
         return (BA_ENABLE_M15);
      case 4:
         return (BA_ENABLE_M30);
      case 5:
         return (BA_ENABLE_H1);
      case 6:
         return (BA_ENABLE_H2);
      case 7:
         return (BA_ENABLE_H4);
      case 8:
         return (BA_ENABLE_D1);
      case 9:
         return (BA_ENABLE_W1);
      case 10:
         return (BA_ENABLE_MN);
   }
}

int nPeriod(int p=-1)
{
   if(p==-1)
      p=Period();
      
   switch(p)
   {
      case PERIOD_M1:
         return (0);
      case PERIOD_M5:
         return (1);
      case PERIOD_M10:
         return (2);
      case PERIOD_M15:
         return (3);
      case PERIOD_M30:
         return (4);
      case PERIOD_H1:
         return (5);
      case PERIOD_H2:
         return (6);
      case PERIOD_H4:
         return (7);
      case PERIOD_D1:
         return (8);
      case PERIOD_W1:
         return (9);
      case PERIOD_MN1:
         return (10);
   }
}

