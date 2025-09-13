//=============================================================================================================================
//---------------------------------------------------------------------------------------------------------------------PROPERTY
#property copyright "Copyright © 2011, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---------------------------------------------------------------------------------------------------------------------INCLUDES
#include <stdlib.mqh>
#include <stderror.mqh>
#include <WinUser32.mqh>
#include <HybridEA.mqh>
string Warning;
//-----------------------------------------------------------------------------------------------------------------------------
//=============================================================================================================================
//------------------------------------------------------------------------------------------------------------------TIME CONFIG
int ALLOWED_SECONDS_1F=0, ALLOWED_SECONDS_1T=1, ALLOWED_SECONDS_2F=0, ALLOWED_SECONDS_2T=0;
//-----------------------------------------------------------------------------------------------------------POSITIONS SETTINGS
int POS_MIN_QUANT=1, POS_REQ_QUANT=6, POS_MAX_QUANT=50;
//------------------------------------------------------------------------------------------------------------------REAL CONFIG
string Currency="EURUSD";
string Currency1="EURUSD",Currency2="EURGBP",Currency3="EURCHF",Currency4="EURJPY",Currency5="EURAUD";
double DividerREAL1=5.0;
//------------------------------------------------------------------------------------------------------------------SRVI CONFIG
double DividerRVI1=2.0, MultiplifierRVI1=1.25, RVI_UNSAFE_DIFFERENTION=0.050;
//------------------------------------------------------------------------------------------------------------------OPEN CONFIG
double Lots=0.5;
int MaximalPositions=1;
//-----------------------------------------------------------------------------------------------------------------CLOSE CONFIG
double MinimalProfit=18.00, MinimalLoss=9.00;
//-----------------------------------------------------------------------------------------------------------------------------
//=============================================================================================================================
//-----------------------------------------------------------------------------------------------------------------------------
void InitiateFunctionAccess()
      {
       if(FunctionAccess!="Initiated"||FunctionAccess!="Allowed"||FunctionAccess!="Blocked")
        {
         FunctionAccess="Initiated";
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("Acces always initiated"); 
          }
        }
      }

void AllowFunctionAccess()
      {
       if(FunctionAccess=="Blocked"||FunctionAccess=="Initiated")
        {
         FunctionAccess="Allowed";
        }      
      }

void BlockFunctionAccess()
      {
       if(FunctionAccess=="Allowed"||FunctionAccess=="Initiated")
        {
         FunctionAccess="Blocked";
        }      
      }
//-----------------------------------------------------------------------------------------------------------------------------
void AllowTimeAccess()
      {
       TimeAccess="Allowed";
      }

void BlockTimeAccess()
      {
       TimeAccess="Blocked";
      }

