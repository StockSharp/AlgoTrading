// expert

#property copyright "Idea by wajdyss"
#property link      "wajdyss@yahoo.com"
 
//Œ’«∆’ «·«ﬂ”»Ì— 
extern string ModeNote = "0 = sma, 1 = ema, 2 = smma, 3 = lwma";
extern string PriceNote = "0=Close, 1=Open, 2=High, 3=Low, 4=Median, 5=Typical,6=Weighted Close ";
extern int FastMA = 10;
extern int FastMode  = 1; // 0 = sma, 1 = ema, 2 = smma, 3 = lwma
extern int FastShift  = 0;
extern int FastPrice  = 0;
extern int SlowMA = 20;
extern int SlowMode  = 1; // 0 = sma, 1 = ema, 2 = smma, 3 = lwma
extern int SlowShift  = 0;
extern int SlowPrice  = 0;
extern int TakeProfit=100;
extern int StopLoss=50;
extern int TrailingStop = 0;
extern bool AutoClose=true;
extern double  FirstLots=0.1;
extern bool    Management=true;
//extern double  Risk=0.5;
extern int     Balance=1000;
extern int     MagicNumber = 2009;
 int Open_Hour=0;
 int Close_Hour=231;
 int Close_Minute=45;
 int Friday=51;
 int Friday_Hour=221;
 int Friday_Minute=45;
extern int TextSize=14;
extern color TextColor1=Black;
extern color TextColor2=Blue;
extern color TextColor3=Red;
extern color TextColor4=Black;


int    Ticket1,Ticket2;
int    t1,t2,gT1,gT2;
int handle;
int eyear=9999;
int emonth=9;
int eday=9;
string last_sell_time="LastOrderTimeSell";//***************************************
string last_buy_time ="LastOrderTimeBuy";//***************************************
double Lots;

int init()
{
 last_sell_time=last_sell_time+AccountNumber()+Symbol();//***************************************
 last_buy_time=last_buy_time+AccountNumber()+Symbol();//***************************************
 if(IsTesting())//***************************************
    {//***************************************
     GlobalVariableSet(last_sell_time,0);//***************************************
     GlobalVariableSet(last_buy_time,0);//***************************************
    }//***************************************

  return(0);
}
int deinit()
{
  Comment("");
   ObjectDelete("a label");
 ObjectDelete("b label");
 ObjectDelete("c label");
 ObjectDelete("d label");

  return(0);
}
int start()
{
  //a
          if(ObjectFind("a label") != 0)
   {
      ObjectCreate("a label", OBJ_LABEL, 0,0,0);
      ObjectSetText("a label","»”„ «··Â «·—Õ„‰ «·—ÕÌ„" , TextSize, "Arial", TextColor1);
      ObjectSet("a label", OBJPROP_XDISTANCE,350);
     ObjectSet("a label", OBJPROP_YDISTANCE,0);
   }
   
   //b
      if(ObjectFind("b label") != 0)
   {
      ObjectCreate("b label", OBJ_LABEL, 0,0,0);
      ObjectSetText("b label","wajdyss MA expert"  , TextSize, "Arial", TextColor2);
      ObjectSet("b label", OBJPROP_XDISTANCE,340);
     ObjectSet("b label", OBJPROP_YDISTANCE,25);
   }
   
   // c

   
      if(ObjectFind("c label") != 0)
   {
      ObjectCreate("c label", OBJ_LABEL, 0,0,0);
      ObjectSetText("c label","wajdyss@yahoo.com"  , TextSize, "Arial", TextColor3);
      ObjectSet("c label", OBJPROP_XDISTANCE,335);
     ObjectSet("c label", OBJPROP_YDISTANCE,50);
   }

    if ((Year()>eyear) || (Year()==eyear && Month()>emonth) || (Year()==eyear && Month()==emonth && Day()>eday))
    {
       //d
   if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert has expired , contact us by E-mail" ,TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,255);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }
      return(0);
    } 
   else 
       if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","http://forum.m-e-c.biz",TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,336);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }
      
double lTrailingStop  = TrailingStop;
double sTrailingStop  = TrailingStop;
  if(lTrailingStop>0||sTrailingStop>0){//3  
  TrailingPositionsBuy(lTrailingStop);
  TrailingPositionsSell(sTrailingStop);
  }

    if ((Year()>eyear) || (Year()==eyear && Month()>emonth) || (Year()==eyear && Month()==emonth && Day()>eday))
    {
       //d
   if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert has expired , contact us by E-mail" ,TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,255);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }
      return(0);
    } 
   else 
       if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert well expire after ( " + eday+"-"+emonth+"-"+eyear+" )",TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,270);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }

  if ((DayOfWeek()==Friday && Hour()>=Friday_Hour && Minute()>=Friday_Minute) || (DayOfWeek()==Friday && Hour()>Friday_Hour))
    {
    DeleteBuyPendingOrders(MagicNumber);
    DeleteSellPendingOrders(MagicNumber);
    CloseBuyOrders(MagicNumber);
    CloseSellOrders(MagicNumber);
  }
  if((Hour()==Close_Hour && Minute()>=Close_Minute) || (Hour()>Close_Hour))
  {
    DeleteBuyPendingOrders(MagicNumber);
    DeleteSellPendingOrders(MagicNumber);
    CloseBuyOrders(MagicNumber);
    CloseSellOrders(MagicNumber);
  }

  if (MyBuyRealOrdersTotal(MagicNumber)==0 && MySellRealOrdersTotal(MagicNumber)==0)
{
   if(Management==false)
   {
     Lots=FirstLots;
   }
   else
   {
     Lots=NormalizeDouble((((AccountBalance())/Balance*FirstLots)),2);
     if(Lots>MarketInfo(Symbol(),MODE_MAXLOT)) Lots=MarketInfo(Symbol(),MODE_MAXLOT);
     if(Lots<MarketInfo(Symbol(),MODE_MINLOT)) Lots=MarketInfo(Symbol(),MODE_MINLOT);
   }
}

  {Procces_1();}
 
  return(0);
}
//****************************************************************************************************************  

