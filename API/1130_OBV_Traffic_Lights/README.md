# OBV Traffic Lights Strategy

Uses a Heikin Ashi based On-Balance Volume and three EMAs colored like traffic lights. Long when OBV and the fast EMA are above the slow EMA; short when both are below. Positions close when conditions disappear.

- **Entry Criteria**: OBV > Slow EMA and Fast EMA > Slow EMA for long; OBV < Slow EMA and Fast EMA < Slow EMA for short.
- **Exit Criteria**: Opposite signal or loss of agreement.
- **Indicators**: OBV, EMA, Highest/Lowest
