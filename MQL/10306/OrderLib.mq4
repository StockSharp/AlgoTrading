/*=============================================================
 Info:    Order Library 
 Name:    OrderLib.mq4
 Author:  Eric Pribitzer
 Version: 0.94
 Update:  2011-11-03
 Notes:   MAX_ORDERS issue. OL_ClearBuffer()removed (Thx Valentin!)
 Version: 0.93
 Update:  2011-05-24
 Notes:   StopLoss calculation for LOT and RISK, OL_ postfix added
 Version: 0.92
 Update:  2011-05-05
 Notes:   add OP_ALL at OL_enumOrderList(...), OL_PROFITPIPS (calc TP+SL)
 Version: 0.9
 Update:  2011-03-30
 Notes:   LotSize Calculation Update, CUSTOM_TICKVALUE
 Version: 0.8
 Update:  2011-03-28
 Notes:   Bug WriteBuffer removed in TestingMode
          OL_RISK_PIPS and OL_PROFIT_PIPS added and tested
          OL_MAX_ORDERS added 
 Version: 0.7
 Update:  2010-02-11 
 Notes:   ---
  
 Copyright (C) 2010 Erich Pribitzer
=============================================================*/

#property copyright "Copyright © 2011, Erich Pribitzer"
#property link      "http://www.wartris.com"
#property library

#include <BasicH.mqh>
#include <OrderLibH.mqh>


bool   OL_ALLOW_ORDER           = true;
double OL_RISK_PERC             = 0;                                  // in %
int    OL_RISK_PIPS             = 0;                                  // 0=auto (calc price-stoploss)
int    OL_PROFIT_PIPS           = 0;
int    OL_TRAILING_STOP         = 0;
double OL_LOT_SIZE              = 1;                                  // 0=auto lot depends on risk pips and risk percent per trade
double OL_INITIAL_LOT           = 0;
double OL_CUSTOM_TICKVALUE      = 0;                                  // 0=no custom tickvalue
int    OL_SLIP_PAGE             = 0;
bool   OL_STOPSBYMODIFY         = false;                              // true = sl,tp will set by modify order
double OL_MAX_LOT               = 2;
int    OL_MAGIC                 = 0xA1200;                           // 0xA12xx Note: the first two digits must be zero
int    OL_MAX_ORDERS            = 3;
bool   OL_ORDER_DUPCHECK        = true;
bool   OL_OPPOSITE_CLOSE        = false;                             // oder will only closed when the new TP lies between SL and Price of an active order    
bool   OL_ORDER_COLOR           = true;
bool   OL_MYSQL_LOG             = true;    

double OL_orderInfo[OL_SIZE][OL_ORDER_BUFFER_SIZE];
string OL_orderInfoDesc[OL_ORDER_BUFFER_SIZE];
double OL_orderProp[OL_SIZE];
string OL_orderDesc;

#define OL_ORDER_OPEN_CLR Red
#define OL_ORDER_CLOSE_CLR Green
#define OL_ORDER_MODIFY_CLR Violet

#define OL_CL_OPPOSITCLOSE 1
#define OL_CL_TIMEEXPIRE 2
#define OL_CL_STOPLOSS 3
#define OL_CL_TAKEPROFIT 4
#define OL_CL_UNKNOWN 5
#define OL_CL_BYEA 6
#define OL_CL_DUPCHECK 7
#define OL_CL_SIZE 8

string OL_CL_STRING[OL_CL_SIZE]={"","Opposit","Expire","StopLoss","TakePorfit","Unknown","ByEA","Dupcheck"};

double OL_BUFFER_COUNT = 0;

bool OrderLib_Initialized;

int OL_Init(bool ALLOW_ORDER,double RISK_PERC,int RISK_PIPS,int PROFIT_PIPS,int TRAILING_STOP, double LOT_SIZE,double INITIAL_LOT,double CUSTOM_TICKVALUE, int SLIPPAGE, bool STOPSBYMODIFY, double MAX_LOT, int MAX_ORDERS, int MAGIC, bool ORDER_DUPCHECK, bool OPPOSITE_CLOSE, bool ORDER_COLOR, bool MYSQL_LOG)
{
   OL_ALLOW_ORDER           = ALLOW_ORDER;
   OL_RISK_PERC             = RISK_PERC;
   OL_RISK_PIPS             = RISK_PIPS;
   OL_PROFIT_PIPS           = PROFIT_PIPS;
   OL_TRAILING_STOP         = TRAILING_STOP;
   OL_LOT_SIZE              = LOT_SIZE;
   OL_INITIAL_LOT           = INITIAL_LOT;
   OL_CUSTOM_TICKVALUE      = CUSTOM_TICKVALUE;
   OL_SLIP_PAGE             = SLIPPAGE;
   OL_STOPSBYMODIFY          = STOPSBYMODIFY;
   OL_MAX_LOT               = MAX_LOT;
   OL_MAX_ORDERS            = MAX_ORDERS;
   OL_MAGIC                 = MAGIC;
   OL_ORDER_DUPCHECK        = ORDER_DUPCHECK;
   OL_OPPOSITE_CLOSE        = OPPOSITE_CLOSE;
   OL_ORDER_COLOR           = ORDER_COLOR;
   OL_MYSQL_LOG             = MYSQL_LOG;
   
   BA_Init();


   if(IsTesting()==true)
   {
      //OL_ClearBuffer();
      //Print("Testing Mode: Clear Order Buffer");
   }

   if(OrderLib_Initialized==false)
   {         
     Print("Init OrderLib V0.94");
     OL_ReadBuffer();
     OL_SyncBuffer();
     OrderLib_Initialized=true; 
   }   

}

