# Sobres de 5 minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Sobres de 5 minutos** reproduce el experto MetaTrader que intercambia velas de cinco minutos alrededor de una envolvente de media móvil ponderada lineal.
Busca picos de precios que se extienden mucho más allá de las bandas y luego entra en la dirección de reversión a la media.
Un filtro de diferencial, un stop-loss estático, un take-profit opcional y un trailing stop reflejan la gestión del dinero original.

## Lógica de trading
- **Indicador**: Media Móvil Lineal Ponderada (LWMA) calculada sobre el precio medio (alto+bajo)/2 con un periodo de 3.
- **Ancho del sobre**: 0,05% de desviación del valor LWMA (bandas superior e inferior).
- **Detección de señal** (evaluada según la vela completada anteriormente y la oferta actual):
  - **Largo**: el mínimo de la vela anterior se mantiene a más de `DistancePoints` por debajo de la banda inferior **y** la oferta actual también está más allá de esa distancia.
  - **Corto**: el máximo de la vela anterior se mantiene a más de `DistancePoints` por encima de la banda superior **y** la oferta actual también está más allá de esa distancia.
- **Filtros**:
  - Sólo una posición a la vez (las nuevas entradas requieren que la posición actual sea plana).
  - Si `MaxSpreadPoints` es mayor que cero, el diferencial entre oferta y demanda debe permanecer por debajo de este umbral antes de enviar un nuevo pedido.

## Gestión del riesgo
- **Volumen de orden**: el parámetro `TradeVolume` controla el tamaño de la orden de mercado.
- **Stop-loss**: `StopLossPoints` convierte a distancia de precio absoluta utilizando el tamaño del tick del instrumento.
- **Take-profit**: Optional `TakeProfitPoints`; póngalo en cero para desactivarlo.
- **Trailing stop**: Optional `TrailingStopPoints`; póngalo en cero para desactivarlo.
- **Protección**: El asistente `StartProtection` aplica todas las salidas con órdenes de mercado, coincidiendo con el comportamiento de MetaTrader.

## Parámetros
- `TradeVolume = 1m`
- `DistancePoints = 140`
- `EnvelopePeriod = 3`
- `EnvelopeDeviationPercent = 0.05m`
- `StopLossPoints = 250`
- `TakeProfitPoints = 0`
- `TrailingStopPoints = 120`
- `MaxSpreadPoints = 25`
- `CandleType = TimeFrame(5 minutes)`

## Etiquetas
- Categoría: Reversión a la media
- Dirección: Ambos
- Indicators: WeightedMovingAverage
- Stops: Yes (fixed + trailing)
- Plazo: Intradiario (M5)
- Complejidad: Principiante
- Nivel de riesgo: medio
- Estacionalidad: No
- Redes neuronales: No
- Divergencia: No
