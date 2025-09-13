//+------------------------------------------------------------------+
//|                                                          Monitor |
//|                              Copyright 2006-2013, FINEXWARE GmbH |
//|     programming & support - Alexey Sergeev (profy.mql@gmail.com) |
//+------------------------------------------------------------------+
#property copyright "Copyright 2006-2012, FINEXWARE GmbH"
#property link      "profy.mql@gmail.com"

// Flag values
#define modeOpen						0 // open mode
#define modeCreate					1 // create mode

#define ERROR_FILE_NOT_FOUND		2

// types 
#define HANDLE32	int
#define HANDLE64	long

// import
#import "MemMap32.dll"
HANDLE32 MemOpen(string path,int size,int mode,int &err);                // open/create memory-mapped file and returns handle
void MemClose(HANDLE32 hmem);                                            // close memory mapped file
HANDLE32 MemGrows(HANDLE32 hmem, string path,int newsize,int &err);      // increase size of memory-mapped file
int MemWrite(HANDLE32 hmem,int &v[], int pos, int sz, int &err);         // write v vector (sz bytes) to memory-mapped file starting from position pos
int MemRead(HANDLE32 hmem, int &v[], int pos, int sz, int &err);         // read v vector (sz bytes) from memory-mapped file starting from position pos
int MemWriteStr(HANDLE32 hmem, uchar &str[], int pos, int sz, int &err); // write string
int MemReadStr(HANDLE32 hmem, uchar &str[], int pos, int &sz, int &err); // read string 
#import "MemMap64.dll"
HANDLE64 MemOpen(string path,int size,int mode,int &err);                // open/create memory-mapped file and returns handle
void MemClose(HANDLE64 hmem);                                            // close memory mapped file
HANDLE64 MemGrows(HANDLE64 hmem, string path,int newsize,int &err);      // increase size of memory-mapped file
int MemWrite(HANDLE64 hmem,int &v[], int pos, int sz, int &err);         // write v vector (sz bytes) to memory-mapped file starting from position pos
int MemRead(HANDLE64 hmem, int &v[], int pos, int sz, int &err);         // read v vector (sz bytes) from memory-mapped file starting from position pos
int MemWriteStr(HANDLE64 hmem, uchar &str[], int pos, int sz, int &err); // write string
int MemReadStr(HANDLE64 hmem, uchar &str[], int pos, int &sz, int &err); // read string
#import

// redefine function calls (for support 32 and 64 bit)
HANDLE64 MemOpen(string path,int size,int mode, int &error) { if (_IsX64) return(MemMap64::MemOpen(path, size, mode, error)); return(MemMap32::MemOpen(path, size, mode, error)); } // open/create memory-mapped file and returns handle
void MemClose(HANDLE64 h) { if (_IsX64) MemMap64::MemClose(h); else MemMap32::MemClose((HANDLE32)h); } // close memory mapped file
HANDLE64 MemGrows(HANDLE64 h, string path,int newsize,int &error) { if (_IsX64) return(MemMap64::MemGrows(h, path, newsize, error)); return(MemMap32::MemGrows((HANDLE32)h, path, newsize, error));  } // increase size of memory-mapped file
int MemWrite(HANDLE64 h,int &v[], int pos, int sz, int &error) { if (_IsX64) return(MemMap64::MemWrite(h, v, pos, sz, error)); return(MemMap32::MemWrite((HANDLE32)h, v, pos, sz, error));  } // write v vector (sz bytes) to memory-mapped file starting from position pos
int MemRead(HANDLE64 h, int &v[], int pos, int sz, int &error) { if (_IsX64) return(MemMap64::MemRead(h, v, pos, sz, error)); return(MemMap32::MemRead((HANDLE32)h, v, pos, sz, error));  } // read v vector (sz bytes) from memory-mapped file starting from position pos
int MemWriteStr(HANDLE64 h, uchar &str[], int pos, int sz, int &error) { if (_IsX64) return(MemMap64::MemWriteStr(h, str, pos, sz, error)); return(MemMap32::MemWriteStr((HANDLE32)h, str, pos, sz, error)); } // write string
int MemReadStr(HANDLE64 h, uchar &str[], int pos, int &sz, int &error) { if (_IsX64) return(MemMap64::MemReadStr(h, str, pos, sz, error)); return(MemMap32::MemReadStr((HANDLE32)h, str, pos, sz, error)); } // read string 

