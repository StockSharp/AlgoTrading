# Estrategia de Trading Fibonacci Contra-Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza una Media Móvil Ponderada por Volumen (VWMA) y la desviación estándar para construir bandas de Fibonacci. Entra en largo cuando el precio cae por debajo de la banda inferior seleccionada y en corto cuando el precio sube por encima de la banda superior. Opcionalmente, las posiciones se cierran cuando el precio cruza la base VWMA.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por debajo de la banda inferior elegida.
  - **Corto**: El cierre cruza por encima de la banda superior elegida.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Base**: Salida opcional cuando el precio cruza la VWMA.
  - **Reverso**: La señal de la banda opuesta revierte la posición.
- **Stops**: Ninguno.
- **Indicadores**: VolumeWeightedMovingAverage, StandardDeviation.
