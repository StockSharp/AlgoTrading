# Estrategia de Confirmación de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina SuperTrend, MACD y VWAP para confirmar tendencias.

## Detalles
- **Criterios de entrada**: Dirección del SuperTrend con confirmación MACD y precio relativo al VWAP.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: MACD cruzando su línea de señal en contra de la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**: Longitud ATR 10, Factor 3, MACD rápido 12, lento 26, señal 9.
- **Filtros**: SuperTrend y VWAP.
