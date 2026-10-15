# Estrategia Expert Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Expert Ichimoku replica la lógica del asesor experto original MQL5 "Expert Ichimoku" usando la API de alto nivel de StockSharp. El sistema es un modelo de seguimiento de tendencia direccional que combina múltiples componentes del indicador Ichimoku Kinko Hyo con filtros de acción del precio y un módulo opcional de dimensionamiento de posición estilo martingala.

La estrategia evalúa señales en velas completadas de un marco temporal configurable. Las operaciones largas y cortas son mutuamente exclusivas — la estrategia mantiene una única posición neta y cambia de dirección solo después de cerrar la exposición existente. Todos los valores del indicador se calculan en la serie de velas suscrita; no se requieren datos externos.

## Lógica principal

### Configuración del indicador

* **Tenkan-sen (Línea de conversión):** Media móvil rápida usada para la detección de cruces.
* **Kijun-sen (Línea base):** Media móvil lenta que forma el socio del cruce.
* **Senkou Span A / Senkou Span B:** Límites de la nube evaluados en la barra anterior para confirmar la estructura de mercado alcista o bajista.
* **Chikou Span (Línea rezagada):** Confirmación de momento mediante condiciones de ruptura de precio rezagado.

Las longitudes del indicador son configurables por el usuario y coinciden con los valores predeterminados del experto MQL5 (9 / 26 / 52).

### Reglas de entrada

Una posición larga se abre cuando se satisfacen todas las condiciones siguientes:

1. **Disparador de momento:** Ya sea que
   * Tenkan-sen cruzó por encima de Kijun-sen en la barra cerrada más reciente (Tenkan<sub>t-1</sub> ≤ Kijun<sub>t-1</sub> y Tenkan<sub>t</sub> > Kijun<sub>t</sub>), o
   * El Chikou Span rompió por encima del precio histórico (Chikou<sub>t-1</sub> ≤ Close<sub>t-11</sub> y Chikou<sub>t</sub> > Close<sub>t-10</sub>),
2. **Filtro de nube:** El cierre actual está por encima de ambos spans de Senkou de la barra anterior (precio completamente por encima de la nube),
3. **Filtro de acción del precio:** La vela anterior cerró alcista (Close<sub>t-1</sub> > Open<sub>t-1</sub>),
4. **Filtro de posición:** No hay exposición larga actualmente activa. Si existe una posición corta, se cierra primero; la nueva posición larga se envía solo después de aplanar el corto.

Una posición corta se abre bajo condiciones simétricas:

1. **Disparador de momento:** Ya sea que
   * Tenkan-sen cruzó por debajo de Kijun-sen (Tenkan<sub>t-1</sub> ≥ Kijun<sub>t-1</sub> y Tenkan<sub>t</sub> < Kijun<sub>t</sub>), o
   * El Chikou Span rompió por debajo del precio histórico (Chikou<sub>t-1</sub> ≥ Open<sub>t-11</sub> y Chikou<sub>t</sub> < Open<sub>t-10</sub>),
2. **Filtro de nube:** El cierre actual está por debajo de ambos spans de Senkou de la barra anterior,
3. **Filtro de acción del precio:** La vela anterior cerró bajista (Close<sub>t-1</sub> < Open<sub>t-1</sub>),
4. **Filtro de posición:** La exposición larga existente se cierra antes de abrir el corto.

### Dimensionamiento de posición y opción de martingala

* El tamaño base de la orden es igual a la propiedad `Volume` de la estrategia.
* Cuando se habilita **Use Martingale**, el siguiente tamaño de entrada se duplica si la operación completada anterior cerró con pérdida. Las operaciones rentables o en punto de equilibrio restablecen el multiplicador.
* El tamaño de orden resultante está limitado por `Volume × Max Position Multiplier`, reflejando la protección de número máximo de posiciones en el EA original.

### Gestión de riesgo

* **Stop-Loss / Take-Profit estático:** Los desplazamientos de precio absolutos opcionales se aplican a cada nueva posición. Si el precio de cierre alcanza el stop o el objetivo, la posición se cierra al mercado.
* **Stop de seguimiento:** Cuando tanto `Trailing Stop Offset` como `Trailing Step` son positivos, el nivel de stop se aprieta solo después de que el precio avance más allá de `offset + step` desde la entrada, emulando la lógica de seguimiento incremental de la versión MQL5.
* La estrategia opera una posición neta. Al salir (via stop, objetivo, seguimiento o reversión), el PnL realizado se evalúa para actualizar el indicador de martingala para la próxima señal.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| Tenkan Period | Longitud de la línea Tenkan-sen. | 9 |
| Kijun Period | Longitud de la línea Kijun-sen. | 26 |
| Senkou Span B Period | Longitud de la línea Senkou Span B. | 52 |
| Stop Loss Offset | Distancia absoluta entre el precio de entrada y el stop de protección. Establezca en 0 para deshabilitar. | 0 |
| Take Profit Offset | Distancia absoluta entre el precio de entrada y el objetivo de ganancia. Establezca en 0 para deshabilitar. | 0 |
| Trailing Stop Offset | Distancia base de seguimiento aplicada después de la activación. | 0 |
| Trailing Step | Movimiento adicional requerido antes de apretar el stop de seguimiento. | 0 |
| Max Position Multiplier | Límite superior para el tamaño efectivo de la orden (relativo a `Volume`). | 5 |
| Use Martingale | Si se debe duplicar el siguiente tamaño de operación después de una operación perdedora. | true |
| Candle Type | Serie de velas utilizada para los cálculos. | Marco temporal de 1 hora |

## Notas prácticas

* La estrategia requiere al menos 12 velas completadas antes de que todas las condiciones puedan evaluarse (las comparaciones de Chikou hacen referencia a precios hasta 11 barras atrás).
* Dado que las estrategias de StockSharp operan en posiciones netas, el parámetro `Max Position Multiplier` limita el tamaño máximo del contrato en lugar de gestionar múltiples tickets independientes. Esto mantiene el comportamiento alineado con el límite de exposición de la implementación MQL5.
* La lógica del stop de seguimiento refleja el EA: el stop se mueve solo cuando el precio ha progresado por `Trailing Stop Offset + Trailing Step`. Establecer cualquiera de los parámetros en cero deshabilita los ajustes de seguimiento.
* Las declaraciones de registro reportan cada entrada y salida, facilitando la auditoría de los puntos de decisión al reproducir datos de mercado.

## Flujo de trabajo de uso

1. Configure el tipo de vela y el instrumento deseados en un `StrategyContainer` o plantilla de diseñador.
2. Establezca el `Volume` base y ajuste los parámetros de riesgo según la volatilidad del símbolo (por ejemplo, convierta las distancias basadas en pips del EA original en unidades de precio para el mercado seleccionado).
3. Inicie la estrategia. Una vez que el indicador tenga suficiente historial, evaluará cruces y confirmaciones de la línea rezagada en cada barra completada, gestionando automáticamente las salidas y el dimensionamiento de martingala.