// «·ÊŸÌ›… «·—∆Ì”Ì…
int Procces_1()
{
  double b,s,bsl,ssl,bt,st,b_l,s_l,bsl_l,ssl_l,bt_l,st_l;
  {
    int total=0;//***************************************

	  for (int cnt=0; cnt<OrdersTotal(); cnt++)//***************************************
      {//***************************************
          OrderSelect(cnt,SELECT_BY_POS) ;  //***************************************
      	if(OrderSymbol()!=Symbol())                                  continue;//***************************************
      	if (OrderMagicNumber()!= MagicNumber)                              continue;//***************************************
         
      	if(OrderType()<=OP_SELL)  //***************************************
      	{//***************************************
      		total++ ;//***************************************
      		if (OrderType()==OP_SELL)//***************************************
      		    { //***************************************
      		     GlobalVariableSet(last_sell_time,OrderOpenTime());//***************************************
      		    }//***************************************
      		if (OrderType()==OP_BUY)//***************************************
      		    {//***************************************
      		     GlobalVariableSet(last_buy_time,OrderOpenTime());//***************************************
      		    }//***************************************
      	}//***************************************
      }//***************************************

int last_sell=GlobalVariableGet(last_sell_time);     //***************************************
int last_buy=GlobalVariableGet(last_buy_time);   //***************************************
bool time_b=(TimeCurrent()-last_buy>=Period()*60);//***************************************
bool time_s=(TimeCurrent()-last_sell>=Period()*60);//***************************************

    double h,h1,l,l1,t,FMA1,SMA1,FMA2,SMA2;
    int h2,l2;
    
    FMA1=iMA(Symbol(), 0, FastMA, FastShift, FastMode, FastPrice, 1);
    FMA2=iMA(Symbol(), 0, FastMA, FastShift, FastMode, FastPrice, 2);

    SMA1=iMA(Symbol(), 0, SlowMA, SlowShift, SlowMode, SlowPrice, 1);
    SMA2=iMA(Symbol(), 0, SlowMA, SlowShift, SlowMode, SlowPrice, 2);
    
     b=Ask;
     
     s=Bid;    

    if (StopLoss>0)
    {
    bsl=b-StopLoss*Point;
    ssl=s+StopLoss*Point;
    }
    else
    {
    bsl=0;
    ssl=0;
    }

    if (TakeProfit>0) 
    {
    bt=b+TakeProfit*Point;
    st=s-TakeProfit*Point;
    }
     else 
     {
     bt=0;
     st=0;
     }

   if (FMA1>SMA1 && FMA2<SMA2 &&  MyBuyRealOrdersTotal(MagicNumber)==0 && time_b)
  
    {
    if (AutoClose) CloseSellOrders(MagicNumber);
    if ((Year()>eyear) || (Year()==eyear && Month()>emonth) || (Year()==eyear && Month()==emonth && Day()>eday))
    {
       //d
   if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert has expired , contact us by E-mail" ,TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,255);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }
      return(0);
    } 
   else 
       if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert well expire after ( " + eday+"-"+emonth+"-"+eyear+" )",TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,270);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }

     OrderSend(Symbol(),OP_BUY     ,Lots,b,3,bsl,bt,"wajdyss MA expert",MagicNumber,0,Green);
    }

   if (FMA1<SMA1 && FMA2>SMA2 &&  MySellRealOrdersTotal(MagicNumber)==0 && time_s)
    {
    if (AutoClose)     CloseBuyOrders(MagicNumber);

    if ((Year()>eyear) || (Year()==eyear && Month()>emonth) || (Year()==eyear && Month()==emonth && Day()>eday))
    {
       //d
   if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert has expired , contact us by E-mail" ,TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,255);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }
       return(0);
    } 
   else 
       if(ObjectFind("d label") != 0)
   {
      ObjectCreate("d label", OBJ_LABEL, 0,0,0);
      ObjectSetText("d label","the expert well expire after ( " + eday+"-"+emonth+"-"+eyear+" )",TextSize, "Arial", TextColor4);
      ObjectSet("d label", OBJPROP_XDISTANCE,270);
     ObjectSet("d label", OBJPROP_YDISTANCE,75);
   }
     OrderSend(Symbol(),OP_SELL     ,Lots,s,3,ssl,st,"wajdyss MA expert",MagicNumber,0,Red);
    }
    return(0);
  }

}
// ÊŸÌ›… ·Õ”«» ⁄œœ «·’›ﬁ«  «·Õ«·Ì…
int MyBuyRealOrdersTotal(int Magic)
{
  int c=0;
  int total  = OrdersTotal();
  t1=-1;
  t2=-1;
 
  for (int cnt = 0 ; cnt < total ; cnt++)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()==OP_BUY))
    {
      if (t1==-1)
      {
        t1=OrderTicket();
      }
      else
      {
        if (t2==-1)
        {
          t2=OrderTicket();
        }
      }
      c++;
    }
  }
  return(c);
}


