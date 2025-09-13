//+------------------------------------------------------------------+
//|                                                     Training.mq4 |
//|                                                      Denis Orlov |
//|                                    http://denis-or-love.narod.ru |
/*   "The program-simulator for perfecting strategy, testing of indicators"
      "and trainings of trading skills in general."
      
      "Программа-тренажер для оттачивания стратегий, проверки индикаторов"
      "и тренинга торговых навыков вообще."
      "Подробно о программе : "
      "http://codebase.mql4.com/ru/6012"
      
      "In detail about the program :"
      "http://codebase.mql4.com/6016"
      
      "*****"
      "ПОЛЬЗУЙТЕСЬ И ПРОЦВЕТАЙТЕ!"
      "Все мои индикаторы All my indicators :"
      "http://codebase.mql4.com/author/denis_orlov"
      */
//+------------------------------------------------------------------+
#property copyright "Denis Orlov"
#property link      "http://denis-or-love.narod.ru"

#include <stdlib.mqh>//for ErrorDescription
#include <WinUser32.mqh>//keybd_event

extern double lot=0.1;
extern int tprofit=30;
extern int stloss=30;
//extern bool BreakPointAlert=True;
//extern string BreakPointSound="alert.wav";

int PX=5 ,PYH=40,PYL=2, PYTresh=39, FSize=14;//параметры панели
color BClr=Green, SClr=Red, ClClr=Yellow, ProfClr=Green, LossClr=Red, NClr1=Blue;
int BuyX=5,BuyPX=50, SellX=100, SellPX=140, 
OrPrX=200, OrLX=220, OrSlX=260, BPX=315, BPX2=390,
BModX=45, SModX=150;

int STicket=-1, BTicket=-1;
double upstop=0;
double lowstop=0;

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
    
    DrawLabels(Pr+"Buy Modification", 2, BModX, PYL, "Mod.", BClr,0, FSize);
    DrawLabels(Pr+"Sell Modification", 2, SModX, PYL, "Mod.", SClr,0, FSize);
    
    DrawLabels(Pr+"About Program...", 2, 90, 30, "?", SClr,0, FSize);
    
    DrawLabels(Pr+"Orders Profit", 2, OrPrX, PYL, tprofit, BClr,0, FSize);
    DrawLabels(Pr+"Orders Lot", 2, OrLX, PYH, DoubleToStr(lot,2), NClr1,0, FSize);
    DrawLabels(Pr+"Stop Loss", 2, OrSlX, PYL, stloss, SClr,0, FSize);
    
    DrawLabels(Pr+"Upper Stop", 2, BPX, PYL, DoubleToStr(High[0]+10*Point,4), NClr1,0, FSize);
    DrawLabels(Pr+"Lower Stop", 2, BPX2, PYL, DoubleToStr(Low[0]-10*Point,4), NClr1,0, FSize);
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
     GlobalVariableSet( "TesterTimeCurrent", TimeCurrent());//iTime(NULL, 1,0)
     if( GlobalVariableGet( "BreakPoint")==1)
         {
            GlobalVariableSet( "BreakPoint", -1);
            BreakPoint(GetGlobalString("BP"));
            return;
         }
     Control();
//----
   return(0);
  }
