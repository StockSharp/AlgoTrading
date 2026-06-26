# Estrategia de Daily Range
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión de StockSharp del asesor experto de MetaTrader 5 `MQL/23334/Daily range.mq5`. El EA original rastrea los precios más altos y más bajos alcanzados durante los últimos días, desplaza estos niveles por un porcentaje configurable del rango diario, y opera rupturas. El port en C# preserva el comportamiento adoptando la API de estrategia de alto nivel de StockSharp.

## Lógica de la estrategia
### Cálculo del rango
* La estrategia almacena estadísticas agregadas para cada día de negociación (máximo, mínimo, último cierre).
* Se mantiene una ventana deslizante de `SlidingWindowDays` días recientes (incluido el actual).
* `RangeMode` selecciona cómo se calcula el rango de referencia:
  * **HighestLowest** – la distancia entre el máximo más alto y el mínimo más bajo en la ventana.
  * **CloseToClose** – el cambio absoluto promedio entre precios de cierre diarios consecutivos dentro de la ventana.
* Una vez que se alcanza el `StartTime` configurado en un nuevo día, la estrategia reconstruye los niveles de ruptura superior e inferior:
  * `Upper = Highest + Range × OffsetCoefficient`
  * `Lower = Lowest − Range × OffsetCoefficient`
* Hasta que se alcanza `StartTime`, los niveles de ruptura del día anterior permanecen activos (reflejando la implementación MQL).

### Reglas de entrada
* Una entrada larga se activa cuando el precio de cierre de la vela procesada es mayor o igual al nivel superior actual y se han abierto menos de `MaxPositionsPerDay` entradas largas el mismo día.
* Una entrada corta se activa cuando el precio de cierre cae al nivel inferior o por debajo y el límite diario de entradas cortas no se ha alcanzado.
* Al cambiar de una posición existente al lado opuesto, la estrategia primero compensa el volumen pendiente y luego añade el nuevo `Volume` encima, coincidiendo con el comportamiento de neteo del EA original.
* Las señales se evalúan solo en velas terminadas entregadas por la suscripción `CandleType` configurada y solo cuando `IsFormedAndOnlineAndAllowTrading()` informa que se permite operar.

### Reglas de salida
* Las distancias de stop-loss y take-profit se derivan del rango actual: `Range × StopLossCoefficient` y `Range × TakeProfitCoefficient` respectivamente.
* Para posiciones largas, se envía una orden de cierre si el mínimo de la vela toca el nivel de stop o el máximo supera el nivel de take-profit.
* Para posiciones cortas, se envía una orden de cierre si el máximo de la vela toca el nivel de stop o el mínimo cruza el nivel de take-profit.
* Establecer cualquiera de los coeficientes en cero desactiva la protección correspondiente.

### Controles de riesgo y límites
* Se mantienen contadores diarios separados para entradas largas y cortas. Se reinician cada vez que comienza un nuevo día de negociación.
* La propiedad `Volume` de la `Strategy` base controla el tamaño de las entradas adicionales.
* No se registran órdenes pendientes; las salidas se ejecutan con órdenes de mercado en la siguiente iteración de la estrategia después de que se detecta la condición.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `RangeMode` | Determina cómo se calcula el rango diario (`HighestLowest` o `CloseToClose`). | `HighestLowest` |
| `SlidingWindowDays` | Número de días calendario incluidos en la ventana deslizante utilizada para el cálculo del rango. | `3` |
| `StopLossCoefficient` | Multiplicador aplicado al rango actual para definir la distancia del stop-loss. | `0.03` |
| `TakeProfitCoefficient` | Multiplicador aplicado al rango actual para definir la distancia del take-profit. | `0.05` |
| `OffsetCoefficient` | Desplazamiento adicional aplicado a los niveles de ruptura por encima del máximo y por debajo del mínimo. | `0.01` |
| `MaxPositionsPerDay` | Número máximo de entradas permitidas por dirección durante un único día de negociación. | `3` |
| `StartTime` | Hora del día cuando se calcula un rango fresco para la sesión actual. | `10:05` |
| `CandleType` | Suscripción de velas usada para el cálculo del rango y la evaluación de señales. | `Marco temporal de 15 minutos` |

## Notas de implementación
* La estrategia se basa exclusivamente en la infraestructura de alto nivel `Strategy` de StockSharp (`SubscribeCandles`, `WhenNew`, y órdenes de mercado) y no manipula libros de órdenes sin procesar.
* Las estadísticas de rango se almacenan sin usar búsquedas de valores de indicadores; todos los cálculos ocurren dentro de la estrategia, en línea con las directrices del repositorio.
* Las órdenes protectoras se simulan monitoreando los extremos de las velas en lugar de registrar órdenes stop/límite separadas, lo que mantiene la implementación portable entre diferentes adaptadores.
* El soporte de Python se omite intencionalmente según lo solicitado. Solo se proporciona la versión en C# en esta carpeta.
* Para operaciones en vivo, asegúrese de que haya suficientes velas históricas disponibles para que el primer cálculo del rango tenga suficientes datos con los que trabajar.
