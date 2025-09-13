//+------------------------------------------------------------------+
//|                                        �� Alligator'� vol.1.1.mq4|
//|                      Copyright � 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, ������ ������� ����������."
#property link      "Vitalya_1983@list.ru"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

extern double MaxLot       = 0.5,         //������������ ������ ���������� ����
              koeff        = 1.3,         //����������� ���������� ����� ��� �����������
              risk         = 0.04,        //����, ������ �� ������ ���������� ����
              shirina1     = 0.0005,      //������ "����" Alligator'� �� �������� ������
              shirina2     = 0.0001;      //������ "����" Alligator'� �� �������� ������
              
extern bool Ruchnik = false,              //"������" ��������� ������. ���� ���� ��������� ��������
            Vhod_Alligator= true,         //��������� ��������� ������ ����������
            Vhod_Fractals = false,        //�������� ���������� ���������
            Vyhod_Alligator = false,      //��������� ��������� ������ ����������
            OnlyOneOrder = true,          //���� False, ���������� ������� ��� ������� �������
            EnableMartingail = true,      //����������
            Trailing = true;              //������������

extern int  TP              = 80,         //TP
            SL              = 80,         //SL  :)
            TrailingStop    = 30,         //���� �������
            profit          = 20,         //����������� ������� ��� ������������ ���������
            blue            = -8,         //��������� ����������         
            red             = -3,
            green           = 8,          
            Fractal_bars    = 10,         //���������� �����...
            visota_fractal  = 30,         //����� ������� ������ ��������������� �� ������ � ����������� �������
            spred           = 10,         //� �� ������ ����� �� ��. �������. � ����� ����� 10
            Koleno          = 10,         //���������� "�����" �����������
            Zaderzhka       = 2000;       //����� ����� ��������

bool Proverka_buy,Proverka_sell,Prodazha,Pokupka,Trailing_buy,Trailing_sell,Vihod_Alligator_sell,Vihod_Alligator_buy, Fractal;
int  seychas_buy=1,seychas_sell=1, bylo_buy,bylo_sell,i; 
string text ;
double Lot, Magic_number, up, down;
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int init ()                               //��������� ��� ����� ���������
   {
   text = "�� Alligator�:   ";
   if (Period() == 1)      string per = "M1  ";
   if (Period() == 5)      per = "M5  ";
   if (Period() == 15)     per = "M15  ";
   if (Period() == 30)     per = "M30  ";
   if (Period() == 60)     per = "H1  ";
   if (Period() == 240)    per = "H4  ";
   if (Period() == 1440)   per = "D1  ";
   text = text + Symbol()+"  "+per;
   if (Vhod_Alligator)
         {
         text = text + "��������� ���  ";
         }
      if (Vhod_Fractals)
         {
         text = text + "Fraktals ���  ";
         }
      if (!OnlyOneOrder)
         {
         text = text + "������� ���  ";
         }
      if (EnableMartingail)
         {
         text = text + "���������� ���  ";
         }
      if (Trailing)
         {
         text = text + "Trailing ���  ";
         }
      Alert (text);
return (0);   
   }
int deinit()
  {
  text="";
  }
return (0);   

