# Arrancador V6 Mod E
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Starter V6 Mod E** es una conversión de alto nivel StockSharp del MetaTrader 4 asesor experto `Starter_v6mod_e_www_forex-instruments_info.mq4`. El puerto mantiene la combinación original de extremos del oscilador de Laguerre, confirmación de impulso dual EMA, filtrado CCI y puerta de ángulo EMA mientras adapta la ejecución a la arquitectura basada en eventos de StockSharp.

## Lógica comercial

- **Puerta de tendencia:** se mide una pendiente EMA de 34 períodos entre turnos de inicio y fin configurables. La pendiente se expresa en unidades de pips; sólo las pendientes positivas permiten operaciones largas, sólo las pendientes negativas permiten ventas cortas y las lecturas planas bloquean nuevas entradas.
- **Extremos de Laguerre:** un Laguerre RSI hecho a mano (gamma = 0,7 por defecto) rastrea los estados de sobreventa/sobrecompra en la escala de 0 a 1. Las posiciones largas requieren que tanto los valores actuales como los anteriores se mantengan por debajo del nivel `Laguerre Oversold`, las posiciones cortas requieren que ambos valores estén por encima de `Laguerre Overbought`.
- **EMA filtro de impulso:** Las EMA (precio medio) de 120 y 40 períodos deben subir para posiciones largas y ambas caer para posiciones cortas, lo que refleja el filtro MA original.
- **CCI confirmación:** un CCI de 14 períodos debe estar por debajo de `-CCI Threshold` para largos y por encima de `+CCI Threshold` para cortos, replicando el filtro `Alpha` de MQL.
- **Seguridad del viernes:** las nuevas operaciones se bloquean después de `Friday Block Hour` y las posiciones restantes se liquidan una vez que se alcanza `Friday Exit Hour`.

## Gestión de riesgos

- Las distancias configurables de stop-loss, take-profit y trailing-stop (en pips) emulan el bloque de gestión del dinero del experto.
- Los trailingstops siguen el mejor precio favorable después de la entrada y cierran la operación cuando el retroceso excede la distancia configurada.
- El cierre manual de posiciones se ejecuta a través de `SellMarket`/`BuyMarket`, lo que garantiza un cumplimiento de alto nivel con API.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de órdenes para cada entrada al mercado. |
| `StopLossPips` | Distancia de parada de protección en pips. |
| `TakeProfitPips` | Objetivo de beneficio en pips. |
| `TrailingStopPips` | Distancia del trailing stop en pips (0 desactiva el trailing). |
| `SlowEmaPeriod` | Periodo de la lentitud EMA calculado el PRICE_MEDIAN. |
| `FastEmaPeriod` | Período de la EMA rápida calculado el día PRICE_MEDIAN. |
| `AngleEmaPeriod` | EMA período utilizado para el detector de ángulo. |
| `AngleStartShift` / `AngleEndShift` | Desplazamientos de barra utilizados para calcular la pendiente EMA. |
| `AngleThreshold` | Pendiente mínima (en unidades de pips) requerida para permitir el comercio. |
| `CciPeriod` / `CciThreshold` | Período y umbral absoluto para el filtro CCI. |
| `LaguerreGamma` | Parámetro gamma para el oscilador de Laguerre. |
| `LaguerreOversold` / `LaguerreOverbought` | Umbrales de entrada en la escala de Laguerre 0-1. |
| `CandleType` | Tipo de datos de vela (predeterminado 1 minuto). |
| `FridayBlockHour` / `FridayExitHour` | Horas (hora local del instrumento) que controlan los límites de riesgo del viernes. |

## Notas de conversión

- El oscilador de Laguerre se implementa directamente a partir de la fórmula recursiva original, manteniendo el rango de salida 0–1 y el suavizado gamma.
- La pendiente EMA reemplaza el ángulo auxiliar MQL al calcular las diferencias normalizadas por pips entre los puntos históricos EMA.
- Las funciones de administración de dinero, como el corte de capital y el apilamiento de cuadrícula, se omiten intencionalmente porque la variante MT4 que se está convirtiendo las deshabilita de forma predeterminada y StockSharp fomenta el control explícito de la cartera.
- Los pedidos se envían a través de `BuyMarket`/`SellMarket` y dependen de `OnNewMyTrade` para realizar un seguimiento de los precios de ejecución para la lógica de seguimiento.
