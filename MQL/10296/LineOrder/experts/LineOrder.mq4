/*=============================================================
 Info:    LineOrder EA
 Name:    LineOrder.mq4
 Author:  Erich Pribitzer
 Version: 1.0
 Update:  2011-05-12 
 Notes:   ---
  
 Copyright (C) 2011 Erich Pribitzer 
 Email: seizu@gmx.at
=============================================================*/

#property copyright "Copyright © 2011, Erich Pribitzer"
#property link      "http://www.wartris.com"


#include <BasicH.mqh>
#include <OrderLibE.mqh>
#include <OrderLibH.mqh>

extern  string LO_SETTINGS = "===== LINE ORDER SETTINGS ====";
extern  string LO_PREFIX="#";
extern  color  LO_ORDER_CLR=Gray;
extern  int    LO_ORDER_STYLE=STYLE_DASH;
extern  color  LO_STOPLOSS_CLR=Red;
extern  int    LO_STOPLOSS_STYLE=STYLE_DASH;
extern  color  LO_TAKEPROFIT_CLR=Green;
extern  int    LO_TAKEPROFIT_STYLE=STYLE_DASH;


#define LO_KEY_S  0     // SL (pip)
#define LO_KEY_T  1     // TP (pip)
#define LO_KEY_SQ 2     // SL (quote)
#define LO_KEY_TQ 3     // TP (quote)
#define LO_KEY_LOT 4    // LOTSIZE
#define LO_KEY_TS  5    // TRALING STOP
#define LO_KEY_PRICE 6  // PRICE (NOT A KEY !!!!)
#define LO_KEY_SIZE 7   //


// option commands
string LO_KEYS[]={"sl","tp","sq","tq","lo","ts"};


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   BA_Init();
   OL_Init(OL_ALLOW_ORDER,OL_RISK_PERC,OL_RISK_PIPS,OL_PROFIT_PIPS,OL_TRAILING_STOP,OL_LOT_SIZE,OL_INITIAL_LOT,OL_CUSTOM_TICKVALUE,OL_SLIP_PAGE,OL_STOPSBYMODIFY,OL_MAX_LOT,OL_MAX_ORDERS,OL_MAGIC,OL_ORDER_DUPCHECK,OL_OPPOSITE_CLOSE, OL_ORDER_COLOR,OL_MYSQL_LOG);   


   // reload lines
   int binx=0;
   while (true)
   {   
      binx=OL_enumOrderList(binx,OL_ALL);
      if(binx<0) break;
      LO_drawOrderLines("",binx);
      binx++;
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
   OL_Deinit();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   OL_ReadBuffer();
   OL_SyncBuffer();
   LO_lineUpdate();
   LO_checkLineOrders();
   LO_cleanupLines();
   OL_WriteBuffer();

//----

   return(0);
  }
//+------------------------------------------------------------------+



