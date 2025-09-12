# OBV Traffic Lights 策略

该策略使用基于 Heikin Ashi 的能量潮(OBV)，并通过三条以红绿灯颜色标记的指数均线来判断趋势。OBV 与快速 EMA 同时高于慢速 EMA 时做多；OBV 与快速 EMA 同时低于慢速 EMA 时做空。当条件消失时平仓。

- **入场条件**：OBV > 慢速 EMA 且 快速 EMA > 慢速 EMA；OBV < 慢速 EMA 且 快速 EMA < 慢速 EMA。
- **离场条件**：条件相反或不再满足。
- 指标：OBV、EMA、Highest/Lowest
