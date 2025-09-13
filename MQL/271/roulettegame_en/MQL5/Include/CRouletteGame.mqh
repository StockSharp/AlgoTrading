//+------------------------------------------------------------------+
//|                                                CRouletteGame.mqh |
//|                        Copyright 2010, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"

class CRouletteGame
  {
   private:
      string rg;
      int Money;          // money
      int InBets;         // money in bets
      int Better;         // better
      int VipStatus;      // current status
      int TotalInBets;    // total in bets (needed fot vip status)
      bool BetsMenu;      // bets menu
      bool Playing;       // playing
      int Bets[39];       // array with bets for each sector
      int Random;         // current win number
      int CurrentIndex;   // current sector index
      int VarBetter;      // bets change (1: greater, -1: lower)
      int Winner;         // winner
      
      void CreateBitmap(string name,int x,int y,string off,bool saw=true);                                    // creates a bitmap
      void CreateBitmapButton(string name,int x,int y,string on,string off,bool saw=true);                    // creates a bitmap button
      void CreateButton(string name,string text,int x,int y,int xsize,int ysize,int fontsize,bool saw=true);  // creates a button
      void CreateLabel(string name,string text,int x,int y,int fontsize,bool saw=true);                       // creates a text label
      int GenerateRandom(void);               // returns random from 0 to 36
      int GetIndex(string name);              // returns the sector index by name of graphic object
      string GetName(int index);              // returns the name of graphic object by sector index
      string GetPicOn(int index);             // returns file name of the sector image by index (pressed w/o bet)
      string GetPicOff(int index);            // returns file name of the sector image by index (normal w/o bet)
      string GetPicBetOn(int index);          // returns file name of the sector image by index (pressed with bet)
      string GetPicBetOff(int index);         // returns file name of the sector image by index (normal with bet)
      string GetPicVipStatus(int index);      // returns file name of vip-status by index
      void ClickUnlockedButton(string name);  // click on allowed button
      void ClickButtonBetsMenu(string name);  // click on bets menu button
      void ChangeMoney(int newvalue);
      void ChangeInBets(int newvalue);
      void ChangeMoneyAndInbets(int newmoney,int newinbets);  // visual effect of change money amount and bets
      void WinnerMoney(int winner);                           // visual effect of win
      void ChangePlay();                                      // visual effect of play
      void PressAllBitmap();                                  // visual effect of all sectors pressed
      void ChangeVipStatus(int newvipstatus);                 // visual effect of vip status change
      void RoulettePlaying(int random);               // Roulette playing
      void CreateBetsMenu(string name);               // Creates bets menu
      void CloseBetsMenu(string name);                // Closes bets meny
      bool IdGameObjects(string name);                // Returns result: true-base, false-alternative
      void MainGameObjects(string name);              // base mode
      void AlternativeGameObjects(string name);       // alternative mode
   
   public:
      CRouletteGame();
      void CreateGameObjects();        // creates all graphic objects
      void DeleteGameObjects();        // deletes all graphic objects
      // Graphic objects click processing method (bitmaps/buttons)
      // The first it determine the mode (base or alternative), the next it
      // proceed depending on mode
      void ClickGameObjects(string name);        
  };
//+------------------------------------------------------------------+
void CRouletteGame::CRouletteGame(void)
  {
   rg="\\RouletteGame\\";
  }
//+------------------------------------------------------------------+
void CRouletteGame::CreateBitmap(string name,int x,int y,string off,bool saw=true)
  {
   ObjectCreate(0,name,OBJ_BITMAP_LABEL,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetInteger(0,name,OBJPROP_COLOR,Tan);
   ObjectSetInteger(0,name,OBJPROP_CORNER,CORNER_LEFT_UPPER);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_STATE,true);
   ObjectSetString(0,name,OBJPROP_BMPFILE,0,rg+off);
   if(saw==true) ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   else ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
  }
//+------------------------------------------------------------------+
void CRouletteGame::CreateBitmapButton(string name,int x,int y,string on,string off,bool saw=true)
  {
   ObjectCreate(0,name,OBJ_BITMAP_LABEL,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetInteger(0,name,OBJPROP_COLOR,Tan);
   ObjectSetInteger(0,name,OBJPROP_CORNER,CORNER_LEFT_UPPER);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_STATE,false);
   ObjectSetString(0,name,OBJPROP_BMPFILE,1,rg+off);
   ObjectSetString(0,name,OBJPROP_BMPFILE,0,rg+on);
   if(saw==true) ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   else ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
  }
//+------------------------------------------------------------------+
void CRouletteGame::CreateButton(string name,string text,int x,int y,int xsize,int ysize,int fontsize,bool saw=true)
  {
   ObjectCreate(0,name,OBJ_BUTTON,0,0,0);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   ObjectSetInteger(0,name,OBJPROP_COLOR,Tan);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xsize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ysize);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,C'118,53,21');
   ObjectSetInteger(0,name,OBJPROP_CORNER,CORNER_LEFT_UPPER);
   ObjectSetString(0,name,OBJPROP_FONT,"Stencil");
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,fontsize);
   ObjectSetInteger(0,name,OBJPROP_STATE,false);
   if(saw==true) ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   else ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
  }
