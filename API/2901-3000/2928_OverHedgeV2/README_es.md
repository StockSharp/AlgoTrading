# Estrategia de Cuadrícula OverHedgeV2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el asesor experto OverHedge V2 de MetaTrader en la API de alto nivel de StockSharp. Construye una cuadrícula hedgeada siguiendo la dirección de una EMA rápida y una lenta, luego alterna órdenes largas y cortas dentro de un túnel dinámico. Las posiciones se añaden según una progresión geométrica de lotes y toda la cesta se liquida una vez que el beneficio no realizado agregado alcanza el objetivo configurado.

## Lógica de trading

- **Filtro de tendencia:** Una EMA de 8 períodos debe divergir de una EMA de 21 períodos al menos `MinDistancePips`. El filtro decide la dirección de la primera operación en cada ciclo.
- **Túnel de cuadrícula:** El ancho del túnel es igual al spread actual multiplicado por dos más `TunnelWidthPips` convertido a unidades de precio. Define el disparador del lado opuesto una vez que el ciclo comienza.
- **Alternancia de órdenes:** Las primeras tres posiciones se abren en la dirección de la tendencia. Después el algoritmo alterna de lado para hedgear la exposición usando los mismos anclajes de túnel como referencia.
- **Escalada de lotes:** Cada orden subsiguiente multiplica el volumen anterior por `BaseMultiplier` comenzando desde `StartVolume`. El tamaño se alinea a las restricciones de volumen del instrumento.
- **Salida del ciclo:** Cuando la ganancia no realizada neta por lote de instrumento está por encima de `MinProfitTargetPips` y el beneficio total de la cesta supera `ProfitTargetPips`, la estrategia cierra todas las posiciones abiertas y restablece el estado.
- **Apagado manual:** Establecer `ShutdownGrid` en `true` cierra cualquier posición restante y evita nuevas órdenes hasta que se desactiva.

## Condiciones de entrada

### Entradas largas
- El filtro de tendencia indica una tendencia alcista (`EMA_short - EMA_long > MinDistancePips`).
- El precio Ask es mayor o igual al anclaje de compra actual.
- La estrategia no está en modo de apagado y la cesta no ha alcanzado su objetivo de beneficio.

### Entradas cortas
- El filtro de tendencia indica una tendencia bajista (`EMA_long - EMA_short > MinDistancePips`).
- El precio Ask es menor o igual al anclaje de venta actual.
- La bandera de apagado es falsa y el objetivo de beneficio de la cesta aún no se ha alcanzado.

## Gestión de salida

- **Salida de beneficio:** Cuando el beneficio no realizado de la cesta satisface `ProfitTargetPips` con cada lado abierto ganando al menos `MinProfitTargetPips` por lote, todas las posiciones se cierran a mercado.
- **Salida de emergencia:** Establecer `ShutdownGrid` en `true` cierra inmediatamente cualquier exposición abierta.

## Indicadores y datos

- EMA de 8 períodos (rápida) y EMA de 21 períodos (lenta) calculadas en la serie de velas configurada.
- La suscripción de Nivel 1 se usa para rastrear el mejor bid/ask para construir el túnel y comparar las condiciones de entrada con los spreads en tiempo real.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `StartVolume` | Volumen inicial de la primera orden en un ciclo. |
| `BaseMultiplier` | Multiplicador geométrico aplicado al volumen de cada orden subsiguiente. |
| `TunnelWidthPips` | Ancho de túnel adicional en pips añadido al doble del spread actual. |
| `ProfitTargetPips` | Objetivo de beneficio de la cesta medido en pips convertidos a distancia de precio. |
| `MinProfitTargetPips` | Movimiento mínimo favorable por lado antes de que la cesta pueda cerrarse. |
| `ShortEmaPeriod` | Período de la EMA rápida usada para confirmación de dirección. |
| `LongEmaPeriod` | Período de la EMA lenta usada para confirmación de dirección. |
| `MinDistancePips` | Separación mínima de EMA requerida para declarar una tendencia. |
| `CandleType` | Marco temporal de las velas que alimentan las EMA y el bucle de trading. |
| `ShutdownGrid` | Interruptor booleano que fuerza la liquidación y bloquea nuevas operaciones. |

## Notas prácticas

- El período de vela predeterminado es una hora; ajústelo para que coincida con el marco temporal usado en el EA original.
- La estrategia depende de datos de mejor bid/ask; proporcione cotizaciones de Nivel 1 durante el trading en vivo o el backtesting.
- Debido a que StockSharp mantiene una posición neta por instrumento, las compras y ventas alternantes reducirán o revertirán la exposición neta en lugar de mantener tickets hedgeados independientes, pero la lógica de la cesta sigue imitando la captura de beneficio prevista.
- Siempre verifique los pasos de volumen específicos del instrumento y los tamaños de tick para que el túnel generado y la escala de lotes coincidan con el mercado que opera.
