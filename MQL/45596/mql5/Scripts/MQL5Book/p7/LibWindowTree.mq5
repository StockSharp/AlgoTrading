//+------------------------------------------------------------------+
//|                                                LibWindowTree.mq5 |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <WinAPI/WinUser.mqh>

//+------------------------------------------------------------------+
//| Obtain window class name and title by handle                     |
//+------------------------------------------------------------------+
void GetWindowData(HANDLE w, string &clazz, string &title)
{
   static ushort receiver[MAX_PATH];
   if(GetWindowTextW(w, receiver, MAX_PATH))
   {
      title = ShortArrayToString(receiver);
   }
   if(GetClassNameW(w, receiver, MAX_PATH))
   {
      clazz = ShortArrayToString(receiver);
   }
}

//+------------------------------------------------------------------+
//| Walk up through parent windows                                   |
//+------------------------------------------------------------------+
HANDLE TraverseUp(HANDLE w)
{
   HANDLE p = 0;
   while(w != 0)
   {
      p = w;
      string clazz, title;
      GetWindowData(w, clazz, title);
      Print("'", clazz, "' '", title, "'");
      w = GetParent(w);
   }
   return p;
}

//+------------------------------------------------------------------+
//| Walk down through child windows                                  |
//+------------------------------------------------------------------+
HANDLE TraverseDown(const HANDLE w, const int level = 0)
{
   HANDLE child = FindWindowExW(w, NULL, NULL, NULL); // get 1-st child window (if any)
   while(child)                                       // keep going while children exist
   {
      string clazz, title;
      GetWindowData(child, clazz, title);
      Print(StringFormat("%*s", level * 2, ""), "'", clazz, "' '", title, "'");
      TraverseDown(child, level + 1);
      child = FindWindowExW(w, child, NULL, NULL);    // get next child window
   }
   return child;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // to get the main window could call
   //    FindWindowW("MetaQuotes::MetaTrader::5.00", NULL); 
   // or traverse up:
   HANDLE h = TraverseUp(ChartGetInteger(0, CHART_WINDOW_HANDLE));
   Print("Main window handle: ", h);
   TraverseDown(h, 1);
}
//+------------------------------------------------------------------+
/*

'AfxFrameOrView140su' ''
'Afx:000000013F110000:b:0000000000010003:0000000000000006:00000000000306BA' 'EURUSD,H1'
'MDIClient' ''
'MetaQuotes::MetaTrader::5.00' '12345678 - MetaQuotes-Demo: Demo Account - Hedge - MetaQuotes Software Corp. - [EURUSD,H1]'
Main window handle: 263576
  'msctls_statusbar32' 'For Help, press F1'
  'AfxControlBar140su' 'Standard'
    'ToolbarWindow32' 'Timeframes'
    'ToolbarWindow32' 'Line Studies'
    'ToolbarWindow32' 'Standard'
  'AfxControlBar140su' 'Toolbox'
    'Afx:000000013F110000:b:0000000000010003:0000000000000000:0000000000000000' 'Toolbox'
      'AfxWnd140su' ''
        'ToolbarWindow32' ''
...
  'MDIClient' ''
    'Afx:000000013F110000:b:0000000000010003:0000000000000006:00000000000306BA' 'EURUSD,H1'
      'AfxFrameOrView140su' ''
        'Edit' '0.00'
    'Afx:000000013F110000:b:0000000000010003:0000000000000006:00000000000306BA' 'XAUUSD,Daily'
      'AfxFrameOrView140su' ''
        'Edit' '0.00'
    'Afx:000000013F110000:b:0000000000010003:0000000000000006:00000000000306BA' 'EURUSD,M15'
      'AfxFrameOrView140su' ''
        'Edit' '0.00'

*/
//+------------------------------------------------------------------+
