# Ultimate T3 Fibonacci BTC Scalping Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies two Tilson T3 moving averages to capture short-term BTC moves. A crossover between the Fibonacci-tuned and standard T3 lines generates long or short entries. Optional TP/SL management and closing on opposite signals are supported.

Testing indicates an average annual return of about 38%. It works best on BTC pairs with low latency.

The strategy buys when the fast T3 crosses above the slow T3 and sells on the opposite cross. Positions can be closed on reverse signals, or by percentage take profit and stop loss levels.

## Details

- **Entry Criteria**:
  - **Long**: Fast T3 crosses above slow T3.
  - **Short**: Fast T3 crosses below slow T3.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover or TP/SL if enabled.
- **Stops**: Optional percentage-based.
- **Filters**:
  - None.
