# Estrategia Renko Fractals Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Renko Fractals Grid es un port directo del asesor experto MetaTrader 4 "RENKO FRACTALS GRID". La estrategia opera rompimientos de fractales recientes de Bill Williams confirmados por un filtro de volatilidad estilo Renko, una tendencia de media móvil ponderada y la fuerza del momentum derivada del indicador de tasa de cambio. La versión de StockSharp mantiene la gestión de posiciones en cuadrícula del robot original, incluyendo dimensionamiento de posiciones con martingala, manejo de punto de equilibrio, trailing stops, protección de equity y trailing opcional de beneficio flotante en unidades de moneda.

## Lógica de trading
- **Rompimiento de fractal:** Una configuración larga requiere que el fractal alcista más reciente sea roto por la última vela cerrada mientras al menos uno de los tres cierres previos permaneció por debajo de ese nivel. Las operaciones cortas reflejan este comportamiento con fractales bajistas.
- **Filtro Renko:** La estrategia inspecciona el rango high/low de las últimas _CandlesToRetrace_ barras. Un rompimiento es válido solo cuando el cierre actual está al menos un "bloque" Renko (ya sea una distancia fija en pips o el último valor ATR) alejado de esos extremos.
- **Filtro de tendencia:** Las medias móviles ponderadas rápidas y lentas deben estar alineadas (rápida sobre lenta para largos y por debajo para cortos).
- **Verificación de momentum:** La desviación absoluta de los últimos tres valores de tasa de cambio de 100 debe superar los umbrales configurados. Esto imita el filtro de momentum MQL basado en `iMomentum`.
- **Confirmación MACD:** Las operaciones se permiten solo cuando la línea principal del MACD está en el lado correcto de su línea de señal. La misma verificación se usa para el timing de salida.

## Gestión de riesgos
- **Cuadrícula martingala:** Cada posición adicional multiplica el volumen base por _LotExponent_ mientras el número de operaciones simultáneas está limitado por _MaxTrades_.
- **Stop-loss y take-profit:** Offsets de precio estáticos en pips se aplican desde el precio de entrada promedio.
- **Punto de equilibrio:** Cuando el precio avanza por _BreakEvenTriggerPips_, el stop se mueve a la entrada más _BreakEvenOffsetPips_.
- **Trailing stop:** Un trailing stop basado en velas mantiene la mejor excursión observada desde la entrada.
- **Trailing monetario:** La gestión opcional de beneficio flotante cierra todas las operaciones después de un retroceso de _MoneyStopLoss_ una vez que el beneficio abierto supera _MoneyTakeProfit_.
- **Stop de equity:** La estrategia rastrea el pico de equity en ejecución (basado en el valor del portafolio y PnL abierto). Si la caída supera _EquityRiskPercent_, toda la posición se liquida.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Tipo de vela principal utilizado para todos los indicadores. |
| `FastMaLength` / `SlowMaLength` | Periodos de las medias móviles ponderadas que definen la dirección de la tendencia. |
| `MomentumLength` | Lookback de tasa de cambio para el filtro de momentum. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desviación absoluta mínima de 100 requerida para entradas. |
| `UseAtrFilter` | Usar ATR en lugar de una distancia fija en pips para la confirmación Renko. |
| `BoxSizePips` | Tamaño del bloque Renko sintético cuando el filtrado ATR está deshabilitado. |
| `CandlesToRetrace` | Número de velas inspeccionadas al medir máximos y mínimos recientes. |
| `BaseVolume` | Volumen de operación inicial antes de aplicar el multiplicador martingala. |
| `LotExponent` | Multiplicador aplicado a cada nueva posición en la cuadrícula. |
| `MaxTrades` | Número máximo de posiciones simultáneas por dirección. |
| `StopLossPips` / `TakeProfitPips` | Distancias de stop protector estático y objetivo. |
| `TrailingStopPips` | Distancia del trailing stop en pips (establecer en cero para deshabilitar). |
| `UseBreakEven` | Habilitar mover el stop al punto de equilibrio. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Distancia requerida antes de la activación del punto de equilibrio y el offset aplicado después. |
| `UseMoneyTarget` | Habilitar trailing de beneficio flotante en unidades de moneda. |
| `MoneyTakeProfit` / `MoneyStopLoss` | Umbral de beneficio que activa el trailing monetario y el retroceso máximo permitido. |
| `UseEquityStop` | Habilitar stop-out global basado en equity. |
| `EquityRiskPercent` | Caída máxima permitida desde el pico de equity antes de cerrar todas las operaciones. |

## Notas de implementación
- El EA original evalúa el MACD en el marco temporal mensual. El port de StockSharp usa la misma configuración de indicadores en el marco temporal de trabajo porque los datos multi-marco temporal no están disponibles por defecto.
- Todos los offsets de precio que se originaron de "pips" en MQL se convierten a través del paso de precio del instrumento para trabajar con cotizaciones de pip fraccionadas.
- El seguimiento de beneficio realizado se aproxima a través de eventos de órdenes completadas, lo cual es suficiente para la lógica de caída de equity en ausencia de estadísticas de cuenta proporcionadas por el broker.
- La estrategia utiliza suscripciones de velas de alto nivel con vinculación de indicadores según lo requerido por las directrices del proyecto y mantiene todos los comentarios en línea en inglés como se solicitó.