//+------------------------------------------------------------------+
void CRouletteGame::CreateLabel(string name,string text,int x,int y,int fontsize,bool saw=true)
  {
   ObjectCreate(0,name,OBJ_LABEL,0,0,0);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   ObjectSetInteger(0,name,OBJPROP_COLOR,Tan);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetDouble(0,name,OBJPROP_ANGLE,0.0);
   ObjectSetInteger(0,name,OBJPROP_ANCHOR,ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0,name,OBJPROP_CORNER,CORNER_LEFT_UPPER);
   ObjectSetString(0,name,OBJPROP_FONT,"Stencil");
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,fontsize);
   if(saw==true) ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   else ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
  }
//+------------------------------------------------------------------+
int CRouletteGame::GenerateRandom()
  {
   int random=-1;
   random=rand()%37;
   return(random);
  }
//+------------------------------------------------------------------+
int CRouletteGame::GetIndex(string name)
  {
   string Name[39]={ "rg_zero","rg_red","rg_black","rg_red1","rg_black2","rg_red3","rg_black4","rg_red5","rg_black6",
   "rg_red7","rg_black8","rg_red9","rg_black10","rg_black11","rg_red12","rg_black13","rg_red14","rg_black15","rg_red16",
   "rg_black17","rg_red18","rg_red19","rg_black20","rg_red21","rg_black22","rg_red23","rg_black24","rg_red25","rg_black26",
   "rg_red27","rg_black28","rg_black29","rg_red30","rg_black31","rg_red32","rg_black33","rg_red34","rg_black35","rg_red36" };
   int index=-1;
   for(int i=0; i<=38; i++)
    {
     if(name==Name[i]) { index=i; break; }
    }
   return(index);
  }
//+------------------------------------------------------------------+
string CRouletteGame::GetName(int index)
  {
   if(index<0 || index>38) return("");
   string Name[39]={ "rg_zero","rg_red","rg_black","rg_red1","rg_black2","rg_red3","rg_black4","rg_red5","rg_black6",
   "rg_red7","rg_black8","rg_red9","rg_black10","rg_black11","rg_red12","rg_black13","rg_red14","rg_black15","rg_red16",
   "rg_black17","rg_red18","rg_red19","rg_black20","rg_red21","rg_black22","rg_red23","rg_black24","rg_red25","rg_black26",
   "rg_red27","rg_black28","rg_black29","rg_red30","rg_black31","rg_red32","rg_black33","rg_red34","rg_black35","rg_red36" };
   return(Name[index]);
  }
//+------------------------------------------------------------------+
string CRouletteGame::GetPicOn(int index)
  {
   if(index<0 || index>38) return("");
   string PicOn[39]={ "zero_on.bmp","red_on.bmp","black_on.bmp","red1_on.bmp","black2_on.bmp","red3_on.bmp",
   "black4_on.bmp","red5_on.bmp","black6_on.bmp","red7_on.bmp","black8_on.bmp","red9_on.bmp","black10_on.bmp","black11_on.bmp",
   "red12_on.bmp","black13_on.bmp","red14_on.bmp","black15_on.bmp","red16_on.bmp","black17_on.bmp","red18_on.bmp",
   "red19_on.bmp","black20_on.bmp","red21_on.bmp","black22_on.bmp","red23_on.bmp","black24_on.bmp","red25_on.bmp",
   "black26_on.bmp","red27_on.bmp","black28_on.bmp","black29_on.bmp","red30_on.bmp","black31_on.bmp","red32_on.bmp",
   "black33_on.bmp","red34_on.bmp","black35_on.bmp","red36_on.bmp" };
   return(PicOn[index]);
  }
//+------------------------------------------------------------------+
string CRouletteGame::GetPicOff(int index)
  {
   if(index<0 || index>38) return("");
   string PicOff[39]={ "zero_off.bmp","red_off.bmp","black_off.bmp","red1_off.bmp","black2_off.bmp","red3_off.bmp",
   "black4_off.bmp","red5_off.bmp","black6_off.bmp","red7_off.bmp","black8_off.bmp","red9_off.bmp","black10_off.bmp",
   "black11_off.bmp","red12_off.bmp","black13_off.bmp","red14_off.bmp","black15_off.bmp","red16_off.bmp","black17_off.bmp",
   "red18_off.bmp","red19_off.bmp","black20_off.bmp","red21_off.bmp","black22_off.bmp","red23_off.bmp","black24_off.bmp",
   "red25_off.bmp","black26_off.bmp","red27_off.bmp","black28_off.bmp","black29_off.bmp","red30_off.bmp","black31_off.bmp",
   "red32_off.bmp","black33_off.bmp","red34_off.bmp","black35_off.bmp","red36_off.bmp" };
   return(PicOff[index]);
  }
//+------------------------------------------------------------------+
string CRouletteGame::GetPicBetOn(int index)
  {
   if(index<0 || index>38) return("");
   string PicBetOn[39]={ "bet_zero_on.bmp","bet_red_on.bmp","bet_black_on.bmp","bet_red1_on.bmp","bet_black2_on.bmp",
   "bet_red3_on.bmp","bet_black4_on.bmp","bet_red5_on.bmp","bet_black6_on.bmp","bet_red7_on.bmp","bet_black8_on.bmp",
   "bet_red9_on.bmp","bet_black10_on.bmp","bet_black11_on.bmp","bet_red12_on.bmp","bet_black13_on.bmp","bet_red14_on.bmp",
   "bet_black15_on.bmp","bet_red16_on.bmp","bet_black17_on.bmp","bet_red18_on.bmp","bet_red19_on.bmp","bet_black20_on.bmp",
   "bet_red21_on.bmp","bet_black22_on.bmp","bet_red23_on.bmp","bet_black24_on.bmp","bet_red25_on.bmp","bet_black26_on.bmp",
   "bet_red27_on.bmp","bet_black28_on.bmp","bet_black29_on.bmp","bet_red30_on.bmp","bet_black31_on.bmp","bet_red32_on.bmp",
   "bet_black33_on.bmp","bet_red34_on.bmp","bet_black35_on.bmp","bet_red36_on.bmp" };
   return(PicBetOn[index]);
  }
