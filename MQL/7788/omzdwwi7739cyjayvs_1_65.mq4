//+------------------------------------------------------------------+
//|                                               ���������_1.64.mq4 |
//|                                                            Drknn |
//|                   02.03.2007                       drknn@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Drknn"
#property link      "drknn@mail.ru"
//+-------------------------------------------------------------------------------------------------+
//|   �������� �����:                                                                               |
//| - ������� ������ ������ ���� (��� ��������, ��� � ����������)                                   |  
//| - ���������                                                                                     |
//| - ������ ���������� �������� �� ������ ���������� ��������� �, ������ ���                       |
//|   �������� ������������, ��� ������� �� �������� ���������� ��������� ��������.                 |
//| - ������������� �� ���������� ������, ������� �������� ������������, ���� �������� �����        |
//|   ���������, ��� ������ ������ � ����� ��� (��������, ����� �������� - ���� ��������)           |
//| - ���������� �� ������ ��� � ��� � ���� ��������, ���� ������ ��� �� ������ � ���������         |
//|                                                                                                 |
//| - �������� ����������� ��� ������������� ��������, � �� ��������� ��������� ���� ��� ��������. |
//| - �������� ������� ����� ����� ��������� � ���� ������� ���������.                              |
//| - ���� ������������ �������� ��������� ������������� ���������� ����� ������ ���� ����,         |
//|   �� ���� ����� ����� ���������� �� ������� ���� �� ���������� _Step ��� ������� ������� ����.  |
//+-------------------------------------------------------------------------------------------------+
// ================ ���������, ������������� ������������� ===========================
// ----- ���������, ����� ��� ���� ������� --------------------------------------------
extern string   t1="------ � �������� ������� MAGIC = 0 ------";
extern int      MAGIC=0;              //� ������� �������� ������� MAGIC=0 
extern double   Lot=0.2;              //��� ��� ��������� ������
// ----- ���������� ���������� ��� ��� ���� ����� ------------------------------------- 
extern string   t2="--- ����������� ���������� ---";
extern bool     WaitClose=true;       //���� true, �� ���������� ������� ������ �����, ����� ��������� ��������
                                      //����� ����� ����� ��������� ��� ������ ���������� ���������
extern bool     Ustan_BuyStop=true;   //����� �� ������ ���-C��� ���� � ����� ������ � ��� ���
extern bool     Ustan_SellLimit=false;//����� �� ������ C���-����� ���� � ����� ������ � ��� ���
extern bool     Ustan_SellStop=true;  //����� �� ������ C���-C���� ���� � ����� ������ � ��� ���
extern bool     Ustan_BuyLimit=false; //����� �� ������ ���-����� ���� � ����� ������ � ��� ���
// ----- ��������� �������� ������� ---------------------------------------------
extern string   t3="--- ��������� �������� ---";
extern int      ryn_MaxOrderov=2;     //����������� ������������� ����� �������� ������� ������ ����
extern int      ryn_TakeProfit=200;   //����-������  ��������� ������ 
extern int      ryn_StopLoss=100;     //����-����  ��������� ������
extern int      ryn_TrStop=100;       //��������-���� ��������� ������. ���� = 0 �� ����� ���
extern int      ryn_TrStep=10;        //��� ����� ��������� ������
extern bool     WaitProfit=true;      // ���� true, �� ����� ������ = �������� TrailingStop � ������ ����� �������� �������
                                      //�����, ������������� �� ��������� �������������� �������
// ----- ��������� �������� ������� ---------------------------------------------
extern string   t4="--- ��������� �������� ---";
extern int      st_Step=50;           //���������� � ������� �� ������ ������� ���� �� ������ ��������� ��������� ������
extern int      st_TakeProfit=200;    //����-������  �������� �������
extern int      st_StopLoss=100;      //����-����  �������� �������
extern int      st_TrStop=0;          //��������-���� �������� �������. ���� = 0 �� ����� ��� � st_TrStep �� �����
extern int      st_TrStep=3;          //��� ����� �������� �������
// ----- ��������� �������� ������� ---------------------------------------------
extern string   t5="--- ��������� �������� ---";
extern int      lim_Step=50;          //���������� � ������� �� ������ ������� ���� �� ������ ��������� ��������� ������
extern int      lim_TakeProfit=200;   //����-������ �������� ������� 
extern int      lim_StopLoss=100;     //����-���� �������� �������
extern int      lim_TrStop=0;         //��������-���� �������� �������. ���� = 0 �� ����� ��� � lim_TrStep �� �����
extern int      lim_TrStep=3;         //��� ��������� �������� �������
//------ ������� (���������) ������ � �������� ����� ----------------------------------------------------------
extern string   t6="--- ������ ��� ������ �� ������� ---";
extern bool     UseTime=true;         //���/���� �������� � ��������� �������. ����=false, �� ��� �������� ��������� �� �����, 
                                      //� ������, ���������: Hhour, Mminute, Popravka_Hhour, TIME_Buy, TIME_Sell, TIME_BuyStop,
                                      //TIME_SellLimit, TIME_SellStop, TIME_BuyLimit.
