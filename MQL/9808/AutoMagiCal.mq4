
//=================================================================================================
//===AutoMagiCal=================================================================================
//=============================================================================================

#property   copyright "Copyright © 2010, Thomas Stutz"
#property   link      "t.s@my-sc.eu"

//===Variables=================================================================================

int      Magic,MagiCal[5],Mcar,mx;

double   mNr;

string   StrMagic;

//===Init======================================================================================

int init()

   {

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

      if(mx == 0) {AutoMagiCal();}

      return(0);

   }

//===AutoMagiCal===Automatic=Magic=Number=Calculator===========================================

void AutoMagiCal()

   {

      for(Mcar = 0; Mcar < 5; Mcar++)

         {

            MagiCal[Mcar] = StringGetChar(Symbol(), Mcar);

         }

      StrMagic = MagiCal[0] + "" + MagiCal[1] + "" + MagiCal[2] + "" + MagiCal[3] + "" + MagiCal[4] + "" + MagiCal[5];
      mNr = StrToDouble(StrMagic) ;
      mNr = mNr / 1000;
      Magic = mNr;

      ObjectCreate("MagicLabel",OBJ_LABEL,0,0,0);
      ObjectSet("MagicLabel",OBJPROP_CORNER,0);
      ObjectSet("MagicLabel",OBJPROP_XDISTANCE,10);
      ObjectSet("MagicLabel",OBJPROP_YDISTANCE,10);
      ObjectSet("MagicLabel",OBJPROP_BACK,true);
      ObjectSetText("MagicLabel", "Magic No. =  " + Magic, 9, "Arial", Lime);

      if(Magic != 0) {mx = 1;}
      else {ObjectSetText("MagicLabel", "Magic No. Errör !!!don´t Trade!!!  ", 9, "Arial", Lime);}

      return(0);

   }

//===End=======================================================================================