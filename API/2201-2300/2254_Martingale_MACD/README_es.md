# Estrategia Martingale MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto MQL original "MartGreg_1" en el framework StockSharp. Utiliza dos indicadores de Convergencia/Divergencia de Medias Móviles (MACD) para buscar reversiones y aplica una regla de martingala para el dimensionamiento de posiciones.

## Cómo funciona

- El primer MACD busca extremos locales en las últimas tres velas completadas.
- El segundo MACD compara los dos últimos valores para determinar la dirección del momentum.
- Se abre una posición larga cuando el primer MACD forma un valle y el segundo MACD decrece.
- Se abre una posición corta cuando el primer MACD forma un pico y el segundo MACD aumenta.
- Tras cada operación perdedora, el tamaño de la siguiente orden se duplica hasta el límite configurado.
- El stop-loss y el take-profit se establecen en puntos de precio absolutos.

## Parámetros

- `Shape` – divisor para calcular el volumen inicial a partir del saldo de la cuenta.
- `Doubling Count` – número máximo de duplicaciones consecutivas tras pérdidas.
- `Stop Loss` – stop de protección en puntos.
- `Take Profit` – objetivo de beneficio en puntos.
- `MACD1 Fast/Slow` – períodos para el primer MACD.
- `MACD2 Fast/Slow` – períodos para el segundo MACD.
- `Candle Type` – marco temporal para el análisis.

