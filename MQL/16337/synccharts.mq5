//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
#property strict

input bool bSyncVLine=true;

//------------------------------------------------------------------	struct tagObj
struct tagObj { string name; long dt,clr,wth,style,sel; };

//------------------------------------------------------------------	OnInit
int OnInit() { EventSetMillisecondTimer(200); return INIT_SUCCEEDED; }
//------------------------------------------------------------------	OnDeinit
void OnDeinit(const int reason) { EventKillTimer(); }
//------------------------------------------------------------------	OnTimer
void OnTimer()
  {
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)) return;

   long cid=ChartID();
   ChartSetInteger(cid,CHART_AUTOSCROLL,false);
   ChartSetInteger(cid,CHART_SHIFT,false);
   long fb=ChartGetInteger(cid, CHART_FIRST_VISIBLE_BAR);
   long wb=ChartGetInteger(cid, CHART_WIDTH_IN_BARS);
   ENUM_TIMEFRAMES tf=ChartPeriod(cid);
   long scale=ChartGetInteger(cid,CHART_SCALE);
   long mode=ChartGetInteger(cid,CHART_MODE);
   long id=ChartFirst();

   tagObj vr[]; int vn=0;
   if(bSyncVLine)
     {
      int n=ObjectsTotal(cid,0,OBJ_VLINE);
      for(int i=0; i<n;++i) { ArrayResize(vr,vn+1); GetObj(cid,ObjectName(cid,i,0,OBJ_VLINE),vr[vn]); vn++; }
     }

   do
     {
      if(id!=cid)
        {
         ChartSetInteger(id,CHART_SCALE,scale);
         ChartSetSymbolPeriod(id,ChartSymbol(id),tf);
         if(ChartGetInteger(id,CHART_FIRST_VISIBLE_BAR)!=fb) ChartNavigate(id,CHART_END,-int(fmax(fb-wb+2,0)));
         ChartSetInteger(id,CHART_AUTOSCROLL,false);
         ChartSetInteger(id,CHART_SHIFT,false);
         ChartSetInteger(id,CHART_MODE,mode);

         if(bSyncVLine) { ObjectsDeleteAll(id,0,OBJ_VLINE); for(int i=0; i<vn;++i) NewObj(id,vr[i]); }
         ChartRedraw(id);
        }
      id=ChartNext(id);
     }
   while(id>0);
  }
//------------------------------------------------------------------	GetObj
void GetObj(long cid,string name,tagObj &vl)
  {
   vl.name=name;
   vl.clr=ObjectGetInteger(cid,name,OBJPROP_COLOR);
   vl.dt=(datetime)ObjectGetInteger(cid,name,OBJPROP_TIME,0);
   vl.wth=ObjectGetInteger(cid,name,OBJPROP_WIDTH);
   vl.style=ObjectGetInteger(cid,name,OBJPROP_STYLE);
   vl.sel=ObjectGetInteger(cid,name,OBJPROP_SELECTED);
  }
//------------------------------------------------------------------	NewObj
void NewObj(long cid,tagObj &vl)
  {
   ObjectCreate(cid,vl.name,OBJ_VLINE,0,vl.dt,0);
   ObjectSetInteger(cid, vl.name, OBJPROP_COLOR, vl.clr);
   ObjectSetInteger(cid, vl.name, OBJPROP_WIDTH, vl.wth);
   ObjectSetInteger(cid, vl.name, OBJPROP_STYLE, vl.style);
   ObjectSetInteger(cid, vl.name, OBJPROP_SELECTED, vl.sel);
  }
//+------------------------------------------------------------------+
