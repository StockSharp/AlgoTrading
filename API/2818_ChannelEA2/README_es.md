# Estrategia ChannelEA2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia ChannelEA2 replica el experto MetaTrader "ChannelEA2" en StockSharp. La estrategia construye un canal de precios intradía entre las horas de inicio y fin de sesión configuradas. Cuando la sesión termina, coloca órdenes stop por encima del máximo del canal y por debajo del mínimo del canal. Cada orden stop lleva un stop loss protector definido por el borde opuesto del canal. El enfoque tiene como objetivo capturar rupturas después de un período de consolidación durante la ventana de sesión.

## Lógica de trading
- En la primera vela finalizada cuyo tiempo de apertura cruza el `BeginHour`, la estrategia reinicia la sesión.
  - Todas las posiciones abiertas se cierran con órdenes de mercado.
  - Cualquier orden activa, incluidas las anteriores entradas stop o stops de protección, se cancela.
  - Los máximos y mínimos de la sesión se inicializan usando la primera vela dentro de la nueva sesión.
- Durante la sesión (desde `BeginHour` hasta `EndHour`), el máximo y mínimo de cada vela finalizada actualiza los límites del canal.
- En la primera vela que se abre después de que la sesión ha terminado (`EndHour`), la estrategia calcula:
  - Una orden de compra stop en el máximo de sesión registrado más un buffer opcional medido en pasos de precio.
  - Una orden de venta stop en el mínimo de sesión registrado menos el mismo buffer.
  - El stop loss para la orden de compra es el mínimo de sesión, mientras que el stop loss para la orden de venta es el máximo de sesión.
- Si se abre una posición, se cancela la orden de entrada opuesta y se registra un stop protector en el mercado usando el nivel de stop almacenado.
- Las órdenes permanecen activas hasta el inicio de la siguiente sesión, cuando todo se reinicia de nuevo.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `BeginHour` | Hora (0-23) cuando la sesión se reinicia y el canal comienza a recopilar datos. | `1` |
| `EndHour` | Hora (0-23) cuando las órdenes stop están programadas. Admite sesiones nocturnas cuando `BeginHour > EndHour`. | `10` |
| `TradeVolume` | Volumen usado para cada orden de entrada. | `1` |
| `CandleType` | Serie de velas usada para construir el canal (predeterminado velas de 1 hora). | `1 hora` |
| `StopBufferMultiplier` | Multiplicador del paso de precio del instrumento usado como buffer de seguridad para la activación de entrada y stops de protección. | `2` |

## Gestión de riesgo
- La estrategia llama automáticamente a `StartProtection()` para que StockSharp gestione posiciones inesperadas.
- Las órdenes stop de protección se envían inmediatamente después de que aparece una posición. Se cancelan cuando la posición vuelve a cero.
- Los precios de stop se desplazan por `StopBufferMultiplier * PriceStep` para evitar violar los límites de distancia de stop de la bolsa.

## Notas adicionales
- El rango del canal se congela una vez que se generan las órdenes stop; las velas posteriores no afectan los niveles de entrada hasta que comienza la siguiente sesión.
- Si el instrumento no tiene `PriceStep` definido, el buffer se ignora y las órdenes se colocan en los niveles exactos del canal.
- Los valores de volumen son decimales, permitiendo contratos o lotes fraccionarios cuando el broker lo soporta.
- La estrategia dibuja velas y operaciones ejecutadas en el área del gráfico para seguimiento visual.
