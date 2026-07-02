# Estrategia de Udy Ivan Madumere
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El asesor experto Udy Ivan Madumere abre una posición de mercado única una vez al día cuando aparece una vela horaria específica. El puerto StockSharp mantiene este comportamiento intacto observando la serie de velas configuradas, comparando precios de apertura históricos y reaccionando inmediatamente después de que se cierra la barra objetivo. Todas las decisiones de ejecución, gestión de posiciones y manejo de volúmenes se reproducen para que la estrategia se comporte como la MetaTrader 4 original dentro del entorno StockSharp.

Características clave:

- Evalúa una vela terminada por día en `TradeHour` y nunca envía más de una posición simultánea.
- Mide la diferencia entre los precios de apertura `Open[FirstLookback]` y `Open[SecondLookback]` para decidir si ir en corto o en largo.
- Refleja la escalera de equilibrio MetaTrader para ajustar el tamaño del lote base automáticamente cuando `UseAutoVolume` está habilitado.
- Aplica distancias de stop-loss y take-profit asimétricas (separadas para largas y cortas) y un trailing stop que sólo afecta a las posiciones cortas.
- Obliga a que todas las operaciones se cierren después de un número configurable de horas, incluso si no se alcanzaron los niveles de protección.

## Flujo de trabajo comercial
1. Suscríbase al tipo de vela seleccionado (`CandleType`) y espere a que las barras estén completamente terminadas para evitar señales prematuras.
2. Realice un seguimiento del historial de precios de apertura para que las diferencias `Open[FirstLookback] - Open[SecondLookback]` (configuración corta) y `Open[SecondLookback] - Open[FirstLookback]` (configuración larga) se puedan evaluar exactamente como en MetaTrader.
3. Cuando se abre la vela más reciente a las `TradeHour`:
   - Si la diferencia bajista es mayor que `ShortDeltaPoints * PriceStep`, envíe una orden de venta de mercado.
   - De lo contrario, si la diferencia alcista supera `LongDeltaPoints * PriceStep`, envíe una orden de compra de mercado.
4. Sólo se permite un pedido por día. El indicador `canTrade` se restablece después de que haya pasado la hora configurada para permitir otro intento en la siguiente sesión.
5. Al ingresar la orden, la estrategia vuelve a calcular el lote base:
   - `UseAutoVolume = true` activa la escalera heredada que aumenta el tamaño del lote cuando el saldo de la cuenta cruza umbrales predefinidos.
   - Si el saldo actual está por debajo de la instantánea de la operación anterior, el resultado se multiplica por `BigLotMultiplier`, coincidiendo con el comportamiento de recuperación del "lote grande" del EA.
6. Mientras la posición está abierta, se ejecuta la siguiente lógica de salida en cada vela completa:
   - La toma de ganancias y el límite de pérdidas se evalúan con respecto al precio de entrada registrado.
   - Las operaciones cortas también siguen el stop una vez que el mejor precio ha mejorado al menos `TrailingStopPoints`.
   - La posición se cierra con fuerza una vez que ha estado activa durante `MaxHoldingHours`.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H1` | Serie de velas procesadas por la estrategia. |
| `TradeHour` | `int` | `18` | Hora del día (0-23) en la que se evalúa la señal diaria. |
| `FirstLookback` | `int` | `6` | Número de velas completadas referenciadas como `Open[FirstLookback]`. |
| `SecondLookback` | `int` | `2` | Número de velas completadas referenciadas como `Open[SecondLookback]`. |
| `LongDeltaPoints` | `decimal` | `6` | Se requiere una diferencia mínima de precio de apertura alcista (en MetaTrader puntos) para entrar en posición larga. |
| `ShortDeltaPoints` | `decimal` | `21` | Se requiere una diferencia mínima de precio de apertura bajista (en MetaTrader puntos) para entrar en corto. |
| `TakeProfitLongPoints` | `decimal` | `39` | Distancia de toma de ganancias, expresada en puntos, para posiciones largas. |
| `StopLossLongPoints` | `decimal` | `147` | Distancia de stop-loss, en puntos, para posiciones largas. |
| `TakeProfitShortPoints` | `decimal` | `200` | Distancia de toma de ganancias, en puntos, para posiciones cortas. |
| `StopLossShortPoints` | `decimal` | `267` | Distancia de stop-loss, en puntos, para posiciones cortas. |
| `TrailingStopPoints` | `decimal` | `30` | La distancia del trailing-stop (puntos) se aplica solo a posiciones cortas. |
| `BaseVolume` | `decimal` | `0.01` | Tamaño del lote inicial antes de los ajustes de administración del dinero. |
| `UseAutoVolume` | `bool` | `true` | Habilite la escalera de equilibrio MetaTrader que anula `BaseVolume`. |
| `BigLotMultiplier` | `decimal` | `1` | Se aplicó un multiplicador adicional cuando el saldo cayó desde la operación anterior. |
| `MaxHoldingHours` | `int` | `504` | Tiempo máximo de retención en horas. Zero desactiva el temporizador. |

## Notas de implementación
- Los umbrales de precios se convierten de MetaTrader "puntos" en distancias de precios reales utilizando los `PriceStep` del instrumento.
- El buffer de precios de apertura se recorta a `max(FirstLookback, SecondLookback) + 1` entradas, evitando asignaciones innecesarias y manteniendo el historial requerido.
- El trailing stop para operaciones cortas almacena el mínimo mejor logrado y actualiza el nivel de protección sólo cuando el nuevo candidato está más cerca del precio actual.
- Las instantáneas del saldo de la cuenta se basan en `Portfolio.CurrentValue` (recurriendo a `BeginValue`) para que los entornos de demostración, en vivo y de prueba retrospectiva se comporten de manera consistente.
- Cada comentario dentro del código está escrito en inglés según lo solicitado, lo que hace que la lógica sea fácil de auditar o ampliar.

## Consejos de uso
- Haga coincidir `CandleType` con el período de tiempo utilizado por el histórico EA (la plantilla original espera velas de una hora).
- Cuando ejecute símbolos que utilicen microlotes, ajuste `BaseVolume` y los valores de la escalera de lotes automáticos a las especificaciones del contrato del lugar.
- Combine la estrategia con gráficos StockSharp a través de los asistentes integrados (`DrawCandles`, `DrawOwnTrades`) para verificar que los pedidos aparezcan solo una vez al día a la hora configurada.
