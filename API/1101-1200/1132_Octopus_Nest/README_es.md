# Estrategia Octopus Nest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia busca rupturas de compresión utilizando Bandas de Bollinger y Canales de Keltner. La dirección se confirma con EMA y Parabolic SAR. Los stops se colocan en máximos/mínimos recientes con una relación riesgo/recompensa configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio por encima de EMA y PSAR, fuera de la compresión.
  - **Corto**: Precio por debajo de EMA y PSAR, fuera de la compresión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss en extremos recientes y take-profit basado en la relación riesgo/recompensa.
- **Stops**: Sí, fijo por máximo/mínimo reciente.
- **Filtros**: Compresión Bollinger/Keltner, tendencia EMA, dirección PSAR.
