# Estrategia de Pulso de Volatilidad con Salida Dinámica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en momentum que detecta la expansión de volatilidad. Entra en la dirección del momentum cuando el ATR supera su media y sale usando un stop y take profit basados en ATR tras un período de mantenimiento.

## Detalles

- **Criterios de entrada**: Expansión de volatilidad ATR con confirmación de momentum
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss y take profit establecidos tras el período de mantenimiento
- **Stops**: Stop basado en ATR, take profit por relación riesgo-recompensa
- **Valores predeterminados**:
  - `AtrLength` = 14
  - `MomentumLength` = 20
  - `VolThreshold` = 0.5
  - `MinVolatility` = 1.0
  - `ExitBars` = 42
  - `RiskReward` = 2
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: ATR, SMA, Momentum
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