int OL_Deinit()
{
   OL_WriteBuffer();
   OrderLib_Initialized=false;
   Print("Deinit OrderLib");
}

int OL_SyncBuffer()
{

   if(IsTesting()==true) return;
      
   int total=OrdersTotal();
   int magic=0;
   string symbol="";
   double tp=0;
   double sl=0;
   double lot=0;
   int ma=0;
   int inx=0;
   double buff[OL_ORDER_BUFFER_SIZE];
   
   ArrayInitialize(buff,0);
   
   
   for(int pos=0;pos<total;pos++)
   {
      if(OrderSelect(pos,SELECT_BY_POS)==false) continue;
      magic=OrderMagicNumber();
      symbol=OrderSymbol();
      if( (magic&0xFFFFF00) == OL_MAGIC && Symbol() == symbol)
      {         
         // change local order buffer, mark order as aktive 
         buff[magic&0xFF]=1;
/*
         tp=NormalizeDouble(OrderTakeProfit(),DIGITS);
         sl=NormalizeDouble(OrderTakeProfit(),DIGITS);
         lot=NormalizeDouble(OrderLots(),LOTDIGITS);
         if(OL_orderInfo[OL_TAKEPROFIT][magic&0xFF]!=tp || OL_orderInfo[OL_STOPLOSS][magic&0xFF]!=sl || OL_orderInfo[OL_LOTSIZE][magic&0xFF]!=lot)
         {
*/
         if(OL_orderInfo[OL_ID][magic&0xFF]>0)
         {  
            tp=NormalizeDouble(OrderTakeProfit(),DIGITS);
            sl=NormalizeDouble(OrderStopLoss(),DIGITS);
            
            if(tp>0)
               OL_orderInfo[OL_TAKEPROFIT][magic&0xFF]=tp;
            if(sl>0)
               OL_orderInfo[OL_STOPLOSS][magic&0xFF]=sl;
               
            OL_orderInfo[OL_LOTSIZE][magic&0xFF]=NormalizeDouble(OrderLots(),LOTDIGITS);
         }
//       }
      }      
   }

   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   { 
      // sync local order buffer via real active orders
      if(buff[binx]==0 && OL_orderInfo[OL_ID][binx]>0)     // only running orders
      {
         OL_orderInfo[OL_ID][binx]=0;
         OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_UNKNOWN;
         Print("Clear Index: ",binx);
      }   
   }

}

int OL_ReadBuffer()
{
      
      string key="";
      int count=0;
      
      ArrayInitialize(OL_orderProp,0);
       
      for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
      {
         if(IsTesting()==false)
         {
            if(GlobalVariableCheck(OL_FIX + Symbol()  + "_BI" + binx + "I" + OL_ID)==true) 
            {
               count++;
            }
            else
            {
               OL_orderInfo[OL_ID][binx]=0;
               continue;  
            }
         
            for(int inx=0;inx<OL_SIZE;inx++)
            {
               OL_orderInfo[inx][binx]=GlobalVariableGet(OL_FIX + Symbol()  + "_BI" + binx + "I" + inx);
            }
         }
             
         if(OL_orderInfo[OL_ID][binx]!=0)
            OL_Update(binx);
      }
      return (count);
}

