# Heikin Ashi Consecutive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en velas Heikin Ashi consecutivas

Las pruebas indican un retorno anual promedio de aproximadamente 73%. Funciona mejor en el mercado de criptomonedas.

Heikin Ashi Consecutive espera varias velas Heikin Ashi del mismo color para confirmar el momentum. Tras una racha de barras alcistas o bajistas, la estrategia se une al movimiento y sale en la primera vela opuesta o con un stop ATR.

Dado que los gráficos Heikin Ashi suavizan los datos de precios, una serie de velas del mismo color destaca un movimiento direccional fuerte. El stop ATR Trailing intenta bloquear las ganancias si la secuencia se revierte abruptamente.


## Detalles

- **Criterios de entrada**: Señales basadas en Heikin.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ConsecutiveCandles` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Heikin
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