bool LO_getLineProperties(string key, double &outProp[], string &lineName)
{
   double price=0;
   string desc="";
   ArrayInitialize(outProp,0);   
   price=NormalizeDouble(ObjectGet(key, OBJPROP_PRICE1),DIGITS);
   
   if(price<=0) return (false);
   
   outProp[LO_KEY_PRICE]=price;
   desc=ObjectDescription(key)+" ";
 
   int inx_start=0;
   int inx_stop=0;
   for(int t=0;t<LO_KEY_SIZE-1;t++)
   {
      inx_start=StringFind(desc,LO_KEYS[t]+"=",0);    
      if(inx_start==-1) continue;
      else
      {
         inx_start=StringLen(LO_KEYS[t]+"=")+inx_start;
         inx_stop=StringFind(desc," ",inx_start);
         if(inx_stop==-1) continue;
         else
         {
            //Print("t=",t,StringSubstr(desc,inx_start,inx_stop-inx_start));
            outProp[t]=NormalizeDouble(StrToDouble(StringSubstr(desc,inx_start,inx_stop-inx_start)),DIGITS);
         }
      }
   }
 
   lineName=key;
   return (true);  
}
void LO_cleanupLines()
{
   for(int binx=0;binx<OL_ORDER_BUFFER_SIZE;binx++)
   {
      //remove closed oder lines

      if(OL_getOrderProperty(binx,OL_CLOSEREASON)>0 && OL_getOrderProperty(binx,OL_ID)==0)
      {
         int oid=OL_getOrderProperty(binx,OL_OID);
         ObjectDelete(LO_PREFIX+oid);
         ObjectDelete(LO_PREFIX+oid+" SL");
         ObjectDelete(LO_PREFIX+oid+" TP");
      }
   }
}
void LO_checkLineOrders()
{
   double lineProp[LO_KEY_SIZE];
   string lineName="";
   
   if(LO_getLineProperties(LO_PREFIX+"buytp",lineProp,lineName))
   {
      OL_addOrderProperty(OL_TYPE,OP_BUY);
      OL_addOrderProperty(OL_RISKPIPS,lineProp[LO_KEY_S]);
      OL_addOrderProperty(OL_PROFITPIPS,lineProp[LO_KEY_T]);
      OL_addOrderProperty(OL_STOPLOSS,lineProp[LO_KEY_SQ]);                
      OL_addOrderProperty(OL_TAKEPROFIT,lineProp[LO_KEY_PRICE]);         
   }
   else if(LO_getLineProperties(LO_PREFIX+"buysl",lineProp,lineName))
   {
      OL_addOrderProperty(OL_TYPE,OP_BUY);
      OL_addOrderProperty(OL_RISKPIPS,lineProp[LO_KEY_S]);
      OL_addOrderProperty(OL_PROFITPIPS,lineProp[LO_KEY_T]);
      OL_addOrderProperty(OL_TAKEPROFIT,lineProp[LO_KEY_TQ]);                
      OL_addOrderProperty(OL_STOPLOSS,lineProp[LO_KEY_PRICE]);            
   }
   else if(LO_getLineProperties(LO_PREFIX+"buy",lineProp,lineName))
   {
      OL_addOrderProperty(OL_TYPE,OP_BUY);
      OL_addOrderProperty(OL_RISKPIPS,lineProp[LO_KEY_S]);
      OL_addOrderProperty(OL_PROFITPIPS,lineProp[LO_KEY_T]);
      OL_addOrderProperty(OL_TAKEPROFIT,lineProp[LO_KEY_TQ]);                
      OL_addOrderProperty(OL_STOPLOSS,lineProp[LO_KEY_SQ]);               
   }
   else if(LO_getLineProperties(LO_PREFIX+"selltp",lineProp,lineName))
   {
      OL_addOrderProperty(OL_TYPE,OP_SELL);
      OL_addOrderProperty(OL_RISKPIPS,lineProp[LO_KEY_S]);
      OL_addOrderProperty(OL_PROFITPIPS,lineProp[LO_KEY_T]);
      OL_addOrderProperty(OL_STOPLOSS,lineProp[LO_KEY_SQ]);                
      OL_addOrderProperty(OL_TAKEPROFIT,lineProp[LO_KEY_PRICE]);    
   }
   else if(LO_getLineProperties(LO_PREFIX+"sellsl",lineProp,lineName))
   {
      OL_addOrderProperty(OL_TYPE,OP_SELL);
      OL_addOrderProperty(OL_RISKPIPS,lineProp[LO_KEY_S]);
      OL_addOrderProperty(OL_PROFITPIPS,lineProp[LO_KEY_T]);
      OL_addOrderProperty(OL_TAKEPROFIT,lineProp[LO_KEY_TQ]);                
      OL_addOrderProperty(OL_STOPLOSS,lineProp[LO_KEY_PRICE]);    
   }
   else if(LO_getLineProperties(LO_PREFIX+"sell",lineProp,lineName))
   {
      OL_addOrderProperty(OL_TYPE,OP_SELL);
      OL_addOrderProperty(OL_RISKPIPS,lineProp[LO_KEY_S]);
      OL_addOrderProperty(OL_PROFITPIPS,lineProp[LO_KEY_T]);
      OL_addOrderProperty(OL_TAKEPROFIT,lineProp[LO_KEY_TQ]);                
      OL_addOrderProperty(OL_STOPLOSS,lineProp[LO_KEY_SQ]);   
   }
   else
      return (false);

   
   //Print(lineName,"=",lineProp[LO_KEY_PRICE],",",lineProp[LO_KEY_S],",",lineProp[LO_KEY_T],",",lineProp[LO_KEY_SQ],",",lineProp[LO_KEY_TQ],",",lineProp[LO_KEY_LOT]);
   
   int order_inx=-1;
   int ticketID=0;
   double price=0;
   double sl=0;
   double tp=0;

   double lot=OL_LOT_SIZE;
   if(lineProp[LO_KEY_LOT]>0)
      lot=NormalizeDouble(lineProp[LO_KEY_LOT],LOTDIGITS);

   OL_addOrderProperty(OL_TRAILINGSTOP,lineProp[LO_KEY_TS]);
   OL_addOrderProperty(OL_LOTSIZE,lot);       

   //order_inx=1;
   
   OL_addOrderDescription("LineOrder");
   order_inx=OL_registerOrder();   
 
   if(order_inx==-1)
         Print("OL_registerOrder failed!!!");  
   
    OL_processOrder();                    // Close/Modify or Open order(s)
    
    if(order_inx>=0)
    {    
      LO_drawOrderLines(lineName,order_inx);
    }    
}


