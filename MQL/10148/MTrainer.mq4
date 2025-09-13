//+------------------------------------------------------------------+
//|                                                    MTrainer.mq4   
//|                                                    V 1.0        
//|                                                    Jonus                                          
//+------------------------------------------------------------------+
// manually set trades in MT Strategy Tester Visial Mode
//+------------------------------------------------------------------+

#property copyright "Jonus"
#property link      "www.KissMyEA.com"
#include <stdlib.mqh>
#include <stderror.mqh>

//------------------------------------------
extern double   BSRate   = 5;                //order size %
extern double   PCRate   = 0;                //take partial close 
extern int      SlowDown = 100;              //ST 32 is too fast!
extern color    OScolor  = DimGray;          //order send color
extern color    OPcolor  = DodgerBlue;       //order pending color
extern color    OOcolor  = Lime;             //order open color
extern color    TPcolor  = Gold;             //Take Profit color  
extern color    SLcolor  = Tomato;           //Stop Loss color
extern color    PCcolor  = White;            //take partial color
extern bool     AlertOn  = true;             //show alert errors?
extern bool     ExtPips  = true;             //broker extended pips?
//-------------------------------------------
static int      EAnum = 660000;
static string   EAname="MTrainer 1.0";
static int      OP_PEND = 777;
static int      OP_OPEN = 888;
//-------------------------------------------
int             BarCount;
int             MagNum=0; 
double          MyPoint;
int             MyDigit;
datetime        TimeX; 
string          TimeD;
string          TimeM;
double          OrderLevel;
double          Spread;
string          OTstatus;
datetime        CheckDateTime;
int             res,error;
bool            OpenOrder=false;
bool            PCused=false;
string          BS="BS";
string          TP="TP";
string          SL="SL";
string          PC="PC";
double          BSlev;
double          TPlev;
double          SLlev;

//-------------------------------------------

