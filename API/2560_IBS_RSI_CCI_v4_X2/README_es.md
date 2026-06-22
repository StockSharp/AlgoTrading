# Estrategia IBS RSI CCI v4 X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia IBS RSI CCI v4 X2** es un sistema de momentum multi-marco temporal que combina el Internal Bar Strength (IBS), el Relative Strength Index (RSI) y el Commodity Channel Index (CCI). El algoritmo original del ecosistema MetaTrader 5 ha sido portado a StockSharp y rediseñado para usar suscripciones de velas de alto nivel con enlaces de indicadores. Se evalúan dos pipelines de indicadores independientes: un marco temporal lento de "tendencia" que define el sesgo direccional y un marco temporal rápido de "señal" que genera decisiones de entrada y salida.

En cada marco temporal la estrategia calcula un oscilador compuesto. El valor del oscilador se deriva de las contribuciones ponderadas de IBS, RSI y CCI. Los cambios rápidos en el valor compuesto se suavizan, se limitan mediante un umbral de momentum configurable y se envuelven con una envolvente de volatilidad que imita la lógica de búfer del indicador original. Los cruces entre el valor compuesto y su envolvente suavizada son los disparadores principales para las decisiones.

### Lógica de trading

1. **Detección de tendencia** – El marco temporal lento monitorea el oscilador compuesto. Si el compuesto se mantiene por encima de la envolvente la estrategia marca una tendencia alcista, de lo contrario señala una tendencia bajista.
2. **Generación de señal** – El marco temporal rápido evalúa dos valores consecutivos del compuesto y la envolvente. Los cruces en la barra más reciente confirman una señal accionable solo cuando la barra anterior respalda la transición.
3. **Reglas de entrada** –
   * Entrar largo solo cuando se permiten operaciones largas, la tendencia actual es alcista y el compuesto cruza por debajo de la envolvente en el marco temporal rápido (reversión bajista a alcista en la orientación del indicador original).
   * Entrar corto solo cuando se permiten operaciones cortas, la tendencia actual es bajista y el compuesto cruza por encima de la envolvente en el marco temporal rápido.
4. **Reglas de salida** –
   * Salidas inmediatas opcionales en cruces del compuesto cuando los interruptores `_CloseLongOnSignalCross` o `_CloseShortOnSignalCross` están habilitados.
   * Salidas forzadas basadas en tendencia cuando `_CloseLongOnTrendFlip` o `_CloseShortOnTrendFlip` solicitan el cierre tan pronto como el sesgo del marco temporal lento se invierte.
   * La gestión de riesgo se maneja mediante `StartProtection` de StockSharp, traduciendo las distancias de stop loss y take profit configuradas en puntos a desplazamientos de precio absolutos usando el paso de precio del instrumento.

### Indicadores y cálculos

* **Internal Bar Strength (IBS):** `(close - low) / max(high - low, price step)` suavizado por una media móvil seleccionable.
* **RSI:** RSI estándar aplicado a un precio configurable (cierre, apertura, máximo, mínimo, mediana, típico o ponderado).
* **CCI:** Implementación personalizada de CCI con media móvil simple y estimador de desviación media derivado del precio aplicado seleccionado.
* **Oscilador compuesto:** Suma ponderada de los valores transformados de IBS, RSI y CCI dividida por tres, limitada por el ajuste `Threshold` para replicar el "limitador de momentum" original.
* **Envolvente:** Los valores máximos y mínimos del compuesto sobre el rango configurado se suavizan dos veces y se promedian para producir la línea de base de señal usada para los cruces.

