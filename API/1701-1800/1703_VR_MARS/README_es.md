# Estrategia VR MARS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta muestra demuestra una versión simplificada del panel de trading manual **VR---MARS-EN** portado de MQL4 a StockSharp.

El script original proporcionaba cinco tamaños de lote predefinidos y botones para enviar órdenes de compra o venta. También mostraba múltiples etiquetas con estadísticas de trading. En esta versión en C# se elimina el panel visual, pero se conserva la idea central de seleccionar uno de los cinco tamaños de lote y ejecutar una orden de mercado.

## Parámetros

- `Lot1` – tamaño del primer lote.
- `Lot2` – tamaño del segundo lote.
- `Lot3` – tamaño del tercer lote.
- `Lot4` – tamaño del cuarto lote.
- `Lot5` – tamaño del quinto lote.
- `SelectedLot` – número del 1 al 5 que especifica qué tamaño de lote se usará.
- `Buy` – cuando es `true`, se envía una orden de compra de mercado al iniciar la estrategia.
- `Sell` – cuando es `true`, se envía una orden de venta de mercado al iniciar la estrategia.

Solo debe habilitarse uno de los indicadores de dirección a la vez. Cuando la estrategia se inicia, activa la protección de posición y envía la orden de mercado correspondiente usando métodos auxiliares de alto nivel.

## Notas

Esta estrategia está destinada a fines educativos y no implementa ninguna lógica de trading más allá de la ejecución inmediata de órdenes.
