# Estrategia de cuadrícula DLM v1.4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación StockSharp del asesor experto MetaTrader 4 de Alejandro Galindo "DLM v1.4". El robot original combina un filtro de señal Fisher Transform con un esquema de promedio estilo martingala que construye progresivamente una cuadrícula de posiciones cada vez que el precio se mueve con respecto a la última entrada. La versión StockSharp mantiene las mismas ideas de administración de dinero al tiempo que adapta la lógica de ejecución y protección al API de alto nivel (suscripciones de velas, vinculaciones de indicadores y ayudas de mercado/límite).

## Lógica comercial
- Analice las velas terminadas del período configurado y calcule dos indicadores: la transformada de Fisher y un suavizado SMA de los valores de Fisher.
- Determine la dirección de la canasta a partir de la posición relativa de las dos líneas. Cuando Fisher supera el más suave, la estrategia se prepara para comprar; cuando cae por debajo del más suave, se prepara para vender. El indicador `ReverseSignals` invierte esta interpretación.
- Abra la primera posición inmediatamente (orden de mercado) una vez que haya una dirección disponible y el comercio automático esté habilitado (`ManualTrading = false`).
- Mientras la cesta esté activa, siga agregando nuevas entradas cada vez que el precio se mueva `GridDistancePips` con respecto a la ejecución más reciente. Dependiendo de la bandera `UseLimitOrders`, las operaciones adicionales se envían como órdenes de mercado (en el siguiente cierre de vela) o como órdenes de límite en reposo colocadas exactamente a un paso de la cuadrícula del último llenado.
- El volumen de cada nueva operación sigue el crecimiento de la martingala original: multiplique el tamaño del lote base por 1,5 cuando sea `MaxTrades > 12`; de lo contrario, duplique el tamaño. El tamaño base en sí puede ser fijo (`LotSize`) o derivarse del capital de la cuenta cuando `UseMoneyManagement` está habilitado.
- Cada llenado actualiza los niveles agregados de stop-loss y take-profit para que toda la canasta comparta un único conjunto de niveles de protección. La lógica del trailing-stop puede reforzar el stop después de que el precio se mueve `GridDistancePips + TrailingStopPips` en la dirección rentable.

## Protección de cuenta
- **Guardia segura de ganancias** (`SecureProfitProtection`): una vez que el número de entradas abiertas alcanza `OrdersToProtect`, las ganancias no realizadas (en la moneda de la cuenta) se comparan con `SecureProfit`. Si se alcanza el umbral, toda la cesta se cierra inmediatamente.
- **Protección del capital** (`EquityProtection` + `EquityProtectionPercent`): monitorea el valor actual de la cartera y cierra la canasta cada vez que el capital cae por debajo del porcentaje seleccionado del capital capturado al inicio de la estrategia.
- **Protección contra retiro de dinero** (`AccountMoneyProtection` + `AccountMoneyProtectionValue`): deja de operar cuando el retiro de moneda del capital inicial excede el monto configurado.
- **Protección de por vida** (`OrdersLifeSeconds`): aplica una duración máxima para la entrada más reciente; cuando se excede el límite, todas las operaciones se cierran y se detiene el ciclo de martingala.
- **Filtro de viernes** (`TradeOnFriday`): evita que se inicien nuevas cestas los viernes si está deshabilitado.

Todas las salidas protectoras utilizan órdenes de mercado para garantizar la ejecución. Las órdenes límite pendientes se cancelan cada vez que se activa un bloqueo de protección o cuando se restablece la red.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Distancia de toma de ganancias compartida (pips) aplicada a cada entrada. |
| `StopLossPips` | Distancia inicial de stop-loss (pips) para cada nueva operación. |
| `TrailingStopPips` | Distancia de trailing stop que se activa después del umbral de activación. |
| `MaxTrades` | Número máximo de pasos promediados permitidos en la cesta. |
| `GridDistancePips` | Movimiento adverso mínimo (pips) antes de agregar la siguiente orden. |
| `LotSize` | Tamaño del lote base cuando la administración del dinero está deshabilitada. |
| `UseMoneyManagement` | Permite el dimensionamiento basado en el equilibrio a través de la fórmula de riesgo original. |
| `RiskPercent` | Porcentaje de riesgo utilizado para derivar el tamaño del lote base dinámico. |
| `AccountType` | Escalado aplicado al tamaño del lote dinámico (0 estándar, 1 mini, 2 micro). |
| `SecureProfitProtection` | Habilita la guardia de ganancias flotantes. |
| `SecureProfit` | Beneficio no realizado (unidades monetarias) necesarias para activar la guardia. |
| `OrdersToProtect` | Número mínimo de entradas abiertas antes de que se active la ganancia segura. |
| `EquityProtection` | Habilita la red de seguridad de porcentaje de capital. |
| `EquityProtectionPercent` | Umbral de porcentaje de capital relativo al inicio de la estrategia. |
| `AccountMoneyProtection` | Habilita la protección basada en retiros (moneda). |
| `AccountMoneyProtectionValue` | Retiro máximo tolerado en la moneda de la cuenta. |
| `TradeOnFriday` | Permite/no permite abrir cestas nuevas los viernes. |
| `OrdersLifeSeconds` | Vida útil máxima (segundos) de la última orden antes de la liquidación. |
| `ReverseSignals` | Invierte la dirección de la Transformada de Fisher. |
| `UseLimitOrders` | Cambie entre entradas de mercado y límite para operaciones promedio. |
| `ManualTrading` | Deshabilita las entradas automáticas cuando se establece en verdadero. |
| `CandleType` | Plazo utilizado para los cálculos del indicador. |
| `FisherLength` | Longitud retrospectiva de la transformada de Fisher. |
| `SignalSmoothing` | SMA período aplicado para suavizar los valores de Fisher. |
| `DefaultPipValue` | Valor del pip de reserva utilizado para convertir las pérdidas y ganancias no realizadas en moneda. |

## Notas
- Todos los comentarios en el código fuente están en inglés como lo exigen las pautas del repositorio.
- La estrategia se basa exclusivamente en la API de alto nivel de StockSharp (`SubscribeCandles`, `Bind`, `BuyLimit`, `SellLimit`, etc.) y no manipula los buffers del indicador directamente.
- Los cálculos de administración de dinero reutilizan la fórmula de riesgo original, pero los ajustes de volumen y precio se pasan a través de `Security.ShrinkVolume` y `Security.ShrinkPrice` para respetar la especificación del contrato del instrumento.
- La conversión mantiene el comportamiento del MetaTrader EA lo más parecido posible y al mismo tiempo tiene en cuenta las diferencias de StockSharp (por ejemplo, las salidas de la cesta utilizan órdenes de mercado en lugar de modificar las órdenes existentes).
