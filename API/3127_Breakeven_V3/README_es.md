# Gestor Breakeven V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
El Gestor Breakeven V3 es una conversión del asesor experto de MetaTrader 5 `Breakeven v3 (edición de barabashkakvn)`.
El script original no abre trades. En cambio, calcula continuamente el nivel de break-even del portfolio para el
símbolo seleccionado y mueve las órdenes de protección (stop-loss o take-profit) para cada posición larga y corta abierta
de modo que todo el libro se cierre alrededor de ese precio de break-even con un buffer opcional.

## Lógica de la estrategia
* **Reconstrucción del break-even** – cada vez que se ejecuta un trade o llegan nuevas cotizaciones, la estrategia reconstruye el
  precio promedio ponderado de apertura para la exposición larga y corta por separado. Incluye las comisiones por posición que StockSharp
  informa en los objetos `MyTrade` para reflejar la implementación MQL.
* **Cálculo del precio objetivo** – el precio de break-even se desplaza por `Delta (points)` puntos MetaTrader. El desplazamiento se
  agrega cuando la exposición neta es larga y se resta cuando es corta, replicando el parámetro "Delta" original.
* **Colocación de órdenes de protección** –
  * Cuando la exposición neta es larga, se coloca un **sell limit** de take-profit para el volumen largo total y un **buy stop**
    de stop-loss se adjunta al volumen corto agregado al mismo precio.
  * Cuando la exposición neta es corta, se coloca un **buy limit** de take-profit para el volumen corto completo y un **sell stop**
    de stop-loss protege cualquier cobertura larga.
  * Si ambos lados están planos, todas las órdenes de protección se cancelan.
* **Monitoreo de cotizaciones y diagnósticos** – la estrategia se suscribe a actualizaciones de Nivel 1. El bid/ask más reciente se usa para
  calcular estadísticas de distancia al objetivo y un beneficio flotante estimado. Cuando `Enable Logging` es true, estos valores
  se escriben en el log de la estrategia para emular los comentarios en gráfico de la versión MQL.

## Parámetros
* **Delta (points)** – offset aplicado al precio de break-even calculado. El valor se expresa en puntos MetaTrader,
  es decir, una décima de pip en símbolos FX de cinco dígitos. Por defecto: `100`.
* **Enable Logging** – activa la salida de log detallada describiendo el nivel de break-even actual, la distancia al objetivo y
  el PnL flotante. Por defecto: `true`.

## Notas de uso
* La estrategia es un gestor de trades. Debe lanzarse sobre una estrategia existente o posición manual. No
  abrirá órdenes de mercado por sí misma.
* Al inicio el código inspecciona el portfolio y reconstruye un lote sintético para cada lado de la posición usando
  el precio promedio informado por StockSharp. Para mayor precisión, mantener la estrategia en ejecución cuando se abren nuevos trades.
* Los cargos de swap no están disponibles desde StockSharp, por lo que solo se incluye la información de comisión al reconstruir
  el precio de break-even. Si el broker aplica swaps nocturnos, deben manejarse manualmente.
* El script asume que la cuenta permite cobertura (posiciones largas y cortas simultáneas). Si el broker neta posiciones,
  los agregados largo y corto se reducirán a una única exposición neta igual que en MetaTrader.
* No hay versión Python de este port. Solo se proporciona la implementación C#.