int OL_WriteBuffer()
{     
      string key="";
      int count=0;
      for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
      {
         if(OL_orderInfo[OL_ID][binx]!=0) 
         {
            count++;
            OL_Update(binx);
         }
         
         //order has closed state ?? >>
         if(OL_orderInfo[OL_CLOSEREASON][binx]>0)
         {
            int clinx=OL_orderInfo[OL_CLOSEREASON][binx];
            OL_orderInfo[OL_CTIME][binx]=TimeCurrent();
            OL_orderInfo[OL_CBAR][binx]=Bars;
            OL_orderInfo[OL_CSPREAD][binx]=NormalizeDouble(Ask-Bid,DIGITS)/POINT;
            OL_orderInfo[OL_CTICKCOUNT][binx]= GetTickCount() & 0x7FFFFFFF;  

            // was active ?
            if(OL_orderInfo[OL_OID][binx]>0) // yes >>
            {
               // print stats                          
               Print("Close reason: ",OL_CL_STRING[clinx],
               " / Order ID: ",OL_orderInfo[OL_OID][binx],
               " / Minutes: ",NormalizeDouble((OL_orderInfo[OL_CTIME][binx]-OL_orderInfo[OL_OTIME][binx])/60,1),
               " / HiProfit: ",OL_orderInfo[OL_HIPROFIT][binx],
               " / LoProfit: ",OL_orderInfo[OL_LOPROFIT][binx],
               " / Profit: ",OL_orderInfo[OL_PROFIT][binx],
               " / Desc: ",OL_orderInfoDesc[binx]);
             
            } 

            int xx=OL_orderInfo[OL_CLOSEREASON][binx];         
            OL_orderInfo[OL_CLOSEREASON][binx]=0;
            OL_orderInfo[OL_ERRORNO][binx]=0;
            Print("Clear ",binx,",",OL_CL_STRING[xx]);
            
         }

         if(IsTesting()==false )
         {
            for(int inx=0;inx<OL_SIZE;inx++)
            {            
               key=OL_FIX + Symbol()  + "_BI" + binx + "I" + inx;
               if(OL_orderInfo[OL_ID][binx]==0)
               {
                  GlobalVariableDel(key);    
               }
               else
               {
                  //Print("Write Global....",key);
                  GlobalVariableSet( key,  OL_orderInfo[inx][binx]);              
               }
            }
         }
      }
}


void OL_ClearBuffer()
{
   string key;
   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   {
      for(int inx=0;inx<OL_SIZE;inx++)
      {            
         key=OL_FIX + Symbol()  + "_BI" + binx + "I" + inx;
         GlobalVariableDel(key);
         OL_orderInfo[OL_ID][binx]=0;
         OL_orderInfo[OL_CLOSEREASON][binx]=0;   
      }
   }
}


void OL_addOrderProperty(int property,double value)
{
   OL_orderProp[property]=value;      
}

void OL_addOrderDescription(string desc)
{
   OL_orderDesc=desc;      
}

int OL_registerOrder()
{
   
   int buy=OL_orderCount(OP_BUY);
   int sell=OL_orderCount(OP_SELL);
   
   if(buy+sell >= OL_MAX_ORDERS)
   {
      Print("Info: max order limit reached");
      return (0);
   }
   
   int inx;
   int binx=OL_GetFreeIndex();
   if(binx==-1) 
   {
      
      ArrayInitialize(OL_orderProp,0);
      return (-1);
   }
   
   if(OL_OPPOSITE_CLOSE==true)
      OL_processOppositClose(OL_orderProp[OL_TYPE],OL_orderProp[OL_TAKEPROFIT]);
      
   OL_orderProp[OL_OPRICE]=NormalizeDouble(OL_orderProp[OL_OPRICE],DIGITS);
   OL_orderProp[OL_TAKEPROFIT]=NormalizeDouble(OL_orderProp[OL_TAKEPROFIT],DIGITS);
   OL_orderProp[OL_STOPLOSS]=NormalizeDouble(OL_orderProp[OL_STOPLOSS],DIGITS);


   if(OL_orderProp[OL_OPRICE]==0)
   {
      if(OL_orderProp[OL_TYPE]==OP_BUY)
         OL_orderProp[OL_OPRICE]=NormalizeDouble(Ask,DIGITS);
      else
         OL_orderProp[OL_OPRICE]=NormalizeDouble(Bid,DIGITS);
   }

   if(OL_ORDER_DUPCHECK==true)
   {
      for(inx=0;inx<OL_ORDER_BUFFER_SIZE;inx++)
      {
         if(OL_orderInfo[OL_TYPE][inx]==OL_orderProp[OL_TYPE] && OL_orderProp[OL_OPRICE]==OL_orderInfo[OL_OPRICE][inx] )
         {
            if(OL_orderInfo[OL_ID][inx]<0)
            {
               OL_orderInfo[OL_ID][inx]=0;                        // clear pending
               OL_orderInfo[OL_CLOSEREASON][inx]=OL_CL_DUPCHECK;

            }
            else if(OL_orderInfo[OL_ID][inx]>0)
            {
               ArrayInitialize(OL_orderProp,0);
               return (-1);                                    // same order is running!! cancel register order
            }      
         }     
      }
   }

   OL_orderInfo[OL_ID][binx]=OL_SCHEDULED;                     // oder SCHEDULED!!
   for(inx=1;inx<OL_SIZE;inx++)
   {
      OL_orderInfo[inx][binx]=OL_orderProp[inx];      
   }
   OL_orderInfoDesc[binx]=OL_orderDesc;   
   OL_orderDesc="";
   ArrayInitialize(OL_orderProp,0);
   return (binx);   
}

double OL_getOrderProperty(int binx,int property)
{ 
  return (OL_orderInfo[property][binx]);
}

void OL_setOrderProperty(int binx,int property,double value)
{   
   if(OL_orderInfo[OL_ID][binx]>0 && OL_orderInfo[OL_CLOSEREASON][binx]==0)     // change only active and none closed orders
   {
      OL_orderInfo[property][binx]=value;      
   }
}


