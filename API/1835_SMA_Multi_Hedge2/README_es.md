# Estrategia SMA Multi Hedge2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera un instrumento base mientras hace cobertura con un instrumento correlacionado. La dirección de la tendencia se determina mediante una Media Móvil Simple (SMA). Cuando la correlación entre los instrumentos base y de cobertura supera un umbral, ambos instrumentos se operan para formar un par neutral al mercado.

## Cómo funciona

1. Calcular la tendencia del instrumento base usando una SMA de longitud configurable.
2. Medir la correlación entre los instrumentos base y de cobertura usando la diferencia entre el precio y su propia SMA.
3. Si la correlación alcanza el nivel esperado, abrir posiciones en ambos instrumentos. La dirección de la cobertura puede seguir u oponerse a la base según la configuración.
4. Las posiciones se cierran automáticamente cuando la ganancia combinada alcanza el valor objetivo.

## Parámetros

- `SmaPeriod` — período de la SMA usado para detectar la tendencia. Por defecto 20.
- `CorrelationPeriod` — número de muestras usado para evaluar la correlación. Por defecto 20.
- `ExpectedCorrelation` — correlación absoluta mínima requerida para activar la cobertura. Por defecto 0.8.
- `ProfitTarget` — objetivo de ganancia total en unidades monetarias. Por defecto 30.
- `CandleType` — tipo de datos para suscripción de velas. Por defecto marco temporal de 1 minuto.
- `FollowBase` — si es verdadero, la cobertura opera en la misma dirección cuando la correlación es positiva.

## Indicadores

- SMA
- Correlación (cálculo personalizado)

## Notas

Este es un port simplificado de la estrategia MQL original. La gestión de riesgo y dinero debe ajustarse antes de operar en vivo.

