# Estrategia de Escalpe con Momentum Chaikin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de escalpe utiliza el oscilador Chaikin para capturar cambios de momentum. Las operaciones largas ocurren cuando el oscilador cruza por encima de cero y el precio está por encima de la SMA de 200 períodos. Las operaciones cortas ocurren con un cruce por debajo de cero y el precio por debajo de la SMA. Los múltiplos del ATR definen los niveles de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: El oscilador Chaikin cruza por encima/debajo de cero con el precio por encima/debajo de la SMA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss y take-profit basados en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastLength` = 3
  - `SlowLength` = 10
  - `SmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplierSL` = 1.5m
  - `AtrMultiplierTP` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Chaikin Oscillator, SMA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
