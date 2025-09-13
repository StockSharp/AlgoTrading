//+------------------------------------------------------------------+
//|                                                 InputResizer.mq4 |
//|                                       Copyright © 2011, MaryJane |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, MaryJane"
#property link      "http://codebase.mql4.com/7657"

#property indicator_chart_window

#import "user32.dll"
int GetForegroundWindow();
int FindWindowExW(int,int,string,string);
int GetWindowRect(int,int &arr[]);
int GetClientRect(int,int &arr[]);
int SetWindowLongW(int,int,int);
int GetWindowLongW(int,int);
int GetSystemMenu(int,bool);
int InsertMenuW(int,int,int,int,string);
int SetWindowPos(int,int,int,int,int,int,int);
int GetParent(int);
int GetWindowTextW(int,short &arr[],int);
int ShowWindow(int,int);
int GetClassNameW(int,short &arr[],int);
int GetDlgCtrlID(int);
int GetDlgItem(int,int);
bool IsZoomed(int);
bool InvalidateRect(int,int &arr[],bool);
#import

#define GWL_STYLE         -16 
#define WS_SIZEBOX        0x00040000
#define WS_MINIMIZEBOX    0x00020000
#define WS_MAXIMIZEBOX    0x00010000
#define MF_STRING         0x00000000
#define MF_BYPOSITION     0x00000400
#define SW_MAXIMIZE       0x0003
#define SC_SIZE           0xF000
#define SC_MAXIMIZE       0xF030
#define SC_MINIMIZE       0xF020
#define SC_RESTORE        0xF120
#define SWP_NOSIZE        0x0001
#define SWP_NOMOVE        0x0002
#define SWP_NOZORDER      0x0004
#define SWP_NOACTIVATE    0x0010

#define CID_IDBOX         0x0000
#define CID_OK            0x0001
#define CID_CANCEL        0x0002
#define CID_RESET         0x3021
#define CID_TAB           0x3020
#define CID_LIST_ST       0x054D
#define CID_LOAD_ST       0x0FAB
#define CID_SAVE_ST       0x0FAC
#define CID_LIST_EX       0x0567
#define CID_LOAD_EX       0x0FAD
#define CID_SAVE_EX       0x0FAE

extern bool   RememberSize       = true;
extern bool   Individual         = true;
extern bool   InitMaximized      = false;
extern bool   InitCustom         = true;
extern int    initX              = 200;
extern int    initY              = 200;
extern int    initWidth          = 350;
extern int    initHeight         = 450;
extern int    SleepTime          = 300;

