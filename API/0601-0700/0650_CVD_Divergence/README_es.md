# Divergencia CVD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina la divergencia del delta de volumen acumulado (CVD) con Hull Moving Averages, RSI, MACD y un filtro de volumen. Una operación se abre cuando la tendencia, el momentum y el volumen coinciden y el CVD muestra divergencia o continúa en la dirección del trade. Las posiciones se cierran con señales opuestas o cruce de indicadores.

## Detalles

- **Criterios de entrada**: Alineación de tendencia por HMA, confirmación de RSI y MACD, alto volumen y divergencia/continuación de CVD.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o cruce de indicadores.
- **Stops**: Sin stops explícitos.
- **Valores predeterminados**:
  - `HmaFastLength` = 20
  - `HmaSlowLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5m
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: HMA, RSI, MACD, Volumen
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
