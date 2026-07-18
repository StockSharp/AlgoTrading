# Estrategia de Fortaleza de Divisas CCFp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia porta el clásico asesor experto CCFp de MetaTrader a la API de alto nivel de StockSharp. Calcula una puntuación de fortaleza relativa para las ocho divisas principales (USD, EUR, GBP, CHF, JPY, AUD, CAD, NZD) usando ratios entre medias móviles simples rápidas y lentas en los siete pares principales basados en USD (EURUSD, GBPUSD, AUDUSD, NZDUSD, USDCAD, USDCHF, USDJPY). Cuando la diferencia entre dos fortalezas de divisas supera un umbral configurable, la estrategia abre posiciones de mercado que expresan la divisa más fuerte contra la más débil.

La implementación sigue la arquitectura de alto nivel recomendada: cada instrumento tiene su propia suscripción de velas, los indicadores se vinculan a través de `Bind` y la gestión de órdenes usa `RegisterOrder` con órdenes de mercado. Los comentarios en las órdenes ejecutadas reutilizan el formato `(TOPDOWN)` original para mantener el mismo estilo de contabilidad que la versión MQL.

## Instrumentos requeridos
Adjuntar los siguientes valores a los parámetros de la estrategia:

- `EURUSD`
- `GBPUSD`
- `AUDUSD`
- `NZDUSD`
- `USDCAD`
- `USDCHF`
- `USDJPY`

Los siete pares deben compartir el mismo marco temporal que se establece a través del parámetro `Candle Type`.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `Fast MA` | Período de media móvil rápida usado en el cálculo de fortaleza. |
| `Slow MA` | Período de media móvil lenta usado en el cálculo de fortaleza. |
| `Strength Step` | Diferencia mínima entre dos divisas que debe superarse para desencadenar una nueva señal. |
| `Close Opposite` | Si está habilitado, la estrategia cierra las posiciones opuestas antes de enviar una nueva orden. |
| `Candle Type` | Serie de velas procesada por los indicadores. |
| `Volume` base | Tomado de la propiedad estándar `Strategy.Volume` y usado para cada orden de mercado enviada. |

## Lógica de trading
1. Cada uno de los siete pares principales USD está suscrito con su propio par de medias móviles simples (rápida y lenta).
2. Cada vez que llega una vela terminada, la estrategia convierte el ratio de los promedios lento y rápido en los mismos valores de fortaleza sintéticos producidos por el indicador CCFp original.
3. Después de que se actualicen los siete pares, se recalculan las ocho puntuaciones de fortaleza de divisas.
4. Cuando la diferencia entre una divisa "top" y una "down" cruza hacia arriba el nivel `Strength Step`, mientras la divisa top está subiendo y la divisa down está bajando, se detecta una oportunidad.
5. La estrategia abre órdenes de mercado que expresan exposición larga a la divisa fuerte y exposición corta a la divisa débil:
   - Si USD es la divisa fuerte, solo se coloca una orden en el par contraparte (por ejemplo, short `EURUSD`).
   - Si USD es la divisa débil, la estrategia compra el par donde la divisa fuerte es la base (por ejemplo, long `EURUSD`).
   - Cuando ambas divisas son no-USD, la estrategia envía dos órdenes: long la divisa top contra USD y short la divisa down contra USD.
6. Si `Close Opposite` está habilitado y una posición opuesta aún está abierta en un par objetivo, la estrategia envía una orden de mercado de cierre antes de entrar en un nuevo trade.

## Gestión de riesgo
- La estrategia no adjunta órdenes explícitas de stop-loss o take-profit; el control de riesgo es manejado por el indicador `Close Opposite` junto con herramientas de gestión de portafolio manual.
- El tamaño de entrada es controlado por la propiedad `Volume`. Configurarlo de acuerdo al tamaño de la cuenta y la exposición deseada por segmento.

## Diferencias vs la implementación MQL original
- El cálculo de fortaleza de divisas usa indicadores `SimpleMovingAverage` de StockSharp en un único marco temporal. El apilamiento de coeficientes de múltiples marcos temporales del indicador MQL puede emularse ajustando los períodos `Fast MA` y `Slow MA`.
- Los stops protectores no se trailingan automáticamente; en cambio, la estrategia se centra en reproducir la lógica de entrada/salida y deja el control avanzado de riesgo a la capa de portafolio de StockSharp.
- El enrutamiento de órdenes usa el ayudante de alto nivel `RegisterOrder` y las referencias de seguridad de StockSharp en lugar de objetos de trade de MetaTrader.
