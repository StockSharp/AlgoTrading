# Sma Pullback Atr Exits

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Con velas cerradas, SMA de cierres y ATR de Wilder formados, comprar si cierre < SMA rápida y rápida > lenta; vender corto si cierre > rápida y rápida < lenta. Igualdades no entran. Volume solo desde cero, esperando órdenes pendientes. Fijar SL = ATR×AtrMultiplierSl y TP = ATR×AtrMultiplierTp en la señal. Protección local nativa ancla distancias a la ejecución real y observa bid/ask. ATR posterior no mueve niveles. Cerrar el resto completo por protección, sin invertir por señal contraria.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `FastSmaLength` | 8 |
| `SlowSmaLength` | 30 |
| `AtrLength` | 14 |
| `AtrMultiplierSl` | 1.2 |
| `AtrMultiplierTp` | 2 |

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
