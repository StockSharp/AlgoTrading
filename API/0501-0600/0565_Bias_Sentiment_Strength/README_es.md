# Bias Sentiment Strength

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

En velas terminadas de 15 minutos se combinan MACD(12,26,9), RSI Wilder(14), estocástico(21,14,14), Awesome Oscillator(5,34), Alligator y sesgo por volumen. K bruto usa extremos de 21 barras; `Ks` es su SMA de 14 valores y `D` otra SMA de 14. Alligator usa SMMAs del precio mediano: mandíbula 13 desplazada 8, dientes 8 desplazados 5 y labios 5 desplazados 3.

`BASS = 2 * (MACD - Signal) + (RSI - 50) / 5 + 0.15 * (Ks - D) + 0.6 * AO + (Lips - Teeth) + (Teeth - Jaw) + VWMA(30) - SMA(30)`. Todos los componentes deben estar formados; por defecto se necesitan al menos 47 barras completas. La configuración inválida falla explícitamente.

Por encima de `BassThreshold` se busca largo `Volume`; por debajo de `-BassThreshold`, corto `Volume`. El umbral predeterminado es cero. Se usan niveles, permitiendo reentrada tras un stop sin nuevo cruce por cero. La reversión usa `Volume + abs(Position)` y una orden pendiente bloquea otra. El stop nativo local a mercado cierra toda la exposición ejecutada a `StopLossPercent` por ciento, predeterminado 1. No hay take-profit.

La protección local nativa usa velas y actualizaciones de precio suscritas del instrumento. Las distancias parten de los precios ejecutados. Las salidas son a mercado y dependen del mercado disponible y del paso de precio.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `RsiPeriod` | `14` |
| `StochK` | `21` |
| `StochSmooth` | `14` |
| `StochD` | `14` |
| `AoShort` | `5` |
| `AoLong` | `34` |
| `VolumeLength` | `30` |
| `BassThreshold` | `0` |
| `StopLossPercent` | `1` |
| `CandleType` | `15m` |
