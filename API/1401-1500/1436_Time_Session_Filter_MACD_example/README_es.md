# Filtro de Sesión Temporal - Ejemplo MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que demuestra el uso de un filtro de sesión temporal con MACD y EMA de tendencia. Opera solo durante las horas configuradas.

## Detalles

- **Criterios de entrada**: MACD cruza la señal dentro de la sesión activa y precio relativo a la EMA de tendencia.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto o fin de sesión cuando está habilitado.
- **Stops**: No.
- **Valores predeterminados**:
  - `SessionStart` = 11:00
  - `SessionEnd` = 15:00
  - `CloseAtSessionEnd` = false
  - `FastEmaPeriod` = 11
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `TrendMaLength` = 55
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD, EMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
