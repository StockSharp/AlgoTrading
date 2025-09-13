//+------------------------------------------------------------------+
//|                               50 peHta6 tynnel MA medFIN.mq4.mq4 |
//|                                       Copyright � 2009, costy_   |
//|                                                 jena@deneg.net   |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2009, costy_"
#property link      "jena@deneg.net"
 
#define STUPID 0x60BE45
 
extern string     Lots_Desc         =  "���� 0 ����������� ������������ ���";
extern double     Lots              =  1;
 
extern string     RiskPercentage_Desc =  "��������� ��� ������������� ���� -- % �����. ���� 0 ������������ ����������� ��������� ������ ����, ���� Lots > 0 ��� ��������� ������������";
extern int        RiskPercentage    =  0;
 
extern int        Slippage          =  25;
 
extern string     Target_Desc       =  "���� ������, ���� 0 ������ �� ������������ 90-200";
extern int        Target            =  0 ;
 
extern string     Loss_Desc         =  "���� ����, ���� 0 ���� �� ������������ 30-80";
extern int        Loss              =  0;
 
extern string     MA_DESC           =  "������� �� ";
extern int        F55        = 55;
extern int        shift        = 13;
extern int        METHOD_MA=1;
extern int        timeframe =0;

extern string     ���������_��������  =  "� �����(�� -1��� �.�. ma shift 1)";
extern int        ���������=20;
extern int        ������=8;

 
extern string     MaxOrders_Desc    =  "���� 0 ���������� ������������ �������� ������� �� �������������� 1-3";
extern int        MaxOrders         =  1;
 
double LotsToBid;
string symbol;
  bool ����_�������?;
  bool ����_�������?;
 //---------------------------
  int k=1;

 // ����������� ���������� ������� ��� ��������� ����� ���������
// ������ ������ ������� - ��. ����� � ����� ����
#import "TrailingAll.ex4"
   void TrailingByShadows(int ticket,int tmfrm,int bars_n, int indent,bool trlinloss);
   void TrailingByFractals(int ticket,int tmfrm,int frktl_bars,int indent,bool trlinloss);   
   void TrailingStairs(int ticket,int trldistance,int trlstep);   
   void TrailingUdavka(int ticket,int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3);
   void TrailingByTime(int ticket,int interval,int trlstep,bool trlinloss);   
   void TrailingByATR(int ticket,int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss);
   void TrailingRatchetB(int ticket,int pf_level_1,int pf_level_2,int pf_level_3,int ls_level_1,int ls_level_2,int ls_level_3,bool trlinloss);  
   void TrailingByPriceChannel(int iTicket,int iBars_n,int iIndent);
   void TrailingByMA(int iTicket,int iTmFrme,int iMAPeriod,int iMAShift,int MAMethod,int iApplPrice,int iShift,int iIndent);
   void TrailingFiftyFifty(int iTicket,int iTmFrme,double dCoeff,bool bTrlinloss); 
   void KillLoss(int iTicket,double dSpeedCoeff);   
#import
 
 
  
 //--------------------------------------------------------------
// �������� �������
void CloseBuys(int MagicNumber, int Slippage)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_BUY)
      {
         if(OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, Blue))
         {
            i--;
         }
         RefreshRates();
      }
   }
}
 //-----------------------------------------------------
 
// �������� ������
void CloseSells(int MagicNumber, int Slippage)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_SELL)
      {
         if (OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, Red))
         {
            i--;
         }
         RefreshRates();
      }
   }
}
 //----------------------------------------------------
 
// ������� ���-�� �������� �������
int GetOrdersCount(int MagicNumber, int Type)
{
   int count = 0;
   
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == Type)
      {
         count++;
      }
   }
   
   return (count);
}
 //-------------------------------------------------------
 
// ���������� ������������� ����
double GetLotsToBid(int RiskPercentage)
{
   double margin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double step = MarketInfo(Symbol(), MODE_LOTSTEP);
   double account = AccountFreeMargin();
   
   double percentage = account*RiskPercentage/100;
   
   double lots = MathRound(percentage/margin/step)*step;
   
   if(lots < minLot)
   {
      lots = minLot;
   }
   
   if(lots > maxLot)
   {
      lots = maxLot;
   }
 
   return (lots);
}
 //----------------------------------------------------
 