input int MaxLate=5000; // miliseconds delay

// memory-mapped file
#define HEAD		8             // header size
#define iSize		4*4           // vector size (4*int(4))
#define sSize		4+30          // string size
#define Size		(iSize+sSize) // size
#define kprice		0.000001

HANDLE64 hmem;
int err, price[][4], adr=0; string term[]; // arrays used for data exchange
string file; // name of memory-mapped file

string sID;
string g_inf;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   file="Local\\Monitor_"+StringSubstr(Symbol(),0,(int)MathMin(6,StringLen(Symbol()))); // name of memory-mapped file
   sID="memmapobj."; // prefix

   int head[2];
   hmem=::MemOpen(file,-1,modeOpen,err); // open file
   if(hmem>0) // if opened successfully
     {
      Print("open OK h="+string(hmem));
      int r=::MemRead(hmem, head,0,HEAD,err);
      Print("read head uses="+string(head[0])+"  adr="+string(head[1])+"  | r="+string(r)+"  err="+string(err));
      head[0]++;
      head[1]++;
      adr=head[1]; // number of connected apps and folder of the current client terminal
      hmem=::MemGrows(hmem,file,HEAD+(adr+1)*Size,err);
      Print("grows to "+string(HEAD+(adr+1)*Size)+"  | h="+string(hmem)+"  err="+string(err));
      int w=::MemWrite(hmem,head,0,HEAD,err);
      Print("write head w="+string(w)+"  err="+string(err));
     }
   else
   if(err==ERROR_FILE_NOT_FOUND) // if file not found, create it
     {
      Print("-err("+string(err)+") memfile not found. Create it...'"+file+"'");
      head[0]=1;
      head[1]=0;
      adr=head[1];
      hmem=::MemOpen(file,HEAD+(adr+1)*Size,modeCreate,err);
      Print("create size="+string(HEAD+(adr+1)*Size)+"  h="+string(hmem)+"  err="+string(err));
      if(hmem<=0 || err!=0)
        {
         Print("-err("+string(err)+") create memfile  h="+string(hmem)); return(0);
        }
      else Print("create OK h="+string(hmem));
      int w=::MemWrite(hmem,head,0,HEAD,err); Print("write head w="+string(w)+"  err="+string(err));
      if(err!=0)
        {
         Print("-err("+string(err)+") write memfile  h="+string(hmem));
         return(0);
        }
     }
   else
     {
      Print("-unknow err("+string(err)+")  h="+string(hmem));
      return(0);
     }
   Print("OK ("+string(err)+") h="+string(hmem));
   OnTick();
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(hmem>0) // if handle>0 (file opened)
     {
      int head[2];
      int r=::MemRead(hmem,head,0,HEAD,err);
      Print("read head uses="+string(head[0])+"  adr="+string(head[1])+"  | r="+string(r)+"  err="+string(err));
      head[0]--;
      int w=::MemWrite(hmem,head,0,HEAD,err);
      Print("write head  w="+string(w)+"  err="+string(err));
      if(head[0]>0) Print("reduce use of memfile "+string(hmem)+"  to "+string(head[0]));
      else
        {
         Print("close memfile "+string(hmem));
         ::MemClose(hmem);
        }
     }
   ObjectsDeleteAll2(0,-1,sID); // delete all objects
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Print("started");
   while(true)
     {
      g_inf="";
      INF("");
      INF("File to "+file,true);
      int w=WritePrice();
      INF("write="+string(w)+"  err="+string(err));
      int c,n=ReadPrice(c);
      if(n==-1) INF("-err read Head");
      if(n==-2) INF("-err read Data");
      int t=(int)GetTickCount();
      INF(TimeToString(TimeLocal(),TIME_MINUTES|TIME_SECONDS)+" || "+string(t));
      INF("-----------------------------------------------------");
      INF("Terminals="+string(n)+"  active="+string(c),true); INF("");
      for(int i=0; i<c; i++)
        {
         int tm=price[i][0];
         int acc=price[i][1];
         double bid=price[i][2]*kprice;
         int mt=price[i][3];
         INF(string(t-tm)+" ("+string(tm)+") | "+string(acc)+" | "+DTS(bid,6)+" | "+term[i]+" | MT "+string(mt));
         SetHLine(sID+string(i),bid,Lime,1,STYLE_DOT,""); // set lines
        }
      for(int i=c; i<n; i++) ObjectDelete(0,sID+string(i));
      Comment(g_inf);
      if(IsStopped() || !TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
        {
         Print("stopped");
         break;
        }
      Sleep(500);
     }
  }
