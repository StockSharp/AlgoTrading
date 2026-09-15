# Diagrama de la estrategia de arbitraje de base porcentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un par tiene dos precios: el que vale y aquel al que se puede operar. El diagrama construye el primero como un instrumento sintético y su media móvil, lee el segundo en los dos libros de órdenes, expresa la distancia entre ambos en porcentaje y abre las dos patas cuando esa distancia supera un umbral.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de índice construye un instrumento sintético dividiendo el precio del primer instrumento entre el del segundo, se suscribe una serie de velas a ese sintético y una media móvil sobre esas velas es el valor justo del par.
- Dos bloques de libro de órdenes aportan los precios a los que el par se puede operar realmente: un convertidor toma el mejor bid del primer instrumento y otro toma el mejor ask del segundo.
- Un libro cambia muchas veces dentro de una misma barra, así que los dos mejores precios no se envían directamente a las condiciones: dos variables retienen cada una la última cotización y la liberan cuando termina una vela operada, y una fórmula divide una entre otra para obtener el ratio ejecutable.
- Un bloque Sync retiene el ratio ejecutable y la vela de ratio con la misma marca de tiempo y los deja salir juntos, de modo que el valor justo y el precio operable que se comparan pertenecen siempre a la misma barra.
- Una fórmula convierte el par liberado en un único número: la distancia del ratio ejecutable a la media justa, en porcentaje de esa media. Ese número es la base.
- Una variable engancha la base a la vela operada, y todas las comparaciones posteriores se evalúan sobre esa barra, de modo que cada orden queda fechada por la barra en la que se decidió.
- La base se compara con el umbral de entrada y con ese mismo umbral en negativo, lo que da una puerta por cada dirección, y con una banda mucho más estrecha alrededor del valor justo, cuyas dos respuestas una condición lógica une en la señal de retorno.
- Un bloque de retardo, armado por cualquiera de las dos entradas, cuenta las barras operadas terminadas y activa un único Flag cuando se alcanza el límite de mantenimiento; un bloque Combination fusiona ese flag con la señal de retorno en un solo flujo de salida, y dos bloques de cierre retiran el par, uno por instrumento.

## Reglas de entrada y salida

- **Entrada en largo**: El ratio ejecutable está por debajo de la media justa en más que la base mínima: el diagrama compra el primer instrumento y vende el segundo, un volumen de orden en cada pata. Cada pata está configurada para abrirse solo desde su propia posición plana, de modo que una señal que se repite mientras el par ya está abierto no le añade nada.
- **Entrada en corto**: El ratio ejecutable está por encima de la media justa en más que la base mínima: el diagrama vende el primer instrumento y compra el segundo, un volumen de orden en cada pata. La misma condición de posición plana protege cada pata por separado.
- **Salida**: Las dos patas se retiran por lo que ocurra primero: la base vuelve a entrar en la banda estrecha alrededor del valor justo, o el bloque de retardo ha contado el límite de mantenimiento de barras operadas terminadas desde la entrada que lo armó. Ambos motivos llegan al mismo disparador a través de un único bloque de fusión. Los bloques de cierre no llevan dirección y calculan ellos mismos su volumen a partir de lo que está abierto, y un bloque de cierre disparado sobre una pata plana simplemente no hace nada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Traded Candles | 00:05:00 | Duración de las velas sobre las que se toman las decisiones y se registran las órdenes. |
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | Expresión a partir de la cual se construye el instrumento sintético: el precio del primer instrumento dividido entre el del segundo. Ambos instrumentos deben existir en los datos conectados, y con ambos se opera. |
| Ratio Candles | 00:05:00 | Duración de las velas sobre las que se construye el valor justo. Debe coincidir con la de la serie operada; de lo contrario, las dos no se encontrarán en un mismo conjunto. |
| Sync Interval | 00:05:00 | Intervalo por el que agrupa el bloque Sync. Debe coincidir con la duración de la vela: un intervalo más corto separa el par de valores que van juntos, y uno más largo une valores de barras distintas. |
| Fair Average Length | 20 | Número de velas de ratio en la media móvil de la que se toma el valor justo. |
| Minimum Basis, % | 0.5 | A qué distancia debe estar el ratio operable del valor justo, en porcentaje, para que se abran las dos patas. |
| Exit Basis, % | 0.1 | A qué distancia debe volver la base al valor justo, en porcentaje, para que se retire el par. |
| Volume Per Leg | 1 | Tamaño de la orden de cada pata, en lotes. A las dos patas se les envía el mismo tamaño. |
| Max Hold Bars | 72 | Cuántas barras operadas terminadas se puede mantener el par antes de cerrarlo con independencia de la base. |

## Detalles del diagrama

- El libro de órdenes nunca se lee directamente hacia una condición. Se actualiza muchas veces por barra, mientras que las condiciones viven en la barra; las dos variables de retención son las que ponen a ambos en un mismo reloj, y su disparador es la vela operada, no el libro.
- Nada que registre una orden se dispara desde una salida de Sync. Un conjunto liberado queda fechado por el más antiguo de sus miembros, y una orden fechada antes de la hora actual se rechaza, así que Sync alimenta la aritmética mientras la vela operada gobierna todos los disparadores.
- Las dos series de velas están configuradas solo para velas terminadas. Una actualización de una barra en formación fecharía la orden en el momento en que la barra se abrió, que ya está en el pasado cuando se decide sobre esa barra.
- Las constantes se reenvían en cada vela operada. Una comparación necesita que sus dos valores vuelvan a llegar en cada evaluación, así que una constante que se envía una sola vez detiene en silencio la condición a la que alimenta.
- Los umbrales inferiores no son constantes propias, sino fórmulas sobre los superiores, de modo que ambas bandas se mantienen simétricas sea cual sea el valor de los umbrales, y el límite de mantenimiento se cuenta en barras terminadas y no en tiempo de reloj.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