void CheckTime()
      {
       if(FunctionAccess=="Allowed")
        {
         datetime CURR_MINUTE,PREV_MINUTE,CURR_SECONDS;
         datetime HCURR_MINUTE=TimeMinute(TimeLocal());
         //--------------------------------------------
         if(PREV_MINUTE==false&&CURR_MINUTE==false)
          {
           if(CURR_MINUTE==false)
            {
             CURR_MINUTE=HCURR_MINUTE;
            }
            
           CURR_SECONDS=TimeSeconds(TimeLocal());

           if(CURR_SECONDS>=ALLOWED_SECONDS_1F&&CURR_SECONDS<=ALLOWED_SECONDS_1T || CURR_SECONDS>=ALLOWED_SECONDS_2F&&CURR_SECONDS<=ALLOWED_SECONDS_2T)
            {
             AllowTimeAccess();
            }
           else
            {
             BlockTimeAccess();
            }
          }
         //--------------------------------------------
         if(PREV_MINUTE==false&&CURR_MINUTE!=false)
          {
           if(PREV_MINUTE==false)
            {
             if(CURR_MINUTE<TimeMinute(TimeLocal())||CURR_MINUTE>TimeMinute(TimeLocal()))
              {
               PREV_MINUTE=CURR_MINUTE;
              }
            }

           if(CURR_MINUTE<TimeMinute(TimeLocal())||CURR_MINUTE>TimeMinute(TimeLocal()))
            {
             CURR_MINUTE=HCURR_MINUTE;
            }
            
           CURR_SECONDS=TimeSeconds(TimeLocal());

           if(CURR_SECONDS>=ALLOWED_SECONDS_1F&&CURR_SECONDS<=ALLOWED_SECONDS_1T || CURR_SECONDS>=ALLOWED_SECONDS_2F&&CURR_SECONDS<=ALLOWED_SECONDS_2T)
            {
             AllowTimeAccess();
            }
           else
            {
             BlockTimeAccess();
            }
          }
         //--------------------------------------------
         if(PREV_MINUTE!=false&&CURR_MINUTE!=false)
          {
           if(CURR_MINUTE<TimeMinute(TimeLocal())||CURR_MINUTE>TimeMinute(TimeLocal()))
            {
             PREV_MINUTE=CURR_MINUTE;
            }

           if(CURR_MINUTE<TimeMinute(TimeLocal())||CURR_MINUTE>TimeMinute(TimeLocal()))
            {
             CURR_MINUTE=HCURR_MINUTE;
            }
            
           CURR_SECONDS=TimeSeconds(TimeLocal());

           if(CURR_SECONDS>=ALLOWED_SECONDS_1F&&CURR_SECONDS<=ALLOWED_SECONDS_1T || CURR_SECONDS>=ALLOWED_SECONDS_2F&&CURR_SECONDS<=ALLOWED_SECONDS_2T)
            {
             AllowTimeAccess();
            }
           else
            {
             BlockTimeAccess();
            }
          } 
        } 
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess Blocked 'CheckTime()'");
          }
        } 
      } 
//-----------------------------------------------------------------------------------------------------------------------------
void ClearValues()
      {
       if(FunctionAccess=="Allowed")
        {
         if(CurrentPos!=false)
          {
           CurrentPos=false;
          }
         if(PosValue!=false)
          {
           PosValue=false;
          }
         BlockFunctionAccess(); 
        }    
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess Blocked 'ClearValues()'");
          }
        }      
      }
//-----------------------------------------------------------------------------------------------------------------------------
void CalculateBaseValues()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         if(CurrentPos==false||CurrentPos>=POS_MIN_QUANT)
          {
           //-----------------------
           AllowFunctionAccess();
           SetNormal();
           //-----------------------
           AllowFunctionAccess();
           CalculateTotal();
           //-----------------------

           //-----------------------
           AllowFunctionAccess();
           CalculateRVI();
           //-----------------------

           //-----------------------
           AllowFunctionAccess();
           SetChannelScalper();
           //-----------------------
          }
         BlockFunctionAccess(); 
        }    
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'CalculateBaseValues()'");
          }
        }      
      }

void CalculateExtendedValues()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         if(CurrentPos>=POS_REQ_QUANT)
          {
           //-----------------------
           AllowFunctionAccess();
           SetMidDifferention();
           //-----------------------
           AllowFunctionAccess();
           SetBorderDifferention();
           //-----------------------
          }
         BlockFunctionAccess(); 
        }    
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'CalculateExtendedValues()'");
          }
        }      
      }

void SetNormal()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         Value[BID]=MarketInfo(Currency,MODE_BID);
         Value[ASK]=MarketInfo(Currency,MODE_ASK);
         Value[CLOSE]=iClose(Currency,0,1);
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'SetNormal()'");
          }
        } 
      }