//+------------------------------------------------------------------+
//| WritePrice                                                       |
//+------------------------------------------------------------------+
int WritePrice()
  {
   if(hmem<=0) return(0);
   int data[4];
   double Bid=SymbolInfoDouble(Symbol(),SYMBOL_BID);
   uchar name[]; StringToCharArray(TerminalInfoString(TERMINAL_COMPANY), name);
   data[0]=(int)GetTickCount();
   data[1]=(int)AccountInfoInteger(ACCOUNT_LOGIN);
   data[2]=int(Bid/kprice);
   data[3]=5;/*MT5*/
   int w0=::MemWrite(hmem,data,HEAD+adr*Size,iSize,err);
   int w1=::MemWriteStr(hmem,name,HEAD+adr*Size+iSize, ArraySize(name)-1, err);
   return(w0+w1);
  }
//+------------------------------------------------------------------+
//| ReadPrice                                                        |
//+------------------------------------------------------------------+
int ReadPrice(int &c)
  {
   if(hmem<=0) return(0);
   int data[4],head[2];
   int r=::MemRead(hmem,head,0,HEAD,err);
   INF("read head  uses="+string(head[0])+"  adr="+string(head[1])+"  | r="+string(r)+"  err="+string(err));
   if(r<HEAD || err!=0) return(-1);
   int n=head[1]+1;
   c=0; ArrayResize(price,c); ArrayResize(term,c); // number of terminals
   int rs=0, t=(int)GetTickCount();
   for(int i=0; i<n; i++)
     {
      ArrayInitialize(data,0); uchar name[100];
      r=::MemRead(hmem,data,HEAD+i*Size,iSize,err); if(r<iSize || err!=0) return(-2);
      r=::MemReadStr(hmem,name,HEAD+i*Size+iSize,rs,err); if(r<0 || err!=0) return(-3);
      if(MathAbs(t-data[0])>MaxLate) continue;
      ArrayResize(price,c+1); ArrayResize(term,c+1);
      price[c][0]=data[0];
      price[c][1]=data[1];
      price[c][2]=data[2];
      price[c][3]=data[3];
      term[c]=CharArrayToString(name);
      c++;
     }
   return(n);
  }
//+------------------------------------------------------------------+
//| SetHLine                                                         |
//+------------------------------------------------------------------+
void SetHLine(string name,double pr,color clr,int width,int style,string st)
  {
   ObjectCreate(0,name,OBJ_HLINE,0,0,0);
   ObjectSetDouble(0,name,OBJPROP_PRICE,0,pr);
   ObjectSetInteger(0,name,OBJPROP_WIDTH,width);
   ObjectSetInteger(0,name,OBJPROP_COLOR,clr);
   ObjectSetString(0,name,OBJPROP_TEXT,st);
   ObjectSetInteger(0,name,OBJPROP_STYLE,style);
  }
//+------------------------------------------------------------------+
//| INF                                                              |
//+------------------------------------------------------------------+
void INF(string st,bool ini=false)
  {
   if(ini) g_inf=g_inf+"\n        "+st; else g_inf=g_inf+"\n            "+st;
  }
//+------------------------------------------------------------------+
//| DTS                                                              |
//+------------------------------------------------------------------+
string DTS(double d,int n=-1)
  {
   if(d==EMPTY_VALUE) return("<>");
   if(n<0) return(DoubleToString(d,_Digits));
   else return(DoubleToString(d,n));
  }
//+------------------------------------------------------------------+
//| ObjectsDeleteAll2                                                |
//+------------------------------------------------------------------+
void ObjectsDeleteAll2(int wnd=-1,int type=-1,string pref="")
  {
   string names[];
   int n=ObjectsTotal(0);
   ArrayResize(names,n);
   for(int i=0; i<n; i++) names[i]=ObjectName(0, i);
   for(int i=0; i<n; i++)
     {
      if(wnd>=0) if(ObjectFind(0,names[i])!=wnd) continue;
      if(type>=0) if(ObjectGetInteger(0, names[i], OBJPROP_TYPE)!=type) continue;
      if(pref!="") if(StringSubstr(names[i], 0, StringLen(pref))!=pref) continue;
      ObjectDelete(0,names[i]);
     }
  }
//+------------------------------------------------------------------+
