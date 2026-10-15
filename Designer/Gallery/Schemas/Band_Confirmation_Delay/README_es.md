# Diagrama de la estrategia Band Confirmation Delay
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

No hay por qué actuar sobre una señal en el mismo segundo en que aparece. Este diagrama construye una banda de volatilidad por encima de una media móvil y, cuando el cierre la atraviesa, el diagrama no compra. En su lugar arma un bloque de retardo, espera un número fijo de velas y solo entonces vuelve a hacerse la misma pregunta: ¿sigue el precio por encima de la banda? La respuesta en ese momento, y no la que inició la espera, es la que decide la operación.

![schema](schema.svg)

## Resumen de la estrategia

- Una única serie de velas de cinco minutos, solo velas terminadas, alimenta todo el diagrama.
- Una media móvil da la línea media y una desviación estándar da la anchura; una fórmula las combina en una banda superior: la línea media más el multiplicador por la desviación.
- Dos comparaciones reducen todo el cuadro a dos preguntas: si el cierre está por encima de la banda y si el cierre está por debajo de la línea media.
- Una condición lógica combina la ruptura con una posición que no es larga y arma el bloque de retardo de entrada. Mientras el bloque cuenta, las rupturas posteriores se ignoran, de modo que una espera nunca se reinicia con la vela siguiente.
- Cuando el bloque de retardo termina de contar emite un único impulso, y una segunda condición lógica une ese impulso con una ruptura recalculada en ese momento, un enfriamiento ya transcurrido y una posición todavía plana. Sin esa segunda condición el diagrama compraría a ciegas, sobre la base de una señal que ya podría haber desaparecido.
- La salida está construida igual, en espejo: un cierre por debajo de la línea media con una posición larga arma el segundo bloque de retardo, y su impulso, unido a un retorno todavía válido por debajo de la línea media, cierra la operación.
- Un contador de enfriamiento corre en paralelo: cada ejecución propia lo pone a cero, cada vela lo incrementa hasta su tope y una comparación mantiene fuera las nuevas entradas hasta que hayan pasado suficientes velas.
- Las entradas y las salidas son órdenes a mercado a través de bloques Position modify, que abren desde posición plana y cierran la posición completa; el panel del gráfico dibuja las velas, ambos indicadores, las órdenes y las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre termina por encima de la banda superior mientras la posición no es larga. Eso arma el retardo de entrada. Un número configurado de velas después, el retardo libera su impulso y, si en ese momento el cierre sigue por encima de la banda, el enfriamiento desde la última ejecución se ha agotado y la posición sigue plana, el bloque Position modify compra a mercado el volumen de la orden.
- **Entrada en corto**: No hay entradas cortas. Por debajo de la banda el diagrama simplemente se mantiene al margen; la única orden que llega a enviar contra una posición larga es la que la cierra.
- **Salida**: El cierre termina por debajo de la línea media mientras hay una posición larga abierta, lo que arma el retardo de salida. Un número configurado de velas después llega el impulso y, si el cierre sigue por debajo de la línea media y la posición sigue siendo larga, el bloque Position modify de cierre vende a mercado la posición completa. No hay bloque de stop-loss ni de take-profit: este diagrama trata de esperar a que llegue una confirmación, y la línea media es lo único que pone fin a una operación.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la única serie de velas sobre la que funciona todo el diagrama. |
| Middle Line Length | 40 | Longitud de la media móvil que dibuja la línea media y una de las dos mitades de la banda. |
| Deviation Length | 40 | Longitud de la desviación estándar que fija la anchura de la banda; normalmente se mantiene igual a la longitud de la línea media. |
| Band Multiplier | 1.1 | A cuántas desviaciones por encima de la línea media se sitúa la banda superior. Auméntelo para obtener rupturas más raras y más extendidas. |
| Entry Confirm Candles | 3 | Velas que cuenta el retardo de entrada entre la ruptura y la nueva comprobación que puede comprar. |
| Exit Confirm Candles | 3 | Velas que cuenta el retardo de salida entre el retorno por debajo de la línea media y la nueva comprobación que puede cerrar. |
| Cooldown Candles | 8 | Velas que deben transcurrir tras una ejecución propia antes de permitir una nueva entrada. |
| Order Volume | 1 | Tamaño de la orden, en lotes, que se envía en la entrada; la salida siempre cierra lo que esté abierto. |

## Detalles del diagrama

- El bloque de retardo cuenta los valores que le llegan después de haber sido armado, no velas verdaderas consecutivas. Un nuevo armado se ignora mientras cuenta, y el bloque se reinicia una vez que ha disparado, de modo que la espera es una pausa real y no un recuento acumulado de barras favorables.
- Precisamente por eso el impulso liberado se combina con una comparación nueva en lugar de ir directo a la orden. El impulso dice que la espera ha terminado; la comparación dice si el motivo de esa espera sigue existiendo.
- Aquí la longitud de la confirmación es deliberadamente mayor que una vela para que el bloque de retardo tenga algo visible que hacer. Ponga ambos retardos en una vela y el diagrama operará en la propia barra de la ruptura, que es la misma estructura sin la pausa.
- El bloque de velas emite solo velas terminadas. Una actualización de una vela en formación lleva la hora de apertura de la barra, y una orden construida a partir de ella queda fechada por detrás del reloj y se rechaza.
- El enfriamiento es un contador, no un temporizador: una variable que conserva su valor entre velas, que las ejecuciones propias ponen a cero y que una fórmula incrementa con un tope igual a la longitud del enfriamiento, de modo que no puede desbocarse ni bloquear las entradas de forma permanente.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
