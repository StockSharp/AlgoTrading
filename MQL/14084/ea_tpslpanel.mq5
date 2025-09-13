//+------------------------------------------------------------------+
//|                                                 EA_TPSLpanel.mq5 |
//|                                 Copyright 2015, SearchSurf (RRD) |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, SearchSurf - RmDj (TheHow)"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property description "This EA is a simple TP/SL panel. TP/SL will not be sent as pending order, and if triggered, it will be executed as an immediate order."
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
input double LotSize       = 0.01;          // Lot size
input color  PANELcolor    = clrLightBlue;  // Panel Color
input color  TP_PointerClr = clrBlue;       // Take Profit Pointer Color
input color  SL_PointerClr = clrRed;        // Stop Loss Pointer Color
input color  StandbyShadow = clrBrown;      // Shadow Color in Standby mode
input bool   AllowPointer  = true;          // Show pointer

int   DtickRUN,shadow; // Decisions on which TP/SL to run on Ontick,shadow panel effect.
int   AnObjClicked;    // Pointer if an edit obj is clicked.
color JustifyEntryClr; // Color to justify entry.
bool  Astate,Runbttn;  // Allow,Run button
bool  Ostate,CLRbttn;  // Out,Clear button
color Rcolor,Ccolor;   // Color assignment for RUN and CLR
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   DtickRUN=0;
   AnObjClicked=0;
   JustifyEntryClr=clrBlack;
   Astate=0;
   Ostate=0;
   Rcolor=clrLightGray;
   Ccolor=clrLightGray;
   Runbttn=0;
   CLRbttn=0;
   shadow=0;
