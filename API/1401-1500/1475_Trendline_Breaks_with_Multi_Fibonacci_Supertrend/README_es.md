# Estrategia de Rupturas de Línea de Tendencia con Multi Fibonacci Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia promedia tres cálculos de SuperTrend que usan multiplicadores Fibonacci (0.618, 1.618, 2.618) y suaviza el resultado con una EMA. Las líneas de tendencia dinámicas se construyen a partir de máximos y mínimos de oscilación con pendientes derivadas del ATR. Se abre una operación larga cuando el precio rompe por encima de la línea de tendencia superior, el SuperTrend suavizado está subiendo y el valor +DI supera a −DI. Las operaciones cortas siguen las mismas reglas de forma inversa.

## Detalles
- **Entrada**: ruptura de línea de tendencia con confirmación DMI y acuerdo del SuperTrend.
- **Salida**: precio que cruza de vuelta sobre la tendencia suavizada o alcanza el stop/objetivo basado en ATR‑.
- **Indicadores**: SuperTrend, ATR, Average Directional Index.
- **Tipo**: ruptura, largo y corto.