extern int      Hhour=23;             //������������ ���� ���������� ������
extern int      Mminute=59;           //������������ ������ ���������� ������
extern bool     TIME_Buy=false;       //���/���� ����������� �� �� ������� � �������� �����
extern bool     TIME_Sell=false;      //���/���� ����������� �� �� �������
extern bool     TIME_BuyStop=true;    //����� �� ���������� ���C��� � �������� �����
extern bool     TIME_SellLimit=false; //����� �� ���������� C�������� � �������� �����
extern bool     TIME_SellStop=true;   //����� �� ���������� C���C���� � �������� �����
extern bool     TIME_BuyLimit=false;  //����� �� ���������� �������� � �������� �����
// ----- ��������� �������� ----------------------------------------------------
extern string   t7="--- �������� ---";
extern int      PipsProfit=0;          //������ ��� �������� ����� ������� 1, 2, 3, ...
extern int      Proskalz=3;            //��������������� � ������� (����� ������ ����� PipsProfit>0)
// -----------   ����������� ���������� �������� --------------------------
extern string   t8="--- ���������� ������ ---";
extern bool     UseGlobalLevels=true;  //�������� ���������� � ���������� �������� �������� ����� ���������
                                       //���� UseGlobalLevels=false, �� �������t Global_TakeProfit � Global_StopLoss �� �����.
extern double   Global_TakeProfit=2.0; //���������� ����-������ (������� � ���������)
extern double   Global_StopLoss=2.0;   //���������� ����-���� (������� � ���������)
// ----- ������ ��������� ------------------------------------------------------ 
extern string   t9="--- ������ ��������� ---";
extern bool     UseOrderSound=true;             // ������������ �������� ������ ��� ��������� �������
extern bool     UseTrailingSound=true;          // ������������ �������� ������ ��� ���������
extern string   NameOrderSound ="alert.wav";    // ������������ ��������� ����� ��� �������
extern string   NameTrallingSound ="expert.wav";// ������������ ��������� ����� ��� ���������
// ================== ���������� ���������� ===============================================================  
string     Comm1,Comm2,Comm3,Comm4,Comm5,Comm6,Comm7,ED,SMB;
double     PNT,NewPrice,SL,TP,Balans,Free;
int        MinLevel,i,SchBuyStop,SchSellStop,SchBuyLimit,SchSellLimit,SchSell,SchBuy,SBid,SAsk,BBid,BAsk,GTP,GSL,GLE,total;
bool       fm,Rezult,TrailBuyStop,TrailSellStop,TrailBuyLimit,TrailSellLimit,SigBuy,SigSell,NewOrder;
bool       SigTIME_Buy,SigTIME_Sell,SigTIME_BuyStop,SigTIME_SellLimit,SigTIME_SellStop,SigTIME_BuyLimit;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   if(!IsExpertEnabled())//���� ���� 
   {
      Alert("������! �� ������ ������ *���������*"); Comment("������! �� ������ ������ *���������*"); return(0);
   }
   else 
   {
      Comment("��� ������ ���� ���������, �������� ����� ������."); Print("��� ������ ���� ���������, �������� ����� ������.");
   }
   SMB=Symbol();//������ �������� ����
   PNT=MarketInfo(SMB,MODE_POINT);//������ ������ � ������ ���������. ��� �������� ����������� �������� � ���������������� ���������� Point
   MinLevel=MarketInfo(SMB,MODE_STOPLEVEL);//���������� ���������� ������� ����-�����/����-������� � �������
   Proverka();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   RefreshRates();
//----��������� �������� ������
   if(!IsTradeAllowed())
     {
      Comment("�������� ��������� � ���������� ���������, ���� �������� ����� �����");
      return(0);
     }
   Proverka();
