//+------------------------------------------------------------------+
//|                                                      TRAYLER.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
#include <stderror.mqh>
#include <stdlib.mqh>
//---- input parameters
extern double    lots = 0.1;
extern bool      DelAllOrders=false;
extern bool      DelSelfOrders=false;
extern bool      Tp=true;
extern bool      sound=true;
extern bool      coment=true;
//-------------
bool traling = true;
static datetime prevtime = 0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
ObjectsDeleteAll(0,OBJ_HLINE);   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
double spr = MarketInfo(Symbol(),MODE_SPREAD);
int total=OrdersTotal();
int ticket,Count,OpCount,OtlCount,err;
//-----
Comment("         ");
//-----------------------------------------------------------
  if(total>0)
   {
   //---------------------------   
    for(int m=0;m<total;m++)
      {
       OrderSelect(m,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()){Count++;}
         if(OrderSymbol()==Symbol()&&(OrderType()==OP_BUY ||OrderType()==OP_SELL)){OpCount++;int tick=OrderTicket();}
          if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP)
           ||(OrderType()==OP_BUYLIMIT)||(OrderType()==OP_SELLLIMIT)){OtlCount++;}
            if(coment){Comment("Найдено ордеров ="+total+"\nОтложенных ="+OtlCount+"\nОткрытых ордеров ="+OpCount+"\nОрдер№"+tick);}   
             }
      //----------------------------------             
      if(OtlCount<1&&OpCount<1)
       {
        if(ObjectFind("Order1")<0)
         { 
          ObjectCreate("Order1", OBJ_HLINE, 0,10, Ask+(30*Point), Blue);
           ObjectSet("Order1",OBJPROP_STYLE,STYLE_DASHDOT );
            ObjectSet("Order1",OBJPROP_COLOR,Blue );
             }
        if(ObjectFind("Order2")<0)
         {     
          ObjectCreate("Order2", OBJ_HLINE, 0,10, Bid-(30*Point), Red);
           ObjectSet("Order2",OBJPROP_STYLE,STYLE_DASHDOT );
            ObjectSet("Order2",OBJPROP_COLOR,Red );
            }
       }         
      //------------------------------------             
           if(OtlCount>0&&OpCount<1)      
            {
             for (int n=0;n<total;n++)
              {
              OrderSelect(n, SELECT_BY_POS, MODE_TRADES);
               if(OrderSymbol()==Symbol()&& OrderType()==OP_BUYSTOP)
                {
                 int TickBuySt=OrderTicket();
                  if(ObjectFind("Order1")<0)
                   { 
                    ObjectCreate("Order1", OBJ_HLINE, 0,10,OrderOpenPrice(), Blue);
                     ObjectSet("Order1",OBJPROP_STYLE,STYLE_DASHDOT );
                      ObjectSet("Order1",OBJPROP_COLOR,Blue );
                       }
                        }
               if(OrderSymbol()==Symbol()&& OrderType()==OP_SELLSTOP)
                {
                 int TickSellSt=OrderTicket();
                  if(ObjectFind("Order2")<0)
                   { 
                    ObjectCreate("Order2", OBJ_HLINE, 0,10,OrderOpenPrice(), Red);
                     ObjectSet("Order2",OBJPROP_STYLE,STYLE_DASHDOT );
                      ObjectSet("Order2",OBJPROP_COLOR,Red );
                       }
                        } 
                   }
              }          
          //--------------------------------     
            if(TickBuySt>0&&ObjectFind("Order1")>=0)
             {
              double BuyStPr = ObjectGet("Order1",OBJPROP_PRICE1 );
               OrderSelect(TickBuySt,SELECT_BY_TICKET,MODE_TRADES);
                if(MathAbs(BuyStPr- OrderOpenPrice())!=0){OrderModify(TickBuySt, BuyStPr,OrderStopLoss(),OrderTakeProfit(), 0, CLR_NONE);}
                 }
            if(TickSellSt>0&&ObjectFind("Order2")>=0)
             {
              double SellStPr = ObjectGet("Order2",OBJPROP_PRICE1 );
               OrderSelect(TickSellSt,SELECT_BY_TICKET,MODE_TRADES);
                if(MathAbs(SellStPr- OrderOpenPrice())!=0){OrderModify(TickSellSt, SellStPr,OrderStopLoss(),OrderTakeProfit(), 0, CLR_NONE);}
                 }
    //----------------------------
     for(int i=0;i<total;i++)
      {
       OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()&&(OrderType()==OP_BUY ||OrderType()==OP_SELL))
         {
     //----------------------    
          if(DelAllOrders)
           {
            for (n=total;n>0;n--)
             {
              OrderSelect(n-1, SELECT_BY_POS, MODE_TRADES);
               if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP)
                ||(OrderType()==OP_BUYLIMIT)||(OrderType()==OP_SELLLIMIT))
                 err=del(OrderTicket());
                   ObjectsDeleteAll(0,OBJ_HLINE);
                    if(sound&&err==0){PlaySound("expert.wav");}
                     } 
                      }
            //-----//-------------       
           if(DelSelfOrders)
            {
             for (n=total;n>0;n--)
              {
               OrderSelect(n-1, SELECT_BY_POS, MODE_TRADES);
                if(OrderSymbol()==Symbol()&&((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP)
                 ||(OrderType()==OP_BUYLIMIT)||(OrderType()==OP_SELLLIMIT)))
                  err=del(OrderTicket());
                    ObjectsDeleteAll(0,OBJ_HLINE);
                     if(sound&&err==0){PlaySound("expert.wav");}
                      } 
                       }           
       //-----------//---------------------                       
       if(OrderType()==OP_BUY&& Minute()%2==0 && traling)
        {
          //-------------------------
            if(Tp)
             {
              double tp=OrderTakeProfit();
               if(tp==0.0&&OrderProfit()>0)
                {
                 int TpFrAp = FindFractal(21,true);
                  if(TpFrAp > 0)
                   {
                    double TpFrApPr = iHigh(Symbol(),NULL,TpFrAp)-((spr)*Point);
                     if(OrderTakeProfit()==0){ModifyOrder(OrderOpenPrice(),OrderStopLoss(),TpFrApPr,Green);}
                     if(OrderTakeProfit()!=0&&OrderTakeProfit()<TpFrApPr)
                       {ModifyOrder(OrderOpenPrice(),OrderStopLoss(),TpFrApPr,Green);}
                        }
                         }           
                          }
                          //------------------------
            int FrDn = FindFractal(7,false);
             if(FrDn > 0)
              {
               double ModFrDnPr = iLow(Symbol(),NULL,FrDn)-((spr+2)*Point);
                if(ModFrDnPr > OrderStopLoss()||OrderStopLoss()==0)
                 {
                  ModifyOrder(OrderOpenPrice(),ModFrDnPr,OrderTakeProfit(),Green);
                   }
                    }    
            if(FrDn == 0)
             {
              ModFrDnPr = iLow(Symbol(),NULL,3)-(2*Point);
               if(ModFrDnPr > OrderStopLoss()||OrderStopLoss()==0)
                {
                 ModifyOrder(OrderOpenPrice(),ModFrDnPr,OrderTakeProfit(),Green);//Sleep(70000);
                  }
                   }
                                                                     
          }//if(OrderType()==OP_BUY&& Minute()%5==0 && traling)--------------
            //-------------------------
                   
         if (OrderType()==OP_SELL&& Minute()%2==0 && traling)
          {

            if(Tp==true)
             {
              tp=OrderTakeProfit();
               if(tp==0&&OrderProfit()>0)
                {
                 int TpFrDn = FindFractal(21,false);
                          
                  if(TpFrDn > 0)
                   {
                    double TpFrDnPr = iLow(Symbol(),NULL,TpFrDn)-((spr)*Point);
                     //Print(OrderTakeProfit(),"  ",OrderProfit(),"  ",TpFrDnPr); 
                     if(OrderTakeProfit()==0){ModifyOrder(OrderOpenPrice(),OrderStopLoss(),TpFrDnPr,Green);}
                     if(OrderTakeProfit()!=0&&OrderTakeProfit()>TpFrDnPr)
                       {ModifyOrder(OrderOpenPrice(),OrderStopLoss(),TpFrDnPr,Green);}
                        }
                         }           
                          }
                          //------------------------            
           int ModFrAp = FindFractal(7,true);
            if(ModFrAp > 0)
             {
              double ModFrApPr = iHigh(Symbol(),NULL,ModFrAp)+(2*Point);
               if((OrderStopLoss() - ModFrApPr >0 )||OrderStopLoss()==0)
                {
                 ModifyOrder(OrderOpenPrice(),ModFrApPr,OrderTakeProfit(),Green);//Sleep(70000);
                  }
                   }
            if(ModFrAp == 0)
             {
              double ModSl = iHigh(Symbol(),NULL,3)+(2*Point);
               if((OrderStopLoss() - ModSl >0)||OrderStopLoss()==0)
                {
                 ModifyOrder(OrderOpenPrice(),ModSl,OrderTakeProfit(),Green);//Sleep(70000);
                  }
                   }
                    }
                  } 
                }
              }