int start()
   {
   
   RefreshRates();      
   bool order_est_buy = false;      //���������� ������� �������� �������
   int Magic_number = Period();     //���� ���������� ����� ��� �������� �� ������ ��
   int Orders = OrdersTotal();      //������� �������� �������
   if (Trailing==true)              
      {
      Trailing_start ();            //��������� �������� ����
      }
   if (Vhod_Alligator== true)       // ��������� ��������� ���������� ���������� ������ ����� � �����....
      {
      bylo_buy = seychas_buy;       //"������� ����� ��� �����" :)
      bylo_sell = seychas_sell;
      double blue_line=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_WEIGHTED, MODE_GATORJAW, blue);
      double red_line=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_WEIGHTED, MODE_GATORTEETH , red);
      double green_line=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_WEIGHTED, MODE_GATORLIPS , green);
      double  PriceHigh= High[0];
      if (green_line>blue_line+shirina1)        // ������� �����
      seychas_buy=1;                
      if (blue_line>green_line+shirina1)
      seychas_sell=1;
      if (green_line+shirina2<red_line)         // ������� ������
      seychas_buy=0;
      if (blue_line+shirina2<red_line)
      seychas_sell=0;
      }
   if (Vhod_Fractals)                           //��������� ���������
      {
      Fractal();           
      }
   if (!Proverka_buy ()||OnlyOneOrder == false) //���� �������� ������� ���...
      {
      for (i = OrdersTotal() ; i>=0; i--)       //������������...
         {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);  //���� �� � ��� ����������?
         if(OrderType() == OP_BUYLIMIT && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
            {
            OrderDelete(OrderTicket());         //��������� �� 
            RefreshRates ();
            Sleep (Zaderzhka);                  // ��������
            }
         }
      if (Ruchnik == false && (seychas_buy > bylo_buy||Vhod_Alligator== false))// ���� ��������� ��� ������� ��� �� ������...
         {
         if(up>0||Vhod_Fractals == false)       //� ������� ���� �����
            {       
            Pokupka();                          //��������� �������
            }
         }
      }
   if (!Proverka_sell()||OnlyOneOrder == false) //���� �����, ������ ��� ��������
      {
      for (i = OrdersTotal() ; i>=0; i--)
         {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if(OrderType() == OP_SELLLIMIT && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
            {
            OrderDelete(OrderTicket());
            RefreshRates ();
            Sleep (Zaderzhka);
            }
         }
      if (Ruchnik == false && (seychas_sell > bylo_sell||Vhod_Alligator== false))
         {
         if(down>0||Vhod_Fractals == false)
            {
            Prodazha();
            }
         }
      }
   if  (Vyhod_Alligator == true)          //���� ��������� ��� ������� �� ��������
      {
      if (seychas_buy < bylo_buy&&Vihod_Alligator_buy == true) 
         {
         for (i = 0 ; i<=Orders; i++)     //���� �������� ������ 
            {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_BUY && OrderSymbol() ==Symbol() &&OrderMagicNumber()==Magic_number)  
               OrderClose(OrderTicket() ,OrderLots(),Bid,3,Blue); //� ������ ��
            }
         }
      if (seychas_sell < bylo_sell&&Vihod_Alligator_sell == true)
         {
         Orders=OrdersTotal();
         for (i = 0 ; i<=Orders; i++)
            {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_SELL&& OrderSymbol() ==Symbol() &&OrderMagicNumber() == Magic_number)  
               OrderClose(OrderTicket() ,OrderLots(),Ask,3,Blue);
            }
         }
      }
   return(0);
   }

//+------------------------------------------------------------------+
// �������� ������� �������� �������
bool Proverka_buy()
   {
   bool Otkryt_orders_buy = false; //���� "�������� ������� ���"
   for (i = OrdersTotal() ; i>=0; i--)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      int Magic_number= Period();
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)
      Otkryt_orders_buy = true; //�������!!!
      }
   if (Otkryt_orders_buy==true)
      return(true);
   else 
      {
      Vihod_Alligator_buy = true;
      return (false);
      }
   }
//--------------------------

bool Proverka_sell()
   {
   bool Otkryt_orders_sell = false;
   for (i = OrdersTotal() ; i>=0; i--)
      {
      Magic_number= Period();
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
      Otkryt_orders_sell= true;
      }
   if (Otkryt_orders_sell==true)
      return(true);
   else 
      {
      Vihod_Alligator_sell = true;
      return (false);
      }
   }
    
    
    
    
    
    
// ����������� ������� �� �������
bool Prodazha()
   {
   Lot = AccountFreeMargin()/1000*risk; //����� ������� �� �����
   if (Lot >MaxLot) Lot = MaxLot;       //����������� ���
   if (Lot <0.01) Lot = 0.01;
   if (EnableMartingail == true) TP=SL/koeff; //���� ���������� ���, �� �� �� ������������
   Magic_number=Period();
   Sleep (Zaderzhka);
   RefreshRates ();
   if (TP == 0)
      OrderSend(Symbol(),OP_SELL,Lot,Bid,1,Ask + SL*Point,0,text,Magic_number);
   else
      OrderSend(Symbol(),OP_SELL,Lot,Bid,1,Ask + SL*Point,Bid - TP*Point,text,Magic_number);
   if (EnableMartingail == true)     //���� �� ���������� ����������
      {
      for (i=1; i<=Koleno; i++)
         {
         Lot = Lot*2*koeff; //����������� 1.5 ���� ���������� ���������� ���� � 3 ����
         Sleep (Zaderzhka);
         RefreshRates ();
         OrderSend(Symbol(),OP_SELLLIMIT,Lot,Ask+(SL*Point*(i)-spred*Point),1,Ask + SL*Point*(i+1),Ask+TP*Point*(i-1),text,Magic_number);
         }
      }
   }