//----����� ��������� �������� ������
   if(ryn_TrStop>0 && ryn_TrStop>=MinLevel) Comm1="���� �������� - ���."; else Comm1="���� �������� - ����.";
   if (lim_TrStop>0 && lim_TrStop>=MinLevel) Comm2="���� �������� - ���."; else Comm2="���� �������� - ����.";
   if (st_TrStop>0 && st_TrStop>=MinLevel) Comm3="���� �������� - ���."; else Comm3="���� �������� - ����.";
   if (PipsProfit>0) Comm4="�������� - ���"; else Comm4="�������� - ����";
   double OtlTP=(Balans/100*Global_TakeProfit+Balans);
   double OtlSL=(Balans-Balans/100*Global_StopLoss);
   GTP=MathCeil(OtlTP);
   GSL=MathCeil(OtlSL);
   if (UseGlobalLevels)
     {
      Comm5="- - - - ���������� ������ - - - -";
      Comm6="���������� ����-������ = "+GTP+" $";
      Comm7="���������� ����-����   = "+GSL+" $";
     }
   else 
   {
      Comm5="���������� ������ - ����"; Comm6=""; Comm7="";
   }
   SchOrders();
   SMB=Symbol();
   Comment("������� ������ ��� ",SMB," :","\n","Buy = ",SchBuy,"       Sell = ",SchSell,"\n","BuyStop = ",SchBuyStop,
          "   SellLimit = ",SchSellLimit,"\n","SellStop = ",SchSellStop,"    BuyLimit = ",SchBuyLimit,"\n",Comm1,
          "\n",Comm2,"\n",Comm3,"\n",Comm4,"\n",Comm5,"\n",Comm6,"\n",Comm7);
   // ========================== ��������� ���������� ������� ====================================
   if(Ustan_BuyStop || Ustan_SellLimit || Ustan_SellStop || Ustan_BuyLimit) {UstanOtlozh();}
   //=============================================================================================
   //==========================  ������ �� ��������� �������   =======================
   if(UseTime)
     {
      SigTIME_Buy=false;  SigTIME_BuyStop=false;   SigTIME_SellStop=false;
      SigTIME_Sell=false; SigTIME_SellLimit=false; SigTIME_BuyLimit=false;
      if(Hour()==Hhour && Minute()==Mminute)//���� ������� ��� � ������ ���������
        {
         if(TIME_Buy)
         {SigTIME_Buy=true; UstanRyn();}
         if(TIME_Sell)
         {SigTIME_Sell=true; UstanRyn();}
         if(TIME_BuyStop)
         {SigTIME_BuyStop=true; UstanOtlozh();}
         if(TIME_SellLimit)
         {SigTIME_SellLimit=true; UstanOtlozh();}
         if(TIME_SellStop)
         {SigTIME_SellStop=true; UstanOtlozh();}
         if(TIME_BuyLimit)
         {SigTIME_BuyLimit=true; UstanOtlozh();}
        }
     }
   //============== ����������� ���������� �������� �� Pojmat ��������� =========================
   if(UseGlobalLevels)//���� ��������� �������� ������� ����������/���������� ��������
     {
      Balans=AccountBalance();//������ �����
      Free=AccountEquity();//������� ���������� ����� � ������ "��������"
      if ((Free-Balans)>=(Balans/100*Global_TakeProfit))
        {
         Print("������� �������� �� ",Global_TakeProfit," ���������. ��������� ������ = ",Free);
         Alert("������� �������� �� ",Global_TakeProfit," ���������. ��������� ������ = ",Free);
        }
      if ((Balans-Free)>=(Balans/100*Global_StopLoss))
        {
         Print("������� �������� �� ",Global_StopLoss," ���������. ��������� ����-���� = ",Free);
         Alert("������� �������� �� ",Global_StopLoss," ���������. ��������� ����-���� = ",Free);
        }
     }
   //=================������ ��������==========================
   if (PipsProfit>0)
     {
      SMB=Symbol();
      for(int i=OrdersTotal()-1; i>=0; i-- )
        {//������ �����
         if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
           {//������ ������ � ��������� �������
            if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) continue;
            if(OrderType()==OP_BUY)
              {
               if(Bid>=(OrderOpenPrice()+PipsProfit*Point))
               {OrderClose(OrderTicket(),OrderLots(),Bid,Proskalz);}
              }
            if(OrderType()==OP_SELL)
              {
               if(Ask<=(OrderOpenPrice() - PipsProfit*Point))
                  OrderClose(OrderTicket(),OrderLots(),Ask,Proskalz);
              }
           } // ����� ������ � ��������� �������  
        }// ����� �����
     }
   //================����� �������� ======================================
   // ================= �������� �������� ������� ==================================================================================
   RefreshRates(); SchOrders();  SMB=Symbol();
   if(ryn_TrStop>=MinLevel && ryn_TrStep>0 && (SchBuy>0 || SchSell>0))
     {
      for(i=0; i<OrdersTotal(); i++)
        {
         if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
           {
            if (OrderSymbol()==SMB && OrderMagicNumber()==MAGIC)
              {
               TrailingPositions();
              }
           }
        }
     }
   if(ryn_TrStop>=MinLevel && ryn_TrStep==0)
      Alert("�������� ���������� - ryn_TrStep==0");
   // ===============================================================================================================================
   //============ �������� ���������� ������� =============================================================
   RefreshRates();  SchOrders();//��������� �������� ���������� �������
   SMB=Symbol();
   if((st_TrStop>0 && SchBuyStop+SchSellStop>0) || (SchBuyLimit+SchSellLimit>0 && lim_TrStop>0))
     {
      TrailBuyStop=false; TrailSellStop=false; TrailBuyLimit=false; TrailSellLimit=false;
//----
      for(i=OrdersTotal()-1;i>=0;i--)
        {//������ �����
         if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
           {//������ ������ � ��������� �������
            if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC || OrderType()==OP_BUY || OrderType()==OP_SELL) continue;
            if(OrderType()==OP_BUYSTOP) // �� ������� � ���� ����
              {
               if(Ask<OrderOpenPrice()-(st_TrStop+st_TrStep)*Point)
                  TrailBuyStop=true;
              }
            if(OrderType()==OP_SELLLIMIT) // �� ������� � ���� ����
              {
               if(Bid<OrderOpenPrice()-(st_TrStop+st_TrStep)*Point)
                  TrailSellLimit=true;
              }
            if(OrderType()==OP_SELLSTOP) // �� ����� � ���� �����
              {
               if(Bid>OrderOpenPrice()+(st_TrStop+st_TrStep)*Point)
                  TrailSellStop=true;
              }
            if(OrderType()==OP_BUYLIMIT) // �� ����� � ���� �����
              {
               if(Ask>OrderOpenPrice()+(st_TrStop+st_TrStep)*Point)
                  TrailBuyLimit=true;
              }
           }//����� ������ � ��������� ������� 
        }//����� �����
      if (TrailSellLimit || TrailBuyLimit || TrailSellStop || TrailBuyStop)   TrailingOtlozh();
     }
