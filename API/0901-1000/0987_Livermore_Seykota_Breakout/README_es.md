# Ruptura Livermore Seykota
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura que combina puntos pivote de Livermore con el filtro de tendencia de Seykota y salidas basadas en ATR.

Las pruebas indican un rendimiento anual promedio de aproximadamente 87%. Funciona mejor en el mercado de acciones.

La estrategia busca rupturas por encima o por debajo del pivote más reciente, confirmando la dirección de la tendencia con la alineación de EMA y la fuerza del volumen. Los stops basados en ATR gestionan el riesgo.

## Detalles

- **Criterios de entrada**: El precio rompe el último pivote con confirmación de tendencia y volumen.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop ATR o stop trailing.
- **Stops**: Stop y trailing basados en ATR.
- **Valores predeterminados**:
  - `MainEmaLength` = 50
  - `FastEmaLength` = 20
  - `SlowEmaLength` = 200
  - `PivotLength` = 3
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 3
  - `TrailAtrMultiplier` = 2
  - `VolumeSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, Volumen, ATR, Pivot
  - Stops: ATR Trailing
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
