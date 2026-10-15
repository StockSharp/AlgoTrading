# Diagrama de la estrategia Quote Momentum Sync
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum medido de un cierre al siguiente. El diagrama compara el cierre de la vela que acaba de terminar con el cierre anterior y exige un movimiento de al menos un número fijo de unidades de precio, en la dirección que ya llevaba la vela previa. Cada lectura se toma con el mismo compás de cinco minutos, de modo que el precio de referencia, el filtro de dirección y la comprobación de la posición van siempre acompasados y una señal nunca se arma con valores que pertenecen a momentos distintos.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos ya cerradas del instrumento negociado son los únicos datos de mercado a los que se suscribe el diagrama, y todo lo que viene después funciona a ese compás.
- Previous value guarda la vela precedente completa en lugar de un único número, y dos convertidores extraen de ella su cierre y su apertura.
- Ese cierre previo es el precio de referencia: una fórmula le suma el paso de momentum y otra le resta el paso, lo que da un nivel de disparo para largos y un nivel de disparo para cortos en la barra actual.
- Un tercer convertidor lee el cierre de la vela que acaba de terminar, y dos comparaciones lo sitúan frente a los dos niveles.
- El filtro de dirección es la forma de la vela precedente: su cierre frente a su propia apertura, de modo que una barra alcista solo permite largos y una barra bajista solo cortos.
- Un bloque Position comparado con cero indica si la cuenta está plana, lo que mantiene al diagrama fuera de una operación ya abierta en vez de aumentarla.
- Dos condiciones lógicas reúnen dirección, momentum y posición plana, y cada una acciona un Position modify que abre a mercado con un volumen fijo y solo desde posición plana.
- Position protection toma las ejecuciones de entrada y el cierre actual y cierra la operación con un take-profit o un stop-loss de porcentaje fijo; el diagrama no lleva ninguna otra salida.

## Reglas de entrada y salida

- **Entrada en largo**: La vela precedente cerró por encima de su propia apertura, la vela que acaba de terminar cerró por encima del cierre previo más el paso de momentum y la posición está plana. Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: La vela precedente cerró por debajo de su propia apertura, la vela que acaba de terminar cerró por debajo del cierre previo menos ese mismo paso y la posición está plana. Position modify vende el volumen de la orden a mercado.
- **Salida**: No hay señal de salida en el diagrama. Desde el momento en que una entrada se ejecuta, la operación pertenece a Position protection, que la cierra con un 0.5% de beneficio o un 0.5% de pérdida respecto al precio de entrada. La protección se valora sobre el cierre de la vela, así que los niveles se comprueban una vez por barra, y una lectura contraria se ignora mientras hay una posición abierta: la siguiente entrada espera a que la posición vuelva a estar plana.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas sobre la que funciona todo el diagrama. |
| Momentum Step | 5 | Distancia desde el cierre previo, en unidades de precio, que la vela terminada tiene que superar para que se permita una entrada. |
| Order Volume | 0.01 | Tamaño de la orden, en lotes. |
| Take Profit, % | 0.5 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 0.5 | Distancia del stop-loss, en porcentaje del precio de entrada. |

## Detalles del diagrama

- Previous value está configurado para guardar una vela, no un número, y los campos se leen después mediante convertidores; eso es lo que hace que el precio de referencia y el filtro de dirección procedan de una misma barra.
- La condición de entrada está escrita como dos niveles de precio y no como una diferencia frente a un umbral, que es la misma aritmética vista desde el otro lado y permite que el gráfico muestre el nivel que produjo cada entrada.
- Ambos bloques de entrada abren solo desde posición plana, de modo que las señales repetidas dentro de una operación viva no cuestan nada y no se envía ninguna orden para dar la vuelta ni para piramidar.
- Cada constante se dispara con la serie de velas, así que publica su valor en cada barra; una constante que nunca se activara dejaría su comparación sin segundo operando y la condición no llegaría a armarse nunca.
- El paso de momentum es una distancia absoluta en las propias unidades del precio, no un porcentaje, así que hay que reescalarlo para un instrumento cotizado en otro orden de magnitud, mientras que el take-profit y el stop-loss son porcentajes y se trasladan sin cambios.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
