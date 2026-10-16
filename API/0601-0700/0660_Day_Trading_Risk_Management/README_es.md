# Estrategia de Day Trading con Gestión de Riesgo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando la EMA rápida cruza por encima de la EMA lenta mientras el RSI está por debajo del nivel de sobrecompra. Entra en corto cuando la EMA rápida cruza por debajo de la EMA lenta mientras el RSI está por encima del nivel de sobreventa. La gestión de riesgo utiliza porcentajes fijos de take profit y stop loss.

### Reglas exactas de entrada y protección

Predeterminados: `CandleType=1 minuto`, `FastEmaPeriod=20`, `SlowEmaPeriod=50`, `RsiLength=14`, `RsiOverbought=70`, `RsiOversold=30`, `TakeProfitPercent=2`, `StopLossPercent=1`. En **vela terminada**, compra cuando antes `EMA20 <= EMA50`, ahora `EMA20 > EMA50` y `RSI14 < 70`; vende corto cuando antes `EMA20 >= EMA50`, ahora `EMA20 < EMA50` y `RSI14 > 30`. Desde plano o con inversión de posición opuesta, cantidad `Volume + abs(Position)`. **Sin confirmación RSI no hay entrada**. Las posiciones ejecutadas tienen **take profit nativo fijo del 2% y stop loss del 1%**, sin trailing, activos entre cierres de vela. La implementación anterior solo cruzaba EMA120/450 y carecía de RSI y protección. Los valores numéricos se han definido aquí porque el README original no los especificaba.



## Parámetros
- Tipo de vela
- Longitud de EMA rápida
- Longitud de EMA lenta
- Longitud de RSI
- RSI Sobrecompra
- RSI Sobreventa
- Porcentaje de take profit
- Porcentaje de stop loss
