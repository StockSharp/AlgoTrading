# Estrategia AdaptiveTrader Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
AdaptiveTrader Pro es una estrategia de seguimiento de tendencias de múltiples períodos de tiempo convertida del asesor experto MetaTrader 5 *AdaptiveTrader_Pro_Final_EA.mq5*. Combina RSI, ATR y promedios móviles para operar en la dirección de la tendencia dominante mientras aplica controles de administración del dinero.

La estrategia funciona en un período de tiempo primario configurable (predeterminado 5 minutos) y confirma la dirección de la tendencia utilizando un promedio móvil de período de tiempo más alto (predeterminado 1 hora). Las entradas se basan en señales de sobreventa/sobrecompra RSI que concuerdan con ambas medias móviles.

## Reglas de trading
- **Entrada larga**: Cuando RSI cae por debajo de 30 y el cierre de la vela está por encima del marco temporal principal SMA y del marco temporal superior SMA.
- **Entrada corta**: Cuando RSI sube por encima de 70 y el cierre de la vela está por debajo de ambas SMA.
- **Posición única**: Solo se mantiene una posición direccional a la vez. Las posiciones opuestas se cierran antes de revertirse.

## Gestión de riesgos y comercio
- **Tamaño de la posición**: el tamaño de la posición se calcula a partir del capital de la cartera, el porcentaje de riesgo y la distancia de parada basada en ATR.
- **Manejo de paradas**: un trailing stop basado en ATR sigue el precio y se ajusta al punto de equilibrio después de que la operación se mueve a favor mediante un múltiplo ATR configurable.
- **Beneficio parcial**: una fracción configurable de la posición se cierra en un primer objetivo (ATR múltiplo). El volumen restante lo gestiona el trailing stop.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `MaxRiskPercent` | Porcentaje de riesgo aplicado a la cuenta por operación. | `0.2` |
| `RsiPeriod` | RSI duración en el período de tiempo principal. | `14` |
| `AtrPeriod` | ATR duración en el período de tiempo principal. | `14` |
| `AtrMultiplier` | multiplicador ATR para la distancia de parada inicial. | `1.5` |
| `TrailingStopMultiplier` | multiplicador ATR utilizado mientras se sigue la parada. | `1.0` |
| `TrailingTakeProfitMultiplier` | multiplicador ATR para el objetivo de obtención de beneficios parcial. | `2.0` |
| `TrendPeriod` | SMA duración en el período de tiempo principal. | `20` |
| `HigherTrendPeriod` | SMA duración en el período de tiempo más alto. | `50` |
| `BreakEvenMultiplier` | multiplicador ATR que activa el movimiento del stop al punto de equilibrio. | `1.5` |
| `PartialCloseFraction` | Fracción de la posición inicial cerrada en el primer objetivo. | `0.5` |
| `MaxSpreadPoints` | Spread máximo permitido en pasos de precios antes de abrir operaciones. | `20` |
| `CandleType` | Tipo de vela principal (período de tiempo) utilizado para el análisis. | `5 minute candles` |
| `HigherCandleType` | Tipo de vela de marco temporal más alto utilizado para la confirmación. | `1 hour candles` |

## Notas
- La estrategia utiliza API de alto nivel de StockSharp con suscripciones de velas y vinculación de indicadores.
- Los diferenciales se controlan a través de las mejores cotizaciones de oferta y demanda; la negociación se suspende hasta que el diferencial esté dentro del límite configurado.
- La implementación de Python se omite intencionalmente según las instrucciones.