// Modify or open new order
void OL_processOrder()
{
   OL_processClose(); 
   double price=0;
   
   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   {
      double bid=NormalizeDouble(Bid,DIGITS);
      double ask=NormalizeDouble(Ask,DIGITS);
      
      if(OL_orderInfo[OL_ID][binx]==OL_SCHEDULED) // Open Order
      {
 
         if(OL_ALLOW_ORDER==false) 
         {
            Print("Erro: order disallowed");
            OL_orderInfo[OL_ID][binx]=0;
            return;
         }
         
      // To do: add pending orders, add hidden sl tp
         if(
             ((OL_orderInfo[OL_TYPE][binx]==OP_BUY)  && ( (bid < OL_orderInfo[OL_TAKEPROFIT][binx] || OL_orderInfo[OL_TAKEPROFIT][binx]==0) )) ||
             ((OL_orderInfo[OL_TYPE][binx]==OP_SELL) && ( (ask >= OL_orderInfo[OL_TAKEPROFIT][binx] || OL_orderInfo[OL_TAKEPROFIT][binx]==0) )) ||
             (OL_orderInfo[OL_OTIME][binx]>0 && OL_orderInfo[OL_OTIME][binx] >= TimeCurrent())
           )
         {
             if(OL_orderOpen(binx)==false)
             {             
                  // clear order
                  OL_orderInfo[OL_ID][binx]=0;
             }
             else if(OL_STOPSBYMODIFY==true && OL_orderInfo[OL_TAKEPROFIT][binx]>0 || OL_orderInfo[OL_STOPLOSS][binx]>0)
             {
               
               // set stoploss and takeprofit
               OL_orderModify(binx);

             }
         }

      }
      else if(OL_orderInfo[OL_ID][binx]>0) // Modify active Order
      {
         if(OL_orderInfo[OL_FLAG][binx] == OL_FL_MODIFY)
         {
            // clear flag
            OL_orderInfo[OL_FLAG][binx]=0;
            OL_orderModify(binx);
         }
      }            
   } 

}

int OL_orderCount(int cmd)
{
      int count=0;
      int bx=0;
      while (bx!=-1)
      {   
         bx=OL_enumOrderList(bx,cmd);
         if(bx>=0)
         { 
            count++;
            bx++;
         }
      }
      return (count);
}

// return the order buffer index
// -1 = no elements left
int OL_enumOrderList(int inx, int cmd)
{
   int binx=inx;
   
   while(binx<OL_ORDER_BUFFER_SIZE)
   {     
      if(OL_orderInfo[OL_ID][binx] > 0 && (OL_orderInfo[OL_TYPE][binx]==cmd || (cmd==OL_ALL && (OL_orderInfo[OL_TYPE][binx]==OP_SELL || OL_orderInfo[OL_TYPE][binx]==OP_BUY))) )
      {
         return(binx);      
      }
      binx++;
   }
   return (-1);
}

void OL_processOppositClose(int cmd, double tp)
{
   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   {
      if(
         (cmd==OP_BUY && OL_orderInfo[OL_TYPE][binx]==OP_SELL && OL_orderInfo[OL_ID][binx]!=0 && (tp >= OL_orderInfo[OL_OPRICE][binx] || tp==0) ) ||
         (cmd==OP_SELL && OL_orderInfo[OL_TYPE][binx]==OP_BUY && OL_orderInfo[OL_ID][binx]!=0 && (tp <= OL_orderInfo[OL_OPRICE][binx]|| tp==0) )
        )
      {
            if(OL_orderInfo[OL_ID][binx]>0)
            {
               if(OL_orderClose(binx)==true)
               {              
                  OL_orderInfo[OL_ID][binx]=0;
                  OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_OPPOSITCLOSE;
               }
               else if(OL_orderInfo[OL_ERRORNO][binx]==ERR_INVALID_TICKET)
               {
                  OL_orderInfo[OL_ID][binx]=0;
                  OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_UNKNOWN;
               }
               else
                  Print("Error: close opposite order");
            }
            else if(OL_orderInfo[OL_ID][binx]==OL_SCHEDULED)  // scheduled
            {
              OL_orderInfo[OL_ID][binx]=0; 
              OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_OPPOSITCLOSE;
            }       
      }
   }
}

