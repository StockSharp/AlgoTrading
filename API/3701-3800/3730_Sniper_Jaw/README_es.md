# Estrategia de mandíbula de francotirador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Sniper Jaw** transfiere el asesor experto MetaTrader 4 `SniperJawEA.mq4` a la estrategia de alto nivel de StockSharp API. El sistema analiza el indicador Bill Williams' Alligator en el precio medio de la vela. Una operación sólo se inicia cuando las tres medias móviles suavizadas (mandíbula, dientes y labios) se apilan en estricto orden alcista o bajista y todas avanzan en la misma dirección en comparación con la vela finalizada anterior.

## Lógica de trading

1. **Alligator reconstrucción**: tres `SmoothedMovingAverage` instancias calculan la mandíbula, los dientes y los labios en la mediana de la vela `(High + Low) / 2`. Cada línea se puede avanzar su propio número de barras para reflejar el trazado de MetaTrader.
2. **Confirmación de tendencia**: se produce un sesgo largo cuando los valores desplazados satisfacen `jaw < teeth < lips` **y** cada línea es más alta que en la vela anterior. Se necesita un sesgo corto `jaw > teeth > lips` con las tres líneas moviéndose hacia abajo en comparación con la barra anterior.
3. **Gestión de entradas**: la estrategia abre solo una posición a la vez. Cuando `UseEntryToExit` está habilitado y se activa una nueva señal opuesta, la exposición actual se aplana primero y la nueva orden se envía en la siguiente señal.
4. **Salidas de protección**: las distancias de stop-loss y take-profit se definen en pips y se convierten utilizando el valor `PriceStep`. Tanto las posiciones largas como las cortas se supervisan en cada vela terminada y se cierran una vez que se alcanza cualquiera de los umbrales.
5. **Limitación de señal**: el EA original evitó entradas duplicadas al verificar la marca de tiempo de la barra. El puerto almacena el tiempo de la última vela de señal y omite órdenes adicionales durante la misma barra.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Tamaño comercial en lotes o contratos pasados a `BuyMarket`/`SellMarket`. |
| `EnableTrading` | `true` | Switch maestro que permite deshabilitar nuevas entradas manteniendo activa la gestión de riesgos. |
| `UseEntryToExit` | `true` | Cierra una posición existente antes de armar una señal opuesta. Refleja el indicador "Entrada a Salida" del EA. |
| `StopLossPips` | `20` | Distancia del tope de protección al precio de entrada. Cero desactiva la parada. |
| `TakeProfitPips` | `50` | Distancia del objetivo de beneficio al precio de entrada. Cero desactiva el objetivo. |
| `MinimumBars` | `60` | Número requerido de velas terminadas antes de que se evalúe la primera señal. |
| `JawPeriod` / `TeethPeriod` / `LipsPeriod` | `13 / 8 / 5` | Longitud de las medias móviles suavizadas que forman las líneas Alligator. |
| `JawShift` / `TeethShift` / `LipsShift` | `8 / 5 / 3` | Desplazamiento hacia adelante (en barras) utilizado para alinear los buffers Alligator con la versión MetaTrader. |
| `CandleType` | `1 hour time frame` | Suscripción a la serie de velas primarias. Ajústelo para que coincida con el gráfico utilizado en MetaTrader. |

## Notas de uso

- La implementación solo evalúa velas terminadas (`CandleStates.Finished`) para evitar valores parcialmente formados.
- Los niveles de parada y objetivo se rastrean internamente; la estrategia emite órdenes de mercado para aplanar la posición cuando se viola un nivel.
- La conversión de pasos de precio sigue la convención común de Forex: los símbolos de 5 y 3 decimales tratan un pip como diez pasos de precio.
- Agregue la estrategia a un esquema junto con un conector, una cartera y una configuración de seguridad. Después de iniciar la estrategia, el panel del gráfico mostrará la serie de velas y las líneas Alligator reconstruidas para una validación visual rápida.
