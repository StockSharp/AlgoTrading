//+------------------------------------------------------------------+
//|                                                      Objects.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

void CreateObject(int chartID, int subWnd, string name, ENUM_OBJECT type, string text, bool selectable = false)
{
   int wnd = ObjectFind(chartID, name);
   
   if (wnd >= 0 && wnd != subWnd)
   {
      ObjectDelete(chartID, name);
      ObjectCreate(chartID, name, type, subWnd, 0, 0);
   }
   else if (wnd < 0)
   {
      ObjectCreate(chartID, name, type, subWnd, 0, 0);
   }
   
   ObjectSetInteger(chartID, name, OBJPROP_SELECTABLE, selectable);
   ObjectSetString(chartID, name, OBJPROP_TEXT, text);
}

void MoveObject(int chartID, string name, int x, int y, int corner)
{
   ObjectSetInteger(chartID, name, OBJPROP_XDISTANCE, x);
   ObjectSetInteger(chartID, name, OBJPROP_YDISTANCE, y);
   ObjectSetInteger(chartID, name, OBJPROP_CORNER, corner);
}