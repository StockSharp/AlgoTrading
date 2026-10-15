# Estrategia de cuero cabelludo de VirtPO TestBed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia transfiere el asesor experto **VirtPOTestBed_ScalpM1** MetaTrader 4 al StockSharp nivel alto API. Mantiene la idea original de crear *órdenes pendientes virtuales* que se arman mediante cruces de osciladores Stochastic y se ejecutan una vez que el impulso del precio confirma el movimiento. Todos los filtros, reglas de administración de dinero y controles de programación de la versión MQL se replicaron con indicadores y métodos de pedido StockSharp.

## Lógica principal

1. **Órdenes pendientes virtuales**: cuando no hay ninguna posición abierta, la estrategia verifica el bloque de filtro en cada vela completada:
   * El diferencial debe permanecer por debajo de `SpreadMaxPips` (mejor oferta/demanda obtenida del Nivel 1).
   * El volumen promedio de ticks en las últimas tres barras debe exceder `VolumeLimit`.
   * La volatilidad absoluta del precio (tamaño corporal promedio para `VolatilityPeriod` barras) debe estar por encima de `VolatilityLimit`.
   * El ancho de banda Bollinger (período `BollingerPeriod`, ancho 2) debe permanecer entre `BollingerLowerLimit` y `BollingerUpperLimit`.
   * El horario de negociación debe estar dentro de la ventana configurada (`EntryHour` + `OpenHours`) y fuera de los días laborables deshabilitados (`Day1`, `Day2`, hora límite del viernes).
   * Filtro de tendencia SMA: la diferencia entre el rápido ({PH001}}) y el lento (`SmaSlowPeriod`) SMA en pips debe exceder `SmaDifferencePips` en cualquier dirección.
   * El cuerpo de la barra anterior debe ser más pequeño que `LastBarLimitPips` para evitar perseguir velas largas.

Si los filtros tienen éxito, se evalúan Stochastic cruces:
   * Un cruce alcista a través de `StochasticSetLevel` genera una **parada de compra virtual** por encima de la oferta de `PoThresholdPips`.
   * Un cruce bajista a través de `100 - StochasticSetLevel` genera una **parada de venta virtual** por debajo de la oferta en el mismo umbral.
Cada orden pendiente virtual recuerda su vencimiento (`PoTimeLimitMinutes`) y las distancias stop-loss/take-profit tomadas desde `StopLossPips` y `TakeProfitPips`.

2. **Fase de ejecución**: cuando `TickLevel` está habilitado, la estrategia escucha las operaciones entrantes para ejecutar órdenes virtuales tan pronto como el último precio supere el umbral. Si `TickLevel` está deshabilitado, la verificación del activador se ejecuta al cierre de cada vela terminada. Una vez que el precio cruza el tope virtual, se envía una orden de mercado y se borra la orden virtual.

3. **Gestión de riesgos** – Después de completar las pistas de estrategia:
   * Niveles iniciales de stop-loss y take-profit medidos en pips desde el precio de entrada.
   * Trailing stop opcional (`TrailingStopPips`) que sigue el precio extremo desde la entrada.
   * Tiempo máximo de espera (`CloseTimeMinutes`). Dependiendo de `ProfitType` puede cerrar todas las posiciones (0), solo las rentables (1) o solo las perdedoras (2) cuando expire el cronómetro.

Todas las distancias de precios se convierten de pips utilizando el valor `PriceStep` y el multiplicador de dígitos, reproduciendo el manejo del corredor de cinco dígitos en la implementación MQL. El valor predeterminado `OrderVolume` se aplica a cada orden de mercado. La estrategia restablece automáticamente su estado interno cuando las posiciones se aplanan.

## Notas importantes

* Se requieren datos de nivel 1 para calcular los diferenciales y activar los niveles con precisión. Sin actualizaciones de la mejor oferta/demanda, los filtros bloquearán el comercio.
* La ejecución a nivel de tick refleja el indicador `TickLevel` del EA original; cuando está deshabilitada, la ejecución espera a que se cierre la vela, lo cual es más conservador pero más fácil de realizar una prueba retrospectiva.
* La estrategia solo mantiene una única posición neta, al igual que la versión MQL que restringía el número de órdenes de mercado activas.

## Parámetros

| grupo | Nombre | Descripción |
| --- | --- | --- |
| generales | Tipo de vela | Marco de tiempo utilizado para la suscripción de velas (predeterminado: 1 minuto). |
| Ejecución | Nivel de marca | Utilice ticks comerciales para ejecutar órdenes virtuales de inmediato. |
| Ejecución | Umbral de PO (pips) | Distancia en pips entre el precio de oferta y el nivel de stop virtual. |
| Ejecución | Vida útil de la orden de compra (min) | Tiempo de vencimiento de cada orden virtual pendiente. |
| Filtros | Spread máximo (pips) | Spread máximo permitido antes de armar órdenes. |
| Filtros | Límite de volumen | Volumen de ticks promedio mínimo en las últimas tres barras. |
| Filtros | Período de volatilidad | Número de barras utilizadas para promediar los cuerpos absolutos de las velas. |
| Filtros | Límite de volatilidad | Tamaño medio mínimo del cuerpo de la vela (en pips). |
| Filtros | Bollinger Período | Bollinger periodo de cálculo de banda. |
| Filtros | Bollinger Inferior / Superior | Rango de ancho de banda permitido en pips. |
| Filtros | Límite de la última barra | Tamaño máximo del cuerpo de la vela anterior en pips. |
| Tendencia | Rápido SMA / Lento SMA | Períodos para el filtro de tendencia de media móvil. |
| Tendencia | SMA Diferencia | Distancia mínima SMA en pips para confirmar una tendencia. |
| Stochastic | %K / %D / Suave | Períodos estándar del oscilador Stochastic. |
| Stochastic | Stochastic Establecer | Nivel utilizado para armar órdenes pendientes virtuales. |
| Stochastic | Stochastic Ir | Umbral utilizado para ejecutar la orden de armado. |
| Comercio | Volumen de pedido | Volumen de orden de mercado base. |
| Riesgo | Tomar ganancias / Stop Loss / Trailing Stop | Distancias de salida en pips. |
| Horario | Días de desactivación, primer/segundo día sin comercio | Filtros de días laborables (use 99 para desactivarlos). |
| Horario | Hora de entrada / Horario de apertura | Inicio y duración de la ventana de negociación. |
| Horario | Corte del viernes | Hora tras la cual se detiene la negociación del viernes. |
| Riesgo | Vida útil máxima | Salida basada en tiempo en minutos (establezca ≥5000 para desactivar). |
| Riesgo | Filtro de ganancias | 0 – cerrar independientemente, 1 – cerrar solo los ganadores, 2 – cerrar solo los perdedores cuando se dispara el cronómetro. |

## Diferencias con el original EA

* La clase auxiliar MQL `CPO` se reemplaza con variables de estado internas que llaman a `BuyMarket`/`SellMarket` directamente una vez que el precio cruza el nivel virtual.
* La ejecución de stop-loss y take-profit utiliza máximos/mínimos de velas (para pruebas retrospectivas) o actualizaciones de ticks cuando estén disponibles. No se admiten rellenos parciales ni posiciones cubiertas del entorno MT4 original.
* La administración de dinero basada en cuentas (`GLots`) no se transfiere; la estrategia StockSharp utiliza el parámetro fijo `OrderVolume`.

Estas adaptaciones preservan la idea comercial y al mismo tiempo se ajustan al modelo de programación de alto nivel y posición única de StockSharp.
