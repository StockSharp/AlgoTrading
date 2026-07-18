# Estrategia MACD Volume XAUUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de 15 minutos para XAUUSD que combina el cruce de la línea cero del MACD con un filtro de oscilador de volumen y parámetros de riesgo fijos.

## Detalles

- **Criterios de entrada**: MACD cruzando la línea cero con oscilador de volumen positivo y comparación de volumen.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Niveles de stop-loss o take-profit.
- **Stops**: Stop-loss fijo y multiplicador de take-profit.
- **Valores predeterminados**:
  - `ShortLength` = 5
  - `LongLength` = 8
  - `FastLength` = 16
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Leverage` = 1.0
  - `StopLoss` = 10100
  - `TakeProfitMultiplier` = 1.1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, EMA, Volume
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
