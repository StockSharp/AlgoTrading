# Estrategia de Experto en Órdenes (1916)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición de mercado cuando el precio del instrumento alcanza niveles predefinidos. Imita el comportamiento del experto MQL original que gestionaba órdenes mediante líneas en el gráfico.

## Cómo funciona
- Se suscribe a velas de un marco temporal configurable.
- Cuando el precio de cierre cruza los umbrales `BuyLevel` o `SellLevel`, abre una posición larga o corta de mercado.
- Los valores de stop-loss y take-profit se calculan desde el precio de entrada usando `StopLossPip` y `TakeProfitPip`.
- Un trailing stop opcional mueve el stop-loss hacia el precio actual a medida que se mueve en una dirección favorable.

## Parámetros
- **TakeProfitPip** – distancia desde el precio de entrada al take profit en pips.
- **StopLossPip** – distancia desde el precio de entrada al stop loss en pips.
- **EnableTrailingStop** – habilitar o deshabilitar la lógica de trailing stop.
- **CandleType** – tipo de vela utilizado para cálculos.
- **BuyLevel** – nivel de precio que activa la entrada larga (0 deshabilita).
- **SellLevel** – nivel de precio que activa la entrada corta (0 deshabilita).

## Notas
- La estrategia utiliza API de alto nivel y procesa solo velas terminadas.
- El subsistema de protección se activa al inicio para evitar posiciones grandes accidentales.
