# Estrategia de Alertas de Salida Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando la línea %K del Stochastic cruza por encima de %D en la zona de sobreventa, y entra en corto cuando %K cruza por debajo de %D en la zona de sobrecompra. Las posiciones están protegidas por un stop loss fijo y un take profit medidos en ticks. Cuando ocurre un cruce opuesto fuera de la zona extrema, la posición se cierra sin revertir.

## Parámetros
- `StochLength` – período principal del oscilador Stochastic.
- `KLength` – período de suavizado de la línea %K.
- `DLength` – período de suavizado de la línea %D.
- `StopLossTicks` – distancia del stop loss en ticks.
- `TakeProfitTicks` – distancia del take profit en ticks.
- `CandleType` – marco temporal de las velas.
