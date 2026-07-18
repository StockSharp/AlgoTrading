# Estrategia de Oscilador Ponderado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina RSI, Money Flow Index, Williams %R y DeMarker en un oscilador ponderado suavizado por una media móvil simple. Las posiciones se abren o revierten cuando el oscilador cruza los niveles alto o bajo configurables. El modo de tendencia determina si las operaciones siguen o van en contra de las señales del oscilador.

## Detalles

- **Criterios de entrada**:
  - **Trend = Direct**:
    - **Largo**: el oscilador cae por debajo del nivel bajo.
    - **Corto**: el oscilador sube por encima del nivel alto.
  - **Trend = Against**:
    - **Largo**: el oscilador sube por encima del nivel alto.
    - **Corto**: el oscilador cae por debajo del nivel bajo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El cruce opuesto revierte la posición.
- **Stops**: Sin stops explícitos.
- **Filtros**: Oscilador ponderado con suavizado SMA.
