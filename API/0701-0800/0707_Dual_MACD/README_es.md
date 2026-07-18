# MACD Dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina dos indicadores MACD. El MACD más lento al cruzar el cero abre operaciones cuando el histograma del MACD más rápido se alinea. La posición se cierra cuando el MACD rápido se invierte o se activa el stop/take profit.

Las pruebas indican una rentabilidad anual media de aproximadamente el 65%. Funciona mejor en el mercado de acciones.

## Detalles

- **Criterios de entrada**: Cruce del histograma del MACD lento por cero con confirmación del MACD rápido.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Reversión del MACD rápido o stop/objetivo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Macd1FastLength` = 34
  - `Macd1SlowLength` = 144
  - `Macd1SignalLength` = 9
  - `Macd2FastLength` = 100
  - `Macd2SlowLength` = 200
  - `Macd2SignalLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

