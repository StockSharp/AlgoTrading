# Hans123Trader RangeEstrategia de ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
## Descripción general
La **Estrategia Hans123Trader** recrea el MetaTrader asesor experto "Hans123Trader v1" utilizando la API de alto nivel de StockSharp. El sistema arma órdenes de parada dos veces al día en función de la ruptura del rango de negociación de 5 minutos más reciente. Está diseñado para símbolos de estilo Forex donde los pasos de precio corresponden a pips fraccionarios. Las órdenes pendientes se actualizan cada día de negociación y cualquier posición abierta se cierra forzosamente cuando el calendario avanza.

## Flujo de trabajo principal
1. **Seguimiento de rango**: se mantiene una ventana móvil de 80 barras de velas de 5 minutos a través de los indicadores `Highest` y `Lowest`. Los máximos y mínimos más recientes definen los niveles de ruptura.
2. **Programación de sesiones**: `EndSession1` y `EndSession2` controlan dos ventanas de negociación independientes. Cuando el reloj llega a la hora configurada (minuto `00`), la estrategia calcula nuevas órdenes stop pendientes.
3. **Colocación de órdenes**: se envía una parada de compra `5` puntos por encima del máximo detectado y una parada de venta `5` puntos por debajo del mínimo detectado. Los pedidos se eliminan en el momento en que comienza un nuevo día para imitar el vencimiento de MetaTrader a las 23:59.
4. **Gestión de posición**: después de la entrada, la estrategia aplica el stop-loss inicial solicitado, el take-profit opcional y el trailing stop. Los niveles de protección se expresan en puntos y se convierten en precio utilizando el `PriceStep` del instrumento.
5. **Higiene diaria** – si una posición permanece abierta cuando comienza un nuevo día de negociación, se cierra en el mercado. Todos los pedidos pendientes del día anterior se cancelan antes de que se preparen otros nuevos.

## Reglas de trading
- **Señales de entrada**
  - Dos intentos de fuga por día: uno a las `EndSession1`, otro a las `EndSession2` (las horas son la hora del agente/servidor).
  - Precio de parada de compra = `HighestHigh + 5 points`. Precio de parada de venta = `LowestLow − 5 points`.
  - Ambos pedidos utilizan el parámetro `Volume` actual (predeterminado `1`).
  - Los pedidos se omiten si el volumen no es positivo.
- **Lógica de salida**
  - Stop-loss inicial = precio de entrada ± `InitialStopLoss` puntos (abajo para largos, arriba para cortos).
  - Take-profit = precio de entrada ± `TakeProfit` puntos (arriba para largos, abajo para cortos).
  - El trailing stop refuerza el nivel de protección cada vez que el cierre avanza hacia las ganancias en al menos `TrailingStop` puntos.
  - Cualquier posición que sobreviva hasta el día siguiente se cierra inmediatamente en el mercado.
- **Mantenimiento de pedidos**
  - Las órdenes stop pendientes se cancelan al comienzo de cada día calendario.
  - Una vez que se activa (o cancela/falla) una orden de suspensión, las referencias internas se borran automáticamente.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `BeginSession1` / `BeginSession2` | Conservado para compatibilidad con la interfaz de usuario (sugerencias sobre la hora de inicio). La implementación actual se basa en los activadores de fin de hora. |
| `EndSession1` / `EndSession2` | Horas (0–23) en las que se activan nuevas órdenes de detención; los minutos deben ser exactamente cero. |
| `TrailingStop` | Distancia de seguimiento en puntos. `0` desactiva el seguimiento. |
| `TakeProfit` | Distancia de toma de ganancias en puntos. `0` desactiva la obtención de beneficios. |
| `InitialStopLoss` | Distancia inicial de stop-loss en puntos. `0` deja la operación sin una parada de protección a menos que se active el seguimiento. |
| `CandleType` | Serie de velas utilizada para el rango de 80 barras (por defecto `TimeSpan.FromMinutes(5)`). |
| `Volume` | Volumen base de estrategia heredado de `Strategy`. |

## Notas de conversión
- Las funciones auxiliares MetaTrader `OrderSendExtended` y el bloqueo de variable global no son necesarios; StockSharp gestiona la simultaneidad internamente.
- Los números mágicos se reemplazan por referencias de orden explícitas (campos `_session*`). Los eventos del ciclo de vida del pedido borran estas referencias cuando finaliza el pedido.
- Los pedidos pendientes que vencen a las 23:59 se emulan cancelándolos cuando comienza un nuevo día.
- La lógica de trailing stop utiliza precios de cierre de velas como sustituto de las cotizaciones de oferta/demanda MetaTrader.
- Todas las distancias basadas en puntos se multiplican por `Security.PriceStep`. Cuando no se establece `PriceStep`, los valores de puntos brutos se tratan como distancias de precios absolutas.

## Consejos de uso
- Asigne instrumentos con `PriceStep`, `StepPrice` y `VolumeStep` configurados correctamente para que la conversión de punto a precio y el redondeo de volumen sean precisos.
- Verifique que los datos históricos de 5 minutos estén disponibles; los niveles de ruptura dependen de las 80 velas más recientes.
- Ajuste `EndSession1`/`EndSession2` para que coincida con las sesiones de mercado deseadas (por ejemplo, pausas previas a Londres y Nueva York).
- Utilice Designer o Runner para optimizar `InitialStopLoss`, `TakeProfit` y `TrailingStop` para el instrumento elegido antes de la implementación en vivo.
- Combine la estrategia con StockSharp controles de riesgo si varias estrategias comparten la misma cartera.
