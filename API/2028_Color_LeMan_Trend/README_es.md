# Estrategia de Tendencia Color LeMan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port del asesor experto original de MQL5 *ColorLeManTrend*. Utiliza un indicador de tendencia personalizado basado en máximos y mínimos para identificar la dirección del mercado.

## Idea

El indicador calcula líneas alcistas y bajistas utilizando valores extremos de máximos y mínimos durante tres períodos de retroceso diferentes. Las medias móviles exponenciales suavizan estos valores. Las decisiones de trading se basan en los cruces de las líneas alcistas y bajistas:

- Cuando la línea alcista anterior está por encima de la bajista y la línea alcista actual cae por debajo de la bajista, se genera una señal de **compra**.
- Cuando la línea alcista anterior está por debajo de la bajista y la línea alcista actual sube por encima de la bajista, se genera una señal de **venta**.
- Indicadores opcionales controlan si se pueden abrir o cerrar posiciones largas o cortas.

## Parámetros

- `CandleType` – marco temporal para los cálculos del indicador.
- `Min` – período para el cálculo del extremo más corto.
- `Midle` – período para el cálculo del extremo medio.
- `Max` – período para el cálculo del extremo más largo.
- `PeriodEma` – período de suavizado para las líneas alcista y bajista.
- `StopLossPoints` – stop de protección en puntos.
- `TakeProfitPoints` – take profit en puntos.
- `AllowBuy` – habilitar entradas largas.
- `AllowSell` – habilitar entradas cortas.
- `AllowBuyClose` – permitir el cierre de posiciones largas.
- `AllowSellClose` – permitir el cierre de posiciones cortas.
- `Volume` – volumen de trading por orden.

## Notas

La estrategia procesa únicamente velas terminadas y utiliza órdenes de mercado para todas las operaciones. Los valores de stop-loss y take-profit se aplican mediante la protección de posición incorporada.
