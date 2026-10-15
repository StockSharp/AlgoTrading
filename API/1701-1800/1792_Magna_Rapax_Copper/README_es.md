# Estrategia Magna Rapax Copper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el sistema de medias móviles "arcoíris" del experto MQL original.
Utiliza once medias móviles exponenciales junto con filtros MACD y ADX.

## Cómo funciona

- Calcular EMA(2), EMA(3), EMA(5), EMA(8), EMA(13), EMA(21), EMA(34), EMA(55), EMA(89), EMA(144) y EMA(233) sobre precios de cierre.
- Calcular MACD (Rápido, Lento, Señal) y usar la línea de señal.
- Calcular ADX para medir la fuerza de la tendencia.
- **Comprar** cuando:
  - La línea de señal MACD está por encima de cero.
  - Todas las EMA están estrictamente ascendentes (cada EMA más rápida por encima de la más lenta).
  - El valor ADX está por encima del umbral.
- **Vender** cuando:
  - La línea de señal MACD está por debajo de cero.
  - Todas las EMA están estrictamente descendentes.
  - El valor ADX está por encima del umbral.

Las posiciones se invierten cuando aparece una señal opuesta.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `FastMacd` | Período de EMA rápida para MACD. |
| `SlowMacd` | Período de EMA lenta para MACD. |
| `SignalPeriod` | Período de la línea de señal para MACD. |
| `AdxPeriod` | Período para el indicador ADX. |
| `AdxThreshold` | Valor mínimo de ADX requerido para operar. |
| `CandleType` | Marco temporal de velas utilizado para los cálculos. |

## Notas

- La estrategia usa órdenes de mercado mediante `BuyMarket` y `SellMarket`.
- Solo se mantiene una posición a la vez; una señal opuesta invierte la posición.
- Esta es una conversión directa de la estrategia MQL original sin la lógica opcional de martingala.
