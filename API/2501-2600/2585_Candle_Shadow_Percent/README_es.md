# Estrategia de Porcentaje de Sombra de Vela
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Porcentaje de Sombra de Vela** es una portación directa del asesor experto de MetaTrader *Candle shadow percent*. Busca velas donde la mecha superior o inferior alcanza un porcentaje configurable del cuerpo de la vela. Cuando aparece una mecha superior alta, la estrategia abre una posición corta; cuando aparece una mecha inferior profunda, abre una posición larga. La dirección de la operación está alineada con el algoritmo original y mantiene el flujo de trabajo de gestión de riesgo intacto.

## Notas de conversión
* El experto original dependía de un indicador personalizado. En la versión de StockSharp, las proporciones de mecha y cuerpo se calculan directamente a partir de velas terminadas, por lo que no hay dependencias de indicadores externos.
* Los valores de pip se derivan de `Security.PriceStep`. Ajuste `StopLossPips`, `TakeProfitPips` y `MinBodyPips` para que coincidan con el tamaño de tick del instrumento.
* El dimensionamiento de posición basado en riesgo replica la lógica de `CMoneyFixedMargin` de MetaTrader arriesgando un porcentaje del valor actual del portafolio contra la distancia de stop-loss configurada.

## Calificación de vela
Una vela se considera para trading cuando:
1. Su tamaño de cuerpo absoluto es al menos `MinBodyPips * Security.PriceStep`.
2. La mecha correspondiente es positiva.
3. El ratio mecha-cuerpo satisface la lógica de umbral seleccionada:
   * **Mecha superior** (configuración de venta): `(High − max(Open, Close)) / Body * 100` es mayor o igual a `TopShadowPercent` cuando `TopShadowIsMinimum = true`, de lo contrario debe ser menor o igual a ese valor.
   * **Mecha inferior** (configuración de compra): `(min(Open, Close) − Low) / Body * 100` es mayor o igual a `LowerShadowPercent` cuando `LowerShadowIsMinimum = true`, de lo contrario debe ser menor o igual a ese valor.
4. Cuando ambas mechas satisfacen sus umbrales en la misma vela, la estrategia mantiene solo el lado con el mayor ratio de mecha para evitar señales dobles.

## Reglas de entrada
* **Entrada corta** – activada por una señal de mecha superior válida mientras la estrategia está plana o larga. La estrategia invierte la exposición larga existente si es necesario y establece las órdenes de protección inmediatamente.
* **Entrada larga** – activada por una señal de mecha inferior válida mientras la estrategia está plana o corta. La exposición corta existente se cierra automáticamente antes de establecer la nueva posición larga.

## Reglas de salida
* **Stop-loss** – colocado a `StopLossPips * Security.PriceStep` del precio de entrada. Las posiciones largas usan `entrada − distanciaStop`; las posiciones cortas usan `entrada + distanciaStop`.
* **Take-profit** – objetivo opcional ubicado a `TakeProfitPips * Security.PriceStep` de la entrada. Cuando `TakeProfitPips = 0`, el objetivo está deshabilitado y las posiciones dependen únicamente del stop-loss o señal opuesta para salir.
* La estrategia monitorea velas completadas. Si un rango de vela toca el stop o el objetivo, la posición se cierra en el siguiente ciclo de procesamiento.

## Dimensionamiento de posición
* El riesgo por operación se calcula como `Portfolio.CurrentValue * (RiskPercent / 100)`. Si el valor del portafolio no está disponible, la estrategia recurre al volumen de estrategia configurado.
* La cantidad es igual al monto de riesgo dividido por la distancia del stop-loss. Al invertir, el algoritmo agrega el tamaño absoluto de la exposición actual para asegurar una inversión completa, coincidiendo con el comportamiento del experto original de MetaTrader.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal o tipo de datos usado para las suscripciones de velas. |
| `StopLossPips` | Distancia del stop-loss expresada en pips/ticks relativa al instrumento. Debe ser mayor que cero. |
| `TakeProfitPips` | Distancia del take-profit en pips/ticks. Usar cero para deshabilitar el objetivo. |
| `RiskPercent` | Porcentaje del valor del portafolio arriesgado por operación. |
| `MinBodyPips` | Tamaño mínimo del cuerpo de la vela (en pips/ticks) requerido antes de evaluar los ratios de mecha. |
| `EnableTopShadow` | Habilita señales cortas basadas en la longitud de la mecha superior. |
| `TopShadowPercent` | Porcentaje umbral para el ratio mecha superior-cuerpo. |
| `TopShadowIsMinimum` | Cuando es true, el ratio debe ser mayor o igual al umbral; cuando es false, debe ser menor o igual a él. |
| `EnableLowerShadow` | Habilita señales largas basadas en la longitud de la mecha inferior. |
| `LowerShadowPercent` | Porcentaje umbral para el ratio mecha inferior-cuerpo. |
| `LowerShadowIsMinimum` | Controla si el umbral de la mecha inferior se trata como condición mínima o máxima. |

## Consejos de uso
* Comience con un marco temporal similar al EA original (p. ej., velas de 5 minutos) y ajuste las distancias en pips para su instrumento.
* Aumente `MinBodyPips` si el ruido produce demasiadas señales; disminúyalo para capturar reversiones más pequeñas.
* Combine la estrategia con filtros adicionales (como indicadores de tendencia) extendiendo la clase—las vinculaciones para indicadores adicionales se pueden agregar dentro de `OnStarted`.
* Siempre valide la interpretación del tamaño de tick en un portafolio demo antes de desplegarlo en producción.
