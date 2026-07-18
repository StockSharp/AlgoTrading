# Estrategia ScalpWiz Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia ScalpWiz Bollinger** es un sistema contra-tendencia que utiliza las Bandas de Bollinger para detectar precios extendidos. Cuando el precio de cierre se aleja demasiado de la banda superior o inferior, la estrategia abre una posición en la dirección opuesta esperando una reversión.

Se verifican cuatro niveles de distancia. Cada nivel corresponde a una fuerza de señal diferente y multiplica el volumen de la operación. El tamaño de la posición también se escala por un porcentaje de riesgo del valor actual de la cartera.

## Parámetros

- `BandsPeriod` – número de velas utilizadas para calcular las Bandas de Bollinger.
- `BandsDeviation` – multiplicador de desviación estándar para las bandas.
- `Level1Pips` … `Level4Pips` – distancia desde la banda en pips que activa una señal de nivel 1–4.
- `StrengthLevel1Multiplier` … `StrengthLevel4Multiplier` – multiplicadores de volumen para cada nivel.
- `RiskPercent` – porcentaje del valor de la cartera arriesgado por señal.
- `CandleType` – marco temporal de velas utilizado para los cálculos.

## Lógica de trading

1. Suscribirse a velas del marco temporal seleccionado y calcular las Bandas de Bollinger.
2. En cada vela finalizada:
   - Si el cierre está por encima de la banda superior por una distancia de nivel configurada, abrir una posición corta.
   - Si el cierre está por debajo de la banda inferior por una distancia de nivel configurada, abrir una posición larga.
3. El volumen se calcula a partir del porcentaje de riesgo y el multiplicador de fuerza de señal.

La estrategia fue inspirada por el script MQL original `mcb.scalpwiz.9001.mq4`.
