# Sistema de asesor experto de capa polaca eficiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto directo del asesor experto MQL4 "Sistema de asesor experto de capa polaca eficiente". Está diseñado para gráficos intradiarios (el autor original recomendó velas de 5 o 15 minutos) y restringe el comercio a una sola posición a la vez. La dirección de la tendencia se define por la alineación entre un promedio móvil de precios rápido y lento junto con dos filtros RSI suavizados. Las entradas reales requieren una triple confirmación de los indicadores Stochastic Oscilador, DeMarker y Williams %R para capturar las reversiones de las condiciones extremas que ocurren dentro de la tendencia predominante.

## Lógica comercial
1. **Filtro de tendencias.** Un promedio móvil simple de 9 períodos (SMA) de precios de cierre debe estar por encima del promedio móvil ponderado lineal (LWMA) de 45 períodos para permitir posiciones largas y por debajo para permitir posiciones cortas. Al mismo tiempo, el SMA de 9 períodos de RSI debe estar por encima (para largos) o por debajo (para cortos) del SMA de 45 períodos de RSI. Cualquier desacuerdo entre el precio y los filtros RSI bloquea nuevos pedidos.
2. **Stochastic disparador.** Cuando el filtro de tendencia es alcista, la estrategia espera a que la línea Stochastic %K cruce hacia arriba por encima del umbral de sobreventa (predeterminado 19) y simultáneamente cruce por encima de %D. Para configuraciones bajistas, %K debe cruzar hacia abajo por debajo del umbral de sobrecompra (predeterminado 81) y caer por debajo de %D. El factor de desaceleración se conserva del script MQL4.
3. **Confirmaciones de impulso.** Una señal larga requiere además que DeMarker cruce hacia arriba a través de 0,35 y Williams %R para cruzar hacia arriba a través de −81 en la vela completa actual. Las señales cortas exigen cruces a la baja hasta 0,63 y −19 respectivamente. Todos los cruces se evalúan entre la vela terminada anterior y la actual.
4. **Gestión de posiciones.** Solo se emiten órdenes de mercado y la estrategia permanece plana hasta que un stop u objetivo de protección cierra la operación. Los niveles de protección se recalculan a partir de parámetros basados ​​en pips utilizando el paso del precio del instrumento. Si el escalón de precio no está disponible la protección se desactiva.

## Gestión de riesgos
* **Stop-loss/take-profit.** Las distancias se configuran en pips. Cuando son positivos, los valores se convierten en compensaciones de precios reales usando `Security.PriceStep` (1 pip = 1 paso de precio) y se aplican inmediatamente después de la entrada. Establecer un parámetro en `0` desactiva el nivel de protección correspondiente.
* **Posición única.** El EA original nunca se formó en pirámide, por lo tanto, el puerto se niega a ingresar si ya existe una posición.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Volume` | `0.1` | Volumen de pedidos en lotes. Ajuste según el tamaño del contrato del corredor. |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Tipo de vela utilizado para los cálculos del indicador. Establezca un período de tiempo de 5 o 15 minutos para reflejar el EA original. |
| `RsiPeriod` | `14` | Longitud retrospectiva para el cálculo base RSI. |
| `ShortPricePeriod` | `9` | Periodo del precio rápido SMA utilizado en el filtro de tendencias. |
| `LongPricePeriod` | `45` | Período del precio lento LWMA utilizado en el filtro de tendencia. |
| `ShortRsiPeriod` | `9` | Longitud del rápido SMA aplicado a los valores RSI. |
| `LongRsiPeriod` | `45` | Longitud del SMA lento aplicado a los valores RSI. |
| `StochasticKPeriod` | `5` | Periodo base %K para el oscilador Stochastic. |
| `StochasticDPeriod` | `3` | Período de suavizado para la línea %D. |
| `StochasticSlowing` | `3` | Factor de suavizado adicional aplicado a %K. |
| `DemarkerPeriod` | `14` | Período promedio para el indicador DeMarker. |
| `WilliamsPeriod` | `14` | Período retroactivo para Williams %R. |
| `StochasticOversoldLevel` | `19` | Umbral de sobreventa que %K debe cruzar hacia arriba para permitir entradas largas. |
| `StochasticOverboughtLevel` | `81` | Umbral de sobrecompra que %K debe cruzar hacia abajo para permitir entradas cortas. |
| `DemarkerBuyLevel` | `0.35` | Valor mínimo de DeMarker requerido para entradas largas (cruzando desde abajo). |
| `DemarkerSellLevel` | `0.63` | Valor máximo de DeMarker permitido para entradas cortas (cruzando desde arriba). |
| `WilliamsBuyLevel` | `-81` | Williams Nivel de cruce %R que confirma entradas largas. |
| `WilliamsSellLevel` | `-19` | Williams Nivel de cruce %R que confirma entradas cortas. |
| `StopLossPips` | `7777` | Distancia de stop-loss en pips. El valor predeterminado muy grande desactiva efectivamente la parada a menos que se configure. |
| `TakeProfitPips` | `17` | Distancia de toma de ganancias en pips. Establezca en `0` para desactivar el objetivo fijo. |

## Notas
* Asegúrese de que `Security.PriceStep`, `Security.MinVolume` y `Security.VolumeStep` estén configurados correctamente; la estrategia asume que un pip equivale a un paso de precio al convertir los parámetros de riesgo.
* Los filtros de entrada dependen de los cruces de indicadores entre velas completadas consecutivas. Al importar datos históricos, mantenga la alineación de las barras idéntica al período de tiempo original para reproducir los resultados.
