//=============================================================================================================================
//---------------------------------------------------------------------------------------------------------------------PROPERTY
#property copyright "Copyright ř 2011, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---------------------------------------------------------------------------------------------------------------------INCLUDES
#include <stdlib.mqh>
#include <stderror.mqh>
#include <WinUser32.mqh>
#include <HybridEA1.mqh>
string Warning;
//-----------------------------------------------------------------------------------------------------------------------------
//=============================================================================================================================
//------------------------------------------------------------------------------------------------------------------TIME CONFIG
int ALLOWED_HOURS_1F=0, ALLOWED_HOURS_1T=10, ALLOWED_HOURS_2F=0, ALLOWED_HOURS_2T=10;
int ALLOWED_MINUTES_1F=0, ALLOWED_MINUTES_1T=59, ALLOWED_MINUTES_2F=0, ALLOWED_MINUTES_2T=59;
int ALLOWED_SECONDS_1F=0, ALLOWED_SECONDS_1T=60, ALLOWED_SECONDS_2F=0, ALLOWED_SECONDS_2T=60;
//-----------------------------------------------------------------------------------------------------------POSITIONS SETTINGS
int POS_MIN_QUANT=1, POS_REQ_QUANT=7, POS_MAX_QUANT=30;
//------------------------------------------------------------------------------------------------------------------REAL CONFIG
string Currency="EURUSD";
string Currency1="EURUSD",Currency2="EURGBP",Currency3="EURCHF",Currency4="EURJPY",Currency5="EURAUD";
double DividerREAL1=5.0;
//------------------------------------------------------------------------------------------------------------------SRVI CONFIG
double DividerRVI1=2.0, MultiplifierRVI1=1.25, RVI_UNSAFE_DIFFERENTION=0.020;
//------------------------------------------------------------------------------------------------------------------OPEN CONFIG
double Lots=0.1;
int MaximalPositions=1;
int MinChance=0;
//-----------------------------------------------------------------------------------------------------------------CLOSE CONFIG
double MinimalProfit=3.00, MinimalLoss=3.00;
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
           
           CURR_HOUR=TimeHour(TimeLocal()); 
           CURR_SECONDS=TimeSeconds(TimeLocal());

           if(CURR_SECONDS>=ALLOWED_SECONDS_1F&&CURR_SECONDS<=ALLOWED_SECONDS_1T || CURR_SECONDS>=ALLOWED_SECONDS_2F&&CURR_SECONDS<=ALLOWED_SECONDS_2T)
            {
             if(CURR_HOUR>=ALLOWED_HOURS_1F&&CURR_HOUR<=ALLOWED_HOURS_1T || CURR_HOUR>=ALLOWED_HOURS_2F&&CURR_HOUR<=ALLOWED_HOURS_2T)
              {
               if(CURR_MINUTE>=ALLOWED_MINUTES_1F&&CURR_MINUTE<=ALLOWED_MINUTES_1T || CURR_MINUTE>=ALLOWED_MINUTES_2F&&CURR_MINUTE<=ALLOWED_MINUTES_2T)
                {
                 AllowTimeAccess();
                }
              }  
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
            
           CURR_HOUR=TimeHour(TimeLocal()); 
           CURR_SECONDS=TimeSeconds(TimeLocal());

           if(CURR_SECONDS>=ALLOWED_SECONDS_1F&&CURR_SECONDS<=ALLOWED_SECONDS_1T || CURR_SECONDS>=ALLOWED_SECONDS_2F&&CURR_SECONDS<=ALLOWED_SECONDS_2T)
            {
             if(CURR_HOUR>=ALLOWED_HOURS_1F&&CURR_HOUR<=ALLOWED_HOURS_1T || CURR_HOUR>=ALLOWED_HOURS_2F&&CURR_HOUR<=ALLOWED_HOURS_2T)
              {
               if(CURR_MINUTE>=ALLOWED_MINUTES_1F&&CURR_MINUTE<=ALLOWED_MINUTES_1T || CURR_MINUTE>=ALLOWED_MINUTES_2F&&CURR_MINUTE<=ALLOWED_MINUTES_2T)
                {
                 AllowTimeAccess();
                }
              } 
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

           CURR_HOUR=TimeHour(TimeLocal()); 
           CURR_SECONDS=TimeSeconds(TimeLocal());

           if(CURR_SECONDS>=ALLOWED_SECONDS_1F&&CURR_SECONDS<=ALLOWED_SECONDS_1T || CURR_SECONDS>=ALLOWED_SECONDS_2F&&CURR_SECONDS<=ALLOWED_SECONDS_2T)
            {
             if(CURR_HOUR>=ALLOWED_HOURS_1F&&CURR_HOUR<=ALLOWED_HOURS_1T || CURR_HOUR>=ALLOWED_HOURS_2F&&CURR_HOUR<=ALLOWED_HOURS_2T)
              {
               if(CURR_MINUTE>=ALLOWED_MINUTES_1F&&CURR_MINUTE<=ALLOWED_MINUTES_1T || CURR_MINUTE>=ALLOWED_MINUTES_2F&&CURR_MINUTE<=ALLOWED_MINUTES_2T)
                {
                 if(CURR_HOUR>=ALLOWED_HOURS_1F&&CURR_HOUR<=ALLOWED_HOURS_1T || CURR_HOUR>=ALLOWED_HOURS_2F&&CURR_HOUR<=ALLOWED_HOURS_2T)
                  {
                   if(CURR_MINUTE>=ALLOWED_MINUTES_1F&&CURR_MINUTE<=ALLOWED_MINUTES_1T || CURR_MINUTE>=ALLOWED_MINUTES_2F&&CURR_MINUTE<=ALLOWED_MINUTES_2T)
                    {
                     AllowTimeAccess();
                    }
                  } 
                }
              } 
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

void CalculateSpecialValues()
      {
       if(FunctionAccess=="Allowed")
        {
         if(CurrentPos==false||CurrentPos>=POS_MIN_QUANT)
          {
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
           Alert("FunctionAccess Blocked 'CalculateSpecialValues()'");
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
         //-------------------------------------------
         Value[RSI]=iRSI(Currency,0,10,PRICE_CLOSE,0);
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
         //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
         Total[RSI]=(iRSI(Currency1,0,10,PRICE_CLOSE,0)+iRSI(Currency2,0,10,PRICE_CLOSE,0)+iRSI(Currency3,0,10,PRICE_CLOSE,0)+iRSI(Currency4,0,10,PRICE_CLOSE,0)+iRSI(Currency5,0,10,PRICE_CLOSE,0))/DividerREAL1;
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
       if(FunctionAccess=="Allowed")
        {
         if(B6SignalBuy!=false||B6SignalSell!=false)
          {
           B6SignalBuy=B5SignalBuy;
           B6SignalSell=B5SignalBuy;
          }
         if(B5SignalBuy!=false||B5SignalSell!=false)
          {
           B5SignalBuy=B4SignalBuy;
           B5SignalSell=B4SignalBuy;
          }
         if(B4SignalBuy!=false||B4SignalSell!=false)
          {
           B4SignalBuy=B3SignalBuy;
           B4SignalSell=B3SignalBuy;
          }
         if(B3SignalBuy!=false||B3SignalSell!=false)
          {
           B3SignalBuy=B2SignalBuy;
           B3SignalSell=B2SignalBuy;
          }
         if(B2SignalBuy!=false||B2SignalSell!=false)
          {
           B3SignalBuy=B2SignalBuy;
           B3SignalSell=B2SignalBuy;
          }
         if(B1SignalBuy!=false||B1SignalSell!=false)
          {
           B2SignalBuy=B1SignalBuy;
           B2SignalSell=B1SignalBuy;
          }
         if(CSignalBuy!=false||CSignalSell!=false)
          {
           B1SignalBuy=CSignalBuy;
           B1SignalSell=CSignalSell;
          }

         CSignalBuy=GlobalVariableGet("GSignalBuy");
         CSignalSell=GlobalVariableGet("GSignalSell");

         if(CSignalBuy==1)
          {
           //string add1=(Bid+Ask+Bars)/TIME_SECONDS*8;
           //ObjectCreate(add1,OBJ_VLINE,0,TimeCurrent(),Bid);
           //ObjectSet(add1,OBJPROP_COLOR,Green);
          }
         if(CSignalSell==1)
          {
           //string add2=(Bid+Ask+Bars)/TIME_SECONDS*8;
           //ObjectCreate(add2,OBJ_VLINE,0,TimeCurrent(),Bid);
           //ObjectSet(add2,OBJPROP_COLOR,Red);
          }
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess Blocked 'SetChannelScalper()'");
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
           Pos[CurrentPos][VALUE_RSI]=Value[RSI];
           //------------------------------------
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
             Pos[CurrentPos][VALUE_RSI]=Value[RSI];
             //------------------------------------       
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
             //------------------------------------------------
             Pos[PosValue][RVI_MAIN]=Pos[PosValue+1][RVI_MAIN];
             Pos[PosValue][RVI_SIGNAL]=Pos[PosValue+1][RVI_SIGNAL];
             Pos[PosValue][RVI_DIFFERENTION]=Pos[PosValue+1][RVI_DIFFERENTION];
             //----------------------------------------------------------------
             Pos[PosValue][RSI]=Pos[PosValue+1][RSI];
             //-------------------------------------------------- 
             Pos[PosValue][VALUE_BID]=Pos[PosValue+1][VALUE_BID];
             Pos[PosValue][VALUE_ASK]=Pos[PosValue+1][VALUE_ASK];
             Pos[PosValue][VALUE_CLOSE]=Pos[PosValue+1][VALUE_CLOSE];
             Pos[PosValue][TOTAL_BID]=Pos[PosValue+1][TOTAL_BID];
             Pos[PosValue][TOTAL_ASK]=Pos[PosValue+1][TOTAL_ASK];
             Pos[PosValue][TOTAL_CLOSE]=Pos[PosValue+1][TOTAL_CLOSE];
             //------------------------------------------------------
             Pos[PosValue][TOTAL_RSI]=Pos[PosValue+1][TOTAL_RSI];             
             //--------------------------------------------------
             AllowFunctionAccess();
             ReorganizePositions();
            }
           
           if(PosValue!=false && PosValue<(POS_MAX_QUANT-1))
            {
             PosValue++;
             //------------------------------------------------
             Pos[PosValue][RVI_MAIN]=Pos[PosValue+1][RVI_MAIN];
             Pos[PosValue][RVI_SIGNAL]=Pos[PosValue+1][RVI_SIGNAL];
             Pos[PosValue][RVI_DIFFERENTION]=Pos[PosValue+1][RVI_DIFFERENTION];
             //----------------------------------------------------------------
             Pos[PosValue][RSI]=Pos[PosValue+1][RSI];             
             //--------------------------------------------------
             Pos[PosValue][VALUE_BID]=Pos[PosValue+1][VALUE_BID];
             Pos[PosValue][VALUE_ASK]=Pos[PosValue+1][VALUE_ASK];
             Pos[PosValue][VALUE_CLOSE]=Pos[PosValue+1][VALUE_CLOSE];
             Pos[PosValue][TOTAL_BID]=Pos[PosValue+1][TOTAL_BID];
             Pos[PosValue][TOTAL_ASK]=Pos[PosValue+1][TOTAL_ASK];
             Pos[PosValue][TOTAL_CLOSE]=Pos[PosValue+1][TOTAL_CLOSE];
             //------------------------------------------------------ 
             Pos[PosValue][TOTAL_RSI]=Pos[PosValue+1][TOTAL_RSI];             
             //--------------------------------------------------
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
             Pos[PosValue][VALUE_RSI]=Value[RSI];
             //-------------------------------------------------------
             Pos[PosValue][VALUE_BID]=Value[BID];
             Pos[PosValue][VALUE_ASK]=Value[ASK];
             Pos[PosValue][VALUE_CLOSE]=Value[CLOSE];
             Pos[PosValue][TOTAL_BID]=Total[BID];
             Pos[PosValue][TOTAL_ASK]=Total[ASK];
             Pos[PosValue][TOTAL_CLOSE]=Total[CLOSE];
             //-------------------------------------------------------
             Pos[PosValue][TOTAL_RSI]=Total[RSI];
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
void CheckChances()
      {
       if(FunctionAccess=="Allowed"&&TimeAccess=="Allowed")
        {
         BuyChance=0; SellChance=0;

         //=====================================================================
    
         if(B1SignalBuy==1) 
          {
           BuyChance=BuyChance+6;
          }

         if(B1SignalSell==1) 
          {
           SellChance=SellChance+6;
          }

         //=====================================================================

         if(B2SignalBuy==1) 
          {
           BuyChance=BuyChance+5;
          }

         if(B2SignalSell==1) 
          {
           SellChance=SellChance+5;
          }

         //=====================================================================

         if(B3SignalBuy==1) 
          {
           BuyChance=BuyChance+4;
          }

         if(B3SignalSell==1) 
          {
           SellChance=SellChance+4;
          }

         //=====================================================================

         if(B4SignalBuy==1) 
          {
           BuyChance=BuyChance+3;
          }
          
         if(B4SignalSell==1) 
          {
           SellChance=SellChance+3;
          }

         //=====================================================================

         if(B5SignalBuy==1) 
          {
           BuyChance=BuyChance+2;
          }

         if(B5SignalSell==1) 
          {
           SellChance=SellChance+2;
          }

         //=====================================================================

         if(B6SignalBuy==1) 
          {
           BuyChance=BuyChance+1;
          }

         if(B6SignalSell==1) 
          {
           SellChance=SellChance+1;
          }

         //=====================================================================

         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-1][TOTAL_RSI])
          {
           BuyChance=BuyChance+3;
          }

         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-2][TOTAL_RSI])
          {
           BuyChance=BuyChance+2;
          }
          
         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-3][TOTAL_RSI])
          {
           BuyChance=BuyChance+1;
          }

         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-4][TOTAL_RSI])
          {
           BuyChance=BuyChance+0;
          }
          
         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-5][TOTAL_RSI])
          {
           BuyChance=BuyChance-1;
          }
                    
         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-6][TOTAL_RSI])
          {
           BuyChance=BuyChance-2;
          }

         if(Pos[CurrentPos][TOTAL_RSI]>Pos[CurrentPos-7][TOTAL_RSI])
          {
           BuyChance=BuyChance-3;
          }
          
         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-1][TOTAL_RSI])
          {
           SellChance=SellChance+3;
          }

         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-2][TOTAL_RSI])
          {
           SellChance=SellChance+2;
          }

         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-3][TOTAL_RSI])
          {
           SellChance=SellChance+1;
          }

         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-4][TOTAL_RSI])
          {
           SellChance=SellChance+0;
          }
          
         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-5][TOTAL_RSI])
          {
           SellChance=SellChance-1;
          }
                    
         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-6][TOTAL_RSI])
          {
           SellChance=SellChance-2;
          }

         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-7][TOTAL_RSI])
          {
           SellChance=SellChance-3;
          }
                    
         //=====================================================================

         if(Pos[CurrentPos-1][TOTAL_RSI]>Pos[CurrentPos-2][TOTAL_RSI])
          {
           BuyChance=BuyChance+2;
          }

         if(Pos[CurrentPos-1][TOTAL_RSI]>Pos[CurrentPos-3][TOTAL_RSI])
          {
           BuyChance=BuyChance+1;
          }

         if(Pos[CurrentPos-1][TOTAL_RSI]>Pos[CurrentPos-4][TOTAL_RSI])
          {
           BuyChance=BuyChance+0;
          }
                    
         if(Pos[CurrentPos-1][TOTAL_RSI]>Pos[CurrentPos-5][TOTAL_RSI])
          {
           BuyChance=BuyChance-1;
          }
                              
         if(Pos[CurrentPos-1][TOTAL_RSI]>Pos[CurrentPos-6][TOTAL_RSI])
          {
           BuyChance=BuyChance-2;
          }
          
         if(Pos[CurrentPos-1][TOTAL_RSI]<Pos[CurrentPos-2][TOTAL_RSI])
          {
           SellChance=SellChance+2;
          }
          
         if(Pos[CurrentPos-1][TOTAL_RSI]<Pos[CurrentPos-3][TOTAL_RSI])
          {
           SellChance=SellChance+1;
          }

         if(Pos[CurrentPos-1][TOTAL_RSI]<Pos[CurrentPos-4][TOTAL_RSI])
          {
           SellChance=SellChance+0;
          }          
          
         if(Pos[CurrentPos-1][TOTAL_RSI]<Pos[CurrentPos-5][TOTAL_RSI])
          {
           SellChance=SellChance-1;
          }
                    
         if(Pos[CurrentPos-1][TOTAL_RSI]<Pos[CurrentPos-6][TOTAL_RSI])
          {
           SellChance=SellChance-2;
          }
          
         //=====================================================================          
          
         if(Pos[CurrentPos-2][TOTAL_RSI]>Pos[CurrentPos-3][TOTAL_RSI])
          {
           BuyChance=BuyChance+1;
          }
           
         if(Pos[CurrentPos-2][TOTAL_RSI]>Pos[CurrentPos-4][TOTAL_RSI])
          {
           BuyChance=BuyChance+0;
          }

         if(Pos[CurrentPos-2][TOTAL_RSI]>Pos[CurrentPos-5][TOTAL_RSI])
          {
           BuyChance=BuyChance-1;
          }
          
         if(Pos[CurrentPos-2][TOTAL_RSI]<Pos[CurrentPos-3][TOTAL_RSI])
          {
           SellChance=SellChance+1;
          }

         if(Pos[CurrentPos-2][TOTAL_RSI]<Pos[CurrentPos-4][TOTAL_RSI])
          {
           SellChance=SellChance+0;
          }
          
         if(Pos[CurrentPos-2][TOTAL_RSI]<Pos[CurrentPos-5][TOTAL_RSI])
          {
           SellChance=SellChance-1;
          }    
          
         //=====================================================================          

         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-1][VALUE_RSI])
          {
           BuyChance=BuyChance+3;
          }

         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-2][VALUE_RSI])
          {
           BuyChance=BuyChance+2;
          }

         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-3][VALUE_RSI])
          {
           BuyChance=BuyChance+1;
          }

         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-4][VALUE_RSI])
          {
           BuyChance=BuyChance+0;
          }
          
         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-5][VALUE_RSI])
          {
           BuyChance=BuyChance-1;
          }

         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-6][VALUE_RSI])
          {
           BuyChance=BuyChance-2;
          }
                              
         if(Pos[CurrentPos][VALUE_RSI]>Pos[CurrentPos-7][VALUE_RSI])
          {
           BuyChance=BuyChance-3;
          }
          
          if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-1][TOTAL_RSI])
          {
           SellChance=SellChance+3;
          }

          if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-2][TOTAL_RSI])
          {
           SellChance=SellChance+2;
          }

          if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-3][TOTAL_RSI])
          {
           SellChance=SellChance+1;
          }

         if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-4][TOTAL_RSI])
          {
           SellChance=SellChance+0;
          }
          
          if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-5][TOTAL_RSI])
          {
           SellChance=SellChance-1;
          }
                    
          if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-6][TOTAL_RSI])
          {
           SellChance=SellChance-2;
          }

          if(Pos[CurrentPos][TOTAL_RSI]<Pos[CurrentPos-7][TOTAL_RSI])
          {
           SellChance=SellChance-3;
          }
                    
         //=====================================================================

         if(Pos[CurrentPos-1][VALUE_RSI]>Pos[CurrentPos-2][VALUE_RSI])
          {
           BuyChance=BuyChance+2;
          }

         if(Pos[CurrentPos-1][VALUE_RSI]>Pos[CurrentPos-3][VALUE_RSI])
          {
           BuyChance=BuyChance+1;
          }

         if(Pos[CurrentPos-1][VALUE_RSI]>Pos[CurrentPos-4][VALUE_RSI])
          {
           BuyChance=BuyChance+0;
          }

         if(Pos[CurrentPos-1][VALUE_RSI]>Pos[CurrentPos-5][VALUE_RSI])
          {
           BuyChance=BuyChance-1;
          }
          
         if(Pos[CurrentPos-1][VALUE_RSI]>Pos[CurrentPos-6][VALUE_RSI])
          {
           BuyChance=BuyChance-2;
          }
          
         if(Pos[CurrentPos-1][VALUE_RSI]<Pos[CurrentPos-2][VALUE_RSI])
          {
           SellChance=SellChance+2;
          }

         if(Pos[CurrentPos-1][VALUE_RSI]<Pos[CurrentPos-3][VALUE_RSI])
          {
           SellChance=SellChance+1;
          }

         if(Pos[CurrentPos-1][VALUE_RSI]<Pos[CurrentPos-4][VALUE_RSI])
          {
           SellChance=SellChance+0;
          }
          
         if(Pos[CurrentPos-1][VALUE_RSI]<Pos[CurrentPos-5][VALUE_RSI])
          {
           SellChance=SellChance-1;
          }
          
         if(Pos[CurrentPos-1][VALUE_RSI]<Pos[CurrentPos-6][VALUE_RSI])
          {
           SellChance=SellChance-2;
          }
          
         //=====================================================================

         if(Pos[CurrentPos-2][VALUE_RSI]>Pos[CurrentPos-3][VALUE_RSI])
          {
           BuyChance=BuyChance+1;
          }
           
         if(Pos[CurrentPos-2][VALUE_RSI]>Pos[CurrentPos-4][VALUE_RSI])
          {
           BuyChance=BuyChance+0;
          }
          
         if(Pos[CurrentPos-2][VALUE_RSI]>Pos[CurrentPos-5][VALUE_RSI])
          {
           BuyChance=BuyChance-1;
          }
          
         if(Pos[CurrentPos-2][VALUE_RSI]<Pos[CurrentPos-3][VALUE_RSI])
          {
           SellChance=SellChance+1;
          }

         if(Pos[CurrentPos-2][VALUE_RSI]<Pos[CurrentPos-4][VALUE_RSI])
          {
           SellChance=SellChance+0;
          }
          
         if(Pos[CurrentPos-2][VALUE_RSI]<Pos[CurrentPos-5][VALUE_RSI])
          {
           SellChance=SellChance-1;
          }

         //=====================================================================

        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-1][TOTAL_ASK])
         {
          BuyChance=BuyChance+3;
         }           

        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-2][TOTAL_ASK])
         {
          BuyChance=BuyChance+2;
         }  

        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-3][TOTAL_ASK])
         {
          BuyChance=BuyChance+1;
         }  

        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-4][TOTAL_ASK])
         {
          BuyChance=BuyChance+0;
         } 
         
        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-5][TOTAL_ASK])
         {
          BuyChance=BuyChance-1;
         }          
         
        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-6][TOTAL_ASK])
         {
          BuyChance=BuyChance-2;
         }  
         
        if(Pos[CurrentPos][TOTAL_ASK]>Pos[CurrentPos-7][TOTAL_ASK])
         {
          BuyChance=BuyChance-3;
         }  
         
        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-1][TOTAL_BID])
         {
          SellChance=SellChance+3;
         } 
         
        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-2][TOTAL_BID])
         {
          SellChance=SellChance+2;
         } 

        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-3][TOTAL_BID])
         {
          SellChance=SellChance+1;
         } 

        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-4][TOTAL_BID])
         {
          SellChance=SellChance+0;
         } 
         
        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-5][TOTAL_BID])
         {
          SellChance=SellChance-1;
         } 
                  
        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-6][TOTAL_BID])
         {
          SellChance=SellChance-2;
         } 

        if(Pos[CurrentPos][TOTAL_BID]<Pos[CurrentPos-7][TOTAL_BID])
         {
          SellChance=SellChance-3;
         } 
                  
         //=====================================================================

        if(Pos[CurrentPos-1][TOTAL_ASK]>Pos[CurrentPos-2][TOTAL_ASK])
         {
          BuyChance=BuyChance+2;
         }           

        if(Pos[CurrentPos-1][TOTAL_ASK]>Pos[CurrentPos-3][TOTAL_ASK])
         {
          BuyChance=BuyChance+1;
         }           

        if(Pos[CurrentPos-1][TOTAL_ASK]>Pos[CurrentPos-4][TOTAL_ASK])
         {
          BuyChance=BuyChance+0;
         } 
         
        if(Pos[CurrentPos-1][TOTAL_ASK]>Pos[CurrentPos-5][TOTAL_ASK])
         {
          BuyChance=BuyChance-1;
         } 
                  
        if(Pos[CurrentPos-1][TOTAL_ASK]>Pos[CurrentPos-6][TOTAL_ASK])
         {
          BuyChance=BuyChance-2;
         } 
         
        if(Pos[CurrentPos-1][TOTAL_BID]<Pos[CurrentPos-2][TOTAL_BID])
         {
          SellChance=SellChance+2;
         }           

        if(Pos[CurrentPos-1][TOTAL_BID]<Pos[CurrentPos-3][TOTAL_BID])
         {
          SellChance=SellChance+1;
         }           

        if(Pos[CurrentPos-1][TOTAL_BID]<Pos[CurrentPos-4][TOTAL_BID])
         {
          SellChance=SellChance+0;
         }           

        if(Pos[CurrentPos-1][TOTAL_BID]<Pos[CurrentPos-5][TOTAL_BID])
         {
          SellChance=SellChance-1;
         }           
         
        if(Pos[CurrentPos-1][TOTAL_BID]<Pos[CurrentPos-6][TOTAL_BID])
         {
          SellChance=SellChance-2;
         }   
         
         //=====================================================================

        if(Pos[CurrentPos-2][TOTAL_ASK]>Pos[CurrentPos-3][TOTAL_ASK])
         {
          BuyChance=BuyChance+1;
         }   

        if(Pos[CurrentPos-2][TOTAL_ASK]>Pos[CurrentPos-4][TOTAL_ASK])
         {
          BuyChance=BuyChance+0;
         }   
         
        if(Pos[CurrentPos-2][TOTAL_ASK]>Pos[CurrentPos-5][TOTAL_ASK])
         {
          BuyChance=BuyChance-1;
         }   
         
        if(Pos[CurrentPos-2][TOTAL_BID]<Pos[CurrentPos-3][TOTAL_BID])
         {
          SellChance=SellChance+1;
         } 

        if(Pos[CurrentPos-2][TOTAL_BID]<Pos[CurrentPos-4][TOTAL_BID])
         {
          SellChance=SellChance+0;
         } 

        if(Pos[CurrentPos-2][TOTAL_BID]<Pos[CurrentPos-5][TOTAL_BID])
         {
          SellChance=SellChance-1;
         } 
         
         //=====================================================================

        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-1][VALUE_ASK])
         {
          BuyChance=BuyChance+3;
         }           

        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-2][VALUE_ASK])
         {
          BuyChance=BuyChance+2;
         }           
         
        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-3][VALUE_ASK])
         {
          BuyChance=BuyChance+1;
         }           
                  
        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-4][VALUE_ASK])
         {
          BuyChance=BuyChance+0;
         }  
         
        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-5][VALUE_ASK])
         {
          BuyChance=BuyChance-1;
         }  
                  
        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-6][VALUE_ASK])
         {
          BuyChance=BuyChance-2;
         } 

        if(Pos[CurrentPos][VALUE_ASK]>Pos[CurrentPos-7][VALUE_ASK])
         {
          BuyChance=BuyChance-3;
         }
                  
        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-1][VALUE_BID])
         {
          SellChance=SellChance+3;
         }           

        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-2][VALUE_BID])
         {
          SellChance=SellChance+2;
         }    

        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-3][VALUE_BID])
         {
          SellChance=SellChance+1;
         }    

        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-4][VALUE_BID])
         {
          SellChance=SellChance+0;
         }

        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-5][VALUE_BID])
         {
          SellChance=SellChance-1;
         }

        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-6][VALUE_BID])
         {
          SellChance=SellChance-2;
         }
                  
        if(Pos[CurrentPos][VALUE_BID]<Pos[CurrentPos-7][VALUE_BID])
         {
          SellChance=SellChance-3;
         }
         
         //=====================================================================
         
        if(Pos[CurrentPos-1][VALUE_ASK]>Pos[CurrentPos-2][VALUE_ASK])
         {
          BuyChance=BuyChance+2;
         }           
         
        if(Pos[CurrentPos-1][VALUE_ASK]>Pos[CurrentPos-3][VALUE_ASK])
         {
          BuyChance=BuyChance+1;
         }           

        if(Pos[CurrentPos-1][VALUE_ASK]>Pos[CurrentPos-4][VALUE_ASK])
         {
          BuyChance=BuyChance+0;
         }           

        if(Pos[CurrentPos-1][VALUE_ASK]>Pos[CurrentPos-5][VALUE_ASK])
         {
          BuyChance=BuyChance-1;
         }           

        if(Pos[CurrentPos-1][VALUE_ASK]>Pos[CurrentPos-6][VALUE_ASK])
         {
          BuyChance=BuyChance-2;
         }
         
        if(Pos[CurrentPos-1][VALUE_BID]<Pos[CurrentPos-2][VALUE_BID])
         {
          SellChance=SellChance+2;
         }

        if(Pos[CurrentPos-1][VALUE_BID]<Pos[CurrentPos-3][VALUE_BID])
         {
          SellChance=SellChance+1;
         }
                  
        if(Pos[CurrentPos-1][VALUE_BID]<Pos[CurrentPos-4][VALUE_BID])
         {
          SellChance=SellChance+0;
         }
         
        if(Pos[CurrentPos-1][VALUE_BID]<Pos[CurrentPos-5][VALUE_BID])
         {
          SellChance=SellChance-1;
         }
                  
        if(Pos[CurrentPos-1][VALUE_BID]<Pos[CurrentPos-6][VALUE_BID])
         {
          SellChance=SellChance-2;
         }
         
         //=====================================================================
           
        if(Pos[CurrentPos-2][VALUE_ASK]>Pos[CurrentPos-3][VALUE_ASK])
         {
          BuyChance=BuyChance+1;
         }
         
        if(Pos[CurrentPos-2][VALUE_ASK]>Pos[CurrentPos-4][VALUE_ASK])
         {
          BuyChance=BuyChance+0;
         }
         
        if(Pos[CurrentPos-2][VALUE_ASK]>Pos[CurrentPos-5][VALUE_ASK])
         {
          BuyChance=BuyChance-1;
         }
         
        if(Pos[CurrentPos-2][VALUE_BID]<Pos[CurrentPos-3][VALUE_BID])
         {
          SellChance=SellChance+1;
         }

        if(Pos[CurrentPos-2][VALUE_BID]<Pos[CurrentPos-4][VALUE_BID])
         {
          SellChance=SellChance+0;
         }

        if(Pos[CurrentPos-2][VALUE_BID]<Pos[CurrentPos-5][VALUE_BID])
         {
          SellChance=SellChance-1;
         }
         
         //=====================================================================

        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-1][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+3;
         }
         
        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-2][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+2;
         }                   

        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-3][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+1;
         }
         
        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-4][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+0;
         }
         
        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-5][TOTAL_CLOSE])
         {
          BuyChance=BuyChance-1;
         }
                  
        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-6][TOTAL_CLOSE])
         {
          BuyChance=BuyChance-2;
         }
         
        if(Pos[CurrentPos][TOTAL_CLOSE]>Pos[CurrentPos-7][TOTAL_CLOSE])
         {
          BuyChance=BuyChance-3;
         }
         
        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-1][TOTAL_CLOSE])
         {
          SellChance=SellChance+3;
         }  

        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-2][TOTAL_CLOSE])
         {
          SellChance=SellChance+2;
         }
                  
        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-3][TOTAL_CLOSE])
         {
          SellChance=SellChance+1;
         }                  

        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-4][TOTAL_CLOSE])
         {
          SellChance=SellChance+0;
         }             
         
        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-5][TOTAL_CLOSE])
         {
          SellChance=SellChance-1;
         }                      

        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-6][TOTAL_CLOSE])
         {
          SellChance=SellChance-2;
         }
            
        if(Pos[CurrentPos][TOTAL_CLOSE]<Pos[CurrentPos-7][TOTAL_CLOSE])
         {
          SellChance=SellChance-3;
         }
         
         //=====================================================================
                  
        if(Pos[CurrentPos-1][TOTAL_CLOSE]>Pos[CurrentPos-2][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+2;
         }           

        if(Pos[CurrentPos-1][TOTAL_CLOSE]>Pos[CurrentPos-3][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+1;
         }
         
        if(Pos[CurrentPos-1][TOTAL_CLOSE]>Pos[CurrentPos-4][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+0;
         }
        
        if(Pos[CurrentPos-1][TOTAL_CLOSE]>Pos[CurrentPos-5][TOTAL_CLOSE])
         {
          BuyChance=BuyChance-1;
         }
                   
        if(Pos[CurrentPos-1][TOTAL_CLOSE]>Pos[CurrentPos-6][TOTAL_CLOSE])
         {
          BuyChance=BuyChance-2;
         }
                  
        if(Pos[CurrentPos-1][TOTAL_CLOSE]<Pos[CurrentPos-2][TOTAL_CLOSE])
         {
          SellChance=SellChance+2;
         }

        if(Pos[CurrentPos-1][TOTAL_CLOSE]<Pos[CurrentPos-3][TOTAL_CLOSE])
         {
          SellChance=SellChance+1;
         }
         
        if(Pos[CurrentPos-1][TOTAL_CLOSE]<Pos[CurrentPos-4][TOTAL_CLOSE])
         {
          SellChance=SellChance+0;
         }

        if(Pos[CurrentPos-1][TOTAL_CLOSE]<Pos[CurrentPos-5][TOTAL_CLOSE])
         {
          SellChance=SellChance-1;
         }

        if(Pos[CurrentPos-1][TOTAL_CLOSE]<Pos[CurrentPos-6][TOTAL_CLOSE])
         {
          SellChance=SellChance-2;
         }         
            
         //=====================================================================         
         
        if(Pos[CurrentPos-2][TOTAL_CLOSE]>Pos[CurrentPos-3][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+1;
         }

        if(Pos[CurrentPos-2][TOTAL_CLOSE]>Pos[CurrentPos-4][TOTAL_CLOSE])
         {
          BuyChance=BuyChance+0;
         }

        if(Pos[CurrentPos-2][TOTAL_CLOSE]>Pos[CurrentPos-5][TOTAL_CLOSE])
         {
          BuyChance=BuyChance-1;
         }
         
        if(Pos[CurrentPos-2][TOTAL_CLOSE]<Pos[CurrentPos-3][TOTAL_CLOSE])
         {
          SellChance=SellChance+1;
         }

        if(Pos[CurrentPos-2][TOTAL_CLOSE]<Pos[CurrentPos-4][TOTAL_CLOSE])
         {
          SellChance=SellChance+0;
         }
         
        if(Pos[CurrentPos-2][TOTAL_CLOSE]<Pos[CurrentPos-5][TOTAL_CLOSE])
         {
          SellChance=SellChance-1;
         }

         //=====================================================================    

        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-1][VALUE_CLOSE])
         {
          BuyChance=BuyChance+3;
         }           

        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-2][VALUE_CLOSE])
         {
          BuyChance=BuyChance+2;
         }           

        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-3][VALUE_CLOSE])
         {
          BuyChance=BuyChance+1;
         }           
         
        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-4][VALUE_CLOSE])
         {
          BuyChance=BuyChance+0;
         }       
         
        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-5][VALUE_CLOSE])
         {
          BuyChance=BuyChance-1;
         }       

        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-6][VALUE_CLOSE])
         {
          BuyChance=BuyChance-2;
         } 
                  
        if(Pos[CurrentPos][VALUE_CLOSE]>Pos[CurrentPos-7][VALUE_CLOSE])
         {
          BuyChance=BuyChance-3;
         } 
         
        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-1][VALUE_CLOSE])
         {
          SellChance=SellChance+3;
         }  

        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-2][VALUE_CLOSE])
         {
          SellChance=SellChance+2;
         }  

        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-3][VALUE_CLOSE])
         {
          SellChance=SellChance+1;
         }  
         
        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-4][VALUE_CLOSE])
         {
          SellChance=SellChance+0;
         }  

        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-5][VALUE_CLOSE])
         {
          SellChance=SellChance-1;
         }  

        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-6][VALUE_CLOSE])
         {
          SellChance=SellChance-2;
         }  

        if(Pos[CurrentPos][VALUE_CLOSE]<Pos[CurrentPos-7][VALUE_CLOSE])
         {
          SellChance=SellChance-3;
         }  
         
         //=====================================================================    

        if(Pos[CurrentPos-1][VALUE_CLOSE]>Pos[CurrentPos-2][VALUE_CLOSE])
         {
          BuyChance=BuyChance+2;
         }           

        if(Pos[CurrentPos-1][VALUE_CLOSE]>Pos[CurrentPos-3][VALUE_CLOSE])
         {
          BuyChance=BuyChance+1;
         }           

        if(Pos[CurrentPos-1][VALUE_CLOSE]>Pos[CurrentPos-4][VALUE_CLOSE])
         {
          BuyChance=BuyChance+0;
         }   
         
        if(Pos[CurrentPos-1][VALUE_CLOSE]>Pos[CurrentPos-5][VALUE_CLOSE])
         {
          BuyChance=BuyChance-1;
         }           
                 
        if(Pos[CurrentPos-1][VALUE_CLOSE]>Pos[CurrentPos-6][VALUE_CLOSE])
         {
          BuyChance=BuyChance-2;
         }     
         
        if(Pos[CurrentPos-1][VALUE_CLOSE]<Pos[CurrentPos-2][VALUE_CLOSE])
         {
          SellChance=SellChance+2;
         } 

        if(Pos[CurrentPos-1][VALUE_CLOSE]<Pos[CurrentPos-3][VALUE_CLOSE])
         {
          SellChance=SellChance+1;
         } 

        if(Pos[CurrentPos-1][VALUE_CLOSE]<Pos[CurrentPos-4][VALUE_CLOSE])
         {
          SellChance=SellChance+0;
         }          
         
        if(Pos[CurrentPos-1][VALUE_CLOSE]<Pos[CurrentPos-5][VALUE_CLOSE])
         {
          SellChance=SellChance-1;
         }          

        if(Pos[CurrentPos-1][VALUE_CLOSE]<Pos[CurrentPos-6][VALUE_CLOSE])
         {
          SellChance=SellChance-2;
         }    
       
         //=====================================================================    
         
        if(Pos[CurrentPos-2][VALUE_CLOSE]>Pos[CurrentPos-3][VALUE_CLOSE])
         {
          BuyChance=BuyChance+1;
         }

        if(Pos[CurrentPos-2][VALUE_CLOSE]>Pos[CurrentPos-4][VALUE_CLOSE])
         {
          BuyChance=BuyChance+0;
         }
         
        if(Pos[CurrentPos-2][VALUE_CLOSE]>Pos[CurrentPos-5][VALUE_CLOSE])
         {
          BuyChance=BuyChance-1;
         }
         
        if(Pos[CurrentPos-2][VALUE_CLOSE]<Pos[CurrentPos-3][VALUE_CLOSE])
         {
          SellChance=SellChance+1;
         }

        if(Pos[CurrentPos-2][VALUE_CLOSE]<Pos[CurrentPos-4][VALUE_CLOSE])
         {
          SellChance=SellChance+0;
         }
        
        if(Pos[CurrentPos-2][VALUE_CLOSE]<Pos[CurrentPos-5][VALUE_CLOSE])
         {
          SellChance=SellChance-1;
         }         
     
         //=====================================================================    

        BlockFunctionAccess(); 
        }
       else
        {
         if(Warning=="Show")
          {
           Alert("FunctionAccess/TimeAccess Blocked 'CheckChances()'");
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
           if(Pos[CurrentPos][RVI_DIFFERENTION]>RVI_UNSAFE_DIFFERENTION)
            {
             if(Pos[CurrentPos][RVI_SIGNAL]>Pos[CurrentPos-1][RVI_MAIN]
                                           ||
                Pos[CurrentPos][RVI_SIGNAL]>Pos[CurrentPos-2][RVI_MAIN]
                                           ||
                Pos[CurrentPos][RVI_SIGNAL]>Pos[CurrentPos-3][RVI_MAIN]
                                           ||
                Pos[CurrentPos][RVI_SIGNAL]<Pos[CurrentPos-1][RVI_MAIN] 
                                           ||
                Pos[CurrentPos][RVI_SIGNAL]<Pos[CurrentPos-2][RVI_MAIN]
                                           ||
                Pos[CurrentPos][RVI_SIGNAL]<Pos[CurrentPos-3][RVI_MAIN])
              {                                                                                     
               if(BuyChance>=MinChance||SellChance>=MinChance)
                {
                 if(BuyChance>SellChance)
                  {
                   if(CSignalBuy==1&&CSignalSell==0)
                    {
                     TradeSystem=BUY;
                    }
                  }
                 if(SellChance>BuyChance)
                  {
                   if(CSignalBuy==0&&CSignalSell==1)
                    {
                     TradeSystem=SELL;
                    }
                  }
                 if(BuyChance==SellChance)
                  {
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
             Alert(BuyChance);
            }  
           if(TradeSystem==SELL)
            {
             ClosePositions();
             //---------------------------------------------------------------------------------------------------------
             OrderSend(Symbol(),OP_SELL,Lots,Bid,SlipPage,0,0,Commentation,Magic,Expiration,Green);
             TradeSystem=false;
             Alert(SellChance);
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
                 ClosePositions();
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
                 ClosePositions();
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
               Pos[PosValue][TOTAL_RSI]=false;               
               //------------------------------               
               Pos[PosValue][RVI_MAIN]=false;
               Pos[PosValue][RVI_SIGNAL]=false;
               Pos[PosValue][RVI_DIFFERENTION]=false;                
               //------------------------------ 
               Pos[PosValue][RSI]=false;                  
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
                 Pos[PosValue][TOTAL_RSI]=false;               
                 //------------------------------
                 Pos[PosValue][RVI_MAIN]=false;
                 Pos[PosValue][RVI_SIGNAL]=false;
                 Pos[PosValue][RVI_DIFFERENTION]=false;                
                 //------------------------------ 
                 Pos[PosValue][RSI]=false;                  
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
               Pos[PosValue][TOTAL_RSI]=false;
               //------------------------------                
               Pos[PosValue][RVI_MAIN]=false;
               Pos[PosValue][RVI_SIGNAL]=false;
               Pos[PosValue][RVI_DIFFERENTION]=false;                
               //------------------------------
               Pos[PosValue][RSI]=false;                
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
                 Pos[PosValue][TOTAL_RSI]=false;
                 //------------------------------                  
                 Pos[PosValue][RVI_MAIN]=false;
                 Pos[PosValue][RVI_SIGNAL]=false;
                 Pos[PosValue][RVI_DIFFERENTION]=false;                
                 //------------------------------ 
                 Pos[PosValue][RSI]=false;                
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
                 Pos[PosValue][TOTAL_RSI]=false;
                 //------------------------------                 
                 Pos[PosValue][RVI_MAIN]=false;
                 Pos[PosValue][RVI_SIGNAL]=false;
                 Pos[PosValue][RVI_DIFFERENTION]=false;                
                 //------------------------------ 
                 Pos[PosValue][RSI]=false;                
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
   HideTestIndicators(true);
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
   CalculateSpecialValues();
   //--------------------------------
   AllowFunctionAccess();                  
   AddPositions();                  
   //--------------------------------
   return(0);
  }

int start()
  {
   HideTestIndicators(true);
   //----------------------------
   AllowFunctionAccess();                  
   CheckTime();                  
   //----------------------------
   AllowFunctionAccess();
   CalculateBaseValues();
   //----------------------------
   AllowFunctionAccess();
   CalculateSpecialValues();
   //----------------------------
   AllowFunctionAccess();
   AddPositions();
   //----------------------------
   AllowFunctionAccess();
   CalculateExtendedValues();
   //----------------------------
   AllowFunctionAccess();
   CheckChances();
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
   HideTestIndicators(true);
   //----------------------------------
   AllowFunctionAccess();
   RemovePositions();
   //----------------------------------
   return(0);
  }
//-----------------------------------------------------------------------------------------------------------------------------
//=============================================================================================================================