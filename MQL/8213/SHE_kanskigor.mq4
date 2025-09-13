//+------------------------------------------------------------------+
//|                                             SHE_kanskigor.mq4 |
//|                                         Copyright � 2006, Shurka |
//|                                                 shforex@narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, Shurka"
#property link      "shforex@narod.ru"
#define MAGIC 130306

extern double        Lots=0.1;         // ���������� ����� ��� ������
extern int           Profit=35;        // �������� �����������, ���� 0 - ������ ��� �������
extern int           Stop=55;          // �������� ���������, ���� 0 - ������ ��� �����
extern int           Slippage=5;       // ���������������
extern string        Symb="*";         // ������ ��� ������. ���� * �� �� �������� ������� �������
                                       // ����� ����� ������� ���������� ���� EURUSD
extern string        StartTime="00:05";// ����� ������ �� ��������

datetime             TimeStart;
double               stoplevel,profitlevel;
string               SMB;
bool                 trade=false;

//+------------------------------------------------------------------+
//| �������� �������                                                 |
//+------------------------------------------------------------------+
int start()
{
   int      i,b;
   
   // ��������� ����� �� �������� �������� StartTime �� ��������� TimeStart
   TimeStart=StrToTime(StartTime);
   // ���� ������� ����� ������ ���������� ��� ������ ��� �� 5 �����, �� ������� � ������ �� ������.
   // �� �������������� ������ ���������� trade ������. ������ ���������� ���������� � ���, ��� ��� �����������.
   if(CurTime()<TimeStart || CurTime()>TimeStart+300) { trade=false; return(0); }
   // ���� trade �������, ������ ��� ������ ���������.
   if(trade) return(0);
   // ���� ���� �������� ����� ���� ������ ���� ��������, ������ �������� ����� ������
   if(iOpen(SMB,PERIOD_D1,1)>iClose(SMB,PERIOD_D1,1)) b=OP_BUY; else b=OP_SELL;
   // ���� ��������
   if(b==OP_BUY)
   {
      // ���� Stop ��� ����� 0, �� � ����������� �������� 0, ����� Ask-Stop
      if(Stop==0) stoplevel=0; else stoplevel=MarketInfo(SMB,MODE_ASK)-Stop*MarketInfo(SMB,MODE_POINT);
      // �� �� � � ������ �������
      if(Profit==0) profitlevel=0; else profitlevel=MarketInfo(SMB,MODE_ASK)+Profit*MarketInfo(SMB,MODE_POINT);
      // ����������� � ������� �� ���� Ask �� ������ stoplevel � �������� profitlevel
      i=OrderSend(SMB,OP_BUY,Lots,MarketInfo(SMB,MODE_ASK),Slippage,stoplevel,profitlevel,NULL,MAGIC,0,Red);
      // ���� ����� ������ ��������, �� ��������� torg ������� � ������, ����� ������ ���� �� ���������
      if(i!=-1) trade=true;
   }
   // � �������� �� �� �����, ��� � � ��������.
   if(b==OP_SELL)
   {
      if(Stop==0) stoplevel=0; else stoplevel=MarketInfo(SMB,MODE_BID)+Stop*MarketInfo(SMB,MODE_POINT);
      if(Profit==0) profitlevel=0; else profitlevel=MarketInfo(SMB,MODE_BID)-Profit*MarketInfo(SMB,MODE_POINT);
      i=OrderSend(SMB,OP_SELL,Lots,MarketInfo(SMB,MODE_BID),Slippage,stoplevel,profitlevel,NULL,MAGIC,0,Blue);
      if(i!=-1) trade=true;
   }
   return(0);
}
//+------------------------------------------------------------------+
//| ������� ������������� ���������                                  |
//+------------------------------------------------------------------+
int init()
{
   int i;
   // ���������� ���� ��� ��������
   if(Symb=="*") SMB=Symbol(); else SMB=Symb;
   return(0);
}
//+------------------------------------------------------------------+
//| ������� ��������������� ���������                                |
//+------------------------------------------------------------------+
int deinit() { return(0); }
//+------------------------------------------------------------------+