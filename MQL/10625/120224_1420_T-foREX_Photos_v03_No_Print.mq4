//+------------------------------------------------------------------+
//|                        120224 1420 T-foREX Photos v02 Orders.mq4 |
//|                                                          T-foREX |
//|                                               http://jobaweb.net |
//+------------------------------------------------------------------+

#property copyright "T-foREX"
#property link      "http://jobaweb.net"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

int init()
{
   //----

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
   for (pos = 0; pos < OrdersTotal(); pos++) if (OrderSelect(pos, SELECT_BY_POS) && OrderSymbol() == Symbol() && OrderType() < 2) nOrd += 1;

   if (nOrd != nOld)
   {
      nOld = nOrd;
      string photoTime = TimeToStr(TimeLocal());
      string photoName = Symbol() + " " + 
                         StringSubstr(photoTime,  0, 4) + StringSubstr(photoTime,  5, 2) + StringSubstr(photoTime, 8, 2) + " " +
                         StringSubstr(photoTime, 11, 2) + StringSubstr(photoTime, 14, 2) + 
                         " M" + Period();

      WindowScreenShot(photoName + ".gif", 800, 600);
   }

   //----
   return(0);
}

//+------------------------------------------------------------------+