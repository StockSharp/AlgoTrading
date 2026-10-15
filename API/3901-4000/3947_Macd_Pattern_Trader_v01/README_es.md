# Estrategia MacdPatternTraderV01
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`MacdPatternTraderV01Strategy` es un fiel StockSharp puerto del asesor experto FORTRADER "MacdPatternTraderv01" MetaTrader 4. El sistema busca MACD patrones de gancho que aparecen después de que el oscilador se estira hasta un nivel extremo y luego retrocede hacia la línea cero. Cuando se forma un gancho bajista después de un pico de sobrecompra, la estrategia abre posiciones cortas, mientras que un gancho alcista después de una caída de sobreventa activa posiciones largas. La versión StockSharp conserva la gestión de riesgos original de múltiples capas, incluidos niveles recursivos de stop-loss y take-profit, así como escalado de posiciones por etapas.

La implementación de C# utiliza la suscripción de vela de alto nivel API con los indicadores `MACD`, `ExponentialMovingAverage` y `SimpleMovingAverage`. Todos los cálculos se realizan en velas terminadas, reflejando las llamadas `iMACD` y `iMA` con cambios de barra explícitos de la versión MQL. La lógica auxiliar adicional rastrea manualmente los máximos y mínimos recientes para reproducir las búsquedas de precios recursivas que EA utiliza para las órdenes de protección.

## Lógica de señal

1. **Condiciones de armado**
   - Una configuración *bajista* se activa una vez que la línea principal MACD excede `BearishThreshold`. La bandera de armado se borra tan pronto como MACD cruza por debajo de cero.
   - Una configuración *alcista* se activa una vez que la línea principal MACD cae por debajo de `BullishThreshold`. La bandera se borra cuando MACD se vuelve positivo.
2. **Confirmación de gancho**
   - Las entradas cortas requieren que `macd₀ < BearishThreshold`, `macd₀ < macd₁`, `macd₁ > macd₂`, la bandera bajista permanezca activa y `macd₂ < BearishThreshold` mientras `macd₀` se mantenga por encima de cero.
   - Las entradas largas requieren que `macd₀ > BullishThreshold`, `macd₀ > macd₁`, `macd₁ < macd₂`, la bandera alcista permanezca activa y `macd₂ > BullishThreshold` mientras que `macd₀` permanezca negativo.
3. **Ejecución de orden**
   - Cuando se completa el gancho, la estrategia envía una orden de mercado con el volumen `OrderVolume`. Almacena simultáneamente los precios de stop-loss y take-profit calculados para su posterior seguimiento.

## Gestión del riesgo

### Stop Loss

El stop-loss imita la función MQL `StopLoss(type)`:

- Las operaciones cortas buscan el máximo más alto en las últimas `StopLossBars` velas **excluyendo** la barra recién cerrada, luego agregan `OffsetPoints * PriceStep` al resultado.
- Las operaciones largas buscan el mínimo más bajo de las últimas `StopLossBars` velas históricas, restando la misma compensación.

Esta lógica se implementa con búsquedas extremas manuales en un búfer en memoria limitado (1000 valores) para evitar la creación de grandes colecciones personalizadas.

### Tomar ganancias

La toma de ganancias reproduce la rutina recursiva `TakeProfit(type)` MQL:

1. Comience con el bloque más reciente de valores `TakeProfitBars`. Incluya la vela que activó la señal.
2. Calcule el extremo (bajo para cortos, alto para largos) dentro de ese bloque.
3. Retroceda `TakeProfitBars` velas y repita mientras el nuevo bloque produce un extremo más favorable.
4. Deténgase en el primer bloque que **no** mejora el extremo y utilice el último valor registrado como obtención de ganancias.

### Gestión de posiciones parciales

- Después de la entrada, la estrategia registra el volumen original y el precio de entrada.
- Las salidas parciales se permiten sólo después de que el beneficio flotante expresado en la moneda de la cuenta supere `ProfitThreshold`.
- Para posiciones largas:
  1. Cierre un tercio del volumen inicial cuando el cierre de la vela supere el medio EMA (`EmaMediumPeriod`).
  2. Cierre la mitad de la posición restante cuando el máximo de la vela atraviese el promedio de los valores `SmaPeriod` y `EmaLongPeriod`.
- Para las posiciones cortas, las reglas se reflejan con el cierre de la vela por debajo del medio EMA y el mínimo de la vela por debajo del promedio compuesto.

Las órdenes de protección se verifican antes de escalar para garantizar que las paradas bruscas o los objetivos siempre tengan prioridad.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `StopLossBars` | 6 | Número de velas históricas para la búsqueda de swing de stop-loss. |
| `TakeProfitBars` | 20 | Tamaño de bloque utilizado por el algoritmo recursivo de obtención de beneficios. |
| `OffsetPoints` | 10 | Se agregaron puntos adicionales al precio del stop-loss. |
| `MacdFastPeriod` | 5 | Longitud rápida EMA del indicador MACD. |
| `MacdSlowPeriod` | 13 | Longitud lenta de EMA del indicador MACD. |
| `MacdSignalPeriod` | 1 | Longitud de la señal EMA del indicador MACD. |
| `BearishThreshold` | 0.0045 | Nivel positivo MACD que arma configuraciones cortas. |
| `BullishThreshold` | -0.0045 | Nivel negativo MACD que arma configuraciones largas. |
| `OrderVolume` | 1 | Volumen por orden de mercado. |
| `EmaShortPeriod` | 7 | EMA rápida utilizado en la primera salida parcial. |
| `EmaMediumPeriod` | 21 | Medio EMA utilizado en filtros y salidas parciales. |
| `SmaPeriod` | 98 | SMA utilizado dentro del promedio de salida compuesto. |
| `EmaLongPeriod` | 365 | EMA largo combinado con el SMA para la segunda salida parcial. |
| `ProfitThreshold` | 5 | Beneficio flotante mínimo (en unidades monetarias) antes del escalamiento horizontal. |
| `CandleType` | plazo de 1 hora | Serie de velas procesadas por la estrategia. |

Todos los parámetros están expuestos a través de `StrategyParam<T>` y admiten la optimización cuando corresponda.

## Notas de implementación

- La estrategia se basa exclusivamente en enlaces `SubscribeCandles` de alto nivel. No inserta indicadores en la colección `Indicators`, siguiendo las pautas del proyecto.
- El historial de MACD se almacena utilizando un registro de desplazamiento compacto de tres valores (`_macdPrev1..3`) para imitar el acceso a `iMACD(..., shift)`.
- Los niveles de precios protectores se registran como decimales; cuando las velas alcanzan un tope o un objetivo, la estrategia cierra toda la posición con órdenes de mercado y reinicia la máquina de estado interna.
- El PnL flotante se estima utilizando `PriceStep`/`StepPrice` para que el umbral de salida parcial permanezca constante independientemente de la escala de precios del instrumento.
- Los buffers de velas para máximos y mínimos tienen un límite de 1000 elementos, lo que es suficiente para los parámetros predeterminados pero evita un crecimiento descontrolado.

## Uso

1. Cree una instancia de `MacdPatternTraderV01Strategy`, asigne la seguridad, la cartera y el conector deseados.
2. Opcionalmente, ajuste parámetros como `CandleType`, `StopLossBars` o `OrderVolume` para adaptarlos al instrumento negociado.
3. Iniciar la estrategia; se suscribirá a la serie de velas configurada, dibujará MACD e intercambiará marcadores en el gráfico y administrará las órdenes automáticamente.

La estrategia contiene extensos comentarios en línea que describen cada bloque traducido para facilitar el mantenimiento y una mayor personalización.
