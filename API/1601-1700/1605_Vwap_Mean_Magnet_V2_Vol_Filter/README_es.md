# Estrategia VWAP Mean Magnet v2 (Filtro de Volumen)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina el concepto de reversión a la media del VWAP con RSI y un filtro de volumen. Las operaciones se realizan cuando el precio se desvía del VWAP y el RSI alcanza niveles extremos, siempre que el volumen actual supere la media móvil multiplicada por un factor.

## Detalles

- **Criterios de entrada**:
  - **Largo**: precio < VWAP, RSI < sobrevendido, filtro de volumen supera.
  - **Corto**: precio > VWAP, RSI > sobrecomprado, filtro de volumen supera.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cierre de posición cuando el precio regresa al VWAP.
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Volume lookback` = 20
  - `Volume multiplier` = 3
  - `Stop loss %` = 0.5
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
