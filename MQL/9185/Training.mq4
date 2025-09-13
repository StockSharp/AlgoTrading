//+------------------------------------------------------------------+
//|                                                     Training.mq4 |
//|                                                      Denis Orlov |
//|                                    http://denis-or-love.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Denis Orlov"
#property link      "http://denis-or-love.narod.ru"

#include <stdlib.mqh>

extern double lot=0.1;
extern int tprofit=30;
extern int stloss=30;

int PX=5 ,PYH=40,PYL=2, PYTresh=39, FSize=14;//параметры панели
color BClr=Green, SClr=Red, ClClr=Yellow, ProfClr=Green, LossClr=Red, NClr1=Blue;
int BuyX=5,BuyPX=50, SellX=100, SellPX=140, 
OrPrX=200, OrLX=220, OrSlX=260, stepX=15;

int STicket=-1, BTicket=-1;

string Pr="Training ";
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
    DrawLabels(Pr+"Buy", 2, BuyX, PYL, "Buy", NClr1,0, FSize);
    DrawLabels(Pr+"Buy Profit", 2, BuyPX, PYH, "", NClr1,0, FSize);
    DrawLabels(Pr+"Sell", 2, SellX, PYL, "Sell", NClr1,0, FSize);
    DrawLabels(Pr+"Sell Profit", 2, SellPX, PYH, "", NClr1,0, FSize);
    
    DrawLabels(Pr+"Orders Profit", 2, OrPrX, PYL, tprofit, BClr,0, FSize);
    DrawLabels(Pr+"Orders Lot", 2, OrLX, PYH, DoubleToStr(lot,2), NClr1,0, FSize);
    DrawLabels(Pr+"Stop Loss", 2, OrSlX, PYL, stloss, SClr,0, FSize);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
      Delete_My_Obj(Pr);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
     Control();
//----
   return(0);
  }
//+------------------------------------------------------------------+
int Control()
   {
   Comment("Баланс счета = ",AccountBalance()," ; Прибыль = ", AccountProfit()); 
   
  //Comment("BTicket = ",BTicket," ; STicket = ",STicket); 
   
   if(ObjectFind(Pr+"Orders Lot")>-1) 
       lot=StrToDouble( ObjectDescription(Pr+"Orders Lot"));
     // else lot=0.1;
   if(ObjectFind(Pr+"Stop Loss")>-1) 
       stloss=StrToInteger( ObjectDescription(Pr+"Stop Loss") );
     // else stloss=30; 
   if(ObjectFind(Pr+"Orders Profit")>-1) 
      tprofit=StrToInteger( ObjectDescription(Pr+"Orders Profit") );
     // else tprofit=30;  
      
    DrawLabels(Pr+"Orders Profit", 2, OrPrX, PYL, tprofit , BClr,0, FSize);
    DrawLabels(Pr+"Orders Lot", 2, OrLX, PYH, DoubleToStr(lot,2), NClr1,0, FSize);
    DrawLabels(Pr+"Stop Loss", 2, OrSlX, PYL, stloss, SClr,0, FSize);  
    
    //=====================================ПРОВЕРЯЕМ ОРДЕРА
             for(int pos=OrdersTotal()-1; pos>=0; pos--)
         {
           if(OrderSelect(pos,SELECT_BY_POS,MODE_TRADES)==false) continue; 

             if(BTicket==-1 && OrderType()==OP_BUY)
                  {
                   BTicket=OrderTicket();
                   DrawLabels(Pr+"Buy", 2, BuyX, PYH, "Buy", BClr,0, FSize);
                  }
                
             if(STicket==-1 && OrderType()==OP_SELL)
                  {
                  STicket=OrderTicket( );
                  DrawLabels(Pr+"Sell", 2, SellX, PYH, "Sell", SClr,0, FSize);
                  }
                
          }
      
   //=================================ПОКУПАЕМ
      int y=ObjectGet(Pr+"Buy",OBJPROP_YDISTANCE);
      
       if(y>PYTresh && BTicket<0) 
         {
         BTicket=OrderSend(Symbol(),OP_BUY, lot, nd(Ask),3, 
         Ask-stloss*Point, Ask+tprofit*Point,"",0,0, BClr);
         
         if(BTicket==-1)
            {
             int err=GetLastError();
             Comment("Ошибка открытия ордера ",err,": ",ErrorDescription(err));
            // Alert("error(",err,"): ",ErrorDescription(err));
             //return(0);
            }

         
         }
        // else
        if(y<PYTresh)
         {
            if(BTicket!=-1) 
               {
               OrderClose( BTicket, lot, nd(Bid), 3, ClClr);
               //
               BTicket=-1;
               }
               
         DrawLabels(Pr+"Buy", 2, BuyX, PYL, "Buy", NClr1,0, FSize);
         DrawLabels(Pr+"Buy Profit", 2, BuyPX, PYL, "", NClr1,0, FSize); 
                
         } 
         //=================================ПРОДАЕМ
         y=ObjectGet(Pr+"Sell",OBJPROP_YDISTANCE);
       if(y>PYTresh && STicket<0) 
         {
         STicket=OrderSend(Symbol(),OP_SELL, lot, nd(Bid), 3, Bid+stloss*Point, Bid-tprofit*Point, "",0,0, SClr);
         
         if(STicket==-1)
               {
                 err=GetLastError();
                Comment("Ошибка открытия ордера ",err,": ",ErrorDescription(err));
               // Alert("error(",err,"): ",ErrorDescription(err));
                //return(0);
               }
         }
         
        if(y<PYTresh)
         {
            if(STicket!=-1) 
            {
               OrderClose( STicket, lot,nd(Ask), 3, ClClr);
        
               STicket=-1;
             }
         DrawLabels(Pr+"Sell", 2, SellX, PYL, "Sell", NClr1,0, FSize);
         DrawLabels(Pr+"Sell Profit", 2, SellPX, PYL, "", NClr1,0, FSize); 
         } 
         
      //========================== ОТРИСОВЫВАЕМ...
       if(BTicket!=-1)
         {
          if(OrderSelect(BTicket, SELECT_BY_TICKET))
            {
          double Buy_Profit=OrderProfit( );
          if(Buy_Profit<0) color clr=LossClr; else clr=ProfClr;
          DrawLabels(Pr+"Buy Profit", 2, BuyPX, PYH, DoubleToStr(Buy_Profit,1), clr,0, FSize);
          DrawLabels(Pr+"Buy", 2, BuyX, PYH, "Buy", BClr,0, FSize);
            }
            else
            {
            BTicket=-1;
         DrawLabels(Pr+"Buy", 2, BuyX, PYL, "Buy", NClr1,0, FSize);
         DrawLabels(Pr+"Buy Profit", 2, BuyPX, PYL, "", NClr1,0, FSize); 
            }
         }
           
        if(STicket!=-1)
         {
          if(OrderSelect(STicket, SELECT_BY_TICKET))
            {
          double Sell_Profit=OrderProfit( );
          if(Sell_Profit<0)  clr=LossClr; else clr=ProfClr;
          DrawLabels(Pr+"Sell Profit", 2, SellPX, PYH, DoubleToStr(Sell_Profit,1), clr,0, FSize); 
          DrawLabels(Pr+"Sell", 2, SellX, PYH, "Sell", SClr,0, FSize);
            }
            else
            {
            STicket=-1;
         DrawLabels(Pr+"Sell", 2, SellX, PYL, "Sell", NClr1,0, FSize);
         DrawLabels(Pr+"Sell Profit", 2, SellPX, PYL, "", NClr1,0, FSize); 
            }
         }
         
         //=====================================

            
   }
   
