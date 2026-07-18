# Estrategia AcceleratorBot USDJPY H4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia AcceleratorBot es una conversión del experto MQL4 original diseñado para USDJPY en el marco temporal H4. Combina la fortaleza de la tendencia del Índice Direccional Promedio (ADX), el momentum del Oscilador Estocástico y los valores de Aceleración/Desaceleración (AC) en múltiples marcos temporales. Los patrones de velas se utilizan como filtros direccionales.

## Detalles

- **Criterios de entrada**: Señales de tendencia o momentum confirmadas por filtros de velas.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta, stop loss, take profit o trailing stop.
- **Stops**: Fijo y Trailing.
- **Valores predeterminados**:
  - `StopLossPoints` = 750
  - `TakeProfitPoints` = 9999
  - `TrailPoints` = 0
  - `AdxPeriod` = 14
  - `AdxThreshold` = 20m
  - `X1` = 0
  - `X2` = 150
  - `X3` = 500
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia y momentum
  - Dirección: Ambos
  - Indicadores: ADX, Stochastic, AC
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
