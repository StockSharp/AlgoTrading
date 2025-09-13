
      /*##################################################################################
      #  PRICE QUOTES BY EMAIL                                                           #
      #                                                                                  #
      #  The purpose of this EA is to receive by email the price quotes of the symbols   #
      #  selected, as well as some information about your account. You can insert        #
      #  up to 50 different symbols of any type (FOREX, CFD).                            #
      #                                                                                  #
      #  ATTENTION: This EA must be placed on an active chart and it will only work      #
      #  when the market is opened and the chart is producing new ticks. You also need   #
      #  to enable and configure the email on MT4 (TOOLS >> OPTIONS >> EMAIL)            #
      #                                                                                  #
      ##################################################################################*/
#property copyright "BLACKHAWK"
#include <stderror.mqh>
#include <stdlib.mqh>
static bool SendNow;

// ######## INTRODUCE THE LAPSE OF TIME (MINUTES) FOR THE EMAIL FRECUENCY ######## 

int SendQuotes =   0; // --> Send quotes each X minutes. Please Specify the minutes and compile. (Not less than 1 minute).
                       // --> If you want to stop sending emails just put 0 (zero) minutes and compile. 


// ########  INTRODUCE BELOW THE NAMES OF THE SYMBOLS THAT YOU WANT TO INCLUDE IN YOUR EMAIL ######## 

/* If you want less than 50 symbols, just leave empty values ("").  If the symbol name introduced 
   is not correctly written or does not exist, it will be automatically replaced by an empty value.*/
                        
string   S1       = "EURUSD";
string   S2       = "GBPUSD";
string   S3       = "USDCHF";
string   S4       = "USDJPY";
string   S5       = "";
string   S6       = "";
string   S7       = "";
string   S8       = "";
string   S9       = "";
string   S10      = "";
string   S11      = "";
string   S12      = "";
string   S13      = "";
string   S14      = "";
string   S15      = "";
string   S16      = "";
string   S17      = "";
string   S18      = "";
string   S19      = "";
string   S20      = "";
string   S21      = "";
string   S22      = "";
string   S23      = "";
string   S24      = "";
string   S25      = "";
string   S26      = "";
string   S27      = "";
string   S28      = "";
string   S29      = "";
string   S30      = "";
string   S31      = "";
string   S32      = "";
string   S33      = "";
string   S34      = "";
string   S35      = "";
string   S36      = "";
string   S37      = "";
string   S38      = "";
string   S39      = "";
string   S41      = "";
string   S40      = "";
string   S42      = "";
string   S43      = "";
string   S44      = "";
string   S45      = "";
string   S46      = "";
string   S47      = "";
string   S48      = "";
string   S49      = "";
string   S50      = "";



//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

// ATTENTION: Unless you have good knowledges about MQL don't touch the
// code below. If you are going to touch the code below, please make a 
// copy of the original code. 