//---
   ChartSetInteger(0,CHART_EVENT_MOUSE_MOVE,0,1);

   ObjectCreate(0,"SHADOW",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(0,"SHADOW",OBJPROP_XDISTANCE,15);
   ObjectSetInteger(0,"SHADOW",OBJPROP_YDISTANCE,100);
   ObjectSetInteger(0,"SHADOW",OBJPROP_XSIZE,280);
   ObjectSetInteger(0,"SHADOW",OBJPROP_YSIZE,95);
   ObjectSetInteger(0,"SHADOW",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(0,"SHADOW",OBJPROP_BGCOLOR,StandbyShadow);

   ObjectCreate(0,"PANEL1",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(0,"PANEL1",OBJPROP_XDISTANCE,10);
   ObjectSetInteger(0,"PANEL1",OBJPROP_YDISTANCE,95);
   ObjectSetInteger(0,"PANEL1",OBJPROP_XSIZE,280);
   ObjectSetInteger(0,"PANEL1",OBJPROP_YSIZE,95);
   ObjectSetInteger(0,"PANEL1",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(0,"PANEL1",OBJPROP_BGCOLOR,PANELcolor);

   ObjectCreate(0,"RUN",OBJ_RECTANGLE_LABEL,0,0,0);   // Run Button
   ObjectSetInteger(0,"RUN",OBJPROP_XDISTANCE,25);
   ObjectSetInteger(0,"RUN",OBJPROP_YDISTANCE,100);
   ObjectSetInteger(0,"RUN",OBJPROP_XSIZE,90);
   ObjectSetInteger(0,"RUN",OBJPROP_YSIZE,20);
   ObjectSetInteger(0,"RUN",OBJPROP_ZORDER,1);
   ObjectSetInteger(0,"RUN",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(0,"RUN",OBJPROP_BGCOLOR,Rcolor);

   ObjectCreate(0,"CLR",OBJ_RECTANGLE_LABEL,0,0,0);   // Clear button
   ObjectSetInteger(0,"CLR",OBJPROP_XDISTANCE,130);
   ObjectSetInteger(0,"CLR",OBJPROP_YDISTANCE,100);
   ObjectSetInteger(0,"CLR",OBJPROP_XSIZE,40);
   ObjectSetInteger(0,"CLR",OBJPROP_YSIZE,20);
   ObjectSetInteger(0,"CLR",OBJPROP_ZORDER,1);
   ObjectSetInteger(0,"CLR",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(0,"CLR",OBJPROP_BGCOLOR,Ccolor);

   ObjectCreate(0,"LABEL1",OBJ_LABEL,0,0,0);           // Label for RUN button
   ObjectSetInteger(0,"LABEL1",OBJPROP_XDISTANCE,40);
   ObjectSetInteger(0,"LABEL1",OBJPROP_YDISTANCE,102);
   ObjectSetInteger(0,"LABEL1",OBJPROP_COLOR,clrBlack);
   ObjectSetString(0,"LABEL1",OBJPROP_TEXT,"< RUN >");

   ObjectCreate(0,"LABEL2",OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,"LABEL2",OBJPROP_XDISTANCE,20);
   ObjectSetInteger(0,"LABEL2",OBJPROP_YDISTANCE,125);
   ObjectSetInteger(0,"LABEL2",OBJPROP_FONTSIZE,9);
   ObjectSetInteger(0,"LABEL2",OBJPROP_COLOR,clrBlack);
   ObjectSetString(0,"LABEL2",OBJPROP_TEXT,"  CurrentPrice:          TP_Price:         SL_Price:");

   ObjectCreate(0,"LABEL3",OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,"LABEL3",OBJPROP_XDISTANCE,190);
   ObjectSetInteger(0,"LABEL3",OBJPROP_YDISTANCE,100);
   ObjectSetInteger(0,"LABEL3",OBJPROP_FONTSIZE,9);
   ObjectSetInteger(0,"LABEL3",OBJPROP_COLOR,clrBlack);
   ObjectSetString(0,"LABEL3",OBJPROP_TEXT,"LotSize: "+DoubleToString(LotSize,2));

   ObjectCreate(0,"LABEL4",OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,"LABEL4",OBJPROP_XDISTANCE,40);
   ObjectSetInteger(0,"LABEL4",OBJPROP_YDISTANCE,142);
   ObjectSetInteger(0,"LABEL4",OBJPROP_COLOR,clrBlack);
   ObjectSetString(0,"LABEL4",OBJPROP_TEXT," ---- ");

   ObjectCreate(0,"LABEL5",OBJ_LABEL,0,0,0);           // Label for Clear button
   ObjectSetInteger(0,"LABEL5",OBJPROP_XDISTANCE,137);
   ObjectSetInteger(0,"LABEL5",OBJPROP_YDISTANCE,102);
   ObjectSetInteger(0,"LABEL5",OBJPROP_COLOR,clrBlack);
   ObjectSetString(0,"LABEL5",OBJPROP_TEXT,"CLR");

   ObjectCreate(0,"LABEL6",OBJ_LABEL,0,0,0);           // for Status remarks.
   ObjectSetInteger(0,"LABEL6",OBJPROP_XDISTANCE,20);
   ObjectSetInteger(0,"LABEL6",OBJPROP_YDISTANCE,170);
   ObjectSetInteger(0,"LABEL6",OBJPROP_COLOR,clrRed);
   ObjectSetString(0,"LABEL6",OBJPROP_TEXT,"> TP/SL inactive.");

   ObjectCreate(0,"EDIT1",OBJ_EDIT,0,0,0);   // TAKE PROFIT LIMIT PRICE
   ObjectSetInteger(0,"EDIT1",OBJPROP_XDISTANCE,130);
   ObjectSetInteger(0,"EDIT1",OBJPROP_YDISTANCE,140);
   ObjectSetInteger(0,"EDIT1",OBJPROP_XSIZE,70);
   ObjectSetInteger(0,"EDIT1",OBJPROP_YSIZE,20);
   ObjectSetInteger(0,"EDIT1",OBJPROP_BGCOLOR,PANELcolor);
   ObjectSetInteger(0,"EDIT1",OBJPROP_BORDER_COLOR,TP_PointerClr);
   ObjectSetInteger(0,"EDIT1",OBJPROP_COLOR,clrBlack);
   ObjectSetInteger(0,"EDIT1",OBJPROP_READONLY,1);

   ObjectCreate(0,"EDIT2",OBJ_EDIT,0,0,0);   // STOP LOSS LIMIT PRICE
   ObjectSetInteger(0,"EDIT2",OBJPROP_XDISTANCE,210);
   ObjectSetInteger(0,"EDIT2",OBJPROP_YDISTANCE,140);
   ObjectSetInteger(0,"EDIT2",OBJPROP_XSIZE,70);
   ObjectSetInteger(0,"EDIT2",OBJPROP_YSIZE,20);
   ObjectSetInteger(0,"EDIT2",OBJPROP_BGCOLOR,PANELcolor);
   ObjectSetInteger(0,"EDIT2",OBJPROP_BORDER_COLOR,SL_PointerClr);
   ObjectSetInteger(0,"EDIT2",OBJPROP_COLOR,clrBlack);
   ObjectSetInteger(0,"EDIT2",OBJPROP_READONLY,1);

   ObjectSetString(0,"LABEL4",OBJPROP_TEXT,DoubleToString(GetBarPrice("close",3,0),_Digits));

   ChartRedraw(0);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectDelete(0,"SHADOW");
   ObjectDelete(0,"PANEL1");
   ObjectDelete(0,"RUN");
   ObjectDelete(0,"CLR");
   ObjectDelete(0,"LABEL1");
   ObjectDelete(0,"LABEL2");
   ObjectDelete(0,"LABEL3");
   ObjectDelete(0,"LABEL4");
   ObjectDelete(0,"LABEL5");
   ObjectDelete(0,"LABEL6");
   ObjectDelete(0,"EDIT1");
   ObjectDelete(0,"EDIT2");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   double TP,SL,CurP;
   string E1_str,E2_str;

   CurP=GetBarPrice("close",3,0);

   if(OpenPosition()=="none" && DtickRUN!=0 && Runbttn==1)
     {
      Alert("Open position was detected closed already: TP/SL Run cancelled.");
      CLRprocess();
     }

   if(DtickRUN==1) // TP&SL
     {
      E1_str=ObjectGetString(0,"EDIT1",OBJPROP_TEXT);
      E2_str=ObjectGetString(0,"EDIT2",OBJPROP_TEXT);
      TP=NormalizeDouble(StringToDouble(E1_str),_Digits);
      SL=NormalizeDouble(StringToDouble(E2_str),_Digits);

      if((OpenPosition()=="buy" && CurP>=TP) || (OpenPosition()=="buy" && CurP<=SL))
        {
         // sell here...
         if(ExecuteTrade("SELL",CurP,LotSize))
           {
            if(CurP>=TP) Alert("TP for BUY position reached. ",TP);
            if(CurP<=SL) Alert("SL for BUY position reached. ",SL);
           }
         else
           {
            if(CurP>=TP) Alert("Warning!!! TP for BUY reached but ORDER FAILED! ",TP);
            if(CurP<=SL) Alert("Warning!!! SL for BUY reached but ORDER FAILED! ",SL);
           }
         CLRprocess();
        }

      if((OpenPosition()=="sell" && CurP<=TP) || (OpenPosition()=="sell" && CurP>=SL))
        {
         // buy here...
         if(ExecuteTrade("BUY",CurP,LotSize))
           {
            if(CurP<=TP) Alert("TP for SELL position reached. ",TP);
            if(CurP>=SL) Alert("SL for SELL position reached. ",SL);
           }
         else
           {
            if(CurP<=TP) Alert("Warning!!! TP for SELL reached but ORDER FAILED! ",TP);
            if(CurP>=SL) Alert("Warning!!! SL for SELL reached but ORDER FAILED! ",SL);
           }
         CLRprocess();
        }
     }

   if(DtickRUN==2) // TP only
     {
      E1_str=ObjectGetString(0,"EDIT1",OBJPROP_TEXT);
      TP=NormalizeDouble(StringToDouble(E1_str),_Digits);

      if(OpenPosition()=="buy" && CurP>=TP)
        {
         // sell here...
         if(ExecuteTrade("SELL",CurP,LotSize)) Alert("TP for BUY position reached. ",TP);
         else Alert("Warning!!! TP for BUY reached but ORDER FAILED! ",TP);
         CLRprocess();
        }

      if(OpenPosition()=="sell" && CurP<=TP)
        {
         // buy here...
         if(ExecuteTrade("BUY",CurP,LotSize)) Alert("TP for SELL position reached. ",TP);
         else Alert("Warning!!! TP for SELL reached but ORDER FAILED! ",TP);
         CLRprocess();
        }
     }

   if(DtickRUN==3) // SL only
     {
      E2_str=ObjectGetString(0,"EDIT2",OBJPROP_TEXT);
      SL=NormalizeDouble(StringToDouble(E2_str),_Digits);

      if(OpenPosition()=="buy" && CurP<=SL)
        {
         // sell here...
         if(ExecuteTrade("SELL",CurP,LotSize)) Alert("SL for BUY position reached. ",SL);
         else Alert("Warning!!! SL for BUY reached but ORDER FAILED! ",SL);
         CLRprocess();
        }

      if(OpenPosition()=="sell" && CurP>=SL)
        {
         // buy here...
         if(ExecuteTrade("BUY",CurP,LotSize))Alert("SL for SELL position reached. ",SL);
         else Alert("Warning!!! SL for SELL reached but ORDER FAILED! ",SL);
         CLRprocess();
        }
     }

// Panel Shadow effects:
   if(Runbttn==0) ObjectSetInteger(0,"SHADOW",OBJPROP_BGCOLOR,StandbyShadow);
   else
     {
      if(shadow==0)
        {
         shadow=1;
         ObjectSetInteger(0,"SHADOW",OBJPROP_BGCOLOR,clrDarkGray);
        }
      else
        {
         shadow=0;
         ObjectSetInteger(0,"SHADOW",OBJPROP_BGCOLOR,clrBlack);
        }
     }
   ObjectSetString(0,"LABEL4",OBJPROP_TEXT,"= "+DoubleToString(GetBarPrice("close",3,0),_Digits));
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   double dprice=0;
   datetime dt=0;
   int window=0;
//---
   if(id==CHARTEVENT_MOUSE_MOVE)
     {
      //Print("Lparm: ",lparam,"Dparm: ",dparam,"StateMouse: ", (uint)sparam);
      if(lparam>11 && dparam>96 && lparam<290 && dparam<190 && (uint)sparam==1) ChartSetInteger(0,CHART_MOUSE_SCROLL,0);
      else ChartSetInteger(0,CHART_MOUSE_SCROLL,1);
      // for RUN button     
      if(lparam>22 && dparam>101 && lparam<109 && dparam<119 && (uint)sparam==1)
        {
         Rcolor=clrDarkGray;

         if(Astate==0)
           {
            Astate=1;
            if(Runbttn==0) Runbttn=1;
            else Runbttn=0;
            Runbttn=RUNprep(Runbttn);
           }
        }
      else
        {
         Astate=0;
         Rcolor=clrLightGray;
        }
      // for CLR button
      if(lparam>130 && dparam>102 && lparam<169 && dparam<119 && (uint)sparam==1)
        {
         Ccolor=clrDarkGray;

         if(Ostate==0)
           {
            Ostate=1;
            if(Runbttn==0) CLRprocess();
            else Alert("Cannot clear TP and SL entries while running...");
           }
        }
      else
        {
         Ostate=0;
         Ccolor=clrLightGray;
        }
     } // --- end of chartevent mouse move   

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="EDIT1" && !Runbttn)
     {
      AnObjClicked=1;
      return;
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="EDIT2" && !Runbttn)
     {
      AnObjClicked=2;
      return;
     }

   if(id==CHARTEVENT_CLICK)
     {
      if(ChartXYToTimePrice(0,(int)lparam,(int)dparam,window,dt,dprice) && lparam>312)
        {
         if(AnObjClicked==1)
           {
            ObjectSetString(0,"EDIT1",OBJPROP_TEXT,DoubleToString(dprice,_Digits));
            if(AllowPointer) EntryPointer("TP",NormalizeDouble(dprice,_Digits),1,3);
           }

         if(AnObjClicked==2)
           {
            ObjectSetString(0,"EDIT2",OBJPROP_TEXT,DoubleToString(dprice,_Digits));
            if(AllowPointer) EntryPointer("SL",NormalizeDouble(dprice,_Digits),1,3);
           }
         AnObjClicked=0;
        }
      ChartRedraw(0);
      return;
     }
   CLRcol(CLRbttn);
   RUNcol(Runbttn);
   ObjectSetString(0,"LABEL4",OBJPROP_TEXT,"= "+DoubleToString(GetBarPrice("close",3,0),_Digits));
//ObjectSetString(0,"LABEL4",OBJPROP_TEXT,"= "+DoubleToString(Astate));
   ChartRedraw(0);
  }
//==========*****Below are additional minor functions used for major ones above*******============================

//+------------------------------------+
//| Execute TRADE                      |
//+------------------------------------+  
bool ExecuteTrade(string Entry,double ThePrice,double lot) // Entry = BUY or SELL / returns true if successfull.
  {
   bool success;

   success=true;

   MqlTradeRequest mreq; // for trade send request.
   MqlTradeResult mresu; // get trade result.
   ZeroMemory(mreq); // Initialize trade send request.

   Print("Order Initialized");
   mreq.action=TRADE_ACTION_DEAL;                              // immediate order execution
   if(Entry=="BUY") mreq.price = NormalizeDouble(ThePrice,_Digits);   // latest bid price
   if(Entry=="SELL") mreq.price = NormalizeDouble(ThePrice,_Digits);  // latest ask price
   mreq.symbol=_Symbol;                                        // currency pair
   mreq.volume=lot;                                        // number of lots to trade
   mreq.magic=11119;                                        // Order Magic Number
   if(Entry=="SELL") mreq.type = ORDER_TYPE_SELL;                // Sell Order
   if(Entry=="BUY") mreq.type = ORDER_TYPE_BUY;                  // Buy Order
   mreq.type_filling = ORDER_FILLING_FOK;                        // Order execution type
   mreq.deviation=100;                                           // Deviation from current price
//--- send order
   if(!OrderSend(mreq,mresu))
     {
      Alert("Order Not Sent: ",GetLastError());
      ResetLastError();
      success=false;
     }
// Result code
   if(mresu.retcode==10009 || mresu.retcode==10008) //Request is completed or order placed       
     {
      if(Entry=="SELL") Print("A Sell order has been successfully placed with Ticket#:",mresu.order,"!!");
      if(Entry=="BUY") Print("A Buy order has been successfully placed with Ticket#:",mresu.order,"!!");
     }
   else
     {
      Alert("Order not completed -error:",GetLastError());
      ResetLastError();
      success=false;
     }

   return(success);
  }
//+--------------------------------+
//| RUN color status               |
//+--------------------------------+ 
void RUNcol(bool run)
  {
   ObjectSetInteger(0,"RUN",OBJPROP_BGCOLOR,Rcolor);
   if(run==1)ObjectSetString(0,"LABEL1",OBJPROP_TEXT,"<CANCEL>");
   if(run==0)ObjectSetString(0,"LABEL1",OBJPROP_TEXT,"< RUN >");
  }
//+--------------------------------+
//| CLR color status               |
//+--------------------------------+ 
void CLRcol(bool clr) //... clr variable not used... reserved for next project.
  {
   ObjectSetInteger(0,"CLR",OBJPROP_BGCOLOR,Ccolor);
  }
//+------------------------------------+
//| Get Price on Bar candle            |
//+------------------------------------+ 
// prices= close, bid, ask ... maxbar = number of bar to check... bar= 0 current, 1 previous and so on.
double GetBarPrice(string prices,int maxbar,int bar)
  {
   double RCurP;

   MqlTick CurP;
   if(!SymbolInfoTick(Symbol(),CurP))
     {
      Alert("SymbolInfoTick() failed, error = ",GetLastError());
      CLRprocess();
      return(0);
     }

   if(prices=="close") RCurP=CurP.bid;
   else if(prices=="bid") RCurP=CurP.bid;
   else if(prices=="ask") RCurP=CurP.ask;
   else
     {
      Alert("Price type error: Unidentified field.");
      return(0.0);
     }
   return(RCurP);
  }
//+------------------------------------+
//| Pointer entry to the graph         |
//+------------------------------------+ 
// Entry = "ENTER/TP/SL", price = entry price, create=0-delete 1-create, maxbar= number of reference bar
void EntryPointer(string Entry,double price,int create,int maxbar)
  {
// Check if there's enough maxbar bars
   if(Bars(_Symbol,_Period)<maxbar)
     {
      Alert("Bars not enough!");
      return;
     }
   MqlRates BarRates[];   // Handles the storage of prices, volumes and spread of each bar
                          // Set BarRates arrays in series.
   ArraySetAsSeries(BarRates,true);

   if(CopyRates(_Symbol,_Period,0,maxbar,BarRates)<0)
     {
      Alert("Error copying BarRates data...error:",GetLastError(),"!!");
      return;
     }

   if(create==0 && Entry=="ENTER")
     {
      if(!ObjectFind(0,"ePNTR")) ObjectDelete(0,"ePNTR");
     }

   if(create==1 && Entry=="ENTER")
     {
      if(ObjectFind(0,"ePNTR"))
        {
         price=NormalizeDouble(price,_Digits);
         ObjectCreate(0,"ePNTR",OBJ_ARROW_RIGHT_PRICE,0,BarRates[0].time,price);
         ObjectSetInteger(0,"ePNTR",OBJPROP_COLOR,JustifyEntryClr);
        }
      else ObjectMove(0,"ePNTR",0,BarRates[0].time,price);
     }

   if(create==0 && Entry=="TP")
     {
      if(!ObjectFind(0,"tPNTR")) ObjectDelete(0,"tPNTR");
     }

   if(create==1 && Entry=="TP")
     {
      if(ObjectFind(0,"tPNTR"))
        {
         price=NormalizeDouble(price,_Digits);
         ObjectCreate(0,"tPNTR",OBJ_ARROW_RIGHT_PRICE,0,BarRates[0].time,price);
         ObjectSetInteger(0,"tPNTR",OBJPROP_COLOR,TP_PointerClr);
        }
      else ObjectMove(0,"tPNTR",0,BarRates[0].time,price);
     }

   if(create==0 && Entry=="SL")
     {
      if(!ObjectFind(0,"sPNTR")) ObjectDelete(0,"sPNTR");
     }

   if(create==1 && Entry=="SL")
     {
      if(ObjectFind(0,"sPNTR"))
        {
         price=NormalizeDouble(price,_Digits);
         ObjectCreate(0,"sPNTR",OBJ_ARROW_RIGHT_PRICE,0,BarRates[0].time,price);
         ObjectSetInteger(0,"sPNTR",OBJPROP_COLOR,SL_PointerClr);
        }
      else ObjectMove(0,"sPNTR",0,BarRates[0].time,price);
     }
   ChartRedraw(0);
  }
//+------------------------------------+
//| Check if there's an open position  |
//+------------------------------------+ 
string OpenPosition() // Returns "none", "buy", "sell"
  {
   string post;
   post="none";

   if(PositionSelect(_Symbol)==true) // open position 
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         post="buy";
        }
      else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         post="sell";
        }
     }
   return(post);
  }
//+------------------------------------+
//| PROCESS CLR CONDITIONS             |
//+------------------------------------+ 
void CLRprocess()
  {
   ObjectSetString(0,"LABEL1",OBJPROP_TEXT,"< RUN  >");
   ObjectSetString(0,"LABEL6",OBJPROP_TEXT,"> TP/SL inactive.");
   ObjectSetInteger(0,"SHADOW",OBJPROP_BGCOLOR,StandbyShadow);
   ObjectSetString(0,"EDIT1",OBJPROP_TEXT,""); // Delete TP entry
   ObjectSetString(0,"EDIT2",OBJPROP_TEXT,""); // Delete SL entry
   DtickRUN=0;
   Runbttn=0;
   EntryPointer("TP",0,0,3);  // Delete TP pointer on graph
   EntryPointer("SL",0,0,3);  // Delete SL pointer on graph
  }
//+------------------------------------+
//| Pre-PROCESS RUN CONDITIONS         |
//| Check conditions if to allow       |   
//| RUN process...                     |    
//+------------------------------------+ 
bool RUNprep(bool run)
  {
   int condition;
   condition=true;
   double TP,SL,CurP;
   string str,remarks;

   remarks="> TP/SL inactive.";
   condition=true;

   if(!run)
     {
      DtickRUN=0;
      ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
      return(false);
     }
// Still here, "run" variable then is true, check if allowed:
   if(OpenPosition()=="none")
     {
      Alert("No open position detected: Cannot assign TP/SL...");
      ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
      DtickRUN=0;
      return(false);
     }

   if(StringLen(ObjectGetString(0,"EDIT1",OBJPROP_TEXT))<=0 && StringLen(ObjectGetString(0,"EDIT2",OBJPROP_TEXT))<=0)
     {
      Alert("Invalid TP or SL entry: At least one of the two entries should be filled.");
      ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
      DtickRUN=0;
      return(false);
     }

   CurP=GetBarPrice("close",3,0); // Get current price.
                                  // Is TP valid?
   str=ObjectGetString(0,"EDIT1",OBJPROP_TEXT);
   TP=NormalizeDouble(StringToDouble(str),_Digits);
   if(StringLen(ObjectGetString(0,"EDIT1",OBJPROP_TEXT))>0 && OpenPosition()=="buy") // Check TP for BUY if valid.
     {
      if(CurP>=TP)
        {
         Alert("Invalid TP entry for BUY: TP already reached!!!");
         ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
         DtickRUN=0;
         return(false);
        }
     }

   if(StringLen(ObjectGetString(0,"EDIT1",OBJPROP_TEXT))>0 && OpenPosition()=="sell") // Check TP for SELL if valid.
     {
      if(CurP<=TP)
        {
         Alert("Invalid TP entry for SELL: TP already reached!!!");
         ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
         DtickRUN=0;
         return(false);
        }
     }
// Is SL valid?
   str=ObjectGetString(0,"EDIT2",OBJPROP_TEXT);
   SL=NormalizeDouble(StringToDouble(str),_Digits);
   if(StringLen(ObjectGetString(0,"EDIT2",OBJPROP_TEXT))>0 && OpenPosition()=="buy") // Check SL for BUY if valid.
     {
      if(CurP<=SL)
        {
         Alert("Invalid SL entry for BUY: SL already reached!!!");
         ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
         DtickRUN=0;
         return(false);
        }
      else  DtickRUN=1;
     }

   if(StringLen(ObjectGetString(0,"EDIT2",OBJPROP_TEXT))>0 && OpenPosition()=="sell") // Check SL for SELL if valid.
     {
      if(CurP>=SL)
        {
         Alert("Invalid SL entry for SELL: SL already reached!!!");
         ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
         DtickRUN=0;
         return(false);
        }
     }
// still here, condition is true then...
   if(StringLen(ObjectGetString(0,"EDIT1",OBJPROP_TEXT))>0 && StringLen(ObjectGetString(0,"EDIT2",OBJPROP_TEXT))>0)
     {
      remarks="> TP/SL is active and running.";
      DtickRUN=1;
     }
   if(StringLen(ObjectGetString(0,"EDIT1",OBJPROP_TEXT))>0 && StringLen(ObjectGetString(0,"EDIT2",OBJPROP_TEXT))<=0)
     {
      remarks="> Only TP is active and running.";
      DtickRUN=2;
     }
   if(StringLen(ObjectGetString(0,"EDIT1",OBJPROP_TEXT))<=0 && StringLen(ObjectGetString(0,"EDIT2",OBJPROP_TEXT))>0)
     {
      remarks="> Only SL is active and running.";
      DtickRUN=3;
     }
   ObjectSetString(0,"LABEL6",OBJPROP_TEXT,remarks);
   return(run);
  }
// ===== nothing follows ===
//+------------------------------------------------------------------+
