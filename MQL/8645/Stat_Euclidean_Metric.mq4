//+------------------------------------------------------------------+
//|                                        Stat_Euclidean_Metric.mq4 |
//|                                                      StatBars TO |
//|                                      http://ridecrufter.narod.ru |
//+------------------------------------------------------------------+
#property copyright "StatBars TO"
#property link      "http://ridecrufter.narod.ru"

#define v_dim_x 6 //Количество векторов учавствующих в распозновании...
#define Num_neighbour 10 //Количество ближайших соседей по которым и принимается решение о пренадлежности вектора к 0 или 1

extern bool Base=false;
extern double buy_threshold=0.6;
extern double sell_threshold=0.6;
extern bool inverse_position_open_?=true;
extern double invers_buy_threshold=0.3;
extern double invers_sell_threshold=0.3;
extern int fast=12;
extern int slow=34;
extern int tp=40;
extern int sl=30;
extern bool close_orders=false;

double base_buy[][v_dim_x];
double base_sell[][v_dim_x];

int numbers_of_vectors_buy=0;
int numbers_of_vectors_sell=0;

int init()
  {
   if(!Base)
   {
      int Hadle_1=FileOpen("Buy_Position.dat",FILE_BIN|FILE_READ);
      ArrayResize(base_buy,FileSize(Hadle_1)/(v_dim_x*8));
      
      int count=0;
      while(!FileIsEnding(Hadle_1))
      {
         base_buy[count][0]=FileReadDouble(Hadle_1,DOUBLE_VALUE);
         base_buy[count][1]=FileReadDouble(Hadle_1,DOUBLE_VALUE);
         base_buy[count][2]=FileReadDouble(Hadle_1,DOUBLE_VALUE);
         base_buy[count][3]=FileReadDouble(Hadle_1,DOUBLE_VALUE);
         base_buy[count][4]=FileReadDouble(Hadle_1,DOUBLE_VALUE);
         base_buy[count][5]=FileReadDouble(Hadle_1,DOUBLE_VALUE);
         Print(base_sell[count][5]);
         count++;
      }
      numbers_of_vectors_buy=count;
      int Hadle_2=FileOpen("Sell_Position.dat",FILE_BIN|FILE_READ);
      ArrayResize(base_sell,FileSize(Hadle_2)/(v_dim_x*8));
      count=0;
      while(!FileIsEnding(Hadle_2))
      {
         base_sell[count][0]=FileReadDouble(Hadle_2,DOUBLE_VALUE);
         base_sell[count][1]=FileReadDouble(Hadle_2,DOUBLE_VALUE);
         base_sell[count][2]=FileReadDouble(Hadle_2,DOUBLE_VALUE);
         base_sell[count][3]=FileReadDouble(Hadle_2,DOUBLE_VALUE);
         base_sell[count][4]=FileReadDouble(Hadle_2,DOUBLE_VALUE);
         base_sell[count][5]=FileReadDouble(Hadle_2,DOUBLE_VALUE);
         Print(base_sell[count][5]);
         count++;
      }
      numbers_of_vectors_sell=count;
   }
   FileClose(Hadle_1);
   FileClose(Hadle_2);
   return(0);
  }