// ����������� ������� �� �������
bool Pokupka()
   {
   double Lot = AccountFreeMargin()/1000*risk;
   if (Lot >MaxLot) Lot = MaxLot;
   if (Lot <0.01) Lot = 0.01;
   if (EnableMartingail == true) TP=SL/koeff;
   Magic_number=Period();
   Sleep (Zaderzhka);
   RefreshRates ();
   if (TP == 0)
      OrderSend(Symbol(),OP_BUY,Lot,Ask,1,Bid - SL*Point,0,text,Magic_number);
   else
      OrderSend(Symbol(),OP_BUY,Lot,Ask,1,Bid - SL*Point,Ask + TP*Point,text,Magic_number);
   if (EnableMartingail == true)
      {
      for (i=1; i<=Koleno; i++)
         {
         Lot = Lot*2*koeff;
         Sleep (Zaderzhka);
         RefreshRates ();
         OrderSend(Symbol(),OP_BUYLIMIT,Lot,Bid - (SL*Point*(i)-spred*Point),1,Bid - SL*Point*(i+1),Bid - TP*Point*(i-1),text,Magic_number);
         
         }
      }
   }


int Trailing_start ()
   {
   for (i = OrdersTotal() ; i>=0; i--) //���� �������� ������
      {
      RefreshRates();
      Magic_number= Period();
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
         {
         if (TP==0)
            double TrailingStopKoeff  = TrailingStop;
         else
            {
            //����������� ��������� ���� ������ ���������� SL � TP ��� ����������� ���� � TP
            TrailingStopKoeff = (OrderTakeProfit() - OrderOpenPrice())/(OrderTakeProfit () - Bid)*TrailingStop;
            //���� �� ������ ����������� � ����������, �� �� ���������� ��� ������
            if (TrailingStopKoeff >TrailingStop) TrailingStopKoeff  = TrailingStop; 
            }
         
         //���� SL ������ ���������, �� SL = ���������
         if(Bid-OrderOpenPrice()>profit&&Bid>Point*TrailingStopKoeff +OrderOpenPrice()&&OrderStopLoss()<Bid-Point*TrailingStopKoeff )
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStopKoeff,OrderTakeProfit(),text,Blue);
            Sleep (Zaderzhka);
            Vihod_Alligator_buy = false;
            }
         }
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
         {
         
         TrailingStopKoeff = (OrderOpenPrice()-OrderTakeProfit())/(Ask - OrderTakeProfit ())*TrailingStop;
         if (TrailingStopKoeff >TrailingStop) TrailingStopKoeff  = TrailingStop;
         if(OrderOpenPrice()-Ask>profit&&OrderOpenPrice()-Ask>Point*TrailingStopKoeff&&OrderStopLoss()>Ask+Point*TrailingStopKoeff )
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStopKoeff,OrderTakeProfit(),text,Blue);
            Sleep (Zaderzhka);
            Vihod_Alligator_sell = false;
            }
         }
      }
   return (0);
   }
   
   
double Fractal ()
   {
   up =0;         //����� ��������
   down = 0;
   for (i=Fractal_bars;i>=3;i--)
      {
      double up_prov =iFractals(NULL, 0, MODE_UPPER, i); //� ����� ����� ������ ����
      if (up_prov >up)                                   //�������
         up = up_prov;
      double down_prov=iFractals(NULL, 0, MODE_LOWER, i);
      if (down_prov>down)
         down = down_prov;
      }
   if (up<Ask+visota_fractal*Point)                      //� ���� ������� ������ ���� �������
   up = 0;
   if (down<Bid-visota_fractal*Point)
   down = 0;
   }