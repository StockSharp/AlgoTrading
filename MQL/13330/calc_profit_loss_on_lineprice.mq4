//+------------------------------------------------------------------+
//|                                                           MSR    |
//|                                      Copyright 2015, MSR corp    |
//|                                        http://www.msrcorp.net    |
//+------------------------------------------------------------------+
#property copyright "MSR Calc Profit Loss on LinePrice"
#property link      "http://ruforum.mt5.com/threads/71794-sovetnik-msr-3.0-multi-v-poiskah-tretey-volni?p=11799234&viewfull=1#post11799234"
#property version     "1.0"      
#property description "MSR Calc Profit Loss on LinePrice"
#property description "По вопросам доработки и сотрудничества обращайтесь:"
#property description "e-mail: complus@inbox.ru"
#property strict; 
//---- Внешние параметры советника
extern string _Parameters_Trade="-------------"; // Параметры
extern int MAGIC=1000;   // MagicNumber for calculate, -1 any Magic
extern string NL="LP";   // Name of Line
extern int fs=16;        // FontSize
extern color cm=clrRed;  // Color prof/loss
//---- Глобальные переменные советника
double dt,ss;
string pl="";
//+------------------------------------------------------------------+
//| Expert start function                                            |
//+------------------------------------------------------------------+
void start()
  {
   for(int cnt=ObjectsTotal()-1; cnt>=0; cnt--)
     {
      string name=ObjectName(cnt);
      if(ObjectType(name)==OBJ_HLINE)
        {
         if(name==NL)
           {
            dt=ObjectGet(name,OBJPROP_PRICE1);
           }
        }
     }
//---
   if(dt>0)
     {
      ss=ProfitIFTakeInCurrency(MAGIC);
      if(ss>0){pl="+";}
      else{pl="";}
      string LM="Profit/Loss = "+pl+DoubleToStr(ss,2)+" "+AccountCurrency();
      Title(LM);
     }
  }
//+------------------------------------------------------------------+
//|   Show Profit/Loss                                               |
//+------------------------------------------------------------------+
void Title(string Show)
  {
   string name_0="L_1";
   if(ObjectFind(name_0)==-1)
     {
      ObjectCreate(name_0,OBJ_LABEL,0,0,0);
      ObjectSet(name_0,OBJPROP_CORNER,0);
      ObjectSet(name_0,OBJPROP_XDISTANCE,390);
      ObjectSet(name_0,OBJPROP_YDISTANCE,50);
     }
   ObjectSetText(name_0,Show,fs,"Arial",cm);
  }
//+----------------------------------------------------------------------------+
//| Автор: Ким Игорь В. aka KimIV,  http://www.kimiv.ru                        |
//+----------------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
//| Calculation:                                                               |
//| mn - MagicNumber                ( -1  - любой магик)                       |
//+----------------------------------------------------------------------------+
double ProfitIFTakeInCurrency(int mn)
  {
   int    i, k=OrdersTotal(); // Подсчет открытых позиций
   double m;                  // Способ расчета прибыли: 0 - Forex, 1 - CFD, 2 - Futures
   double l;                  // Размер контракта в базовой валюте инструмента
   double p;                  // Размер пункта в валюте котировки
   double t;                  // Минимальный шаг изменения цены инструмента в валюте котировки
   double v;                  // Размер минимального изменения цены инструмента в валюте депозита
   double s=0;                // Подсчет стопа в валюте депозита
//---
   l=MarketInfo(Symbol(), MODE_LOTSIZE);
   m=MarketInfo(Symbol(), MODE_PROFITCALCMODE);
   p=MarketInfo(Symbol(), MODE_POINT);
   t=MarketInfo(Symbol(), MODE_TICKSIZE);
   v=MarketInfo(Symbol(), MODE_TICKVALUE);
//---
   for(i=0; i<k; i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && (mn<0 || OrderMagicNumber()==mn))
           {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               if(OrderType()==OP_BUY)
                 {
                  if(m==0) s+=(dt-OrderOpenPrice())/p*v*OrderLots();
                  if(m==1) s+=(dt-OrderOpenPrice())/p*v/t/l*OrderLots();
                  if(m==2) s+=(dt-OrderOpenPrice())/p*v*OrderLots();
                  s+=OrderCommission()+OrderSwap();
                 }
               if(OrderType()==OP_SELL)
                 {
                  if(m==0) s+=(OrderOpenPrice()-dt)/p*v*OrderLots();
                  if(m==1) s+=(OrderOpenPrice()-dt)/p*v/t/l*OrderLots();
                  if(m==2) s+=(OrderOpenPrice()-dt)/p*v*OrderLots();
                  s+=OrderCommission()+OrderSwap();
                 }
              }
           }
        }
     }
   return(s);
  }
//+----------------------------------------------------------------------------+
