# Cuadrícula EA Estrategia profesional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia profesional Grid EA** reproduce el comportamiento principal del asesor experto MetaTrader 4 original. La estrategia combina escalamiento basado en cuadrícula con RSI o entradas de ruptura cronometradas y funciones de gestión de riesgos virtuales, como punto de equilibrio y paradas finales. Está diseñado para carteras netas, lo que significa que siempre funciona con una única posición neta y borra automáticamente la dirección opuesta cuando se abre una nueva operación.

## Lógica de trading
- **Modo de entrada**: elija entre umbrales RSI, desgloses controlados por tiempo o operación totalmente manual. En modo manual, la estrategia solo gestiona las posiciones existentes y el escalado de la cuadrícula.
- **Filtro direccional**: restringe el comercio a direcciones largas, cortas o en ambas direcciones.
- **Escalado de cuadrícula**: después de la entrada inicial, la estrategia puede agregar posiciones cuando el precio retrocede en un número configurable de puntos. Tanto el paso como el volumen del pedido pueden crecer geométricamente.
- **Controles de riesgo**: los filtros virtuales de stop-loss, take-profit, breakeven, trailing stop y sesión reflejan el comportamiento original del asesor experto.
- **Salidas de superposición**: los parámetros se proporcionan para completar, pero debido al modelo de posición neta, ambas direcciones no se pueden mantener simultáneamente. Por lo tanto, la lógica de superposición permanece inactiva y los niveles se documentan para compatibilidad futura.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Mode` | Dirección comercial permitida (Compra, Venta, Ambos). |
| `EntryMode` | Fuente de señal (RSI, Puntos Fijos, Manual). |
| `RsiPeriod`, `RsiUpper`, `RsiLower` | Configuración RSI utilizada en modo RSI. |
| `CandleType` | Suscripción de velas para señales y gestión de riesgos. |
| `Distance`, `TimerSeconds` | Distancia de ruptura e intervalo de actualización para entradas de punto fijo. |
| `InitialVolume`, `FromBalance`, `Risk %` | Bloque de gestión de dinero. Si `Risk %` > 0, el tamaño de la posición se deriva del capital de la cuenta y la distancia del límite de pérdidas; de lo contrario, se utiliza un lote fijo o basado en el saldo. |
| `LotMultiplier`, `MaxLot` | Multiplicador y límite para adiciones a la cuadrícula. |
| `Step`, `StepMultiplier`, `MaxStep` | Configuración de espaciado de cuadrícula en puntos. |
| `OverlapOrders`, `OverlapPips` | Reservado para lógica de superposición cubierta (deshabilitada en esta implementación). |
| `Stop Loss`, `Take Profit` | Niveles de protección iniciales en puntos (`-1` desactiva). |
| `Break Even Stop`, `Break Even Step` | Mueva el stop al punto de equilibrio después de que el precio se mueva en el paso definido. |
| `Trailing Stop`, `Trailing Step` | Configuración de trailing stop. |
| `Start Time`, `End Time` | Ventana de sesión de negociación en hora de la plataforma local (HH:mm). |

## Trazar
Cuando el área del gráfico está disponible, la estrategia traza las velas de precios, la línea RSI y todas las operaciones propias, coincidiendo con el diseño del asesor experto fuente.

## Notas
- La estrategia cancela automáticamente los niveles de ruptura pendientes una vez que se llenan o cuando la dirección está desactivada.
- Debido a que StockSharp utiliza posiciones netas, solo se puede abrir un lado del mercado a la vez. Abrir una posición larga borra los cortos existentes y viceversa.
- Asegúrese de que las propiedades del instrumento (`PriceStep`, `StepPrice`) estén configuradas de modo que los parámetros basados en puntos coincidan con la configuración original de MT4.
