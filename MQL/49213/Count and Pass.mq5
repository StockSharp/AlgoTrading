input int count = 50; // Set the counting limit as an input

int Counter; // counter variable

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
 Counter ++; // add 1 to the counter on each tick.  
 Comment("Current Count -:", Counter);
 
 if(Counter == count)  // Count "X" times and pass   | This block Executed only once per each count.
 {
  
  // Your code goes here......

 Alert(count," Times counted"); 
 Counter = 0; // Reset the counter at the end of your code block. This is must. 
 } 

} // OnTick End  <<----------------------