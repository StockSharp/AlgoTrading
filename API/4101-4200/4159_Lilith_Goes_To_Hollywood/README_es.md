# Lilith va a la estrategia de Hollywood
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea el comportamiento de la MetaTrader experta "Lilith va a Hollywood" dentro del StockSharp nivel alto API. Implementa una red de cobertura que puede operar en dos modos muy diferentes:

* **Modo automatizado**: Parabolic SAR activa entradas inmediatas al mercado cada vez que el precio cruza el valor de parada y reversión.
* **Modo manual**: las órdenes stop/límite pendientes se estacionan alrededor de precios de referencia definidos por el usuario y se dejan para ejecutar.

En ambos casos, la estrategia realiza un seguimiento de la exposición larga y corta por separado, calcula el PnL flotante de la red abierta y utiliza esa información para decidir cuándo desplegar órdenes de recuperación adicionales.

## Modos de funcionamiento
* **Automatizado**: cuando no hay ninguna posición abierta, la estrategia se suscribe al indicador Parabolic SAR (0,02/0,2 factores). Si el cierre de la vela está por encima del indicador, compra en el mercado, si está por debajo, vende. El precio ejecutado se convierte en el nuevo **enfoque** y las paradas de recuperación se arman a una distancia de anclaje configurable a su alrededor.
* **Manual**: cuando no hay ninguna posición abierta, la estrategia envía una única orden pendiente por lado. Si el mercado cotiza por debajo del nivel de compra, se crea una parada de compra; de lo contrario, se envía un límite de compra. El lado de la venta refleja la misma lógica en torno al nivel `PriceDown`. Una vez que una de las órdenes se completa, la otra parte permanece activa hasta que se cancela manualmente o mediante la estrategia.

## Lógica de gestión de pedidos
* La cuadrícula sigue acumulando totales de volúmenes largos/cortos completados y órdenes de compra/venta pendientes. Esto permite que la estrategia mida los desequilibrios entre ambos lados del libro.
* Siempre que el beneficio flotante alcanza el objetivo dinámico (`account value / 1000`), la estrategia cierra todas las posiciones y cancela todas las órdenes pendientes.
* Si el PnL flotante cae por debajo de `-AccountValue * RiskPercent / 100`, se implementa una cobertura de emergencia abriendo órdenes de mercado que cubren el exceso neto a corto o largo plazo.
* Las órdenes de recuperación se expresan como órdenes stop colocadas alrededor del precio de enfoque (modo automatizado) o alrededor de los precios manuales configurados. Su tamaño se calcula como `(opposite exposure * XFactor) - current exposure`, imitando la lógica MT4 de sobredimensionar el siguiente pedido para reequilibrar la red.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Automated` | Permite entradas de mercado impulsadas por Parabolic SAR. Desactivar para trabajar en modo manual de orden pendiente. |
| `PriceUp` | Precio de referencia utilizado para crear órdenes de compra stop/limit en modo manual. |
| `PriceDown` | Precio de referencia utilizado para crear órdenes stop/limit de venta en modo manual. |
| `AnchorSteps` | Distancia, expresada en incrementos de precio, utilizada para compensar las órdenes de recuperación del precio focal. |
| `ManualVolume` | Tamaño de lote base cuando se opera manualmente o cuando el tamaño de posición dinámico produce cero. |
| `XFactor` | Multiplicador aplicado a la exposición contraria al dimensionar las órdenes de recuperación. |
| `RiskPercent` | Pérdida flotante máxima (porcentaje del valor de la cuenta) tolerada antes de que la estrategia implemente una cobertura de emergencia. |
| `CandleType` | Marco temporal utilizado para impulsar la Parabolic SAR y la lógica de gestión general. |

## Controles de riesgo
* La toma de ganancias es dinámica y aumenta con el valor de la cuenta, lo que proporciona una forma automática de aumentar el objetivo a medida que la cuenta crece.
* La cobertura de emergencia puede neutralizar caídas extremas al aplanar el lado más expuesto de la red una vez que la pérdida flotante excede el umbral `RiskPercent`.
* Todas las órdenes pendientes se redondean al tamaño del tick del instrumento y los volúmenes se ajustan para respetar los límites de cambio, igualando las protecciones típicas del experto MetaTrader original.

## Notas de conversión
* MetaTrader las garrapatas se reemplazan con velas terminadas. El período de tiempo predeterminado de un minuto mantiene la estrategia reactiva, pero se puede ajustar mediante el parámetro `CandleType`.
* La configuración `Anchor` de la fuente MQL expresó la distancia en puntos. Aquí se configura como una serie de pasos de precio para que se adapte automáticamente al tamaño del tick del instrumento.
* La salida del "Comentario" original se convirtió en mensajes de registro de estrategia (`LogInfo`), por lo que el diario de la plataforma contiene los mismos comentarios sin depender de las anotaciones del gráfico.
