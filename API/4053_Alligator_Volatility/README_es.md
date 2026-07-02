# Alligator Estrategia de volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de volatilidad Alligator es una versión de alto nivel StockSharp del asesor experto "Alligator vol 1.1" MetaTrader. Combina el indicador Bill Williams' Alligator con confirmación de ruptura fractal opcional, órdenes promedio estilo martingala y gestión de riesgos de seguimiento. El módulo está destinado a operadores discrecionales que desean automatizar el flujo de trabajo original manteniendo un control granular sobre el tamaño y los filtros de las posiciones.

## Descripción general de la lógica

- Se suscribe a las velas del período de tiempo seleccionado y calcula tres promedios móviles suavizados (mandíbula, dientes, labios) que forman el indicador Alligator.
- Detecta fases alcistas cuando los labios permanecen por encima de la mandíbula al menos el `EntryGap` configurado y permanecen por encima de los dientes el `ExitGap`. Las fases bajistas requieren que la mandíbula domine los labios mientras se mantiene por encima de los dientes.
- Realiza un seguimiento de los fractales de Bill Williams dentro de las últimas `FractalBars` velas. El filtro de ruptura fractal es opcional y garantiza nuevos máximos para las posiciones largas o nuevos mínimos para las posiciones cortas.
- Realiza una orden de mercado inicial una vez que aparece un nuevo estado Alligator. Cuando la martingala está habilitada, las órdenes límite promedio adicionales se distribuyen alrededor de múltiplos de la distancia de stop-loss con un tamaño de posición exponencial.
- Gestiona las salidas de posiciones mediante toma de ganancias, stop-loss, trailing stop opcional y reversión de estado Alligator opcional.

## Reglas de entrada

1. La estrategia espera velas terminadas e ignora datos parciales.
2. Una configuración larga requiere uno de los siguientes:
   - Alligator entrada habilitada, el estado alcista cambia de falso a verdadero y (si está habilitado) un fractal superior válido está al menos a `FractalDistancePips` del cierre actual.
   - La entrada Alligator está deshabilitada, pero (si está habilitada) la condición de ruptura fractal aún pasa.
3. Una configuración corta refleja las condiciones largas utilizando el estado bajista Alligator y fractales inferiores.
4. El parámetro `ManualMode` bloquea las entradas automáticas, lo que permite el envío de pedidos discrecionales a través de la interfaz de usuario.
5. Cuando `OnlyOnePosition` es verdadero, la estrategia se niega a abrir una nueva posición si ya existe una exposición opuesta.

## reglas de salida

- Las paradas y objetivos iniciales se colocan inmediatamente después de que aumenta la posición. Las distancias se calculan a partir del precio de entrada promedio usando `StopLossPips` y `TakeProfitPips` convertido con el paso de precio del instrumento.
- Si `EnableTrailing` es verdadero, el stop sigue al precio después de que la operación obtenga al menos `TrailingActivationPips` de ganancia. Los largos se sitúan por debajo del cierre/máximo más alto de la vela, los cortos se sitúan por encima del cierre/mínimo más bajo.
- Cuando `UseAlligatorExit` está activo, la posición se cierra una vez que el estado Alligator colapsa (el estado alcista desaparece para los largos o el estado bajista desaparece para los cortos).
- Alcanzar el precio de toma de ganancias o límite de pérdidas cierra la posición y cancela las órdenes promedio pendientes en ese lado.

## Martingale cuadrícula

- `EnableMartingale` activa una escalera de órdenes limitadas después de la entrada al mercado.
- Cada paso multiplica el volumen ejecutado anteriormente por `2 * MartingaleMultiplier` (con un límite de `MaxVolume`).
- Los precios límite están espaciados por la distancia del límite de pérdidas (`StopLossPips`) y se desplazan en `GridSpreadPips` para compensar el diferencial del corredor.
- Las órdenes pendientes se cancelan cada vez que se procesa una nueva señal, se aplana la posición o se produce una salida manual.

## gestión del dinero

- El volumen de pedidos se calcula a partir del capital de la cuenta usando `RiskPerThousand`: `volume = equity / 1000 * RiskPerThousand`.
- `MinVolume` actúa como respaldo cuando la información sobre el capital no está disponible. `MaxVolume` limita tanto los pasos comerciales iniciales como los de martingala.
- Todos los precios se redondean al tick de cambio más cercano antes de enviar los pedidos.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Tipo de datos utilizado para la suscripción de velas. | plazo de 15 minutos |
| `ManualMode` | Deshabilite las entradas automáticas cuando sea cierto. | `false` |
| `UseAlligatorEntry` | Requiere expansión Alligator antes de ingresar. | `true` |
| `UseFractalFilter` | Enforce fractal breakout confirmation. | `false` |
| `UseAlligatorExit` | Cierre las operaciones cuando el Alligator colapse. | `false` |
| `OnlyOnePosition` | Permitir sólo una única posición abierta. | `true` |
| `EnableMartingale` | Agregue órdenes de límite promedio. | `true` |
| `EnableTrailing` | Activar la gestión de trailing stop. | `true` |
| `RiskPerThousand` | Multiplicador de volumen basado en acciones. | `0.04` |
| `MaxVolume` | Tamaño máximo de pedido permitido. | `0.5` |
| `MinVolume` | Tamaño del pedido alternativo. | `0.01` |
| `StopLossPips` / `TakeProfitPips` | Distancia hasta detenerse y apuntar en pips. | `80` |
| `TrailingStopPips` | Distancia del trailing stop en pips. | `30` |
| `TrailingActivationPips` | Se requiere beneficio antes de los ajustes finales. | `20` |
| `EntryGap` | Distancia mínima entre labios y mandíbula (precio unidades). | `0.0005` |
| `ExitGap` | Separación mínima de los dientes (precio unitario). | `0.0001` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Longitudes SMMA para las líneas Alligator. | `13 / 8 / 5` |
| `JawShift`, `TeethShift`, `LipsShift` | Bar shift applied when evaluating signals. | `8 / 5 / 3` |
| `FractalBars` | Número de velas escaneadas en busca de fractales. | `10` |
| `FractalDistancePips` | Distancia requerida entre precio y fractal. | `30` |
| `MartingaleDepth` | Número de órdenes límite promedio. | `10` |
| `MartingaleMultiplier` | Multiplicador adicional para promediar el volumen. | `1.3` |
| `GridSpreadPips` | Desplazamiento de extensión aplicado a la cuadrícula. | `10` |

## Notas

- El indicador Alligator se procesa en medianas de velas y utiliza retrasos de una barra para evitar trabajar con valores inacabados.
- `EntryGap` y `ExitGap` se expresan en unidades de precio absoluto. Ajústelos para que coincidan con el tamaño de garrapata del instrumento si es necesario.
- La detección de fractales refleja el patrón estándar de cinco barras de Bill Williams. Cuando el filtro está activo, ignora las configuraciones hasta que se recopile suficiente historial.
- La estrategia no crea órdenes de parada protectora ni de toma de ganancias en el intercambio. Todas las salidas son manejadas internamente por la lógica de la estrategia.
- Se admiten cambios manuales a órdenes pendientes o activas; la estrategia limpia sus redes internas cuando se ejecutan o cancelan pedidos.
