# Diagrama de la estrategia Narrow Range Limit Bracket
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un mercado que deja de moverse es un mercado que se prepara para moverse. Este diagrama mide la altura de cada vela, detecta el momento en que esa altura se reduce hasta la menor de varias velas y encierra esa vela entre dos niveles: su máximo por encima y su mínimo por debajo. El nivel que el precio rebase primero al cierre decide la dirección de la operación.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de 15 minutos ya cerradas alimentan tres conversores que leen el máximo, el mínimo y el cierre de cada vela.
- Una fórmula resta el mínimo al máximo, de modo que cada vela queda reducida a un único número: su rango.
- El indicador Lowest se aplica sobre ese número, no sobre el precio, y devuelve el rango más pequeño de las últimas seis velas; una fórmula lo multiplica por el Squeeze Factor para obtener la anchura dentro de la cual debe caber una vela para considerarse estrecha.
- Una segunda rama reconstruye la misma medición una vela atrás: Previous value entrega la vela anterior, dos conversores y una fórmula dan su rango, y otro Previous value toma la lectura que el indicador Lowest tenía en ese momento.
- Una condición lógica reúne tres respuestas: esta vela es estrecha, la anterior no lo era y la posición está plana. Eso es la compresión.
- Con la compresión, el máximo y el mínimo de esa vela se guardan en dos variables, que mantienen esos dos precios vela tras vela: el bracket. Una tercera variable lo arma.
- A partir de la vela siguiente, dos comparaciones vigilan el cierre frente a los dos niveles, y una condición lógica libera una entrada a mercado cuando el cierre rebasa uno de ellos, el bracket está armado y la posición está plana.
- Dos paneles de gráfico dibujan el resultado: en uno, el precio con los niveles del bracket, las órdenes y las ejecuciones; en el otro, el rango de la vela frente a su límite de compresión.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre de una vela queda por encima del nivel superior del bracket, el bracket está armado y la posición está plana. Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: El cierre de una vela queda por debajo del nivel inferior del bracket, con las mismas condiciones de bracket armado y posición plana. Position modify vende el volumen de la orden a mercado.
- **Salida**: El diagrama no tiene señal de salida propia. Combination fusiona las dos entradas en un único flujo de ejecuciones que alimenta a Position protection: este se hace cargo de la posición y la cierra con 550 unidades de precio de beneficio o 550 unidades de precio de pérdida, medidas desde el precio de ejecución. Ese mismo flujo de ejecuciones desarma el bracket, de modo que un nivel ya operado no puede volver a operarse.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:15:00 | Marco temporal de las velas con las que trabaja todo el diagrama. |
| Period | 6 | Número de velas sobre las que el indicador Lowest mide el rango más pequeño. |
| Squeeze Factor | 1.08 | Cuánto más ancha que el menor rango reciente puede ser la vela actual y seguir contando como estrecha. |
| Expansion Factor | 1.05 | Cuánto más ancha que su propio menor rango reciente debía ser la vela anterior para que la compresión cuente como nueva. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Take Profit | 550 | Distancia del take-profit respecto al precio de ejecución, en unidades de precio. |
| Stop Loss | 550 | Distancia del stop-loss respecto al precio de ejecución, en unidades de precio. |

## Detalles del diagrama

- La compresión compara una vela con su propia historia reciente, por lo que no necesita una definición fija de lo que es una vela estrecha: el Squeeze Factor solo indica cuánto debe acercarse el rango actual al menor rango reciente.
- El Expansion Factor protege la otra cara de la misma idea. Sin él, una serie de velas igual de tranquilas seguiría generando señales una y otra vez; exigir que la vela anterior fuera más ancha que su propio límite hace que la señal marque el momento en que la volatilidad se desploma, y no todo el tramo tranquilo.
- Al indicador Lowest se le entrega un número en lugar de una vela, y eso es lo que permite que un indicador de precio corriente funcione como medidor de volatilidad.
- La compresión arma el bracket y la ejecución de una entrada lo desarma; sus niveles se mantienen en su sitio hasta que la siguiente compresión los sustituye. Por tanto, un bracket vive hasta que se opera o hasta que aparece una nueva compresión, en lugar de caducar por número de barras.
- La compresión exige además una posición plana, de modo que los niveles nunca se reescriben mientras hay una operación en curso, y la condición de posición de ambos bloques de entrada rechaza una segunda orden sobre una posición abierta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
