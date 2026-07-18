# Estrategia de Cuadrícula Optimizada con KNN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre posiciones largas cuando la línea T3 rápida cruza por encima de la línea T3 lenta y el cambio promedio de precio basado en KNN es positivo. Los umbrales de entrada y salida se ajustan según el cambio promedio. Las posiciones se cierran una vez que la línea T3 rápida cruza por debajo de la lenta y el precio supera el umbral de beneficio.

- **Condiciones de entrada**: `t3Fast > t3Slow` y `averageChange > 0`
- **Condiciones de salida**: `t3Fast < t3Slow` y `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **Indicadores**: T3