void CalculateTotal()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         Total[BID]=(MarketInfo(Currency1,MODE_BID)+MarketInfo(Currency2,MODE_BID)+MarketInfo(Currency3,MODE_BID)+MarketInfo(Currency4,MODE_BID)+MarketInfo(Currency5,MODE_BID))/DividerREAL1;
         Total[ASK]=(MarketInfo(Currency1,MODE_ASK)+MarketInfo(Currency2,MODE_ASK)+MarketInfo(Currency3,MODE_ASK)+MarketInfo(Currency4,MODE_ASK)+MarketInfo(Currency5,MODE_ASK))/DividerREAL1;
         Total[CLOSE]=(iClose(Currency1,0,1)+iClose(Currency2,0,1)+iClose(Currency3,0,1)+iClose(Currency4,0,1)+iClose(Currency5,0,1))/DividerREAL1;
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'CalculateTotal()'");
          }
        }
      }      

void CalculateRVI()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         RVISignal=iRVI(NULL,0,10,MODE_SIGNAL,0);
         RVIMain=iRVI(NULL,0,10,MODE_MAIN,0);
         if(iRVI(NULL,0,10,MODE_SIGNAL,0)>iRVI(NULL,0,10,MODE_MAIN,0))
          {
           RVIRozdiel=(iRVI(NULL,0,10,MODE_SIGNAL,0)-iRVI(NULL,0,10,MODE_MAIN,0))/2;  
          }      
         if(iRVI(NULL,0,10,MODE_MAIN,0)>iRVI(NULL,0,10,MODE_SIGNAL,0))
          {
           RVIRozdiel=(iRVI(NULL,0,10,MODE_MAIN,0)-iRVI(NULL,0,10,MODE_SIGNAL,0))/2;  
          }      
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'CalculateRVI()'");
          }
        }
      }

void SetMidDifferention()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         RVI_MID_DIFFERENTION=Pos[CurrentPos][RVI_DIFFERENTION]/DividerRVI1;         
         //--------------------------------------------------------------
         BlockFunctionAccess();
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'SetMidDifferention()'");
          } 
        }
      }

void SetBorderDifferention()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         RVI_BORDER_DIFFERENTION=RVI_MID_DIFFERENTION*MultiplifierRVI1;         
         //---------------------------------------------------------
         BlockFunctionAccess();
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'SetMidDifferention()'");
          }
        }
      }
      
void SetChannelScalper()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         if(i==false)
          {
           i=2;
          }
         else
          {
           i++;
          }

         double atr = iATR(NULL,0,11,0);
         double cprice = iMA(NULL,0,1,0,MODE_SMA,PRICE_CLOSE,0);
         double mprice = (High[i]+Low[i])/2; 

         Up[i]  = mprice+2.0*atr;
         Dn[i]  = mprice-2.0*atr;
         
         if(cprice > Up[i-1])
          {
           Direction[i] =  1;
          }
         if(cprice < Dn[i-1])
          {
           Direction[i] = -1;
          }

         if(Direction[i] > 0)
          {
           Dn[i] = MathMax(Dn[i],Dn[i-1]);
           SignalBuy=1; SignalSell=0;
           
           Alert("01");
          }
         if(Direction[i] < 0)
          {
           Up[i] = MathMin(Up[i],Up[i-1]);
           SignalBuy=0; SignalSell=1;
         
           Alert("02");
          }
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'SetChannelScalper()'");
          }
        }
      }
