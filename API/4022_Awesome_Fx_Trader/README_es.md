# Impresionante comerciante de divisas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce la configuración MetaTrader de `MQL/8539`, que consta de los indicadores personalizados **AwesomeFxTradera.mq4** y **t_ma.mq4**. El código original pinta el histograma Bill Williams Awesome Oscillator en verde o rojo dependiendo de si el valor está subiendo o bajando, y superpone un promedio móvil ponderado lineal (LWMA) de 34 períodos junto con un clon suavizado de la misma curva. El puerto StockSharp mantiene los mismos cálculos y convierte los colores del indicador en señales comerciales.

## Lógica original MQL

1. **AwesomeFxTradera.mq4** calcula dos medias móviles exponenciales aplicadas al **precio de apertura** con los períodos 8 y 13. Su diferencia se almacena en `ExtBuffer0`. El búfer se pinta de verde cuando el valor actual es mayor que la barra anterior y de rojo cuando es menor. Esto codifica efectivamente la dirección del impulso, no solo su signo.
2. **t_ma.mq4** traza una LWMA de 34 períodos del precio de apertura (`ExtMapBuffer1`) y una media móvil simple de 6 períodos de esa LWMA (`ExtMapBuffer2`). El seguimiento más suave indica si el promedio de la tendencia se acelera o desacelera.

Por lo tanto, el gráfico MetaTrader destaca el impulso alcista cuando el oscilador está por encima de cero y sigue aumentando mientras el precio cotiza por encima de la LWMA suavizada. El impulso bajista es la configuración opuesta.

## StockSharp implementación

El `AwesomeFxTraderStrategy` se suscribe a un tipo de vela configurable (predeterminado **M15**) y alimenta los indicadores con el precio de apertura de la vela para que coincida con los buffers MetaTrader.

1. Las EMA rápidas y lentas se recalculan en cada vela terminada; su diferencia reproduce el histograma oscilante.
2. El LWMA sigue la tendencia de 34 barras y un SMA de 6 barras la suaviza. La comparación de ambas series revela si la curva de tendencia está subiendo o bajando.
3. El color del oscilador se reconstruye comparando el valor actual del histograma con la barra anterior, siguiendo la lógica `bool up` de la implementación MQL.
4. **Reglas de entrada**:
   - Entre en largo cuando el oscilador sea positivo, creciente (búfer verde) y el LWMA esté por encima de su nivel más suave.
   - Entre en corto cuando el oscilador sea negativo, caiga (buffer rojo) y el LWMA esté por debajo de su nivel más suave.
5. **Reglas de salida/inversión**: una señal opuesta invierte la posición. El tamaño de la orden aumenta automáticamente según la posición actual absoluta, de modo que los cortos se cierran antes de que se establezca un largo y viceversa.

No se definen niveles adicionales de stop-loss o take-profit en el código fuente, por lo que el puerto se basa únicamente en cambios de impulso para las salidas. Las declaraciones de registro documentan cada activación comercial junto con las lecturas del indicador.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `FastEmaPeriod` | 8 | Longitud del EMA rápido utilizado en la réplica del oscilador. |
| `SlowEmaPeriod` | 13 | Duración del EMA lenta. |
| `TrendLwmaPeriod` | 34 | Período del filtro de tendencias LWMA tomado de `t_ma.mq4`. |
| `TrendSmoothingPeriod` | 6 | Ventana del SMA aplicada a los valores LWMA. |
| `CandleType` | plazo de 15 minutos | Tipo de datos de vela utilizado para cálculos de impulso y tendencia. |

Todos los parámetros se pueden optimizar a través de la interfaz de usuario StockSharp gracias a los metadatos `StrategyParam`.

## Mapeo de archivos

| MetaTrader archivo | StockSharp contraparte | Notas |
| --- | --- | --- |
| `MQL/8539/AwesomeFxTradera.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Recrea el oscilador abierto EMA y su lógica de color ascendente/descendente. |
| `MQL/8539/t_ma.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Implementa la LWMA de 34 períodos con un SMA más suave de 6 períodos para la detección de tendencias. |

La versión de Python se omite intencionalmente según lo solicitado.
