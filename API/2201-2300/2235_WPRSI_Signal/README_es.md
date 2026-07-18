# Estrategia de Señal WPRSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el experto WPRSIsignal de MetaTrader. Combina el Williams Percent Range (WPR) y el Índice de Fuerza Relativa (RSI) para generar señales de compra y venta.

## Lógica
- Se genera una señal de **compra** cuando el WPR cruza por encima de -20 desde abajo y el RSI está por encima de 50. La señal se confirma solo si el WPR permanece por encima de -20 durante las próximas `FilterUp` barras.
- Se genera una señal de **venta** cuando el WPR cruza por debajo de -80 desde arriba y el RSI está por debajo de 50. La señal se confirma solo si el WPR permanece por debajo de -80 durante las próximas `FilterDown` barras.
- Cuando se confirma una señal de compra, la estrategia abre una posición larga si no hay ninguna activa. Cuando se confirma una señal de venta, abre una posición corta si no hay ninguna activa.

## Parámetros
- `Period` – longitud de cálculo para WPR y RSI.
- `FilterUp` – número de barras que deben mantener el WPR por encima de -20 para confirmar una señal de compra.
- `FilterDown` – número de barras que deben mantener el WPR por debajo de -80 para confirmar una señal de venta.
- `CandleType` – marco temporal de las velas usadas para los cálculos.

## Uso
Adjunte la estrategia a cualquier activo. La estrategia usa `SubscribeCandles` y `Bind` para recibir datos de velas y valores de indicadores. Las posiciones se gestionan con órdenes de mercado: `BuyMarket` para entradas largas y `SellMarket` para entradas cortas. La estrategia no implementa stop-loss ni take-profit; las posiciones se cierran mediante señales opuestas.