La implementación evita el sondeo directo de valores de indicadores (`GetValue`) manteniendo todo el estado dentro de las clases calculadoras y alimentando las velas secuencialmente a través de la API de alto nivel.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `OrderVolume` | Tamaño de orden base usado al abrir una nueva posición. |
| `TrendCandleType` | Tipo de vela para la suscripción al marco temporal lento. |
| `TrendIbsPeriod`, `TrendIbsMaType` | Período de suavizado IBS y tipo de media móvil para el marco temporal lento. |
| `TrendRsiPeriod`, `TrendRsiPrice` | Período RSI y precio aplicado para el marco temporal lento. |
| `TrendCciPeriod`, `TrendCciPrice` | Período CCI y precio aplicado para el marco temporal lento. |
| `TrendThreshold` | Umbral de límite de momentum usado en el compuesto del marco temporal lento. |
| `TrendRangePeriod`, `TrendSmoothPeriod` | Rango de look-back y ventana de suavizado para la envolvente del marco temporal lento. |
| `TrendSignalBar` | Desplazamiento (número de velas cerradas hacia atrás) usado al leer valores del marco temporal lento. |
| `AllowLongEntries`, `AllowShortEntries` | Habilitar o deshabilitar nuevas operaciones largas/cortas. |
| `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip` | Forzar cierres de posición cuando el sesgo del marco temporal lento se invierte. |
| `SignalCandleType` | Tipo de vela para la suscripción al marco temporal rápido. |
| `SignalIbsPeriod`, `SignalIbsMaType` | Configuración de suavizado IBS para el marco temporal rápido. |
| `SignalRsiPeriod`, `SignalRsiPrice` | Ajustes RSI para el marco temporal rápido. |
| `SignalCciPeriod`, `SignalCciPrice` | Ajustes CCI para el marco temporal rápido. |
| `SignalThreshold` | Umbral de límite de momentum usado en el compuesto del marco temporal rápido. |
| `SignalRangePeriod`, `SignalSmoothPeriod` | Rango de envolvente y suavizado en el marco temporal rápido. |
| `SignalSignalBar` | Desplazamiento aplicado al evaluar señales del marco temporal rápido. |
| `CloseLongOnSignalCross`, `CloseShortOnSignalCross` | Disparadores de salida opcionales en cruces del marco temporal rápido. |
| `StopLossPoints`, `TakeProfitPoints` | Distancias de stop loss y take profit medidas en puntos de paso de precio. |

## Notas de uso

1. Configure el instrumento y los tipos de vela antes de iniciar la estrategia. Ambos marcos temporales se suscribirán automáticamente a través de `GetWorkingSecurities`.
2. La configuración predeterminada refleja la versión MQL original: velas de tendencia de 8 horas con velas de señal de 1 hora y ajustes de indicadores idénticos en ambos marcos temporales.
3. Dado que el oscilador compuesto se limita internamente, los períodos de volatilidad extrema pueden producir respuestas más planas que las estrategias de momentum típicas. Ajuste los parámetros `Threshold`, `RangePeriod` y `SmoothPeriod` para adaptar la sensibilidad.
4. La protección de posición integrada depende del `PriceStep` del instrumento. Asegúrese de que los metadatos del instrumento proporcionen un paso válido; de lo contrario, considere ajustar el respaldo en el código.
5. Use los helpers de gráficos de StockSharp si necesita visualizar el comportamiento. La estrategia ya dibuja las velas del marco temporal de señal y las operaciones ejecutadas cuando hay un área de gráfico disponible.

## Riesgos y limitaciones

* La estrategia asume la entrega secuencial de velas. Las actualizaciones de velas fuera de orden pueden desincronizar los búferes internos.
* La desviación media en el CCI personalizado se recalcula a partir de los valores en búfer; la precisión depende de recibir un flujo de datos continuo sin brechas.
* Cuando `OrderVolume` se combina con exposición existente, los giros se realizarán enviando una única orden de mercado dimensionada para cerrar la posición opuesta y abrir la nueva. Asegúrese de que los permisos del bróker permitan ese comportamiento.
* El port preserva la orientación del indicador original (coeficientes negativos). Por lo tanto, las señales pueden parecer contraintuitivas hasta que revise el diseño del indicador heredado.

## Ampliación de la estrategia

* Ajuste los tipos de media móvil de forma independiente para la envolvente y el suavizado IBS para explorar reacciones más rápidas o lentas.
* Reemplace el calculador CCI personalizado por el indicador integrado de StockSharp si una versión futura expone los selectores de precio necesarios.
* Agregue superposiciones de gráfico vinculando los valores compuestos a paneles de gráfico adicionales cuando se requiera más retroalimentación visual.
* Combine con controles de riesgo adicionales como pérdida diaria máxima o filtros de tiempo de operación para implementaciones en producción.
