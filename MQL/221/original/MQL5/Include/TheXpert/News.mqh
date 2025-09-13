//+------------------------------------------------------------------+
//|                                                         News.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

string GetNearestNews()
{
   int objects = ObjectsTotal(0, 0, OBJ_EVENT);

   datetime now = TimeCurrent();
   
   string name;
   string nearest;
   datetime nearestTime = 0;
   
   for (int i = 0; i < objects; i++)
   {
      name = ObjectName(0, i, 0, OBJ_EVENT);
      
      datetime current = datetime(ObjectGetInteger(0, name, OBJPROP_TIME, 0));
      if (current > now)
      {
         if (current < nearestTime || nearestTime == 0)
         {
            nearestTime = current;
            nearest = name;
         }
      }
   }
   
   return nearest;
}