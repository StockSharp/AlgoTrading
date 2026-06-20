# Estrategia de Anomalía del Día de Pago
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia explota el efecto "payday" manteniendo un ETF de mercado amplio alrededor de las fechas típicas de pago de salarios. El ETF se posee desde dos días de negociación antes del fin de mes hasta el tercer día de negociación del nuevo mes, capturando las entradas de capital de las contribuciones salariales.

El resto del mes la cartera está en efectivo. Las velas diarias determinan la ventana y las órdenes de mercado ajustan la posición.

## Detalles

- **Instrumento**: ETF de mercado amplio.
- **Ventana**: desde dos días antes del fin de mes hasta el tercer día de negociación del mes siguiente.
- **Posicionamiento**: largo durante la ventana, sin posición en otros momentos.
- **Datos**: velas diarias.
- **Control de riesgo**: operación omitida si el valor de la orden está por debajo de `MinTradeUsd`.
