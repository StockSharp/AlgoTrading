# Gann Laplace Híbrido VSA Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un filtro de tendencia al estilo Gann con análisis de spread de volumen (VSA) suavizado con Laplace. El valor VSA se calcula como el spread de precio dividido entre el rango de la vela y multiplicado por el volumen, luego suavizado con una EMA. Se abren operaciones cuando el VSA suavizado se alinea con el precio relativo a la media móvil de tendencia.

## Detalles

- **Criterios de entrada**:
  - **Largo**: VSA suavizado > 0 y cierre > MA de tendencia.
  - **Corto**: VSA suavizado < 0 y cierre < MA de tendencia.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: el VSA suavizado se vuelve negativo.
  - **Corto**: el VSA suavizado se vuelve positivo.
- **Stops**: Usa StartProtection.
- **Valores predeterminados**:
  - `Trend Period` = 20
  - `VSA Smoothing` = 14
  - `Candle Type` = 15m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MA, Volumen
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Medio plazo