//----
   return(0);
  }
//+ ========================== ����� ������ ��������� ========================================================================== +
// ___________________________________________________________________________________________
//|                                                                                           |
//|                                                                                           |
//|                       ����� ���� ������������ (�������),                                  |
//|                  ������� � ������ ���������� ���������� �� ���� ���������                 |
//|                                                                                           |
//|___________________________________________________________________________________________|

//===================== ��������� ���������� ������� ===========================================================================
// ������� UstanOtlozh() ������������� ������ ���������� ����� 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UstanOtlozh()
  {
   RefreshRates(); SchOrders(); SMB=Symbol();
   if(
      (SchSellStop==0 && (SchSell<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_SellStop && st_Step>=MinLevel) || (SigTIME_SellStop && st_Step>=MinLevel))
     )
     {
      NewPrice=Bid-st_Step*Point;
      if(st_StopLoss==0) SL=0.0000;
      else SL=NewPrice+st_StopLoss*Point;
      if(st_TakeProfit==0) TP=0.0000;
      else TP=NewPrice-st_TakeProfit*Point;
      fm=OrderSend(SMB,OP_SELLSTOP,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*��������*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*������������*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_SellStop=false;
         Print("����� SellStop ����������");
         Comment("����� SellStop ����������");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//���� ���������� �� �������
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("������ � ",GLE," ��������� SellStop-������");
         Print ("�������� ������ - ",ED);
        }
     }
   if(
      (SchBuyStop==0 && (SchBuy<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_BuyStop && st_Step>=MinLevel) || (SigTIME_BuyStop && st_Step>=MinLevel))
     )
     {
      NewPrice=Ask+st_Step*Point;
      if(st_StopLoss==0) SL=0.0000;
      else SL=NewPrice-st_StopLoss*Point;
      if(st_TakeProfit==0) TP=0.0000;
      else TP=NewPrice+st_TakeProfit*Point;
      fm=OrderSend(SMB,OP_BUYSTOP,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*��������*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*������������*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_BuyStop=false;
         Print("����� BuyStop ����������");
         Comment("����� BuyStop ����������");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//���� ���������� �� �������
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("������ � ",GLE," ��������� BuyStop-������");
         Print ("�������� ������ - ",ED);
        }
     }
   if(
      (SchBuyLimit==0 && (SchBuy<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_BuyLimit && lim_Step>=MinLevel) || (SigTIME_BuyLimit && lim_Step>=MinLevel))
     )
     {
      NewPrice=Ask-lim_Step*Point;
      if(lim_StopLoss==0) SL=0.0000;
      else SL=NewPrice-lim_StopLoss*Point;
      if(lim_TakeProfit==0) TP=0.0000;
      else TP=NewPrice+st_TakeProfit*Point;
      fm=OrderSend(SMB,OP_BUYLIMIT,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*��������*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*������������*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_BuyLimit=false;
         Print("����� BuyLimit ����������");
         Comment("����� BuyLimit ����������");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//���� ���������� �� �������
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("������ � ",GLE," ��������� BuyLimit-������");
         Print ("�������� ������ - ",ED);
        }
     }
   if(
      (SchSellLimit==0 && (SchSell<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_SellLimit && lim_Step>=MinLevel) || (SigTIME_SellLimit && lim_Step>=MinLevel))
     )
     {
      NewPrice=Bid+lim_Step*Point;
      if(lim_StopLoss==0) SL=0.0000;
      else SL=NewPrice+lim_StopLoss*Point;
      if(lim_TakeProfit==0) TP=0.0000;
      else TP=NewPrice-lim_TakeProfit*Point;
      fm=OrderSend(SMB,OP_SELLLIMIT,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*��������*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*������������*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_SellLimit=false;
         Print("����� SellLimit ����������");
         Comment("����� SellLimit ����������");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//���� ���������� �� �������
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("������ � ",GLE," ��������� SellLimit-������");
         Print ("�������� ������ - ",ED);
        }
     }
  }