//+--------------------------------------------------------------------------------------+
void init(){if(!IsDllsAllowed()) Alert("InputResizerEA: DLLs not allowed!");}
//+--------------------------------------------------------------------------------------+
void deinit(){}
//+--------------------------------------------------------------------------------------+
void start()
  {
   static int x,y,xc,yc,oxd,oyd,cxd,rxd,tx,ty,px,py;
   static int gx,gy,lx,ly,lxd,lyd,sxd,syd;
   static int r[4],s[4];
   static int r0,r1,r2,r3,m,pWnd;
   static int ok,cancel,reset,tab,list,load,save,idbox,pidbox;
   static string gvName,gvAdd;
   static bool resizable;
   short cNameBuffer[];

// We determine the active (topmost) window
   int fWnd=GetForegroundWindow();
   ArrayResize(cNameBuffer,30);
   GetClassNameW(GetParent(fWnd),cNameBuffer,30);
   string cName=ShortArrayToString(cNameBuffer);
//Print("Yes.");
//Print(cName);
   if(cName=="MetaQuotes::MetaTrader::4.00" || cName=="#32770")
     { // Parent = MT4 or another dialog box
      ArrayResize(cNameBuffer,7);
      GetClassNameW(fWnd,cNameBuffer,7);
      cName=ShortArrayToString(cNameBuffer);
      if(cName=="#32770")
        {// Yes, it is a dialog box, so let's try to get further handles..
         idbox=GetDlgItem(fWnd,CID_IDBOX);
         ok=GetDlgItem(fWnd,CID_OK);
         cancel= GetDlgItem(fWnd,CID_CANCEL);
         reset = GetDlgItem(fWnd,CID_RESET);
         tab=GetDlgItem(fWnd,CID_TAB);
         if(idbox && ok && cancel && reset && tab)
           { // Yes, all those elements are present so let's explore the idbox
            list=FindWindowExW(idbox,0,"SysListView32","List1");
            switch(GetDlgCtrlID(list))
              {
               case CID_LIST_ST: {load = GetDlgItem(idbox, CID_LOAD_ST); save = GetDlgItem(idbox, CID_SAVE_ST); break;}
               case CID_LIST_EX: {load = GetDlgItem(idbox, CID_LOAD_EX); save = GetDlgItem(idbox, CID_SAVE_EX); break;}
               default: {list=0; load=0; save=0;}
              }
            if(list!=0) pWnd=fWnd; // a new window has been found.
           }
        }
     }

   if(pWnd==fWnd) // so if we caught a window...
     {
      if(!resizable) // it's not resizable until we do it:
        {
         // This adds sizable border, minimize and maximize box.Context help question mark in
         // window caption disappears - the styles can't coexist.
         SetWindowLongW(pWnd,GWL_STYLE,GetWindowLongW(pWnd,GWL_STYLE)|WS_SIZEBOX|WS_MINIMIZEBOX|WS_MAXIMIZEBOX);
         // The above alone doesn't do, the window has to have the items in right-click-on
         // titlebar menu:
         int menu=GetSystemMenu(pWnd,false);
         InsertMenuW(menu,1,MF_BYPOSITION|MF_STRING,SC_SIZE,"Size");
         InsertMenuW(menu,1,MF_BYPOSITION|MF_STRING,SC_MAXIMIZE,"Maximize");
         InsertMenuW(menu,1,MF_BYPOSITION|MF_STRING,SC_MINIMIZE,"Minimize");
         InsertMenuW(menu,1,MF_BYPOSITION|MF_STRING,SC_RESTORE,"Restore");
         // Now our window is sizable with mouse, but since the WM_XXX messages are not parsed
         // by MT4 (everything inside stays at its place, we have to take care of it.
         // Now we take all necessary coordinates of inside controls realtive to parent
         // window as long as it's in original size (hence so many int variables)
         GetWindowRect(pWnd,r);
         GetWindowRect(ok,s); oxd=r[2]-s[0]; oyd=r[3]-s[1];
         GetWindowRect(cancel,s); cxd = r[2] - s[0];
         GetWindowRect(reset, s); rxd = r[2] - s[0];
         GetWindowRect(tab,s); tx=s[2]-s[0]; ty=s[3]-s[1];
         GetWindowRect(idbox,s); gx = s[2] - s[0]; gy = s[3] - s[1];
         GetWindowRect(list, s); lx = s[2] - s[0]; ly = s[3] - s[1];
         GetWindowRect(load, s); lxd = r[2] - s[0]; lyd = r[3] - s[1];
         GetWindowRect(save, s); sxd = r[2] - s[0]; syd = r[3] - s[1];
         GetClientRect(pWnd,s); xc=s[2]; yc=s[3];
         // ..before we leave this stage, let's check if we have some memories of this guy
         if(RememberSize)
           {
            gvName="iRes_";
            if(Individual) // one for all ar one for one?!
              {
               // ridiculously long empty string should accomodate looooooooooooooooooooongest robot names ;-)
               ArrayResize(cNameBuffer, 50);
               GetWindowTextW(pWnd,cNameBuffer,50); // retrieve EA/indicator name
               gvAdd = ShortArrayToString(cNameBuffer);
               gvName=gvName+StringTrimRight(gvAdd)+"_"; // add name to global variable name
              }
            // let's check if we know that guy
            if(GlobalVariableCheck(gvName+"r0"))
              {
               // if r0 there, other three musketeers will be there too so let's get 'em all:
               r0 = GlobalVariableGet(gvName + "r0");
               r1 = GlobalVariableGet(gvName + "r1");
               r2 = GlobalVariableGet(gvName + "r2");
               r3 = GlobalVariableGet(gvName + "r3");
               m=GlobalVariableGet(gvName+"m");
               // now let's put the guy into former shape:
               if(m==1) ShowWindow(pWnd,SW_MAXIMIZE); // if maxed previously, just re-max
               else SetWindowPos(pWnd,0,r0,r1,r2-r0,r3-r1,SWP_NOZORDER|SWP_NOACTIVATE);
              }
            else
              { // normally we store, but this is firstttime so apply defaults, if any:
               if(InitMaximized) ShowWindow(pWnd,SW_MAXIMIZE);
               else if(InitCustom) SetWindowPos(pWnd,0,initX,initY,initX+initWidth,initY+initHeight,SWP_NOZORDER|SWP_NOACTIVATE);
              }
           }
         else // ..if we don't store anything, let's set defaults, if any:
           {
            if(InitMaximized) ShowWindow(pWnd,SW_MAXIMIZE);
            else if(InitCustom) SetWindowPos(pWnd,0,initX,initY,initX+initWidth,initY+initHeight,SWP_NOZORDER|SWP_NOACTIVATE);
           }

         // This causes the box to redraw, otherwise we get artefacts
         InvalidateRect(pWnd,s,true);
         px=0; py=0; // reset comparators
                     // ..and that's for us to know this round was done:
         resizable=true;
        }
      // Now we get position and dimensions of the window on every pass
      GetWindowRect(pWnd,r);
      GetClientRect(pWnd,s); x=s[2]; y=s[3];

      // We're lurkin' for any change:
      if(x!=px || y!=py || r0!=r[0] || r1!=r[1] || idbox!=pidbox)
        {
         if(x!=px || y!=py || idbox!=pidbox)
           {
            // and if there's a change in dimension, we move buttons and resize the child box and the
            // list form inside:
            if(ok!=0) SetWindowPos(ok,0,x-oxd+4,y-oyd+4,0,0,SWP_NOZORDER|SWP_NOSIZE|SWP_NOACTIVATE);
            if(cancel!=0) SetWindowPos(cancel,0,x-cxd+4,y-oyd+4,0,0,SWP_NOZORDER|SWP_NOSIZE|SWP_NOACTIVATE);
            if(reset !=0) SetWindowPos(reset,0,x-rxd+4,y-oyd+4,0,0,SWP_NOZORDER|SWP_NOSIZE|SWP_NOACTIVATE);
            if(tab!=0) SetWindowPos(tab,0,0,0,tx+x-xc,ty+y-yc,SWP_NOZORDER|SWP_NOMOVE|SWP_NOACTIVATE);
            if(idbox!=0) SetWindowPos(idbox,0,0,0,gx+x-xc,gy+y-yc,SWP_NOZORDER|SWP_NOMOVE|SWP_NOACTIVATE);
            if(list != 0) SetWindowPos(list, 0, 0, 0, lx + x - xc, ly + y - yc, SWP_NOZORDER | SWP_NOMOVE | SWP_NOACTIVATE);
            if(load != 0) SetWindowPos(load, 0, x-lxd-7, y-lyd-25, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
            if(save != 0) SetWindowPos(save, 0, x-sxd-7, y-syd-25, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
            // next time no change unless really changed:
            px=x; py=y;
           }
         if(RememberSize)
           {
            // store last known resized position and size on ANY change
            GetWindowRect(pWnd,r);
            GlobalVariableSet(gvName+"r0",r[0]);
            GlobalVariableSet(gvName+"r1",r[1]);
            GlobalVariableSet(gvName+"r2",r[2]);
            GlobalVariableSet(gvName+"r3",r[3]);
            GlobalVariableSet(gvName+"m",IsZoomed(pWnd));
           }
         // This causes the box to redraw, otherwise we get artefacts
         InvalidateRect(pWnd,s,true);
         // next time no change unless really changed:
         px=x; py=y; r0=r[0]; r1=r[1]; pidbox=idbox;
        }
     }

   else resizable = false; // if there's no window (has been closed),
                           // next time we'll have to resize a new one
}
//+------------------------------------------------------------------+
