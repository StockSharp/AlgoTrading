# Estrategia BBTrend SuperTrend Decision
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia deriva el valor **BBTrend** a partir de dos Bandas de Bollinger con diferentes longitudes y lo alimenta en un cálculo de SuperTrend. La dirección resultante del SuperTrend decide si abrir posiciones largas o cortas. Se pueden activar protecciones opcionales de toma de ganancias y stop-loss basadas en porcentaje.

## Detalles

- **Criterios de entrada**:
  - Largo: La dirección del SuperTrend es hacia arriba.
  - Corto: La dirección del SuperTrend es hacia abajo.
- **Largo/Corto**: Ambos, configurable.
- **Criterios de salida**:
  - Dirección opuesta del SuperTrend.
- **Stops**: TP/SL porcentual opcional.
- **Valores predeterminados**:
  - Longitud BB corta = 20, Longitud BB larga = 50, StdDev = 2.
  - Longitud SuperTrend = 10, factor = 7.
  - Take Profit = 30%, Stop Loss = 20%.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, SuperTrend
  - Stops: TP/SL opcional
  - Complejidad: Moderado
  - Marco temporal: Corto
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