void OL_processClose()
{  
   double bid=NormalizeDouble(Bid,DIGITS);
   double ask=NormalizeDouble(Ask,DIGITS);
   
   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   {
      if(OL_orderInfo[OL_ID][binx]!=0)
      {
     
         if(OL_orderInfo[OL_TAKEPROFIT][binx]>0 && ((OL_orderInfo[OL_TYPE][binx]==OP_BUY && bid >= OL_orderInfo[OL_TAKEPROFIT][binx]) || (OL_orderInfo[OL_TYPE][binx]==OP_SELL && ask <= OL_orderInfo[OL_TAKEPROFIT][binx])) ) 
         {
            OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_TAKEPROFIT;
         }
         else if(OL_orderInfo[OL_STOPLOSS][binx]>0 && ((OL_orderInfo[OL_TYPE][binx]==OP_BUY && bid <= OL_orderInfo[OL_STOPLOSS][binx]) || (OL_orderInfo[OL_TYPE][binx]==OP_SELL && ask >= OL_orderInfo[OL_STOPLOSS][binx])) ) 
         {      
            OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_STOPLOSS;         
         }
         else if( OL_orderInfo[OL_EXPIRATION][binx]>0 && OL_orderInfo[OL_EXPIRATION][binx] <= TimeCurrent() )
         {
            OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_TIMEEXPIRE;                  
         }
         else if(OL_orderInfo[OL_FLAG][binx]==OL_FL_CLOSE)
         {
            OL_orderInfo[OL_FLAG][binx]=0;
            OL_orderInfo[OL_CLOSEREASON][binx]=OL_CL_BYEA;                              
         }
         else
            continue;

         if(OL_orderInfo[OL_ID][binx]>0)
         {
            if(OL_orderClose(binx)==true)
            {
               OL_orderInfo[OL_ID][binx]=0;
            }
            else if(OL_orderInfo[OL_ERRORNO][binx]==ERR_INVALID_TICKET)
            {

               OL_orderInfo[OL_ID][binx]=0;
            }
            else
               Print("Error: order close failed");
         }
         else
         {                  
           OL_orderInfo[OL_ID][binx]=0;
         }     
      }
   }             
}

int OL_GetFreeIndex()
{
   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   {
      if(OL_orderInfo[OL_ID][binx]==0  && OL_orderInfo[OL_CLOSEREASON][binx]==0  )
         return (binx);
   }
   return (-1);  
}


