# Diagrama de la estrategia de dirección de barras emparejadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos instrumentos, una barra. El diagrama lee la dirección de la misma vela de cinco minutos en el instrumento operado y en un segundo instrumento de referencia, y compra solo cuando ambas discrepan: la barra de referencia cerró al alza mientras que la barra operada cerró a la baja. La posición se devuelve en cuanto el precio cierra por encima del máximo de la barra anterior.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de velas alimentan el diagrama. El operado funciona sobre el propio instrumento de la estrategia; el de referencia apunta a un instrumento concreto mediante una variable Security, de modo que el par es un ajuste y no una decisión de cableado.
- Ambas series están configuradas para usar solo velas finalizadas, así que una vela sin terminar nunca puede alterar la decisión.
- Sync mantiene una línea por instrumento y deja salir ambas velas juntas al ritmo de cinco minutos. Los dos flujos llegan de forma independiente, y solo después de esa retención las dos velas pertenecen a la misma barra, que es lo único que hace que compararlas tenga sentido.
- Los conversores extraen la apertura y el cierre de cada vela liberada, reduciendo cada instrumento a los dos números que dicen hacia dónde fue su barra.
- Dos comparaciones leen esos números: el cierre de referencia por encima de la apertura de referencia significa que la barra de referencia cerró al alza; el cierre operado por debajo de la apertura operada significa que la barra operada cerró a la baja.
- Previous value guarda la vela operada un paso atrás y un conversor lee el máximo de ella. El bloque retiene la vela en sí y el campo se lee después, que es el orden que da un valor en cada barra.
- La posición se ancla a la barra mediante una variable que emite con el disparo de la vela y luego se compara con cero dos veces: igual o menor que cero admite una entrada, mayor que cero admite una salida.
- Dos puertas lógicas AND accionan dos bloques Position modify —uno abre con la condición Open position, el otro cierra con Close position— y el panel del gráfico muestra las velas operadas, el nivel de salida, las órdenes y las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: En una barra liberada, el instrumento de referencia cerró por encima de su propia apertura, el instrumento operado cerró por debajo de la suya y la posición es igual o menor que cero. La puerta AND se activa y Position modify compra el volumen de la orden a mercado con la condición Open position, de modo que una barra que repite el patrón mientras la posición ya está abierta no añade nada.
- **Entrada en corto**: No hay lado corto. El diagrama es solo largo: una barra bajista en el instrumento operado se lee como el descuento con el que comprar, nunca como un motivo para vender.
- **Salida**: Mientras la posición es mayor que cero, una barra liberada cuyo cierre está por encima del máximo de la barra anterior activa la puerta de salida, y el segundo Position modify cierra a mercado con la condición Close position. No hay stop-loss ni take-profit: el máximo de la barra anterior es toda la regla de salida y, como el bloque cierra lo que está abierto en lugar de vender un tamaño fijo, una salida no puede dar la vuelta a la posición hacia corto.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | El instrumento cuya dirección de barra confirma la entrada; debe estar disponible desde la conexión junto con el operado. |
| Reference Candles | 00:05:00 | Marco temporal de las velas de referencia. |
| Traded Candles | 00:05:00 | Marco temporal de las velas operadas. |
| Sync Interval | 00:05:00 | El ritmo con el que Sync libera ambos instrumentos; manténlo igual al marco temporal de las velas, de lo contrario el par liberado no es la barra que asumen las comparaciones. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |

## Detalles del diagrama

- Ambos bloques Position modify están configurados para no requerir una conexión en línea, de modo que el mismo diagrama se comporta igual sobre histórico que sobre un flujo en vivo.
- La entrada lleva la condición Open position a propósito. Sin ella, el bloque actuaría ante cada cambio de posición con el que se dispare, y una sola señal se convertiría en un flujo de órdenes.
- Cada constante del diagrama —el cero y el volumen de la orden— se dispara con la vela liberada. Una variable emite con su disparo, no con su propio valor, así que una constante sin disparar dejaría mudas las comparaciones contiguas durante toda la ejecución.
- Ambos extremos de cada línea de Sync están enlazados. Un valor enviado al bloque y nunca retirado lo deja esperando una línea que nunca se cierra, y la estrategia no llegaría a arrancar.
- El nivel de salida se dibuja en el gráfico como su propia línea, de modo que la barra que cierra por encima de él puede leerse directamente en el panel, junto a la ejecución que vino después.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