int start()
  {

/* 
The EA wil work only once and then it will sleep during the minutes selected. 
*/
int sleep;
if (SendQuotes < 1)   {sleep = 86400000; SendNow = false;}
if (SendQuotes >= 1)  {sleep = SendQuotes * 60 * 1000; SendNow = True;}


while (IsStopped() == False && SendNow == True)         
         {     
RefreshRates();


/* 
If one symbol has been written incorrectly the EA will stop working. To avoid this we must
check if all the symbols are correctly written, and one symbol is not correct, we must rename 
the symbol as empty symbol (""). One way do this is by checking the Lotsize. The Lotsize can
never be zero, so if Lotsize is zero, the symbol does not exist or it has been written incorrectly
*/
if(MarketInfo(S1,MODE_LOTSIZE)==0) S1=""; if(S1!=""){
string S1_name       = "\n" + S1 + ": ";
int    S1_Digits     = MarketInfo(S1,MODE_DIGITS);
double S1_Price      = MarketInfo(S1,MODE_BID);
string S1_Pr         = DoubleToStr(S1_Price,S1_Digits); 
double S1_Change     = (((S1_Price / iClose(S1,PERIOD_D1,1))-1)*100);
string S1_Sign;      if(S1_Change>0)S1_Sign = "+";
string S1_Ch         = " (" + S1_Sign + DoubleToStr(S1_Change,2) + "%)";}
else{S1_name =""; S1_Pr = ""; S1_Ch = "";}

if(MarketInfo(S2,MODE_LOTSIZE)==0) S2=""; if(S2!=""){
string S2_name       = "\n" + S2 + ": ";
int    S2_Digits     = MarketInfo(S2,MODE_DIGITS);
double S2_Price      = MarketInfo(S2,MODE_BID);
string S2_Pr         = DoubleToStr(S2_Price,S2_Digits); 
double S2_Change     = (((S2_Price / iClose(S2,PERIOD_D1,1))-1)*100);
string S2_Sign;      if(S2_Change>0)S2_Sign = "+";
string S2_Ch         = " (" + S2_Sign + DoubleToStr(S2_Change,2) + "%)";}
else{S2_name =""; S2_Pr = ""; S2_Ch = "";}

if(MarketInfo(S3,MODE_LOTSIZE)==0) S3=""; if(S3!=""){
string S3_name       = "\n" + S3 + ": ";
int    S3_Digits     = MarketInfo(S3,MODE_DIGITS);
double S3_Price      = MarketInfo(S3,MODE_BID);
string S3_Pr         = DoubleToStr(S3_Price,S3_Digits); 
double S3_Change     = (((S3_Price / iClose(S3,PERIOD_D1,1))-1)*100);
string S3_Sign;      if(S3_Change>0)S3_Sign = "+";
string S3_Ch         = " (" + S3_Sign + DoubleToStr(S3_Change,2) + "%)";}
else{S3_name = "";S3_Pr = ""; S3_Ch = "";}

if(MarketInfo(S4,MODE_LOTSIZE)==0) S4=""; if(S4!=""){
string S4_name       = "\n" + S4 + ": ";
int    S4_Digits     = MarketInfo(S4,MODE_DIGITS);
double S4_Price      = MarketInfo(S4,MODE_BID);
string S4_Pr         = DoubleToStr(S4_Price,S4_Digits); 
double S4_Change     = (((S4_Price / iClose(S4,PERIOD_D1,1))-1)*100);
string S4_Sign;      if(S4_Change>0)S4_Sign = "+";
string S4_Ch         = " (" + S4_Sign + DoubleToStr(S4_Change,2) + "%)";}
else{S4_name = ""; S4_Pr = ""; S4_Ch = "";}

if(MarketInfo(S5,MODE_LOTSIZE)==0) S5=""; if(S5!=""){
string S5_name       = "\n" + S5 + ": ";
int    S5_Digits     = MarketInfo(S5,MODE_DIGITS);
double S5_Price      = MarketInfo(S5,MODE_BID);
string S5_Pr         = DoubleToStr(S5_Price,S5_Digits); 
double S5_Change     = (((S5_Price / iClose(S5,PERIOD_D1,1))-1)*100);
string S5_Sign;      if(S5_Change>0)S5_Sign = "+";
string S5_Ch         = " (" + S5_Sign + DoubleToStr(S5_Change,2) + "%)";}
else{S5_name =""; S5_Pr = ""; S5_Ch = "";}

if(MarketInfo(S6,MODE_LOTSIZE)==0) S6=""; if(S6!=""){
string S6_name       = "\n" + S6 + ": ";
int    S6_Digits     = MarketInfo(S6,MODE_DIGITS);
double S6_Price      = MarketInfo(S6,MODE_BID);
string S6_Pr         = DoubleToStr(S6_Price,S6_Digits); 
double S6_Change     = (((S6_Price / iClose(S6,PERIOD_D1,1))-1)*100);
string S6_Sign;      if(S6_Change>0)S6_Sign = "+";
string S6_Ch         = " (" + S6_Sign + DoubleToStr(S6_Change,2) + "%)";}
else{S6_name =""; S6_Pr = ""; S6_Ch = "";}

if(MarketInfo(S7,MODE_LOTSIZE)==0) S7=""; if(S7!=""){
string S7_name       = "\n" + S7 + ": ";
int    S7_Digits     = MarketInfo(S7,MODE_DIGITS);
double S7_Price      = MarketInfo(S7,MODE_BID);
string S7_Pr         = DoubleToStr(S7_Price,S7_Digits); 
double S7_Change     = (((S7_Price / iClose(S7,PERIOD_D1,1))-1)*100);
string S7_Sign;      if(S7_Change>0)S7_Sign = "+";
string S7_Ch         = " (" + S7_Sign + DoubleToStr(S7_Change,2) + "%)";}
else{S7_name = "";S7_Pr = ""; S7_Ch = "";}

if(MarketInfo(S8,MODE_LOTSIZE)==0) S8=""; if(S8!=""){
string S8_name       = "\n" + S8 + ": ";
int    S8_Digits     = MarketInfo(S8,MODE_DIGITS);
double S8_Price      = MarketInfo(S8,MODE_BID);
string S8_Pr         = DoubleToStr(S8_Price,S8_Digits); 
double S8_Change     = (((S8_Price / iClose(S8,PERIOD_D1,1))-1)*100);
string S8_Sign;      if(S8_Change>0)S8_Sign = "+";
string S8_Ch         = " (" + S8_Sign + DoubleToStr(S8_Change,2) + "%)";}
else{S8_name = ""; S8_Pr = ""; S8_Ch = "";}

if(MarketInfo(S9,MODE_LOTSIZE)==0) S9=""; if(S9!=""){
string S9_name       = "\n" + S9 + ": ";
int    S9_Digits     = MarketInfo(S9,MODE_DIGITS);
double S9_Price      = MarketInfo(S9,MODE_BID);
string S9_Pr         = DoubleToStr(S9_Price,S9_Digits); 
double S9_Change     = (((S9_Price / iClose(S9,PERIOD_D1,1))-1)*100);
string S9_Sign;      if(S9_Change>0)S9_Sign = "+";
string S9_Ch         = " (" + S9_Sign + DoubleToStr(S9_Change,2) + "%)";}
else{S9_name =""; S9_Pr = ""; S9_Ch = "";}

if(MarketInfo(S10,MODE_LOTSIZE)==0) S10=""; if(S10!=""){
string S10_name       = "\n" + S10 + ": ";
int    S10_Digits     = MarketInfo(S10,MODE_DIGITS);
double S10_Price      = MarketInfo(S10,MODE_BID);
string S10_Pr         = DoubleToStr(S10_Price,S10_Digits); 
double S10_Change     = (((S10_Price / iClose(S10,PERIOD_D1,1))-1)*100);
string S10_Sign;      if(S10_Change>0)S10_Sign = "+";
string S10_Ch         = " (" + S10_Sign + DoubleToStr(S10_Change,2) + "%)";}
else{S10_name =""; S10_Pr = ""; S10_Ch = "";}

if(MarketInfo(S11,MODE_LOTSIZE)==0) S11=""; if(S11!=""){
string S11_name       = "\n" + S11 + ": ";
int    S11_Digits     = MarketInfo(S11,MODE_DIGITS);
double S11_Price      = MarketInfo(S11,MODE_BID);
string S11_Pr         = DoubleToStr(S11_Price,S11_Digits); 
double S11_Change     = (((S11_Price / iClose(S11,PERIOD_D1,1))-1)*100);
string S11_Sign;      if(S11_Change>0)S11_Sign = "+";
string S11_Ch         = " (" + S11_Sign + DoubleToStr(S11_Change,2) + "%)";}
else{S11_name = "";S11_Pr = ""; S11_Ch = "";}

if(MarketInfo(S12,MODE_LOTSIZE)==0) S12=""; if(S12!=""){
string S12_name       = "\n" + S12 + ": ";
int    S12_Digits     = MarketInfo(S12,MODE_DIGITS);
double S12_Price      = MarketInfo(S12,MODE_BID);
string S12_Pr         = DoubleToStr(S12_Price,S12_Digits); 
double S12_Change     = (((S12_Price / iClose(S12,PERIOD_D1,1))-1)*100);
string S12_Sign;      if(S12_Change>0)S12_Sign = "+";
string S12_Ch         = " (" + S12_Sign + DoubleToStr(S12_Change,2) + "%)";}
else{S12_name = ""; S12_Pr = ""; S12_Ch = "";}

if(MarketInfo(S13,MODE_LOTSIZE)==0) S13=""; if(S13!=""){
string S13_name       = "\n" + S13 + ": ";
int    S13_Digits     = MarketInfo(S13,MODE_DIGITS);
double S13_Price      = MarketInfo(S13,MODE_BID);
string S13_Pr         = DoubleToStr(S13_Price,S13_Digits); 
double S13_Change     = (((S13_Price / iClose(S13,PERIOD_D1,1))-1)*100);
string S13_Sign;      if(S13_Change>0)S13_Sign = "+";
string S13_Ch         = " (" + S13_Sign + DoubleToStr(S13_Change,2) + "%)";}
else{S13_name =""; S13_Pr = ""; S13_Ch = "";}

if(MarketInfo(S14,MODE_LOTSIZE)==0) S14=""; if(S14!=""){
string S14_name       = "\n" + S14 + ": ";
int    S14_Digits     = MarketInfo(S14,MODE_DIGITS);
double S14_Price      = MarketInfo(S14,MODE_BID);
string S14_Pr         = DoubleToStr(S14_Price,S14_Digits); 
double S14_Change     = (((S14_Price / iClose(S14,PERIOD_D1,1))-1)*100);
string S14_Sign;      if(S14_Change>0)S14_Sign = "+";
string S14_Ch         = " (" + S14_Sign + DoubleToStr(S14_Change,2) + "%)";}
else{S14_name =""; S14_Pr = ""; S14_Ch = "";}

if(MarketInfo(S15,MODE_LOTSIZE)==0) S15=""; if(S15!=""){
string S15_name       = "\n" + S15 + ": ";
int    S15_Digits     = MarketInfo(S15,MODE_DIGITS);
double S15_Price      = MarketInfo(S15,MODE_BID);
string S15_Pr         = DoubleToStr(S15_Price,S15_Digits); 
double S15_Change     = (((S15_Price / iClose(S15,PERIOD_D1,1))-1)*100);
string S15_Sign;      if(S15_Change>0)S15_Sign = "+";
string S15_Ch         = " (" + S15_Sign + DoubleToStr(S15_Change,2) + "%)";}
else{S15_name = "";S15_Pr = ""; S15_Ch = "";}

if(MarketInfo(S16,MODE_LOTSIZE)==0) S16=""; if(S16!=""){
string S16_name       = "\n" + S16 + ": ";
int    S16_Digits     = MarketInfo(S16,MODE_DIGITS);
double S16_Price      = MarketInfo(S16,MODE_BID);
string S16_Pr         = DoubleToStr(S16_Price,S16_Digits); 
double S16_Change     = (((S16_Price / iClose(S16,PERIOD_D1,1))-1)*100);
string S16_Sign;      if(S16_Change>0)S16_Sign = "+";
string S16_Ch         = " (" + S16_Sign + DoubleToStr(S16_Change,2) + "%)";}
else{S16_name = ""; S16_Pr = ""; S16_Ch = "";}

if(MarketInfo(S17,MODE_LOTSIZE)==0) S17=""; if(S17!=""){
string S17_name       = "\n" + S17 + ": ";
int    S17_Digits     = MarketInfo(S17,MODE_DIGITS);
double S17_Price      = MarketInfo(S17,MODE_BID);
string S17_Pr         = DoubleToStr(S17_Price,S17_Digits); 
double S17_Change     = (((S17_Price / iClose(S17,PERIOD_D1,1))-1)*100);
string S17_Sign;      if(S17_Change>0)S17_Sign = "+";
string S17_Ch         = " (" + S17_Sign + DoubleToStr(S17_Change,2) + "%)";}
else{S17_name =""; S17_Pr = ""; S17_Ch = "";}

if(MarketInfo(S18,MODE_LOTSIZE)==0) S18=""; if(S18!=""){
string S18_name       = "\n" + S18 + ": ";
int    S18_Digits     = MarketInfo(S18,MODE_DIGITS);
double S18_Price      = MarketInfo(S18,MODE_BID);
string S18_Pr         = DoubleToStr(S18_Price,S18_Digits); 
double S18_Change     = (((S18_Price / iClose(S18,PERIOD_D1,1))-1)*100);
string S18_Sign;      if(S18_Change>0)S18_Sign = "+";
string S18_Ch         = " (" + S18_Sign + DoubleToStr(S18_Change,2) + "%)";}
else{S18_name =""; S18_Pr = ""; S18_Ch = "";}

if(MarketInfo(S19,MODE_LOTSIZE)==0) S19=""; if(S19!=""){
string S19_name       = "\n" + S19 + ": ";
int    S19_Digits     = MarketInfo(S19,MODE_DIGITS);
double S19_Price      = MarketInfo(S19,MODE_BID);
string S19_Pr         = DoubleToStr(S19_Price,S19_Digits); 
double S19_Change     = (((S19_Price / iClose(S19,PERIOD_D1,1))-1)*100);
string S19_Sign;      if(S19_Change>0)S19_Sign = "+";
string S19_Ch         = " (" + S19_Sign + DoubleToStr(S19_Change,2) + "%)";}
else{S19_name = "";S19_Pr = ""; S19_Ch = "";}

if(MarketInfo(S20,MODE_LOTSIZE)==0) S20=""; if(S20!=""){
string S20_name       = "\n" + S20 + ": ";
int    S20_Digits     = MarketInfo(S20,MODE_DIGITS);
double S20_Price      = MarketInfo(S20,MODE_BID);
string S20_Pr         = DoubleToStr(S20_Price,S20_Digits); 
double S20_Change     = (((S20_Price / iClose(S20,PERIOD_D1,1))-1)*100);
string S20_Sign;      if(S20_Change>0)S20_Sign = "+";
string S20_Ch         = " (" + S20_Sign + DoubleToStr(S20_Change,2) + "%)";}
else{S20_name = ""; S20_Pr = ""; S20_Ch = "";}

if(MarketInfo(S21,MODE_LOTSIZE)==0) S21=""; if(S21!=""){
string S21_name       = "\n" + S21 + ": ";
int    S21_Digits     = MarketInfo(S21,MODE_DIGITS);
double S21_Price      = MarketInfo(S21,MODE_BID);
string S21_Pr         = DoubleToStr(S21_Price,S21_Digits); 
double S21_Change     = (((S21_Price / iClose(S21,PERIOD_D1,1))-1)*100);
string S21_Sign;      if(S21_Change>0)S21_Sign = "+";
string S21_Ch         = " (" + S21_Sign + DoubleToStr(S21_Change,2) + "%)";}
else{S21_name =""; S21_Pr = ""; S21_Ch = "";}

if(MarketInfo(S22,MODE_LOTSIZE)==0) S22=""; if(S22!=""){
string S22_name       = "\n" + S22 + ": ";
int    S22_Digits     = MarketInfo(S22,MODE_DIGITS);
double S22_Price      = MarketInfo(S22,MODE_BID);
string S22_Pr         = DoubleToStr(S22_Price,S22_Digits); 
double S22_Change     = (((S22_Price / iClose(S22,PERIOD_D1,1))-1)*100);
string S22_Sign;      if(S22_Change>0)S22_Sign = "+";
string S22_Ch         = " (" + S22_Sign + DoubleToStr(S22_Change,2) + "%)";}
else{S22_name =""; S22_Pr = ""; S22_Ch = "";}

if(MarketInfo(S23,MODE_LOTSIZE)==0) S23=""; if(S23!=""){
string S23_name       = "\n" + S23 + ": ";
int    S23_Digits     = MarketInfo(S23,MODE_DIGITS);
double S23_Price      = MarketInfo(S23,MODE_BID);
string S23_Pr         = DoubleToStr(S23_Price,S23_Digits); 
double S23_Change     = (((S23_Price / iClose(S23,PERIOD_D1,1))-1)*100);
string S23_Sign;      if(S23_Change>0)S23_Sign = "+";
string S23_Ch         = " (" + S23_Sign + DoubleToStr(S23_Change,2) + "%)";}
else{S23_name = "";S23_Pr = ""; S23_Ch = "";}

if(MarketInfo(S24,MODE_LOTSIZE)==0) S24=""; if(S24!=""){
string S24_name       = "\n" + S24 + ": ";
int    S24_Digits     = MarketInfo(S24,MODE_DIGITS);
double S24_Price      = MarketInfo(S24,MODE_BID);
string S24_Pr         = DoubleToStr(S24_Price,S24_Digits); 
double S24_Change     = (((S24_Price / iClose(S24,PERIOD_D1,1))-1)*100);
string S24_Sign;      if(S24_Change>0)S24_Sign = "+";
string S24_Ch         = " (" + S24_Sign + DoubleToStr(S24_Change,2) + "%)";}
else{S24_name = ""; S24_Pr = ""; S24_Ch = "";}

if(MarketInfo(S25,MODE_LOTSIZE)==0) S25=""; if(S25!=""){
string S25_name       = "\n" + S25 + ": ";
int    S25_Digits     = MarketInfo(S25,MODE_DIGITS);
double S25_Price      = MarketInfo(S25,MODE_BID);
string S25_Pr         = DoubleToStr(S25_Price,S25_Digits); 
double S25_Change     = (((S25_Price / iClose(S25,PERIOD_D1,1))-1)*100);
string S25_Sign;      if(S25_Change>0)S25_Sign = "+";
string S25_Ch         = " (" + S25_Sign + DoubleToStr(S25_Change,2) + "%)";}
else{S25_name =""; S25_Pr = ""; S25_Ch = "";}

if(MarketInfo(S26,MODE_LOTSIZE)==0) S26=""; if(S26!=""){
string S26_name       = "\n" + S26 + ": ";
int    S26_Digits     = MarketInfo(S26,MODE_DIGITS);
double S26_Price      = MarketInfo(S26,MODE_BID);
string S26_Pr         = DoubleToStr(S26_Price,S26_Digits); 
double S26_Change     = (((S26_Price / iClose(S26,PERIOD_D1,1))-1)*100);
string S26_Sign;      if(S26_Change>0)S26_Sign = "+";
string S26_Ch         = " (" + S26_Sign + DoubleToStr(S26_Change,2) + "%)";}
else{S26_name =""; S26_Pr = ""; S26_Ch = "";}

if(MarketInfo(S27,MODE_LOTSIZE)==0) S27=""; if(S27!=""){
string S27_name       = "\n" + S27 + ": ";
int    S27_Digits     = MarketInfo(S27,MODE_DIGITS);
double S27_Price      = MarketInfo(S27,MODE_BID);
string S27_Pr         = DoubleToStr(S27_Price,S27_Digits); 
double S27_Change     = (((S27_Price / iClose(S27,PERIOD_D1,1))-1)*100);
string S27_Sign;      if(S27_Change>0)S27_Sign = "+";
string S27_Ch         = " (" + S27_Sign + DoubleToStr(S27_Change,2) + "%)";}
else{S27_name =""; S27_Pr = ""; S27_Ch = "";}

if(MarketInfo(S28,MODE_LOTSIZE)==0) S28=""; if(S28!=""){
string S28_name       = "\n" + S28 + ": ";
int    S28_Digits     = MarketInfo(S28,MODE_DIGITS);
double S28_Price      = MarketInfo(S28,MODE_BID);
string S28_Pr         = DoubleToStr(S28_Price,S28_Digits); 
double S28_Change     = (((S28_Price / iClose(S28,PERIOD_D1,1))-1)*100);
string S28_Sign;      if(S28_Change>0)S28_Sign = "+";
string S28_Ch         = " (" + S28_Sign + DoubleToStr(S28_Change,2) + "%)";}
else{S28_name = "";S28_Pr = ""; S28_Ch = "";}

if(MarketInfo(S29,MODE_LOTSIZE)==0) S29=""; if(S29!=""){
string S29_name       = "\n" + S29 + ": ";
int    S29_Digits     = MarketInfo(S29,MODE_DIGITS);
double S29_Price      = MarketInfo(S29,MODE_BID);
string S29_Pr         = DoubleToStr(S29_Price,S29_Digits); 
double S29_Change     = (((S29_Price / iClose(S29,PERIOD_D1,1))-1)*100);
string S29_Sign;      if(S29_Change>0)S29_Sign = "+";
string S29_Ch         = " (" + S29_Sign + DoubleToStr(S29_Change,2) + "%)";}
else{S29_name = ""; S29_Pr = ""; S29_Ch = "";}

if(MarketInfo(S30,MODE_LOTSIZE)==0) S30=""; if(S30!=""){
string S30_name       = "\n" + S30 + ": ";
int    S30_Digits     = MarketInfo(S30,MODE_DIGITS);
double S30_Price      = MarketInfo(S30,MODE_BID);
string S30_Pr         = DoubleToStr(S30_Price,S30_Digits); 
double S30_Change     = (((S30_Price / iClose(S30,PERIOD_D1,1))-1)*100);
string S30_Sign;      if(S30_Change>0)S30_Sign = "+";
string S30_Ch         = " (" + S30_Sign + DoubleToStr(S30_Change,2) + "%)";}
else{S30_name =""; S30_Pr = ""; S30_Ch = "";}

if(MarketInfo(S31,MODE_LOTSIZE)==0) S31=""; if(S31!=""){
string S31_name       = "\n" + S31 + ": ";
int    S31_Digits     = MarketInfo(S31,MODE_DIGITS);
double S31_Price      = MarketInfo(S31,MODE_BID);
string S31_Pr         = DoubleToStr(S31_Price,S31_Digits); 
double S31_Change     = (((S31_Price / iClose(S31,PERIOD_D1,1))-1)*100);
string S31_Sign;      if(S31_Change>0)S31_Sign = "+";
string S31_Ch         = " (" + S31_Sign + DoubleToStr(S31_Change,2) + "%)";}
else{S31_name =""; S31_Pr = ""; S31_Ch = "";}

if(MarketInfo(S32,MODE_LOTSIZE)==0) S32=""; if(S32!=""){
string S32_name       = "\n" + S32 + ": ";
int    S32_Digits     = MarketInfo(S32,MODE_DIGITS);
double S32_Price      = MarketInfo(S32,MODE_BID);
string S32_Pr         = DoubleToStr(S32_Price,S32_Digits); 
double S32_Change     = (((S32_Price / iClose(S32,PERIOD_D1,1))-1)*100);
string S32_Sign;      if(S32_Change>0)S32_Sign = "+";
string S32_Ch         = " (" + S32_Sign + DoubleToStr(S32_Change,2) + "%)";}
else{S32_name = "";S32_Pr = ""; S32_Ch = "";}

if(MarketInfo(S33,MODE_LOTSIZE)==0) S33=""; if(S33!=""){
string S33_name       = "\n" + S33 + ": ";
int    S33_Digits     = MarketInfo(S33,MODE_DIGITS);
double S33_Price      = MarketInfo(S33,MODE_BID);
string S33_Pr         = DoubleToStr(S33_Price,S33_Digits); 
double S33_Change     = (((S33_Price / iClose(S33,PERIOD_D1,1))-1)*100);
string S33_Sign;      if(S33_Change>0)S33_Sign = "+";
string S33_Ch         = " (" + S33_Sign + DoubleToStr(S33_Change,2) + "%)";}
else{S33_name = ""; S33_Pr = ""; S33_Ch = "";}

if(MarketInfo(S34,MODE_LOTSIZE)==0) S34=""; if(S34!=""){
string S34_name       = "\n" + S34 + ": ";
int    S34_Digits     = MarketInfo(S34,MODE_DIGITS);
double S34_Price      = MarketInfo(S34,MODE_BID);
string S34_Pr         = DoubleToStr(S34_Price,S34_Digits); 
double S34_Change     = (((S34_Price / iClose(S34,PERIOD_D1,1))-1)*100);
string S34_Sign;      if(S34_Change>0)S34_Sign = "+";
string S34_Ch         = " (" + S34_Sign + DoubleToStr(S34_Change,2) + "%)";}
else{S34_name =""; S34_Pr = ""; S34_Ch = "";}

if(MarketInfo(S35,MODE_LOTSIZE)==0) S35=""; if(S35!=""){
string S35_name       = "\n" + S35 + ": ";
int    S35_Digits     = MarketInfo(S35,MODE_DIGITS);
double S35_Price      = MarketInfo(S35,MODE_BID);
string S35_Pr         = DoubleToStr(S35_Price,S35_Digits); 
double S35_Change     = (((S35_Price / iClose(S35,PERIOD_D1,1))-1)*100);
string S35_Sign;      if(S35_Change>0)S35_Sign = "+";
string S35_Ch         = " (" + S35_Sign + DoubleToStr(S35_Change,2) + "%)";}
else{S35_name =""; S35_Pr = ""; S35_Ch = "";}

if(MarketInfo(S36,MODE_LOTSIZE)==0) S36=""; if(S36!=""){
string S36_name       = "\n" + S36 + ": ";
int    S36_Digits     = MarketInfo(S36,MODE_DIGITS);
double S36_Price      = MarketInfo(S36,MODE_BID);
string S36_Pr         = DoubleToStr(S36_Price,S36_Digits); 
double S36_Change     = (((S36_Price / iClose(S36,PERIOD_D1,1))-1)*100);
string S36_Sign;      if(S36_Change>0)S36_Sign = "+";
string S36_Ch         = " (" + S36_Sign + DoubleToStr(S36_Change,2) + "%)";}
else{S36_name =""; S36_Pr = ""; S36_Ch = "";}

if(MarketInfo(S37,MODE_LOTSIZE)==0) S37=""; if(S37!=""){
string S37_name       = "\n" + S37 + ": ";
int    S37_Digits     = MarketInfo(S37,MODE_DIGITS);
double S37_Price      = MarketInfo(S37,MODE_BID);
string S37_Pr         = DoubleToStr(S37_Price,S37_Digits); 
double S37_Change     = (((S37_Price / iClose(S37,PERIOD_D1,1))-1)*100);
string S37_Sign;      if(S37_Change>0)S37_Sign = "+";
string S37_Ch         = " (" + S37_Sign + DoubleToStr(S37_Change,2) + "%)";}
else{S37_name = "";S37_Pr = ""; S37_Ch = "";}

if(MarketInfo(S38,MODE_LOTSIZE)==0) S38=""; if(S38!=""){
string S38_name       = "\n" + S38 + ": ";
int    S38_Digits     = MarketInfo(S38,MODE_DIGITS);
double S38_Price      = MarketInfo(S38,MODE_BID);
string S38_Pr         = DoubleToStr(S38_Price,S38_Digits); 
double S38_Change     = (((S38_Price / iClose(S38,PERIOD_D1,1))-1)*100);
string S38_Sign;      if(S38_Change>0)S38_Sign = "+";
string S38_Ch         = " (" + S38_Sign + DoubleToStr(S38_Change,2) + "%)";}
else{S38_name = ""; S38_Pr = ""; S38_Ch = "";}

if(MarketInfo(S39,MODE_LOTSIZE)==0) S39=""; if(S39!=""){
string S39_name       = "\n" + S39 + ": ";
int    S39_Digits     = MarketInfo(S39,MODE_DIGITS);
double S39_Price      = MarketInfo(S39,MODE_BID);
string S39_Pr         = DoubleToStr(S39_Price,S39_Digits); 
double S39_Change     = (((S39_Price / iClose(S39,PERIOD_D1,1))-1)*100);
string S39_Sign;      if(S39_Change>0)S39_Sign = "+";
string S39_Ch         = " (" + S39_Sign + DoubleToStr(S39_Change,2) + "%)";}
else{S39_name =""; S39_Pr = ""; S39_Ch = "";}

if(MarketInfo(S40,MODE_LOTSIZE)==0) S40=""; if(S40!=""){
string S40_name       = "\n" + S40 + ": ";
int    S40_Digits     = MarketInfo(S40,MODE_DIGITS);
double S40_Price      = MarketInfo(S40,MODE_BID);
string S40_Pr         = DoubleToStr(S40_Price,S40_Digits); 
double S40_Change     = (((S40_Price / iClose(S40,PERIOD_D1,1))-1)*100);
string S40_Sign;      if(S40_Change>0)S40_Sign = "+";
string S40_Ch         = " (" + S40_Sign + DoubleToStr(S40_Change,2) + "%)";}
else{S40_name =""; S40_Pr = ""; S40_Ch = "";}

if(MarketInfo(S41,MODE_LOTSIZE)==0) S41=""; if(S41!=""){
string S41_name       = "\n" + S41 + ": ";
int    S41_Digits     = MarketInfo(S41,MODE_DIGITS);
double S41_Price      = MarketInfo(S41,MODE_BID);
string S41_Pr         = DoubleToStr(S41_Price,S41_Digits); 
double S41_Change     = (((S41_Price / iClose(S41,PERIOD_D1,1))-1)*100);
string S41_Sign;      if(S41_Change>0)S41_Sign = "+";
string S41_Ch         = " (" + S41_Sign + DoubleToStr(S41_Change,2) + "%)";}
else{S41_name =""; S41_Pr = ""; S41_Ch = "";}

if(MarketInfo(S42,MODE_LOTSIZE)==0) S42=""; if(S42!=""){
string S42_name       = "\n" + S42 + ": ";
int    S42_Digits     = MarketInfo(S42,MODE_DIGITS);
double S42_Price      = MarketInfo(S42,MODE_BID);
string S42_Pr         = DoubleToStr(S42_Price,S42_Digits); 
double S42_Change     = (((S42_Price / iClose(S42,PERIOD_D1,1))-1)*100);
string S42_Sign;      if(S42_Change>0)S42_Sign = "+";
string S42_Ch         = " (" + S42_Sign + DoubleToStr(S42_Change,2) + "%)";}
else{S42_name = "";S42_Pr = ""; S42_Ch = "";}

if(MarketInfo(S43,MODE_LOTSIZE)==0) S43=""; if(S43!=""){
string S43_name       = "\n" + S43 + ": ";
int    S43_Digits     = MarketInfo(S43,MODE_DIGITS);
double S43_Price      = MarketInfo(S43,MODE_BID);
string S43_Pr         = DoubleToStr(S43_Price,S43_Digits); 
double S43_Change     = (((S43_Price / iClose(S43,PERIOD_D1,1))-1)*100);
string S43_Sign;      if(S43_Change>0)S43_Sign = "+";
string S43_Ch         = " (" + S43_Sign + DoubleToStr(S43_Change,2) + "%)";}
else{S43_name = ""; S43_Pr = ""; S43_Ch = "";}

if(MarketInfo(S44,MODE_LOTSIZE)==0) S44=""; if(S44!=""){
string S44_name       = "\n" + S44 + ": ";
int    S44_Digits     = MarketInfo(S44,MODE_DIGITS);
double S44_Price      = MarketInfo(S44,MODE_BID);
string S44_Pr         = DoubleToStr(S44_Price,S44_Digits); 
double S44_Change     = (((S44_Price / iClose(S44,PERIOD_D1,1))-1)*100);
string S44_Sign;      if(S44_Change>0)S44_Sign = "+";
string S44_Ch         = " (" + S44_Sign + DoubleToStr(S44_Change,2) + "%)";}
else{S44_name =""; S44_Pr = ""; S44_Ch = "";}

if(MarketInfo(S45,MODE_LOTSIZE)==0) S45=""; if(S45!=""){
string S45_name       = "\n" + S45 + ": ";
int    S45_Digits     = MarketInfo(S45,MODE_DIGITS);
double S45_Price      = MarketInfo(S45,MODE_BID);
string S45_Pr         = DoubleToStr(S45_Price,S45_Digits); 
double S45_Change     = (((S45_Price / iClose(S45,PERIOD_D1,1))-1)*100);
string S45_Sign;      if(S45_Change>0)S45_Sign = "+";
string S45_Ch         = " (" + S45_Sign + DoubleToStr(S45_Change,2) + "%)";}
else{S45_name =""; S45_Pr = ""; S45_Ch = "";}

if(MarketInfo(S46,MODE_LOTSIZE)==0) S46=""; if(S46!=""){
string S46_name       = "\n" + S46 + ": ";
int    S46_Digits     = MarketInfo(S46,MODE_DIGITS);
double S46_Price      = MarketInfo(S46,MODE_BID);
string S46_Pr         = DoubleToStr(S46_Price,S46_Digits); 
double S46_Change     = (((S46_Price / iClose(S46,PERIOD_D1,1))-1)*100);
string S46_Sign;      if(S46_Change>0)S46_Sign = "+";
string S46_Ch         = " (" + S46_Sign + DoubleToStr(S46_Change,2) + "%)";}
else{S46_name =""; S46_Pr = ""; S46_Ch = "";}

if(MarketInfo(S47,MODE_LOTSIZE)==0) S47=""; if(S47!=""){
string S47_name       = "\n" + S47 + ": ";
int    S47_Digits     = MarketInfo(S47,MODE_DIGITS);
double S47_Price      = MarketInfo(S47,MODE_BID);
string S47_Pr         = DoubleToStr(S47_Price,S47_Digits); 
double S47_Change     = (((S47_Price / iClose(S47,PERIOD_D1,1))-1)*100);
string S47_Sign;      if(S47_Change>0)S47_Sign = "+";
string S47_Ch         = " (" + S47_Sign + DoubleToStr(S47_Change,2) + "%)";}
else{S47_name = "";S47_Pr = ""; S47_Ch = "";}

if(MarketInfo(S48,MODE_LOTSIZE)==0) S48=""; if(S48!=""){
string S48_name       = "\n" + S48 + ": ";
int    S48_Digits     = MarketInfo(S48,MODE_DIGITS);
double S48_Price      = MarketInfo(S48,MODE_BID);
string S48_Pr         = DoubleToStr(S48_Price,S48_Digits); 
double S48_Change     = (((S48_Price / iClose(S48,PERIOD_D1,1))-1)*100);
string S48_Sign;      if(S48_Change>0)S48_Sign = "+";
string S48_Ch         = " (" + S48_Sign + DoubleToStr(S48_Change,2) + "%)";}
else{S48_name = ""; S48_Pr = ""; S48_Ch = "";}

if(MarketInfo(S49,MODE_LOTSIZE)==0) S49=""; if(S49!=""){
string S49_name       = "\n" + S49 + ": ";
int    S49_Digits     = MarketInfo(S49,MODE_DIGITS);
double S49_Price      = MarketInfo(S49,MODE_BID);
string S49_Pr         = DoubleToStr(S49_Price,S49_Digits); 
double S49_Change     = (((S49_Price / iClose(S49,PERIOD_D1,1))-1)*100);
string S49_Sign;      if(S49_Change>0)S49_Sign = "+";
string S49_Ch         = " (" + S49_Sign + DoubleToStr(S49_Change,2) + "%)";}
else{S49_name =""; S49_Pr = ""; S49_Ch = "";}

if(MarketInfo(S50,MODE_LOTSIZE)==0) S50=""; if(S50!=""){
string S50_name       = "\n" + S50 + ": ";
int    S50_Digits     = MarketInfo(S50,MODE_DIGITS);
double S50_Price      = MarketInfo(S50,MODE_BID);
string S50_Pr         = DoubleToStr(S50_Price,S50_Digits); 
double S50_Change     = (((S50_Price / iClose(S50,PERIOD_D1,1))-1)*100);
string S50_Sign;      if(S50_Change>0)S50_Sign = "+";
string S50_Ch         = " (" + S50_Sign + DoubleToStr(S50_Change,2) + "%)";}
else{S50_name =""; S50_Pr = ""; S50_Ch = "";}


SendMail("New Quotes on Date: " + TimeToStr(TimeLocal(),TIME_DATE) + " Time: " +TimeToStr(TimeLocal(),TIME_MINUTES),
   "My Account Information:"
+  "\n"   
+  "\nAccount Balance:\t"  + DoubleToStr(AccountBalance(),2)
+  "\nAccount Equity:\t"   + DoubleToStr(AccountEquity(),2)
+  "\nAccount Profit:\t"   + DoubleToStr(AccountProfit(),2)
+  "\nAccount Margin:\t"   + DoubleToStr(AccountMargin(),2)
+  "\n" 
+  "\nLast Bid Price (Percent Change between Last Price and Yesterday Close Price):"
+  "\n" 
+ S1_name + S1_Pr + S1_Ch
+ S2_name + S2_Pr + S2_Ch
+ S3_name + S3_Pr + S3_Ch
+ S4_name + S4_Pr + S4_Ch
+ S5_name + S5_Pr + S5_Ch
+ S6_name + S6_Pr + S6_Ch
+ S7_name + S7_Pr + S7_Ch
+ S8_name + S8_Pr + S8_Ch
+ S9_name + S9_Pr + S9_Ch
+ S10_name + S10_Pr + S10_Ch
+ S11_name + S11_Pr + S11_Ch
+ S12_name + S12_Pr + S12_Ch
+ S13_name + S13_Pr + S13_Ch
+ S14_name + S14_Pr + S14_Ch
+ S15_name + S15_Pr + S15_Ch
+ S16_name + S16_Pr + S16_Ch
+ S17_name + S17_Pr + S17_Ch
+ S18_name + S18_Pr + S18_Ch
+ S19_name + S19_Pr + S19_Ch
+ S20_name + S20_Pr + S20_Ch
+ S21_name + S21_Pr + S21_Ch
+ S22_name + S22_Pr + S22_Ch
+ S23_name + S23_Pr + S23_Ch
+ S24_name + S24_Pr + S24_Ch
+ S25_name + S25_Pr + S25_Ch
+ S26_name + S26_Pr + S26_Ch
+ S27_name + S27_Pr + S27_Ch
+ S28_name + S28_Pr + S28_Ch
+ S29_name + S29_Pr + S29_Ch
+ S30_name + S30_Pr + S30_Ch
+ S31_name + S31_Pr + S31_Ch
+ S32_name + S32_Pr + S32_Ch
+ S33_name + S33_Pr + S33_Ch
+ S34_name + S34_Pr + S34_Ch
+ S35_name + S35_Pr + S35_Ch
+ S36_name + S36_Pr + S36_Ch
+ S37_name + S37_Pr + S37_Ch
+ S38_name + S38_Pr + S38_Ch
+ S39_name + S39_Pr + S39_Ch
+ S40_name + S40_Pr + S40_Ch
+ S42_name + S42_Pr + S42_Ch
+ S43_name + S43_Pr + S43_Ch
+ S44_name + S44_Pr + S44_Ch
+ S45_name + S45_Pr + S45_Ch
+ S46_name + S46_Pr + S46_Ch
+ S47_name + S47_Pr + S47_Ch
+ S48_name + S48_Pr + S48_Ch
+ S49_name + S49_Pr + S49_Ch
+ S50_name + S50_Pr + S50_Ch

);

         Sleep(sleep);    // Desactivate the script for the selected period.
         }
         
   return(0);
  }
//+------------------------------------------------------------------+


 int init()
      {
      SendNow = false;
      return;
      }