# Estrategia de Patrón de Vela Envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera sobre un patrón envolvente seleccionado. Elija entre envolvente alcista o bajista y un lado de la operación para abrir cuando aparezca el patrón. La posición se mantiene durante un número fijo de barras antes de cerrarse.

## Detalles

- **Criterios de entrada**: Patrón envolvente seleccionado (alcista o bajista).
- **Largo/Corto**: Largo o corto configurable.
- **Criterios de salida**: Posición cerrada tras el número especificado de barras.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `HoldPeriods` = 17
  - `Pattern` = Bullish
  - `Side` = Long
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