// =============== �������� �������� ������� ===============================================================
//   ������� UstanRyn() ��������� �������� ������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UstanRyn()
  {
   bool NewOrderSell,NewOrderBuy; SMB=Symbol();
   int  OldTimeBuy,OldTimeSell;
   RefreshRates(); SchOrders();
   //==== �������� ������� ���������� ��������� ������ ============
   //  ���� ���� �������� ����� ��� ����, ����� �� ����� ����� ����������� ������ ���� ������
   //  ���� � ������ �� ��������� ������� ����� ������ ���������� ������� � Buy � Sell �����,
   //  �� ���� �������� ������ �� ������, �� ������� ��� ������ �� ����� ���� 
   for( i=OrdersTotal()-1; i>0; i-- )
     {//������ �����
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
        {//������ ������ � ��������� �������
         if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) {continue;}
         if(OrderType()==OP_BUY)
           {
            if(OrderOpenTime()>=OldTimeBuy)//���� ����� �������� � ����� ������ ������ ��� � ���������� ���������, ��...
            {OldTimeBuy=OrderOpenTime();}//���������� ����� ���������� ��������� Buy-������
           }
         if(OrderType()==OP_SELL)
           {
            if(OrderOpenTime()>=OldTimeSell)//���� ����� �������� � ����� ������ ������ ��� � ���������� ���������, ��...
            {OldTimeSell=OrderOpenTime();}//���������� ����� ���������� ��������� Sell-������
           }
        }//����� ������ � ��������� �������
     }//����� �����  OldTimeBuy,OldTimeSell
   if(OldTimeBuy>=Time[0]) {NewOrderBuy=false;}
   if(OldTimeBuy<Time[0])  {NewOrderBuy=true;}
   if(OldTimeSell>=Time[0]) {NewOrderSell=false;}
   if(OldTimeSell<Time[0])  {NewOrderSell=true;}
   //==== ����� "�������� ������� ���������� ��������� ������" ====  
   if (NewOrderBuy && SigTIME_Buy && SchBuy==0)
     {//���� ����� ������� Buy-�����           
      if(ryn_StopLoss==0) {SL=0.0000;}
      else {SL=Ask-ryn_StopLoss*Point;}
      if(ryn_TakeProfit==0) {TP=0.0000;}
      else {TP=Ask+ryn_TakeProfit*Point;}
      fm=OrderSend(SMB,OP_BUY,Lot,Ask,3,SL,TP,NULL,MAGIC,0,Blue);
      if(fm!=0 && fm!=-1 && UseOrderSound) {PlaySound(NameOrderSound);}
      if(fm!=0 && fm!=-1)
        {
         SigTIME_Buy=false;
         Comment("������ �� ������� �������");
         Print("������ �� ������� �������");
        }
      if (fm==-1 || fm==0)
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("������ � ",GLE," ��������� Buy-������");
         Print ("�������� ������ - ",ED);
        }
     }//����� "���� ����� ������� Buy-�����"
   if(NewOrderSell && SigTIME_Sell && SchSell==0)
     { //���� ����� ������� Sell-����� 
      if(ryn_StopLoss==0) {SL=0.0000;}
      else {SL=Bid+ryn_StopLoss*Point;}
      if(ryn_TakeProfit==0) {TP=0.0000;}
      else {TP=Bid-ryn_TakeProfit*Point;}
      fm=OrderSend(SMB,OP_SELL,Lot,Bid,3,SL,TP,NULL,MAGIC,0,Red);
      if(fm!=0 && fm!=-1 && UseOrderSound) {PlaySound(NameOrderSound);}
      if(fm!=0 && fm!=-1)
        {
         SigTIME_Sell=false;
         Comment("������ �� ������� ������� ");
         Print("������ �� ������� ������� ");
        }
      if (fm==-1 || fm==0)
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("������ � ",GLE," ��������� SellL-������");
         Print ("�������� ������ - ",ED);
        }
     }//����� "���� ����� ������� Sell-�����"
  }
//=========== �������� ���������� �������  =================================================================

//  ������� SchOrders() �������� �� ���� ������� � ������������, ������� ������ ������� ������� ������� ����.
//  ��� ������ ����������� �������� ���� ��� ����� ��������, ����� ������� ������� ������� SchOrders()
//  ��� ������� ������ � ����� ����� ����� �������� ��� ��� ���� �������.
//  SchBuyStop  - ������� ������� BuyStop
//  SchSellStop - ������� ������� SellStop
//  SchBuyLimit - ������� ������� BuyLimit
//  SchSellLimit  ������� ������� SellLimit
//  SchBuy      - ������� Buy �������
//  SchSell     - ������� Sell �������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SchOrders()
  {
   // ��� ������ ������ ��������� �� �� ��������
   SchBuyStop=0; SchSellStop=0; SchBuyLimit=0; SMB=Symbol();
   SchBuy=0; SchSell=0; SchSellLimit=0;
   for(i=OrdersTotal()-1;i>=0;i--)
     {//������ �����
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {//������ ������ � ��������� �������
         if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) continue;
         if(OrderType()==OP_BUYSTOP)
            SchBuyStop++;
         if(OrderType()==OP_SELLSTOP)
            SchSellStop++;
         if(OrderType()==OP_SELLLIMIT)
            SchSellLimit++;
         if(OrderType()==OP_BUYLIMIT)
            SchBuyLimit++;
         if(OrderType()==OP_BUY)
            SchBuy++;
         if(OrderType()==OP_SELL)
            SchSell++;
        }//����� ������ � ��������� �������
     }//����� �����
  }
