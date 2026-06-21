# Estrategia Smart Fib
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza la ruptura de una media móvil simple para las entradas y bandas de Fibonacci basadas en ATR para las salidas.

## Detalles

- **Criterios de entrada**: Cierre cruzando por encima o por debajo de la SMA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio alcanza la banda Fibonacci ATR.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SmaLength` = 50
  - `FibSmaLength` = 8
  - `AtrLength` = 6
  - `FirstFactor` = 1.618
  - `SecondFactor` = 2.618
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
