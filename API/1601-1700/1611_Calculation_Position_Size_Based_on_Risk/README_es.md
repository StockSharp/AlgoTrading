# Estrategia de Cálculo del Tamaño de Posición Basado en Riesgo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Demuestra el dimensionamiento de operaciones a partir del riesgo de la cuenta y un porcentaje de stop-loss. Las entradas son aleatorias para mostrar la lógica de cálculo del tamaño de posición.

## Detalles

- **Criterios de entrada**:
  - **Largo**: cada 333 barras.
  - **Corto**: cada 444 barras.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**:
  - Solo stop loss.
- **Stops**: Stop Loss.
- **Valores predeterminados**:
  - `Stop Loss %` = 10
  - `Risk Value` = 2
  - `Risk Is Percent` = true
  - `Long Period` = 333
  - `Short Period` = 444
- **Filtros**:
  - Categoría: Risk Management
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
