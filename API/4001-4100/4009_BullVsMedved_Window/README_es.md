# Estrategia de ventana Bull vs Medved
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Bull vs Medved es una conversión StockSharp del MetaTrader 4 experto *Bull_vs_Medved.mq4*. El sistema intenta
introduzca retrocesos dentro de un fuerte impulso alcista o bajista colocando órdenes límite pendientes durante seis períodos predefinidos de cinco minutos.
ventanas repartidas a lo largo del día de negociación. La versión StockSharp mantiene la idea de operar solo una vez por ventana, cancela las operaciones obsoletas.
órdenes pendientes y utiliza el tamaño del cuerpo de la vela de señal para derivar distancias dinámicas de stop-loss y take-profit.

## Lógica comercial
1. Suscríbase al flujo de velas definido por `CandleType` y maneje solo velas terminadas.
2. Mantenga las dos últimas velas completadas para que la vela actual (`shift1`), la vela anterior (`shift2`) y la vela
antes de eso (`shift3`) replicar las referencias `Close[1..3]` utilizadas en MetaTrader.
3. Durante cada ventana de negociación (`EntryWindowMinutes` minutos a partir de `StartTime0..5`), verifique los siguientes patrones:
   - **Alcista**: `shift3` cierra por encima de la apertura de `shift2`, el cuerpo de `shift2` tiene al menos 10 puntos de corredor y el cuerpo de
`shift1` es al menos `CandleSizePoints` puntos. Si `IsBadBull` es falso (tres cuerpos largos seguidos), establezca un límite de compra.
   - **Cool Bull**: `shift2` es un retroceso mínimo de 20 puntos que cierra por debajo de la apertura de `shift1`, que a su vez cierra por encima
el `shift2` abierto con un cuerpo de al menos el 40% del umbral; colocar un límite de compra.
   - **Bajista**: el cuerpo de `shift1` tiene al menos `CandleSizePoints` puntos pero es bajista; colocar un límite de venta.
4. Los límites de compra se colocan en `ask - BuyIndentPoints * PriceStep`, los límites de venta en `bid + SellIndentPoints * PriceStep`. solo uno
Puede existir una orden o posición pendiente en un momento, por lo que la estrategia omite nuevas señales si una operación ya está activa dentro del
ventana.
5. Las paradas y los objetivos están ocultos dentro de la estrategia. Cuando se ejecuta una orden de entrada, el cuerpo de la vela de `shift1` se multiplica por
`StopLossMultiplier` y `TakeProfitMultiplier`, normalizados a `PriceStep` y almacenados como precios de salida.
6. En cada vela terminada, la estrategia evalúa si el máximo/mínimo superó el stop u objetivo almacenado. Llegar al nivel
cierra la posición abierta con una orden de mercado y borra las banderas de protección.
7. Los pedidos pendientes de más de 230 minutos se cancelan para imitar la rutina de limpieza MetaTrader y `_orderPlacedInWindow` se
se reinicia cuando el precio sale de la ventana de negociación.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Volumen utilizado para cada orden límite. |
| `CandleSizePoints` | `decimal` | `75` | Tamaño mínimo del cuerpo alcista/bajista (en puntos de corredor) para la vela de señal. |
| `StopLossMultiplier` | `decimal` | `0.8` | Multiplicador aplicado al cuerpo de la vela de señal para construir la distancia de parada. |
| `TakeProfitMultiplier` | `decimal` | `0.8` | Multiplicador aplicado al cuerpo de la vela de señal para construir la distancia objetivo. |
| `BuyIndentPoints` | `decimal` | `16` | Número de puntos del corredor restados de la demanda al establecer límites de compra. |
| `SellIndentPoints` | `decimal` | `20` | Número de puntos del corredor agregados a la oferta al establecer límites de venta. |
| `EntryWindowMinutes` | `int` | `5` | Duración de cada sesión en minutos. |
| `CandleType` | `DataType` | velas de 5 minutos | Serie de velas procesadas por la estrategia. |
| `StartTime0..5` | `TimeSpan` | `00:05`, `04:05`, `08:05`, `12:05`, `16:05`, `20:05` | Hora de inicio de cada ventana de negociación. |

## Diferencias con el experto original.
- El experto MetaTrader asigna stop-loss y take-profit a la orden pendiente. El puerto StockSharp simula eso
comportamiento almacenando niveles ocultos y cerrando la posición neta con órdenes de mercado cuando las velas las rompen.
- Los umbrales de precios utilizan `Security.PriceStep`, por lo que la conversión funciona en cotizaciones de divisas de 4 y 5 dígitos sin
parámetros.
- Sólo se utilizan velas terminadas para evaluar las reglas de parada/objetivo, mientras que las paradas MetaTrader pueden activarse dentro de la barra mediante el
servidor comercial.
- Se omiten las alertas de sonido y los campos de comentarios del EA original; en su lugar, los registros StockSharp proporcionan comentarios.

## Consejos de uso
- La estrategia está diseñada para símbolos de divisas que utilizan precios de pips fraccionarios. Verifique `PriceStep` para confirmar que el sistema basado en puntos
Los filtros coinciden con la distancia de pips prevista.
- Debido a que las paradas y tomas de ganancias están ocultas, considere ejecutar la estrategia en un entorno dedicado o protegerla con un
módulo de riesgo del lado del corredor en caso de que se caiga la conexión.
- Ajuste los valores de `StartTime` si la sesión de su corredor difiere del horario original basado en GMT. Cada ventana se puede desactivar mediante
establecer las horas de inicio fuera de su día de negociación.
- Adjunte la estrategia a un gráfico para visualizar las órdenes limitadas y confirme que solo se intenta una entrada en cada ventana.