//-----------------------------------------------------------------------------------------------------------------------------
void AddPositions()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         if(CurrentPos==false)
          {
           CurrentPos=POS_MIN_QUANT;
           //-----------------------------------------------
           Pos[CurrentPos][RVI_MAIN]=RVIMain;
           Pos[CurrentPos][RVI_SIGNAL]=RVISignal;
           //------------------------------------
           if(RVIMain>RVISignal)
            {
             Pos[CurrentPos][RVI_DIFFERENTION]=RVIMain-RVISignal;
            }
           if(RVISignal>RVIMain)
            {
             Pos[CurrentPos][RVI_DIFFERENTION]=RVISignal-RVIMain;
            }
           //-----------------------------------------------
           Pos[CurrentPos][VALUE_BID]=Value[BID];
           Pos[CurrentPos][VALUE_ASK]=Value[ASK];
           Pos[CurrentPos][VALUE_CLOSE]=Value[CLOSE];
           Pos[CurrentPos][TOTAL_BID]=Total[BID];
           Pos[CurrentPos][TOTAL_ASK]=Total[ASK];
           Pos[CurrentPos][TOTAL_CLOSE]=Total[CLOSE];
           //-----------------------------------------------
           BlockFunctionAccess();
          }
         if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
          {
           if(CurrentPos>=POS_MIN_QUANT&&CurrentPos<POS_MAX_QUANT)
            {
             CurrentPos++; 
             //-----------------------------------------------
             Pos[CurrentPos][RVI_MAIN]=RVIMain;
             Pos[CurrentPos][RVI_SIGNAL]=RVISignal;
             //------------------------------------
             if(RVIMain>RVISignal)
              {
               Pos[CurrentPos][RVI_DIFFERENTION]=RVIMain-RVISignal;
              }
             if(RVISignal>RVIMain)
              {
               Pos[CurrentPos][RVI_DIFFERENTION]=RVISignal-RVIMain;
              }
             //-----------------------------------------------------
             Pos[CurrentPos][VALUE_BID]=Value[BID];
             Pos[CurrentPos][VALUE_ASK]=Value[ASK];
             Pos[CurrentPos][VALUE_CLOSE]=Value[CLOSE];
             Pos[CurrentPos][TOTAL_BID]=Total[BID];
             Pos[CurrentPos][TOTAL_ASK]=Total[ASK];
             Pos[CurrentPos][TOTAL_CLOSE]=Total[CLOSE];
             //-----------------------------------------------
             BlockFunctionAccess();
            }
          }       
         if(CurrentPos==POS_MAX_QUANT)
          {
           AllowFunctionAccess();
           ReorganizePositions();
           //--------------------
          } 
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'AddPositions()'");
          }
        } 
      }

