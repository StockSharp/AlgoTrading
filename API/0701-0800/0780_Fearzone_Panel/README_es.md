# Panel Fearzone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia inspirada en el panel FearZone de «Framgångsrik Aktiehandel». Busca ventas masivas de pánico donde domina el miedo.

La estrategia espera que ambos indicadores Fearzone estén activos y al menos un disparador de pánico, mientras el precio permanezca por encima de la media móvil de 200 períodos.

## Detalles

- **Criterios de entrada**: FZ1 y FZ2 activos más impulso negativo, zona de rebote o estocástico en sobreventa, con cierre por encima de MA200.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: El precio cae por debajo de MA200.
- **Stops**: No.
- **Valores predeterminados**:
  - `LookbackPeriod` = 22
  - `BollingerPeriod` = 200
  - `ImpulsePeriod` = 10
  - `ImpulsePercent` = 0.1m
  - `MaPeriod` = 200
  - `StochThreshold` = 30m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: BollingerBands, RateOfChange, StochasticOscillator, SimpleMovingAverage, Highest
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
