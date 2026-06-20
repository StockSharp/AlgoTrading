# Estrategia de Rotación de Estilos por Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia en Python rota entre un conjunto de ETFs de factores y un ETF de mercado amplio. Al final de cada mes, los ETFs se clasifican por su rendimiento total de los últimos tres meses. La cartera invierte completamente en el fondo de mayor rango durante el mes siguiente para capturar el momentum a mediano plazo.

El enfoque siempre mantiene un único ETF y lo reevalúa mensualmente. Se utilizan velas diarias para los cálculos y todas las operaciones de rebalanceo se ejecutan al precio de mercado.

## Detalles

- **Universo**: lista de ETFs de factores y un ETF de referencia de mercado.
- **Señal**: calcular el rendimiento total de 63 días (tres meses) y seleccionar el instrumento más fuerte.
- **Rebalanceo**: primer día de negociación de cada mes.
- **Posicionamiento**: totalmente largo en el ETF seleccionado, todos los demás sin posición.
- **Control de riesgo**: las órdenes se omiten cuando el valor de la operación requerida cae por debajo de `MinTradeUsd`.
