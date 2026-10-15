# Estrategia de Ciclo de Tendencia Schaff con Color
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en función del indicador **Schaff Trend Cycle (STC)**. El STC aplica un doble cálculo estocástico a una serie MACD y oscila entre -100 y 100. Los valores por encima del nivel alto sugieren presión alcista, mientras que los valores por debajo del nivel bajo sugieren presión bajista.

## Lógica de Trading

- Suscribirse a las velas del marco temporal seleccionado.
- Calcular el MACD usando medias exponenciales rápida y lenta.
- Aplicar dos cálculos estocásticos consecutivos para derivar el STC.
- Cuando el STC sube por encima del nivel alto y continúa al alza:
  - Cerrar cualquier posición corta.
  - Entrar en una posición larga.
- Cuando el STC cae por debajo del nivel bajo y continúa a la baja:
  - Cerrar cualquier posición larga.
  - Entrar en una posición corta.

La estrategia siempre actúa sobre velas completamente formadas.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `FastPeriod` | Período de EMA rápida usado en MACD | `23` |
| `SlowPeriod` | Período de EMA lenta usado en MACD | `50` |
| `Cycle` | Longitud del ciclo estocástico | `10` |
| `HighLevel` | Umbral de sobrecompra para STC | `60` |
| `LowLevel` | Umbral de sobreventa para STC | `-60` |
| `CandleType` | Marco temporal de las velas procesadas | `4h` |

## Notas

- Los valores del STC se reescalan a un rango de -100…100 para facilitar la comparación con los niveles predeterminados.
- Las órdenes se envían con las llamadas `BuyMarket()` y `SellMarket()`; las posiciones se revierten automáticamente cuando aparecen señales opuestas.
- Esta estrategia se enfoca únicamente en las señales del indicador y no utiliza órdenes de stop-loss ni take-profit.