//+------------------------------------------------------------------+
//| ������������� ������� ������� ������                             |
//+------------------------------------------------------------------+
void TrailingPositions()
  {
   if(OrderType()==OP_BUY)
     {
      if(!WaitProfit || (Bid-OrderOpenPrice())>ryn_TrStop*Point)
        {
         if (OrderStopLoss()<Bid-(ryn_TrStop+ryn_TrStep-1)*Point)
         {ModifyStopLoss(Bid-ryn_TrStop*Point);}
        }
     }
   if(OrderType()==OP_SELL)
     {
        if(!WaitProfit || OrderOpenPrice()-Ask>ryn_TrStop*Point) {
         if(OrderStopLoss()>Ask+(ryn_TrStop+ryn_TrStep-1)*Point || OrderStopLoss()==0)
         {ModifyStopLoss(Ask+ryn_TrStop*Point);}
        }
     }
  }
//+------------------------------------------------------------------+
//| ������� ������ StopLoss                                          |
//| ���������:                                                       |
//|   ldStopLoss - ������� StopLoss                                  |
//+------------------------------------------------------------------+
void ModifyStopLoss(double ldStopLoss)
  {
   fm=OrderModify(OrderTicket(),OrderOpenPrice(),ldStopLoss,OrderTakeProfit(),0,CLR_NONE);
   if(fm!=0 && fm!=-1 && UseTrailingSound) PlaySound(NameTrallingSound);
   if(fm==0 || fm==-1) {ModifError();}
  }
//======== ������������ ����������� ������ ����� �����  ============================================
// ������� TrailingOtlozh() ����������� ���������� ����� ����� �� �����
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingOtlozh()
  {
   RefreshRates(); SMB=Symbol();
   for(i=OrdersTotal()-1;i>=0;i--)//����. �������� �� ���� �������
     {//������ �����
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {//������ ������ � ��������� �������
         if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) {continue;}
         if(OrderType()==OP_BUYSTOP)//��������� ������, ���� ����
           {
            if(TrailBuyStop)
              {
               NewPrice=Ask+st_TrStop*Point;
               if(st_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice-st_StopLoss*Point;}
               if(st_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice+st_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1) {ModifError();}
              }
           }
         if(OrderType()==OP_SELLSTOP) // ��������� �����, ���� �����
           {
            if(TrailSellStop)
              {
               NewPrice=Bid-st_TrStop*Point;
               if(st_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice+st_StopLoss*Point;}
               if(st_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice-st_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1)  {ModifError();}
              }
           }
         if(OrderType()==OP_BUYLIMIT) // ��������� �����, ���� �����
           {
            if(TrailBuyLimit)
              {
               NewPrice=Ask-st_TrStop*Point;
               if(lim_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice-lim_StopLoss*Point;}
               if(lim_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice+lim_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1) {ModifError();}
              }
           }
         if(OrderType()==OP_SELLLIMIT)//��������� ������, ���� ����
           {
            if(TrailSellLimit)
              {
               NewPrice=Bid+st_TrStop*Point;
               if(lim_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice+lim_StopLoss*Point;}
               if(lim_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice-lim_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1) {ModifError();}
              }
           }
        }//����� ������ � ��������� �������
     }//����� �����
  }//����� �������
