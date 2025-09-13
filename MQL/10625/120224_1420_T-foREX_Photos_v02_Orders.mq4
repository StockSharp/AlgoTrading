//+------------------------------------------------------------------+
//|                        120224 1420 T-foREX Photos v02 Orders.mq4 |
//|                                                          T-foREX |
//|                                               http://jobaweb.net |
//+------------------------------------------------------------------+

#property copyright "T-foREX"
#property link      "http://jobaweb.net"

datetime firstTime = 0;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

int init()
{
   //----
   string www = "http://jobaweb.net";
   if (ObjectFind(www) != 0)
   {
      ObjectCreate(www, OBJ_LABEL, 0, 0, 0);
      ObjectSet(www, OBJPROP_BACK,      true);
      ObjectSet(www, OBJPROP_CORNER,    3);
      ObjectSet(www, OBJPROP_XDISTANCE, 5);
      ObjectSet(www, OBJPROP_YDISTANCE, 250);
      ObjectSet(www, OBJPROP_ANGLE,     90);
      ObjectSetText(www, www, 10, "Tahoma", Lime);
   }

   if (firstTime == 0)
   {
      firstTime = TimeLocal();
      print("T-foREX starts @ " + TimeToStr(TimeCurrent()), true);
   }

   //----
   return(0);
}

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+

int deinit()
{
   //----

   //----
   return(0);
}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

int start()
{
   takePic();
}

int nOld = 0;
int takePic()
{
   //----
   int  pos, nOrd = 0;
   for (pos = 0; pos < OrdersTotal(); pos++) if (OrderSelect(pos, SELECT_BY_POS) && OrderSymbol() == Symbol()) nOrd += 1;

   if (nOrd != nOld)
   {
      nOld = nOrd;
      string photoTime = TimeToStr(TimeLocal());
      string photoName = Symbol() + " " + 
                         StringSubstr(photoTime,  0, 4) + StringSubstr(photoTime,  5, 2) + StringSubstr(photoTime, 8, 2) + " " +
                         StringSubstr(photoTime, 11, 2) + StringSubstr(photoTime, 14, 2) + 
                         " M" + Period();
      print   (photoName);

      if (WindowScreenShot(photoName + ".gif", 800, 600)) print(photoName + " OK!", true);
      else                                                print(photoName + " ERROR " + GetLastError(), true);
   }

   //----
   return(0);
}

//+------------------------------------------------------------------+
//| general management                                               |
//+------------------------------------------------------------------+

int print(string text, bool flashing = false)
{
   if (ObjectFind( "Check text") < 0)
   {
      ObjectCreate("Check text", OBJ_LABEL,         0, 0, 0);
      ObjectSet(   "Check text", OBJPROP_COLOR,     White);
      ObjectSet(   "Check text", OBJPROP_BACK,      false);
      ObjectSet(   "Check text", OBJPROP_CORNER,    0);
      ObjectSet(   "Check text", OBJPROP_XDISTANCE, 250);
      ObjectSet(   "Check text", OBJPROP_YDISTANCE, 20);
   }
   ObjectSetText(  "Check text", TimeToStr(TimeLocal(), TIME_SECONDS) + " " + text, 10, "Tahoma");

   if      (flashing && ObjectGet("Check text", OBJPROP_COLOR) == White) ObjectSet("Check text", OBJPROP_COLOR, Red);
   else if (flashing && ObjectGet("Check text", OBJPROP_COLOR) == Red)   ObjectSet("Check text", OBJPROP_COLOR, White);
   else                                                                  ObjectSet("Check text", OBJPROP_COLOR, White);

   return(0);
}

//+------------------------------------------------------------------+