# Estrategia Vortex Indicator Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto de MetaTrader **Exp_VortexIndicator_Duplex** a la API de alto nivel de StockSharp. Se mantienen dos flujos independientes del indicador Vortex: uno gobierna las operaciones largas y el otro las cortas. Cada flujo puede usar su propio marco temporal, longitud de indicador y desplazamiento de barra, lo que permite un comportamiento asimétrico entre configuraciones alcistas y bajistas.

## Cómo funciona

1. Se abren dos suscripciones de velas según `LongCandleType` y `ShortCandleType`. Cada flujo actualiza su propia instancia de `VortexIndicator`.
2. En cada vela finalizada, la estrategia registra los valores más recientes de VI+ y VI-. Los parámetros `LongSignalBar`/`ShortSignalBar` definen cuántas velas cerradas hacia atrás se deben usar para la evaluación de señales, coincidiendo con la entrada `SignalBar` de MetaTrader.
3. **Entrada larga** – permitida cuando `AllowLongEntries = true`. Se envía una orden de compra si el valor actual de VI+ del flujo largo está por encima de VI-, mientras que el valor muestreado anterior tenía VI+ menor o igual a VI-. Cualquier exposición corta existente se cierra antes de establecer la nueva posición larga.
4. **Salida larga** – habilitada mediante `AllowLongExits`. La posición larga se cierra cuando el valor VI- del flujo largo sube por encima de VI+. Además, los niveles de stop-loss y take-profit expresados en pasos de precio (`LongStopLossSteps`, `LongTakeProfitSteps`) se supervisan en cada vela; alcanzar cualquiera de los umbrales también cierra la operación.
5. **Entrada corta** – gobernada por `AllowShortEntries`. Se coloca una orden de venta cuando el VI+ del flujo corto cae por debajo de VI- después de haber estado anteriormente por encima. La exposición larga existente se cierra durante la reversión.
6. **Salida corta** – controlada por `AllowShortExits`. La posición corta se cubre cuando VI+ sube de nuevo por encima de VI-. Las distancias protectoras (`ShortStopLossSteps`, `ShortTakeProfitSteps`) cierran la operación si se alcanzan.
7. El dimensionamiento de posición usa el parámetro `TradeVolume`. La estrategia depende del `PriceStep` del instrumento para convertir conteos de pasos en distancias de precio absolutas; establecer un parámetro de paso en cero deshabilita la regla de protección correspondiente.

Las verificaciones de stop/take se evalúan en cada vela finalizada de ambos marcos temporales. Si la cuenta no tiene posición, los datos de entrada almacenados se borran para reflejar la implementación de MetaTrader.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `LongCandleType` | H4 | Marco temporal usado para el indicador Vortex del lado largo. |
| `ShortCandleType` | H4 | Marco temporal usado para el indicador del lado corto. |
| `LongLength` | 14 | Período de VI aplicado al flujo largo. |
| `ShortLength` | 14 | Período de VI aplicado al flujo corto. |
| `LongSignalBar` | 1 | Desplazamiento de vela cerrada para la evaluación larga (0 = barra finalizada actual). |
| `ShortSignalBar` | 1 | Desplazamiento de vela cerrada para la evaluación corta. |
| `AllowLongEntries` | true | Habilita la apertura de posiciones largas. |
| `AllowLongExits` | true | Habilita el cierre de posiciones largas. |
| `AllowShortEntries` | true | Habilita la apertura de posiciones cortas. |
| `AllowShortExits` | true | Habilita el cierre de posiciones cortas. |
| `LongStopLossSteps` | 1000 | Distancia de stop-loss para operaciones largas, expresada en pasos de precio. |
| `LongTakeProfitSteps` | 2000 | Distancia de take-profit para operaciones largas, expresada en pasos de precio. |
| `ShortStopLossSteps` | 1000 | Distancia de stop-loss para operaciones cortas, expresada en pasos de precio. |
| `ShortTakeProfitSteps` | 2000 | Distancia de take-profit para operaciones cortas, expresada en pasos de precio. |
| `TradeVolume` | 1 | Tamaño base de la orden de mercado utilizado al entrar en una posición. |

## Notas de ejecución

- La estrategia cierra cualquier posición opuesta antes de abrir una nueva, reproduciendo efectivamente el comportamiento de MT5 donde números mágicos separados gestionaban señales largas y cortas.
- Las distancias protectoras se convierten mediante `distance = steps * Security.PriceStep`. Asegúrese de que el instrumento tenga un paso de precio válido; de lo contrario, la estrategia usa 1.0 como respaldo.
- Establezca cualquier parámetro de stop/take en cero para deshabilitar esa ruta de protección mientras mantiene activas las salidas basadas en señales.
- Dado que ambos marcos temporales pueden activar la gestión de riesgo, elija `TradeVolume` con cuidado para evitar reversiones repetidas en mercados delgados.
