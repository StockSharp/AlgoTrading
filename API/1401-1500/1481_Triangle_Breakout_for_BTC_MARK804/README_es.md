# Estrategia de Ruptura de Triángulo para BTC (MARK804)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera rupturas del triángulo de media móvil simple cuando el volumen se dispara y gestiona las posiciones con stops basados en ATR.

## Detalles

- **Criterios de entrada**: precio que cruza por encima de la línea SMA superior o por debajo de la línea SMA inferior con volumen por encima de su SMA
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o take-profit basados en ATR
- **Stops**: Sí
- **Valores predeterminados**:
  - `TriangleLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrLength` = 14
  - `VolumeMultiplier` = 1.5
  - `AtrMultiplierSl` = 1.0
  - `AtrMultiplierTp` = 1.5
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: SMA, ATR, Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
