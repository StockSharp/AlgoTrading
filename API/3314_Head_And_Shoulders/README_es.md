# Estrategia Head and Shoulders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La **estrategia Head and Shoulders** es un port directo del asesor experto de MetaTrader "HEAD AND SHOULDERS" (MQL ID 26066). El robot original combina reconocimiento del patrón cabeza y hombros con filtros de momentum, media móvil y MACD, además de gestionar posiciones con trailing stops, protección de patrimonio y reglas de break-even. Esta implementación StockSharp se centra en la lógica discrecional del motor de entrada y salida mediante la API de alto nivel, proporcionando enlaces limpios a indicadores y gestión automática de riesgo mediante `StartProtection`.

## Lógica de negociación
1. **Detección de patrón**
   - Usa una ventana fractal de cinco barras para aproximar máximos y mínimos de swing, reflejando el reconocimiento de patrones basado en fractales del EA fuente.
   - Confirma un cabeza y hombros *bajista* cuando aparecen tres máximos fractales secuenciales y el máximo central (la cabeza) supera ambos hombros por un umbral de dominancia configurable.
   - Confirma un cabeza y hombros *invertido* cuando se forman tres mínimos fractales secuenciales y el mínimo central está suficientemente por debajo de ambos hombros.
   - La línea de cuello se calcula desde los mínimos fractales más recientes (patrón bajista) o máximos (patrón alcista) situados entre los hombros y la cabeza.
2. **Filtros de momentum y tendencia**
   - Las medias móviles simples rápida y lenta deben alinearse con la dirección de tendencia esperada.
   - El momentum absoluto (diferencia entre el valor actual y el periodo de retrospección) debe superar un umbral y apuntar en la misma dirección que la operación.
   - El valor MACD debe coincidir con la dirección de ruptura para evitar señales contra tendencia.
3. **Ejecución de ruptura**
   - Las operaciones largas se disparan cuando el precio de cierre rompe por encima de la línea de cuello alcista y todos los filtros coinciden.
   - Las operaciones cortas se disparan cuando el cierre rompe por debajo de la línea de cuello bajista bajo filtros bajistas alineados.
4. **Gestión de posición**
   - Las posiciones salen si la línea de cuello se vulnera en la dirección opuesta o si las medias móviles y MACD pierden alineación.
   - Las órdenes protectoras opcionales se configuran mediante el helper integrado `StartProtection`, usando parámetros de stop-loss, take-profit y trailing-stop expresados en pasos de precio.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | Marco 1H | Serie principal de velas para detectar patrones. |
| `OrderVolume` | `1` | Tamaño base de orden. |
| `FastMaLength` / `SlowMaLength` | `6` / `85` | Longitudes de los filtros de tendencia de medias móviles. |
| `MomentumPeriod` | `14` | Periodo de retrospección del indicador momentum. |
| `MomentumThreshold` | `0.3` | Momentum absoluto mínimo requerido para confirmación. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | `12`, `26`, `9` | Configuración MACD. |
| `ShoulderTolerancePercent` | `5` | Desviación máxima permitida entre hombro izquierdo y derecho. |
| `HeadDominancePercent` | `2` | Cantidad mínima por la que la cabeza debe superar cada hombro. |
| `StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps` | `100`, `200`, `0` | Tamaños de órdenes protectoras en pasos de precio (cero desactiva un componente). |

Todos los parámetros creados con `Param()` exponen metadatos para UI y pueden optimizarse mediante el optimizador de StockSharp.

## Diferencias frente al experto original
- Elimina el stop de patrimonio, trailing y rutinas de modificación de órdenes específicos de MetaTrader en favor de mecanismos integrados de protección de cartera de StockSharp.
- Se centra exclusivamente en órdenes de mercado y llamadas de API de alto nivel (`BuyMarket` / `SellMarket`).
- Simplifica funciones auxiliares como alertas, notificaciones push y dibujo de objetos gráficos; la versión StockSharp registra detecciones con `LogInfo`.
- El reconocimiento de patrones mantiene el espíritu de la lógica fractal original, pero se reescribe para evitar acceso directo a arrays de datos y manipulación de tickets de órdenes.

## Notas de uso
- Como la estrategia depende de velas completadas, asegúrese de que las suscripciones de datos entreguen barras terminadas (`CandleStates.Finished`).
- La protección trailing usa pasos de precio; verifique que `Security.PriceStep` refleje el tamaño de tick del instrumento antes de activarla.
- El detector de patrones guarda solo fractales recientes para evitar colecciones sin límite, por lo que es apto para sesiones live largas.
- Para capas de confirmación adicionales (por ejemplo, MACD de marco superior como en el EA original), extienda la estrategia con suscripciones extra usando el mismo enfoque de binding mostrado aquí.

## Referencias
- MetaTrader Expert Advisor: `HEAD AND SHOULDERS.mq4` (MQL ID 26066).
- Documentación de StockSharp sobre [estrategias de alto nivel](https://doc.stocksharp.com/topics/strategy/highlevel.html) y [binding de indicadores](https://doc.stocksharp.com/topics/strategy/highlevel/bind.html).
