# Estrategia ColorMetro DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia ColorMetro DeMarker** es una implementación en StockSharp del asesor experto MQL5 `Exp_ColorMETRO_DeMarker`.
Utiliza el indicador DeMarker combinado con niveles escalonados para generar señales de trading.

## Parámetros
- **DeMarker Period** – período del indicador DeMarker.
- **Fast Step** – tamaño del paso para construir el nivel rápido (MPlus).
- **Slow Step** – tamaño del paso para construir el nivel lento (MMinus).
- **Candle Type** – marco temporal de las velas para el análisis.
- **Enable Buy Open** – permitir la apertura de posiciones largas.
- **Enable Sell Open** – permitir la apertura de posiciones cortas.
- **Enable Buy Close** – permitir el cierre de posiciones largas.
- **Enable Sell Close** – permitir el cierre de posiciones cortas.

## Lógica de Trading
1. El valor DeMarker se escala a 0–100 y se calculan dos niveles dinámicos (MPlus y MMinus) usando los tamaños de paso rápido y lento.
2. Cuando el nivel rápido anterior era superior al nivel lento y el nivel rápido actual cruza por debajo del lento, la estrategia compra y opcionalmente cierra posiciones cortas.
3. Cuando el nivel rápido anterior era inferior al nivel lento y el nivel rápido actual cruza por encima del lento, la estrategia vende y opcionalmente cierra posiciones largas.
4. Todos los cálculos usan únicamente velas completadas.

Este enfoque permite seguir los cambios de tendencia indicados por los niveles escalonados del DeMarker.
