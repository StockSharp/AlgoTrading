# Estrategia de JBrainTrend1Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de JBrainTrend1Stop** es un port de StockSharp del asesor experto de MetaTrader 5 `Exp_JBrainTrend1Stop`. Combina dos medidas de Average True Range, un oscilador Stochastic y medias móviles Jurik para detectar reversiones de tendencia BrainTrading. Cuando el precio suavizado por Jurik realiza un swing suficientemente grande y el Stochastic sale de su zona neutral, la estrategia cambia el sesgo, actualiza la línea de stop BrainTrend y (opcionalmente) invierte la posición neta después de un retraso configurable.

## Lógica de trading

1. Suscribirse a velas definidas por `CandleType` y alimentarlas a:
   - Un `AverageTrueRange` primario con longitud `AtrPeriod`.
   - Un `AverageTrueRange` extendido con período `AtrPeriod + StopDPeriod`.
   - Un `StochasticOscillator` con `StochasticPeriod` y suavizado de %K de una barra (para coincidir con la configuración MT5).
   - Tres instancias de `JurikMovingAverage` (máximo, mínimo y cierre) configuradas con `JmaLength` y `JmaPhase`.
2. Para cada vela terminada calcular:
   - `range = ATR / 2.3` (coincidiendo con la constante original `d = 2.3`).
   - `range1 = ATR_extended * 1.5` (coincidiendo con `s = 1.5`).
   - `val3 = |JMA_close - JMA_close[shift 2]|` que reproduce la diferencia del buffer MT5.
3. Cuando `val3 > range` y el Stochastic sale de su banda neutral:
   - Si `%K < 47` la estrategia entra en estado BrainTrend bajista (`_trendState = -1`), inicializa el stop de venta en `JMA_high + range1 / 4` y genera una señal de **venta**.
   - Si `%K > 53` la estrategia entra en estado alcista (`_trendState = 1`), inicializa el stop de compra en `JMA_low - range1 / 4` y genera una señal de **compra**.
4. Mientras el estado permanece sin cambios, el stop BrainTrend se arrastra hacia el precio por `range1` (`JMA_high + range1` para tendencias bajistas, `JMA_low - range1` para tendencias alcistas).
5. Las señales se liberan después de `SignalBar` barras completadas. Al ejecutarse:
   - Una señal de compra cierra posiciones cortas (si `SellClose` está habilitado) y opcionalmente abre una nueva larga (si `BuyOpen` está habilitado).
   - Una señal de venta cierra posiciones largas (si `BuyClose` está habilitado) y opcionalmente abre una nueva corta (si `SellOpen` está habilitado).

Los gráficos muestran automáticamente el cierre suavizado por Jurik y el oscilador Stochastic junto con marcadores de operaciones.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Serie de velas procesada por la estrategia. | H4 (marco temporal de 4 horas) |
| `AtrPeriod` | Longitud del ATR primario usado para el desencadenador BrainTrend. | 7 |
| `StochasticPeriod` | Período para %K/%D del oscilador Stochastic (suavizado de %K de una barra). | 9 |
| `StopDPeriod` | Barras adicionales añadidas al período ATR secundario (`AtrPeriod + StopDPeriod`). | 3 |
| `JmaLength` | Longitud de la media móvil Jurik aplicada a máximo/mínimo/cierre. | 7 |
| `JmaPhase` | Argumento de fase enviado a las medias móviles Jurik (limitado a [-100; 100]). | 100 |
| `SignalBar` | Número de barras completadas a esperar antes de disparar una nueva señal. | 1 |
| `BuyOpen` / `SellOpen` | Permitir entrar en posiciones largas/cortas después de una señal. | `true` |
| `BuyClose` / `SellClose` | Permitir cerrar posiciones largas/cortas existentes ante una señal opuesta. | `true` |

Usar la propiedad `Volume` de la estrategia o la configuración del broker para controlar el tamaño de la orden.

## Diferencias con la versión MT5

- El bloque de gestión de dinero original (`MM`, `MMMode`, `Deviation_`, dimensionamiento dinámico de lotes) es reemplazado por el dimensionamiento estándar de órdenes de StockSharp via `Volume` y órdenes de mercado. El control de deslizamiento no está reproducido.
- Las distancias absolutas de stop-loss y take-profit (`StopLoss_`, `TakeProfit_`) no están implementadas. La protección puede configurarse manualmente a través del entorno de hosting si se requiere.
- Los niveles de stop BrainTrend se usan internamente para el tiempo de señal; no se colocan como órdenes pendientes.
- Las medias móviles Jurik dependen de la implementación `JurikMovingAverage` de StockSharp. El parámetro de fase se aplica mediante reflexión, coincidiendo con el comportamiento de otros ports BrainTrading en este repositorio.

## Uso

1. Adjuntar la estrategia a un instrumento y establecer `CandleType` (p. ej., velas de 4 horas para consistencia con el EA).
2. Ajustar los parámetros del indicador (`AtrPeriod`, `StochasticPeriod`, `StopDPeriod`, `JmaLength`, `JmaPhase`) para alinearse con la sensibilidad BrainTrend deseada.
3. Ajustar `SignalBar` para retrasar la ejecución de señales por varias barras completadas si es necesario.
4. Configurar `Volume` y los toggles de apertura/cierre para reflejar la dirección de trading preferida.
5. (Opcional) Agregar gestión de riesgo externa como stop-loss o límites de cartera via la plataforma de hosting.

Una vez ejecutándose, la estrategia rastreará las reversiones BrainTrend, cerrará las posiciones opuestas y opcionalmente cambiará la dirección después del retraso configurado.
