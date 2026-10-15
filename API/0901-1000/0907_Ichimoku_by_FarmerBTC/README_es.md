# Estrategia Ichimoku by FarmerBTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ichimoku by FarmerBTC abre posiciones largas cuando el precio opera por encima de la nube Ichimoku, la nube es alcista, una SMA de marco temporal superior confirma la tendencia alcista y el volumen supera su media móvil multiplicada por un factor. Sale cuando el precio cae por debajo de la nube.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Solo largos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `TenkanPeriod` = 10
  - `KijunPeriod` = 30
  - `SenkouSpanBPeriod` = 53
  - `SmaLength` = 13
  - `VolumeLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CandleType` = 1 hour
  - `HtfCandleType` = 1 day
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Ichimoku, SMA, Volumen
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
