# Edición Simulador VisualTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto simplificado de los scripts VisualTrader de MetaTrader.

Abre una única posición de mercado en la dirección elegida y adjunta órdenes de stop-loss y take-profit protectoras. Los parámetros permiten configurar la dirección, el take profit y el stop loss en valores de precio absolutos. La estrategia demuestra cómo los scripts de gestión manual de operaciones pueden recrearse usando la API de alto nivel de StockSharp.

## Parámetros

- **Trade Direction** – elegir Buy o Sell para la orden inicial.
- **Take Profit** – valor opcional de take profit en precio absoluto. Establecer en 0 para desactivar.
- **Stop Loss** – valor opcional de stop loss en precio absoluto. Establecer en 0 para desactivar.
- **Volume** – volumen base de la estrategia utilizado para la orden de mercado.

## Lógica de Trading

Al iniciar, la estrategia:

1. Crea órdenes protectoras usando `StartProtection`.
2. Envía una orden de mercado según la dirección de trading seleccionada.

El ejemplo no depende de indicadores ni de datos de mercado y está destinado a fines de demostración.
