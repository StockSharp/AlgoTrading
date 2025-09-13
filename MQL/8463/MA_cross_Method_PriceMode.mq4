//============================================================================================
//
//
//
//
//
//============================================================================================
extern int    MA1_Period=3;                              // ������ 1-� ��
extern int    MA2_Period=13;                             // ������ 2-� ��
extern int    MA1_Method=0;                              // ����� ���������� ��1 (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern int    MA2_Method=3;                              // ����� ���������� ��2 (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern int    MA1_Price=0;                               // ����� ���������� ���� ��1 
extern int    MA2_Price=4;                               // ����� ���������� ���� ��2
extern int    MA1_Shift=0;                               // ��������� ����� ��1
extern int    MA2_Shift=0;                               // ��������� ����� ��2
extern double Lot = 0.1;                                 // ������������� ���
extern int    slippage = 0;                              // ���������� ���� ��� �������� ������� 
int New_Bar;                                             // 0/1 ���� ����������� ������ ����
int Time_0;                                              // ����� ������ ������ ����
int PosOpen;                                             // ����������� �����������
int PosClose;                                            // ����������� �����������
int total;                                               // ���������� �������� �������
double MA1_0;                                            // ������� �������� 1-� �� (�����)
double MA1_1;                                            // ���������� �������� 1-� �� (�����)
double MA2_0;                                            // ������� �������� 2-� �� (�������)
double MA2_1;                                            // ���������� �������� 2-� �� (�������)
int orderBuy;                                            // 1 = ���� ������ ������ Buy
int orderSell;                                           // 1 = ���� ������ ������ Sell 
//============================================================================================
int init()  
   {

   }  
//============================================================================================
int start()  
   {
   orderBuy=0;
   orderSell=0; 
   double price;  
   int openOrders=0;
   int total=OrdersTotal();                                  // ����� ���������� �������
   for(int i=total-1;i>=0;i--)
      {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)     // �������� �����
         {
         if(OrderType()==OP_BUY)                             // ���� ����� ����� �� �������
            {
            orderBuy=1;
            if(CrossPositionClose()==1)                      // �������� �����, ���� �������������
               {                                             // ������� CrossPositionClose()=1
               price=MarketInfo(Symbol(),MODE_BID);
               OrderClose(OrderTicket(),OrderLots(),price,slippage,CLR_NONE);
               }
            }
         if(OrderType()==OP_SELL)                            // ���� ����� ����� �� �������
            {
            orderSell=1;
            if(CrossPositionClose()==2)                      // �������� �����, ���� �������������
               {                                             // ������� CrossPositionClose()=2
               price=MarketInfo(Symbol(),MODE_ASK);
               OrderClose(OrderTicket(),OrderLots(),price,slippage,CLR_NONE);
               }
            }
         }
      }
   
   New_Bar=0;                                                // ��� ������ ���������
   if (Time_0 != Time[0])                                    // ���� ��� ������ ����� ������ ����
      {
      New_Bar= 1;                                            // � ��� � ����� ���
      Time_0 = Time[0];                                      // �������� ����� ������ ������ ����
      } 
   
   MA1_0=iMA(NULL,0, MA1_Period, MA1_Shift,MAMethod(MA1_Method), MAPrice(MA1_Price), 0);    // �������    �������� 1-� ��
   MA1_1=iMA(NULL,0, MA1_Period, MA1_Shift,MAMethod(MA1_Method), MAPrice(MA1_Price), 1);    // ���������� �������� 1-� ��
   MA2_0=iMA(NULL,0, MA2_Period, MA2_Shift,MAMethod(MA2_Method), MAPrice(MA2_Price), 0);    // �������    �������� 2-� ��
   MA2_1=iMA(NULL,0, MA2_Period, MA2_Shift,MAMethod(MA2_Method), MAPrice(MA2_Price), 1);    // ���������� �������� 2-� ��
   
   if (CrossPositionOpen()==1 && New_Bar==1)                 // �������� ����� ����� = ����. Buy
      {
      OpenBuy();
      }      
   if (CrossPositionOpen()==2 && New_Bar==1)                 // �������� ������ ���� = ����. Sell
      {
      OpenSell();
      }    
   return;
   }  
//============================================================================================
int CrossPositionOpen()
   {
   PosOpen=0;                                                 // ��� ��� ������ ������!!:)
   if ((MA1_1<=MA2_0 && MA1_0>MA2_0) || (MA1_1<MA2_0 && MA1_0>=MA2_0))   // ����������� ����� �����  
      {
      PosOpen=1;
      }                  
   if ((MA1_1>=MA2_0 && MA1_0<MA2_0) || (MA1_1>MA2_0 && MA1_0<=MA2_0))   // ����������� ������ ����
      {
      PosOpen=2;
      }             
   return(PosOpen);                                          // ���������� ����������� ���������.
   }
//============================================================================================
int CrossPositionClose()
   {
   PosClose=0;                                                // ��� ��� ������ ������!!:)
   if ((MA1_1>=MA2_0 && MA1_0<MA2_0) || (MA1_1>MA2_0 && MA1_0<=MA2_0))   // ����������� ������ ����        {
      {
      PosClose=1;
      }                  
   if ((MA1_1<=MA2_0 && MA1_0>MA2_0) || (MA1_1<MA2_0 && MA1_0>=MA2_0))   // ����������� ����� �����
      {
      PosClose=2;
      }             
   return(PosClose);                                          // ���������� ����������� ���������.
   }
//============================================================================================
int OpenBuy() 
   {
   if (total==1)
      {
      OrderSelect(0, SELECT_BY_POS,MODE_TRADES);              // ������� �����
      if (OrderType()==OP_BUY) return;                        // ���� �� buy, �� �� �����������
      }
   OrderSend(Symbol(),OP_BUY, Lot, Ask, slippage, 0, 0, "Buy: MA_cross_Method_PriceMode", 1, 0, CLR_NONE);// �����������
   return;
   }
//============================================================================================
int OpenSell() 
   {
   if (total==1)
      {
      OrderSelect(0, SELECT_BY_POS,MODE_TRADES);              // ������� �����
      if (OrderType()==OP_SELL) return;                       // ���� �� sell, �� �� �����������
      }
   OrderSend(Symbol(),OP_SELL, Lot, Bid, slippage, 0, 0, "Sell: MA_cross_Method_PriceMode", 2, 0, CLR_NONE);
   return;
   }
//============================================================================================
int MAMethod(int MA_Method)
   {
      switch(MA_Method)
        {
         case 0: return(0);                                   // ���������� MODE_SMA=0
         case 1: return(1);                                   // ���������� MODE_EMA=1
         case 2: return(2);                                   // ���������� MODE_SMMA=2
         case 3: return(3);                                   // ���������� MODE_LWMA=3
        }
   }
//============================================================================================
int MAPrice(int MA_Price)
   {
      switch(MA_Price)
        {
         case 0: return(PRICE_CLOSE);                         // ���������� PRICE_CLOSE=0        
         case 1: return(PRICE_OPEN);                          // ���������� PRICE_OPEN=1
         case 2: return(PRICE_HIGH);                          // ���������� PRICE_HIGH=2
         case 3: return(PRICE_LOW);                           // ���������� PRICE_LOW=3
         case 4: return(PRICE_MEDIAN);                        // ���������� PRICE_MEDIAN=4
         case 5: return(PRICE_TYPICAL);                       // ���������� PRICE_TYPICAL=5
         case 6: return(PRICE_WEIGHTED);                      // ���������� PRICE_WEIGHTED=6
        }
   }
//============================================================================================

