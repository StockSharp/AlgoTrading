# Diagrama de la estrategia ATR Step Streak
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos medias móviles deciden la dirección, pero no se compra nada en el momento en que se cruzan. El diagrama espera un número fijo de velas cerradas después de ese momento y solo entonces pregunta si la operación sigue mereciendo la pena. La espera la realiza un bloque N values, que se arma con la comparación de las dos medias y habla una vez transcurridas las velas que se le indicó contar. Una segunda condición mide cuánto espacio queda hasta el extremo del rango reciente, y se mide en volatilidad y no en precio: el cierre tiene que estar al menos a un paso de ATR del máximo más alto antes de una compra, y a ese mismo paso por encima del mínimo más bajo antes de una venta.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas cerradas de quince minutos alimentan un conversor que extrae el precio de cierre y cinco indicadores: una media móvil simple rápida y otra lenta, un ATR, y el máximo más alto y el mínimo más bajo del canal reciente.
- Dos bloques Comparison leen las medias una contra otra e informan en cada vela: uno es verdadero mientras la media rápida está por encima de la lenta, el otro mientras está por debajo.
- Cada comparación arma su propio bloque N values. El bloque toma el flujo de velas en su entrada, cuenta las velas cerradas que siguen a la señal de armado, emite un único pulso cuando se agota la cuenta y vuelve a armarse con la siguiente comparación verdadera, de modo que el lado de entrada del diagrama funciona con un reloj más lento que los datos de mercado.
- Una Formula multiplica el ATR por el multiplicador de paso, y otras dos Formulas convierten ese paso en un par de niveles de guarda: el máximo más alto menos el paso, y el mínimo más bajo más el paso.
- Otras dos comparaciones preguntan si el cierre sigue por debajo de la guarda superior, o todavía por encima de la inferior, es decir, si el precio se ha adentrado tanto en el canal que ya no queda espacio para la operación.
- Un bloque Position se mide contra una variable de valor cero, de modo que el lado de entrada sabe si la posición está plana.
- Cada lado reúne cuatro condiciones en un Logical condition configurado como And: el pulso de N values, la comparación de las medias verificada de nuevo en ese instante, el espacio hasta el extremo del canal y la posición plana. Solo cuando las cuatro llegan juntas habla la puerta.
- Modify position abre la operación a mercado con el volumen tomado de una variable, otros dos bloques Modify position la cierran con la comparación opuesta, Position protection se arma con cada ejecución de entrada, y el panel del gráfico dibuja las velas, ambas medias, ambos extremos del canal, todas las órdenes y todas las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: La media rápida está por encima de la lenta, el bloque N values de ese lado emite su pulso en esta vela, el cierre está al menos a un paso de ATR por debajo del máximo más alto del canal y la posición está plana. Las cuatro condiciones se encuentran en el And largo, y Modify position compra el volumen de la orden a mercado. El bloque está configurado solo para abrir, de modo que no se compra nada mientras ya se mantiene una posición de cualquier lado.
- **Entrada en corto**: La imagen especular: la media rápida está por debajo de la lenta, el bloque N values bajista emite su pulso, el cierre está al menos a un paso de ATR por encima del mínimo más bajo del canal y la posición está plana. El And corto pasa la señal a un bloque Modify position que vende el volumen de la orden a mercado, de nuevo solo abriendo.
- **Salida**: Hay dos formas de salir. El cruce de las medias es la primera: la comparación que resulta verdadera tras el cruce activa un bloque de cierre para el lado que se mantiene, y ese bloque envía la posición entera a mercado. Position protection es la segunda: armada por cada ejecución de entrada, toma el beneficio en el 2% y corta la pérdida en el 1%, medidos contra el precio de cierre de las velas cerradas que se introduce en su entrada de precio. Como ambos bloques de entrada solo abren, la comparación que termina una operación nunca abre la contraria; la siguiente entrada tiene que esperar al siguiente pulso que encuentre la posición plana.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:15:00 | Marco temporal de las velas sobre las que trabaja todo el diagrama. Cada recuento del diagrama, las medias, el ATR, el canal y el periodo de espera, se mide en estas velas. |
| Fast SMA Length | 20 | Longitud de la media rápida. Acórtala y las dos medias se cruzan más a menudo, lo que arma el bloque de espera con más frecuencia y produce más entradas. |
| Slow SMA Length | 60 | Longitud de la media lenta. La diferencia entre esta y la longitud rápida decide cuánto tiene que durar una tendencia para ser reconocida siquiera. |
| Bull Streak Bars | 3 | Cuántas velas cerradas cuenta el bloque N values del lado largo entre el momento en que las medias se alinean y el pulso que emite. Uno hace que el diagrama entre en la vela siguiente al cruce; un valor grande retrasa la entrada hasta bien avanzado el movimiento y, como el bloque se rearma después de cada pulso, también separa más las entradas entre sí. |
| Bear Streak Bars | 3 | La misma espera en el lado corto. Mantenla igual que la del lado largo salvo que las dos direcciones deban confirmarse a lo largo de periodos de tiempo distintos. |
| ATR Length | 14 | Ventana del ATR que mide la volatilidad. Fija la unidad en la que se expresa la distancia hasta el extremo del canal. |
| Step Multiplier | 2 | Cuántos ATR de espacio debe tener el precio hasta el extremo del canal. Súbelo y las entradas solo se toman bien lejos del extremo, lo que es más raro; bájalo hacia cero y la condición prácticamente desaparece, dejando sola a la señal de tendencia retardada. |
| Channel High Length | 20 | Sobre cuántas velas se toma el máximo más alto. Es el techo por debajo del cual una entrada larga tiene que mantenerse a la distancia del paso. |
| Channel Low Length | 20 | Sobre cuántas velas se toma el mínimo más bajo. Es el suelo por encima del cual una entrada corta tiene que mantenerse a la distancia del paso. Mantenlo igual que la ventana del máximo salvo que busques un canal asimétrico. |
| Order Volume | 1 | Volumen que envían ambos bloques de entrada. Los bloques de cierre no toman volumen: envían lo que mantenga la posición. |
| Take Profit, % | 2 | Objetivo de beneficio del bloque de protección, como porcentaje del precio de ejecución. |
| Stop Loss, % | 1 | Límite de pérdida del bloque de protección, como porcentaje del precio de ejecución. Junto con el objetivo decide cuántas operaciones terminan por la protección en lugar de por el cruce de las medias. |

