# Estrategia de Tendencia FATL MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de seguimiento de tendencia basado en el indicador **FATL MACD**. La FATL (Fast Adaptive Trend Line) se resta del precio para producir un oscilador similar al MACD que luego se suaviza mediante una media móvil adaptativa. Los valores positivos indican impulso alcista; los valores negativos indican impulso bajista.

El algoritmo analiza la pendiente de este oscilador en cada vela completada:

- Cuando el valor anterior es inferior al valor anterior a él, el oscilador ha girado hacia arriba. Si el valor actual sube aún más, la estrategia abre una posición larga y cierra cualquier posición corta.
- Cuando el valor anterior es superior al valor anterior a él, el oscilador ha girado hacia abajo. Si el valor actual continúa cayendo, la estrategia abre una posición corta y cierra cualquier posición larga.

Todos los parámetros principales son configurables:

- **Fast EMA** – período de la media móvil rápida del MACD (predeterminado 12).
- **Slow EMA** – período de la media móvil lenta del MACD (predeterminado 26).
- **Signal EMA** – período de la línea de señal del MACD (predeterminado 9).
- **Candle Type** – serie de velas utilizada para el cálculo del indicador.

Las posiciones se abren con órdenes de mercado y se cierran cuando aparece una señal opuesta.