void ReorganizePositions()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {    
         if(CurrentPos==POS_MAX_QUANT)
          {
           if(PosValue==false)
            {
             PosValue=(CurrentPos-POS_MAX_QUANT)+1;
             //-------------------------------------------------------
             Pos[PosValue][RVI_MAIN]=Pos[PosValue+1][RVI_MAIN];
             Pos[PosValue][RVI_SIGNAL]=Pos[PosValue+1][RVI_SIGNAL];
             Pos[PosValue][RVI_DIFFERENTION]=Pos[PosValue+1][RVI_DIFFERENTION];
             //---------------------------------------------------- 
             Pos[PosValue][VALUE_BID]=Pos[PosValue+1][VALUE_BID];
             Pos[PosValue][VALUE_ASK]=Pos[PosValue+1][VALUE_ASK];
             Pos[PosValue][VALUE_CLOSE]=Pos[PosValue+1][VALUE_CLOSE];
             Pos[PosValue][TOTAL_BID]=Pos[PosValue+1][TOTAL_BID];
             Pos[PosValue][TOTAL_ASK]=Pos[PosValue+1][TOTAL_ASK];
             Pos[PosValue][TOTAL_CLOSE]=Pos[PosValue+1][TOTAL_CLOSE];
             //---------------------------------------------------- 
             AllowFunctionAccess();
             ReorganizePositions();
            }
           
           if(PosValue!=false && PosValue<(POS_MAX_QUANT-1))
            {
             PosValue++;
             //-------------------------------------------------------
             Pos[PosValue][RVI_MAIN]=Pos[PosValue+1][RVI_MAIN];
             Pos[PosValue][RVI_SIGNAL]=Pos[PosValue+1][RVI_SIGNAL];
             Pos[PosValue][RVI_DIFFERENTION]=Pos[PosValue+1][RVI_DIFFERENTION];
             //---------
             Pos[PosValue][VALUE_BID]=Pos[PosValue+1][VALUE_BID];
             Pos[PosValue][VALUE_ASK]=Pos[PosValue+1][VALUE_ASK];
             Pos[PosValue][VALUE_CLOSE]=Pos[PosValue+1][VALUE_CLOSE];
             Pos[PosValue][TOTAL_BID]=Pos[PosValue+1][TOTAL_BID];
             Pos[PosValue][TOTAL_ASK]=Pos[PosValue+1][TOTAL_ASK];
             Pos[PosValue][TOTAL_CLOSE]=Pos[PosValue+1][TOTAL_CLOSE];
             //---------------------------------------------------- 
             AllowFunctionAccess();
             ReorganizePositions();
            }
           if(PosValue==(POS_MAX_QUANT-1))//PosValue!=false && PosValue==POS_MAX_QUANT)
            {
             PosValue++;
             //-------------------------------------------------------
             Pos[PosValue][RVI_MAIN]=RVIMain;
             Pos[PosValue][RVI_SIGNAL]=RVISignal;
             //-------------------------------------------------------
             if(RVIMain>RVISignal)
              {
               Pos[PosValue][RVI_DIFFERENTION]=RVIMain-RVISignal;
              }
             if(RVISignal>RVIMain)
              {
               Pos[PosValue][RVI_DIFFERENTION]=RVISignal-RVIMain;
              }             
             //-------------------------------------------------------
             Pos[PosValue][VALUE_BID]=Value[BID];
             Pos[PosValue][VALUE_ASK]=Value[ASK];
             Pos[PosValue][VALUE_CLOSE]=Value[CLOSE];
             Pos[PosValue][TOTAL_BID]=Total[BID];
             Pos[PosValue][TOTAL_ASK]=Total[ASK];
             Pos[PosValue][TOTAL_CLOSE]=Total[CLOSE];
             //-------------------------------------------------------
             PosValue=false;
             BlockFunctionAccess();
            }
          }
         else
          {
           if(Warning=="Show")
            {
             Alert("Incorrect launch of 'ReorganizePositions()'");
            }
          } 
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'ReorganizePositions()'");
          }
        }      
      }   
//-----------------------------------------------------------------------------------------------------------------------------
void CheckTradeDirection()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         if(CurrentPos>=POS_REQ_QUANT)
          {
           if(Pos[CurrentPos][RVI_SIGNAL]<Pos[CurrentPos][RVI_MAIN]-RVI_MID_DIFFERENTION)
            {
             if(Pos[CurrentPos][RVI_DIFFERENTION]>RVI_UNSAFE_DIFFERENTION)
              {
               if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-4][TOTAL_ASK])
                {
                 if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-2][VALUE_ASK])
                  {   
                   if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-4][TOTAL_CLOSE])
                    {
                     if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-2][VALUE_CLOSE])
                      {
                       if(SignalBuy==1&&SignalSell==0)
                        {
                         TradeSystem=BUY;               
                        }       
                      }
                    }
                  }
                }
              }                         
            }         
           if(Pos[CurrentPos][RVI_SIGNAL]>Pos[CurrentPos][RVI_MAIN]+RVI_MID_DIFFERENTION)
            {
             if(Pos[CurrentPos][RVI_DIFFERENTION]>RVI_UNSAFE_DIFFERENTION)
              {
               if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-4][TOTAL_BID])
                {
                 if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-2][VALUE_BID])
                  {   
                   if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-4][TOTAL_CLOSE])
                    {
                     if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-2][VALUE_CLOSE])
                      {
                       if(SignalBuy==0&&SignalSell==1)
                        {
                         TradeSystem=SELL;               
                        }  
                      }
                    }
                  }
                }
              }   
            }         
           BlockFunctionAccess();
          }
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'CheckTradeDirection()'");
          }
        } 
      }      
