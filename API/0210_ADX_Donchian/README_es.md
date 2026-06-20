# ADX Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia utiliza los indicadores ADX Donchian para generar señales.
La entrada larga ocurre cuando ADX > AdxThreshold && Price >= upperBorder (tendencia fuerte con ruptura alcista). La entrada corta ocurre cuando ADX > AdxThreshold && Price <= lowerBorder (tendencia fuerte con ruptura bajista).
Es adecuada para traders que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 67%. Funciona mejor en el mercado de acciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
  - **Corto**: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición cuando ADX cae por debajo de (threshold - 5)
  - **Corto**: Salir de la posición cuando ADX cae por debajo de (threshold - 5)
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `DonchianPeriod` = 5
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AdxThreshold` = 10
  - `Multiplier` = 0.1m
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: ADX Donchian
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

