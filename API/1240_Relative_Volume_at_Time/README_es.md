# Volumen Relativo en el Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compara el volumen en un momento específico del día con el volumen promedio de las velas recientes.

## Detalles

- **Criterios de entrada**: volumen relativo por encima del umbral en el momento del día especificado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: volumen relativo vuelve por debajo de 1.
- **Stops**: No.
- **Valores predeterminados**:
  - `Period` = 5
  - `Threshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TargetHour` = 9
  - `TargetMinute` = 30
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: SMA, Volume
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
