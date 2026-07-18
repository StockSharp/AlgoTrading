# Estrategia de Histograma Color XTRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en los cambios de dirección de un TRIX suavizado (momentum de media móvil exponencial triple) calculado a partir de precios de cierre logarítmicos. Se abre una posición larga cuando el histograma TRIX gira al alza tras una caída, mientras que se abre una posición corta cuando gira a la baja tras una subida. Las posiciones se invierten en giros opuestos. No se utilizan stop-loss ni take-profit.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `TRIX rising` && `previous TRIX falling`
  - **Corto**: `TRIX falling` && `previous TRIX rising`
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**:
  - Largo: `TRIX turns downward`
  - Corto: `TRIX turns upward`
- **Stops**: No
- **Valores predeterminados**:
  - `TRIX Length` = 5
  - `Smooth Length` = 5
  - `Momentum Period` = 1
  - `Candle Type` = marco temporal 4h
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: TRIX
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