//========= ���������� �������� ������ =========================================================================================
// ������� ���������� �� ��� ������ � � ���������� ��������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string ErrorDescription(int error_code)
  {
   string error_string;
//----
   switch(error_code)
     {
      //---- codes returned from trade server
      case 0:    error_string=" ��� ������"; break;
      case 1:   error_string=" ��� ������, �� ��������� ����������"; break;
      case 2:   error_string=" ����� ������"; break;
      case 3:    error_string=" ������������ ���������"; break;
      case 4:    error_string=" �������� ������ �����"; break;
      case 5:    error_string=" ������ ������ ����������� ���������"; break;
      case 6:    error_string=" ��� ����� � �������� ��������"; break;
      case 7:    error_string=" ������������ ����"; break;
      case 8:    error_string=" ������� ������ �������"; break;
      case 9:    error_string=" ������������ �������� ���������� ���������������� �������"; break;
      case 64:    error_string=" ���� ������������"; break;
      case 65:    error_string=" ������������ ����� �����"; break;
      case 128:    error_string=" ����� ���� �������� ���������� ������"; break;
      case 129:    error_string=" ������������ ����"; break;
      case 130:    error_string=" ������������ �����"; break;
      case 131:    error_string=" ������������ �����"; break;
      case 132:    error_string=" ����� ������"; break;
      case 133:    error_string=" �������� ���������"; break;
      case 134:    error_string=" ������������ ����� ��� ���������� ��������"; break;
      case 135:    error_string=" ���� ����������"; break;
      case 136:    error_string=" ��� ���"; break;
      case 137:    error_string=" ������ �����"; break;
      case 138:    error_string=" ����� ����"; break;
      case 139:    error_string=" ����� ������������ � ��� ��������������"; break;
      case 140:    error_string=" ��������� ������ �������"; break;
      case 141:    error_string=" ������� ����� ��������"; break;
      case 145:    error_string=" ����������� ���������, ��� ��� ����� ������� ������ � �����"; break;
      case 146:    error_string=" ���������� �������� ������"; break;
      case 147:    error_string=" ������������� ���� ��������� ������ ��������� ��������"; break;
      case 148:    error_string=" ���������� �������� � ���������� ������� �������� �������, �������������� ��������"; break;
      case 4000:    error_string=" ��� ������"; break;
      case 4001:    error_string=" ������������ ��������� �������"; break;
      case 4002:    error_string=" ������ ������� - ��� ���������"; break;
      case 4003:    error_string=" ��� ������ ��� ����� �������"; break;
      case 4004:    error_string=" ������������ ����� ����� ������������ ������"; break;
      case 4005:    error_string=" �� ����� ��� ������ ��� �������� ����������"; break;
      case 4006:    error_string=" ��� ������ ��� ���������� ���������"; break;
      case 4007:    error_string=" ��� ������ ��� ��������� ������"; break;
      case 4008:    error_string=" �������������������� ������"; break;
      case 4009:    error_string=" �������������������� ������ � �������"; break;
      case 4010:    error_string=" ��� ������ ��� ���������� �������"; break;
      case 4011:    error_string=" ������� ������� ������"; break;
      case 4012:    error_string=" ������� �� ������� �� ����"; break;
      case 4013:    error_string=" ������� �� ����"; break;
      case 4014:    error_string=" ����������� �������"; break;
      case 4015:    error_string=" ������������ �������"; break;
      case 4016:    error_string=" �������������������� ������"; break;
      case 4017:    error_string=" ������ DLL �� ���������"; break;
      case 4018:    error_string=" ���������� ��������� ����������"; break;
      case 4019:    error_string=" ���������� ������� �������"; break;
      case 4020:    error_string=" ������ ������� ������������ ������� �� ���������"; break;
      case 4021:    error_string=" ������������ ������ ��� ������, ������������ �� �������"; break;
      case 4022:    error_string=" ������� ������"; break;
      case 4050:    error_string=" ������������ ���������� ���������� �������"; break;
      case 4051:    error_string=" ������������ �������� ��������� �������"; break;
      case 4052:    error_string=" ���������� ������ ��������� �������"; break;
      case 4053:    error_string=" ������ �������"; break;
      case 4054:    error_string=" ������������ ������������� �������-���������"; break;
      case 4055:    error_string=" ������ ����������������� ����������"; break;
      case 4056:    error_string=" ������� ������������"; break;
      case 4057:    error_string=" ������ ��������� ����������� ����������"; break;
      case 4058:    error_string=" ���������� ���������� �� ����������"; break;
      case 4059:    error_string=" ������� �� ��������� � �������� ������"; break;
      case 4060:    error_string=" ������� �� ������������"; break;
      case 4061:    error_string=" ������ �������� �����"; break;
      case 4062:    error_string=" ��������� �������� ���� string"; break;
      case 4063:    error_string=" ��������� �������� ���� integer"; break;
      case 4064:    error_string=" ��������� �������� ���� double"; break;
      case 4065:    error_string=" � �������� ��������� ��������� ������"; break;
      case 4066:    error_string=" ����������� ������������ ������ � ��������� ����������"; break;
      case 4067:    error_string=" ������ ��� ���������� �������� ��������"; break;
      case 4099:    error_string=" ����� �����"; break;
      case 4100:    error_string=" ������ ��� ������ � ������"; break;
      case 4101:    error_string=" ������������ ��� �����"; break;
      case 4102:    error_string=" ������� ����� �������� ������"; break;
      case 4103:    error_string=" ���������� ������� ����"; break;
      case 4104:    error_string=" ������������� ����� ������� � �����"; break;
      case 4105:    error_string=" �� ���� ����� �� ������"; break;
      case 4106:    error_string=" ����������� ������"; break;
      case 4107:    error_string=" ������������ �������� ���� ��� �������� �������"; break;
      case 4108:    error_string=" �������� ����� ������"; break;
      case 4109:    error_string=" �������� �� ���������"; break;
      case 4110:    error_string=" ������� ������� �� ���������"; break;
      case 4111:    error_string=" �������� ������� �� ���������"; break;
      case 4200:    error_string=" ������ ��� ����������"; break;
      case 4201:    error_string=" ��������� ����������� �������� �������"; break;
      case 4202:    error_string=" ������ �� ����������"; break;
      case 4203:    error_string=" ����������� ��� �������"; break;
      case 4204:    error_string=" ��� ����� �������"; break;
      case 4205:    error_string=" ������ ��������� �������"; break;
      case 4206:    error_string=" �� ������� ��������� �������"; break;
      case 4207:    error_string=" ������ ��� ������ � ��������"; break;
     }
//----
   return(error_string);
  }
