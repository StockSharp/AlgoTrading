//+------------------------------------------------------------------+
//|                                        virtual_trailing_stop.mq4 |
//|                                         Copyright 2015, cmillion |
//|                                               http://cmillion.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, cmillion@narod.ru"
#property link      "http://cmillion.ru"
#property strict
//---
input int     Stoploss      = 0;    // ����-����
input int     Takeprofit    = 0;    // ����-������
input int     TrailingStop  = 5;    // ����� �����
input int     TrailingStart = 5;    // ����������� ������� ��� ������
input int     TrailingStep  = 1;    // ��� �����
double   TrallB = 0;
double   TrallS = 0;
int      slippage=30;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double OOP,SL;
   int b=0,s=0,tip,TicketB=0,TicketS=0;
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
           {
            tip = OrderType();
            OOP = NormalizeDouble(OrderOpenPrice(),Digits);
            if(tip==OP_BUY)
              {
               b++;
               TicketB=OrderTicket();
               if(Stoploss!=0   && Bid<=OOP - Stoploss   * Point) {if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),slippage,clrNONE)) continue;}
               if(Takeprofit!=0 && Bid>=OOP + Takeprofit * Point) {if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),slippage,clrNONE)) continue;}
               if(TrailingStop>0)
                 {
                  SL=NormalizeDouble(Bid-TrailingStop*Point,Digits);
                  if(SL>=OOP+TrailingStart*Point && (TrallB==0 || TrallB+TrailingStep*Point<SL)) TrallB=SL;
                 }
              }
            if(tip==OP_SELL)
              {
               s++;
               if(Stoploss!=0   && Ask>=OOP + Stoploss   * Point) {if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),slippage,clrNONE)) continue;}
               if(Takeprofit!=0 && Ask<=OOP - Takeprofit * Point) {if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),slippage,clrNONE)) continue;}
               TicketS=OrderTicket();
               if(TrailingStop>0)
                 {
                  SL=NormalizeDouble(Ask+TrailingStop*Point,Digits);
                  if(SL<=OOP-TrailingStart*Point && (TrallS==0 || TrallS-TrailingStep*Point>SL)) TrallS=SL;
                 }
              }
           }
        }
     }
   if(b!=0)
     {
      if(b>1) Comment("���� ��������� �������� ������ � 1 �������");
      else
      if(TrallB!=0)
        {
         Comment("������ ����� ",TicketB);
         DrawHline("SL Buy",TrallB,clrBlue,1);
         if(Bid<=TrallB)
           {
            if(OrderSelect(TicketB,SELECT_BY_TICKET))
               if(OrderProfit()>0)
                  if(!OrderClose(TicketB,OrderLots(),NormalizeDouble(Ask,Digits),slippage,clrRed))
                     Comment("������ �������� ������ ",GetLastError());
           }
        }
     }
   else {TrallB=0;ObjectDelete("SL Buy");}
//---
   if(s!=0)
     {
      if(s>1) Comment("���� ��������� �������� ������ � 1 �������");
      else
      if(TrallS!=0)
        {
         Comment("������ ����� ",TicketS);
         DrawHline("SL Sell",TrallS,clrRed,1);
         if(Ask>=TrallS)
           {
            if(OrderSelect(TicketS,SELECT_BY_TICKET))
               if(OrderProfit()>0)
                  if(!OrderClose(TicketS,OrderLots(),NormalizeDouble(Ask,Digits),slippage,clrRed))
                     Comment("������ �������� ������ ",GetLastError());
           }
        }
     }
   else {TrallS=0;ObjectDelete("SL Sell");}
//---
   int err;
   if(IsTesting() && OrdersTotal()==0)
     {
      double Lot=0.1;
      err=OrderSend(Symbol(),OP_BUY,Lot,NormalizeDouble(Ask,Digits),slippage,0,0,"����",0);
      err=OrderSend(Symbol(),OP_SELL,Lot,NormalizeDouble(Bid,Digits),slippage,0,0,"����",0);
      return(0);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DrawHline(string name,double P,color clr,int WIDTH)
  {
   if(ObjectFind(name)!=-1) ObjectDelete(name);
   ObjectCreate(name,OBJ_HLINE,0,0,P,0,0,0,0);
   ObjectSet(name,OBJPROP_COLOR,clr);
   ObjectSet(name,OBJPROP_STYLE,2);
   ObjectSet(name,OBJPROP_WIDTH,WIDTH);
  }
//+------------------------------------------------------------------+
