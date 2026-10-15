# Estrategia Xmacd con Modos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador MACD que soporta cuatro modos de entrada diferentes:

- **Breakdown**: abrir operaciones cuando el MACD cruza la línea cero.
- **MacdTwist**: reaccionar a un cambio en la dirección del MACD de bajada a subida o viceversa.
- **SignalTwist**: usar los puntos de giro de la línea de señal como disparadores.
- **MacdDisposition**: operar en los cruces entre el MACD y su línea de señal.

La estrategia se suscribe a velas de 4 horas y calcula un MACD clásico (EMA 12/26 con señal de 9 períodos). Puede abrir y cerrar posiciones ante señales opuestas. El riesgo se gestiona mediante stop loss y take-profit opcionales expresados como porcentajes del precio de entrada.

## Detalles

- **Criterios de entrada**: Señales basadas en MACD dependiendo del modo seleccionado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
  - `Mode` = MacdDisposition
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Swing (4h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