## Detalles del diagrama

- El bloque N values es un retardo, no un contador de barras consecutivas: se arma con la primera comparación verdadera que ve, cuenta las velas cerradas que siguen y habla una sola vez. No comprueba que la condición se haya mantenido durante toda la ventana, y por eso la misma comparación se vuelve a consultar dentro del And en el momento en que llega el pulso: una tendencia que se deshizo durante la espera no supera esa segunda pregunta y no se envía ninguna orden.
- Un Logical condition limpia sus entradas en cuanto ha hablado, de modo que la puerta de entrada solo puede evaluarse en las velas que llevan un pulso. Entre dos pulsos las comparaciones siguen actualizándose, pero nada llega a los bloques de entrada, y eso es lo que impide que un diagrama cuyas condiciones son verdaderas durante horas opere en cada vela.
- Los niveles de guarda se trazan con el mismo ATR que mide la volatilidad, así que la distancia que el precio debe mantener respecto al extremo del canal crece cuando el mercado se mueve más rápido y se reduce cuando se calma. Ambos indicadores de canal incluyen la vela que se está evaluando, de modo que el nivel se mueve con los extremos en lugar de quedarse rezagado.
- Las constantes del diagrama, el cero, el volumen de la orden y el multiplicador de paso, se disparan todas con el flujo de velas, así que sus valores llegan en el mismo tick que los valores de indicador con los que se comparan y se multiplican.
- Los bloques de cierre están conectados directamente a las comparaciones de las medias, de modo que se disparan en cada vela en la que las medias mantienen ese orden. Su condición de cierre hace el filtrado: cuando no hay posición en ese lado el bloque no hace nada en absoluto, y ninguna orden sale del diagrama.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
