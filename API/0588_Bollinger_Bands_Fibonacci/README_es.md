# Estrategia de Bollinger Bands y Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de Bandas de Bollinger filtradas por niveles de Fibonacci. Se abre una posición larga cuando el precio cruza por encima de la banda superior y el mínimo de la vela está por encima de un soporte basado en Fibonacci. Se abre una posición corta cuando el precio cruza por debajo de la banda inferior y el máximo de la vela está por debajo de una resistencia basada en Fibonacci.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la banda superior y mínimo > Fibonacci bajo.
  - **Corto**: El cierre cruza por debajo de la banda inferior y máximo < Fibonacci alto.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: El cierre cruza por debajo de la banda media.
  - **Corto**: El cierre cruza por encima de la banda media.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2
  - `FibonacciLength` = 50
  - `FibonacciLevel0` = 0
  - `FibonacciLevel100` = 1
- **Filtros**:
  - Categoría: Ruptura de bandas
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Highest, Lowest
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: 1H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