// �������
void OpenBuy()
{
   double TP = 0;
   if (Target > 0)
   {
      TP = Bid + Target*Point;
   }
 
   double SL = 0;
   if (Loss > 0)
   {
      SL = Bid - Loss*Point;
   }
   
   if (Lots == 0) LotsToBid = GetLotsToBid(RiskPercentage);
    
   
   OrderSend(Symbol(), OP_BUY, LotsToBid, Ask, Slippage, SL, TP, NULL, STUPID, 0, Blue);
}
 //----------------------------------------------------
 
// �������
void OpenSell()
{
   double TP = 0;
   if (Target > 0)
   {
      TP = Ask - Target*Point;
   }
 
   double SL = 0;
   if (Loss > 0)
   {
      SL = Ask + Loss*Point;
   }
   
   if (Lots == 0) LotsToBid = GetLotsToBid(RiskPercentage);
   
   OrderSend(Symbol(), OP_SELL, LotsToBid, Bid, Slippage,  SL, TP, NULL, STUPID, 0, Red);
}
 //------------------------------------------------------
 // �������� ������� �������� � ���������� ���������
void Check()
{
  int X=1*k;
  int Y=13*k;
 
 //--------------------------------------------------------------
   double ma1         = iMA(symbol, timeframe, F55, 0, METHOD_MA, PRICE_MEDIAN,  shift);
   double ma0         = iMA(symbol, timeframe, F55, 0, METHOD_MA, PRICE_MEDIAN,  1);



    if(Hour()<���������&&Hour()>������){
              if(ma0>ma1&&����_�������?<1 ){CheckBuy();����_�������?=2;����_�������?=0;
                        }                                 //���� ��������� ���� ������ buy

              if(ma0<ma1&&����_�������?<1 ){CheckSell();����_�������?=2;����_�������?=0;
                       }    }                             //���� ��������� ���� ������ sell
  
     

}
void PrintComments() {
}
   
 //--------------------------------------------------------------
 //--------------------------------------------------------------
   
void CheckBuy()
{
      if (GetOrdersCount(STUPID, OP_SELL) > 0)
      {
         CloseSells(STUPID, Slippage);
        
      }
      if (GetOrdersCount(STUPID, OP_BUY) < MaxOrders || MaxOrders == 0) 
      {
         OpenBuy();
      }}
void CheckSell()
{
      if (GetOrdersCount(STUPID, OP_BUY) > 0)
      {
         CloseBuys(STUPID, Slippage);
      }
      if (GetOrdersCount(STUPID, OP_SELL) < MaxOrders || MaxOrders == 0) 
      {
         OpenSell();
      }}
 //--------------------------------------------------------------
 //--------------------------------------------------------------
int init()
{

   LotsToBid = Lots;
   symbol = Symbol();
}
 //--------------------------------------------------------------