int deinit()
  {
   if(Base)
   {
      int Hadle_1=FileOpen("Buy_Position.dat",FILE_BIN|FILE_WRITE);
      
   
      int count;
   
      double ordinate_1,ordinate_2,ordinate_3,ordinate_4,ordinate_5;
   
      for(int i=OrdersHistoryTotal()-1;i>=0;i--)
      {
         OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);
         if(OrderType()==0)
         {
            if(OrderProfit()>=0)//Здесь записывается база векторов для положительно закрывшихся ордеров
            {
               count=iBarShift(Symbol(),Period(),OrderOpenTime());
               count++;//Мы будем рассматривать предыдущий бар(перед открытием ордера) чтобы прогнозировать убыточность/прибыльность позиции
               ordinate_1=iMA(Symbol(),Period(),89,0,0,5,count)/iMA(Symbol(),Period(),144,0,0,5,count);
               ordinate_2=iMA(Symbol(),Period(),144,0,0,5,count)/iMA(Symbol(),Period(),233,0,0,5,count);
               ordinate_3=iMA(Symbol(),Period(),21,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_4=iMA(Symbol(),Period(),55,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_5=iMA(Symbol(),Period(),2,0,0,5,count)/iMA(Symbol(),Period(),55,0,0,5,count);
            
               FileWriteDouble(Hadle_1,ordinate_1,DOUBLE_VALUE);//Записываем вектор в базу...
               FileWriteDouble(Hadle_1,ordinate_2,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,ordinate_3,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,ordinate_4,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,ordinate_5,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,1,DOUBLE_VALUE);
            }
            if(OrderProfit()<0)//Здесь записывается база векторов для закрывшихся ордеров с отрицательным профитом(проще говоря с лосом)
            {
               count=iBarShift(Symbol(),Period(),OrderOpenTime());
               count++;//Мы будем рассматривать предыдущий бар(перед открытием ордера) чтобы прогнозировать убыточность/прибыльность позиции
               ordinate_1=iMA(Symbol(),Period(),89,0,0,5,count)/iMA(Symbol(),Period(),144,0,0,5,count);
               ordinate_2=iMA(Symbol(),Period(),144,0,0,5,count)/iMA(Symbol(),Period(),233,0,0,5,count);
               ordinate_3=iMA(Symbol(),Period(),21,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_4=iMA(Symbol(),Period(),55,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_5=iMA(Symbol(),Period(),2,0,0,5,count)/iMA(Symbol(),Period(),55,0,0,5,count);
            
               FileWriteDouble(Hadle_1,ordinate_1,DOUBLE_VALUE);//Записываем вектор в базу...
               FileWriteDouble(Hadle_1,ordinate_2,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,ordinate_3,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,ordinate_4,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,ordinate_5,DOUBLE_VALUE);
               FileWriteDouble(Hadle_1,0,DOUBLE_VALUE);//Обратить внимание что здесь вектор помечен как 0, а выше как 1
            }
         }
      }
      int Hadle_2=FileOpen("Sell_Position.dat",FILE_BIN|FILE_WRITE);
      for(i=OrdersHistoryTotal()-1;i>=0;i--)
      {
         OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);
         if(OrderType()==1)//Теперь записываем базу для коротких позиций
         {
            if(OrderProfit()>=0)//Здесь записывается база векторов для положительно закрывшихся ордеров
            {
               count=iBarShift(Symbol(),Period(),OrderOpenTime());
               count++;//Мы будем рассматривать предыдущий бар(перед открытием ордера) чтобы прогнозировать убыточность/прибыльность позиции
               ordinate_1=iMA(Symbol(),Period(),89,0,0,5,count)/iMA(Symbol(),Period(),144,0,0,5,count);
               ordinate_2=iMA(Symbol(),Period(),144,0,0,5,count)/iMA(Symbol(),Period(),233,0,0,5,count);
               ordinate_3=iMA(Symbol(),Period(),21,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_4=iMA(Symbol(),Period(),55,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_5=iMA(Symbol(),Period(),2,0,0,5,count)/iMA(Symbol(),Period(),55,0,0,5,count);
            
               FileWriteDouble(Hadle_2,ordinate_1,DOUBLE_VALUE);//Записываем вектор в базу...
               FileWriteDouble(Hadle_2,ordinate_2,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,ordinate_3,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,ordinate_4,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,ordinate_5,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,1,DOUBLE_VALUE);
            }
            if(OrderProfit()<0)//Здесь записывается база векторов для закрывшихся ордеров с отрицательным профитом(проще говоря с лосом)
            {
               count=iBarShift(Symbol(),Period(),OrderOpenTime());
               count++;//Мы будем рассматривать предыдущий бар(перед открытием ордера) чтобы прогнозировать убыточность/прибыльность позиции
               ordinate_1=iMA(Symbol(),Period(),89,0,0,5,count)/iMA(Symbol(),Period(),144,0,0,5,count);
               ordinate_2=iMA(Symbol(),Period(),144,0,0,5,count)/iMA(Symbol(),Period(),233,0,0,5,count);
               ordinate_3=iMA(Symbol(),Period(),21,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_4=iMA(Symbol(),Period(),55,0,0,5,count)/iMA(Symbol(),Period(),89,0,0,5,count);
               ordinate_5=iMA(Symbol(),Period(),2,0,0,5,count)/iMA(Symbol(),Period(),55,0,0,5,count);
            
               FileWriteDouble(Hadle_2,ordinate_1,DOUBLE_VALUE);//Записываем вектор в базу...
               FileWriteDouble(Hadle_2,ordinate_2,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,ordinate_3,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,ordinate_4,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,ordinate_5,DOUBLE_VALUE);
               FileWriteDouble(Hadle_2,0,DOUBLE_VALUE);//Обратить внимание что здесь вектор помечен как 0, а выше как 1
            }
         }
      }
   }
   return(0);
  }


int start()
  {
   
   if(!isNewBar())return(0);
   
   double MACD_1=iMACD(Symbol(),Period(),fast,slow,9,PRICE_TYPICAL,0,1);
   double MACD_2=iMACD(Symbol(),Period(),fast,slow,9,PRICE_TYPICAL,0,2);
   double MACD_3=iMACD(Symbol(),Period(),fast,slow,9,PRICE_TYPICAL,0,3);
   double Prob_win;
   double veсtor[5];
   
   if(Base)
   {
      if(MACD_3<=MACD_2 && MACD_2>MACD_1)
      {
         if(close_orders)Close_Orders_by_type(OP_BUY);
         OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Ask+sl*Point,Bid-tp*Point);
      }
      
      if(MACD_3>=MACD_2 && MACD_2<MACD_1)
      {
         if(close_orders)Close_Orders_by_type(OP_SELL);
         OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Bid-sl*Point,Ask+tp*Point);
      }
   }
   if(!Base)
   {
      if(MACD_3<=MACD_2 && MACD_2>MACD_1)
      {
         if(close_orders)Close_All_Orders();
         veсtor[0]=iMA(Symbol(),Period(),89,0,0,5,1)/iMA(Symbol(),Period(),144,0,0,5,1);
         veсtor[1]=iMA(Symbol(),Period(),144,0,0,5,1)/iMA(Symbol(),Period(),233,0,0,5,1);
         veсtor[2]=iMA(Symbol(),Period(),21,0,0,5,1)/iMA(Symbol(),Period(),89,0,0,5,1);
         veсtor[3]=iMA(Symbol(),Period(),55,0,0,5,1)/iMA(Symbol(),Period(),89,0,0,5,1);
         veсtor[4]=iMA(Symbol(),Period(),2,0,0,5,1)/iMA(Symbol(),Period(),55,0,0,5,1);
         
         Prob_win=Euclidean_Metric(base_sell,veсtor,numbers_of_vectors_sell);
         
         if(Prob_win>=sell_threshold)OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Ask+sl*Point,Bid-tp*Point);
         if(inverse_position_open_? && Prob_win<=invers_sell_threshold)OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Bid-sl*Point,Ask+tp*Point);
      }
      
      if(MACD_3>=MACD_2 && MACD_2<MACD_1)
      {
         if(close_orders)Close_All_Orders();
         veсtor[0]=iMA(Symbol(),Period(),89,0,0,5,1)/iMA(Symbol(),Period(),144,0,0,5,1);
         veсtor[1]=iMA(Symbol(),Period(),144,0,0,5,1)/iMA(Symbol(),Period(),233,0,0,5,1);
         veсtor[2]=iMA(Symbol(),Period(),21,0,0,5,1)/iMA(Symbol(),Period(),89,0,0,5,1);
         veсtor[3]=iMA(Symbol(),Period(),55,0,0,5,1)/iMA(Symbol(),Period(),89,0,0,5,1);
         veсtor[4]=iMA(Symbol(),Period(),2,0,0,5,1)/iMA(Symbol(),Period(),55,0,0,5,1);
         
         Prob_win=Euclidean_Metric(base_buy,veсtor,numbers_of_vectors_buy);
         
         if(Prob_win>=buy_threshold)OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Bid-sl*Point,Ask+tp*Point);
         if(inverse_position_open_? && Prob_win<=invers_buy_threshold)OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Ask+sl*Point,Bid-tp*Point);
         
         
      }
   }

   return(0);
  }


