//+------------------------------------------------------------------+
//|                                                     e-Friday.mq4 |
//|                                           ��� ����� �. aka KimIV |
//|                                              http://www.kimiv.ru |
//|                                                                  |
//| 08.10.2005  ������ �������                                       |
//+------------------------------------------------------------------+
#property copyright "��� ����� �. aka KimIV"
#property link      "http://www.kimiv.ru"
//-------
#define   MAGIC     20051008
//------- ������� ��������� ��������� --------------------------------
extern string _Parameters_Trade="----- ��������� ��������";
extern double Lots           =0.1;    // ������ ���������� ����
extern int    StopLoss       =75;     // ������ �������������� �����
extern int    TakeProfit     =0;      // ������ �������������� �����
extern int    HourOpenPos    =7;      // ����� �������� �������
extern bool   UseClosePos    =True;   // ������������ �������� �������
extern int    HourClosePos   =19;     // ����� �������� �������
extern bool   UseTrailing    =True;   // ������������ ����
extern bool   ProfitTrailing =True;   // ������� ������ ������
extern int    TrailingStop   =60;     // ������������� ������ �����
extern int    TrailingStep   =5;      // ��� �����
extern int    Slippage       =3;      // ��������������� ����
//----
extern string _Parameters_Expert="----- ��������� ���������";
extern bool   UseOneAccount=False;        // ��������� ������ �� ����� �����
extern int    NumberAccount=11111;        // ����� ��������� �����
extern string Name_Expert  ="e-Friday.mq4";
extern bool   UseSound     =True;         // ������������ �������� ������
extern string NameFileSound="expert.wav"; // ������������ ��������� �����
extern color  clOpenBuy    =LightBlue;    // ���� �������� �������
extern color  clOpenSell   =LightCoral;   // ���� �������� �������
extern color  clModifyBuy  =Aqua;         // ���� ����������� �������
extern color  clModifySell =Tomato;       // ���� ����������� �������
extern color  clCloseBuy   =Blue;         // ���� �������� �������
extern color  clCloseSell  =Red;          // ���� �������� �������
//---- ���������� ���������� ��������� -------------------------------
//------- ����������� ������� ������� --------------------------------
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
  void deinit() 
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
  void start() 
  {
     if (UseOneAccount && AccountNumber()!=NumberAccount) 
     {
      if (!IsTesting()) Comment("�������� �� �����: "+AccountNumber()+" ���������!");
      return;
     }
      else if (!IsTesting()) Comment("");
      if (DayOfWeek()!=5 || Hour()<HourOpenPos || Hour()>HourClosePos) 
      {
      if (!IsTesting()) Comment("����� �������� ��� �� ���������!");
      return;
     }
      else if (!IsTesting()) Comment("");
   if (Hour()==HourOpenPos) OpenPosition();
   if (Hour()>=HourClosePos && UseClosePos) CloseAllPositions();
   if (UseTrailing) TrailingPositions();
  }
//+------------------------------------------------------------------+
//| ��������� �������                                                |
//+------------------------------------------------------------------+
  void OpenPosition() 
  {
   double ldStop=0, ldTake=0;
   double Op1=iOpen (NULL, PERIOD_D1, 1);
   double Cl1=iClose(NULL, PERIOD_D1, 1);
//----
     if (!ExistPosition()) 
     {
        if (Op1>Cl1) 
        {
         if (StopLoss!=0) ldStop=Ask-StopLoss*Point;
         if (TakeProfit!=0) ldTake=Ask+TakeProfit*Point;
         SetOrder(OP_BUY, Ask, ldStop, ldTake);
        }
        if (Op1<Cl1) 
        {
         if (StopLoss!=0) ldStop=Bid+StopLoss*Point;
         if (TakeProfit!=0) ldTake=Bid-TakeProfit*Point;
         SetOrder(OP_SELL, Bid, ldStop, ldTake);
        }
     }
  }
//+------------------------------------------------------------------+
//| ���������� ���� ������������� �������                            |
//+------------------------------------------------------------------+
  bool ExistPosition() 
  {
   bool Exist=False;
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
           {
              if (OrderType()==OP_BUY || OrderType()==OP_SELL) 
              {
               Exist=True; break;
              }
           }
        }
     }
   return(Exist);
  }
//+------------------------------------------------------------------+
//| ��������� ������                                                 |
//| ���������:                                                       |
//|   op     - ��������                                              |
//|   pp     - ����                                                  |
//|   ldStop - ������� ����                                          |
//|   ldTake - ������� ����                                          |
//+------------------------------------------------------------------+
  void SetOrder(int op, double pp, double ldStop, double ldTake) 
  {
   color  clOpen;
   string lsComm=GetCommentForOrder();
//----
   if (op==OP_BUYLIMIT || op==OP_BUYSTOP) clOpen=clOpenBuy;
   else clOpen=clOpenSell;
   //  Lots=MathCeil(AccountFreeMargin()/10000*10)/10;
   OrderSend(Symbol(),op,Lots,pp,Slippage,ldStop,ldTake,lsComm,MAGIC,0,clOpen);
   if (UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+
//| ���������� � ���������� ������ ���������� ��� ������ ��� ������� |
//+------------------------------------------------------------------+
  string GetCommentForOrder() 
  {
   return(Name_Expert);
  }
//+------------------------------------------------------------------+
//| �������� ���� ������� �� �������� ����                           |
//+------------------------------------------------------------------+
  void CloseAllPositions() 
  {
   bool fc;
     for(int i=OrdersTotal()-1; i>=0; i--) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
           {
            fc=False;
              if (OrderType()==OP_BUY) 
              {
               fc=OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, clCloseBuy);
              }
              if (OrderType()==OP_SELL) 
              {
               fc=OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, clCloseSell);
              }
            if (fc && UseSound) PlaySound(NameFileSound);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| ������������� ������� ������� ������                             |
//+------------------------------------------------------------------+
  void TrailingPositions() 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
           {
              if (OrderType()==OP_BUY) 
              {
                 if (!ProfitTrailing || (Bid-OrderOpenPrice())>TrailingStop*Point) 
                 {
                    if (OrderStopLoss()<Bid-(TrailingStop+TrailingStep-1)*Point) 
                    {
                     ModifyStopLoss(Bid-TrailingStop*Point, clModifyBuy);
                    }
                 }
              }
              if (OrderType()==OP_SELL) 
              {
                 if (!ProfitTrailing || OrderOpenPrice()-Ask>TrailingStop*Point) 
                 {
                    if (OrderStopLoss()>Ask+(TrailingStop+TrailingStep-1)*Point || OrderStopLoss()==0) 
                    {
                     ModifyStopLoss(Ask+TrailingStop*Point, clModifySell);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| ������� ������ StopLoss                                          |
//| ���������:                                                       |
//|   ldStopLoss - ������� StopLoss                                  |
//|   clModify   - ���� �����������                                  |
//+------------------------------------------------------------------+
  void ModifyStopLoss(double ldStop, color clModify) 
  {
   bool   fm;
   double ldOpen=OrderOpenPrice();
   double ldTake=OrderTakeProfit();
//----
   fm=OrderModify(OrderTicket(), ldOpen, ldStop, ldTake, 0, clModify);
   if (fm && UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+