// ======================= ����� ������ � �������� ������ ������  ==========================================
//   ������� WriteError() ����� ����� ��������� ������ ������ � � ������� ��������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WriteError()
  {
   GLE=GetLastError();
   ED=ErrorDescription(GLE);
   Print("������ ", GLE, " ��� ������ ������ ����� ",i);
   Print ("�������� ������ - ",ED);
  }
// ======================= ������ ����������� =====================================================
//  ������� ModifError() ����� ����� ��������� ������ ����������� � � ������� ��������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ModifError()
  {
   GLE=GetLastError();
   ED=ErrorDescription(GLE);
   Print("����������� ������ � ",OrderTicket(), " ������� ������ � ",GLE);
   Print ("�������� ������: ",ED);
  }
// ======================= �������� ������������ ���������������� ��������� =============================================
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Proverka()
  {
     if(ryn_TrStop<MinLevel && ryn_TrStop!=0) 
     {
      Comment("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      Print("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      Alert("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(ryn_TrStop>=MinLevel && ryn_TrStep==0) 
     {
      Comment("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      Print("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      Alert("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      return(0);
     }
     if(ryn_TakeProfit<MinLevel && ryn_TakeProfit!=0) 
     {
      Comment("������! ryn_TakeProfit �� ����� ���� ����� ",MinLevel);
      Print("������! ryn_TakeProfit �� ����� ���� ����� ",MinLevel);
      Alert("������! ryn_TakeProfit �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(ryn_StopLoss<MinLevel && ryn_StopLoss!=0) 
     {
      Comment("������! ryn_StopLoss �� ����� ���� ����� ",MinLevel);
      Print("������! ryn_StopLoss �� ����� ���� ����� ",MinLevel);
      Alert("������! ryn_StopLoss �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(st_TakeProfit<MinLevel && st_TakeProfit!=0) 
     {
      Comment("������! st_TakeProfit �� ����� ���� ����� ",MinLevel);
      Print("������! st_TakeProfit �� ����� ���� ����� ",MinLevel);
      Alert("������! st_TakeProfit �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(st_StopLoss<MinLevel && st_StopLoss!=0) 
     {
      Comment("������! st_StopLoss �� ����� ���� ����� ",MinLevel);
      Print("������! st_StopLoss �� ����� ���� ����� ",MinLevel);
      Alert("������! st_StopLoss �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(st_TrStop<MinLevel && st_TrStop!=0) 
     {
      Comment("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      Print("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      Alert("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(st_TrStop>=MinLevel && st_TrStep==0) 
     { 
      Comment("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      Print("������! ������������ �������� ������� �� ����� ���� ����� 1");
      Alert("������! ������������ �������� ������� �� ����� ���� ����� 1");
      return(0);
     }
     if(st_Step<MinLevel)                   
     { 
      Comment("������! ���������� st_Step �� ����� ���� ����� ",MinLevel);
      Print("������! ���������� st_Step �� ����� ���� ����� ",MinLevel);
      Alert("������! ���������� st_Step �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(lim_TakeProfit<MinLevel && lim_TakeProfit!=0) 
     {
      Comment("������! lim_TakeProfit �� ����� ���� ����� ",MinLevel);
      Print("������! lim_TakeProfit �� ����� ���� ����� ",MinLevel);
      Alert("������! lim_TakeProfit �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(lim_StopLoss<MinLevel && lim_StopLoss!=0) 
     {
      Comment("������! lim_StopLoss �� ����� ���� ����� ",MinLevel);
      Print("������! lim_StopLoss �� ����� ���� ����� ",MinLevel);
      Alert("������! lim_StopLoss �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(lim_TrStop<MinLevel && lim_TrStop!=0) 
     {
      Comment("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      Print("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      Alert("������! ������������ �������� ������� �� ����� ���� ����� ",MinLevel);
      return(0);
     }
     if(lim_TrStop>=MinLevel && lim_TrStep==0) 
     {
      Comment("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      Print("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      Alert("������! ��� ������ �������� ������� �� ����� ���� ����� 1");
      return(0);
     }
     if(lim_Step<MinLevel)                  
     {
      Comment("������! ���������� lim_Step �� ����� ���� ����� ",MinLevel);
      Print("������! ���������� lim_Step �� ����� ���� ����� ",MinLevel);
      Alert("������! ���������� lim_Step �� ����� ���� ����� ",MinLevel);
      return(0);
     }
  }
//+------------------------------------------------------------------+