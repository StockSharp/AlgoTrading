# SuperATR 7-Step Profit Strategy

Combines an adaptive ATR trend filter with a seven-stage take-profit system. Momentum-normalized ATR defines trend strength, while entries occur when the short moving average aligns with the confirmed trend direction.

- **Long**: Trend strength above threshold, price above short MA and short MA above long MA.
- **Short**: Trend strength below negative threshold, price below short MA and short MA below long MA.
- **Indicators**: Momentum, Standard Deviation, SMA, ATR.
- **Take Profit**: Four ATR-based levels and three fixed-percentage levels, each closing a portion of the position when enabled.

