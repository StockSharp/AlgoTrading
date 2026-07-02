# Estrategia de error X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **X Bug Strategy** es un sistema cruzado de media móvil convertido del asesor experto MQL4 con el mismo nombre. Compara dos promedios móviles simples calculados sobre el precio medio de las velas. Cuando la media rápida cruza por encima o por debajo de la media lenta, la estrategia abre una posición en la dirección del cruce. La implementación reproduce las características originales del Asesor Experto, incluida la inversión de señal opcional, el cierre automático de posición en señales opuestas y órdenes de protección basadas en pips.

## Lógica de trading
1. Suscríbase al tipo de vela configurado (velas de un minuto por defecto) y calcule dos medias móviles simples: una línea rápida y una línea lenta. Los promedios utilizan el precio medio y respetan los cambios de indicador configurados.
2. Detecte un cruce alcista cuando el valor rápido actual está por encima del valor lento mientras que el valor rápido dos barras antes estaba por debajo del valor lento. Detecte un cruce bajista utilizando la condición opuesta.
3. Opcionalmente, invierta la señal de cruce cuando **ReverseSignals** esté habilitado para operar en la dirección opuesta.
4. Cuando **CloseOnSignal** está habilitado, cierre inmediatamente cualquier posición opuesta antes de ingresar una nueva con la nueva señal.
5. Ingrese posiciones largas en señales alcistas y posiciones cortas en señales bajistas. La estrategia evita apilar posiciones en la misma dirección; solo opera cuando la posición actual es plana o está alineada con la señal.

## Gestión del riesgo
- **StopLossPips**: establece una parada de protección absoluta en pips. El stop se expresa en pips enteros; El precio fraccionario (cotizaciones de 5 o 3 dígitos) se maneja automáticamente convirtiendo el valor del pip utilizando el paso del precio del valor.
- **TakeProfitPips**: configura la distancia objetivo de ganancias en pips.
- **TrailingStopPips**: cuando **UseTrailingStop** está habilitado, activa un trailing stop que comienza en la distancia de pips configurada una vez que la posición genera ganancias. El paso final coincide con la distancia final, replicando la lógica MetaTrader original.
- Todas las órdenes de protección se gestionan a través de `StartProtection` con salidas de mercado para mantener la paridad con el experto MQL4.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen comercial base utilizado para las entradas al mercado. | `0.1` |
| `StopLossPips` | Distancia de stop-loss medida en pips; configúrelo en `0` para deshabilitarlo. | `70` |
| `TakeProfitPips` | Distancia de obtención de beneficios medida en pips; configúrelo en `0` para deshabilitarlo. | `5000` |
| `UseTrailingStop` | Habilita o deshabilita la gestión de trailing stop. | `true` |
| `TrailingStopPips` | Distancia de seguimiento en pips. | `90` |
| `FastPeriod` | Período de la media móvil rápida. | `1` |
| `FastShift` | Barras para desplazar la media móvil rápida antes de evaluar las señales. | `0` |
| `SlowPeriod` | Período de la media móvil lenta. | `14` |
| `SlowShift` | Barras para desplazar la media móvil lenta antes de evaluar las señales. | `10` |
| `CloseOnSignal` | Cerrar una posición contraria inmediatamente cuando aparezca una nueva señal. | `true` |
| `ReverseSignals` | Invierta la dirección de la señal para operar en contra del cruce. | `false` |
| `AppliedPrice` | Fuente del precio de las velas suministrada a las medias móviles. | `Median` |
| `CandleType` | Tipo de datos de vela para generación de señales. | `1 minute` período de tiempo |

## Notas
- La conversión de pips multiplica el paso del precio por 10 para los símbolos cotizados con 5 o 3 decimales, coincidiendo con el comportamiento original del Asesor Experto.
- No se proporciona ningún puerto Python; sólo la estrategia C# se incluye en este directorio.
- Las paradas finales, las paradas y los objetivos son opcionales. Establezca los valores de pip correspondientes en cero para desactivarlos.
