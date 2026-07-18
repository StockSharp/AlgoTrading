# Estrategia de ruptura del rango de reserva anticipada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Early Bird Range Break** es una adaptación de C# del asesor experto MetaTrader "earlyBird3". Su objetivo son las rupturas de rango que se producen poco después de la apertura de la sesión de negociación europea. El algoritmo observa un rango de consolidación temprano en la mañana, filtra posibles rupturas con un RSI de 14 períodos e ingresa hasta tres órdenes de mercado en la dirección de la ruptura. Cada orden utiliza niveles de toma de ganancias predefinidos, un límite de pérdidas compartido y un mecanismo de seguimiento opcional que se habilita solo cuando la volatilidad se expande más allá de su promedio reciente.

## Requisitos de datos
- Un flujo de velas de marco temporal único (predeterminado: velas de 5 minutos) para el instrumento negociado.
- El instrumento debe proporcionar un `PriceStep` válido porque todas las distancias de stop-loss y take-profit están definidas en puntos.
- Los tiempos de negociación se evalúan utilizando las marcas de tiempo de las velas entrantes (hora del servidor de la fuente de datos).

## Sesión de negociación
1. **Construcción de rango**: entre `RangeStartHour` y `RangeEndHour` la estrategia registra el máximo más alto y el mínimo más bajo.
2. **Ventana de negociación**: después de `TradingStartHour:TradingStartMinute` y antes de `TradingEndHour`, la lógica de ruptura se activa.
3. **Cierre forzado**: en `ClosingHour` todas las posiciones restantes se liquidan independientemente de las ganancias o pérdidas.
4. **Solo entre semana** – Las señales se procesan de lunes a viernes.

## Lógica de entrada
1. Un nivel de ruptura largo se establece en `range high + EntryBufferPoints`, mientras que un nivel de ruptura corto se establece en `range low - EntryBufferPoints`. El colchón se expresa en puntos de precio.
2. El filtro RSI debe ser mayor que 50 para una configuración larga y menor o igual a 50 para una configuración corta.
3. Sólo se permite una ruptura por dirección cada día de negociación. Cuando se activa, se envían inmediatamente tres órdenes de mercado (volumen predeterminado `0.1`).
4. Si ya hay una posición opuesta abierta y `HedgeTrading` está deshabilitado, la nueva señal se ignora. Cuando `HedgeTrading` está habilitado, la estrategia primero cierra la posición existente y luego ingresa en la nueva dirección. Esto refleja la intención del EA original, pero utiliza la inversión de posición porque las cuentas StockSharp se compensan.

## Gestión de salidas
1. **Stop-loss**: se aplica un stop-loss compartido (`StopLossPoints`) a la posición agregada. Si el precio cruza el nivel, el tamaño restante se cierra inmediatamente.
2. **Escalera de obtención de beneficios**: tres objetivos (`TakeProfit1Points`, `TakeProfit2Points`, `TakeProfit3Points`) cierran una porción de posición cada uno. La parte restante permanece abierta hasta que se detiene, se rastrea o se cierra al final de la sesión.
3. **Trailing stop**: cuando solo queda una porción, el rango de vela actual debe exceder `ATR * TrailingRiskMultiplier`. Si el precio ha avanzado al menos `TrailingStopPoints`, el stop-loss se mueve en la dirección comercial manteniendo la distancia del stop inicial.
4. **Cierre de sesión**: cualquier exposición abierta se aplana por completo una vez que la hora actual llega a `ClosingHour`.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `AutoTrading` | Activa/desactiva la ejecución de órdenes. | `true` |
| `HedgeTrading` | Permite invertir la posición en señales opuestas (implementado como plano y reverso). | `true` |
| `OrderType` | `0` – ambas direcciones, `1` – solo largo, `2` – solo corto. | `0` |
| `TradeVolume` | Volumen por orden de mercado enviada. | `0.1` |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. | `60` |
| `TakeProfit1Points` | Distancia de toma de ganancias para la primera porción. | `10` |
| `TakeProfit2Points` | Distancia de toma de ganancias para la segunda parte. | `20` |
| `TakeProfit3Points` | Distancia de toma de ganancias para la tercera parte. | `30` |
| `TrailingStopPoints` | Movimiento mínimo favorable antes de que se active el trailing stop. | `15` |
| `TrailingRiskMultiplier` | Multiplicador aplicado a ATR al validar la expansión de la volatilidad. | `1.0` |
| `EntryBufferPoints` | Distancia adicional agregada a los niveles de ruptura. | `2` |
| `RangeStartHour` | Hora en que comienza el rango de referencia. | `3` |
| `RangeEndHour` | Hora en la que finaliza el rango de referencia. | `7` |
| `TradingStartHour` | Hora en la que se permiten entradas de grupos. | `7` |
| `TradingStartMinute` | Minuto en el que se permiten entradas de grupos. | `15` |
| `TradingEndHour` | Hora tras la cual no se realizan nuevas entradas. | `15` |
| `ClosingHour` | Hora en la que se cierran todas las operaciones. | `17` |
| `RsiPeriod` | RSI retrospectiva utilizada para el filtrado. | `14` |
| `VolatilityPeriod` | ATR mirada retrospectiva para la puerta de volatilidad. | `16` |
| `CandleType` | Serie de velas utilizadas para el análisis (5 minutos predeterminado). | `TimeSpan.FromMinutes(5)` |

## Notas de implementación
- La estrategia se suscribe a velas a través dla API de alto nivel de StockSharp y vincula los indicadores RSI y ATR directamente a la suscripción.
- Los valores del indicador se consumen dentro de la devolución de llamada `ProcessCandle` sin llamar a `GetValue` ni almacenar buffers personalizados, siguiendo las pautas del proyecto.
- Sólo se procesan velas terminadas; las actualizaciones parciales se ignoran.
- Todas las distancias de precios se convierten de puntos a precios absolutos utilizando el instrumento `PriceStep`. Asegúrese de que la definición de seguridad exponga el tamaño de marca correcto.
- El asesor experto original mantuvo MQL órdenes separadas para cobertura. StockSharp utiliza posiciones netas, por lo que este puerto realiza una operación de cierre e inversión cuando `HedgeTrading` está habilitado.

## Consejos de uso
- Alinee el período de tiempo de la vela con el centro de negociación utilizado en el EA original (M5 a H1 en MetaTrader). Ajuste `RangeStartHour`, `RangeEndHour` y la ventana de negociación para reflejar el cronograma del mercado local de su fuente de datos.
- Al optimizar, concéntrese en el amortiguador de ruptura, la escalera de toma de ganancias y el filtro de volatilidad porque definen el equilibrio entre rupturas falsas y movimientos perdidos.
- El seguimiento es intencionalmente conservador: si necesita salidas más estrictas, considere reducir `TrailingRiskMultiplier` o `StopLossPoints` para que los ajustes de seguimiento se produzcan con más frecuencia.
