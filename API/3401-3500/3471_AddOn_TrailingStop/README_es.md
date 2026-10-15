# Parada dinámica adicional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto del experto MetaTrader **AddOn_TrailingStop**. La estrategia no abre posiciones por sí sola y solo ajusta los trailingstops para una posición neta existente.

## como funciona

- Se suscribe a los datos de Level1 para monitorear las mejores ofertas y solicitar cotizaciones más recientes.
- Calcula el tamaño del pip a partir de los decimales de seguridad para que las entradas se comporten como en MetaTrader (4/5 dígitos = 0,0001 pip, 2/3 dígitos = 0,01 pip).
- Cuando se abre una posición larga y el precio de oferta avanza `TrailingStartPips` pips, la estrategia mueve el trailing stop interno a `Bid - TrailingStartPips` pips.
- El stop largo sólo avanza cuando el nuevo nivel es al menos `TrailingStepPips` pips más alto que el stop anterior.
- Cuando se abre una posición corta y el precio de venta cae `TrailingStartPips` pips, la estrategia mueve el trailing stop interno a `Ask + TrailingStartPips` pips.
- La parada corta solo avanza cuando el nuevo nivel es al menos `TrailingStepPips` pips más bajo que la parada anterior.
- Si la cotización actual cruza el trailing stop, la estrategia cierra toda la posición en el mercado y restablece su estado.

## Parámetros

- `EnableTrailing` (predeterminado **verdadero**): habilita o deshabilita la administración de trailing stop.
- `TrailingStartPips` (predeterminado **15**): se requiere ganancia en pips antes de que se active el seguimiento.
- `TrailingStepPips` (predeterminado **5**): se requiere ganancia adicional en pips antes de que el stop pueda moverse nuevamente.
- `MagicNumber` (predeterminado **0**): identificador mantenido para lograr paridad con el experto MQL. Es informativo porque StockSharp opera en la posición de la estrategia actual.

## Notas

- Requiere una fuente de datos configurada `Security`, `Portfolio` y Nivel 1.
- Diseñado para complementar otras estrategias que manejan entradas.
- Utiliza `StrategyParam<T>` para que cada entrada pueda optimizarse o exponerse en la interfaz de usuario.
- Envía órdenes `BuyMarket`/`SellMarket` cuando se alcanza el trailing stop porque StockSharp gestiona automáticamente las órdenes de protección después de salir de la posición.
