# XFatlXSatlCloud Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
XFatlXSatlCloud Duplex es una estrategia bidireccional convertida del asesor experto original de MQL5. Opera cruzamientos del indicador XFatlXSatlCloud, que mezcla un filtro digital FATL rápido con un filtro SATL más lento y luego suaviza ambos con medias móviles configurables. Se pueden aplicar configuraciones separadas a los lados largo y corto, incluyendo diferentes marcos temporales, métodos de suavizado y fuentes de precio aplicadas.

## Lógica de trading
La estrategia evalúa únicamente las velas terminadas. Dos suscripciones independientes se ejecutan en paralelo: una impulsa la lógica larga y la otra la lógica corta. Cada suscripción alimenta el indicador XFatlXSatlCloud implementado en C# y produce el siguiente comportamiento:

- **Entrada larga** – se activa cuando la línea rápida cruza por encima de la línea lenta en la barra definida por `LongSignalBar`. Si hay una posición corta abierta se cierra primero (solo si `ShortAllowClose` está habilitado). Se envía entonces una orden de compra de mercado con `LongVolume` contratos y se registra el precio de entrada para las verificaciones de riesgo.
- **Salida larga** – se ejecuta cuando la línea rápida cae por debajo de la línea lenta en la barra desplazada. Las comprobaciones opcionales de stop-loss y take-profit basadas en precio (`LongStopLoss`, `LongTakeProfit`) pueden cerrar la posición antes si el rango de la vela viola los desplazamientos definidos.
- **Entrada corta** – se activa cuando la línea rápida cruza por debajo de la línea lenta en la barra definida por `ShortSignalBar`. La exposición larga existente se aplana primero si `LongAllowClose` está habilitado. Se envía una orden de venta de mercado con `ShortVolume` contratos.
- **Salida corta** – se ejecuta cuando la línea rápida sube por encima de la línea lenta en la barra desplazada. Los controles de riesgo opcionales (`ShortStopLoss`, `ShortTakeProfit`) monitorean los extremos intrabarra.

Todos los valores de indicadores se calculan únicamente en velas terminadas, asegurando que cada decisión se base en datos finales y refleje el comportamiento MQL original.

## Gestión de riesgo
La estrategia realiza un seguimiento del último precio de entrada por separado para posiciones largas y cortas. Si se especifica un offset de stop-loss o take-profit y la vela actual supera el umbral correspondiente, la posición se cierra inmediatamente (sujeto a la bandera `AllowClose` relevante). Los offsets se miden en unidades de precio absolutas del instrumento negociado.

## Parámetros
| Grupo | Nombre | Descripción |
| --- | --- | --- |
| Trading | `LongVolume` | Tamaño de orden para entradas largas (mayor que cero). |
| Trading | `ShortVolume` | Tamaño de orden para entradas cortas (mayor que cero). |
| Trading | `LongAllowOpen` | Habilitar o deshabilitar la apertura de nuevas posiciones largas. |
| Trading | `LongAllowClose` | Habilitar o deshabilitar las salidas largas (necesario para stops y salidas cruzadas). |
| Trading | `ShortAllowOpen` | Habilitar o deshabilitar la apertura de nuevas posiciones cortas. |
| Trading | `ShortAllowClose` | Habilitar o deshabilitar las salidas cortas. |
| Signals | `LongSignalBar` | Número de barras completadas a mirar atrás al verificar el cruce para largos. |
| Signals | `ShortSignalBar` | Número de barras completadas a mirar atrás al verificar el cruce para cortos. |
| Data | `LongCandleType` | Tipo de vela (marco temporal) usado para la suscripción del indicador largo. |
| Data | `ShortCandleType` | Tipo de vela usado para la suscripción del indicador corto. |
| Indicators | `LongMethod1` | Método de suavizado aplicado a la salida FATL en el lado largo. Valores soportados: SMA, EMA, SMMA, LWMA, Jurik, ZeroLag, Kaufman. |
| Indicators | `LongLength1` | Longitud del suavizador rápido largo. |
| Indicators | `LongPhase1` | Parámetro de fase reenviado al suavizador rápido (mantenido por compatibilidad, solo Jurik lo usa conceptualmente). |
| Indicators | `LongMethod2` | Método de suavizado aplicado a la salida SATL en el lado largo (mismo conjunto soportado que arriba). |
| Indicators | `LongLength2` | Longitud del suavizador lento largo. |
| Indicators | `LongPhase2` | Parámetro de fase para el suavizador lento largo. |
| Indicators | `LongAppliedPrice` | Precio aplicado usado para construir el indicador largo (cierre, apertura, mediana, típico, ponderado, simple, cuarto, trend-follow o Demark). |
| Indicators | `ShortMethod1` | Método de suavizado para la línea rápida corta. |
| Indicators | `ShortLength1` | Longitud del suavizador rápido corto. |
| Indicators | `ShortPhase1` | Parámetro de fase para el suavizador rápido corto. |
| Indicators | `ShortMethod2` | Método de suavizado para la línea lenta corta. |
| Indicators | `ShortLength2` | Longitud del suavizador lento corto. |
| Indicators | `ShortPhase2` | Parámetro de fase para el suavizador lento corto. |
| Indicators | `ShortAppliedPrice` | Precio aplicado usado para construir el indicador corto. |
| Risk | `LongStopLoss` | Distancia de precio absoluta para el stop-loss largo (0 deshabilita la verificación). |
| Risk | `LongTakeProfit` | Distancia de precio absoluta para el take-profit largo (0 deshabilita la verificación). |
| Risk | `ShortStopLoss` | Distancia de precio absoluta para el stop-loss corto (0 deshabilita la verificación). |
| Risk | `ShortTakeProfit` | Distancia de precio absoluta para el take-profit corto (0 deshabilita la verificación). |

## Notas de implementación
- El indicador XFatlXSatlCloud se implementa como un indicador de alto nivel de StockSharp. Los componentes rápido y lento se producen aplicando los coeficientes de respuesta al impulso finito FATL/SATL originales seguidos de indicadores de suavizado seleccionados por el usuario.
- Solo se exponen las medias móviles de StockSharp comúnmente disponibles (`Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`, `ZeroLag`, `Kaufman`). Otras familias de suavizado MQL (como Parabolic o T3) no están incluidas.
- `LongSignalBar` y `ShortSignalBar` imitan el parámetro `SignalBar` original. Un valor de 1 significa "usar la barra completada anterior" al detectar el cruce.
- Los offsets de stop-loss y take-profit esperan distancias de precio absolutas. Se aplican usando el máximo/mínimo de la vela relativo al precio de entrada registrado y no dependen de los valores de punto específicos del broker.