//-----------------------------------------------------------------------------------------------------------------------------
void OpenPositions()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {    
         if(OrdersTotal()<MaximalPositions&&CurrentPos>=POS_REQ_QUANT)
          {
           if(TradeSystem==BUY)
            {
             ClosePositions();
             //---------------------------------------------------------------------------------------------------------
             OrderSend(Symbol(),OP_BUY,Lots,Ask,SlipPage,0,0,Commentation,Magic,Expiration,Green);
             TradeSystem=false;
            }  
           if(TradeSystem==SELL)
            {
             ClosePositions();
             //---------------------------------------------------------------------------------------------------------
             OrderSend(Symbol(),OP_SELL,Lots,Bid,SlipPage,0,0,Commentation,Magic,Expiration,Green);
             TradeSystem=false;
            }  
           BlockFunctionAccess();
          }
        }    
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'OpenPositions()'");
          }
        }
      }
      
void ClosePositions()
      {
       if(FunctionAccess=="Allowed")
        {    
         if(OrdersTotal()>0)
          {
           if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES))
            {
             int TradingType=OrderType();
             if(TradingType==OP_BUY)
              {
               if(OrderProfit()>=+1*MinimalProfit)
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,SlipPage,Red);
                }
               if(OrderProfit()<=-1*MinimalLoss)
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,SlipPage,Red);
                 //-----------------------------------------------------
                 //TradeSystem=SELL;
                 //OpenPositions();
                }
               BlockFunctionAccess();
              }
              
             if(TradingType==OP_SELL)
              {
               if(OrderProfit()>=+1*MinimalProfit)
                {
                 OrderClose(OrderTicket(),OrderLots(),Ask,SlipPage,Red);
                }
               if(OrderProfit()<=-1*MinimalLoss)
                {
                 OrderClose(OrderTicket(),OrderLots(),Ask,SlipPage,Red);
                 //-----------------------------------------------------
                 //TradeSystem=BUY;
                 //OpenPositions();
                }
               BlockFunctionAccess();
              } 
            }
          }
        }    
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess Blocked 'ClosePositions()'");
          }
        }
      }
