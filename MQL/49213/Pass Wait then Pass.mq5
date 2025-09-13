input int count = 50; // Set the counting limit as an input
input int wait = 50; // Set the waiting limit as an input

int Counter; // counter variable default value is "0"
int Waiter; // Waiting variable default value is "0"

// Expert Initializing --------------------
int OnInit()
  {
   return(INIT_SUCCEEDED);
  }

// Expert DeInitializing -------------------
void OnDeinit(const int reason)
  {

  }

// Expert OnTick --------------------------
void OnTick()
  {
   Comment("Counted Ticks -: ", Counter, "\n", "Waited Ticks -: ", Waiter);

   if(Counter < count) // Pass "X" times
     {
      Counter++; // update the counter

      // Your code goes here.

     }
   else
      if(Waiter < wait) // Wait for "X" times
        {
         Waiter++; // update the waiter

         // Your code goes here.

        }

   if(Waiter == wait) // Waiting Limit is reached
     {
      Counter = 0; // reset counter
      Waiter = 0; // reset waiter
     }






  } // OnTick End  <<----------------------
//+------------------------------------------------------------------+