# Líneas de Tendencia Basadas en ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que construye líneas de tendencia basadas en ATR desde puntos pivote y opera sus rupturas.

## Detalles

- **Criterios de entrada**: Ruptura de líneas de tendencia basadas en ATR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Ruptura opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `LookbackLength` = 30
  - `AtrPercent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Price Action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
