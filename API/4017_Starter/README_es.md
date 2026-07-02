# Estrategia inicial de 2005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Starter 2005** es una StockSharp conversión de alto nivel API del clásico MetaTrader 4 asesor experto `Starter.mq4` lanzado en 2005. El sistema original mezclaba un oscilador de Laguerre, un filtro de pendiente de media móvil exponencial y una confirmación del índice de canales de productos básicos. Este port mantiene intacto el árbol de decisiones mientras adapta la administración y ejecución del dinero a las convenciones StockSharp:

- Un proxy Laguerre RSI replica el buffer `iCustom("Laguerre")` que oscila entre 0 y 1.
- Un EMA de 5 períodos calculado sobre el precio medio proporciona la misma confirmación de pendiente ascendente/descendente utilizada por el experto en MT4.
- Un CCI de 14 períodos medido sobre los precios de cierre filtra las configuraciones débiles al igual que la variable `Alpha` original.
- La rutina de tamaño de lote adaptable refleja la función histórica `LotsOptimized()`, incluidas las reducciones basadas en rachas después de pérdidas consecutivas.
- Las salidas de posición se activan cuando Laguerre sale de la zona extrema o cuando la operación alcanza una distancia de ganancia configurable equivalente a `Point * Stop`.

## Lógica comercial
1. **Preparación de indicadores**
   - El valor de Laguerre RSI se reconstruye a través de un filtro de Laguerre de cuatro etapas con `Gamma` configurable.
   - La longitud de EMA tiene por defecto cinco velas y opera en `(High + Low) / 2` para coincidir con `PRICE_MEDIAN` en MQL4.
   - El período CCI por defecto es 14 en los precios de cierre y se mantiene un umbral muy pequeño (`±5`) para permanecer fiel al código heredado.
2. **Configuración larga**
   - Laguerre debe situarse cerca de cero (`LaguerreEntryTolerance` emula la estricta comparación `== 0`).
   - EMA debe estar subiendo en comparación con la vela terminada anterior.
   - CCI debe caer por debajo de `-CciThreshold`.
3. **Configuración corta**
   - Laguerre debe sentarse cerca de uno (`1 - LaguerreEntryTolerance` se aproxima a `== 1`).
   - EMA debe estar cayendo.
   - CCI debe elevarse por encima de `+CciThreshold`.
4. **Sale**
   - Las posiciones largas se cierran cuando Laguerre sube por encima de `LaguerreExitHigh` (por defecto `0.9`) o cuando el precio avanza `TakeProfitPoints * PriceStep` desde la entrada.
   - Los cortos se cierran cuando Laguerre cae por debajo de `LaguerreExitLow` (por defecto `0.1`) o cuando el precio cae en la misma distancia.
   - Cualquier otra posición plana manual restablece automáticamente el estado interno para evitar la entrada de datos obsoletos.

## gestión del dinero
El asistente `CalculateOrderVolume` reproduce el comportamiento de MT4 `LotsOptimized()`:

1. **Tamaño basado en el riesgo**: el capital multiplicado por `MaximumRisk` se divide por `RiskDivider` (el valor predeterminado es 500, como en la regla original `/500`). Cuando se divide por el precio actual, se obtiene el tamaño del lote ajustado al riesgo.
2. **Lote alternativo**: si el tamaño del riesgo produce un número menor que `BaseVolume`, el algoritmo mantiene el lote base.
3. **Reducción de la racha de pérdidas**: después de dos o más operaciones perdedoras consecutivas, el volumen se reduce en `volume * losses / DecreaseFactor`, coincidiendo exactamente con el bucle MQL que inspeccionó el historial comercial.
4. **Normalización**: los volúmenes se normalizan según el `VolumeStep` del instrumento y se fijan entre `MinVolume` y `MaxVolume` para evitar pedidos rechazados.

El seguimiento de pérdidas consecutivas se reinicia después de cualquier salida rentable y se incrementa después de operaciones perdedoras; los resultados de equilibrio dejan el contador intacto, reflejando el comportamiento original que ignoraba los tickets de beneficio cero.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `BaseVolume` | `decimal` | `1.2` | Tamaño de lote mínimo utilizado cuando el tamaño del riesgo sugiere una cantidad menor. |
| `MaximumRisk` | `decimal` | `0.036` | Fracción del patrimonio expuesto en una nueva posición antes de aplicar el divisor. |
| `RiskDivider` | `decimal` | `500` | Divisor aplicado al capital de riesgo, reproduciendo la regla original `AccountFreeMargin() * MaximumRisk / 500`. |
| `DecreaseFactor` | `decimal` | `2` | Divisor de rachas utilizado para reducir el volumen después de pérdidas consecutivas. |
| `MaPeriod` | `int` | `5` | EMA longitud en el precio medio de la vela. |
| `CciPeriod` | `int` | `14` | Retrospectiva del índice de canales de productos básicos. |
| `CciThreshold` | `decimal` | `5` | Nivel absoluto de CCI requerido para activar una señal. |
| `LaguerreGamma` | `decimal` | `0.66` | Factor de suavizado del filtro de Laguerre. |
| `LaguerreEntryTolerance` | `decimal` | `0.02` | Se utiliza una tolerancia de alrededor de 0/1 para imitar las comprobaciones de igualdad originales. |
| `LaguerreExitHigh` | `decimal` | `0.9` | Nivel de salida superior para posiciones largas. |
| `LaguerreExitLow` | `decimal` | `0.1` | Nivel de salida más bajo para posiciones cortas. |
| `TakeProfitPoints` | `decimal` | `10` | Objetivo de beneficio expresado en puntos de precio (`Point * Stop` en MQL). |
| `CandleType` | `DataType` | `TimeFrame(5m)` | Suscripción de vela procesada por la estrategia. |

## Notas de implementación
- Laguerre RSI se implementa en línea utilizando la recursividad de cuatro niveles del indicador original; no se requieren llamadas a `GetValue()`.
- Los indicadores EMA y CCI se actualizan manualmente dentro de la devolución de llamada de la vela para garantizar que el feed de precio medio coincida con la opción `PRICE_MEDIAN` de MetaTrader.
- Las entradas al mercado respetan las banderas `AllowLong()` / `AllowShort()` y garantizan que no haya órdenes activas pendientes, preservando el diseño de posición única de la fuente EA.
- El seguimiento de los resultados comerciales utiliza el precio de decisión de la vela (último precio, cierre o apertura) para estimar la dirección de PnL y mantener el contador de racha de pérdidas.
- Los comentarios en línea en inglés describen cada bloque de decisión importante para ayudar en el mantenimiento futuro.

## Consejos de uso
- El EA original estaba destinado a gráficos de divisas intradiarios; Comience con instrumentos líquidos que ofrezcan pequeños incrementos de precios para que el objetivo de ganancias de 10 puntos se alinee con un pip.
- Debido a que el script MT4 solo mantiene una posición, ejecute la estrategia en entornos donde es poco probable que se ejecuten parcialmente y se realicen órdenes simultáneas (pruebas históricas o mercados líquidos).
- Ajuste `LaguerreEntryTolerance` si el oscilador rara vez toca exactamente 0 o 1 en su conjunto de datos.
- Sintonice `RiskDivider` y `DecreaseFactor` juntos para equilibrar el crecimiento del riesgo y la mitigación de pérdidas.
