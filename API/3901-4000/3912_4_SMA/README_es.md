# 4 SMA Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia 4 SMA replica el asesor experto MetaTrader **4 SMA.mq4**. Funciona con velas de 30 minutos calculadas con precios medios y compara cuatro promedios móviles simples (5, 20, 40 y 60 períodos) para detectar rupturas de impulso. El puerto StockSharp mantiene el comportamiento de posición única del código original y utiliza ayudantes API de alto nivel para entradas al mercado y gestión de riesgos.

## Lógica de trading
- Calcule el precio medio `(high + low) / 2` de cada vela terminada e introdúzcalo en las cuatro SMA.
- **La entrada larga** ocurre cuando el SMA rápido está por encima del SMA medio, el SMA medio está por encima del SMA lento, el SMA lento está por encima del SMA muy lento en al menos un paso de precio, y el {PH006}} lento anterior estaba por debajo o igual al SMA muy lento. Sólo puede haber una posición larga activa a la vez.
- **La entrada corta** es la condición de espejo: el SMA rápido está por debajo del SMA medio, el SMA medio está por debajo del {PH003}} lento, el {PH004}} muy lento está por encima del {PH005}} lento en al menos un paso de precio, y el {PH006}} lento anterior estaba por encima o igual al {PH007}} muy lento. Sólo puede haber una posición corta activa a la vez.

## Gestión de Puestos
- La estrategia cierra posiciones largas cuando el lento SMA cruza por debajo del muy lento SMA y cierra posiciones cortas cuando el lento SMA cruza por encima del muy lento SMA.
- Los niveles de protección se calculan previamente después de cada entrada. Las distancias de stop-loss y take-profit siguen la configuración original basada en puntos y se basan en el paso del precio del valor.
- Los topes dinámicos se activan después de que el precio supera la distancia de seguimiento configurada. La parada se sigue vela a vela y nunca se afloja.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| Tipo de vela | Serie de velas utilizadas para los cálculos (30 minutos por defecto). | marco de tiempo M30 |
| tomar ganancias | Distancia de toma de ganancias en puntos. | 50 |
| Detener pérdida | Distancia de stop-loss en puntos. | 50 |
| Parada final | Distancia del trailing stop en puntos. | 11 |
| Longitud rápida | Duración del ayuno SMA. | 5 |
| Longitud media | Longitud del medio SMA. | 20 |
| longitud lenta | Duración del lento SMA. | 40 |
| Longitud muy lenta | Duración del muy lento SMA. | 60 |

Todos los parámetros numéricos están expuestos para su optimización a través de la interfaz de usuario del parámetro StockSharp.

## Diferencias con la versión MQL
- El trailing stop original manipuló las órdenes MT4 directamente; el puerto recalcula los precios de salida y emite órdenes de mercado cuando se superan los niveles.
- Los cálculos conscientes del paso del precio permiten que la estrategia opere en instrumentos con tamaños de ticks no forex.
- La implementación de StockSharp se basa en enlaces de alto nivel `SubscribeCandles` y parámetros de estrategia, manteniéndose cerca de las mejores prácticas del marco.