bool OL_orderOpen(int binx)
{

   double money=AccountFreeMargin();
   double tickvalue=MarketInfo(Symbol(),MODE_TICKVALUE);
   if(OL_CUSTOM_TICKVALUE>0)
      tickvalue=OL_CUSTOM_TICKVALUE;   
   int rp=0;   // risk pips
   
   if(OL_orderInfo[OL_TRAILINGSTOP][binx]==0)
      OL_orderInfo[OL_TRAILINGSTOP][binx]=OL_TRAILING_STOP;
      
   if(OL_orderInfo[OL_SLIPPAGE][binx]==0)
      OL_orderInfo[OL_SLIPPAGE][binx]=OL_SLIP_PAGE;      


   // Calc OL_STOPLOSS only if OL_RISK_PIPS or OL_orderInfo[OL_RISKPIPS][binx] is given
   if(OL_orderInfo[OL_STOPLOSS][binx]==0 && (OL_RISK_PIPS > 0 || OL_orderInfo[OL_RISKPIPS][binx]>0))
   {   
      rp=OL_RISK_PIPS;
      if(OL_orderInfo[OL_RISKPIPS][binx]>0)
         rp=OL_orderInfo[OL_RISKPIPS][binx];

      OL_orderInfo[OL_STOPLOSS][binx]=OL_calcStopLoss(OL_orderInfo[OL_TYPE][binx],rp,OL_OPEN,0);
   }
   
   // Calc OL_STOPLOSS if only RISK and LOT is given
   if((OL_orderInfo[OL_RISKPERC][binx]>0 || OL_RISK_PERC>0) && (OL_RISK_PIPS == 0 || OL_orderInfo[OL_RISKPIPS][binx]==0) && 
      (OL_LOT_SIZE>0 || OL_orderInfo[OL_LOTSIZE][binx]>0) && OL_orderInfo[OL_STOPLOSS][binx]==0)
   {
      double rperc=OL_RISK_PERC;      
      if(OL_orderInfo[OL_RISKPERC][binx]>0)
         rperc=OL_orderInfo[OL_RISKPERC][binx];
      
      double ls=OL_LOT_SIZE;
      if(OL_orderInfo[OL_LOTSIZE][binx]>0)
         ls=OL_orderInfo[OL_LOTSIZE][binx];
               
      rp=0;
      rp=(money/100*rperc)/(ls*tickvalue);
      OL_orderInfo[OL_STOPLOSS][binx]=OL_calcStopLoss(OL_orderInfo[OL_TYPE][binx],rp,OL_OPEN,0);
   }
   

   // Calc TAKEPROFIT only if OL_PROFIT_PIPS or OL_orderInfo[OL_PROFITPIPS][binx] is given
   if(OL_orderInfo[OL_TAKEPROFIT][binx]==0 && (OL_PROFIT_PIPS > 0 || OL_orderInfo[OL_PROFITPIPS][binx]>0))
   {
      int pp=OL_PROFIT_PIPS;
      
      if(OL_orderInfo[OL_PROFITPIPS][binx]>0)
         pp=OL_orderInfo[OL_PROFITPIPS][binx];//+MarketInfo(Symbol(),MODE_SPREAD);

      OL_orderInfo[OL_TAKEPROFIT][binx]=OL_calcTakeProfit(OL_orderInfo[OL_TYPE][binx],pp,OL_OPEN,0);                              
   }
   
   
   int profitPip=MathAbs(OL_orderInfo[OL_TAKEPROFIT][binx]-OL_orderInfo[OL_OPRICE][binx]);

   
   // Calc LOTSIZE 
   if((OL_orderInfo[OL_LOTSIZE][binx]==0 || OL_LOT_SIZE==0) && (OL_orderInfo[OL_RISKPERC][binx]>0 || OL_RISK_PERC>0) && (OL_RISK_PIPS>0 || OL_orderInfo[OL_RISKPIPS][binx]>0))
   {
      int    riskPip;
      double riskPerc;
      
      if(OL_orderInfo[OL_RISKPERC][binx]>0)
         riskPerc=OL_orderInfo[OL_RISKPERC][binx];
      else      
         riskPerc=OL_RISK_PERC;
            
      if(OL_orderInfo[OL_RISKPIPS][binx]>0)
         riskPip=OL_orderInfo[OL_RISKPIPS][binx];   
      else if(OL_RISK_PIPS>0)
         riskPip=OL_RISK_PIPS;
      else
         riskPip=NormalizeDouble(MathAbs(OL_orderInfo[OL_STOPLOSS][binx]-OL_orderInfo[OL_OPRICE][binx]),DIGITS)/POINT;   
         
      OL_orderInfo[OL_LOTSIZE][binx]=NormalizeDouble(OL_INITIAL_LOT,LOTDIGITS)+NormalizeDouble(1.0/tickvalue*(money/100*riskPerc/(riskPip)),LOTDIGITS);
   }
   else
   {      
      if(OL_orderInfo[OL_LOTSIZE][binx]>0)
      {      
         OL_orderInfo[OL_LOTSIZE][binx]=NormalizeDouble(OL_INITIAL_LOT,LOTDIGITS)+NormalizeDouble(OL_orderInfo[OL_LOTSIZE][binx],LOTDIGITS);
      }
      else if(OL_LOT_SIZE>0)
      {
         OL_orderInfo[OL_LOTSIZE][binx]=NormalizeDouble(OL_INITIAL_LOT,LOTDIGITS)+NormalizeDouble(OL_LOT_SIZE,LOTDIGITS);
      }
      else if(OL_INITIAL_LOT>0)
      {
         OL_orderInfo[OL_LOTSIZE][binx]=NormalizeDouble(OL_INITIAL_LOT,LOTDIGITS);
      }   
      else
      {
         Print("Error: wrong lotsize");
         return (false);
      }
   }   
   
   if(OL_orderInfo[OL_LOTSIZE][binx]>OL_MAX_LOT) OL_orderInfo[OL_LOTSIZE][binx]=OL_MAX_LOT;

          
   int      ticketID =0;
   
   color    col=CLR_NONE;
   if(OL_ORDER_COLOR)
      col=OL_ORDER_OPEN_CLR;

   if(OL_orderInfo[OL_TYPE][binx]==OP_SELL)
   {
      if(OL_orderInfo[OL_OPRICE][binx]==0)
         OL_orderInfo[OL_OPRICE][binx]=NormalizeDouble(Bid,DIGITS);
   }
   else
   {
      if(OL_orderInfo[OL_OPRICE][binx]==0)
         OL_orderInfo[OL_OPRICE][binx]=NormalizeDouble(Ask,DIGITS);   
   }

   int      start    = GetTickCount()& 0x7FFFFFFF;
   int      stop;
   int      loop     = 1;
   int      cmd      = OL_orderInfo[OL_TYPE][binx];
   double   sl       = 0;
   double   tp       = 0;
   
   if(OL_STOPSBYMODIFY==false)
   {
     sl=OL_orderInfo[OL_STOPLOSS][binx];
     tp=OL_orderInfo[OL_TAKEPROFIT][binx];
   }
   
   while(loop>=0)
   {
      ticketID=OrderSend(Symbol(),cmd,OL_orderInfo[OL_LOTSIZE][binx],OL_orderInfo[OL_OPRICE][binx],OL_orderInfo[OL_SLIPPAGE][binx],sl,tp,NULL,(OL_MAGIC|binx),0);

      if(loop>0 && ticketID<1 && IsTradeBusy(4,100)==false)
      {
         RefreshRates();
      }
      else
         break;
               
      loop--;
   }

   if(ticketID<1)
   {     
      Print("Error: ",ErrorDescription(GetLastError())); 
      OL_orderInfo[OL_ERRORNO][binx]=GetLastError();
      return(false);
   }
   else
      stop=(GetTickCount()& 0x7FFFFFFF)-start;
      
	OL_orderInfo[OL_OTICKCOUNT][binx]= GetTickCount() & 0x7FFFFFFF;   
   OL_orderInfo[OL_ID][binx]=ticketID;
   OL_orderInfo[OL_OID][binx]=ticketID;
   OL_orderInfo[OL_OBAR][binx]=Bars;
   OL_orderInfo[OL_OSPREAD][binx]=NormalizeDouble(Ask-Bid,DIGITS);
      
   if(OrderSelect(ticketID,SELECT_BY_TICKET)==true)
   {
      OL_orderInfo[OL_OPRICE][binx]=NormalizeDouble(OrderOpenPrice(),DIGITS);
      OL_orderInfo[OL_OTIME][binx]=OrderOpenTime();
      OL_orderInfo[OL_HIPROFITTIME][binx]=OL_orderInfo[OL_OTIME][binx];
      OL_orderInfo[OL_LOPROFITTIME][binx]=OL_orderInfo[OL_OTIME][binx];   
   }
   else
   {
      OL_orderInfo[OL_OTIME][binx]=TimeCurrent();
      OL_orderInfo[OL_HIPROFITTIME][binx]=OL_orderInfo[OL_OTIME][binx];
      OL_orderInfo[OL_LOPROFITTIME][binx]=OL_orderInfo[OL_OTIME][binx];

   }
 
   OL_orderInfo[OL_ERRORNO][binx]=0;
          
   return(true); 
}


