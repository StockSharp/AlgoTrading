//+------------------------------------------------------------------+
//|                                                 RouletteGame.mq5 |
//|                        Copyright 2010, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

#include <CRouletteGame_en.mqh>

bool Load=false;
CRouletteGame RouletteGame;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!Load) RouletteGame.CreateGameObjects();
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(reason!=REASON_CHARTCHANGE)
    {
     Load=false;
     RouletteGame.DeleteGameObjects();
    }
   else Load=true;
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
  }
//+------------------------------------------------------------------+
void OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
  {
   //---- click on graphic objects
   if(id==CHARTEVENT_OBJECT_CLICK && (sparam=="rg_zero" || sparam=="rg_red" || sparam=="rg_black" || sparam=="rg_red1" ||
   sparam=="rg_black2" || sparam=="rg_red3" || sparam=="rg_black4" || sparam=="rg_red5" || sparam=="rg_black6" ||
   sparam=="rg_red7" || sparam=="rg_black8" || sparam=="rg_red9" || sparam=="rg_black10" || sparam=="rg_black11" ||
   sparam=="rg_red12" || sparam=="rg_black13" || sparam=="rg_red14" || sparam=="rg_black15" || sparam=="rg_red16" ||
   sparam=="rg_black17" || sparam=="rg_red18" || sparam=="rg_red19" || sparam=="rg_black20" || sparam=="rg_red21" ||
   sparam=="rg_black22" || sparam=="rg_red23" || sparam=="rg_black24" || sparam=="rg_red25" || sparam=="rg_black26" ||
   sparam=="rg_red27" || sparam=="rg_black28" || sparam=="rg_black29" || sparam=="rg_red30" || sparam=="rg_black31" ||
   sparam=="rg_red32" || sparam=="rg_black33" || sparam=="rg_red34" || sparam=="rg_black35" || sparam=="rg_red36" ||
   sparam=="rg_newgame" || sparam=="rg_exitgame" || sparam=="rg_play" || sparam=="rg_closemenu" || sparam=="rg_bet_100" ||
   sparam=="rg_bet_500" || sparam=="rg_bet_1k" || sparam=="rg_bet_5k" || sparam=="rg_bet_10k" || sparam=="rg_bet_100k" ||
   sparam=="rg_bet_1mio" || sparam=="rg_bet_all" || sparam=="rg_bet_c")) RouletteGame.ClickGameObjects(sparam);
  }
//+------------------------------------------------------------------+
     