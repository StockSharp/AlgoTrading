//+------------------------------------------------------------------+
//|                                           csv_margin_tracker.mq4 |
//|                                                 Copyright © 2013 |
//|                                               r.michelsen@gmx.de |
//+------------------------------------------------------------------+






extern int IntervalSeconds = 900;
extern bool MailAlert = false;
extern int MailAlertIntervalSeconds = 21600;
extern double MailAlertMarginLevel1 = 0.6; // Margin as percentage of Equity
extern double MailAlertMarginLevel2 = 0.8; // Margin as percentage of Equity
int Handle;
int Qnt_Symb;
int i; 
datetime CurrentIntervalStart;
datetime CurrentMailAlertIntervalStart[2];
datetime CurrentTime;
string DataString[3];
double DataDouble[3];
double MailAlertMarginLevel[2];






int init()
{
OpenFile();
WelcomeMessage();
MailAlertMarginLevel[1] = MailAlertMarginLevel1;
MailAlertMarginLevel[2] = MailAlertMarginLevel2;
CurrentTime = TimeCurrent();
SetNewInterval();
SetNewMailAlertInterval(1);
SetNewMailAlertInterval(2);
}






int start()
{
CurrentTime = TimeCurrent();
UpdateData();
if (IntervalEndCheck())
  {
   WriteData();
   SetNewInterval();
  }
for(i=1;i<=2;i++) 
  {
   if (MailAlertCheck(i))
     {
      SendMailAlert(i);
      SetNewMailAlertInterval(i);
     }   
  }  
}  






int deinit()
{
FileClose(Handle);
}






void OpenFile() // opens file
  {
   Handle=FileOpen("margintracker_"+AccountNumber()+".csv",FILE_CSV|FILE_READ|FILE_WRITE,';');
   if(Handle<1) Alert ("File error: ",GetLastError());
  }


bool IntervalEndCheck() // checks if the end of the interval is reached
  {
   if(CurrentTime - CurrentIntervalStart >= IntervalSeconds) return(true);
   return(false);  
  }


void WriteData() // writes data to file
  {
   Qnt_Symb=FileWrite(Handle,DataString[0],DataString[1],DataString[2],DataDouble[0],DataDouble[1],DataDouble[2]);
   if(Qnt_Symb < 0)
     {
      Alert("Error writing to the file",GetLastError());
      FileClose(Handle);   
     }
   FileFlush(Handle);
  }


void SetNewInterval() // clears data from memory and sets start of new time interval
  {
   DataDouble[0] = 1000000000;
   DataDouble[1] = 1000000000;
   DataDouble[2] = 0;
   CurrentIntervalStart = CurrentTime - (CurrentTime % IntervalSeconds);
  }

   
void UpdateData() // updates data in memory
  {
   DataString[0] = CurrentTime;
   DataString[1] = TimeToStr(CurrentTime,TIME_DATE);
   DataString[2] = TimeToStr(CurrentTime,TIME_MINUTES);
   if(AccountBalance() < DataDouble[0]) DataDouble[0] = AccountBalance();
   if(AccountEquity() < DataDouble[1]) DataDouble[1] = AccountEquity();
   if(AccountMargin() > DataDouble[2]) DataDouble[2] = AccountMargin();
  }


bool MailAlertCheck(int x) // checks for conditions to send mail alert
  {
   if(MailAlert == true && AccountMargin() / AccountEquity() >= MailAlertMarginLevel[x] && CurrentTime - CurrentMailAlertIntervalStart[x] >= MailAlertIntervalSeconds) return(true);
   return(false);
  }


void SendMailAlert(int x) // sends mail alert
  {
   SendMail("Account "+AccountNumber()+" Margin reached "+MailAlertMarginLevel[x]*100+" %", "Balance: "+AccountBalance()+". Equity: "+AccountEquity()+". Margin: "+AccountMargin()+"."); 
  }


void SetNewMailAlertInterval(int x) // sets start of new time interval for mail alert
  {
   CurrentMailAlertIntervalStart[x] = CurrentTime;
  }


void WelcomeMessage() // displays welcome message
  {
   Print("tracking Margins for account ", AccountNumber());
   if(AccountStopoutMode() == 0) Print("stopout for account ", AccountNumber()," will be triggered at Margin level < ", AccountStopoutLevel(), "%, i.e. when Margin reaches ", 100/(AccountStopoutLevel()*0.01), "% of Equity");
   else Print("stopout for account ", AccountNumber()," will be triggered at: ", AccountStopoutLevel(), " ", AccountCurrency());
  }