//+------------------------------------------------------------------+
string CRouletteGame::GetPicBetOff(int index)
  {
   if(index<0 || index>38) return("");
   string PicBetOff[39]={ "bet_zero_off.bmp","bet_red_off.bmp","bet_black_off.bmp","bet_red1_off.bmp",
   "bet_black2_off.bmp","bet_red3_off.bmp","bet_black4_off.bmp","bet_red5_off.bmp","bet_black6_off.bmp","bet_red7_off.bmp",
   "bet_black8_off.bmp","bet_red9_off.bmp","bet_black10_off.bmp","bet_black11_off.bmp","bet_red12_off.bmp",
   "bet_black13_off.bmp","bet_red14_off.bmp","bet_black15_off.bmp","bet_red16_off.bmp","bet_black17_off.bmp",
   "bet_red18_off.bmp","bet_red19_off.bmp","bet_black20_off.bmp","bet_red21_off.bmp","bet_black22_off.bmp","bet_red23_off.bmp",
   "bet_black24_off.bmp","bet_red25_off.bmp","bet_black26_off.bmp","bet_red27_off.bmp","bet_black28_off.bmp",
   "bet_black29_off.bmp","bet_red30_off.bmp","bet_black31_off.bmp","bet_red32_off.bmp","bet_black33_off.bmp",
   "bet_red34_off.bmp","bet_black35_off.bmp","bet_red36_off.bmp" };
   return(PicBetOff[index]);
  }
//+------------------------------------------------------------------+
string CRouletteGame::GetPicVipStatus(int index)
  {
   if(index<0 || index>5) return("");
   string PicVipStatus[6]={ "bronze.bmp","silver.bmp","gold.bmp","platinum.bmp","supernova.bmp","supernova_elite.bmp" };
   return(PicVipStatus[index]);
  }
