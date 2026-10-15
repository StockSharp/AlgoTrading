# Estrategia Elite eFibo Trader v2.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Elite eFibo Trader v2.1 recrea el asesor experto MetaTrader que apila órdenes de tamaño Fibonacci en una dirección mientras comparte un stop de protección común. El puerto StockSharp mantiene el comportamiento original: una única orden de mercado lanza una secuencia de órdenes stop espaciadas por `LevelDistancePips`, y cada nivel completado aumenta la exposición de acuerdo con la progresión Fibonacci. La estrategia cierra inmediatamente toda la cesta una vez que se toca el tope compartido o cuando la ganancia flotante alcanza `MoneyTakeProfit`.

El algoritmo es intencionalmente direccional. Establezca `OpenBuy` en `true` (y ​​`OpenSell` en `false`) para negociar retrocesos alcistas, o active los interruptores para ejecutar la variante bajista. Solo hay una escalera activa a la vez, lo que refleja la lógica de ciclo único del script MQL4.

## Requisitos de datos
- Se suscribe al flujo comercial para recuperar el precio de ejecución más reciente utilizado para la colocación de la escalera, la lógica de seguimiento y la evaluación de la toma de ganancias del dinero.
- Se basa en los metadatos de seguridad (`PriceStep`, `StepPrice`, `VolumeStep`) para traducir las entradas de pips de estilo MetaTrader en precios de intercambio y tamaños de lote.

## construcción de escaleras
1. Cuando no hay exposición y se permite el comercio, la estrategia verifica los cambios de dirección. Exactamente uno de `OpenBuy` o `OpenSell` debe ser verdadero; de lo contrario no se inicia ninguna escalera.
2. El primer nivel Fibonacci se abre en el mercado. Los niveles posteriores se programan como órdenes stop compensadas en `LevelDistancePips * pipSize` del precio de referencia registrado cuando comienza la escalera.
3. Los volúmenes provienen de los parámetros `Level1Volume`... `Level14Volume` y están normalizados a la seguridad `VolumeStep`.
4. Todos los niveles heredan el mismo desplazamiento de parada: `StopLossPips * pipSize`. El precio de parada se calcula por ejecución y luego se ajusta para que cada orden activa comparta el nivel de protección más cercano.

## Detener la gestión
- Cada orden ejecutada almacena su precio de entrada y su parada inicial derivada del desplazamiento del pip.
- En cada tick de operación, la estrategia reevalúa todos los stop abiertos y los alinea con el valor más ajustado en la escalera (stop más alto para largos, stop más bajo para cortos) para imitar las repetidas llamadas `OrderModify` de MetaTrader.
- Cuando el último precio comercial cruza cualquier stop compartido, la estrategia cancela las órdenes pendientes restantes y cierra toda la cesta con órdenes de mercado.

## gestión del dinero
- Las ganancias no realizadas se calculan a partir del instrumento `PriceStep` y `StepPrice` de modo que el objetivo de efectivo refleje las lecturas de `OrderProfit()` de MetaTrader.
- Si el beneficio flotante alcanza o supera `MoneyTakeProfit`, todas las posiciones se cierran y las órdenes pendientes se cancelan inmediatamente.
- Cuando `TradeAgainAfterProfit` es `false`, la estrategia permanece inactiva después de alcanzar el objetivo de dinero hasta que se reinicia manualmente.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `OpenBuy` | Permita que la estrategia construya una escalera alcista (debe ser exclusiva de `OpenSell`). |
| `OpenSell` | Permita que la estrategia construya una escalera bajista (debe ser exclusiva de `OpenBuy`). |
| `TradeAgainAfterProfit` | Reanude el comercio después de que la canasta se cierre con la toma de ganancias del dinero. |
| `LevelDistancePips` | Distancia en MetaTrader pips entre órdenes stop consecutivas. |
| `StopLossPips` | Distancia en MetaTrader pips utilizada para derivar la parada de protección para cada nivel lleno. |
| `MoneyTakeProfit` | Objetivo de beneficio en efectivo que cierra toda la cesta. |
| `Level1Volume` … `Level14Volume` | Volúmenes utilizados para cada nivel Fibonacci; póngalo en cero para omitir un nivel. |

## Notas de implementación
- La conversión de pips sigue la convención MetaTrader: si el símbolo tiene 3 o 5 decimales el pip efectivo es igual a `PriceStep * 10`.
- `StartProtection()` se llama una vez durante el inicio para habilitar las comprobaciones de seguridad integradas StockSharp.
- La lógica de parada compartida mantiene intencionalmente sincronizadas todas las órdenes; una vez que aparece una parada más estricta, se propaga a todos los niveles activos.
- Las órdenes pendientes se limpian automáticamente cada vez que la escalera está plana, replicando las múltiples llamadas `subCloseAllPending()` que se encuentran en el código MQL.

## Consejos de uso
- Asegúrese de que `PriceStep`, `StepPrice` y `VolumeStep` estén configurados en el instrumento; de lo contrario, las conversiones de pips o los objetivos monetarios pueden ser inexactos.
- Los sistemas promediados pueden acumular una gran exposición rápidamente. Verifique los límites de volumen y los requisitos de margen antes de ejecutar la estrategia en vivo.
- Desactive `TradeAgainAfterProfit` para reproducir el comportamiento único en el que EA deja de operar después de cerrar una cesta rentable.
