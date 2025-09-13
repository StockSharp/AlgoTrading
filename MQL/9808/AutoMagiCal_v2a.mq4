
//=================================================================================================
//===AutoMagiCal=v2a=============================================================================
//=============================================================================================
#property   copyright "Copyright © 2010, Thomas Stutz"
#property   link      "t.s@my-sc.eu"
//===Variables=================================================================================
int      Magic,MagiCal[5],Mcar;
double   mNr;
string   StrMagic;
//===Init======================================================================================
int init()
   {
      ObjectCreate("MagicLabel",OBJ_LABEL,0,0,0);
      ObjectSet("MagicLabel",OBJPROP_CORNER,0);
      ObjectSet("MagicLabel",OBJPROP_XDISTANCE,10);
      ObjectSet("MagicLabel",OBJPROP_YDISTANCE,10);
      ObjectSet("MagicLabel",OBJPROP_BACK,true);
      return(0);
   }
//===Deinit====================================================================================
int deinit()
   {
      ObjectDelete("MagicLabel");
      return(0);
   }
//===Start=====================================================================================
int start()
   {
      if(Magic == 0) {AutoMagiCal();}
      return(0);
   }
//===AutoMagiCal===Automatic=Magic=Number=Calculator===========================================
void AutoMagiCal()
   {
      for(Mcar = 1; Mcar < 5; Mcar++) {MagiCal[Mcar] = StringGetChar(Symbol(), Mcar);}    //change char from symbol to number
      StrMagic = MagiCal[1] + "" + MagiCal[2] + "" + MagiCal[3] + "" + MagiCal[4];        //sort number in string
      mNr = StrToDouble(StrMagic);                                                        //make string to double | while double is bigger then integer
      
      if(mNr > 999999999) {mNr = mNr / 10;}            //maximum of integer = 2´147´483´647 is bigger then 999´999´999 make / 10
      if(mNr > 9999999999) {mNr = mNr / 100;}
      else
         {
            Magic = NormalizeDouble(mNr, 0);//make double to integer
            ObjectSetText("MagicLabel", "Magic No. =  " + Magic, 9, "Arial", Lime);
         }
      return(0);
   }
//===End=======================================================================================