//+------------------------------------------------------------------+
void CRouletteGame::RoulettePlaying(int random)
  {
   ObjectSetInteger(0,"rg_play",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ChartRedraw();
   
   ChangePlay();
   
   string BigPic[37]={ "big_zero.bmp","big_red1.bmp","big_black2.bmp","big_red3.bmp","big_black4.bmp","big_red5.bmp",
   "big_black6.bmp","big_red7.bmp","big_black8.bmp","big_red9.bmp","big_black10.bmp","big_black11.bmp","big_red12.bmp",
   "big_black13.bmp","big_red14.bmp","big_black15.bmp","big_red16.bmp","big_black17.bmp","big_red18.bmp","big_red19.bmp",
   "big_black20.bmp","big_red21.bmp","big_black22.bmp","big_red23.bmp","big_black24.bmp","big_red25.bmp","big_black26.bmp",
   "big_red27.bmp","big_black28.bmp","big_black29.bmp","big_red30.bmp","big_black31.bmp","big_red32.bmp","big_black33.bmp",
   "big_red34.bmp","big_black35.bmp","big_red36.bmp" };
   Playing=true;
   PlaySound(rg+"rg_snd1.wav");
   Sleep(3000);
   ObjectSetInteger(0,"rg_playing",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   for(int i=0; i<=36; i++)
    {
     PlaySound(rg+"rg_snd2.wav");
     ObjectSetString(0,"rg_playing",OBJPROP_BMPFILE,0,rg+BigPic[i]);
     ObjectSetString(0,"rg_playing",OBJPROP_BMPFILE,1,rg+BigPic[i]);
     ChartRedraw();
     Sleep(100);
    }
   PlaySound(rg+"rg_snd3.wav");
   ObjectSetString(0,"rg_playing",OBJPROP_BMPFILE,0,rg+BigPic[random]);
   ObjectSetString(0,"rg_playing",OBJPROP_BMPFILE,1,rg+BigPic[random]);
   ChartRedraw();

   Winner=0;
   switch(random)
    {
     case 0: Winner+=Bets[0]*36; break;                          // zero
     case 1: Winner+=Bets[3]*36; Winner+=Bets[1]*2; break;       // red1
     case 2: Winner+=Bets[4]*36; Winner+=Bets[2]*2; break;       // black2
     case 3: Winner+=Bets[5]*36; Winner+=Bets[1]*2; break;       // red3
     case 4: Winner+=Bets[6]*36; Winner+=Bets[2]*2; break;       // black4
     case 5: Winner+=Bets[7]*36; Winner+=Bets[1]*2; break;       // red5
     case 6: Winner+=Bets[8]*36; Winner+=Bets[2]*2; break;       // black6
     case 7: Winner+=Bets[9]*36; Winner+=Bets[1]*2; break;       // red7
     case 8: Winner+=Bets[10]*36; Winner+=Bets[2]*2; break;      // black8
     case 9: Winner+=Bets[11]*36; Winner+=Bets[1]*2; break;      // red9
     case 10: Winner+=Bets[12]*36; Winner+=Bets[2]*2; break;     // black10
     case 11: Winner+=Bets[13]*36; Winner+=Bets[2]*2; break;     // black11
     case 12: Winner+=Bets[14]*36; Winner+=Bets[1]*2; break;     // red12
     case 13: Winner+=Bets[15]*36; Winner+=Bets[2]*2; break;     // black13
     case 14: Winner+=Bets[16]*36; Winner+=Bets[1]*2; break;     // red14
     case 15: Winner+=Bets[17]*36; Winner+=Bets[2]*2; break;     // black15
     case 16: Winner+=Bets[18]*36; Winner+=Bets[1]*2; break;     // red16
     case 17: Winner+=Bets[19]*36; Winner+=Bets[2]*2; break;     // black17
     case 18: Winner+=Bets[20]*36; Winner+=Bets[1]*2; break;     // red18
     case 19: Winner+=Bets[21]*36; Winner+=Bets[1]*2; break;     // red19
     case 20: Winner+=Bets[22]*36; Winner+=Bets[2]*2; break;     // black20
     case 21: Winner+=Bets[23]*36; Winner+=Bets[1]*2; break;     // red21
     case 22: Winner+=Bets[24]*36; Winner+=Bets[2]*2; break;     // black22
     case 23: Winner+=Bets[25]*36; Winner+=Bets[1]*2; break;     // red23
     case 24: Winner+=Bets[26]*36; Winner+=Bets[2]*2; break;     // black24
     case 25: Winner+=Bets[27]*36; Winner+=Bets[1]*2; break;     // red25
     case 26: Winner+=Bets[28]*36; Winner+=Bets[2]*2; break;     // black26
     case 27: Winner+=Bets[29]*36; Winner+=Bets[1]*2; break;     // red27
     case 28: Winner+=Bets[30]*36; Winner+=Bets[2]*2; break;     // black28
     case 29: Winner+=Bets[31]*36; Winner+=Bets[2]*2; break;     // black29
     case 30: Winner+=Bets[32]*36; Winner+=Bets[1]*2; break;     // red30
     case 31: Winner+=Bets[33]*36; Winner+=Bets[2]*2; break;     // black31
     case 32: Winner+=Bets[34]*36; Winner+=Bets[1]*2; break;     // red32
     case 33: Winner+=Bets[35]*36; Winner+=Bets[2]*2; break;     // black33
     case 34: Winner+=Bets[36]*36; Winner+=Bets[1]*2; break;     // red34
     case 35: Winner+=Bets[37]*36; Winner+=Bets[2]*2; break;     // black35
     case 36: Winner+=Bets[38]*36; Winner+=Bets[1]*2;            // red36
    }
   Sleep(3000);
   if(Winner>0) WinnerMoney(Winner);
   
   PressAllBitmap();
   
   TotalInBets+=InBets;
   if(TotalInBets>=25000 && VipStatus==0) ChangeVipStatus(1);
   if(TotalInBets>=75000 && VipStatus<=1) ChangeVipStatus(2);
   if(TotalInBets>=150000 && VipStatus<=2) ChangeVipStatus(3); 
   if(TotalInBets>=350000 && VipStatus<=3) ChangeVipStatus(4);
   if(TotalInBets>=1000000 && VipStatus<=4) ChangeVipStatus(5);
   
   InBets=0;
   ObjectSetString(0,"rg_money_bets_val",OBJPROP_TEXT,IntegerToString(InBets)+"$");
   ObjectSetInteger(0,"rg_playing",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ChartRedraw();
   
   ChangePlay();
   
   ObjectSetInteger(0,"rg_play",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ChartRedraw();
 
   ArrayInitialize(Bets,0);
   Playing=false;
  }
//+------------------------------------------------------------------+
void CRouletteGame::PressAllBitmap(void)
  {
   int i;
   PlaySound(rg+"rg_snd4.wav");
   for(i=0; i<=38; i++)
    {
     ObjectSetInteger(0,GetName(i),OBJPROP_STATE,true);
    }
   ChartRedraw();

   Sleep(1500);
   PlaySound(rg+"rg_snd4.wav");
   for(i=0; i<=38; i++)
    {
     ObjectSetInteger(0,GetName(i),OBJPROP_STATE,false);
     ObjectSetString(0,GetName(i),OBJPROP_BMPFILE,0,rg+GetPicOn(i));
     ObjectSetString(0,GetName(i),OBJPROP_BMPFILE,1,rg+GetPicOff(i));
    }
  ChartRedraw();
  Sleep(1500);
  }
//+------------------------------------------------------------------+
void CRouletteGame::WinnerMoney(int winner)
  {
   int i;
   int step;
   int stop;
   step=winner/100;
   stop=Money+winner;
   
   PlaySound(rg+"rg_snd5"); Sleep(2500);
   ObjectSetInteger(0,"rg_money_val",OBJPROP_YDISTANCE,82);
   ObjectSetInteger(0,"rg_money_val",OBJPROP_FONTSIZE,24);
   for(i=Money; i<=stop; i+=step)
    {
     PlaySound(rg+"rg_snd6.wav");
     Money=i;
     ObjectSetString(0,"rg_money_val",OBJPROP_TEXT,IntegerToString(Money)+"$");
     Sleep(10);
     ChartRedraw();
    }
   ObjectSetInteger(0,"rg_money_val",OBJPROP_YDISTANCE,91);
   ObjectSetInteger(0,"rg_money_val",OBJPROP_FONTSIZE,16);
   ChartRedraw();
   Sleep(1500);
  }
//+------------------------------------------------------------------+
void CRouletteGame::ChangePlay(void)
  {
   ObjectSetInteger(0,"rg_pplay",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_pplay",OBJPROP_STATE,true);
   int i;
   for(i=0; i<=10; i++)
    {
     PlaySound(rg+"rg_snd7.wav");
     if(ObjectGetString(0,"rg_pplay",OBJPROP_BMPFILE,0)==rg+"play_up.bmp") ObjectSetString(0,"rg_pplay",OBJPROP_BMPFILE,0,rg+"play_down.bmp");
     else ObjectSetString(0,"rg_pplay",OBJPROP_BMPFILE,0,rg+"play_up.bmp");
     ChartRedraw();
     Sleep(50);
    }
   ObjectSetInteger(0,"rg_pplay",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ChartRedraw();
   Sleep(500);
  }
//+------------------------------------------------------------------+
void CRouletteGame::ChangeVipStatus(int newvipstatus)
  {
   ObjectSetInteger(0,"rg_vipstatus",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ChartRedraw();
   PlaySound(rg+"rg_snd8.wav");
   Sleep(1500);

   PlaySound(rg+"rg_snd8.wav");
   ObjectSetInteger(0,"rg_vipstatus",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetString(0,"rg_vipstatus",OBJPROP_BMPFILE,0,rg+GetPicVipStatus(newvipstatus));
   ObjectSetString(0,"rg_vipstatus",OBJPROP_BMPFILE,1,rg+GetPicVipStatus(newvipstatus));
   ChartRedraw();
   Sleep(1500);
   VipStatus++;
  }
//+------------------------------------------------------------------+
void CRouletteGame::ClickUnlockedButton(string name)
  {
   PlaySound(rg+"rg_snd9.wav");
   Sleep(100);
   ObjectSetInteger(0,name,OBJPROP_STATE,false);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
void CRouletteGame::ClickButtonBetsMenu(string name)
  {
   PlaySound(rg+"rg_snd10.wav");
   Sleep(100);
   ObjectSetInteger(0,name,OBJPROP_STATE,false);
   ChartRedraw();
   ObjectSetString(0,"rg_better_val",OBJPROP_TEXT,IntegerToString(Better)+"$");
   PlaySound(rg+"rg_snd11.wav");
   ChartRedraw();
  }
//+------------------------------------------------------------------+
void CRouletteGame::ChangeMoney(int newvalue)
  {
   ObjectSetString(0,"rg_money_val",OBJPROP_TEXT,IntegerToString(newvalue)+"$");
   ObjectSetInteger(0,"rg_money_val",OBJPROP_YDISTANCE,82);
   ObjectSetInteger(0,"rg_money_val",OBJPROP_FONTSIZE,24);
   ChartRedraw();
   PlaySound(rg+"rg_snd12.wav");
   Sleep(150);
   ObjectSetInteger(0,"rg_money_val",OBJPROP_YDISTANCE,91);
   ObjectSetInteger(0,"rg_money_val",OBJPROP_FONTSIZE,16);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
void CRouletteGame::ChangeInBets(int newvalue)
  {
   ObjectSetString(0,"rg_money_bets_val",OBJPROP_TEXT,IntegerToString(newvalue)+"$");
   ObjectSetInteger(0,"rg_money_bets_val",OBJPROP_YDISTANCE,131);
   ObjectSetInteger(0,"rg_money_bets_val",OBJPROP_FONTSIZE,24);
   ChartRedraw();
   PlaySound(rg+"rg_snd12.wav");
   Sleep(150);
   ObjectSetInteger(0,"rg_money_bets_val",OBJPROP_YDISTANCE,140);
   ObjectSetInteger(0,"rg_money_bets_val",OBJPROP_FONTSIZE,16);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
void CRouletteGame::ChangeMoneyAndInbets(int newmoney,int newinbets)
  {
   if(VarBetter==1)
    {
     ChangeMoney(newmoney); Sleep(150); ChangeInBets(newinbets);
    }
   if(VarBetter==-1)
    {
     ChangeInBets(newinbets); Sleep(150); ChangeMoney(newmoney);
    }  
  }
//+------------------------------------------------------------------+
void CRouletteGame::CreateGameObjects()
  {
   Money=10000;
   InBets=0;
   Better=0;
   VipStatus=0;
   TotalInBets=0;
   BetsMenu=false;
   Playing=false;
   ArrayInitialize(Bets,0);
   CreateBitmap("rg_background",0,0,"background.bmp");
   CreateBitmapButton("rg_zero",21,198,"zero_on.bmp","zero_off.bmp");
   CreateBitmapButton("rg_red",21,229,"red_on.bmp","red_off.bmp");
   CreateBitmapButton("rg_black",21,260,"black_on.bmp","black_off.bmp");
   CreateBitmapButton("rg_red1",52,198,"red1_on.bmp","red1_off.bmp");
   CreateBitmapButton("rg_black2",52,229,"black2_on.bmp","black2_off.bmp");
   CreateBitmapButton("rg_red3",52,260,"red3_on.bmp","red3_off.bmp");
   CreateBitmapButton("rg_black4",83,198,"black4_on.bmp","black4_off.bmp");
   CreateBitmapButton("rg_red5",83,229,"red5_on.bmp","red5_off.bmp");
   CreateBitmapButton("rg_black6",83,260,"black6_on.bmp","black6_off.bmp");
   CreateBitmapButton("rg_red7",114,198,"red7_on.bmp","red7_off.bmp");
   CreateBitmapButton("rg_black8",114,229,"black8_on.bmp","black8_off.bmp");
   CreateBitmapButton("rg_red9",114,260,"red9_on.bmp","red9_off.bmp");
   CreateBitmapButton("rg_black10",145,198,"black10_on.bmp","black10_off.bmp");
   CreateBitmapButton("rg_black11",145,229,"black11_on.bmp","black11_off.bmp");
   CreateBitmapButton("rg_red12",145,260,"red12_on.bmp","red12_off.bmp");
   CreateBitmapButton("rg_black13",176,198,"black13_on.bmp","black13_off.bmp");
   CreateBitmapButton("rg_red14",176,229,"red14_on.bmp","red14_off.bmp");
   CreateBitmapButton("rg_black15",176,260,"black15_on.bmp","black15_off.bmp");
   CreateBitmapButton("rg_red16",207,198,"red16_on.bmp","red16_off.bmp");
   CreateBitmapButton("rg_black17",207,229,"black17_on.bmp","black17_off.bmp");
   CreateBitmapButton("rg_red18",207,260,"red18_on.bmp","red18_off.bmp");
   CreateBitmapButton("rg_red19",238,198,"red19_on.bmp","red19_off.bmp");
   CreateBitmapButton("rg_black20",238,229,"black20_on.bmp","black20_off.bmp");
   CreateBitmapButton("rg_red21",238,260,"red21_on.bmp","red21_off.bmp");
   CreateBitmapButton("rg_black22",269,198,"black22_on.bmp","black22_off.bmp");
   CreateBitmapButton("rg_red23",269,229,"red23_on.bmp","red23_off.bmp");
   CreateBitmapButton("rg_black24",269,260,"black24_on.bmp","black24_off.bmp");
   CreateBitmapButton("rg_red25",300,198,"red25_on.bmp","red25_off.bmp");
   CreateBitmapButton("rg_black26",300,229,"black26_on.bmp","black26_off.bmp");
   CreateBitmapButton("rg_red27",300,260,"red27_on.bmp","red27_off.bmp");
   CreateBitmapButton("rg_black28",331,198,"black28_on.bmp","black28_off.bmp");
   CreateBitmapButton("rg_black29",331,229,"black29_on.bmp","black29_off.bmp");
   CreateBitmapButton("rg_red30",331,260,"red30_on.bmp","red30_off.bmp");
   CreateBitmapButton("rg_black31",362,198,"black31_on.bmp","black31_off.bmp");
   CreateBitmapButton("rg_red32",362,229,"red32_on.bmp","red32_off.bmp");
   CreateBitmapButton("rg_black33",362,260,"black33_on.bmp","black33_off.bmp");
   CreateBitmapButton("rg_red34",393,198,"red34_on.bmp","red34_off.bmp");
   CreateBitmapButton("rg_black35",393,229,"black35_on.bmp","black35_off.bmp");
   CreateBitmapButton("rg_red36",393,260,"red36_on.bmp","red36_off.bmp");
   CreateLabel("rg_money","MONEY:",18,91,16);
   CreateLabel("rg_money_bets","IN BETS:",18,140,16);
   CreateLabel("rg_money_val",IntegerToString(Money)+"$",130,91,16);
   CreateLabel("rg_money_bets_val",IntegerToString(InBets)+"$",130,140,16);
   CreateButton("rg_newgame","NEW GAME",442,23,125,25,14);
   CreateButton("rg_exitgame","EXIT GAME",569,23,125,25,14);
   CreateBitmap("rg_vipstatus",465,79,GetPicVipStatus(VipStatus));
   CreateBitmapButton("rg_play",318,79,"play_on.bmp","play_off.bmp");
   CreateBitmapButton("rg_pplay",318,79,"play_up.bmp","play_down.bmp",false);
   CreateButton("rg_closemenu","CLOSE MENU",569,23,125,25,14,false);
   CreateLabel("rg_better","",405,22,18,false);
   CreateLabel("rg_better_val",IntegerToString(Better)+"$",405,142,18,false);
   CreateButton("rg_bet_100","100$",395,60,100,25,12,false);
   CreateButton("rg_bet_500","500$",496,60,100,25,12,false);
   CreateButton("rg_bet_1k","1 000$",597,60,100,25,12,false);
   CreateButton("rg_bet_5k","5 000$",395,86,100,25,12,false);
   CreateButton("rg_bet_10k","10 000$",496,86,100,25,12,false);
   CreateButton("rg_bet_c","C",597,86,100,25,12,false);
   CreateButton("rg_bet_100k","100 000$",395,112,100,25,12,false);
   CreateButton("rg_bet_1mio","1 000 000$",496,112,100,25,12,false);
   CreateButton("rg_bet_all","ALL MONEY",597,112,100,25,12,false);
   CreateBitmap("rg_playing",612,212,"",false);
  }
//+------------------------------------------------------------------+
void CRouletteGame::DeleteGameObjects(void)
  {
   ObjectDelete(0,"rg_background");
   ObjectDelete(0,"rg_zero");
   ObjectDelete(0,"rg_red");
   ObjectDelete(0,"rg_black");
   ObjectDelete(0,"rg_red1");
   ObjectDelete(0,"rg_black2");
   ObjectDelete(0,"rg_red3");
   ObjectDelete(0,"rg_black4");
   ObjectDelete(0,"rg_red5");
   ObjectDelete(0,"rg_black6");
   ObjectDelete(0,"rg_red7");
   ObjectDelete(0,"rg_black8");
   ObjectDelete(0,"rg_red9");
   ObjectDelete(0,"rg_black10");
   ObjectDelete(0,"rg_black11");
   ObjectDelete(0,"rg_red12");
   ObjectDelete(0,"rg_black13");
   ObjectDelete(0,"rg_red14");
   ObjectDelete(0,"rg_black15");
   ObjectDelete(0,"rg_red16");
   ObjectDelete(0,"rg_black17");
   ObjectDelete(0,"rg_red18");
   ObjectDelete(0,"rg_red19");
   ObjectDelete(0,"rg_black20");
   ObjectDelete(0,"rg_red21");
   ObjectDelete(0,"rg_black22");
   ObjectDelete(0,"rg_red23");
   ObjectDelete(0,"rg_black24");
   ObjectDelete(0,"rg_red25");
   ObjectDelete(0,"rg_black26");
   ObjectDelete(0,"rg_red27");
   ObjectDelete(0,"rg_black28");
   ObjectDelete(0,"rg_black29");
   ObjectDelete(0,"rg_red30");
   ObjectDelete(0,"rg_black31");
   ObjectDelete(0,"rg_red32");
   ObjectDelete(0,"rg_black33");
   ObjectDelete(0,"rg_red34");
   ObjectDelete(0,"rg_black35");
   ObjectDelete(0,"rg_red36");
   ObjectDelete(0,"rg_money");
   ObjectDelete(0,"rg_money_bets");
   ObjectDelete(0,"rg_money_val");
   ObjectDelete(0,"rg_money_bets_val");
   ObjectDelete(0,"rg_newgame");
   ObjectDelete(0,"rg_exitgame");
   ObjectDelete(0,"rg_vipstatus");
   ObjectDelete(0,"rg_play");
   ObjectDelete(0,"rg_pplay");
   ObjectDelete(0,"rg_closemenu");
   ObjectDelete(0,"rg_better");
   ObjectDelete(0,"rg_better_val");
   ObjectDelete(0,"rg_bet_100");
   ObjectDelete(0,"rg_bet_500");
   ObjectDelete(0,"rg_bet_1k");
   ObjectDelete(0,"rg_bet_5k");
   ObjectDelete(0,"rg_bet_10k");
   ObjectDelete(0,"rg_bet_100k");
   ObjectDelete(0,"rg_bet_1mio");
   ObjectDelete(0,"rg_bet_all");
   ObjectDelete(0,"rg_bet_c");
   ObjectDelete(0,"rg_playing");
  }
//+------------------------------------------------------------------+
void CRouletteGame::CreateBetsMenu(string name)
  {
   string NameBetsMenu[39]={ "ZERO","RED","BLACK","RED 1","BLACK 2","RED 3","BLACK 4","RED 5","BLACK 6","RED 7","BLACK 8",
   "RED 9","BLACK 10","BLACK 11","RED 12","BLACK 13","RED 14","BLACK 15","RED 16","BLACK 17","RED 18","RED 19","BLACK 20",
   "RED 21","BLACK 22","RED 23","BLACK 24","RED 25","BLACK 26","RED 27","BLACK 28","BLACK 29","RED 30","BLACK 31","RED 32",
   "BLACK 33","RED 34","BLACK 35","RED 36" };
   int index=GetIndex(name);
   
   ObjectSetInteger(0,"rg_vipstatus",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_play",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_newgame",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_exitgame",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_background_bets",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_closemenu",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_better",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_better_val",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_100",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_500",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_1k",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_5k",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_10k",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_100k",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_1mio",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_all",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_bet_c",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   
   Better=Bets[index];
   ObjectSetString(0,"rg_better",OBJPROP_TEXT,NameBetsMenu[index]);
   ObjectSetString(0,"rg_better_val",OBJPROP_TEXT,IntegerToString(Better)+"$");
   BetsMenu=true;
   CurrentIndex=index;
   VarBetter=0;
   ChartRedraw();
  }
//+------------------------------------------------------------------+
void CRouletteGame::CloseBetsMenu(string name)
  {
   ObjectSetInteger(0,"rg_vipstatus",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_play",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_newgame",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_exitgame",OBJPROP_TIMEFRAMES,OBJ_ALL_PERIODS);
   ObjectSetInteger(0,"rg_background_bets",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_closemenu",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_better",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_better_val",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_100",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_500",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_1k",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_5k",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_10k",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_100k",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_1mio",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_all",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   ObjectSetInteger(0,"rg_bet_c",OBJPROP_TIMEFRAMES,OBJ_NO_PERIODS);
   
   ObjectSetInteger(0,GetName(CurrentIndex),OBJPROP_STATE,false);
   if(Better>0)
    {
     ObjectSetString(0,GetName(CurrentIndex),OBJPROP_BMPFILE,0,rg+GetPicBetOn(CurrentIndex));
     ObjectSetString(0,GetName(CurrentIndex),OBJPROP_BMPFILE,1,rg+GetPicBetOff(CurrentIndex));
    }
   else
    {
     ObjectSetString(0,GetName(CurrentIndex),OBJPROP_BMPFILE,0,rg+GetPicOn(CurrentIndex));
     ObjectSetString(0,GetName(CurrentIndex),OBJPROP_BMPFILE,1,rg+GetPicOff(CurrentIndex));
    }
   if(Better>Bets[CurrentIndex]) // bet greater
    {
     Money-=(Better-Bets[CurrentIndex]);
     InBets+=(Better-Bets[CurrentIndex]);
     VarBetter=1;
    }
   if(Better<Bets[CurrentIndex]) // bet lower
    {
     Money+=(Bets[CurrentIndex]-Better);
     InBets-=(Bets[CurrentIndex]-Better);
     VarBetter=-1;
    }

   Bets[CurrentIndex]=Better;
   CurrentIndex=-1;
   BetsMenu=false;
   ChartRedraw();
   Sleep(250);
  }
//+------------------------------------------------------------------+
bool CRouletteGame::IdGameObjects(string name)
  {
   //---- bitmaps for sectors
   if(name=="rg_zero" || name=="rg_red" || name=="rg_black" || name=="rg_red1" || name=="rg_black2" || name=="rg_red3" ||
   name=="rg_black4" || name=="rg_red5" || name=="rg_black6" || name=="rg_red7" || name=="rg_black8" || name=="rg_red9"
   || name=="rg_black10" || name=="rg_black11" || name=="rg_red12" || name=="rg_black13" || name=="rg_red14" || 
   name=="rg_black15" || name=="rg_red16" || name=="rg_black17" || name=="rg_red18" || name=="rg_red19" ||
   name=="rg_black20" || name=="rg_red21" || name=="rg_black22" || name=="rg_red23" || name=="rg_black24" ||
   name=="rg_red25" || name=="rg_black26" || name=="rg_red27" || name=="rg_black28" || name=="rg_black29" ||
   name=="rg_red30" || name=="rg_black31" || name=="rg_red32" || name=="rg_black33" || name=="rg_red34" ||
   name=="rg_black35" || name=="rg_red36")
    {
     if(BetsMenu==false) return(true);
     else return(false);
    }
   //---- "new game" and "exit game" buttons
   if(name=="rg_newgame") return(true);
   if(name=="rg_exitgame") return(true);
   //---- play
   if(name=="rg_play")
    {
     if(Playing==true || InBets==0) return(false);
     else return(true);
    }
   //---- "close menu" button
   if(name=="rg_closemenu") return(true);
   //---- bets
   if(name=="rg_bet_100")
    {
     if(Money+Bets[CurrentIndex]-Better-100>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_500")
    {
     if(Money+Bets[CurrentIndex]-Better-500>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_1k")
    {
     if(Money+Bets[CurrentIndex]-Better-1000>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_5k")
    {
     if(Money+Bets[CurrentIndex]-Better-5000>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_10k")
    {
     if(Money+Bets[CurrentIndex]-Better-10000>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_100k")
    {
     if(Money+Bets[CurrentIndex]-Better-100000>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_1mio")
    {
     if(Money+Bets[CurrentIndex]-Better-1000000>=0) return(true);
     else return(false);
    }
   if(name=="rg_bet_all") return(true);
   if(name=="rg_bet_c") return(true);
   //----
   return(false);
  }
//+------------------------------------------------------------------+
void CRouletteGame::MainGameObjects(string name)
  {
   //---- bitmaps for sectors
   if(name=="rg_zero" || name=="rg_red" || name=="rg_black" || name=="rg_red1" || name=="rg_black2" || name=="rg_red3" ||
   name=="rg_black4" || name=="rg_red5" || name=="rg_black6" || name=="rg_red7" || name=="rg_black8" || name=="rg_red9"
   || name=="rg_black10" || name=="rg_black11" || name=="rg_red12" || name=="rg_black13" || name=="rg_red14" || 
   name=="rg_black15" || name=="rg_red16" || name=="rg_black17" || name=="rg_red18" || name=="rg_red19" ||
   name=="rg_black20" || name=="rg_red21" || name=="rg_black22" || name=="rg_red23" || name=="rg_black24" ||
   name=="rg_red25" || name=="rg_black26" || name=="rg_red27" || name=="rg_black28" || name=="rg_black29" ||
   name=="rg_red30" || name=="rg_black31" || name=="rg_red32" || name=="rg_black33" || name=="rg_red34" ||
   name=="rg_black35" || name=="rg_red36")
    {
     PlaySound(rg+"rg_snd9.wav");
     CreateBetsMenu(name);
    }
   //---- new game
   if(name=="rg_newgame")
    {
     ClickUnlockedButton(name);
     DeleteGameObjects();
     CreateGameObjects();
     ChartRedraw();
    }
   if(name=="rg_exitgame")
    {
     ClickUnlockedButton(name);
     DeleteGameObjects();
     ChartRedraw();
    }
   //---- play
   if(name=="rg_play")
    {
     Random=GenerateRandom();
     ClickUnlockedButton(name);
     RoulettePlaying(Random);
    }
   //---- close menu
   if(name=="rg_closemenu")
    {
     ClickUnlockedButton(name);
     CloseBetsMenu(name);
     ChangeMoneyAndInbets(Money,InBets);
    }
   //---- bets
   if(name=="rg_bet_100") { Better+=100; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_500") { Better+=500; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_1k") { Better+=1000; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_5k") { Better+=5000; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_10k") { Better+=10000; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_100k") { Better+=100000; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_1mio") { Better+=1000000; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_all") { Better=Money; ClickButtonBetsMenu(name); }
   if(name=="rg_bet_c") { Better=0; ClickButtonBetsMenu(name); }
   //----
  }
//+------------------------------------------------------------------+
void CRouletteGame::AlternativeGameObjects(string name)
  {
   PlaySound(rg+"rg_snd13.wav");
   if(name==GetName(CurrentIndex)) ObjectSetInteger(0,name,OBJPROP_STATE,true);
   else ObjectSetInteger(0,name,OBJPROP_STATE,false);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
void CRouletteGame::ClickGameObjects(string name)
  {
   if(IdGameObjects(name)) MainGameObjects(name); else AlternativeGameObjects(name);
  }
//+------------------------------------------------------------------+