// mode=OL_OPEN or OL_CLOSE
double OL_calcStopLoss(int cmd, int pips, int mode, double price=0)
{
   price=NormalizeDouble(price,DIGITS);
   
   if(cmd==OP_BUY)
   {
      if(price==0)
      {
         if(mode==OL_OPEN)
            price=NormalizeDouble(Ask,DIGITS);
         else
            price=NormalizeDouble(Bid,DIGITS);
      }            
      return (NormalizeDouble(price-(pips*POINT),DIGITS));
   }
   else if(cmd==OP_SELL)
   {
      if(price==0) 
      {
         if(mode==OL_OPEN)
            price=NormalizeDouble(Bid,DIGITS);
         else
            price=NormalizeDouble(Ask,DIGITS);         
      }
      return (NormalizeDouble(price+(pips*POINT),DIGITS));  
   }
   
   return (0);
}

double OL_calcTakeProfit(int cmd, int pips, int mode, double price=0)
{
   price=NormalizeDouble(price,DIGITS);
 
 
   if(cmd==OP_BUY)
   {
      if(price==0) 
      {
         if(mode==OL_OPEN)         
            price=NormalizeDouble(Ask,DIGITS);
         else
            price=NormalizeDouble(Bid,DIGITS);         
      }
      return (NormalizeDouble(price+(pips*POINT),DIGITS));
   }
   else if(cmd==OP_SELL)
   {
      if(price==0) 
      {
         if(mode==OL_OPEN)
            price=NormalizeDouble(Bid,DIGITS);                  
         else
            price=NormalizeDouble(Ask,DIGITS);                  
      }
      return (NormalizeDouble(price-(pips*POINT),DIGITS));  
   }
   
   return (0);
}



bool OL_orderModify(int binx)
{

   double sl,tp;   
   bool ret=true;
   color col=CLR_NONE;
   
   if(OL_ORDER_COLOR)
      col=OL_ORDER_MODIFY_CLR;
   
   if(OL_orderInfo[OL_ID][binx]<1) 
   {
      OL_orderInfo[OL_ERRORNO][binx]=ERR_INVALID_TICKET;
      return (false);
   }
 
   int      ticketID = OL_orderInfo[OL_ID][binx];
   int      loop     = 1;
        
   while(loop>=0)
   {
      ret=OrderModify(ticketID,OL_orderInfo[OL_OPRICE][binx],OL_orderInfo[OL_STOPLOSS][binx],OL_orderInfo[OL_TAKEPROFIT][binx],NULL,col);
      OL_orderInfo[OL_ERRORNO][binx]=GetLastError();
      
      if(loop>0 && ret==false && OL_orderInfo[OL_ERRORNO][binx]!=ERR_INVALID_TICKET && IsTradeBusy(4,100)==false) 
      {
         RefreshRates();
      }
      else
         break;
      
      loop--;
   }
   
   if(ret==false)
      Print("Error: ",ErrorDescription(OL_orderInfo[OL_ERRORNO][binx]));
      
   return(ret);
}

