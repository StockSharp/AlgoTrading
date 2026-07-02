# Estrategia simple de giro de pivote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto C# de alto nivel del MetaTrader 4 Expert Advisor almacenado en `MQL/7610/Simplepivot_www_forex-instruments_info.mq4`. El programa original compara el precio de apertura de cada nueva vela con el rango de velas anterior y alterna entre posiciones de mercado largas y cortas. La versión StockSharp mantiene el mismo comportamiento al confiar exclusivamente en ayudantes de alto nivel como `SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket` y `ClosePosition`.

La lógica convertida:

1. Espera a que una vela terminada obtenga los valores de apertura, máximo y mínimo.
2. Utiliza el rango de velas anterior para construir un pivote simple en el punto medio.
3. Abre una nueva posición larga cuando la vela actual se abre en la mitad inferior del rango o se abre por encima del máximo anterior.
4. Abre una nueva posición corta cuando la vela actual se abre en la mitad superior del rango.
5. Siempre cierra la posición existente antes de ingresar en la dirección opuesta, replicando el comportamiento de boleto único de la versión MQL.

No se implementan niveles de stop-loss o take-profit en el Asesor Experto original, por lo que la posición se invierte sólo cuando una nueva vela dicta una dirección diferente.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `OrderVolume` | 1 | Volumen de orden de mercado utilizado al entrar en una posición. |
| `CandleType` | marco de tiempo de 1 minuto | Tipo de vela solicitado desde la fuente de datos. |

## Detalles de la lógica comercial
1. La primera vela terminada se almacena y se utiliza como referencia para la siguiente decisión. No se envía ningún pedido hasta que no haya una vela completa para analizar.
2. Por cada vela completa posterior:
   - Calcula `pivot = (previousHigh + previousLow) / 2`.
   - Si `Open < previousHigh` **y** `Open > pivot`, la estrategia prepara una entrada corta.
   - De lo contrario, prepara una entrada larga (esto cubre las aperturas en la mitad inferior, las aperturas iguales al pivote y cualquier hueco por encima del máximo anterior o por debajo del mínimo anterior).
3. Si la estrategia ya mantiene una posición en la dirección elegida, la señal se ignora para evitar pagar el diferencial dos veces, reflejando el rendimiento anticipado que se encuentra en el código MQL.
4. Si la dirección cambia, la posición actual se cierra mediante `ClosePosition()` y se envía una nueva orden de mercado mediante `OrderVolume`.
5. El buffer máximo/bajo anterior se actualiza con la última vela completada para impulsar la siguiente decisión.

## Gestión del riesgo
El algoritmo convertido no incluye paradas ni objetivos de ganancias. El tamaño de la posición está controlado únicamente por el parámetro `OrderVolume`, por lo que el riesgo debe gestionarse externamente (por ejemplo, ajustando el volumen o combinando la estrategia con protecciones a nivel de cuenta).

## Visualización
Cuando hay un área del gráfico disponible, la estrategia traza las velas solicitadas y las operaciones ejecutadas, lo que ayuda a validar los cambios de pivote durante las pruebas retrospectivas.
