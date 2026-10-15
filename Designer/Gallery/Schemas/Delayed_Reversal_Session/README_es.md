# Diagrama de la estrategia de reversión retardada con sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama coloca dos cosas entre una señal y una orden: una espera y un reloj. Se traza un corredor porcentual alrededor de una media móvil, y la salida del precio de ese corredor se interpreta como una reversión. La reversión no se opera en el momento en que se produce: se entrega a un bloque de retardo que cuenta un número fijo de velas terminadas, y solo el impulso que sale por el otro extremo llega a los bloques de órdenes, y únicamente mientras el bloque de horario de trabajo indique que el reloj está dentro de la sesión.

![schema](schema.svg)

## Resumen de la estrategia

- Una única serie de velas, de treinta minutos y solo velas terminadas, alimenta todas las ramas del diagrama.
- Una media móvil aporta la línea central; una constante guarda la sensibilidad, y dos fórmulas convierten ese par en un borde superior y otro inferior: la media más y menos la media multiplicada por la sensibilidad.
- Dos bloques de cruce vigilan el cierre frente a esos bordes. Uno se dispara cuando el cierre cruza el borde superior desde abajo; el otro está conectado al revés, con el borde inferior en la entrada ascendente, de modo que se dispara cuando el cierre cae a través del borde inferior.
- Cada cruce arma su propio bloque de retardo. El retardo cuenta las velas terminadas que llegan después del armado y libera un único impulso cuando la cuenta se agota, de modo que la señal se ejecuta más tarde y no en la barra que la generó.
- El bloque de horario de trabajo lee la hora de cada vela e indica si el momento está dentro de la sesión; un NOT lógico convierte esa misma bandera en una bandera de fuera de sesión.
- Un AND lógico por cada lado une el impulso liberado con la bandera de sesión, de modo que un impulso que cae fuera del horario de trabajo se descarta en lugar de ponerse en cola.
- Las entradas son órdenes a mercado mediante bloques Position modify configurados para abrir solo desde posición plana, así una señal retardada abre una posición y los impulsos repetidos en la misma dirección no pueden piramidar.
- Un tercer bloque Position modify cierra lo que esté abierto, impulsado por tres fuentes: la señal retardada de cualquiera de los dos lados y la bandera de fuera de sesión; el panel del gráfico dibuja las velas, la media, ambos bordes del corredor, las órdenes y las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre cruza el borde superior del corredor desde abajo, lo que arma el retardo largo. Un número configurado de velas terminadas después, el retardo libera su impulso; si en ese momento el reloj está dentro de la sesión, el bloque Position modify del lado largo compra el volumen de la orden a mercado. Como el bloque solo abre desde posición plana, la compra se omite cuando ya hay una posición abierta: ese mismo impulso ya ha ido entonces al bloque de cierre.
- **Entrada en corto**: El cierre cae a través del borde inferior del corredor, lo que arma el retardo corto. Un número configurado de velas terminadas después llega el impulso y, si el reloj está dentro de la sesión, el bloque Position modify del lado corto vende el volumen de la orden a mercado, de nuevo solo desde una posición plana.
- **Salida**: Dos cosas terminan una operación. Una señal retardada de cualquiera de los dos lados está conectada tanto al bloque de cierre como a su propio bloque de entrada, de modo que una reversión mantenida en contra de una posición abierta la cierra a mercado; la entrada que sigue espera a la siguiente señal, porque los bloques de apertura solo funcionan desde posición plana. La otra es el reloj: en cuanto la bandera de horario de trabajo pasa a falsa, el bloque NOT se dispara en cada vela y el bloque de cierre cierra la posición y la mantiene plana hasta que la sesión vuelve a abrir. Con la posición plana, el bloque de cierre no hace nada. Aquí no hay bloque de stop-loss ni de take-profit.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:30:00 | Marco temporal de la única serie de velas sobre la que funciona todo el diagrama. |
| Average Length | 20 | Longitud de la media móvil que dibuja el centro del corredor. |
| Corridor Sensitivity | 0.004 | Semiancho del corredor como fracción de la media: el valor por defecto es cuatro décimas de por ciento a cada lado. Auméntalo para obtener reversiones más raras y amplias; redúcelo y los bordes se cruzarán mucho más a menudo. |
| Long Signal Delay | 2 | Velas terminadas contadas entre la reversión al alza y el impulso que puede comprar. Uno significa la vela siguiente; valores mayores retienen la señal más tiempo. |
| Short Signal Delay | 2 | Velas terminadas contadas entre la reversión a la baja y el impulso que puede vender. |
| Session Start | 08:00:00 | Inicio de la sesión de trabajo. Los impulsos liberados antes se descartan y el diagrama permanece plano. |
| Session End | 20:00:00 | Fin de la sesión de trabajo. Desde ese momento, la bandera de fuera de sesión cierra cualquier posición abierta en cada vela hasta que la sesión vuelve a abrir. |
| Order Volume | 1 | Tamaño de la orden, en lotes, enviado en la entrada; el bloque de cierre siempre cierra lo que esté abierto. |

## Detalles del diagrama

- Un bloque de cruce es un evento, no un estado. Solo habla en la vela en la que las dos series intercambian posiciones, así cada retardo se arma una vez por reversión en lugar de rearmarse en cada vela que el precio pasa fuera del corredor.
- El bloque de cruce también informa de la dirección contraria como un valor falso, y tanto la entrada de armado del retardo como el disparador de la orden ignoran el valor falso, de modo que la mitad descendente de un bloque de cruce no puede armar ni disparar la rama ascendente.
- Mientras un retardo está contando, un segundo armado se ignora. Una ráfaga de reversiones produce por tanto un solo impulso y no una cola de ellos, y la cuenta es una pausa real y no un recuento.
- El bloque de velas emite únicamente velas terminadas. La actualización de una vela en formación lleva la hora de apertura de la barra, y una orden construida a partir de ese valor queda fechada por detrás del reloj del emulador y es rechazada.
- La sesión se lee de la hora de la propia vela y no del reloj del sistema, así una reproducción se comporta exactamente igual que una ejecución en vivo y el mismo diagrama puede probarse en histórico sin cambiar nada.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
