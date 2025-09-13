//+------------------------------------------------------------------+
//|                                            TrailingStopLight.mq4 |
//|                              Copyright � 2011, Khlystov Vladimir |
//|                                         http://cmillion.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, cmillion@narod.ru"
#property link      "http://cmillion.narod.ru"
#property show_inputs
//--------------------------------------------------------------------
extern int     Stoploss             = 10,     //��������, ���� 0 �� �� ����������
               Takeprofit           = 50;     //����������, ���� 0 �� �� ����������
extern int     TrailingStop         = 10;     //������ ������, ���� 0 �� ��� ������
extern int     TrailingStart        = 0;      //����� �������� �����, �������� ����� ���������� 40 � ������
extern int     StepTrall            = 2;      //��� ������ - ���������� �������� �� ����� ��� StepTrall
extern int     NoLoss               = 0,      //������� � ��������� ��� �������� ���-�� ������� �������, ���� 0 �� ��� �������� � ���������
               MinProfitNoLoss      = 0;      //����������� ������� ��� �������� ����������
extern int     Magic                = 77;     //�����
extern int     Step                 = 10;     //���������� �� ����
extern double  Lot                  = 0.1;
extern int     TimeModify           = 30;     //���-�� ������ ������ �������� ��������� �������� �����
extern int     slippage             = 30;     //����������� ���������� ���������� ���� ��� �������� ������� (������� �� ������� ��� �������).
//--------------------------------------------------------------------
int  STOPLEVEL,TimeBarB,TimeBarS;
//--------------------------------------------------------------------
int init()
{
}
//--------------------------------------------------------------------
int deinit()
{
}
//--------------------------------------------------------------------
int start()                                  
{
   STOPLEVEL=MarketInfo(Symbol(),MODE_STOPLEVEL);
   double OSL,StLo,PriceB,PriceS,OOP,SL,TP;
   int b,s,TicketB,TicketS,OT;
   for (int i=0; i<OrdersTotal(); i++)
   {    
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      {
         if (OrderSymbol()==Symbol() && Magic==OrderMagicNumber())
         { 
            OT = OrderType(); 
            OSL = NormalizeDouble(OrderStopLoss(),Digits);
            OOP = NormalizeDouble(OrderOpenPrice(),Digits);
            SL=OSL;
            if (OT==OP_BUY)             
            {  
               b++;
               if (OSL<OOP && NoLoss!=0)
               {
                  StLo = NormalizeDouble(OOP+MinProfitNoLoss*Point,Digits); 
                  if (StLo > OSL && StLo <= NormalizeDouble(Bid - STOPLEVEL * Point,Digits)) SL = StLo;
               }
               
               if (TrailingStop>=STOPLEVEL && TrailingStop!=0 && (Bid - OOP)/Point >= TrailingStart)
               {
                  StLo = NormalizeDouble(Bid - TrailingStop*Point,Digits); 
                  if (StLo>=OOP && StLo > OSL+StepTrall*Point) SL = StLo;
               }
               
               if (SL > OSL)
               {  
                  if (!OrderModify(OrderTicket(),OOP,SL,TP,0,White)) Print("Error ",GetLastError(),"   Order Modify Buy   SL ",OSL,"->",SL);
                  else Print("Order Buy Modify   SL ",OSL,"->",SL);
               }
            }                                         
            if (OT==OP_SELL)        
            {
               s++;
               if ((OSL>OOP || OSL==0) && NoLoss!=0)
               {
                  StLo = NormalizeDouble(OOP-MinProfitNoLoss*Point,Digits); 
                  if (StLo < OSL || OSL==0 && StLo >= NormalizeDouble(Ask + STOPLEVEL * Point,Digits)) SL = StLo;
               }
               
               if (TrailingStop>=STOPLEVEL && TrailingStop!=0 && (OOP - Ask)/Point >= TrailingStart)
               {
                  StLo = NormalizeDouble(Ask + TrailingStop*Point,Digits); 
                  if (StLo<=OOP && (StLo < OSL-StepTrall*Point || OSL==0)) SL = StLo;
               }
               
               if ((SL < OSL || OSL==0) && SL!=0)
               {  
                  if (!OrderModify(OrderTicket(),OOP,SL,TP,0,White)) Print("Error ",GetLastError(),"   Order Modify Sell   SL ",OSL,"->",SL);
                  else Print("Order Sell Modify   SL ",OSL,"->",SL);
               }
            } 
            if (OT==OP_BUYSTOP)  {PriceB=OOP; TicketB=OrderTicket();}     
            if (OT==OP_SELLSTOP) {PriceS=OOP; TicketS=OrderTicket();}  
         }
      }
   }
   if (b+TicketB==0)
   {
      if (Stoploss>=STOPLEVEL && Stoploss!=0) SL = NormalizeDouble(Bid - Stoploss * Point,Digits); else SL=0;
      if (Takeprofit>=STOPLEVEL && Takeprofit!=0) TP = NormalizeDouble(Ask + Takeprofit * Point,Digits); else TP=0;
      if (OrderSend(Symbol(),OP_BUYSTOP,Lot,NormalizeDouble(Ask+Step * Point,Digits),slippage,SL,TP,"news",Magic,0,CLR_NONE)!=-1) TimeBarB=TimeCurrent();
   } 
   if (s+TicketS==0)
   {
      if (Stoploss>=STOPLEVEL && Stoploss!=0) SL = NormalizeDouble(Ask + Stoploss * Point,Digits); else SL=0;
      if (Takeprofit>=STOPLEVEL && Takeprofit!=0) TP = NormalizeDouble(Bid - Takeprofit * Point,Digits); else TP=0;
      if (OrderSend(Symbol(),OP_SELLSTOP,Lot,NormalizeDouble(Bid - Step * Point,Digits),slippage,SL,TP,"news",Magic,0,CLR_NONE)!=-1) TimeBarS=TimeCurrent();
   } 
   if (TicketB!=0)
   {
      if (TimeBarB<TimeCurrent()-TimeModify && MathAbs(NormalizeDouble(Ask + Step * Point,Digits)-PriceB)/Point>StepTrall)
      {
         if (Stoploss>=STOPLEVEL && Stoploss!=0) SL = NormalizeDouble(Bid - Stoploss * Point,Digits); else SL=0;
         if (Takeprofit>=STOPLEVEL && Takeprofit!=0) TP = NormalizeDouble(Ask + Takeprofit * Point,Digits); else TP=0;
         if (OrderModify(TicketB,NormalizeDouble(Ask + Step * Point,Digits),SL,TP,0,CLR_NONE)) TimeBarB=TimeCurrent();
      }
   } 
   if (TicketS!=0)
   {
      if (TimeBarS<TimeCurrent()-TimeModify && MathAbs(NormalizeDouble(Bid - Step * Point,Digits)-PriceS)/Point>StepTrall)
      {
         if (Stoploss>=STOPLEVEL && Stoploss!=0) SL = NormalizeDouble(Ask + Stoploss * Point,Digits); else SL=0;
         if (Takeprofit>=STOPLEVEL && Takeprofit!=0) TP = NormalizeDouble(Bid - Takeprofit * Point,Digits); else TP=0;
         if (OrderModify(TicketS,NormalizeDouble(Bid - Step * Point,Digits),SL,TP,0,CLR_NONE)) TimeBarS=TimeCurrent();
      }
   } 
}
//--------------------------------------------------------------------