bool OL_orderClose(int binx)
{
   Print("OL_Close");
   double price;
   bool ret    = false;
   int ticket  = OL_orderInfo[OL_ID][binx];
 
   
   if(OL_orderInfo[OL_ID][binx]<1) 
   {
      OL_orderInfo[OL_ERRORNO][binx]=ERR_INVALID_TICKET;
      return (false);
   }

   int start=GetTickCount()& 0x7FFFFFFF;

   color col=CLR_NONE;
   if(OL_ORDER_COLOR)
      col=OL_ORDER_CLOSE_CLR;   


   if(OL_orderInfo[OL_TYPE][binx]==OP_SELL)
   {
      if(OL_orderInfo[OL_CPRICE][binx]==0)
         OL_orderInfo[OL_CPRICE][binx]=NormalizeDouble(Ask,DIGITS);
   }
   else
   {
      if(OL_orderInfo[OL_CPRICE][binx]==0)
         OL_orderInfo[OL_CPRICE][binx]=NormalizeDouble(Bid,DIGITS);   
   }
   
   int loop=1;
   while(loop>=0)
   {
   
      
      ret=OrderClose(ticket,OL_orderInfo[OL_LOTSIZE][binx],OL_orderInfo[OL_CPRICE][binx],OL_orderInfo[OL_SLIPPAGE][binx],col);
   
      OL_orderInfo[OL_ERRORNO][binx]=GetLastError();

      if(loop>0 && ret==false && OL_orderInfo[OL_ERRORNO][binx]!=ERR_INVALID_TICKET && IsTradeBusy(4,100)==false) 
      {
         RefreshRates();
      }
      else
         break;
               
      loop--;
   }
   
   if(ret==true /* || OL_orderInfo[OL_ERRORNO][binx]==ERR_INVALID_TICKET */ )
   {
      if(OrderSelect(ticket,SELECT_BY_TICKET)==true)
         OL_orderInfo[OL_CPRICE][binx]=OrderClosePrice();
   }
   else
      Print("Error: ",ErrorDescription(OL_orderInfo[OL_ERRORNO][binx]));

   return(ret);
}

bool IsTradeBusy(int retries,int sleep)
{ 
   while(retries>0)
   {  
      if(IsTradeContextBusy() || !IsTradeAllowed())
      {
         Print("Sleep ",sleep);
         Sleep(sleep);
      }
      else return (false);
      
      retries--;
   }
   Print("Trade Busy");
   return (true);
}


void OL_Update(int binx)
{
   if(OL_orderInfo[OL_ID][binx]==0)
   {
      return;
   }

   // activ order?
   if(OL_orderInfo[OL_ID][binx]>0)
   {           
      if(OL_orderInfo[OL_TYPE][binx]==OP_SELL)
      {
         OL_orderInfo[OL_PROFIT][binx]=NormalizeDouble(OL_orderInfo[OL_OPRICE][binx]-Ask,DIGITS)/POINT;
      }
      else
      {
         OL_orderInfo[OL_PROFIT][binx]=NormalizeDouble(Bid-OL_orderInfo[OL_OPRICE][binx],DIGITS)/POINT;
      }
   
      if(OL_orderInfo[OL_PROFIT][binx] > 0 && OL_orderInfo[OL_PROFIT][binx]>OL_orderInfo[OL_HIPROFIT][binx])
      {

         OL_orderInfo[OL_HIPROFITTIME][binx]=TimeCurrent();
         OL_orderInfo[OL_HIPROFIT][binx]=OL_orderInfo[OL_PROFIT][binx];      
               
      }
      
      if(OL_orderInfo[OL_PROFIT][binx] < 0 && OL_orderInfo[OL_PROFIT][binx] < OL_orderInfo[OL_LOPROFIT][binx])
      {
         OL_orderInfo[OL_LOPROFITTIME][binx]=TimeCurrent();
         OL_orderInfo[OL_LOPROFIT][binx]=OL_orderInfo[OL_PROFIT][binx];
      }
      
      // TRAILINGSTOP UPDATES==================
      if(OL_orderInfo[OL_PROFIT][binx]>0)
      {
         if(OL_orderInfo[OL_TRAILINGSTOP][binx]>0)
         {
               double ask=NormalizeDouble(Ask,DIGITS);
               double bid=NormalizeDouble(Bid,DIGITS);

               if(OL_orderInfo[OL_TYPE][binx]==OP_SELL)
               { 
                  if(OL_orderInfo[OL_TRAILING][binx]==0 || ask<OL_orderInfo[OL_TRAILING][binx])
                  {              
                     OL_orderInfo[OL_TRAILING][binx]=ask;
                     OL_orderInfo[OL_STOPLOSS][binx]=NormalizeDouble(ask+(OL_orderInfo[OL_TRAILINGSTOP][binx]*POINT),DIGITS);
                     OL_orderModify(binx);
                  }                   
               }
               else
               {               
                  if(OL_orderInfo[OL_TRAILING][binx]==0 || bid>OL_orderInfo[OL_TRAILING][binx])
                  {
                     OL_orderInfo[OL_TRAILING][binx]=bid;
                     OL_orderInfo[OL_STOPLOSS][binx]=NormalizeDouble(bid-(OL_orderInfo[OL_TRAILINGSTOP][binx]*POINT),DIGITS);
                     OL_orderModify(binx);
                  }                                  
               }
         }
      }


   }

   //active && inactiv orders 
   OL_orderInfo[OL_CTIME][binx]=TimeCurrent();   
    
   return;   
}