void LO_drawOrderLines(string lineName,int order_inx)
{

      int ticketID=OL_getOrderProperty(order_inx,OL_ID);
      double price=OL_getOrderProperty(order_inx,OL_OPRICE);
      double tp=OL_getOrderProperty(order_inx,OL_TAKEPROFIT);
      double sl=OL_getOrderProperty(order_inx,OL_STOPLOSS);
      if(ticketID>0)
      {
         ObjectDelete(lineName);
         LO_drawHLine(LO_PREFIX + ticketID,"",price,LO_ORDER_CLR,LO_ORDER_STYLE);
         if(OL_getOrderProperty(order_inx,OL_STOPLOSS)>0)
            LO_drawHLine(LO_PREFIX + ticketID+" SL","sq="+DoubleToStr(sl,DIGITS),sl,LO_STOPLOSS_CLR,LO_STOPLOSS_STYLE);
         if(OL_getOrderProperty(order_inx,OL_TAKEPROFIT)>0)
            LO_drawHLine(LO_PREFIX + ticketID+" TP","tq="+DoubleToStr(tp,DIGITS),tp,LO_TAKEPROFIT_CLR,LO_TAKEPROFIT_STYLE);
      }
}


void LO_lineUpdate()
{
   int ticketID=0;
   double price=0;
   double sl=0;
   double tp=0;
   int    ts=0;
   int    cmd=0;
   double lineProp[LO_KEY_SIZE];
   string lineName="";

        
   int binx=0;
   while (true)
   {   
      binx=OL_enumOrderList(binx,OL_ALL);
      if(binx<0) break;
      ticketID=OL_getOrderProperty(binx,OL_ID);  
      if(ticketID>0)
      {
         
         LO_getLineProperties(LO_PREFIX+ticketID,lineProp,lineName);
         if(lineProp[LO_KEY_PRICE]==0)
         {
            OL_setOrderProperty(binx,OL_FLAG,OL_FL_CLOSE);  
            ObjectDelete(LO_PREFIX+ticketID+" SL");
            ObjectDelete(LO_PREFIX+ticketID+" TP");
         }  
         else 
         {
            cmd=OL_getOrderProperty(binx,OL_TYPE);
            
            if(lineProp[LO_KEY_LOT]>0 || lineProp[LO_KEY_TS]!=0 || 
               lineProp[LO_KEY_S]>0 || lineProp[LO_KEY_T]>0 ||
               lineProp[LO_KEY_SQ]>0 || lineProp[LO_KEY_TQ]>0 )
            {               
               // clear TRAILING ?
               if(lineProp[LO_KEY_TS]<0)
               {
                  OL_setOrderProperty(binx,OL_TRAILINGSTOP,0);
                  OL_setOrderProperty(binx,OL_TRAILING,0);

               }
               
               // change TRAILINGSTOP ??
               if(lineProp[LO_KEY_TS]>0)
               {
                  OL_setOrderProperty(binx,OL_TRAILINGSTOP,lineProp[LO_KEY_TS]);
                  OL_setOrderProperty(binx,OL_TRAILING,0);
               }
               // set new LOT SIZE
               if(lineProp[LO_KEY_LOT]>0)
               {
                  OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY);  
                  OL_setOrderProperty(binx,OL_LOTSIZE,lineProp[LO_KEY_LOT]);
               }   
 

               if(lineProp[LO_KEY_S]>0 || lineProp[LO_KEY_SQ]>0)
               {
                  if(lineProp[LO_KEY_SQ]==0)
                  {
                     sl=OL_calcStopLoss(cmd,lineProp[LO_KEY_S],OL_CLOSE,0);
                  }
                  else
                     sl=NormalizeDouble(lineProp[LO_KEY_SQ],DIGITS);                                    
                     
                  OL_setOrderProperty(binx,OL_STOPLOSS,sl);
                  LO_drawHLine(LO_PREFIX + ticketID+" SL","sq="+sl,sl,LO_STOPLOSS_CLR,LO_STOPLOSS_STYLE);
                  OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY);
               }

               if(lineProp[LO_KEY_T]>0 || lineProp[LO_KEY_TQ]>0)
               {
                  if(lineProp[LO_KEY_TQ]==0)
                  {
                     tp=OL_calcTakeProfit(cmd,lineProp[LO_KEY_T],OL_CLOSE,0);
                  }
                  else
                     tp=NormalizeDouble(lineProp[LO_KEY_TQ],DIGITS);
                                
                  OL_setOrderProperty(binx,OL_TAKEPROFIT,tp);                  
                  LO_drawHLine(LO_PREFIX + ticketID+" TP","tq="+tp,tp,LO_TAKEPROFIT_CLR,LO_TAKEPROFIT_STYLE);
                  OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY); 
               }
               
               
               LO_drawHLine(LO_PREFIX + ticketID,"",lineProp[LO_KEY_PRICE],LO_ORDER_CLR,LO_ORDER_STYLE);

            }
         
            ts=OL_getOrderProperty(binx,OL_TRAILINGSTOP);
            sl=OL_getOrderProperty(binx,OL_STOPLOSS);
            price=NormalizeDouble(ObjectGet(LO_PREFIX+ticketID+" SL", OBJPROP_PRICE1),DIGITS);
            if(price>0 && ts==0 && lineProp[LO_KEY_TS]==0)
            {
               if(sl>0 && sl!=price)               
               {  
                  // move StopLoss
                  OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY);  
                  OL_setOrderProperty(binx,OL_STOPLOSS,price);
               }
            }
            else
            {
               if(sl>0 && OL_getOrderProperty(binx,OL_TRAILING)>0)
               {
                  LO_drawHLine(LO_PREFIX + ticketID+" SL","sq="+DoubleToStr(sl,DIGITS),sl,LO_STOPLOSS_CLR,LO_STOPLOSS_STYLE);
               }
               else if(sl>0)
               {
                  OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY);  
                  OL_setOrderProperty(binx,OL_STOPLOSS,0);
               }
            }

            tp=OL_getOrderProperty(binx,OL_TAKEPROFIT);
            price=NormalizeDouble(ObjectGet(LO_PREFIX + ticketID+" TP", OBJPROP_PRICE1),DIGITS);
            if(price>0)
            {
               if(tp>0 && tp!=price)               
               {  
                  // move TakeProfit
                  OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY);  
                  OL_setOrderProperty(binx,OL_TAKEPROFIT,price);  
               }
            }
            else if(tp>0)
            {
               OL_setOrderProperty(binx,OL_FLAG,OL_FL_MODIFY);  
               OL_setOrderProperty(binx,OL_TAKEPROFIT,0);
            }           
            
         }         
      }
      binx++;
   }
   
   OL_processOrder();                    // Close/Modify or Open order(s)
     
}



void LO_drawHLine(string name, string desc, double price, color col=Red,int style=STYLE_SOLID)
{

   ObjectDelete(name);         
   ObjectCreate(name,OBJ_HLINE,0,NULL,price); 
   ObjectSet(name,OBJPROP_COLOR,col);
   ObjectSet(name, OBJPROP_RAY, false);
   ObjectSet(name,OBJPROP_WIDTH,1);
   ObjectSet(name,OBJPROP_STYLE,style);
   ObjectSetText(name,desc);
}