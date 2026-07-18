# Estrategia RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simple basada en el Índice de Fuerza Relativa. Compra cuando el RSI cruza por encima del nivel de sobreventa y vende cuando cruza por debajo del nivel de sobrecompra.

## Detalles

- **Criterios de entrada**:
  - Largo: RSI cruza por encima de `OverSold`
  - Corto: RSI cruza por debajo de `OverBought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `OverSold` = 25m
  - `OverBought` = 75m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
