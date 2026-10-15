# Diagrama de la estrategia de votación de SMA en varios marcos temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una media móvil que gira al alza dice poco; tres de ellas girando al alza a la vez, en tres marcos temporales distintos, son una tendencia que merece operarse. Cada marco temporal emite un voto de más uno, menos uno o cero, Sync reúne los tres votos en un único conjunto y solo se abre una posición cuando el voto es unánime.

![schema](schema.svg)

## Resumen de la estrategia

- Tres bloques de velas leen el mismo instrumento en cinco minutos, quince minutos y una hora, y cada uno de ellos deja pasar únicamente velas terminadas.
- Cada serie alimenta su propia media móvil simple, y un bloque Previous value conserva esa misma media tal como estaba una barra antes.
- Una fórmula convierte el par en un voto: más uno cuando la media está por encima de donde estaba, menos uno cuando está por debajo, cero cuando no se ha movido.
- Los votos de cinco y de quince minutos se guardan en variables y los libera el voto horario, de modo que las tres líneas llegan a Sync pertenecientes a un solo y mismo momento.
- Sync mantiene una línea por voto y deja salir a las tres juntas; sin él la lectura horaria llegaría una hora después que la de cinco minutos y las tres nunca podrían compararse.
- Después de Sync cada voto se compara con cero, y dos condiciones lógicas plantean la única pregunta que le importa al diagrama: si los tres apuntan en la misma dirección.
- El veredicto se engancha a la siguiente vela de cinco minutos terminada, que es el compás con el que se fecha toda orden, y Position modify abre a mercado desde una posición plana.
- La posición, muestreada en ese mismo compás de cinco minutos, se compara con cero: el veredicto contrario frente a una posición viva la cierra a mercado, y el nuevo lado se toma en la vela siguiente.

## Reglas de entrada y salida

- **Entrada en largo**: Los tres votos son más uno en el compás horario: las medias de cinco minutos, quince minutos y una hora están cada una por encima de su propio valor una barra atrás. El veredicto se traslada a la siguiente vela de cinco minutos terminada, donde Position modify compra el volumen de la orden a mercado, y solo desde una posición plana.
- **Entrada en corto**: Los tres votos son menos uno en el mismo compás: cada media está por debajo de su propio valor una barra atrás. En la siguiente vela de cinco minutos terminada, Position modify vende el volumen de la orden a mercado, de nuevo solo desde una posición plana.
- **Salida**: No hay take-profit, stop-loss ni temporizador. Una posición vive hasta que los tres marcos temporales se alinean en sentido contrario: el veredicto opuesto, junto con la comparación de la posición, la cierra a mercado en la vela de negociación, y la entrada del nuevo lado llega en la vela siguiente, una vez que la posición vuelve a estar plana. Un voto mixto no cambia nada y deja la posición en paz.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast Candles | 00:05:00 | Marco temporal del voto más rápido, y compás en el que se actúa sobre los veredictos y se fechan las órdenes. |
| Medium Candles | 00:15:00 | Marco temporal del voto intermedio. |
| Slow Candles | 01:00:00 | Marco temporal del voto más lento, y compás en el que se reúnen y se cuentan los tres votos. |
| Fast SMA Length | 13 | Longitud de promediado de la media de cinco minutos. |
| Medium SMA Length | 13 | Longitud de promediado de la media de quince minutos. |
| Slow SMA Length | 13 | Longitud de promediado de la media horaria. |
| Order Volume | 1 | Tamaño de la orden, en lotes, utilizado para ambos lados de entrada; una salida cierra lo que la posición tenga. |

## Detalles del diagrama

- Las tres medias emiten únicamente valores formados y definitivos, de modo que una vela aún en construcción nunca puede mover un voto.
- Reducir cada marco temporal al signo de su pendiente es lo que hace que los tres sean comparables: una media horaria y una de cinco minutos viven en escalas distintas, pero más uno y menos uno no.
- Los dos votos más rápidos entran en Sync a través de una variable que libera el voto horario. Es deliberado: unas líneas que llegasen de una en una dejarían a Sync sosteniendo un conjunto que nunca se completa, y nada posterior volvería a dispararse.
- Sync sella el conjunto que libera con el momento al que ese conjunto pertenece, que va por detrás del reloj para cuando la hora se cierra. Nada en el camino hacia una orden lee ese sello: el veredicto se engancha una segunda vez a la vela de cinco minutos terminada que lleva la operación.
- Ambas entradas están condicionadas a la posición abierta, así que un veredicto repetido en cada vela de cinco minutos no puede acumular órdenes: mientras vive una posición, la repetición simplemente se rechaza.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
