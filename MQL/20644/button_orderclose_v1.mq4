//+------------------------------------------------------------------+
//|                                                     magic_no.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

string bname="";
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
    datetime x = D'2015.07.07 09:00:00';
    bname="B"+TimeToStr(TimeCurrent());
    if(!ObjectCreate(0,bname,OBJ_BUTTON,0,0,0))
     {
      Print("failed to create the button! Error code = ",GetLastError());
      return(false);
     }
//--- set button coordinates
   ObjectSetInteger(0,bname,OBJPROP_CORNER,CORNER_RIGHT_UPPER);
   ObjectSetInteger(0,bname,OBJPROP_XDISTANCE,350);
   ObjectSetInteger(0,bname,OBJPROP_YDISTANCE,10);
   ObjectSetString(0,bname,OBJPROP_TEXT,"Close Market Orders and Delete Pending Orders");
   ObjectSetInteger(0,bname,OBJPROP_XSIZE,300);
   ObjectSetInteger(0,bname,OBJPROP_YSIZE,30);
//--- set button size

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectsDeleteAll();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+---------
void OnChartEvent(const int id,const long& lparam,const double& dparam,const string& sparam)
//OnChartEvent(CHARTEVENT_OBJECT_CLICK,D'2015.07.07 02:00:00',0.71015,bname);
{
   if(CHARTEVENT_OBJECT_CLICK==true && sparam==bname)
   {
    while(CloseAllOrders(0,10)!=0)
    {
     Sleep(20);
    }
   
   } 
}

int CloseAllOrders(int MagicNumber,int Slippage=10)
{
   int cl = 0;
   int TotalOrder=OrdersTotal();
   for(int i =0;i <TotalOrder;i++)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderMagicNumber() == MagicNumber && OrderSymbol() == Symbol())
         {   
            switch(OrderType())
            {
               case OP_BUY:
                           if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                           {
                              Print("Buy Order Closed successfully");
                           }
                           else
                           {
                              cl=-1;
                               int Error = GetLastError() ;
                               if(Error ==129 )
                               {
                                   Alert("Invalid price. Retrying..");
                                   RefreshRates();                     // Update data
                                   if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                                   {
                                       cl = 0 ; 
                                       Print("Buy Order Closed successfully");
                                   }
                               }   
                               else if(Error ==135 )
                               {       // Price changed
                                    RefreshRates();                
                                     if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                                   {
                                       cl = 0 ; 
                                       Print("Buy Order Closed successfully");
                                   }               // Renew data
                               }  
                                else if(Error ==136 )
                               {       // Price changed
                                   while(RefreshRates()==false)     // Before new tick
                                    Sleep(1);                    
                                     if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                                   {   
                                       cl = 0 ; 
                                       Print("Buy Order Closed successfully");
                                   }        // Error is overcomable
                                else if(Error ==146 )
                               {       // Price changed
                                   while(RefreshRates()==false)     // Before new tick
                                    Sleep(1);                    
                                   if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                                   {
                                       cl = 0 ; 
                                       Print("Buy Order Closed successfully");
                                   }               // Renew data
                               }                    // Error is overcomable
                                
                              //Print("Buy Order place Error #",GetLastError());
                           }
                          
                       }
                           break;           
               case OP_SELL:
                           if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed)) 
                           {
                              Print("Sell Order Closed successfully");
                           }
                           else
                           {
                               cl=-2;
                               int Error = GetLastError(); 
                               if(Error ==129 )
                               {
                                   Alert("Invalid price. Retrying..");
                                   RefreshRates();                     // Update data
                                     if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed)) 
                                    {
                                         cl = 0 ; 
                                       Print("Sell Order Closed successfully");
                                    }
                               }   
                               else if(Error ==135 )
                               {       // Price changed
                                    RefreshRates();                
                                   if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed)) 
                                    {
                                         cl = 0 ; 
                                       Print("Sell Order Closed successfully");
                                    }           // Renew data
                               }  
                                else if(Error ==136 )
                               {       // Price changed
                                   while(RefreshRates()==false)     // Before new tick
                                    Sleep(1);                    
                                   if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed)) 
                                    {
                                         cl = 0 ; 
                                       Print("Sell Order Closed successfully");
                                    }
                               }   // Error is overcomable
                                else if(Error ==146 )
                               {       // Price changed
                                   while(RefreshRates()==false)     // Before new tick
                                    Sleep(1);                    
                                  if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed)) 
                                    {
                                         cl = 0 ; 
                                       Print("Sell Order Closed successfully");
                                    }        // Renew data
                               }                    // Error is overcomable
                                
                              //Print("Sell close Place Error #",Error);
                           }
                           break;
               case OP_BUYLIMIT  :  if(OrderDelete(OrderTicket(),clrBlue))
                                       {
                                          Print("BUYLIMIT Order Closed successfully");
                                       }
                                       else
                                       {
                                          cl=-7;
                                          Print("Buy limit delete Error #",GetLastError());
                                       }
                                       break; 
               case OP_SELLLIMIT :  if(OrderDelete(OrderTicket(),clrBlue))
                                    {
                                       Print("SELLLIMIT Order Closed successfully");
                                    }
                                    else
                                    {
                                       cl=-4;
                                       Print("SELLLIMIT Order Deletion failed with Error #",GetLastError());
                                    }
                                    break;
               case OP_BUYSTOP   :  if(OrderDelete(OrderTicket(),clrBlue))
                                    {
                                       Print("BUYSTOP Order Closed successfully");
                                    }
                                    else
                                    {
                                       cl=-5;
                                        Print("Buy Stop deletion Error #",GetLastError());
                                    }
                                    break;
               case OP_SELLSTOP  :  if(OrderDelete(OrderTicket(),clrBlue))
                                    {
                                       Print("SELLSTOP Order Closed successfully");
                                    }
                                    else
                                    {
                                       cl=-6;
                                       Print("SELLStop Order Deletion failed with Error #",GetLastError());
                                    }
                                    break;
               
            }
         }
      }
      else
      {
         cl=-3;
      }
   }
   return(cl);
}