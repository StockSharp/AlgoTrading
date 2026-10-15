# Estrategia de Anomalía de Sentimiento Nocturno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera un ETF de renta variable solo de noche cuando un indicador de sentimiento externo señala un optimismo extremo. Al cierre se compra el ETF si el indicador supera un umbral y se vende a la mañana siguiente, buscando la deriva nocturna asociada con el sentimiento positivo.

No se utilizan datos intradía; el algoritmo reacciona a los valores de sentimiento al final del día y coloca órdenes de mercado al cierre y a la apertura del día siguiente.

## Detalles

- **Instrumento**: ETF de renta variable y serie de datos de sentimiento.
- **Señal**: valor de sentimiento por encima del `Threshold` configurable.
- **Período de tenencia**: cierre del mercado hasta la apertura del día siguiente.
- **Posicionamiento**: largo cuando el sentimiento es alto, de lo contrario sin posición.
- **Control de riesgo**: orden omitida cuando el valor de la operación está por debajo de `MinTradeUsd`.
