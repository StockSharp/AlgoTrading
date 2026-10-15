# Estrategia de Incertidumbre de Política Económica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Incertidumbre de Política Económica (EPU) abre una posición larga cuando la SMA de dos períodos del índice EPU cruza al alza un umbral definido por el usuario. Tras entrar en posición, la estrategia espera un número fijo de barras antes de cerrarla.

Este enfoque busca capturar momentos en que la incertidumbre de política supera los niveles normales.

## Detalles

- **Criterios de entrada**: SMA cruza al alza el umbral.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Salida tras el número especificado de barras.
- **Stops**: No.
- **Valores predeterminados**:
  - `Threshold` = 187
  - `SmaLength` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
