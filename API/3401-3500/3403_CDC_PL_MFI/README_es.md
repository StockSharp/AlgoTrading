# Estrategia de IMF CDC PL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia CDC PL MFI** reproduce el MetaTrader asesor experto `Expert_ADC_PL_MFI` (MQL/299) en StockSharp. Busca los patrones de inversión de dos velas **Dark Cloud Cover** y **Piercing Line** y valida cada señal con el oscilador **Money Flow Index (MFI)**. La estrategia utiliza los mismos períodos de indicador y umbrales de nivel que el experto original, agrega protección opcional de stop-loss y take-profit en unidades de pip y cierra posiciones cuando la IMF cruza niveles de reversión configurables.

## Lógica de trading
1. Suscríbase al tipo de vela configurado (velas de una hora por defecto) y calcule un índice de flujo de dinero con el período especificado. Mantenga promedios móviles simples del tamaño del cuerpo de la vela y los precios de cierre para replicar los filtros de tendencia y volatilidad originales.
2. Cuando se forma un patrón alcista **Piercing Line** (brecha por debajo del mínimo anterior, cierre alcista por encima del punto medio de la vela bajista anterior, ambas velas más grandes que el cuerpo promedio y el cierre anterior por debajo del promedio de la tendencia) *y* el valor actual de MFI está por debajo del **LongEntryLevel** (predeterminado `40`), ingrese o cambie a una posición larga.
3. Cuando se forma un patrón bajista **Cubierta de nubes oscuras** (brecha por encima del máximo anterior, cierre bajista por debajo del punto medio de la vela alcista anterior, ambas velas más grandes que el cuerpo promedio y el cierre anterior por encima del promedio de la tendencia) *y* el valor actual de MFI está por encima del **ShortEntryLevel** (predeterminado `60`), ingrese o cambie a una posición corta.
4. Monitorear a la IMF para cerrar posiciones de manera proactiva:
   - Cierre las posiciones cortas cuando la IMF cruce por encima de **ExitLowerLevel** (`30`) o **ExitUpperLevel** (`70`).
   - Cierre posiciones largas cuando la IMF cruce por debajo de **ExitUpperLevel** (`70`) o **ExitLowerLevel** (`30`).
5. Las órdenes de protección son opcionales. Cuando **TakeProfitPips** o **StopLossPips** son mayores que cero, la estrategia llama a `StartProtection` con las compensaciones de precio correspondientes (distancia del pip multiplicada por el paso del precio del valor).

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Tipo de datos de vela utilizado para la detección de patrones. | `1 hour` período de tiempo |
| `MfiPeriod` | Longitud del oscilador del índice de flujo de dinero. | `49` |
| `BodyAveragePeriod` | Período de la media móvil del cuerpo de la vela utilizado para calificar velas "largas". | `11` |
| `LongEntryLevel` | Umbral de MFI que confirma configuraciones alcistas de Piercing Line. | `40` |
| `ShortEntryLevel` | Umbral de MFI que confirma configuraciones bajistas de Dark Cloud Cover. | `60` |
| `ExitLowerLevel` | Menor nivel de IFM que desencadena la cobertura de posiciones cortas. | `30` |
| `ExitUpperLevel` | Nivel superior de MFI que desencadena el cierre de posiciones largas. | `70` |
| `StopLossPips` | Distancia de stop-loss opcional en pips (0 desactiva la protección). | `50` |
| `TakeProfitPips` | Distancia de toma de ganancias opcional en pips (0 desactiva la protección). | `50` |

## Notas
- El volumen predeterminado es `1` lote. Cuando la estrategia cambia de dirección, envía una orden de mercado única del tamaño de cerrar la posición existente y abrir la nueva, coincidiendo con el comportamiento MQL.
- La detección de patrones refleja la lógica MetaTrader: solo se evalúan las velas completadas, las brechas deben ocurrir más allá del máximo/mínimo anterior y un promedio móvil simple impone la condición de tendencia predominante.
- Los valores del índice de flujo de dinero provienen directamente del indicador consolidado. No se requiere almacenamiento en búfer manual del historial del indicador; la estrategia almacena solo los valores más recientes para detectar cruces de umbrales.
- No se proporciona ningún puerto Python; only the C# implementation is included in this directory.
