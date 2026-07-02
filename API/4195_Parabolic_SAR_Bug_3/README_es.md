# Parabolic SAR Estrategia del error 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Parabolic SAR estrategia de error 3** es una StockSharp adaptación de alto nivel del MetaTrader 4 asesor experto `pSAR_bug_3.mq4` ubicado en `MQL/9786`. El robot reacciona al primer punto Parabolic SAR que aparece en el lado opuesto del precio. Cuando el SAR cae por debajo del cierre de la vela, la estrategia abre una posición larga después de cerrar cualquier exposición corta. Cuando el SAR salta por encima del cierre, invierte a una posición corta. Cada operación está protegida por niveles fijos de stop-loss y take-profit medidos en Parabolic SAR puntos y escalados por el mismo multiplicador que en el programa MQL original.

## Lógica de trading
1. **Datos e indicador de mercado**: la estrategia se suscribe a un tipo de vela configurable (período de tiempo de 15 minutos de forma predeterminada) y vincula un indicador Parabolic SAR con un paso de aceleración especificado por el usuario y una aceleración máxima.
2. **Seguimiento de estado**: después de la primera vela completa, el código almacena si el valor Parabolic SAR está por encima o por debajo del cierre. Las siguientes velas comparan el nuevo estado con el anterior para detectar el giro del indicador.
3. **Entradas largas**: si el Parabolic SAR cambia desde arriba del cierre hasta debajo de él, la estrategia envía una orden de mercado del tamaño de cerrar cualquier posición corta activa y abrir el volumen largo configurado. Los precios protectores de stop-loss y take-profit se calculan inmediatamente después de la entrada.
4. **Entradas cortas**: cuando el Parabolic SAR cruza desde debajo del cierre hacia arriba, el código refleja el comportamiento de las operaciones cortas: aplana las posiciones largas y abre una orden corta.
5. **Salidas** – en cada vela terminada, los precios máximos y mínimos se comparan con los niveles de protección almacenados. Al superar el límite de pérdidas o la toma de ganancias se activa una orden de mercado que cierra la posición abierta, coincidiendo con el enfoque MetaTrader de las órdenes de protección del corredor.

## Gestión del riesgo
- Las distancias de stop-loss y take-profit se convierten multiplicando `StopLossPoints` o `TakeProfitPoints` por el `StopMultiplier` y el instrumento `PriceStep` (o `0.0001` si el símbolo no proporciona un paso).
- Las órdenes de mercado solo se envían cuando `IsFormedAndOnlineAndAllowTrading()` confirma que la suscripción está activa y se permite el comercio.
- Siempre que cambia la dirección de la posición, los niveles de protección no utilizados del lado antiguo se borran para evitar salidas obsoletas.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen de pedidos en lotes. La actualización del valor también cambia la propiedad base `Strategy.Volume`. |
| `StopLossPoints` | `90` | Distancia de stop-loss expresada en Parabolic SAR puntos, posteriormente escalada en `StopMultiplier` y el paso del precio del instrumento. |
| `TakeProfitPoints` | `20` | Distancia de obtención de beneficios expresada en Parabolic SAR puntos, posteriormente escalada en `StopMultiplier` y el paso de precio. |
| `StopMultiplier` | `10` | Multiplicador que reproduce la entrada MetaTrader `StopMult`, lo que permite la compatibilidad con corredores de pips fraccionarios. |
| `SarStep` | `0.02` | Factor de aceleración inicial para el indicador Parabolic SAR. |
| `SarMaximum` | `0.2` | Factor de aceleración máximo para el indicador Parabolic SAR. |
| `CandleType` | `15m time-frame` | Tipo de vela utilizado para cálculos de indicadores y detección de señales. |

## Notas de conversión
- MetaTrader cerró posiciones antes de abrir la operación opuesta utilizando órdenes separadas. La versión StockSharp logra el mismo resultado enviando una única orden de mercado con el tamaño necesario para compensar cualquier exposición opuesta y establecer el nuevo volumen de posición.
- Las órdenes de stop-loss y take-profit del lado del corredor se emulan monitoreando los extremos de las velas y enviando salidas del mercado una vez que se violan los umbrales.
- El parámetro adicional `StopMultiplier` acepta cualquier valor positivo pero el valor predeterminado es `10`, el único multiplicador documentado en los comentarios del código original.
- No se proporciona ninguna versión de Python para esta conversión, exactamente como se solicita en la descripción de la tarea.
