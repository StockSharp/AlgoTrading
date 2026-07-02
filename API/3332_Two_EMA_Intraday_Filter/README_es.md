# Dos estrategias de filtro intradiario EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el MetaTrader Asesor Experto **Expert_2EMA_ITF** utilizando el StockSharp de alto nivel API. Opera en el cruce de dos promedios móviles exponenciales y utiliza el rango verdadero promedio (ATR) para establecer órdenes límite pendientes, paradas protectoras y objetivos. Un filtro de tiempo intradiario adicional bloquea las entradas durante los minutos, horas o días de la semana no deseados.

## Resumen de la lógica
- Calcule los valores EMA rápido y lento en la serie de velas seleccionada.
- Detecte un cruce alcista cuando el EMA rápido sube por encima del EMA lento y un cruce bajista cuando cae por debajo.
- En un cruce alcista, coloque una orden de límite de compra compensada del lento EMA por `LimitMultiplier * ATR` más el diferencial actual. En un cruce bajista, coloque una orden de límite de venta compensada en la dirección opuesta.
- Almacene los precios de stop-loss y take-profit utilizando multiplicadores ATR para que puedan enviarse inmediatamente una vez que se complete la orden de entrada.
- Cancele las órdenes pendientes automáticamente si permanecen sin completar durante más de `ExpirationBars` velas.
- Saltar señales que no pasen el filtro intradiario (verificaciones permitidas de minutos, horas y días). Las máscaras de bits pueden desactivar varios minutos, horas o días simultáneamente.

## Indicadores
- **Rápido EMA**: controla la sensibilidad de la detección de cruce.
- **Lento EMA** – define la dirección de la tendencia.
- **Rango verdadero promedio (ATR)**: mide la volatilidad del mercado y escala las compensaciones de precios de entrada/salida.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Marco de tiempo utilizado para los cálculos. | velas de 30 minutos |
| `FastEmaPeriod` | Período del ayuno EMA. | 5 |
| `SlowEmaPeriod` | Período del EMA lento (debe ser mayor que el período rápido). | 30 |
| `AtrPeriod` | ATR período de cálculo. | 7 |
| `LimitMultiplier` | ATR multiplicador que desplaza los precios límite de entrada. | 1.2 |
| `StopLossMultiplier` | ATR multiplicador para la colocación de stop-loss. | 5 |
| `TakeProfitMultiplier` | ATR multiplicador para colocación de obtención de beneficios. | 8 |
| `ExpirationBars` | Número de barras tras las cuales se cancelan las órdenes no ejecutadas. | 4 |
| `GoodMinuteOfHour` | Minuto específico permitido para inscripciones (-1 inhabilitaciones). | -1 |
| `BadMinutesMask` | Minutos de bloqueo de máscara de bits (el bit *n* bloquea el minuto *n*). | 0 |
| `GoodHourOfDay` | Hora específica permitida para las inscripciones (-1 inhabilitaciones). | -1 |
| `BadHoursMask` | Horas de bloqueo de máscara de bits (el bit *n* bloquea la hora *n*). | 0 |
| `GoodDayOfWeek` | Día específico permitido para entradas (-1 desactiva, 0 = domingo). | -1 |
| `BadDaysMask` | Días de bloqueo de máscara de bits (bit *n* bloquea el día *n*, 0 = domingo). | 0 |

## Gestión de órdenes
1. **Órdenes de entrada**: las órdenes limitadas se registran con un precio desplazado del lento EMA por la compensación basada en ATR. La orden de compra también agrega el diferencial actual si hay cotizaciones de oferta/demanda disponibles.
2. **Vencimiento**: cada orden pendiente almacena el índice de vela cuando se creó. Si `ExpirationBars` es positivo y la orden sobrevive más allá de esa cantidad de barras, se cancela automáticamente.
3. **Órdenes de protección**: cuando una orden de entrada completa la estrategia, cancela cualquier orden de parada/objetivo anterior, luego coloca inmediatamente un límite de pérdidas y una toma de ganancias calculadas a partir de la instantánea ATR que generó la señal. Las órdenes de protección opuestas se cancelan cuando la posición es plana.

## Detalles del filtro intradía
- **Valores permitidos únicos**: `GoodMinuteOfHour`, `GoodHourOfDay` y `GoodDayOfWeek` restringen las operaciones a un minuto, hora o día de la semana específicos cuando no son negativos.
- **Máscaras de bits**: `BadMinutesMask`, `BadHoursMask` y `BadDaysMask` contienen bits que desactivan varios intervalos de tiempo a la vez. Por ejemplo, configurar `BadMinutesMask = (1 << 0) | (1 << 30)` bloquea el comercio durante el minuto 0 y el minuto 30 de cada hora.
- **Lógica combinada**: solo se permite una entrada cuando el tiempo de vela actual supera todas las condiciones permitidas y ninguna de las máscaras la bloquea.

## Diferencias frente al Expert Advisor original
- La versión StockSharp utiliza órdenes de límite pendientes combinadas con registros explícitos de stop-loss y take-profit una vez que se ejecuta la entrada, reflejando los cálculos de la señal MQL.
- La compensación del diferencial para las órdenes de compra utiliza las cotizaciones `Security.BestBid/BestAsk` actuales cuando están disponibles; de lo contrario, la compensación es cero.
- El filtrado de tiempo se expresa mediante máscaras de bits y comparaciones directas en lugar de MetaTrader clases auxiliares de filtro de tiempo específicas.
- Todas las acciones comerciales aprovechan StockSharp ayudantes de alto nivel (`BuyLimit`, `SellLimit`, `SellStop`, `BuyStop`) y la lógica de cancelación automática en lugar de matrices de órdenes manuales.

## Notas de uso
- Asegúrese de que el volumen de la estrategia esté establecido antes de comenzar la estrategia; en caso contrario se produce un aviso y no se envían órdenes.
- Para escenarios de optimización, los metadatos de los parámetros ya permiten el ajuste de EMA períodos, ATR período, multiplicadores y duración de vencimiento.
- La estrategia supone que los tiempos de cierre de las velas representan el final de la barra y los utiliza al evaluar los filtros intradiarios.
