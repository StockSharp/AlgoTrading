# Estrategia Color Zerolag JJRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica del experto **ColorZerolagJJRSX** de MetaTrader. Utiliza dos osciladores RSI suavizados para aproximar el indicador ColorZerolagJJRSX original. El cruce de las líneas rápida y lenta genera señales de trading.

## Cómo funciona

- Cuando el oscilador rápido cruza **por debajo** del oscilador lento, la estrategia cierra cualquier posición corta y opcionalmente abre una nueva posición larga.
- Cuando el oscilador rápido cruza **por encima** del oscilador lento, la estrategia cierra cualquier posición larga y opcionalmente abre una nueva posición corta.
- Los niveles de stop-loss y take-profit de protección se aplican mediante el mecanismo integrado `StartProtection`.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `FastPeriod` | Período de la línea JJRSX rápida. |
| `SlowPeriod` | Período de la línea JJRSX lenta. |
| `BuyOpen` | Permitir apertura de posiciones largas. |
| `SellOpen` | Permitir apertura de posiciones cortas. |
| `BuyClose` | Cerrar posiciones largas existentes ante señal opuesta. |
| `SellClose` | Cerrar posiciones cortas existentes ante señal opuesta. |
| `StopLoss` | Nivel de stop-loss en unidades de precio. |
| `TakeProfit` | Nivel de take-profit en unidades de precio. |
| `CandleType` | Marco temporal utilizado para los cálculos. |

## Notas

- La implementación utiliza indicadores integrados y la API `Bind` de alto nivel.
- El volumen se toma de la propiedad `Volume` de la estrategia.
- No se proporciona versión en Python para esta estrategia.

## Referencias

El código MQL original se encuentra en `MQL/13854` dentro de este repositorio.
