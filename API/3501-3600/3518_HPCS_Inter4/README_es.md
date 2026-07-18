# Estrategia HPCS Inter4 (3518)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia transfiere el asesor experto MetaTrader "_HPCS_IntFourth_MT4_EA_V01_WE" al API de alto nivel de StockSharp. El script original abre inmediatamente una posición larga, aplica niveles protectores de stop-loss y take-profit medidos en MetaTrader pips y cierra la operación con fuerza después de un breve período de tenencia. La versión C# reproduce el mismo comportamiento al combinar el administrador de protección integrado con un temporizador de un segundo que monitorea el tiempo transcurrido desde la entrada.

## Lógica de trading

1. **Inicialización**
   - Cuando comienza la estrategia, calcula el tamaño del pip MetaTrader a partir del valor `PriceStep` y la precisión decimal (los símbolos de 5 y 3 dígitos usan un multiplicador de 10x).
   - El asistente de alto nivel `StartProtection` está configurado con las distancias solicitadas de toma de ganancias y límite de pérdidas. La distancia de stop-loss incluye el buffer adicional que el EA original aplica usando `OrderModify`.
   - El volumen es fijo y proviene del parámetro `OrderVolume`.

2. **Entrada**
   - Una orden de compra de mercado único se envía inmediatamente después de que se lanza la estrategia. No se realizan más entradas.
   - Una vez que se informa el primer llenado, la estrategia almacena el tiempo de ejecución.

3. **Salir**
   - Un cronómetro comprueba la posición abierta cada segundo.
   - Cuando el período de tenencia alcanza `CloseDelaySeconds`, la estrategia cierra la posición larga con una orden de venta de mercado si la exposición sigue siendo positiva.
   - El administrador de protección mantiene automáticamente las órdenes protectoras de stop-loss y take-profit mediante salidas de mercado.

La lógica solo opera en la dirección larga, reflejando el comportamiento del script MetaTrader.

## Parámetros

| Nombre | Descripción | Predeterminado | Optimizable |
| --- | --- | --- | --- |
| `OrderVolume` | Volumen fijo utilizado al enviar la orden de compra inicial del mercado. | `1` | No |
| `StopLossPips` | Distancia base de MetaTrader pips aplicada al stop-loss inicial. | `10` | No |
| `ExtraStopPips` | Búfer de MetaTrader pips adicional restado de la parada después de la entrada. | `10` | No |
| `TakeProfitPips` | MetaTrader distancia del pip del objetivo de ganancias. | `10` | No |
| `CloseDelaySeconds` | Tiempo en segundos antes de que la posición se cierre con fuerza. `0` desactiva la salida del temporizador. | `30` | No |

## Notas de implementación

- El asistente de tamaño de pip multiplica el `PriceStep` informado por 10 para instrumentos de 3 y 5 decimales para que los valores de los parámetros mantengan la misma escala que en MetaTrader.
- `StartProtection` utiliza `UnitTypes.Price` distancias para que las órdenes de protección operen con salidas de mercado, exactamente como lo hizo EA con `OrderClose`.
- `OnNewMyTrade` registra la primera operación de compra completada para iniciar la cuenta regresiva del período de tenencia y restablece el estado cuando la posición está completamente cerrada.
- El cronómetro se ejecuta a intervalos de un segundo para replicar la verificación de tiempo original `OnTick` sin ser sensible a la inactividad del mercado.
- Todos los comentarios del código están escritos en inglés para cumplir con las pautas del repositorio.
