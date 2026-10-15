# Estrategia de Ruptura Gold RR4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gold Breakout RR4 opera rupturas del Canal de Donchian en oro con filtros de volumen y tendencia LWTI. Solo se realiza una operación por día dentro de una sesión especificada y utiliza un riesgo/recompensa fijo de 4:1.

## Detalles

- **Criterios de entrada**: el precio rompe el canal Donchian con volumen superior al promedio y confirmación LWTI dentro de la sesión
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop y objetivo fijos por riesgo/recompensa
- **Stops**: Sí
- **Valores predeterminados**:
  - `DonchianLength` = 96
  - `MaVolumeLength` = 30
  - `LwtiLength` = 25
  - `LwtiSmooth` = 5
  - `StartHour` = 20
  - `EndHour` = 8
  - `RiskReward` = 4
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Donchian Channel, SMA, WMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