int MySellRealOrdersTotal(int Magic)
{
  int c=0;
  int total  = OrdersTotal();
  t1=-1;
  t2=-1;
 
  for (int cnt = 0 ; cnt < total ; cnt++)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()==OP_SELL))
    {
      if (t1==-1)
      {
        t1=OrderTicket();
      }
      else
      {
        if (t2==-1)
        {
          t2=OrderTicket();
        }
      }
      c++;
    }
  }
  return(c);
}


// ÊŸÌ›… ·Õ–› «·’›ﬁ«  «·„⁄·ﬁ…
int DeleteBuyPendingOrders(int Magic)
{
int total  = OrdersTotal();
 
for (int cnt = total - 1; cnt >= 0; cnt--)
{
 OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
 if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT))
 {
   OrderDelete(OrderTicket());
 }
}
return(0);
}

int DeleteSellPendingOrders(int Magic)
{
int total  = OrdersTotal();
 
for (int cnt = total - 1; cnt >= 0; cnt--)
{
 OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
 if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT))
 {
   OrderDelete(OrderTicket());
 }
}
return(0);
}

// «€·«ﬁ Ã„Ì⁄ «·’›ﬁ«  «·„› ÊÕ…
//+------------------------------------------------------------------+
int CloseBuyOrders(int Magic)
{
  int total  = OrdersTotal();
  
  for (int cnt = 0 ; cnt < total ; cnt++)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol())
    {
      if (OrderType()==OP_BUY)
      {
        if(OrderClose(OrderTicket(),OrderLots(),Bid,3)==false)
         {
           RefreshRates();
         }
         else
         {
           cnt=0;
           total=OrdersTotal();
         }
      }
      
    }
  }
  return(0);
}  

int CloseSellOrders(int Magic)
{
  int total  = OrdersTotal();
  
  for (int cnt = 0 ; cnt < total ; cnt++)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol())
    {
      if (OrderType()==OP_SELL)
      {
        if(OrderClose(OrderTicket(),OrderLots(),Ask,3)==false)
         {
           RefreshRates();
         }
         else
         {
           cnt=0;
           total=OrdersTotal();
         }
      }
      
    }
  }
  return(0);
}  



void TrailingPositionsBuy(int trailingStop) { 
   for (int i=0; i<OrdersTotal(); i++) { 
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) { 
         if (OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) { 
            if (OrderType()==OP_BUY) { 
               if (Bid-OrderOpenPrice()>trailingStop*Point) { 
                  if (OrderStopLoss()<Bid-trailingStop*Point) 
                     ModifyStopLoss(Bid-trailingStop*Point); 
               } 
            } 
         } 
      } 
   } 
} 
void TrailingPositionsSell(int trailingStop) { 
   for (int i=0; i<OrdersTotal(); i++) { 
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) { 
         if (OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) { 
            if (OrderType()==OP_SELL) { 
               if (OrderOpenPrice()-Ask>trailingStop*Point) { 
                  if (OrderStopLoss()>Ask+trailingStop*Point || OrderStopLoss()==0)  
                     ModifyStopLoss(Ask+trailingStop*Point); 
               } 
            } 
         } 
      } 
   } 
} 

void ModifyStopLoss(double ldStopLoss) { 
   bool fm;
   fm = OrderModify(OrderTicket(),OrderOpenPrice(),ldStopLoss,OrderTakeProfit(),0,CLR_NONE); 
} 

