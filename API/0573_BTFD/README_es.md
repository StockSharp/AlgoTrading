# BTFD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de compra en caídas basada en volumen y RSI, con cinco niveles de take-profit y un stop de protección.

## Detalles

- **Criterios de entrada**: Pico de volumen por encima de SMA y RSI por debajo de la zona de sobreventa.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cinco objetivos de take-profit escalonados o stop loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `VolumeLength` = 70
  - `VolumeMultiplier` = 2.5
  - `RsiLength` = 20
  - `RsiOversold` = 30
  - `Tp1` = 0.4
  - `Tp2` = 0.6
  - `Tp3` = 0.8
  - `Tp4` = 1.0
  - `Tp5` = 1.2
  - `Q1` = 20
  - `Q2` = 40
  - `Q3` = 60
  - `Q4` = 80
  - `Q5` = 100
  - `StopLossPercent` = 5
  - `CandleType` = TimeSpan.FromMinutes(3)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: RSI, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (3m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
