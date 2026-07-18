# Estrategia Trix Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones basándose en el indicador Trix Candle, que aplica una triple media móvil exponencial a los precios de apertura y cierre de las velas y colorea cada vela según si el cierre suavizado está por encima o por debajo de la apertura suavizada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: vela anterior alcista (color 2) y color de vela actual < 2
  - **Corto**: vela anterior bajista (color 0) y color de vela actual > 0
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**:
  - Largo: vela anterior bajista (color 0)
  - Corto: vela anterior alcista (color 2)
- **Stops**: No
- **Valores predeterminados**:
  - `TRIX Period` = 14
  - `Candle Type` = 4h
  - `Allow Buy Open` = true
  - `Allow Sell Open` = true
  - `Allow Buy Close` = true
  - `Allow Sell Close` = true
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Triple Exponential Moving Average
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
