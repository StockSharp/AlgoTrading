# Estrategia MM Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula los niveles Fibonacci de Murrey Math y opera rupturas. Compra cuando el precio rompe por encima del nivel 100% en un contexto alcista y vende cuando el precio cae por debajo del nivel 0% en un contexto bajista. Las posiciones se cierran cuando el precio cruza el nivel 50% en contra de la operación.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cierra por encima del nivel 100% mientras el extremo más reciente fue un máximo.
  - **Corto**: El precio cierra por debajo del nivel 0% mientras el extremo más reciente fue un mínimo.
- **Criterios de salida**:
  - **Largo**: El precio cae por debajo del nivel 50%.
  - **Corto**: El precio sube por encima del nivel 50%.
- **Indicadores**: Highest, Lowest.
- **Largo/Corto**: Ambos.
- **Stops**: No.
