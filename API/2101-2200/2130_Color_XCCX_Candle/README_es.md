# Estrategia Color XCCX Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida desde el código MQL `MQL/14260`.

Esta estrategia compara dos medias móviles simples (SMA) construidas a partir de los precios de apertura y cierre de las velas. Cuando la SMA calculada a partir de los precios de cierre cruza por encima de la SMA basada en los precios de apertura, se abre una posición larga. Cuando la SMA basada en el cierre cruza por debajo de la SMA basada en la apertura, se abre una posición corta. Cualquier posición opuesta existente se cierra antes de abrir una nueva.

Parámetros:

- `SMA Length` – número de velas utilizadas para calcular ambas SMA.
- `Candle Type` – marco temporal para las velas entrantes.
- `Stop Loss %` – tamaño del stop loss como porcentaje del precio de entrada.
- `Take Profit %` – tamaño del take profit como porcentaje del precio de entrada.

La estrategia utiliza la API de alto nivel de StockSharp para suscribirse a velas y vincular indicadores. También traza ambas SMA y las operaciones ejecutadas en el gráfico cuando la visualización está disponible.
