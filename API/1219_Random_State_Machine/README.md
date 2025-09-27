# Random State Machine Strategy

Random State Machine Strategy enters trades on random state changes filtered by a moving average. Long positions open when a state change occurs and price is above the moving average. Short positions open when price is below the average. The strategy supports optional take-profit/stop-loss, timed exits, and moving-average cross exits.
