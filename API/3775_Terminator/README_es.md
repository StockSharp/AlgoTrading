# Estrategia terminadora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Terminator reproduce la lógica de martingala basada en cuadrículas del asesor experto MetaTrader 4 "Terminator v2.0" utilizando el StockSharp nivel alto API. La estrategia ingresa en la dirección de la pendiente MACD y luego construye una canasta promedio cada vez que el precio se mueve contra la posición en un número configurable de pips. La cesta se gestiona con stop-loss opcional, take-profit, trailing stop y una regla de protección segura de beneficios que puede cerrar la última operación cuando el beneficio flotante alcanza un objetivo.

## Lógica de trading

1. **Generación de señal**: en cada vela terminada, la estrategia evalúa el histograma MACD. Cuando el valor de MACD aumenta respecto al valor anterior se asume un sesgo alcista, mientras que un MACD decreciente indica un sesgo bajista. Una bandera `ReverseSignals` puede invertir la interpretación.
2. **Entrada inicial**: si no hay operaciones abiertas y el filtro de programación (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`) permite operar, la estrategia envía una orden de mercado en la dirección detectada a menos que `ManualTrading` esté habilitado.
3. **Martingale promedio**: cuando hay una cesta abierta, la estrategia espera a que el precio se mueva negativamente en `EntryDistancePips`. Cada entrada adicional duplica el volumen anterior (o lo multiplica por 1,5 si `MaxTrades` es mayor que 12) hasta el límite de `MaxTrades`. El tamaño de la posición también se puede derivar del saldo de la cuenta habilitando `UseMoneyManagement`.
4. **Gestión de riesgos** –
   - **Take-profit**: `TakeProfitPips` define la distancia utilizada para posicionar el nivel de take-profit compartido.
   - **Parada inicial**: `InitialStopPips` opcionalmente configura la parada de protección inicial para la cesta completa.
   - **Stop de seguimiento**: `TrailingStopPips` se activa después de que la canasta gana al menos la distancia de seguimiento más un paso de espaciado y luego mueve el stop en la dirección comercial.
   - **Protección de cuenta**: cuando `UseAccountProtection` está habilitado y el número de operaciones abiertas llega a `MaxTrades - OrdersToProtect`, la ganancia flotante se compara con `SecureProfit` (o el valor actual de la cartera si `ProtectUsingBalance` es verdadero). Si se excede el umbral, la última operación se cierra para asegurar las ganancias y no se permiten nuevas entradas hasta que se reinicie la cesta.
5. **Reinicio de la cesta**: cuando la posición neta vuelve a cero, todos los contadores internos se borran, lo que permite un nuevo ciclo comercial.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Distancia en pips para el nivel de obtención de beneficios de la cesta. |
| `InitialStopPips` | Distancia de parada inicial en pips. Establezca en cero para desactivar. |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establezca en cero para desactivar. |
| `MaxTrades` | Número máximo de entradas de martingala permitidas simultáneamente. |
| `EntryDistancePips` | Movimiento adverso mínimo requerido antes de agregar la siguiente operación. |
| `SecureProfit` | Umbral de beneficio flotante utilizado por el módulo de protección. |
| `UseAccountProtection` | Habilita el bloque de protección de ganancias seguras. |
| `ProtectUsingBalance` | Cuando es verdadero, el umbral de protección es igual al valor actual de la cartera en lugar de `SecureProfit`. |
| `OrdersToProtect` | Número de operaciones finales vigiladas por el bloque de protección (refleja la entrada original de "Órdenes de protección"). |
| `ReverseSignals` | Invierte señales alcistas y bajistas MACD. |
| `ManualTrading` | Desactiva las entradas automáticas manteniendo activa la gestión de la cesta. |
| `LotSize` | Tamaño de lote fijo cuando la administración del dinero está deshabilitada. |
| `UseMoneyManagement` | Habilita el tamaño basado en el equilibrio derivado de `RiskPercent`. |
| `RiskPercent` | Porcentaje de riesgo (por 100%) aplicado cuando la gestión del dinero está activa. |
| `IsStandardAccount` | Alterna entre escalado de lote estándar y mini. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Supuestos de valor de pip utilizados para convertir pips en moneda para la regla de protección. |
| `StartYear`, `StartMonth`, `EndYear`, `EndMonth` | Restrinja la ventana de tiempo en la que se pueden abrir nuevas cestas. |
| `CandleType` | Plazo utilizado para generar la señal MACD. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Configuración del período del indicador MACD. |

## Notas de uso

- La estrategia se suscribe al tipo de vela definido por `CandleType` y solo reacciona a las velas terminadas.
- Para reflejar el comportamiento original de MT4, asegúrese de que los parámetros del valor del pip del símbolo coincidan con las especificaciones de su corredor.
- Cuando `ManualTrading` está habilitado, aún puedes administrar pedidos manualmente; el algoritmo continuará siguiendo los stop y aplicando la protección de la cuenta en la cesta abierta.
- La implementación se centra en el método de entrada basado en MACD del asesor experto original porque los otros modos se basaban en indicadores personalizados que no están disponibles en StockSharp.

## Detalles de conversión

- La gestión del dinero, el espaciado de pips, el escalado de martingala y la lógica de rentabilidad segura siguen la estructura del código MQ4 original.
- Las opciones MT4 `AccountProtection` y `AllSymbolsProtect` se combinan en los parámetros `UseAccountProtection` y `ProtectUsingBalance`.
- `ReverseCondition` y `Manual` banderas del mapa de origen a `ReverseSignals` y `ManualTrading` respectivamente.
- Las reglas de stop-loss y trailing operan en la cesta agregada en lugar de por orden, similar al comportamiento del asesor experto en origen.

## Cómo correr

1. Abra la solución en Visual Studio.
2. Agregue la estrategia a una instancia `StrategyRunner` o `StrategyConnector`.
3. Configure los parámetros en la interfaz de usuario o mediante código.
4. Iniciar la estrategia; se suscribirá automáticamente a la serie de velas especificada y comenzará a evaluar señales.
