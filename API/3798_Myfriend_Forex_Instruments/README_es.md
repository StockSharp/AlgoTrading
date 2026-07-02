# Estrategia de instrumentos Forex de Myfriend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Myfriend Forex Instruments** reproduce el experto "MyFriend" MetaTrader de 2006. Opera con EUR/USD en velas de 30 minutos combinando niveles de pivote diarios, expansiones de canal Donchian y un diferencial de impulso a corto y largo plazo medido a partir de los precios de cierre. El sistema busca velas que atraviesen el pivote diario con un cuerpo real ancho o expansiones abruptas de ancho Donchian. Cuando uno de estos impulsos se alinea con el sesgo de impulso intradiario, la estrategia abre una posición única con niveles de protección predefinidos.

## Lógica comercial

1. **Mapa de pivote diario**: los máximos, mínimos y cierres del día anterior construyen la escalera de pivote clásica (`Pivot`, `R1`, `S1`). Estos niveles permanecen sin cambios durante toda la sesión de negociación y definen el rango de negociación esperado.
2. **Pulso de impulso** – Dos medias móviles simples sobre el precio de cierre (3 y 9 períodos) forman un diferencial de impulso corto/largo. El diferencial se multiplica por 1000 para imitar el cálculo "MP" de MetaTrader y determina si domina la presión alcista o bajista.
3. **Filtros de ruptura**
   - *Empuje de pivote*: después de que una vela se cierra a través del pivote con un cuerpo de más de 12 puntos y la siguiente vela se cierra en la misma dirección, la estrategia señala una operación potencial.
   - *Donchian expansión*: cuando el canal Donchian de 16 períodos se amplía más allá del rango `R1 - S1` y su dirección concuerda con la acción del precio, la señal también se activa.
4. **Gestión de pedidos** – Solo se permite una posición a la vez. Las entradas largas utilizan el mínimo de la vela anterior menos un amortiguador como parada y una toma de ganancias fija de 70 puntos. Las entradas cortas reflejan esta lógica con el máximo anterior más un buffer.
5. **Tácticas de salida**
   - *Salida basada en el tiempo*: entre la 3.ª y 4.ª vela después de la entrada, si la última barra cerrada se mueve 3 puntos frente a la posición, la operación se cierra anticipadamente.
   - *Trailing stop*: una vez que la ganancia abierta supera los 5 puntos y el límite Donchian continúa moviéndose a favor de la operación, el stop se arrastra a lo largo del canal más/menos un buffer de 1 punto.
   - *Objetivos difíciles*: el precio que toca el stop calculado o la toma de ganancias cierra inmediatamente la posición.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `BaseVolume` | Volumen de orden utilizado para cada nueva operación. | `1` |
| `TakeProfitPoints` | Distancia de la toma de ganancias desde la entrada en MetaTrader puntos. | `70` |
| `StopLossBufferPoints` | Se agregó un amortiguador adicional más allá del extremo de la vela anterior para la parada protectora. | `13` |
| `ChannelPeriod` | Donchian período de canal utilizado para pruebas de expansión de ancho y seguimiento. | `16` |
| `UseTrailingStop` | Habilita o deshabilita el trailing stop basado en Donchian. | `true` |
| `TrailingStartPoints` | Se requiere ganancia abierta (puntos) antes de que el trailing stop pueda ajustarse. | `5` |
| `TrailingBufferPoints` | Zona de influencia (puntos) aplicada al límite Donchian al final. | `1` |
| `UseTimeClose` | Habilita la salida de rechazo de 3 a 4 velas. | `true` |
| `CandleType` | Tipo de vela principal (período de tiempo predeterminado de 30 minutos). | `M30` |
| `DailyCandleType` | Tipo de vela diaria utilizada para reconstruir los niveles de pivote. | `D1` |

## Notas

- La estrategia está diseñada para EUR/USD y velas de 30 minutos, reflejando al experto original. Diferentes instrumentos o marcos temporales pueden requerir ajustes de parámetros.
- Los parámetros basados en puntos dependen del `PriceStep` del instrumento. Si los datos del mercado no lo proporcionan, la estrategia vuelve a recurrir a un incremento del precio unitario.
- Solo se procesan velas completadas, que coinciden con el comportamiento MetaTrader del algoritmo fuente.
