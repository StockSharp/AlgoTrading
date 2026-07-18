# Estrategia de Velas Ejecutoras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión directa del experto MetaTrader "Executor Candles". Reacciona a un rico conjunto de patrones de reversión alcistas y bajistas de velas y puede opcionalmente confirmar operaciones con una vela de tendencia de mayor timeframe. Toda la lógica de gestión de operaciones – stops, take-profits y trailing stops – refleja el comportamiento del experto original medido en pips (pasos de precio).

## Cómo funciona

- **Filtro de tendencia**: Cuando `UseTrendFilter` está habilitado, la estrategia observa la vela terminada más reciente de `TrendCandleType`. Los setups largos solo se permiten si esa vela cerró alcista, mientras que los setups cortos requieren un cierre bajista. Con el filtro desactivado (predeterminado), solo se usa la lógica de patrones.
- **Patrones largos**: Martillo, envolvente alcista, línea penetrante, estrella de la mañana y estructuras de estrella doji de la mañana tomadas de las últimas tres velas de trading completadas.
- **Patrones cortos**: Hombre colgado, envolvente bajista, cobertura de nube oscura, estrella vespertina y confirmaciones de estrella doji vespertina.
- **Gestión de operaciones**:
  - Distancias separadas de stop-loss y take-profit para posiciones largas y cortas expresadas en pips (`StopLossBuyPips`, `TakeProfitBuyPips`, `StopLossSellPips`, `TakeProfitSellPips`).
  - Trailing stops opcionales para ambas direcciones controlados por `TrailingStopBuyPips`, `TrailingStopSellPips` y el desplazamiento mínimo `TrailingStepPips`. Una actualización de trailing se hace solo después de que el precio avanza por la distancia del stop más el paso de trailing, replicando la lógica de MetaTrader.
  - Las órdenes se colocan con `OrderVolume` lotes y la posición actual se revierte completamente con órdenes de mercado cuando se activa una condición de salida.

La estrategia se suscribe al `CandleType` configurado para señales de trading y, si es necesario, al `TrendCandleType` para la vela de confirmación. Mantiene un búfer interno de las últimas tres velas de trading terminadas para evaluar los patrones de múltiples barras sin almacenar historiales largos.

## Parámetros

- `CandleType` – timeframe usado para detectar los patrones de velas.
- `TrendCandleType` – vela de mayor timeframe usada cuando el filtro de tendencia está activo.
- `OrderVolume` – tamaño de orden para entradas y salidas de mercado.
- `StopLossBuyPips`, `TakeProfitBuyPips`, `TrailingStopBuyPips` – controles de riesgo para posiciones largas.
- `StopLossSellPips`, `TakeProfitSellPips`, `TrailingStopSellPips` – controles de riesgo para posiciones cortas.
- `TrailingStepPips` – movimiento favorable mínimo antes de que el trailing stop se ajuste.
- `UseTrendFilter` – habilita o deshabilita la confirmación de mayor timeframe.

## Notas

- Todas las distancias basadas en pips se multiplican por el `PriceStep` del instrumento. Asegúrese de que esté configurado correctamente para niveles de riesgo precisos.
- Las verificaciones de entrada se ejecutan en cada vela terminada; los ticks en vivo simplemente actualizan la barra más reciente sin cambiar el flujo de decisiones.
- La estrategia emite solo órdenes de mercado y espera que la ejecución ocurra inmediatamente como en la versión de MetaTrader.
