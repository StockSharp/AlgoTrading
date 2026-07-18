# Estrategia de Velas MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el experto de MetaTrader "Exp_MACDCandle". Convierte la salida de color de un indicador de velas basado en MACD en señales de trading utilizando la API de alto nivel de StockSharp.

## Concepto

El indicador MACD Candle construye velas sintéticas a partir de los valores MACD calculados sobre los precios de apertura y cierre. Si el MACD calculado sobre el cierre está por encima del MACD calculado sobre la apertura, la vela se considera alcista (color 2). Lo contrario genera una vela bajista (color 0). Un color neutro (1) aparece cuando ambos valores son iguales.

La estrategia abre posiciones largas cuando aparece una vela alcista después de una no alcista, y abre posiciones cortas cuando una vela bajista sigue a una no bajista. Las posiciones existentes se revierten en la nueva dirección.

## Parámetros

- `FastLength` – período de la EMA rápida para MACD (predeterminado 12).
- `SlowLength` – período de la EMA lenta para MACD (predeterminado 26).
- `SignalLength` – período de la línea de señal para MACD (predeterminado 9).
- `CandleType` – tipo de vela utilizado para los cálculos, predeterminado `TimeFrameCandle` con un período de cuatro horas.

Todos los parámetros son configurables y soportan optimización.

## Reglas de Entrada y Salida

- **Entrada larga**: el MACD sobre el cierre sube por encima del MACD sobre la apertura mientras la vela anterior no era alcista.
- **Entrada corta**: el MACD sobre la apertura sube por encima del MACD sobre el cierre mientras la vela anterior no era bajista.
- **Salida**: la estrategia cierra la posición actual cuando ocurre una señal opuesta; no se aplica stop‑loss ni take‑profit explícito.

## Notas

- La estrategia utiliza órdenes de mercado (`BuyMarket` y `SellMarket`).
- Las señales se evalúan únicamente en velas terminadas para evitar el ruido.
- El ejemplo está destinado a fines educativos y no incluye gestión de riesgos.
