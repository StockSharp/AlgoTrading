# Estrategia MovingUp MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de cruce de medias móviles con gestión de riesgo opcional.
Abre una posición larga cuando la media móvil rápida cruza por encima de la media móvil lenta y abre una posición corta en el cruce opuesto.

## Parámetros
- **Fast MA** (`FastLength`): período de la media móvil simple rápida.
- **Slow MA** (`SlowLength`): período de la media móvil simple lenta.
- **Use TP** (`UseTakeProfit`): activa la regla de take profit.
- **TP** (`TakeProfit`): distancia en precio para tomar ganancias.
- **Use SL** (`UseStopLoss`): activa la regla de stop loss.
- **SL** (`StopLoss`): distancia en precio para el stop loss.
- **Use TS** (`UseTrailingStop`): activa la lógica de trailing stop.
- **TS** (`TrailingStop`): distancia del trailing stop en precio.
- **Candle** (`CandleType`): tipo de vela usado para los cálculos.

## Lógica de trading
1. Suscribirse a los datos de velas y calcular dos indicadores SMA.
2. Detectar cruces de las MAs rápida y lenta.
3. Entrar largo cuando la MA rápida cruza por encima de la MA lenta si no existe posición larga.
4. Entrar corto cuando la MA rápida cruza por debajo de la MA lenta si no existe posición corta.
5. Aplicar gestión de riesgo en cada nueva vela:
   - Tomar ganancias cuando el precio avanza la distancia especificada.
   - Stop loss cuando el precio se mueve contra la posición la distancia especificada.
   - El trailing stop protege las ganancias una vez que el precio se mueve favorablemente.

## Estrategia MQL original
El script MQL4 original `ma_v_1_3_3.mq4` contiene numerosas funciones adicionales como lógica de incremento/decremento por pasos y dimensionamiento complejo de posiciones. Esta versión en C# se enfoca en el cruce de medias móviles central y los controles de riesgo esenciales.
