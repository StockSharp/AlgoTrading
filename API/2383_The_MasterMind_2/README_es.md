# Estrategia The MasterMind 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia combina el **Stochastic Oscillator** y **Williams %R** para identificar condiciones extremas de sobreventa y sobrecompra.
Se abre una posición larga cuando la línea de señal del Stochastic cae por debajo de **3** y Williams %R es inferior a **-99.9**.
Se abre una posición corta cuando la línea de señal del Stochastic sube por encima de **97** y Williams %R es superior a **-0.1**.

El control de riesgo incluye un stop loss inicial y take profit, un trailing stop con paso ajustable y un disparador de break-even opcional que mueve el stop al precio de entrada tras obtener beneficio suficiente.

## Parámetros

- `LotSize` - volumen de operación en contratos.
- `StochasticPeriod` - período para el Stochastic Oscillator.
- `StochasticK` - suavizado de la línea %K.
- `StochasticD` - suavizado de la línea %D (señal).
- `WilliamsRPeriod` - período para Williams %R.
- `StopLossPoints` - stop loss inicial en puntos de precio.
- `TakeProfitPoints` - take profit inicial en puntos de precio.
- `TrailingStopPoints` - distancia del trailing stop en puntos.
- `TrailingStepPoints` - movimiento favorable mínimo antes de actualizar el trailing stop.
- `BreakEvenPoints` - distancia en puntos para mover el stop a break-even.
- `CandleType` - tipo y marco temporal de las velas usadas en los cálculos.

## Lógica de operación

1. **Señales de entrada**
   - **Compra** cuando señal Stochastic < 3 y Williams %R < -99.9.
   - **Venta** cuando señal Stochastic > 97 y Williams %R > -0.1.
2. **Señales de salida**
   - Las señales de entrada opuestas cierran las posiciones existentes.
   - El stop loss, take profit, break-even y trailing stop se aplican en cada vela.

## Notas

- Funciona en cualquier instrumento que soporte los indicadores requeridos.
- Diseñada con fines educativos y para experimentación adicional.
