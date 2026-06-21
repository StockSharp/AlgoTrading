# Estrategia Turtle Trader V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Turtle Trader V1 combina múltiples osciladores de momentum con un filtro de media móvil. Se abre una posición larga cuando la EMA rápida está por encima de la EMA lenta y RSI, Stochastic, CCI, Momentum y el oscilador Chaikin apuntan hacia arriba. Los cortos requieren las condiciones opuestas.

## Detalles

- **Criterios de entrada**:
  - EMA rápida por encima de la EMA lenta (por debajo para cortos)
  - RSI subiendo y por debajo de 70 para largos, RSI bajando y por encima de 30 para cortos
  - Stochastic %K por debajo de 88 para largos, por encima de 12 para cortos
  - CCI y Momentum aumentando para largos, disminuyendo para cortos
  - Oscilador Chaikin moviéndose en la dirección de la operación
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: ninguno por defecto
- **Valores predeterminados**:
  - `FastMaPeriod` = 10
  - `SlowMaPeriod` = 50
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `CciPeriod` = 20
  - `MomentumPeriod` = 10
  - `ChoFastPeriod` = 3
  - `ChoSlowPeriod` = 10
- **Filtros**:
  - Categoría: Seguimiento de tendencia / Momentum
  - Dirección: Ambos
  - Indicadores: EMA, RSI, Stochastic, CCI, Momentum, Chaikin Oscillator
  - Stops: Ninguno
  - Complejidad: Avanzado
  - Marco temporal: 1 hora
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
