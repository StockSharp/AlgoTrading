# Estrategia Exp Hans Indicator Sistema de Nube Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Exp Hans Indicator Sistema de Nube Tm Plus es una estrategia de breakout basada en sesiones que reproduce el comportamiento del asesor experto MQL5 original. El algoritmo monitorea los estados de color producidos por el indicador Hans en un marco temporal configurable. Abre una nueva posición después de que un breakout alcista (colores 0/1) o bajista (colores 3/4) termina y el precio regresa dentro del canal. La implementación mantiene todas las decisiones de trading en velas cerradas, usa límites de riesgo basados en pips, y refleja la regla de liquidación basada en tiempo de la versión MQL.

La estrategia opera en un único par instrumento/feed de velas obtenido de `GetWorkingSecurities()`. Todos los tamaños de orden se derivan de la propiedad `Volume` de la estrategia y la fracción de gestión de dinero expuesta por los parámetros.

## Lógica del indicador
1. Las marcas de tiempo de las velas se convierten del tiempo del broker (`LocalTimeZone`) a la zona horaria de destino (`DestinationTimeZone`). Por defecto el script trabaja con GMT+4, que coincide con la implementación de referencia.
2. Se recopilan dos rangos de sesión de Londres cada día de trading:
   - **Rango 1**: 04:00–08:00 hora de destino. El máximo/mínimo de este período se convierte en el canal de breakout inicial.
   - **Rango 2**: 08:00–12:00 hora de destino. Una vez completado, reemplaza al primer rango por el resto del día.
3. Cada rango se extiende por `PipsForEntry` pips en ambos lados. Un pip es igual al `PriceStep` del instrumento, multiplicado por 10 cuando el valor tiene 3 o 5 decimales (pips fraccionados estilo MetaTrader).
4. Los colores de las velas se derivan exactamente como en el indicador:
   - Cierre por encima de la banda superior → color `0` (cierre alcista) o `1` (cierre bajista).
   - Cierre por debajo de la banda inferior → color `4` (cierre bajista) o `3` (cierre alcista).
   - Cierre dentro del canal → color neutro `2`.

## Reglas de trading
- **Entrada**: Cuando la vela cerrada anterior tuvo un color alcista (0/1) y la más reciente no es alcista, la estrategia abre una posición larga (si está habilitada). Simétricamente, un color bajista anterior (3/4) seguido de un color neutral/contrario activa una entrada corta.
- **Salida**:
  - Salida direccional cuando el color anterior se vuelve en contra de la posición actual (0/1 para cortos, 3/4 para largos).
  - Salida opcional basada en tiempo una vez que el período de tenencia supera `HoldingMinutes`.
  - Niveles opcionales de stop-loss / take-profit expresados en puntos (`StopLossPoints`, `TakeProfitPoints`). Los niveles se omiten si el valor no expone un `PriceStep` positivo.
- Las salidas se procesan antes que las nuevas entradas, por lo que una posición se aplana antes de que se envíe una orden de reversión.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `MoneyManagement` | Fracción del `Volume` de la estrategia usada por operación. Valores ≤ 0 recurren al volumen completo. | `0.1` |
| `MoneyMode` | Marcador de posición para los modos de gestión de dinero originales. Actualmente solo se aplica `Lot`. | `Lot` |
| `StopLossPoints` / `TakeProfitPoints` | Stop protector y objetivo de beneficio expresados en puntos (pips). Establecer en `0` para deshabilitar. | `1000` / `2000` |
| `DeviationPoints` | Desviación máxima de ejecución aceptable en puntos. Presente por compatibilidad; no aplicado por la capa de órdenes de StockSharp. | `10` |
| `AllowBuyEntries` / `AllowSellEntries` | Habilita entradas largas/cortas. | `true` |
| `AllowBuyExits` / `AllowSellExits` | Habilita salidas automatizadas para posiciones largas/cortas. | `true` |
| `UseTimeExit` | Activa el filtro de liquidación basado en tiempo. | `true` |
| `HoldingMinutes` | Tiempo máximo de tenencia para cualquier posición en minutos. | `1500` |
| `PipsForEntry` | Offset de pip añadido por encima/debajo de los rangos de breakout. | `100` |
| `SignalBar` | Offset de vela cerrada usado para señales. Use valores ≥ 1 para mantenerse alineado con la lógica MT5. | `1` |
| `LocalTimeZone` | Zona horaria del broker/servidor (horas desde UTC). | `0` |
| `DestinationTimeZone` | Zona horaria objetivo usada para los límites de sesión. | `4` |
| `CandleType` | Marco temporal usado para los cálculos de Hans. | Velas de `30m` |

## Gestión del dinero y ejecución
- Tamaño de orden = `Volume * MoneyManagement`, normalizado al `VolumeStep` del instrumento. Si el valor calculado es no positivo, la lógica recurre a un paso de volumen.
- Cuando aparece una señal de reversión, la estrategia envía una única orden de mercado igual al nuevo volumen más cualquier cantidad opuesta abierta. Esto reproduce el comportamiento de `BuyPositionOpen`/`SellPositionOpen` del helper MQL.
- Los niveles de stop-loss y take-profit se recalculan en cada entrada y se borran cuando se cierra o revierte una posición.

## Pautas de uso
1. Adjunte la estrategia a un valor que publique metadatos válidos de `PriceStep`, `Decimals` y `VolumeStep`.
2. Establezca el `Volume` deseado en la estrategia antes de iniciarla. La fracción de gestión de dinero se aplicará encima.
3. Elija un tipo de vela igual al usado en MetaTrader (M30 por defecto). Todos los cálculos se basan en velas completadas.
4. Alinee las zonas horarias si su fuente de datos de mercado difiere del tiempo de destino GMT+4 predeterminado usado por el indicador Hans.
5. Monitoree los registros para ver mensajes sobre tamaño de pip faltante; los niveles de riesgo se omitirán cuando no haya `PriceStep` disponible.

## Notas de implementación
- La detección de color se realiza exclusivamente en velas finalizadas a través de la API de alto nivel `SubscribeCandles`, evitando buffers de indicadores manuales.
- Los niveles de breakout se recomputan una vez por vela y se almacenan en caché en memoria; no se crean colecciones históricas.
- `DeviationPoints` se retiene para completitud de configuración pero no se puede aplicar con órdenes de mercado simples en StockSharp.
- La estrategia reinicia su estado interno en `OnReseted()` para soportar backtests repetidos sin datos de sesión obsoletos.

## Limitaciones
- La implementación actual solo admite `SignalBar ≥ 1`, coincidiendo con el comportamiento original del EA en eventos de nueva barra. Usar `0` requeriría acceso a nivel de tick que no está presente en el port de alto nivel.
- Los modos de gestión de dinero distintos a `Lot` no están implementados. Extienda `GetOrderVolume()` si su flujo de trabajo depende del dimensionamiento basado en balance.
- Sin un valor válido de `PriceStep`, las distancias basadas en pips (stop, take-profit, offsets de Hans) no se pueden calcular y serán ignoradas.
