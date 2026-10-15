# Estrategia de Cruce Vortex con Confirmación de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Vortex para detectar reversiones de tendencia y confirma las entradas con una media móvil suavizada. Se abre una operación larga cuando el Vortex positivo cruza por encima del negativo y el precio está por encima de la línea de suavizado. Una operación corta ocurre en el cruce opuesto por debajo de la línea.

## Parámetros
- **Vortex Length** – período para el cálculo del Vortex.
- **SMA Length** – longitud de la SMA base.
- **Smoothing Length** – longitud de la media móvil de suavizado.
- **MA Type** – método de suavizado.
- **Candle Type** – marco temporal de las velas procesadas.
