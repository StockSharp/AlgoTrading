# Estrategia RSI Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el asesor experto de MetaTrader *"RSI trader v0.15"* en la API de alto nivel de StockSharp. Alinea la dirección de la tendencia entre la acción del precio y un Índice de Fuerza Relativa (RSI) suavizado. El trading se realiza en un único instrumento usando velas de una hora por defecto, pero el marco temporal es configurable a través del parámetro `CandleType`.

## Lógica de trading
1. Calcular un RSI estándar con un período configurable.
2. Suavizar el RSI con dos medias móviles simples (SMA): un promedio de señal rápido y uno de confirmación más lento.
3. Rastrear dos medias móviles del precio de cierre: una media móvil simple corta y una media móvil ponderada larga para aproximar el par SMA/LWMA del MQL original.
4. Generar estados de tendencia en cada vela terminada:
   - **Alineación alcista**: SMA de precio corta por encima de la larga **y** SMA RSI rápida por encima de la lenta.
   - **Alineación bajista**: SMA de precio corta por debajo de la larga **y** SMA RSI rápida por debajo de la lenta.
   - **Lateral / desacuerdo**: las medias móviles apuntan en direcciones opuestas, señalando que no hay tendencia clara.
5. Actuar sobre el estado detectado:
   - Abrir una posición larga cuando aparece alineación alcista y no hay posición actualmente abierta.
   - Abrir una posición corta cuando aparece alineación bajista y no hay posición actualmente abierta.
   - Cerrar inmediatamente cualquier posición abierta cuando se detecta el estado lateral, reflejando la salida protectora de la versión MQL.
6. El modo de reversión opcional invierte todas las direcciones de entrada, permitiendo al usuario operar contra la tendencia con respecto a las señales detectadas.

La estrategia respeta el manejo de protección incorporado de StockSharp y requiere velas completadas antes de tomar cualquier acción.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `RsiPeriod` | Período de retrospección usado para el cálculo del RSI. | 14 |
| `ShortRsiMaPeriod` | Longitud de la SMA rápida aplicada a los valores del RSI. | 9 |
| `LongRsiMaPeriod` | Longitud de la SMA lenta aplicada a los valores del RSI. | 45 |
| `ShortPriceMaPeriod` | Longitud de la SMA corta aplicada a los precios de cierre. | 9 |
| `LongPriceMaPeriod` | Longitud de la media móvil ponderada larga aplicada a los precios. | 45 |
| `Reverse` | Cuando es `true`, las órdenes de compra y venta se intercambian (refleja la entrada "Reverse" original). | `false` |
| `CandleType` | Tipo de datos para velas de precio. Predeterminado es marco temporal de una hora. | `1h` |

Todos los parámetros enteros exponen rangos de optimización que reflejan la flexibilidad de los ajustes de entrada del experto de MetaTrader.

## Gestión de riesgo
- Las posiciones se cierran tan pronto como los trends de precio y RSI no coinciden (estado lateral), reproduciendo el comportamiento de salida inmediata del EA.
- `StartProtection()` se habilita al inicio para cooperar con la infraestructura protectora de StockSharp.

## Notas
- La estrategia depende de la propiedad base `Volume` de `Strategy` para definir el tamaño de la operación.
- Solo se procesan velas completadas; las actualizaciones parciales se ignoran para evitar señales prematuras.
- Se usa la media móvil ponderada para coincidir con la LWMA larga original aplicada a los cierres de precio.