//+------------------------------------------------------------------+
int Control()
   {
   Comment("Balance = ",AccountBalance()," ; Profit = ", AccountProfit()); 
   
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
    
   //=====================================ПРОВЕРЯЕМ СТОПЫ
    if(ObjectFind(Pr+"Upper Stop")>-1) 
       upstop=StrToDouble( ObjectDescription(Pr+"Upper Stop"));
   if(ObjectFind(Pr+"Lower Stop")>-1) 
       lowstop=StrToDouble( ObjectDescription(Pr+"Lower Stop") );
       
       int yup=ObjectGet(Pr+"Upper Stop",OBJPROP_YDISTANCE),  
           ylow=ObjectGet(Pr+"Lower Stop",OBJPROP_YDISTANCE);
    
    //DrawLabels(Pr+"Upper Stop", 2, BPX, yup, DoubleToStr(upstop,4), NClr1,0, FSize);
    //DrawLabels(Pr+"Lower Stop", 2, BPX2, ylow, DoubleToStr(lowstop,4), NClr1,0, FSize); 
    
    if(yup>PYTresh)
      {
         DrawLabels(Pr+"Upper Stop", 2, BPX, PYH, DoubleToStr(upstop,4), ProfClr,0, FSize);
         
         if(Close[0]>=upstop)
            {
            DrawLabels(Pr+"Upper Stop", 2, BPX, PYH, DoubleToStr(upstop,4), LossClr,0, FSize);
               BreakPoint("Upper Stop on "+DoubleToStr(upstop,4));
               //if(BreakPointAlert)Alert("Upper Stop on "+DoubleToStr(upstop,4));//PlaySound(BreakPointSound);
            }
      }
      else DrawLabels(Pr+"Upper Stop", 2, BPX, PYL, DoubleToStr(upstop,4), NClr1,0, FSize);
      
    if(ylow>PYTresh)
      {
         DrawLabels(Pr+"Lower Stop", 2, BPX2, PYH, DoubleToStr(lowstop,4), ProfClr,0, FSize);
         
         if(Close[0]<=lowstop)
            {
            DrawLabels(Pr+"Lower Stop", 2, BPX2, PYH, DoubleToStr(lowstop,4), LossClr,0, FSize);
               BreakPoint("Lower Stop on "+DoubleToStr(lowstop,4));
               //if(BreakPointAlert) PlaySound(BreakPointSound);
            }  
      }
      else DrawLabels(Pr+"Lower Stop", 2, BPX2, PYL, DoubleToStr(lowstop,4), NClr1,0, FSize);
    
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
             Comment("Error of opening Buy ",err,": ",ErrorDescription(err));
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
                Comment("Error of opening Sell ",err,": ",ErrorDescription(err));
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
      //========================== МОДИФИЦИРУЕМ... 
      y=ObjectGet(Pr+"Buy Modification",OBJPROP_YDISTANCE);
      if(y>PYTresh && BTicket!=-1)
         {
            OrderSelect(BTicket,SELECT_BY_TICKET);
            bool Ans=OrderModify(BTicket,OrderOpenPrice(),
            OrderOpenPrice()-stloss*Point, OrderOpenPrice()+tprofit*Point, 0);//Модифи его!
            if(Ans==False)
            {
               err=GetLastError();
                Comment("Error of Modification ",err,": ",ErrorDescription(err));
            }
         }
         
       y=ObjectGet(Pr+"Sell Modification",OBJPROP_YDISTANCE);  
       if(y>PYTresh && STicket!=-1)
         {
            OrderSelect(STicket,SELECT_BY_TICKET);
             Ans=OrderModify(STicket,OrderOpenPrice(),
            OrderOpenPrice()+stloss*Point, OrderOpenPrice()-tprofit*Point, 0);//Модифи его!
            if(Ans==False)
            {
               err=GetLastError();
                Comment("Error of Modification ",err,": ",ErrorDescription(err));
            }
         }  
        
      DrawLabels(Pr+"Buy Modification", 2, BModX, PYL, "Mod.", BClr,0, FSize);
      DrawLabels(Pr+"Sell Modification", 2, SModX, PYL, "Mod.", SClr,0, FSize);
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
         
         //=====================================ABOUT PROGRAM
         y=ObjectGet(Pr+"About Program...",OBJPROP_YDISTANCE);
          if(y>PYTresh) AlertHelp();
         DrawLabels(Pr+"About Program...", 2, 90, 30, "?", SClr,0, FSize);

            
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

void AlertHelp()
   {
      string text=
      "*****"+"Trading Simulator v.2."+"*****\n"+///"\t","\n"
      "Author :"+"Денис Орлов"+"\n"+
      "http://denis-or-love.narod.ru"+"\n"+
      "http://vkontakte.ru/club3368806"+"\n"+"\n"+
      
      "The program-simulator for perfecting strategy, testing of indicators"+"\n"+
      "and trainings of trading skills in general."+"\n"+"\n"+
      
      "In detail about the program :"+"\n"+
      "http://codebase.mql4.com/6016"+"\n"+"\n"+
      
      "*****\n"+
      "ПОЛЬЗУЙТЕСЬ И ПРОЦВЕТАЙТЕ!"+"\n"+"\n"+
      "All my indicators :"+"\n"+
      "http://codebase.mql4.com/author/denis_orlov";
      
      //MessageBox(text, "СПРАВКА ПО ИНДИКАТОРУ",MB_OK|MB_ICONINFORMATION );
       BreakPoint(text);
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
   
double nd(double in)
   {return(NormalizeDouble(in,Digits));
   }
   
void BreakPoint(string Comm="BreakPoint!")
{
   //It is expecting, that this function should work
   //only in tester
   if (!IsVisualMode()) return(0);

   Comment(Comm);
   
   //Press/release Pause button
   //19 is a Virtual Key code of "Pause" button
   //Sleep() is needed, because of the probability
   //to misprocess too quick pressing/releasing
   //of the button
   keybd_event(19,0,0,0);
   Sleep(100);
   keybd_event(19,0,2,0);
}

string GetGlobalString(string Name)
   {
      int    var_total=GlobalVariablesTotal();
         string name;
         for(int i=0;i<var_total;i++)
        {
         name=GlobalVariableName(i);
       //  Alert(name);
         if(StringFind(name, "GlStr_"+Name)==0)
         return(StringSubstr(name,StringLen("GlStr_"+Name)));
        }
     return("");
   }