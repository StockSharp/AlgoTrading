# Estrategia de Fusión RSI-CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina RSI y CCI estandarizados en un único oscilador con bandas dinámicas.
Compra cuando el valor fusionado cruza por encima de la banda inferior y vende o entra en corto cuando cruza por debajo de la banda superior.

## Detalles

- **Criterios de entrada**: la fusión reescalada cruza por encima de la banda inferior para largo; cruza por debajo de la banda superior para corto
- **Largo/Corto**: Ambos (corto opcional)
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Length` = 14
  - `RsiWeight` = 0.5
  - `EnableShort` = false
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, CCI, SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

