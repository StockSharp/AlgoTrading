# Estrategia de Distancia de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera reversiones de Bollinger Bands con un filtro de distancia adicional. Vende cuando el precio cierra por encima de la banda superior más una distancia establecida y compra cuando cierra por debajo de la banda inferior menos la misma distancia. Las posiciones se cierran por objetivo de ganancia o stop loss medidos en pasos de precio.

## Detalles

- **Criterios de entrada**:
  - Largo: cierre por debajo de la banda inferior de Bollinger menos distancia
  - Corto: cierre por encima de la banda superior de Bollinger más distancia
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Objetivo de ganancia alcanzado
  - Stop loss alcanzado
- **Stops**: Absolutos en pasos de precio
- **Valores predeterminados**:
  - `BollingerPeriod` = 4
  - `BollingerDeviation` = 2m
  - `BandDistance` = 3m
  - `ProfitTarget` = 3m
  - `LossLimit` = 20m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
