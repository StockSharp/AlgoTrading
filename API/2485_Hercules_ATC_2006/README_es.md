# Estrategia Hercules A.T.C. 2006
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Hercules A.T.C. 2006 es una estrategia de seguimiento de tendencia en marcos temporales altos que recrea el asesor experto de
MetaTrader publicado en 2006. La versión de StockSharp escucha velas completadas en el marco temporal primario, detecta cruces
alcistas/bajistas entre una EMA(1) rápida y una SMA(72) lenta, y abre operaciones solo cuando filtros adicionales confirman la
ruptura. La estrategia divide su posición en dos tramos con niveles de take-profit independientes y ajusta el stop una vez que
el precio avanza.

## Indicadores y datos

- **Velas primarias:** configurables (por defecto velas de 1 hora).
- **MA rápida:** EMA con longitud `FastMaPeriod` (por defecto 1).
- **MA lenta:** SMA con longitud `SlowMaPeriod` (por defecto 72).
- **Filtro RSI:** RSI de longitud `RsiLength` en el `RsiTimeFrame` (por defecto 1 hora).
- **Envolvente diaria:** SMA de longitud `DailyEnvelopePeriod` en `DailyEnvelopeTimeFrame`
  con desviación de ±`DailyEnvelopeDeviation` por ciento.
- **Envolvente H4:** SMA de longitud `H4EnvelopePeriod` en `H4EnvelopeTimeFrame`
  con desviación de ±`H4EnvelopeDeviation` por ciento.
- **Máximo/mínimo rodante:** máximo más alto y mínimo más bajo de las últimas `HighLowHours`
  horas en el marco temporal primario.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TriggerPips` | 38 | Desplazamiento en pips añadido/sustraído al precio de cruce antes de disparar una orden. |
| `TrailingStopPips` | 90 | Distancia del stop móvil en pips (0 deshabilita el trailing). |
| `TakeProfit1Pips` | 210 | Primera distancia de take-profit en pips para reducir la mitad de la posición. |
| `TakeProfit2Pips` | 280 | Distancia de take-profit final en pips para cerrar la posición restante. |
| `FastMaPeriod` | 1 | Longitud de la EMA rápida usada en el detector de cruce. |
| `SlowMaPeriod` | 72 | Longitud de la SMA lenta de referencia. |
| `StopLossLookback` | 4 | Número de velas completadas usadas para calcular el precio de stop inicial. |
| `HighLowHours` | 10 | Tamaño de la ventana rodante (en horas) usada para el filtro de ruptura. |
| `BlackoutHours` | 144 | Período de enfriamiento (en horas) después de cerrar una operación antes de permitir una nueva entrada. |
| `RsiLength` | 10 | Longitud del RSI en el filtro de marco temporal superior. |
| `RsiUpper` | 55 | Valor mínimo de RSI requerido para permitir entradas largas. |
| `RsiLower` | 45 | Valor máximo de RSI permitido antes de bloquear entradas cortas. |
| `DailyEnvelopePeriod` | 24 | Longitud de la SMA para el filtro de envolvente diaria. |
| `DailyEnvelopeDeviation` | 0.99 | Desviación de la envolvente diaria en porcentaje. |
| `H4EnvelopePeriod` | 96 | Longitud de la SMA para el filtro de envolvente de cuatro horas. |
| `H4EnvelopeDeviation` | 0.1 | Desviación de la envolvente de cuatro horas en porcentaje. |
| `CandleType` | 1 hora | Tipo de vela de trabajo primario. |
| `RsiTimeFrame` | 1 hora | Tipo de vela usado para el filtro RSI. |
| `DailyEnvelopeTimeFrame` | 1 día | Tipo de vela usado para la envolvente diaria. |
| `H4EnvelopeTimeFrame` | 4 horas | Tipo de vela usado para la envolvente de cuatro horas. |

## Reglas de trading

1. **Detección de cruce**
   - Observar los valores de EMA(1) y SMA(72) de las últimas tres barras completadas.
   - Detectar una señal alcista cuando la EMA cruza por encima de la SMA en cualquiera de las dos barras anteriores.
   - Detectar una señal bajista cuando la EMA cruza por debajo de la SMA en cualquiera de las dos barras anteriores.
   - Almacenar el precio de cruce (media de los valores rápido y lento) e iniciar una ventana de activación de dos barras.

2. **Condición de activación**
   - Calcular `TriggerPrice = CrossPrice ± TriggerPips` (convertido a unidades de precio).
   - La activación es válida durante dos velas primarias tras el momento del cruce.
   - Los largos requieren que el máximo de la vela alcance o supere el precio de activación alcista.
   - Los cortos requieren que el mínimo de la vela alcance o rompa el precio de activación bajista.

3. **Filtros de entrada**
   - Sin posición existente y sin enfriamiento activo (`BlackoutHours`).
   - Filtro RSI: `RSI > RsiUpper` para largos, `RSI < RsiLower` para cortos.
   - Filtro de ruptura: el cierre actual debe superar el máximo rodante para largos o caer por debajo del mínimo rodante para cortos.
   - Confirmación de envolvente: el cierre actual debe estar por encima de ambas bandas superiores para largos o por debajo de ambas bandas inferiores para cortos.

4. **Ejecución de órdenes**
   - Enviar una orden de mercado usando el volumen de la estrategia (por defecto 2 unidades, lo que significa dos sub-posiciones iguales).
   - Stop-loss: mínimo (largo) o máximo (corto) de la vela en la posición `StopLossLookback`.
   - Niveles de take-profit: `TakeProfit1Pips` para la primera mitad, `TakeProfit2Pips` para el resto.
   - Iniciar un temporizador de bloqueo para impedir nuevas entradas durante `BlackoutHours` horas.

5. **Gestión de la posición**
   - El stop móvil se activa inmediatamente si `TrailingStopPips` > 0 y se mueve solo a favor de la operación.
   - Reducir a la mitad la posición al primer nivel de take-profit.
   - Cerrar la posición restante cuando se activa el take-profit final, se alcanza el stop-loss o el precio cruza el stop móvil.

## Gestión del riesgo

- Los stops siempre se derivan de velas completadas para reducir el ruido intrabarra.
- Dos objetivos de take-profit aseguran ganancias parciales antes de dejar correr la operación.
- Los stops móviles garantizan que las ganancias queden protegidas después de que el mercado se mueva en la dirección deseada.
- Un largo período de bloqueo (por defecto 144 horas) previene la reentrada rápida tras una ruptura y replica el comportamiento del EA original.

## Notas

- El port de StockSharp preserva la idea de gestión monetaria original al establecer el volumen de la estrategia en dos unidades por defecto, de modo que la salida parcial deja la mitad de la posición corriendo.
- Los valores de desplazamiento de la envolvente de MetaTrader se aproximan usando los valores más recientes porque el desplazamiento hacia adelante no está soportado por la API de alto nivel.
- La estrategia requiere información sobre el paso de precio para traducir correctamente las distancias en pips; asegúrese de que los metadatos del instrumento estén completados.
