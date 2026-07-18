# Estrategia de Divergencia para Múltiples Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detecta divergencias alcistas y bajistas entre el precio y el RSI y el histograma del MACD. Cuando el número de divergencias alcanza el umbral especificado, la estrategia entra en una operación en la dirección opuesta.

## Parámetros
- `RsiPeriod` – período para el cálculo del RSI.
- `MacdFastPeriod` – período rápido para el MACD.
- `MacdSlowPeriod` – período lento para el MACD.
- `MacdSignalPeriod` – período de señal para el MACD.
- `MinDivergence` – mínimo de indicadores que confirman la divergencia.
- `CandleType` – tipo de vela para la suscripción.
