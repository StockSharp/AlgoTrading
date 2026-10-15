# Estrategia MACD PatternTrader de Sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión directa del asesor experto de MetaTrader **MacdPatternTraderAll0.01**. Opera un único instrumento usando seis patrones de entrada diferentes basados en MACD, filtrado opcional de horas de trading, toma parcial de ganancias y una opción de dimensionamiento de posición martingala. Todos los cálculos se realizan en velas completadas entregadas por el `CandleType` configurado.

## Lógica de trading
1. En cada vela terminada la estrategia actualiza seis indicadores MACD (cada patrón tiene sus propias longitudes de EMA rápidas y lentas y una línea de señal de un período).
2. Si el filtrado de horas de trading está habilitado, las nuevas operaciones solo se evalúan entre `SessionStart` y `SessionEnd`. La gestión de riesgos siempre está activa.
3. Cada patrón MACD verifica relaciones de valores muy específicas entre el valor MACD actual y los dos valores anteriores para detectar reversiones de momentum. Cuando se activa un patrón envía una orden de mercado en la dirección correspondiente y establece niveles internos de stop-loss y take-profit.
4. El stop-loss se calcula como el extremo reciente (máximo más alto para cortos, mínimo más bajo para largos) de un lookback configurable más/menos un offset medido en pasos de precio. El take-profit escanea grupos más antiguos de velas en bloques para replicar la búsqueda recursiva de objetivo del asesor experto original.
5. Solo se gestiona una posición neta a la vez. Si aparece una nueva señal en dirección opuesta, la posición actual se cierra y se abre una posición inversa con el volumen ajustado por martingala.
6. Las posiciones activas son monitoreadas por `ManageActivePosition`. La lógica emula la rutina de cierre parcial original:
   - Para largos: cuando el beneficio supera `ProfitThreshold` (5 unidades monetarias) y el cierre anterior está por encima de la EMA a medio plazo, se vende un tercio de la posición. Si el beneficio persiste y el máximo anterior está por encima del promedio de la SMA larga y la EMA muy lenta, se cierra la mitad de la posición restante.
   - Para cortos: las reglas simétricas cierran un tercio y luego la mitad de la posición restante cuando se cumplen los objetivos de beneficio y los filtros de media móvil.
7. La gestión de riesgos se ejecuta en cada vela independientemente de la ventana de trading. Si el precio perfora el nivel de stop-loss o take-profit almacenado dentro de una vela (basándose en máximo/mínimo), toda la posición se cierra al precio de ruptura.
8. Después de que una operación se cierra completamente se evalúa el PnL realizado. Cuando `UseMartingale` está habilitado, una operación perdedora duplica el volumen del siguiente orden, mientras que cualquier salida rentable restablece el volumen al `LotSize` base.

## Patrones clave
- **Patrón 1:** Detecta picos del MACD por encima de `Pattern1MaxThreshold` que empiezan a bajar, y caídas por debajo de `Pattern1MinThreshold` que rebotan.
- **Patrón 2:** Busca cruces del MACD alrededor de la línea cero con excursiones mínimas.
- **Patrón 3:** Usa umbrales de dos niveles (`Pattern3MaxThreshold`, `Pattern3SecondaryMax`, `Pattern3MinThreshold`, `Pattern3SecondaryMin`) para detectar reversiones de tres pasos en ambos lados. También cuenta barras consecutivas por encima del máximo secundario para imitar la acumulación `bars_bup` original.
- **Patrón 4:** Opera cuando el MACD supera los umbrales primarios pero la barra anterior se sitúa dentro del rango secundario más estrecho, anticipando reversiones.
- **Patrón 5:** Responde a rápidos giros del MACD dentro de rangos estrechos definidos por `Pattern5PrimaryMax/Min` y los límites secundarios.
- **Patrón 6:** Usa contadores (`Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6CountBars`) para requerir múltiples excursiones MACD consecutivas antes de activar una operación.

## Gestión de riesgos
- Los objetivos internos de stop-loss y take-profit se recalculan para cada entrada. Los stops usan extremos de precio más un offset medido en pasos de precio. El take-profit busca bloques consecutivos de velas hasta que un extremo no mejora, reproduciendo la lógica recursiva del experto MQL.
- Las salidas parciales respetan el tamaño mínimo de lote original (0.01) y llevan un registro de cuántos cierres parciales se han ejecutado por dirección.
- La estrategia nunca coloca órdenes protectoras en el bróker; en cambio monitorea los máximos y mínimos de las velas para cerrar posiciones a los precios configurados.

## Parámetros
| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizada para indicadores y señales de trading. | Velas de 1 hora |
| `LotSize` | Volumen de operación base antes de los ajustes martingala. | 0.1 |
| `UseTimeFilter` | Habilitar el trading solo entre `SessionStart` y `SessionEnd`. | true |
| `SessionStart` / `SessionEnd` | Ventana de trading (hora local de la bolsa). | 07:00 / 17:00 |
| `UseMartingale` | Duplicar `LotSize` después de una operación perdedora. | true |
| `Ema1Period`, `Ema2Period`, `SmaPeriod`, `Ema3Period` | Medias móviles utilizadas para salidas parciales. | 7, 21, 98, 365 |
| Parámetros específicos de patrón | Cada patrón tiene su propia bandera de habilitación, lookbacks de stop-loss/take-profit, offsets, longitudes de EMA y valores de umbral que coinciden con las entradas del experto original. | Ver valores predeterminados del constructor |

Todos los umbrales y longitudes de EMA están expuestos a través de objetos `StrategyParam`, permitiendo optimización o ajuste fino.

## Notas
- La estrategia asume que el instrumento proporciona `PriceStep` y `PriceStepCost` para traducir offsets y beneficios a la moneda de la cuenta. Cuando no está disponible, las diferencias de precio se usan directamente.
- Los stops y objetivos se simulan internamente; se evaluarán al cierre de la barra. La ejecución intrabar en tiempo real puede diferir del comportamiento de MetaTrader.
- El mecanismo martingala puede aumentar rápidamente la exposición después de una racha perdedora—úselo con precaución.