int init()
{  MagNum=EAnum; 
   if (MagNum==0) return(-1); 
   if (ExtPips==True) 
      {MyPoint= (Point*10); MyDigit=Digits-1;}
   else
      {MyPoint=Point; MyDigit=Digits;}
  
   ShowParameters();                                             //do this before we recalc them
   ShowInfo("Drop horizontal line to initiate pending order");   //show info msg    
   ShowStatus("...");                                            //show status line
   
   DrawTPline(Ask+(50*MyPoint));
   DrawSLline(Bid-(50*MyPoint));
  
   BSRate=BSRate/100;
   PCRate=PCRate/100;

   OpenOrder=false; 
   
   Print("Init +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
   Print("Init "+EAname);
   Print("Init +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
   
   return(0);
}

int deinit()  
{  Print("DeInit +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
   Print("DeInit "+EAname);
   Print("DeInit +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
   CancelPending(OP_PEND);
   CleanUp();
   return(0);  
}

int start()
  {int Ticket, Pending, OT;
   bool   NoBS=false;
   double CurBSlevel=0;
   double CurSLlevel=0;
   double CurTPlevel=0;
   double CurPClevel=0;

   for (int i=0; i<(SlowDown*100); i++) {}
    
   if (ObjectFind(BS)>=0) 
      {CurBSlevel=NormalizeDouble(ObjectGet(BS,OBJPROP_PRICE1),MyDigit);}
   else
      {NoBS=true;}
         
   if (ObjectFind(TP)>=0)        
      {CurTPlevel=NormalizeDouble(ObjectGet(TP,OBJPROP_PRICE1),MyDigit);}
   else
      {DrawTPline(Ask+(50*MyPoint));}
      
   if (ObjectFind(SL)>=0)       
      {CurSLlevel=NormalizeDouble(ObjectGet(SL,OBJPROP_PRICE1),MyDigit);}
   else
      {DrawSLline(Bid-(50*MyPoint));}
   
   if (ObjectFind(PC)>=0)       
      {CurPClevel=NormalizeDouble(ObjectGet(PC,OBJPROP_PRICE1),MyDigit);}
   else
      {if (PCRate>0) {DrawPCline(Bid);}}

   if (CurTPlevel>CurSLlevel) {OT=OP_BUY;  OTstatus="Buy ";}
   else                       {OT=OP_SELL; OTstatus="Sell ";} 

   Pending=CountOrders(OP_PEND);
   Ticket=CountOrders(OP_OPEN);
     
   //OPEN ORDER processing begin -----------------------------------------------------------------+  
   if (Ticket>0)     
      {if (OpenOrder==false)                                            //order just opened 
          {OpenOrder=true; ObjectSet(BS,OBJPROP_COLOR,OOcolor);         //order open color 
           ShowStatus("Open "+OTstatus+CurBSlevel);                     //show status   
           ShowInfo("Order Opened");                                    //show info
          }
          
       if (NoBS==true)                                                  //deleted BS line !
          {if (CloseOpen(Ticket,1)<1)                                   //close order  
              {ShowInfo("Order Closed"); ShowStatus("...");}            //status and info
          }    
              
       if (CurTPlevel!=TPlev || CurSLlevel!=SLlev)                      //modify open order   
          {if (OrderMod(Ticket,CurTPlevel,CurSLlevel)<1)                //modify 
              {TPlev=CurTPlevel; SLlev=CurSLlevel;                      //change SL & TP     
               ShowInfo("Order Modified");}                             //show info
          }
          
       if (PCRate>0 && PCused==false)  
          {if ((OT==OP_BUY && Bid > CurPClevel) || (OT==OP_SELL && Ask < CurPClevel))
              {if (CloseOpen(Ticket,PCRate)<1)                          //close partial  
                  {ShowInfo("Order Partial Closed"); PCused=true;}      //status and info
              }    
           }
           
       return(0);      
      }
   //OPEN ORDER processing end -------------------------------------------------------------------+
                    
   //CLOSED ORDER processing begin ----------------------------------------------------------------+
   if (OpenOrder==true)                                                //if order just closed 
      {OpenOrder=false; ObjectDelete(BS);                              //set flag & delete BSline 
       ShowStatus("..."); ShowInfo("Order Closed"); PCused=false;      //status & info   
       Sleep(2000);                                                    //wait for system to clean up
       CleanUp();                                                      //delete ST garbage
       return(0);
      }                                                            
   //CLOSED ORDER processing end ------------------------------------------------------------------+
      
   //NEW ORDER processing begin -------------------------------------------------------------------+            
   if (Pending==0)                                                      //no orders now?
      {CurBSlevel=FindBSline();                                         //look for BS line
       if (CurBSlevel>0)                                                //have BS line?
          {if (OrderEntry(OT,CurBSlevel,CurTPlevel,CurSLlevel)<1)       //new order sent  
              {BSlev=CurBSlevel; TPlev=CurTPlevel; SLlev=CurSLlevel;    //set currents
               DrawBSline(CurBSlevel);                                  //order pending color is default
               ShowStatus("Pending "+OTstatus+DoubleToStr(CurBSlevel,MyDigit));
               ShowInfo("New Pending");                                 //show info    
               CleanUp();                                               //delete ST garbage   
          }   }   
       return(0);
      }
   //NEW ORDER processing end ---------------------------------------------------------------------+         
       
   //PENDING ORDER processing begin ---------------------------------------------------------------+   
   if (NoBS==true)                                                       //deleted BS line
      {CancelPending(OP_PEND);                                           //cancel pending
       if (CountOrders(OP_PEND)==0)                                      //success cancelled?
          {ShowInfo("Pending Cancelled"); ShowStatus("...");}            //status and info                          
      }
   else   
      {if (CurBSlevel!=BSlev)                                            //cancel and send NEW PRICE
          {CancelPending(OP_PEND);                                       //cancel pending when price changed
           if (CountOrders(OP_PEND)==0)                                  //no orders now?
              {if (OrderEntry(OT,CurBSlevel,CurTPlevel,CurSLlevel)<1)    //new order sent?  
                  {BSlev=CurBSlevel; TPlev=CurTPlevel; SLlev=CurSLlevel; //set currents
                   DrawBSline(CurBSlevel);                               //order pending color is default
                   ShowStatus("Pending "+OTstatus+DoubleToStr(CurBSlevel,MyDigit));
                   ShowInfo("Pending Cancelled - New Pending");          //show info       
          }   }   }
              
      if (CurSLlevel!=SLlev || CurTPlevel!=TPlev)                       //pending order modify 
         {Ticket=CountOrders(OP_PEND);                                  //get ticket 
          if (OrderMod(Ticket,CurTPlevel,CurSLlevel)<1)                 //modify TP/SL
             {TPlev=CurTPlevel; SLlev=CurSLlevel;                       //set currents
              ShowInfo("Pending Modified");                             //show info
         }   } 
     }
   return(0);          
   //PENDING ORDER processing end ------------------------------------------------------------------+  
}

double FindBSline()
{ double          BSlevel=0;
  string          BSname;
  string          OBJname; 
  int             Ototal=ObjectsTotal();
  //if user drops a Hline on the chart and it matches the Order Send Color OScolor
  //   we have a new order 
  
  for(int i=0;i<Ototal;i++)
     {OBJname=ObjectName(i);
      if (ObjectType(OBJname)==OBJ_HLINE && ObjectGet(OBJname,OBJPROP_COLOR)==OScolor) 
         {BSlevel=ObjectGet(OBJname,OBJPROP_PRICE1); BSname=OBJname;} 
     }   
  if (BSname>"") {ObjectDelete(BSname);}     
  BSlevel=NormalizeDouble(BSlevel,MyDigit);
  return(BSlevel);
}
 
void CleanUp()
{string   OBJname; 
 int      Objs=ObjectsTotal(); 
 
 for(int i=0;i<Objs;i++)
     {OBJname=ObjectName(i);
      if (StringFind(OBJname,"#")>=0) //MT arrows and stuff  
         {ObjectDelete(OBJname);}     //delete everyone we find just in case
     }    
 return;    
} 
 
int OrderEntry(int BS, double pc, double tp, double sl)
{ int res=0;
  int Type;
  string Msg="Order Entry: ";
  string OT;
  double Lots;
  double Spread, StopLevel;
  // check all indicators here!!!!
  
  pc=NormalizeDouble(pc,MyDigit);
  tp=NormalizeDouble(tp,MyDigit);
  sl=NormalizeDouble(sl,MyDigit);     
  Spread=MarketInfo(Symbol(),MODE_SPREAD);
  StopLevel=(MarketInfo(Symbol(),MODE_STOPLEVEL)*MyPoint);
  Lots = CalcLotSize();
  RefreshRates();
  
  if (BS==OP_BUY) 
     {if (pc>Ask)   {Type=OP_BUYSTOP; OT="BuyStop";}           
      else          {Type=OP_BUYLIMIT;OT="BuyLimit";}
     }
          
  if (BS==OP_SELL)  
     {if (pc<Bid)   {Type=OP_SELLSTOP; OT="SellStop";}           
      else          {Type=OP_SELLLIMIT;OT="SellLimit";}
     }   
     
  if (OrderSend(Symbol(),Type,Lots,pc,1,sl,tp,EAname,MagNum,0,CLR_NONE)<1)  
     {res=2; Error(Msg+OT+" OP=" + pc+ " TP=" + tp+" SL=" +sl);}                                  
   
  Sleep(2000);   
 

  return(res);                              // successful order entry
}

int OrderMod(int Ticket, double tp, double sl)
{int stat=0;
 tp=NormalizeDouble(tp,MyDigit);
 sl=NormalizeDouble(sl,MyDigit);
 if (OrderSelect(Ticket,SELECT_BY_TICKET,MODE_TRADES)==true)          
    {if (OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,CLR_NONE)<1) 
        {ShowInfo("Order Modify Failed " + DoubleToStr(Ticket,0)); stat=1;}
    }    
 return(stat);  
}    

int CloseOpen(int Ticket, double cprate)                                                      
{  int stat=0;
   double CP;
   string Msg="CloseOpen: ";
   
   if (OrderSelect(Ticket,SELECT_BY_TICKET,MODE_TRADES)==true)
      {if (OrderType()==OP_BUY) {CP=Bid;}
       if (OrderType()==OP_SELL){CP=Ask;}
       if (OrderClose(OrderTicket(),(OrderLots()*cprate),CP,0,CLR_NONE)<1) 
          {Error(Msg + "Order NOT Closed"); stat=1;}
      } 
   return(stat);
}

int CountOrders(int OType)
{int ords=0, Type;
 for(int i=0;i<=OrdersTotal();i++)
     {if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) continue;
      if(OrderMagicNumber()==MagNum) 
        {Type=OrderType();
         if (OType==Type) 
            {ords++;}
         else
         if (OType==OP_PEND && (Type==OP_BUYSTOP || Type==OP_BUYLIMIT || Type==OP_SELLSTOP || Type==OP_SELLLIMIT)) 
            {ords=OrderTicket(); break;}
         else
         if (OType==OP_OPEN && (Type==OP_BUY || Type==OP_SELL))
            {ords=OrderTicket(); break;}
        }    
     }   
 return(ords);
}

void CancelPending(int OType)            // 9 means cancel all order types                                          
{  int cnt, Ticket, Type, TotalTrades=0;
   string Msg="CancelPending: ";
   TotalTrades=OrdersTotal();
   for (cnt=0;cnt<=TotalTrades;cnt++)
       {if (OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES) &&  OrderSymbol()==Symbol() && OrderMagicNumber()==MagNum)
           { Ticket = OrderTicket(); Type = OrderType();
            if (Type==OType || OType==OP_PEND)
                {if (OrderDelete(Ticket)<1) 
                    {Error(Msg + "Order NOT Deleted "); 
                     ShowStatus("Order Cancel Failed!!!");}
                 else 
                    {ShowStatus("Order Cancelled");
                     Print(Msg + "Order Cancelled ");}    
       }   }    }
   return;
}

double CalcLotSize()
{
double BSRateRate      = BSRate                                                          ; 
double LotSize       = 0                                                             ;                                                                 
double MaxLots       = MarketInfo  ( Symbol () , MODE_MAXLOT         )               ; 
double MinLots       = MarketInfo  ( Symbol () , MODE_MINLOT         )               ; 
double LotStep       = MarketInfo  ( Symbol () , MODE_LOTSTEP        )               ; 
double NominalMargin = MarketInfo  ( Symbol () , MODE_MARGINREQUIRED )               ; 
double BSRate          = BSRateRate    * AccountEquity ()                                ; 
double SizeLimit     = BSRate / 1000 ;   
//double SizeLimit     = BSRate        / NominalMargin                                   ; 
int Steps;
                                                                                       
if   ( SizeLimit    >= MinLots )                                                       
     { Steps         = MathFloor ( ( SizeLimit - MinLots ) / LotStep )               ; 
       LotSize       = MinLots     + (Steps     * LotStep)                           ; 
     } 
     
                                                                                       
if   ( LotSize      >= MaxLots )  LotSize = MaxLots                                  ; 
  
return(NormalizeDouble(LotSize,1));
}

void DrawTPline(double TPprice)
 {if (ObjectFind(TP)>=0) ObjectDelete(TP);
  ObjectCreate(TP,OBJ_HLINE,0,Time[100],TPprice,Time[0],TPprice);  
  ObjectSet(TP,OBJPROP_STYLE,STYLE_SOLID);
  ObjectSet(TP,OBJPROP_COLOR,TPcolor);
  return;
 }

void DrawSLline(double SLprice)
 {if (ObjectFind(SL)>=0) ObjectDelete(SL);
  ObjectCreate(SL,OBJ_HLINE,0,Time[100],SLprice,Time[0],SLprice);
  ObjectSet(SL,OBJPROP_STYLE,STYLE_SOLID);
  ObjectSet(SL,OBJPROP_COLOR,SLcolor);
  return;
 }

void DrawBSline(double BSprice)
 {if (ObjectFind(BS)>=0) ObjectDelete(BS);
  ObjectCreate(BS,OBJ_HLINE,0,Time[100],BSprice,Time[0],BSprice);
  ObjectSet(BS,OBJPROP_STYLE,STYLE_SOLID);
  ObjectSet(BS,OBJPROP_COLOR,OPcolor); 
  return;
 }

void DrawPCline(double PCprice)
 {if (ObjectFind(PC)>=0) ObjectDelete(PC);
  ObjectCreate(PC,OBJ_HLINE,0,Time[100],PCprice,Time[0],PCprice);
  ObjectSet(PC,OBJPROP_STYLE,STYLE_SOLID);
  ObjectSet(PC,OBJPROP_COLOR,PCcolor); 
  return;
 }
 
void ShowStatus(string stat)
{if (ObjectFind("StatusLine")<0) 
    {objectCreate("StatusLine",100,27, stat,8,"Arial",DodgerBlue);}
 else 
    {ObjectSetText("StatusLine",stat);}
 return;
} 

void ShowInfo(string stat)
{if (ObjectFind("InfoLine")<0) 
    {objectCreate("InfoLine",10,73, stat,8,"Arial",DimGray);}
 else 
    {ObjectSetText("InfoLine",stat);}
 return;
} 

void ShowParameters()
{objectCreate("Jonus",9,27,"Jonus ",8,"Arial",DodgerBlue);
 objectCreate("Title",37,25, "MTrainer  ",9,"Arial",OrangeRed);
 objectCreate("BSRate",10,40, "Order Rate    %  " +DoubleToStr(BSRate,0),9,"Arial",OrangeRed);
 objectCreate("PCRate",10,55, "Partial Close % " +DoubleToStr(PCRate,0),9,"Arial",OrangeRed);
 WindowRedraw();
 return;  
}   

void Error(string msg)
{string MSG=Symbol()+Period() + " Error: "; 
 int Ecode=GetLastError();
 switch(Ecode) 
       {case ERR_NO_ERROR			           :                break;
        case ERR_NO_RESULT			           :                break; 
        case ERR_COMMON_ERROR		           :                break;
        case ERR_INVALID_TRADE_PARAMETERS	   :                break;
        case ERR_SERVER_BUSY			       : Sleep(90000);  break;    // wait 90 Seconds
        case ERR_OLD_VERSION		           :                break;
        case ERR_NO_CONNECTION		           :                break;
        case ERR_TOO_FREQUENT_REQUESTS	       : Sleep(10000);  break;    // wait 10 Seconds
        case ERR_ACCOUNT_DISABLED		       :                break;
        case ERR_INVALID_ACCOUNT		       :                break;
        case ERR_TRADE_TIMEOUT		           : Sleep(60000);  break;    // wait 60 Seconds
        case ERR_INVALID_PRICE		           :                break;    
        case ERR_INVALID_STOPS		           :                break;    
        case ERR_INVALID_TRADE_VOLUME	       :                break;    
        case ERR_MARKET_CLOSED		           :                break;
        case ERR_TRADE_DISABLED		           : Sleep(90000);  break;    // wait 90 Seconds
        case ERR_NOT_ENOUGH_MONEY		       :                break;
        case ERR_PRICE_CHANGED		           :                break;
        case ERR_OFF_QUOTES			           :                break;
        case ERR_REQUOTE			           :                break;
        case ERR_ORDER_LOCKED		           : Sleep(60000);  break;    // wait 60 Seconds
        case ERR_LONG_POSITIONS_ONLY_ALLOWED: 	                break;
        case ERR_TOO_MANY_REQUESTS		       : Sleep(10000);  break;    // wait 10 Seconds               
        case ERR_TOO_MANY_REQUESTS 		       : Sleep(10000);  break;    // wait 10 Seconds               
        case ERR_TOO_MANY_REQUESTS 		       : Sleep(10000);  break;    // wait 10 Seconds               
        case ERR_TOO_MANY_REQUESTS 		       : Sleep(10000);  break;    // wait 10 Seconds                
        case ERR_TRADE_MODIFY_DENIED		   :                break;
        case ERR_TRADE_CONTEXT_BUSY	           : Sleep(10000);  break;    // wait 10 Seconds
        case ERR_TRADE_EXPIRATION_DENIED	   :                break;
        case ERR_TRADE_TOO_MANY_ORDERS	       :                break;
        case ERR_TRADE_HEDGE_PROHIBITED	       :                break; 
        case ERR_TRADE_PROHIBITED_BY_FIFO	   :                break;
       }

 Print(MSG + msg + " " + ErrorDescription(Ecode));
 if (AlertOn) Alert(MSG + msg + " - " + ErrorDescription(Ecode));
 Sleep(2000);
 return;
}

void objectCreate(string name,int x,int y,string text="-",int size=42,
                  string font="Arial",color colour=CLR_NONE)
  {
   ObjectCreate(name,OBJ_LABEL,0,0,0);
   ObjectSet(name,OBJPROP_CORNER,0);
   ObjectSet(name,OBJPROP_COLOR,colour);
   ObjectSet(name,OBJPROP_XDISTANCE,x);
   ObjectSet(name,OBJPROP_YDISTANCE,y);
   ObjectSetText(name,text,size,font,colour);
 
   return;
  }