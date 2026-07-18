# MACD Señal ATR Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de señal **MACD** transfiere el experto MetaTrader `MACD_signal.mq4` a StockSharp. El robot original midió la
MACD histograma contra una banda de volatilidad basada en ATR y abrió una orden de mercado única cada vez que el histograma cruzó esa
banda. Esta versión de C# recrea la misma lógica de ruptura de impulso utilizando el nivel alto API de StockSharp, almacena el anterior
histograma y lecturas ATR explícitamente, y documenta cada regla de administración de dinero con parámetros nombrados y en inglés.
comentarios en el código fuente.

A diferencia de la implementación MetaTrader que modificaba los tickets directamente, el puerto StockSharp funciona con posiciones netas. eso
por lo tanto, cierra la exposición actual antes de cambiar de dirección y actualiza las paradas dinámicas internamente en lugar de depender de
llamadas `OrderModify` del lado del corredor.

## Lógica comercial
1. Suscríbase a la serie de velas configuradas (`CandleType`) y procese **solo** velas terminadas para evitar barras parciales.
ruido.
2. Alimente un indicador `MovingAverageConvergenceDivergenceSignal` con las longitudes de señal rápida, lenta y EMA elegidas. el
El valor del histograma (`MACD - signal`) se almacena cada vez que se cierra una barra.
3. Calcula el `AverageTrueRange` en las mismas velas. El valor de la barra **anterior** se multiplica por
`ThresholdMultiplier` para recrear el umbral `rr = ATR * LEVEL` de MQL.
4. Detectar una ruptura alcista cuando el histograma actual excede `+threshold` mientras el histograma anterior aún estaba por debajo
eso. Si la cuenta es plana o corta y se permiten operaciones largas en `Direction`, envíe una orden de compra de mercado de tamaño
`TradeVolume`.
5. Detecte una ruptura bajista cuando el histograma cruce por debajo de `-threshold` después de estar por encima en la vela anterior. si
la estrategia es plana o larga y la negociación corta está habilitada, emita una orden de venta de mercado de tamaño `TradeVolume`.
6. Gestiona las posiciones abiertas en cada barra:
   - cerrar posiciones largas tan pronto como el histograma se vuelva negativo; cerrar cortos cuando se vuelva positivo;
   - monitorear la distancia fija de toma de ganancias (`TakeProfitPoints`) contra los máximos o mínimos de las velas para emular el original
MetaTrader parámetro de obtención de beneficios;
   - actualizar las paradas de seguimiento una vez que el precio se aleja más de `TrailingStopPoints` de la entrada y salir si la vela vuelve a visitarse
el nivel final. El stop largo sigue al cierre como indicador del precio de oferta, mientras que el stop corto sigue al cierre como
un proxy para el precio de venta.
7. El EA se niega a operar cuando `TakeProfitPoints` está por debajo del mínimo histórico de 10 puntos, igualando el control de protección
presente en el código MQL.

## Gestión de riesgos
- **Orden única a la vez.** La estrategia siempre se mantiene estable antes de abrir una nueva posición, reflejando la original.
`OrdersTotal() < 1` requisito.
- **Volumen fijo.** `TradeVolume` reemplaza la entrada `Lots` y también se copia en `Strategy.Volume` para que se utilicen las acciones manuales de la interfaz de usuario.
el mismo tamaño.
- **Obtención de ganancias fija.** `TakeProfitPoints` convierte la distancia del punto MQL al tamaño del tick del instrumento usando
`Security.PriceStep`.
- **Salida basada en indicadores.** Un cambio de signo en el histograma desencadena una salida inmediata del mercado, lo que garantiza que el EA no permanezca dentro.
el mercado cuando el impulso se revierte.
- **Trailing stop.** Una vez que el precio se mueve a favor de la operación en más pasos que el número configurado, se retira el stop.
dentro de la zona de ganancias y sigue el precio de cierre sin retroceder nunca.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `10` | Tamaño de orden (lotes) utilizado para cada entrada al mercado y copiado en `Strategy.Volume`. |
| `TakeProfitPoints` | `int` | `10` | Distancia al objetivo fijo de obtención de beneficios expresada en incrementos de precio. Valores inferiores a 10
desactivar el comercio. |
| `TrailingStopPoints` | `int` | `25` | Distancia en pasos de precio para el trailing stop. Establezca en `0` para deshabilitar el seguimiento. |
| `FastPeriod` | `int` | `9` | Longitud de la EMA rápida dentro del indicador MACD. |
| `SlowPeriod` | `int` | `15` | Longitud del EMA lento dentro del indicador MACD. |
| `SignalPeriod` | `int` | `8` | Longitud del EMA utilizado para suavizar la línea de señal MACD. |
| `ThresholdMultiplier` | `decimal` | `0.004` | Multiplicador aplicado a la barra anterior ATR para construir la banda de ruptura. |
| `AtrPeriod` | `int` | `200` | Número de velas utilizadas para calcular el filtro de volatilidad ATR. |
| `CandleType` | `DataType` | plazo de 30 minutos | Plazo primario procesado por la estrategia. |

## Diferencias con el asesor experto original
- MetaTrader expone a `AccountFreeMargin()` y se niega a negociar si el valor es demasiado pequeño. StockSharp estrategias no
tienen la misma instantánea de margen, por lo que el puerto omite esa verificación. Los controles de riesgo a nivel de cartera deben manejarse fuera del
estrategia cuando sea necesario.
- La versión MQL ajustó las órdenes de parada con `OrderModify`. StockSharp trabaja con posiciones netas, por lo que gestiona la conversión
sale internamente monitoreando los máximos/mínimos de las velas y las variables del trailing stop.
- MetaTrader contó las "barras" manualmente e imprimió una advertencia cuando había menos de 100 velas disponibles. StockSharp depende de
preparación del indicador (`BindEx`) para que la estrategia permanezca inactiva automáticamente hasta que MACD y ATR tengan suficientes datos.
- El puerto almacena explícitamente los valores de histograma y ATR anteriores para reproducir la comparación de umbral `Delta`/`Delta1`
sin violar la regla de StockSharp contra la indexación aleatoria de indicadores.

## Consejos de uso
- Mantenga `Security.PriceStep`, `Security.MinVolume` y `Security.VolumeStep` precisos para aumentar el volumen de conversiones y obtener ganancias.
Los cálculos permanecen alineados con el intercambio.
- Aumente `ThresholdMultiplier` o `AtrPeriod` cuando la estrategia opere con demasiada frecuencia en mercados agitados; disminuirlos a
hacer que el sistema sea más sensible a los brotes de volatilidad.
- Menor `TradeVolume` cuando se ejecuta en instrumentos apalancados o de alta volatilidad, porque el script original suponía grandes
tamaños de lote en símbolos Forex.
- Combine la estrategia con filtros de períodos de tiempo más altos a través de la propiedad incorporada `Direction` si solo desea permitir
posiciones largas o cortas durante regímenes de mercado específicos.
