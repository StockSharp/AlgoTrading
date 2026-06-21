# Estrategia de Tendencia de Mercado por Niveles Sin Repintado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de EMA que opcionalmente filtra las operaciones usando RSI. Las posiciones largas se abren cuando la EMA rápida cruza por encima de la EMA lenta, mientras que las operaciones cortas se activan en el cruce opuesto. Cuando `ApplyExitFilters` está habilitado y el filtro RSI está activo, las posiciones se cierran si el RSI sale de la zona permitida.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Fast EMA` cruza por encima de `Slow EMA` y `RSI > RsiLongThreshold` cuando está habilitado
  - **Corto**: `Fast EMA` cruza por debajo de `Slow EMA` y `RSI < RsiShortThreshold` cuando está habilitado
- **Criterios de salida**: Cruce opuesto o fallo del filtro RSI cuando `ApplyExitFilters` es verdadero
- **Tipo**: Seguimiento de tendencia
- **Indicadores**: EMA, RSI
- **Marco temporal**: 5 minutos (por defecto)
