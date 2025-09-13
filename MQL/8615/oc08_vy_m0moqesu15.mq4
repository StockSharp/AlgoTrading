

bool New_Bar=false;                // ���� ������ ����  
                                
extern double StopLoss   =100;     // SL ��� ������������ ������
extern double TakeProfit =100;     // �� ��� ������������ ������

extern double MAp         =12;     // MA ������
extern double MAs         =0;      // MA ��������
extern double MAm         =2;      // �� �����


extern double Lots       =0.1;     // ������ �������� ���������� �����
extern double Prots      =0.02;    // ������� ��������� �������


//--------------------------------------------------------------- 1 --
int start()
  {
   int
   Total,                           // ���������� ������� � ���� 
   Tip=-1,                          // ��� ������. ������ (B=0,S=1)
   Ticket;                          // ����� ������
   double
   MA,                              // ������. ��
   Lot,                             // �����. ����� � ������.������
   Lts,                             // �����. ����� � ������.������
   Min_Lot,                         // ����������� ���������� �����
   Step,                            // ��� ��������� ������� ����
   Free,                            // ������� ��������� ��������
   One_Lot,                         // ��������� ������ ����
   Price,                           // ���� ���������� ������
   SL,                              // SL ���������� ������ 
   TP;                              // TP ���������� ������
   bool
   Cls_B=false,                     // �������� ��� ��������  Buy
   Cls_S=false,                     // �������� ��� ��������  Sell
   Opn_B=false,                     // �������� ��� ��������  Buy
   Opn_S=false;                     // �������� ��� ��������  Sell
//--------------------------------------------------------------- 2 --
   // ���� �������
   
   for(int i=1; i<=OrdersTotal(); i++)          // ���� �������� �����
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // ���� ���� ���������
        {                                       // ������ �������:     
         Ticket=OrderTicket();                  // ����� �������. ���.
         Tip   =OrderType();                    // ��� ���������� ���.
         Price =OrderOpenPrice();               // ���� �������. ���.
         SL    =OrderStopLoss();                // SL ���������� ���.
         TP    =OrderTakeProfit();              // TP ���������� ���.
         Lot   =OrderLots();                    // ���������� �����
        }
     }
//--------------------------------------------------------------- 3 --
   // �������� ��������
   
   static datetime New_Time=0;                  // ����� �������� ����   
   New_Bar=false;                               // ������ ���� ���   
   if(New_Time!=Time[0])                        // ���������� �����     
   {       
   New_Time=Time[0];                            // ������ ����� �����      
   New_Bar=true;                                // �������� ����� ���     
   }
   
   MA=iMA(NULL,0,MAp,MAs,MAm,MODE_MAIN,0);      // ������ �� � ���������
   
   if (Close[0]>MA && New_Bar==true)            // ���� ���� �������� ������ �� � ����������� ����� ���...
     {                                          // 
      Opn_B=true;                               // ...�������� ����. Buy
      Cls_S=true;                               // �������� ����. Sell
     }
   if (Close[0]<MA && New_Bar==true)            // ���� ���� �������� ������ �� � ����������� ����� ���...
     {                                          // 
      Opn_S=true;                               // ...�������� ����. Sell
      Cls_B=true;                               // �������� ����. Buy
     }
//--------------------------------------------------------------- 4 --
   // �������� �������
   
   while(true)                                  // ���� �������� ���.
     {
      if (Tip==0 && Cls_B==true)                // ������ ����� Buy � ���� �������� ���� 
        {                                       // 
         OrderClose(Ticket,Lot,Bid,2);          // �������� Buy
         return;                                // ����� �� start()
        }

      if (Tip==1 && Cls_S==true)                // ������ ����� Sell � ���� �������� ���� 
        {                                       //        
         OrderClose(Ticket,Lot,Ask,2);          // �������� Sell
         return;                                // ����� �� start()
        }
      break;                                    // ����� �� while
     }
//--------------------------------------------------------------- 5 --
   // ��������� ������� (���� ��� ������ ��� "0")
   
   RefreshRates();                                // ���������� ������
   Min_Lot=MarketInfo(NULL,MODE_MINLOT);          // �����. �����. ����� 
   Free   =AccountFreeMargin();                   // ������� ��������
   One_Lot=MarketInfo(NULL,MODE_MARGINREQUIRED);  // ��������� 1 ����
   Step   =MarketInfo(NULL,MODE_LOTSTEP);         // ��� ������� �������

   if (Lots > 0)                                  // ���� ������ ����,�� 
      Lts =Lots;                                  // � ���� � �������� 
   else                                           // % ��������� �������
      Lts=MathFloor(Free*Prots/One_Lot/Step)*Step;// ��� ��������

   if(Lts < Min_Lot) Lts=Min_Lot;                 // �� ������ ���������
   if (Lts*One_Lot > Free)                        // ��� ������ �������.
     {
      Alert(" �� ������� ����� �� ", Lts," �����");
      return;                                     // ����� �� start()
     }
//--------------------------------------------------------------- 6 --
   // �������� �������
   
   while(true)                                             // ���� �������� ���.
     {
      if (OrdersTotal()==0 && Opn_B==true)                 // �������� ���. ��� +
        {                                                  // �������� ����. Buy
         SL=Bid - StopLoss*Point;                          // ���������� SL ����.
         TP=Bid + TakeProfit*Point;                        // ���������� TP ����.
         OrderSend(Symbol(),OP_BUY,Lts,Ask,2,SL,TP);       //�������� Buy
         return;                                           // ����� �� start()
        }
      
      if (OrdersTotal()==0 && Opn_S==true)                  // �������� ���. ��� +
        {                                                   // �������� ����. Sell
         SL=Ask + StopLoss*Point;                           // ���������� SL ����.
         TP=Ask - TakeProfit*Point;                         // ���������� TP ����.
         OrderSend(Symbol(),OP_SELL,Lts,Bid,2,SL,TP);       //�������� Sel
         return;                                            // ����� �� start()
        }
      break;                                                // ����� �� while
     }
//--------------------------------------------------------------- 9 --
   return;                                           // ����� �� start()
  }


