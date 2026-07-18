# Estrategia SilverTrend Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia SilverTrend Duplex** es un port de StockSharp del asesor experto de MetaTrader 5 `Exp_SilverTrend_Duplex`. El robot original combina dos filtros SilverTrend independientes (para decisiones largas y cortas) y ejecuta trades cuando los colores del indicador cambian entre estados alcistas y bajistas. Esta implementación en C# mantiene la arquitectura de doble filtro, permitiéndole ajustar la lógica larga y corta por separado mientras aprovecha la API de alto nivel de StockSharp.

La estrategia opera solo en velas finalizadas. Se pueden configurar dos suscripciones separadas, por lo que las señales largas y cortas pueden observar diferentes marcos temporales o instrumentos si es necesario. Internamente, un `SilverTrendIndicator` personalizado reconstruye la lógica de color de la versión MQL combinando extremos del canal Donchian con el multiplicador de riesgo para emular las bandas SilverTrend originales.

## Lógica de trading

1. **Reconstrucción del indicador**
   - Para cada vela se calculan los límites superior e inferior de Donchian sobre `SSP` barras.
   - Los umbrales adaptativos `smin` y `smax` se derivan usando el coeficiente de riesgo (`33 - risk`), idéntico al algoritmo MQL.
   - Cuando el precio cierra por encima de `smax` se registra un estado alcista, cuando cierra por debajo de `smin` se registra un estado bajista; de lo contrario se retiene el estado anterior. La dirección del cuerpo de la vela determina el código de color final (0..4) exactamente como en el indicador SilverTrend original.

2. **Preparación de señales**
   - Los valores de color se almacenan para las `SignalBar + 1` velas finalizadas más recientes tanto para los filtros largo como corto.
   - Las señales largas se activan cuando el color en el desplazamiento seleccionado cae por debajo de `2` (alcista) mientras el color anterior era mayor que `1` (no alcista), replicando `Value[1] < 2 && Value[0] > 1` de MQL.
   - Las señales cortas se activan cuando el color sube por encima de `2` (bajista) y el color anterior está por encima de `0`, coincidiendo con `Value[1] > 2 && Value[0] > 0` del script.

3. **Ejecución de órdenes**
   - Las entradas usan `BuyMarket` o `SellMarket` con un volumen igual a `Volume + |Position|`, lo que tanto cierra cualquier exposición opuesta como abre el nuevo lado en una sola orden de mercado.
   - Las salidas dependen de que el indicador revierta a la banda de color opuesta. Las posiciones largas se cierran cuando el color sube por encima de `2`, las posiciones cortas cuando cae por debajo de `2`.

La estrategia no replica la matriz de gestión monetaria original ni la colocación de stops en el servidor de `TradeAlgorithms.mqh`. Por lo tanto, el control de riesgo debe gestionarse a través de los mecanismos de protección integrados de StockSharp o las reglas del corredor.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `LongCandleType` | Velas de 4 horas | Tipo de datos usado para el indicador del lado largo. |
| `LongSsp` | 9 | Longitud de retrospectiva SilverTrend para el filtro largo. |
| `LongRisk` | 3 | Multiplicador de riesgo (`33 - risk`) aplicado al ancho del canal. |
| `LongSignalBar` | 1 | Desplazamiento (en velas finalizadas) para evaluar señales largas. Debe ser ≥ 1. |
| `EnableLongEntries` | true | Activa/desactiva la apertura de posiciones largas. |
| `EnableLongExits` | true | Activa/desactiva el cierre de posiciones largas cuando aparecen colores bajistas. |
| `ShortCandleType` | Velas de 4 horas | Tipo de datos usado para el indicador del lado corto. |
| `ShortSsp` | 9 | Longitud de retrospectiva SilverTrend para el filtro corto. |
| `ShortRisk` | 3 | Multiplicador de riesgo para el filtro corto. |
| `ShortSignalBar` | 1 | Desplazamiento para evaluar señales cortas. Debe ser ≥ 1. |
| `EnableShortEntries` | true | Activa/desactiva la apertura de posiciones cortas. |
| `EnableShortExits` | true | Activa/desactiva el cierre de posiciones cortas cuando aparecen colores alcistas. |
| `Volume` | 1 | Volumen base de la orden usado para las entradas. |

## Notas de implementación

- Las señales se evalúan solo después de que tanto el indicador como el historial de colores contienen suficientes datos (`SignalBar + 1` valores). Esto refleja las verificaciones `BarsCalculated` del experto MQL.
- El indicador personalizado expone valores de color decimales en lugar de copiar datos de buffer sin procesar. No se requieren llamadas directas a `GetValue` gracias a la API de alto nivel `Bind`.
- Cuando los tipos de vela largo y corto son idénticos, se crean dos suscripciones intencionalmente para mantener los conjuntos de parámetros aislados. Esto coincide con el comportamiento de doble manejador en el asesor original.
- Las opciones de stop-loss, take-profit, desviación y gestión de margen del script fuente no están replicadas. Puede añadir reglas de riesgo de StockSharp (p.ej. `StopLossRule`) si se necesita un comportamiento similar.

## Consejos de uso

- Optimice `LongSsp`, `ShortSsp` y los valores de riesgo correspondientes por separado para adaptar los umbrales de ruptura a cada régimen de mercado.
- Si desea emular el comportamiento original de "señal en la barra anterior", mantenga `SignalBar` en `1`. Los valores más grandes obligan a la estrategia a esperar barras adicionales antes de reaccionar.
- Combine la estrategia con controles de riesgo a nivel de cartera o filtros de tiempo cuando opere en múltiples instrumentos, ya que el cambio de color SilverTrend puede producir cambios de régimen frecuentes en mercados laterales.
