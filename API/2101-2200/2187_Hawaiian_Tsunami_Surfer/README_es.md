# Estrategia Hawaiian Tsunami Surfer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca picos repentinos de impulso y opera en su contra. Calcula el cambio porcentual del precio de cierre en una barra utilizando un indicador Momentum. Cuando el cambio porcentual supera un umbral mínimo, el movimiento se considera un "tsunami". La estrategia vende después de un fuerte pico alcista y compra después de un fuerte pico bajista. Se aplican stop-loss y take-profit de protección en pasos de precio a través de StartProtection.

## Detalles

- **Criterios de entrada**:
  - Vender cuando el porcentaje de momentum > `TsunamiStrength`.
  - Comprar cuando el porcentaje de momentum < `-TsunamiStrength`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop-loss o take-profit de protección.
- **Stops**: Sí, a través de StartProtection.
- **Valores predeterminados**:
  - `MomentumPeriod` = 1
  - `TsunamiStrength` = 0.24
  - `TakeProfitPoints` = 500
  - `StopLossPoints` = 700
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Momentum
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
