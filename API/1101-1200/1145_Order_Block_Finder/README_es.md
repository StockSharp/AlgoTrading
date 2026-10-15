# Estrategia de Búsqueda de Bloques de Órdenes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia identifica bloques de órdenes alcistas y bajistas basándose en un número especificado de velas consecutivas y un movimiento porcentual mínimo. Cuando se detecta un bloque de órdenes alcista, la estrategia compra; cuando se encuentra un bloque bajista, vende.

## Parámetros
- **Relevant Periods** – número de velas posteriores para confirmar un bloque de órdenes
- **Min Percent Move** – cambio porcentual mínimo entre el bloque y la última vela de confirmación
- **Use Whole Range** – usar el rango High/Low en lugar de los límites basados en Open
- **Candle Type** – tipo de vela utilizado para los cálculos