int start()
{

PrintComments();
   // Check for open new orders and close current ones
   Check();
   for (int i=0;i<OrdersTotal();i++)
         {
         if (!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) break;
         if (OrderMagicNumber()!=STUPID || OrderSymbol()!=Symbol()) continue;
         if ((OrderType()==OP_BUY) || (OrderType()==OP_SELL))
            {
            // !!! ������ ������ ������� ��������� !!!
            // !!! ������ ������ ������� ��������� !!!
            // !!! ������ ������ ������� ��������� !!!
            // ����� ��������� ��������� ��, ��������, ������� �������� �� ���������. ����������� �� 
            // 5-������ ��������� �� �������, � �������� �� ���������� � 3 �., � ���� ������ �� ������
            //TrailingByFractals(OrderTicket(),5,10,8,false);
            // (��� �����, ���������� �������������� ������� ����� OrderSelect() � ������� �������, 
            // ������� �� ����� ������� � ��������� ����������� ���������).
            // ��� ������� �� ������ ��������������� ������ ��� ��������� � ���������� ����� ������ 
            // ��� ���� "���������������" �� ��� ����� ��� ����� ������� �����������.    
//--------------------------------------------------------------


//--------------------------------------------------------------
//| 1) �������� �� ���������                                            |
//| TrailingByFractals(int ticket,int tmfrm,int frktl_bars,int indent,bool trlinloss)

//| ���������:
//| ticket - ���������� ���������� ����� ������(��������� ����� ������� ������� � ������� OrderSelect());
//| tmfrm - ���������, �� ����� �������� �������������� �������� (�������� - 1, 5, 15, 30, 60, 240, 1440, 10080, 43200);
//| bars_n - ���������� ����� � ������� �������� (�� ������ 3);
//| indent - ������ (�������) �� ���������� ���������� ��������, �� ������� ����� �������� �������� (�� ������ 0);
//| trlinloss - ��������� ����, ������� �� ����������� �������� �� "��������" �������, �.�. � ��������� ����� 
//|    ��������� ���������� � ������ �������� (true - ������, false - �������� ���������� ������ ��� �������, 
//|    ��� ����� �������� "�����" ����� ��������, "� �������").

//TrailingByFractals(OrderTicket(),5,8,8,false);
            
//--------------------------------------------------------------
//| 2) �������� �� ����� N ������                                       |
//|  TrailingByShadows(int ticket,int tmfrm,int bars_n, int indent,bool trlinloss)

//| ���������:
//| ticket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| tmfrm - ���������, �� ����� �������� �������������� �������� (�������� - 1, 5, 15, 30, 60, 240, 1440, 10080, 43200);
//| bars_n - ���������� �����, ������� ������������ ��� ����������� ������ ��������� (�� ������ 1);
//| indent - ������ (�������) �� ���������� high/low, �� ������� ����� �������� �������� (�� ������ 0);
//| trlinloss - ��������� ����, ������� �� ����������� �������� �� "��������" �������, �.�. 
//|   � ��������� ����� ��������� ���������� � ������ �������� (true - ������, false - �������� ���������� ������ ��� �������, 
//|   ��� ����� �������� "�����" ����� ��������, "� �������").          
   
//TrailingByShadows(OrderTicket(),5,10,4,false);      

//--------------------------------------------------------------
//| 3) �������� �����������-������������   
//| TrailingStairs(int ticket,int trldistance,int trlstep)

//| ���������:
//| ticket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| trldistance - ���������� �� �������� ����� (�������), �� ������� "������" (�� ������ MarketInfo(Symbol(),MODE_STOPLEVEL));
//| trlstep - "���" ��������� ��������� (�������) (�� ������ 1).

//TrailingStairs(OrderTicket(),30,15);

//--------------------------------------------------------------
//| 4) �������� �����������-"������" 
//| TrailingUdavka(int ticket,int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3)

//| ���������:
//| ticket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| trl_dist_1 - �������� ���������� ��������� (�������) (�� ������ MarketInfo(Symbol(),MODE_STOPLEVEL), 
//|            ������ trl_dist_2 � trl_dist_3);
//| level_1 - ������� ������� (�������), ��� ���������� �������� ��������� ��������� ����� ��������� 
//|            � trl_dist_1 �� trl_dist_2 (������ level_2; ������ trl_dist_1);
//| trl dist_2 - ���������� ��������� (�������) ����� ���������� ������ ������ ������� � level_1 ������� 
//|           (�� ������ MarketInfo(Symbol(),MODE_STOPLEVEL));
//| level_2 - ������� ������� (�������), ��� ���������� �������� ��������� ��������� ����� ��������� 
//|            � trl_dist_2 �� trl_dist_3 ������� (������ trl_dist_1 � ������ level_1);
//| trl dist_3 - ���������� ��������� (�������) ����� ���������� ������ ������ ������� � level_2 ������� (
//|            �� ������ MarketInfo(Symbol(),MODE_STOPLEVEL)).

//TrailingUdavka(OrderTicket(),int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3);

//--------------------------------------------------------------
//| 5) �������� �� ������� 
//| TrailingByTime(int ticket,int interval,int trlstep,bool trlinloss) 
  
//| ���������:
//| ticket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| interval - ���������� ����� ����� � ������� �������� �������, �� ��������� ������� �������� ����������� �������� 
//|           �� ��� trlstep �������;
//| trlstep - ��� (�������), �� ������� �������� ���������� �������� ����� ������ interval �����;
//| trlinloss - � ������ ������ ���� trlinloss==true, �� ������ �� ���������, ����� �� ����� �������� 
//|           (���� �������� �� ��� ����������, ==0, ������ ������ �� ����� ��������).
 
//TrailingByTime(OrderTicket(),30,10,true);

//--------------------------------------------------------------
//| 6) �������� �� ATR (Average True Range, ������� �������� ��������)
//| TrailingByATR(int ticket,int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss)
   
//| ���������:
//| ticket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| atr_timeframe - ���������, �� ������� ������������ �������� ATR (�������� - 1, 5, 15, 30, 60, 240, 1440, 10080, 43200);
//| atr1_period - ������ ������� ATR (������ 0; ����� ���� ����� atr2_period, �� ����� ������� �� ����������, ������ - ��. ����);
//| atr1_shift - ��� ������� ATR ����� "����", � ������� �������������� �������� ATR, ������������ �������� ���� �� ��������� 
//|              ���������� ����� ����� (��������������� ����� �����);
//| atr2_period - ������ ������� ATR (������ 0);
//| atr2_shift - ��� ������� ATR ����� "����", � ������� �������������� �������� ATR, ������������ �������� ���� �� ��������� 
//|              ���������� ����� ����� (��������������� ����� �����);
//| coeff - �������� ������� ��� ATR*coeff, �.�. ��� �����������, ������������, �� ���������� �������� ATR �� �������� ����� 
//|         ������� ���������� ��������;
//| trlinloss - ��������� ����, ������� �� ����������� �������� �� "��������" �������, �.�. � ��������� ����� ��������� 
//|             ���������� � ������ �������� (true - ������, false - �������� ���������� ������ ��� �������, 
//|             ��� ����� �������� "�����" ����� ��������, "� �������").   
   
//TrailingByATR(OrderTicket(),int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss);
   
//--------------------------------------------------------------
//| 7) �������� RATCHET �����������
//| TrailingRatchetB(int ticket,int pf_level_1,int pf_level_2,int pf_level_3,int ls_level_1,int ls_level_2,int ls_level_3,bool trlinloss)   
   
//| ���������:
//| ticket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| pf_level_1 - ������� ������� (�������), ��� ������� �������� ��������� � ��������� + 1 �����;
//| pf_level_2 - ������� ������� (�������), ��� ������� �������� ��������� � +1 �� ���������� pf_level_1 ������� �� ����� ��������;
//| pf_level_3 - ������� ������� (�������), ��� ������� �������� ��������� � pf_level_1 �� pf_level_2 ������� �� ����� �������� 
//|              (�� ���� �������� ������� �������������);
//| ls_level_1 - ���������� �� ����� �������� � ������� "�����", �� ������� ����� ���������� �������� ��� ���������� �������� 
//|              ������� +1 (�.�. ��� +1 �������� ����� ������ �� ls_level_1);
//| ls_level_2 - ���������� �� ����� �������� � "�����", �� ������� ����� ���������� �������� ��� �������, ��� ���� ������� 
//|              ��������� ���� ls_level_1, � ����� �������� ���� (�.�. ����� ����, �� �� ����� ����������� - 
//|              �� �������� ��� ���������� ����������);
//| ls_level_3 - ���������� �� ����� �������� "������", �� ������� ����� ���������� �������� ��� �������, ��� ���� �������� 
//|              ���� ls_level_2, � ����� �������� ����;
//| trlinloss - ��������� ����, ������� �� ����������� �������� �� "��������" �������, �.�. � ��������� ����� ��������� 
//|             ���������� � ������ �������� (true - ������, false - �������� ���������� ������ ��� �������, 
//|             ��� ����� �������� "�����" ����� ��������, "� �������").   
   
//TrailingRatchetB(OrderTicket(),80,2000,1500,50,30,20,false);   
   
//--------------------------------------------------------------
//| 8) �������� �� ������� ������    
//| TrailingByPriceChannel(int iTicket,int iBars_n,int iIndent)

//| ���������:
//| iTicket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| iBars_n - ������ ������ (���-�� �����, ����� ������ ���� ���������� ��� � ���������� ��� - ������� � ������ 
//|           ������� ������ ��������������);
//| iIndent - ������ (�������), � ������� ������������� �������� �� ������� ������.

//TrailingByPriceChannel(OrderTicket(),16,5);
 
//--------------------------------------------------------------
//|  9) �������� �� ����������� ��������
//|  TrailingByMA(OrderTicket(),int iTmFrme,int iMAPeriod,int iMAShift,int MAMethod,int iApplPrice,int iShift,int iIndent)  
   
//| ���������:
//| iTicket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| iTmFrme - ������ �����, �� ������� ����� ������������� ������; ���������� �������� �����: 1 (M1), 5 (M5), 15 (M15), 30 (M30), 60 (H1), 240 (H4), 1440 (D), 10080 (W), 43200 (MN);
//| iMAPeriod - ������ ���������� ��� ���������� ����������� ��������;
//| iMAShift - ����� ���������� ������������ �������� �������;
//| iMAMethod - ����� ����������; ���������� �������� �����: 0 (MODE_SMA), 1 (MODE_EMA), 2 (MODE_SMMA), 3 (MODE_LWMA);
//| iApplPrice - ������������ ����; �������� �����: 0 (PRICE_CLOSE), 1 (PRICE_OPEN), 2 (PRICE_HIGH), 3 (PRICE_LOW), 4 (PRICE_MEDIAN), 5 (PRICE_TYPICAL), 6 (PRICE_WEIGHTED);
//| iShift - ����� ������������ �������� ���� �� ��������� ���������� �������� �����;
//| iIndent - ������ (�������) �� �������� �������� ��� ���������� ���������.

//    ���������� �������� �����:   
//    iTmFrme:    1 (M1), 5 (M5), 15 (M15), 30 (M30), 60 (H1), 240 (H4), 1440 (D), 10080 (W), 43200 (MN);
//    iMAPeriod:  2-infinity, ����� �����; 
//    iMAShift:   ����� ������������� ��� ������������� �����, � ����� 0;
//    MAMethod:   0 (MODE_SMA), 1 (MODE_EMA), 2 (MODE_SMMA), 3 (MODE_LWMA);
//    iApplPrice: 0 (PRICE_CLOSE), 1 (PRICE_OPEN), 2 (PRICE_HIGH), 3 (PRICE_LOW), 4 (PRICE_MEDIAN), 5 (PRICE_TYPICAL), 6 (PRICE_WEIGHTED)
//    iShift:     0-Bars, ����� �����;
//    iIndent:    0-infinity, ����� �����;
  
//TrailingByMA(OrderTicket(),5,21,0,0,0,0,0);

//--------------------------------------------------------------
//|  10) �������� "�����������" 
//|  TrailingFiftyFifty(int iTicket,int iTmFrme,double dCoeff,bool bTrlinloss)   

//| ���������:
//| iTicket - ���������� ���������� ����� ������ (��������� ����� ������� ������� � ������� OrderSelect());
//| iTmFrme - ������ �����, �� ����� �������� ����� �������������� ��������; ���������� �������� �����: 1 (M1), 5 (M5), 15 (M15), 30 (M30), 60 (H1), 240 (H4), 1440 (D), 10080 (W), 43200 (MN);

//| dCoeff - �����������, ������������ ��, � ������� ��� ����� ��������� ���������� ����� ������ �� ������ �������� ���� 
//|          � ������� ����������;
//| bTrlinloss - ��������� ����, ������� �� ������������ �������� �� �������� �������.
       
//TrailingFiftyFifty(OrderTicket(),15,2,false)  ;

//--------------------------------------------------------------

 
            }
         }
   return(0);
}