//-----------------------------------------------------------------------------------------------------------------------------
void RemovePositions()
      {
       if(FunctionAccess=="Allowed")
        { 
         if(CurrentPos>=POS_MIN_QUANT)
          {
           if(CurrentPos==POS_MIN_QUANT)
            {
             if(PosValue==false)
              {
               PosValue=POS_MIN_QUANT;
               //------------------------------
               Pos[PosValue][VALUE_BID]=false;
               Pos[PosValue][VALUE_ASK]=false;
               Pos[PosValue][VALUE_CLOSE]=false;
               Pos[PosValue][TOTAL_BID]=false;
               Pos[PosValue][TOTAL_ASK]=false;
               //------------------------------
               Pos[PosValue][RVI_MAIN]=false;
               Pos[PosValue][RVI_SIGNAL]=false;
               Pos[PosValue][RVI_DIFFERENTION]=false;                
               //------------------------------ 
               CurrentPos=false;
               PosValue=false;
              }
             if(PosValue!=false)
              {
               if(PosValue!=POS_MIN_QUANT)
                {
                 PosValue=POS_MIN_QUANT;
                 //------------------------------
                 Pos[PosValue][VALUE_BID]=false;
                 Pos[PosValue][VALUE_ASK]=false;
                 Pos[PosValue][VALUE_CLOSE]=false;
                 Pos[PosValue][TOTAL_BID]=false;
                 Pos[PosValue][TOTAL_ASK]=false;
                 //------------------------------ 
                 Pos[PosValue][RVI_MAIN]=false;
                 Pos[PosValue][RVI_SIGNAL]=false;
                 Pos[PosValue][RVI_DIFFERENTION]=false;                
                 //------------------------------ 
                 CurrentPos=false;
                 PosValue=false;
                }
              }
            }
           if(CurrentPos>POS_MIN_QUANT)
            {
             if(PosValue==false)
              {
               PosValue=POS_MIN_QUANT;
               //---------------
               Pos[PosValue][VALUE_BID]=false;
               Pos[PosValue][VALUE_ASK]=false;
               Pos[PosValue][VALUE_CLOSE]=false;
               Pos[PosValue][TOTAL_BID]=false;
               Pos[PosValue][TOTAL_ASK]=false;
               //------------------------------ 
               Pos[PosValue][RVI_MAIN]=false;
               Pos[PosValue][RVI_SIGNAL]=false;
               Pos[PosValue][RVI_DIFFERENTION]=false;                
               //------------------------------ 
               RemovePositions();
              }  
             if(PosValue!=false)
              {
               if(PosValue>=POS_MIN_QUANT&&PosValue<CurrentPos)
                {
                 PosValue++;
                 //---------
                 Pos[PosValue][VALUE_BID]=false;
                 Pos[PosValue][VALUE_ASK]=false;
                 Pos[PosValue][VALUE_CLOSE]=false;
                 Pos[PosValue][TOTAL_BID]=false;
                 Pos[PosValue][TOTAL_ASK]=false;
                 //------------------------------ 
                 Pos[PosValue][RVI_MAIN]=false;
                 Pos[PosValue][RVI_SIGNAL]=false;
                 Pos[PosValue][RVI_DIFFERENTION]=false;                
                 //------------------------------ 
                 AllowFunctionAccess();
                 RemovePositions();
                }
               if(PosValue>=POS_MIN_QUANT&&PosValue==CurrentPos)
                {
                 Pos[PosValue][VALUE_BID]=false;
                 Pos[PosValue][VALUE_ASK]=false;
                 Pos[PosValue][VALUE_CLOSE]=false;
                 Pos[PosValue][TOTAL_BID]=false;
                 Pos[PosValue][TOTAL_ASK]=false;
                 //------------------------------ 
                 Pos[PosValue][RVI_MAIN]=false;
                 Pos[PosValue][RVI_SIGNAL]=false;
                 Pos[PosValue][RVI_DIFFERENTION]=false;                
                 //------------------------------ 
                 PosValue=false;
                 CurrentPos=false;
                }   
              }
            }
           BlockFunctionAccess();
          }
         else
          {
           if(Warning=="Show")
            {
             Alert("Incorrect launch of 'RemovePositions()'");
            }
          } 
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess Blocked 'RemovePositions()'");
          }
        }                
      }      
//-----------------------------------------------------------------------------------------------------------------------------
//=============================================================================================================================
//-----------------------------------------------------------------------------------------------------------------------------
int init()
  {
   //--------------------------------
   InitiateFunctionAccess();
   AllowFunctionAccess();
   ClearValues();
   //--------------------------------
   AllowFunctionAccess();                  
   CheckTime();
   //--------------------------------
   AllowFunctionAccess();
   CalculateBaseValues();
   //--------------------------------
   AllowFunctionAccess();                  
   AddPositions();                  
   //--------------------------------
   return(0);
  }

int start()
  {
   //----------------------------
   AllowFunctionAccess();                  
   CheckTime();                  
   //----------------------------
   AllowFunctionAccess();
   CalculateBaseValues();
   //----------------------------
   AllowFunctionAccess();
   AddPositions();
   //----------------------------
   AllowFunctionAccess();
   CalculateExtendedValues();
   //----------------------------
   AllowFunctionAccess();
   CheckTradeDirection();
   //----------------------------
   AllowFunctionAccess();
   ClosePositions();
   //----------------------------
   AllowFunctionAccess();
   OpenPositions();
   //----------------------------
   return(0);
  }

int deinit()
  {
   //----------------------------------
   AllowFunctionAccess();
   RemovePositions();
   //----------------------------------
   return(0);
  }
//-----------------------------------------------------------------------------------------------------------------------------
//=============================================================================================================================