void Close_Orders_by_type(int type)
{
   for(int num=OrdersTotal()-1;num>=0;num--)       
   {
      OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()==type)
      OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),5,Gold);
   }
}
void Close_All_Orders()
{
   for(int num=OrdersTotal()-1;num>=0;num--)       
   {
      OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
      if(OrderProfit()>0)
      OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),5,Gold);
   }
}

double Euclidean_Metric(double&X_Data_Base[][v_dim_x], double Vector[v_dim_x],int num_v)
{
   int i=0,i1=0,i3,i2,i4;
   double Metric[1];
   double t,sum;
   ArrayResize(Metric,num_v);
   ArrayInitialize(Metric,0.0);
   
   for(i=0;i<num_v;i++)
   {
      for(i1=0;i1<v_dim_x-1;i1++)Metric[i]+=MathPow(X_Data_Base[i][i1]-Vector[i1],2);
      Metric[i]=MathSqrt(Metric[i]);
   }
   
   //sort by ASCEND Metric
   for(i3=0;i3<num_v-1;i3++)
      for(i2=i3+1;i2<num_v;i2++)     
         if(Metric[i3]>Metric[i2])
         {
            t=Metric[i3];
            Metric[i3]=Metric[i2];
            Metric[i2]=t;
            for(i4=0;i4<v_dim_x;i4++)
            {
               t=X_Data_Base[i3][i4];
               X_Data_Base[i3][i4]=X_Data_Base[i2][i4];
               X_Data_Base[i2][i4]=t;
            }
         }
   sum=0;
   for(i=0;i<Num_neighbour;i++)
   {
      sum+=X_Data_Base[i][5];//суммируем 0 и 1 ближайших соседей чтобы вывести итоговую вероятность выигрыша в этой сделке
      //Print(X_Data_Base[i][5]);
   }
   return(sum/Num_neighbour);
}

bool isNewBar()
  {
  static datetime BarTime;  
   bool res=false;
    
   if (BarTime!=Time[0]) 
      {
         BarTime=Time[0];  
         res=true;
      } 
   return(res);
  }