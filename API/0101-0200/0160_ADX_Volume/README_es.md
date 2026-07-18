# Estrategia ADX Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia ADX + Volume. Entrar en operaciones cuando el ADX está por encima del umbral con volumen superior al promedio. La dirección se determina por la comparación de DI+ y DI-.

Las pruebas indican un retorno anual promedio de aproximadamente el 67%. Funciona mejor en el mercado de acciones.

Un ADX alto denota una tendencia fuerte y los picos de volumen confirman el compromiso. Las entradas se realizan cuando ambos indicadores muestran fuerza simultáneamente.

Excelente para capturar rupturas enérgicas. Un stop basado en ATR mantiene la exposición bajo control.

## Detalles

- **Criterios de entrada**:
  - Largo: `ADX > AdxThreshold && Volume > AvgVolume`
  - Corto: `ADX > AdxThreshold && Volume > AvgVolume`
- **Largo/Corto**: Ambos
- **Criterios de salida**: La tendencia se debilita por debajo del umbral
- **Stops**: Basados en ATR usando `StopLoss`
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ADX, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
