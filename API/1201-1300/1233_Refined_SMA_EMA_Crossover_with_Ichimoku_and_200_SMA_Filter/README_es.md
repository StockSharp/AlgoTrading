# Estrategia de Cruce SMA EMA Refinado con Ichimoku y Filtro de 200 SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina un cruce corto de SMA/EMA con filtros de Ichimoku Cloud y SMA de 200 períodos. Va largo cuando SMA cruza por encima de EMA, por encima de la nube y de la SMA 200. Vende cuando SMA cruza por debajo de EMA, por debajo de la nube y de la SMA 200.

## Detalles

- **Criterios de entrada:**
  - **Largo:** SMA cruza por encima de EMA, precio por encima de la nube Ichimoku, precio por encima de la SMA 200.
  - **Corto:** SMA cruza por debajo de EMA, precio por debajo de la nube Ichimoku, precio por debajo de la SMA 200.
- **Criterios de salida:** señal inversa.
- **Indicadores:** Ichimoku Cloud, SMA, EMA, 200 SMA.
