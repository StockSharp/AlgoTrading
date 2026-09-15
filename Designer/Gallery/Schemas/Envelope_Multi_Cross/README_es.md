# Diagrama de la estrategia Envelope Multi Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una media rápida y una media lenta no se encuentran en un único punto limpio: se tocan, se separan y vuelven a tocarse alrededor de la misma zona. Este diagrama lo asume y convierte esa zona en tres niveles: una banda estrecha trazada por encima y por debajo de la media lenta, y la propia media lenta. Cada nivel recibe su propio bloque de cruce, y dos bloques Combination canalizan seis cruces independientes hacia un único flujo largo y un único flujo corto, de modo que toda una escalera de señales llega a un solo par de bloques de órdenes.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas terminadas de un instrumento alimentan dos medias móviles exponenciales, una rápida y una lenta, y un conversor que extrae el precio de cierre de esa misma vela.
- Dos fórmulas toman la media lenta y la escalan hacia arriba y hacia abajo en una fracción fija, produciendo una envolvente superior y una envolvente inferior; con la media lenta entre ambas, el diagrama tiene tres niveles en lugar de uno.
- Tres bloques de cruce vigilan que la media rápida suba a través de la envolvente inferior, a través de la media lenta y a través de la envolvente superior, cada uno sobre su propio par de entradas.
- Otros tres bloques de cruce llevan esos mismos tres niveles con las entradas intercambiadas —el nivel arriba, la media rápida abajo—, de modo que informan de la media rápida hundiéndose a través de dichos niveles.
- Un bloque Combination une los tres cruces al alza en un único flujo y un segundo une los tres cruces a la baja; a partir de ese punto, toda la escalera es una sola señal por lado.
- Cada flujo se confirma con una comparación del precio de cierre frente a la media rápida, de modo que una perforación cuenta como entrada solo mientras el precio esté del mismo lado de la línea rápida.
- Las entradas son órdenes de mercado de volumen fijo abiertas desde una posición plana; el flujo opuesto, usado en bruto, acciona por sí solo un bloque de cierre de posición.
- Position protection se hace cargo de cada ejecución de entrada y la acompaña con un take-profit porcentual y un stop-loss dinámico (trailing).

## Reglas de entrada y salida

- **Entrada en largo**: El flujo largo se dispara: la media rápida ha cruzado por encima de la envolvente inferior, por encima de la media lenta o por encima de la envolvente superior. La comparación confirma que la vela cerró por encima de la media rápida, las dos respuestas se encuentran en una condición lógica y el bloque Position modify compra el volumen de la orden a mercado. El ajuste de apertura de posición deja pasar esa orden solo mientras la posición está plana.
- **Entrada en corto**: El flujo corto se dispara de la misma manera, sobre los cruces reflejados: la media rápida se ha hundido a través de la envolvente superior, a través de la media lenta o a través de la envolvente inferior. La comparación confirma que la vela cerró por debajo de la media rápida, y el bloque Position modify vende el volumen de la orden a mercado desde una posición plana.
- **Salida**: Dos cosas independientes terminan una operación. El flujo opuesto, tomado sin la confirmación del precio, dispara un bloque de cierre de posición: cualquier perforación a la baja aplana una posición larga y cualquier perforación al alza aplana una posición corta. Mientras tanto, Position protection vigila las ejecuciones de entrada, lee el precio de cierre y cierra la operación en el take-profit o en el stop dinámico, lo que se alcance primero.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:15:00 | Marco temporal de la serie de velas sobre la que se calcula todo lo demás. |
| Fast EMA Length | 10 | Longitud de la media móvil exponencial rápida: la línea que perfora. |
| Slow EMA Length | 30 | Longitud de la media móvil exponencial lenta: la línea alrededor de la cual se traza la banda. |
| Envelope Buffer | 0.003 | Semiancho de la banda como fracción de la media lenta: 0.003 sitúa las envolventes un 0.3% por encima y por debajo de ella. |
| Order Volume | 1 | Tamaño de la orden, en lotes, para ambas direcciones de entrada. |
| Take Profit, % | 1.5 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 0.8 | Distancia del stop-loss, en porcentaje del precio de entrada; acompaña por detrás a una posición que se mueve a favor. |

## Detalles del diagrama

- Los dos bloques Combination son lo que hace legible la escalera. Sin ellos, cada uno de los seis cruces necesitaría su propio cable hasta los bloques de órdenes, y añadir un cuarto nivel obligaría a redibujar todo el lado derecho del diagrama.
- Un bloque de cruce informa de la dirección en la que se produjo el cruce, y un cruce a la baja llega como una señal negativa que el disparador de una orden ignora en silencio. Por eso el conjunto bajista se construye como tres bloques separados con las entradas intercambiadas, y no negando el conjunto alcista.
- Las velas se suscriben únicamente como terminadas. La actualización de una vela aún en formación lleva la marca de tiempo de la apertura de la barra, y una orden fechada antes del momento actual es rechazada.
- Las entradas usan la condición de apertura de posición, de modo que una señal que llega mientras ya hay una operación en curso no cuesta nada: el bloque informa de un volumen no válido y no se envía ninguna orden. Ese único ajuste hace el trabajo de un filtro de posición explícito dentro de la condición de entrada.
- La banda es deliberadamente estrecha. Es un colchón alrededor de la media lenta, no un canal de volatilidad, de modo que los dos niveles adicionales se disparan cerca del cruce simple y engrosan la señal en lugar de sustituirla.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
