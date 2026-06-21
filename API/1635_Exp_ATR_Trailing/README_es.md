# Estrategia Exp de Trailing ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo demuestra cómo gestionar posiciones existentes con un trailing stop basado en el indicador **Average True Range (ATR)**. La estrategia no genera señales de entrada; solo ajusta el nivel de salida de una posición abierta según la volatilidad del mercado.

## Cómo funciona

1. La estrategia se suscribe a datos de velas de un marco temporal elegido.
2. Un indicador `AverageTrueRange` se calcula en cada vela.
3. Para posiciones largas, el nivel de stop se sube a `Close - ATR * BuyFactor`.
4. Para posiciones cortas, el nivel de stop se baja a `Close + ATR * SellFactor`.
5. Cuando el precio cruza el nivel de trailing, la posición se cierra a mercado.

El trailing stop solo se mueve en la dirección del trade y nunca retrocede, proporcionando una salida ajustada a la volatilidad.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `AtrPeriod` | Período de cálculo del ATR. |
| `BuyFactor` | Multiplicador aplicado al ATR al hacer trailing de una posición larga. |
| `SellFactor` | Multiplicador aplicado al ATR al hacer trailing de una posición corta. |
| `CandleType` | Marco temporal de las velas utilizadas para el análisis. |

## Notas de uso

- Adjuntar la estrategia a un instrumento y abrir una posición manualmente o desde otra estrategia.
- Adecuada para gestión de riesgos donde las salidas se controlan por separado de las entradas.
- El área del gráfico muestra velas, valores ATR y trades ejecutados para análisis visual.

## Referencias

- [Average True Range en la documentación de StockSharp](https://doc.stocksharp.com/topics/indicator_average_true_range.html)
- [Strategy Designer](https://doc.stocksharp.com/topics/designer.html)
