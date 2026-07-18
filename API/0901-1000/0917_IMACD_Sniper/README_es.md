# Estrategia IMACD Sniper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

IMACD Sniper combina cruces del MACD con un filtro de tendencia EMA, confirmación de volumen y patrones de velas fuertes. El take profit y el stop loss dinámicos se basan en el rango promedio reciente.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: La línea MACD cruza por encima de la línea de señal, precio por encima de la EMA, delta de MACD > delta mínimo, ambas líneas lejos de cero, volumen por encima del promedio, vela alcista fuerte.
  - **Corto**: La línea MACD cruza por debajo de la línea de señal, precio por debajo de la EMA, delta de MACD > delta mínimo, ambas líneas lejos de cero, volumen por encima del promedio, vela bajista fuerte.
- **Criterios de salida**: Cruce opuesto del MACD o alcanzar take profit / stop loss.
- **Stops**: Take profit y stop loss dinámicos basados en el rango promedio.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdDeltaMin` = 0.03
  - `MacdZeroLimit` = 0.05
  - `RangeLength` = 14
  - `RangeMultiplierTp` = 4.0
  - `RangeMultiplierSl` = 1.5
  - `EmaLength` = 20
  - `CandleType` = tf(1m)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo y Corto
  - Indicadores: MACD, EMA, Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
