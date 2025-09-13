# Spread Probe Strategy

This strategy measures market liquidity by recording the average bid-ask spread over fixed time intervals. For every quote update it accumulates the current spread (`ask - bid`). At the end of each interval the average spread is written to a CSV file in the format:

```
start_time,end_time,average_spread
```

## Parameters

- **Interval** – length of the averaging period in minutes. Allowed values are 5 to 60 with a step of 5. The default is 15.

## Output

The file name is `<symbol>_BidAskSpread(<Interval>)_<YYYYMMDD>.txt`. Each line contains the interval start time, end time, and the computed average spread.

## Notes

This strategy does not send any orders. It is intended for analysis of market conditions and liquidity rather than trading.
