# Estrategia Color RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera señales del indicador MACD que se puede analizar en cuatro modos diferentes:

- **Breakdown** – operar cuando el histograma del MACD cruza la línea cero.
- **MACD Twist** – operar cuando la línea MACD cambia de dirección.
- **Signal Twist** – operar cuando la línea de señal cambia de dirección.
- **MACD Disposition** – operar en los cruces entre la línea MACD y la línea de señal.

Cada modo puede abrir o cerrar posiciones largas y cortas de forma independiente usando los indicadores correspondientes.

No se utilizan niveles de stop loss ni take profit por defecto.

## Detalles

- **Criterios de entrada**: señal del indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 4 horas
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `Mode` = MACD Disposition
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
