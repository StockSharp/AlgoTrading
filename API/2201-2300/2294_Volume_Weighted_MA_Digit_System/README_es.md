# Estrategia Volume Weighted MA Sistema Digital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el **Volume Weighted MA Digit System**. Construye dos medias móviles ponderadas por volumen (VWMA) basadas en los máximos y mínimos de las velas. El cruce del precio con estas bandas proporciona señales de trading.

## Cómo Funciona

1. **Indicadores**
   - `VWMA High`: VWMA aplicada a los máximos de las velas.
   - `VWMA Low`: VWMA aplicada a los mínimos de las velas.
2. **Señales**
   - **Entrada Larga**: El precio de cierre cruza al alza `VWMA High`.
   - **Entrada Corta**: El precio de cierre cruza a la baja `VWMA Low`.
   - El cruce opuesto cierra las posiciones abiertas.
3. **Gestión de Riesgo**
   - Utiliza `StartProtection` integrado con stop loss y take profit configurables (en puntos).

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `VwmaPeriod` | Longitud del cálculo VWMA | `12` |
| `CandleType` | Marco temporal de las velas utilizado para el cálculo | `4h` |
| `StopLoss` | Stop loss en puntos | `1000` |
| `TakeProfit` | Take profit en puntos | `2000` |

## Notas

- Solo se procesan las velas cerradas.
- La estrategia usa características de API de alto nivel como `SubscribeCandles`, `Bind` e indicadores estándar.
- Estrategia MQL original: `Exp_Volume_Weighted_MA_Digit_System.mq5`.
