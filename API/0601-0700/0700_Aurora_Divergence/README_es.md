# Estrategia de Divergencia Aurora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera divergencias entre el precio y el On-Balance Volume (OBV). Compara las pendientes de regresión lineal del precio y el OBV para detectar posibles reversiones.

## Características clave

- Comparación de pendientes de regresión lineal para señales de divergencia.
- Filtro z-score opcional para evitar precios sobreextendidos.
- Filtro de media móvil en marco temporal superior para confirmación de tendencia.
- Umbral de volatilidad basado en ATR y gestión de riesgo con stop y objetivo dinámicos.
- Enfriamiento tras cada operación y número máximo de barras en posición.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal de velas para los cálculos principales. |
| `Lookback` | Período para los cálculos de pendiente. |
| `ZLength` | Período para la media y desviación estándar en el filtro z-score. |
| `ZThreshold` | Z-score absoluto máximo permitido para entradas. |
| `UseZFilter` | Activar o desactivar el filtro z-score. |
| `HtfCandleType` | Marco temporal superior para la media móvil de tendencia. |
| `HtfMaLength` | Longitud de la media móvil en el marco temporal superior. |
| `AtrLength` | Período ATR para volatilidad y riesgo. |
| `AtrThreshold` | Valor mínimo de ATR para permitir operaciones. |
| `StopAtrMultiplier` | Multiplicador ATR para la distancia del stop-loss. |
| `ProfitAtrMultiplier` | Multiplicador ATR para la distancia del take-profit. |
| `MaxBarsInTrade` | Número máximo de barras para mantener una posición. |
| `CooldownBars` | Barras de espera tras una operación antes de señalar de nuevo. |

## Complejidad

Intermedio
