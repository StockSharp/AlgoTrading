# Estrategia MAMy Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Port del asesor de MetaTrader 5 "MAMy Expert" de Victor Chebotariov a la API de estrategia de alto nivel de StockSharp.
- Reproduce el indicador personalizado original que compara tres medias móviles de diferentes fuentes de precio (apertura, cierre, precio ponderado).
- Funciona estrictamente con velas completadas y gestiona como máximo una posición neta a la vez, reflejando el comportamiento del expert MQL.

## Base del indicador
- La estrategia construye tres medias móviles usando la misma longitud y algoritmo de suavizado:
  - `MA(close)` – calculada sobre los precios de cierre de velas.
  - `MA(open)` – calculada sobre los precios de apertura de velas.
  - `MA(weighted)` – calculada sobre el precio ponderado `(High + Low + 2 × Close) / 4`.
- El parámetro `MaType` selecciona el algoritmo de promediado (Simple, Exponencial, Suavizado o LWMA Ponderado) para las tres series, coincidiendo con las opciones `MODE_*` de MetaTrader.
- Un "búfer de cierre" se calcula como la diferencia `MA(close) − MA(weighted)`.
- Un "búfer de apertura" potencial se produce solo cuando las medias móviles se alinean en una configuración de tendencia:
  - **Configuración bajista**: tanto `MA(close)` como `MA(weighted)` caen, la MA de cierre permanece por debajo de la MA ponderada, ambas permanecen por debajo de la MA de apertura, y el búfer de cierre disminuye.
  - **Configuración alcista**: tanto `MA(close)` como `MA(weighted)` suben, la MA de cierre permanece por encima de la MA ponderada, ambas permanecen por encima de la MA de apertura, y el búfer de cierre aumenta.
  - Cuando cualquiera de las configuraciones es verdadera, el búfer de apertura se convierte en `(MA(weighted) − MA(open)) + (MA(close) − MA(weighted))`; de lo contrario se restablece a cero.
- Si un nuevo búfer de apertura positivo acompaña un cruce negativo del búfer de cierre, el búfer de cierre se fuerza a cero, igual que en el código del indicador original.

## Lógica de señales
- **Condiciones de entrada**
  - **Comprar** cuando el búfer de apertura cruza hacia arriba por cero (`anterior ≤ 0`, `actual > 0`).
  - **Vender** cuando el búfer de apertura cruza hacia abajo por cero (`anterior ≥ 0`, `actual < 0`).
  - Las entradas se consideran solo cuando no hay posición existente.
- **Condiciones de salida**
  - **Cerrar largo** cuando el búfer de cierre cruza por debajo de cero (`anterior ≥ 0`, `actual < 0`).
  - **Cerrar corto** cuando el búfer de cierre cruza por encima de cero (`anterior ≤ 0`, `actual > 0`).
  - Las salidas se evalúan antes que las nuevas entradas, por lo que la estrategia nunca mantiene exposición larga y corta simultánea.
- Las órdenes se emiten al mercado usando el `TradeVolume` configurado. La automatización protectora mediante `StartProtection()` refleja la llamada de seguridad en los ejemplos de StockSharp.

## Gráficos y flujo de datos
- Se suscribe al marco temporal definido por `CandleType` y procesa solo velas terminadas.
- Dibuja velas de precio junto con las tres medias móviles y anota órdenes ejecutadas, proporcionando las mismas señales visuales que el indicador original entregaba en MetaTrader.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Marco temporal principal que suministra velas para el indicador y las señales. |
| `MaPeriod` | `int` | `3` | Longitud aplicada a las tres medias móviles. |
| `MaType` | `MaCalculationType` | `Weighted` | Algoritmo de promediado (Simple, Exponencial, Suavizado, Ponderado). |
| `TradeVolume` | `decimal` | `1` | Volumen usado para cada entrada de orden de mercado. |

## Notas de implementación
- Usa el flujo de trabajo de alto nivel `SubscribeCandles().Bind(...)` de StockSharp y los indicadores de media móvil integrados; no se almacenan búferes personalizados más allá de los últimos valores requeridos para la detección de señales.
- Las señales se evalúan solo después de que todos los indicadores estén completamente formados y la estrategia esté lista para trading en vivo (`IsFormedAndOnlineAndAllowTrading()`).
- La estrategia ignora intencionalmente las entradas concurrentes mientras una posición está abierta, coincidiendo fielmente con la lógica del asesor experto de origen.
