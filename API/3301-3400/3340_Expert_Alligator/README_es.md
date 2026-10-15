# Estrategia experta Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Experta Alligator** es una fiel versión StockSharp del asesor experto integrado de MetaTrader 5 `Expert_Alligator.mq5`. El experto original toma sus decisiones comerciales a partir del indicador Bill Williams' Alligator, que consta de tres promedios móviles suavizados desplazados hacia el futuro: la mandíbula (azul), los dientes (rojo) y los labios (verde). Al monitorear cómo estas líneas se contraen y expanden, el EA identifica nuevos cruces y espera a que se abra la "boca" antes de poder realizar otra operación. Esta conversión de C# recrea el mismo flujo de trabajo con la estrategia de alto nivel API y el conjunto de indicadores de StockSharp.

## Lógica comercial

1. **Preparación de indicadores**
   - Construya tres promedios móviles suavizados del precio medio utilizando las longitudes clásicas de Alligator (13, 8 y 5) y aplique los desplazamientos hacia adelante estándar (8, 5 y 3 barras respectivamente).
   - Almacene un historial continuo de cada línea desplazada para que las compensaciones pasadas y futuras utilizadas por el MetaTrader EA (por ejemplo, `LipsTeethDiff(-2)`) puedan evaluarse de forma segura.

2. **Condiciones de entrada**
   - *Operaciones largas*: se activan cuando las extensiones labios-dientes y dientes-mandíbula se han reducido durante tres barras desplazadas consecutivas mientras permanecen por encima de cero. Esto reproduce el requisito del EA de que la línea verde cruce hacia abajo a través de la roja, confirmando una apertura de la boca hacia arriba.
   - *Operaciones cortas*: refleja la lógica larga con diferenciales que se ajustan por debajo de cero, lo que indica que los labios se cruzan hacia arriba a través de los dientes y la mandíbula.
   - Después de abrir una operación, la estrategia activa una bandera interna `crossed` que bloquea entradas adicionales hasta que los tres diferenciales Alligator se amplíen al menos en la distancia **Medida cruzada** configurada.

3. **Condiciones de salida**
   - Las *posiciones largas* se cierran cuando la extensión de labios y dientes se vuelve negativa en el valor desplazado más reciente mientras permanece positiva en los dos valores más antiguos (índices `-1`, `0`, `1` en el EA original).
   - Las *posiciones cortas* salen cuando ocurre la misma secuencia en la dirección opuesta.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `Order Volume` | Tamaño comercial en lotes o contratos pasados a `BuyMarket`/`SellMarket`. | `0.1` |
| `Candle Type` | Plazo de suscripción de la vela. | `1 Hour` |
| `Jaw Period` | Longitud media móvil suavizada para la línea de la mandíbula. | `13` |
| `Jaw Shift` | Desplazamiento hacia adelante (en barras) de la línea de la mandíbula. | `8` |
| `Teeth Period` | Longitud media móvil suavizada para la línea de dientes. | `8` |
| `Teeth Shift` | Desplazamiento hacia adelante (en barras) de la línea de dientes. | `5` |
| `Lips Period` | Longitud media móvil suavizada para la línea de los labios. | `5` |
| `Lips Shift` | Desplazamiento hacia adelante (en barras) de la línea de los labios. | `3` |
| `Cross Measure` | Spread mínimo (en MetaTrader puntos) que debe desarrollarse después de un cruce antes de que se pueda activar otra operación. | `5` |

## Notas de implementación

- La estrategia calcula el precio medio `(High + Low) / 2` de cada vela terminada y lo introduce en tres `SmoothedMovingAverage` instancias.
- Los historiales desplazados se implementan con matrices de tamaño fijo para reflejar la forma en que MetaTrader expone índices futuros como `-1` o `-2` una vez que las líneas Alligator se desplazan hacia adelante.
- El valor MetaTrader `_Point` se emula a través del `PriceStep` del símbolo. Cuando este último no está disponible, el código vuelve a ser `10^-Decimals` o `0.0001`.
- El resultado del gráfico coincide con el EA al trazar la mandíbula, los dientes y los labios en el panel de velas principal, lo que permite una validación visual rápida.

## Uso

1. Adjunte la estrategia a un `Connector` con un valor que proporcione el tipo de vela deseado (velas predeterminadas de una hora).
2. Llame a `Start()` una vez que el flujo de datos del mercado esté listo.
3. Opcional: ajuste las longitudes, los turnos o el umbral de medida cruzada de Alligator para probar comportamientos personalizados.
4. Supervise las posiciones y el rendimiento a través de las interfaces estándar StockSharp.

No se requieren paradas finales ni módulos de administración de dinero adicionales porque el EA original utiliza un tamaño de lote fijo y se basa únicamente en la geometría de línea Alligator para la gestión comercial.
