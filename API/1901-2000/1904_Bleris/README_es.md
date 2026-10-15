# Estrategia Bleris
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Bleris analiza la tendencia de los extremos de precio recientes para abrir operaciones en la dirección de la tendencia predominante.
La serie de precios se divide en tres segmentos de longitud `SignalBarSample` y se comparan los máximos más altos y los mínimos más bajos de estos segmentos.

- **Indicadores**: Highest, Lowest
- **Parámetros**:
  - `SignalBarSample` – número de velas por segmento.
  - `CounterTrend` – invertir la dirección de trading.
  - `Lots` – volumen de la orden.
  - `CandleType` – marco temporal de las velas.
  - `AnotherOrderPips` – distancia mínima en pips antes de abrir otra orden del mismo tipo.

## Cómo funciona
1. Los indicadores Highest y Lowest calculan los precios extremos de las últimas `SignalBarSample` velas.
2. Máximos decrecientes señalan una tendencia bajista; mínimos crecientes señalan una tendencia alcista.
3. La estrategia compra en tendencia alcista y vende en tendencia bajista. Con `CounterTrend` activado la lógica se invierte.
4. Se ignoran nuevas órdenes en la misma dirección si el precio de la última orden está dentro de `AnotherOrderPips`.

Este ejemplo utiliza la API de alto nivel de StockSharp y está destinado a propósitos educativos.