//----
   return(0);
  }
//+------------------------------------------------------------------+
int FindFractal(int count,bool mode)
{
 if (mode == false){int Md = MODE_LOWER;}
  if (mode == true){Md = MODE_UPPER;}
   for(int i = 2; i< count+2; i++)
    {
     int Fr = iFractals(0,0,Md,i);
      if (Fr != 0)
       {
        return(i);
         break;
         }
          }
 return(0);
 }  
//-----------Блок удаления не сработавших отложенников-----------------
int del(int ticket)
   {
    int err;
    for (int i=0;i<5;i++)
        {
        GetLastError();//обнуляем ошику
        OrderDelete(ticket);
        err = GetLastError();
        if (err == 0){break;} if (err != 0) {PlaySound("timeout.wav");} 
        while (!IsTradeAllowed()){ Sleep(5000);}// если рынок занят то подождем 5 сек 
        } 
    return(err);     
    }    
//----------------Блок модификации ордеров----------------------------+    
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 28.11.2006                                                     |
//|  Описание : Модификация одного предварительно выбранного ордера.           |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    pp - цена установки ордера                                              |
//|    sl - ценовой уровень стопа                                              |
//|    tp - ценовой уровень тейка                                              |
//|    cl - цвет значка модификации                                            |
//+----------------------------------------------------------------------------+
void ModifyOrder(double pp=0, double sl=0, double tp=0, color cl=CLR_NONE) {
  bool   fm;
  double op, pa, pb, os, ot,st,oldsl;
  int    dg=MarketInfo(OrderSymbol(), MODE_DIGITS), er, it;
  int    NumberOfTry = 5;
 
  if (pp<=0) {pp=OrderOpenPrice();}
  if (sl<0 ) {sl=OrderStopLoss();}
  if (tp<0 ) {tp=OrderTakeProfit();}
  
  pp=NormalizeDouble(pp, dg);
  sl=NormalizeDouble(sl, dg);
  tp=NormalizeDouble(tp, dg);
  op=NormalizeDouble(OrderOpenPrice() , dg);
  os=NormalizeDouble(OrderStopLoss()  , dg);
  ot=NormalizeDouble(OrderTakeProfit(), dg);
  st=MarketInfo(OrderSymbol(), MODE_STOPLEVEL)*Point;
  if (pp!=op || sl!=os || tp!=ot) {
    for (it=1; it<=NumberOfTry; it++) {
      if (!IsTesting() && (!IsExpertEnabled() || IsStopped())) break;
      while (!IsTradeAllowed()) Sleep(5000);
      RefreshRates();
      if(OrderType()==OP_BUY&& (sl>(Bid+st))){sl=NormalizeDouble(pb-(st*2),dg);}
      if(OrderType()==OP_SELL&& (sl<(Ask+st))){sl=NormalizeDouble(pa+(st*2),dg);}
      //if(sl==os){break;}
      if(OrderType()==OP_SELL&& (sl>os&&os!=0)){break;}
      if(OrderType()==OP_BUY&& (sl<os&&os!=0)){break;}
      fm=OrderModify(OrderTicket(), pp, sl, tp, 0, cl);
      if (fm) {
        if (sound) PlaySound("ok.wav"); break;
      } else {
        er=GetLastError();
        pa=MarketInfo(OrderSymbol(), MODE_ASK);
        pb=MarketInfo(OrderSymbol(), MODE_BID);

        Print("Error(",er,") modifying order: ",ErrorDescription(er),", try ",it);
        Print("Ask=",pa,"  Bid=",pb,"  sy=",OrderSymbol(),
              "  op= Modify"+(OrderType()),"  pp=",pp,"  sl=",sl,"  tp=",tp);
        Sleep(1000*10);
      }
    }
  }
}  
//-------------------------------------------------------------------+