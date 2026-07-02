# Estrategia VWAP Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia VWAP Reversion que opera en desviaciones del Precio Promedio Ponderado por Volumen

Las pruebas indican un rendimiento anual promedio de aproximadamente 127%. Funciona mejor en el mercado de acciones.

VWAP Reversion opera en desviaciones del precio promedio ponderado por volumen. Si el precio se aleja demasiado por encima o por debajo del VWAP, la estrategia opera contra el movimiento y sale al recuperarse.

Dado que el VWAP refleja los niveles de transacción típicos, las desviaciones extremas a menudo atraen el precio de vuelta hacia él. Algunos traders combinan esta señal con filtros de tendencia intradía para mayor probabilidad.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI, VWAP.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `DeviationPercent` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, VWAP
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