int DrawLabels(string name, int corn, int X, int Y, string Text, color Clr=Green, int Win=0, int FSize=10)
   {
     int Error=ObjectFind(name);// Запрос 
   if (Error!=Win)// Если объекта в ук. окне нет :(
    {  
      ObjectCreate(name,OBJ_LABEL,Win, 0,0); // Создание объекта
    }
     
     ObjectSet(name, OBJPROP_CORNER, corn);     // Привязка к углу   
     ObjectSet(name, OBJPROP_XDISTANCE, X);  // Координата Х   
     ObjectSet(name,OBJPROP_YDISTANCE,Y);// Координата Y   
     ObjectSetText(name,Text,FSize,"Arial",Clr);
   }
//---------------
//------------------------------------- 
/*int DrawTrends(string name, datetime T1, double P1, datetime T2, double P2, color Clr, int W=1, string Text="", bool ray=false, int Win=0)
   {
     int Error=ObjectFind(name);// Запрос 
   if (Error!=Win)// Если объекта в ук. окне нет :(
    {  
      ObjectCreate(name, OBJ_TREND, Win,T1,P1,T2,P2);//создание трендовой линии
    }
     
    ObjectSet(name, OBJPROP_TIME1 ,T1);
    ObjectSet(name, OBJPROP_PRICE1,P1);
    ObjectSet(name, OBJPROP_TIME2 ,T2);
    ObjectSet(name, OBJPROP_PRICE2,P2);
    ObjectSet(name, OBJPROP_RAY , ray);
    ObjectSet(name, OBJPROP_COLOR , Clr);
    ObjectSet(name, OBJPROP_WIDTH , W);
    ObjectSetText(name,Text);
   // WindowRedraw();
   }  */
//-------------------------------------
void Delete_My_Obj(string Prefix)
   {//Alert(ObjectsTotal());
   for(int k=ObjectsTotal()-1; k>=0; k--)  // По количеству всех объектов 
     {
      string Obj_Name=ObjectName(k);   // Запрашиваем имя объекта
      string Head=StringSubstr(Obj_Name,0,StringLen(Prefix));// Извлекаем первые сим

      if (Head==Prefix)// Найден объект, ..
         {
         ObjectDelete(Obj_Name);
         //Alert(Head+";"+Prefix);
         }                
        
     }
   }
   
double nd(double in){return(NormalizeDouble(in,Digits));}