# Estrategia de Cuadrícula de Órdenes Pendientes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento del asesor experto de cuadrícula de órdenes pendientes "AntiFragile" de MetaTrader. Construye continuamente una cuadrícula simétrica de órdenes de stop alrededor del precio de mercado actual y aplica salidas protectoras una vez que se abren las posiciones.

## Lógica central
- Al inicio, la estrategia almacena en caché la mejor oferta y demanda de datos de nivel 1 / libro de órdenes y coloca órdenes de compra-stop por encima del precio y órdenes de venta-stop por debajo del precio.
- Los precios de las órdenes están desplazados del mercado por el parámetro *Distance* y cada nivel subsiguiente está espaciado por *Spacing (ticks)* multiplicado por el paso de precio del instrumento.
- Cada nuevo nivel de cuadrícula aumenta el volumen de la orden en *Volume Increase %* relativo al tamaño inicial, implementando el escalado de estilo martingale de la versión MQL.
- Cuando una orden se ejecuta, la posición neta resultante está protegida con órdenes de stop-loss y take-profit. La lógica de trailing stop opcional reutiliza la última oferta/demanda para ajustar el stop cuando la ganancia no realizada excede la distancia de trailing.
- La cuadrícula se reconstruye automáticamente después de que todas las órdenes pendientes han sido ejecutadas o canceladas y la posición vuelve a ser plana.

## Parámetros
- **Starting Volume** – tamaño de lote/contrato para la primera orden pendiente. Las órdenes subsiguientes escalan por *Volume Increase %*.
- **Volume Increase %** – incremento porcentual añadido a cada nuevo nivel de cuadrícula (0.1 equivale a +0.1% por nivel).
- **Distance** – desplazamiento de precio absoluto añadido antes de la primera orden (interpretado en moneda del instrumento).
- **Spacing (ticks)** – número de pasos de precio entre órdenes de cuadrícula consecutivas.
- **Orders per side** – número máximo de órdenes de cuadrícula para largos y cortos por separado.
- **Take Profit (ticks)** – distancia del objetivo de ganancia desde la entrada promedio, expresada en pasos de precio.
- **Stop Loss (ticks)** – distancia del stop desde la entrada promedio. Establecer en cero para deshabilitar el stop inicial.
- **Trailing Stop (ticks)** – distancia de trailing. Establecer en cero para deshabilitar los ajustes de trailing.
- **Enable Long Grid / Enable Short Grid** – interruptores para colocar órdenes buy-stop o sell-stop.

## Notas de implementación
- Las estrategias de StockSharp usan posiciones netas, por lo tanto las ejecuciones opuestas se compensarán entre sí en lugar de crear cestas cubiertas como en MT4. La cuadrícula sigue siendo simétrica pero solo se rastrea la exposición neta.
- Los volúmenes y precios se redondean a los tamaños de paso del instrumento antes de enviar las órdenes.
- Los stops trailing se recrean cancelando la orden de stop anterior y enviando una nueva a un nivel más ajustado una vez que la ganancia excede la distancia de trailing.
- La estrategia requiere datos del libro de órdenes (SubscribeOrderBook) para impulsar el rastreo de precio y la lógica de trailing.

## Consejos de uso
1. Configure *Starting Volume* y *Volume Increase %* de forma conservadora; los valores predeterminados originales asumen el dimensionamiento de lote de Forex y pueden crecer rápidamente.
2. Asegúrese de que el portafolio soporte órdenes de stop para el lugar de destino. Todas las entradas de cuadrícula son órdenes stop-market.
3. Monitoree los requisitos de margen porque un gran número de órdenes pendientes puede consumir capital reservado.
