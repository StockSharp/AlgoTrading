# Machine Learning Supertrend TP SL Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Supertrend con take profit y stop loss trailing.

Los niveles de stop y beneficio siguen la línea Supertrend, buscando capturar movimientos sostenidos mientras se aseguran ganancias cuando el impulso se desvanece.

## Detalles

- **Criterios de entrada**: Precio cruzando la línea Supertrend.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o alcanzar el take profit/stop loss trailing.
- **Stops**: Sí, trailing según Supertrend.
- **Valores predeterminados**:
  - `AtrPeriod` = 4
  - `AtrFactor` = 2.94m
  - `StopLossMultiplier` = 0.0025m
  - `TakeProfitMultiplier` = 0.022m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
