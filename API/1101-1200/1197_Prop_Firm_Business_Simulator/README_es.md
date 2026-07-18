# Simulador de Negocio de Prop Firm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que simula la gestión de riesgo de una prop firm utilizando rupturas del Canal Keltner con dimensionamiento de posición basado en el riesgo por operación.

El método coloca órdenes stop en los límites del canal. La cantidad se calcula de modo que la distancia entre las bandas represente el porcentaje elegido del capital de la cuenta.

## Detalles

- **Criterios de entrada**: El precio rompe las bandas del Canal Keltner.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Ruptura de la banda opuesta.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 10
  - `Multiplier` = 2m
  - `RiskPerTrade` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Keltner, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
