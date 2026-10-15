# Estrategia MACD EMA SAR Bollinger BullBear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina los indicadores MACD, cruce de EMA, Parabolic SAR, Bandas de Bollinger y Bulls/Bears Power. Opera únicamente durante las horas activas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: MACD < Signal, los dos últimos máximos por debajo de la banda superior de Bollinger, EMA3 > EMA34, SAR por debajo del precio, Bulls Power > 0 y disminuyendo.
  - **Corto**: MACD > Signal, EMA3 < EMA34, SAR por encima del precio, Bears Power < 0 y aumentando.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**:
  - Sin reglas de salida dedicadas; la posición se cierra con la señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15 minutos
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
