# Estrategia de Túnel Seguro FxNode
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader 4 *FxNode - Safe Tunnel*. El sistema utiliza un canal de tendencia basado en ZigZag: los máximos de oscilación más recientes se conectan para formar una línea de resistencia, mientras que los mínimos crean una línea de soporte. Se abre una posición cuando el precio de mercado toca uno de los límites del canal dentro de una tolerancia configurable y pasan todas las verificaciones de seguridad.

La conversión sigue el flujo de trabajo original pero lo adapta a la API de alto nivel de StockSharp:

- La suscripción a velas impulsa la lógica. Solo se procesan velas completamente formadas.
- Un par `Highest`/`Lowest` emula el detector ZigZag utilizado para trazar las líneas de tendencia del túnel.
- Un indicador `AverageTrueRange` proporciona el ancla de stop basada en volatilidad que la versión MQL producía con `ATRCheck() * 10`.
- Las cotizaciones Level1 se monitorean para que la estrategia pueda aplicar un spread máximo antes de permitir nuevas operaciones.

## Lógica de entrada

1. Detectar máximos y mínimos de oscilación con una profundidad ZigZag configurable, desviación (en pips) y backstep. Los dos máximos y dos mínimos más recientes definen las líneas de tendencia.
2. Calcular el precio de cada línea de tendencia en el tiempo de cierre de la vela actual y medir la distancia vertical entre el último máximo y mínimo de oscilación.
3. Configuración larga: el mejor precio ask debe permanecer por encima de la línea de tendencia inferior pero no más lejos que el buffer `TouchDistanceBuyPips`. Los cortos replican la condición alrededor de la línea de tendencia superior y el mejor bid.
4. El filtro de sesión opcional (por defecto medianoche–06:00) debe permitir el trading. La estrategia también bloquea nuevas órdenes el viernes, sábado y domingo, imitando las restricciones originales de `AllowToOrder()`.
5. El spread actual (ask – bid) no debe exceder `MaxSpreadPips` cuando hay cotizaciones disponibles.
6. `MaxOpenPositions` controla la exposición neta máxima. Dado que StockSharp usa netting, este valor actúa como tope en el volumen total de posición en lugar de en tickets separados.

## Lógica de salida

- Stop-loss inicial: el EA original lo colocaba en `ATR * 10`. El port mantiene el mismo multiplicador respetando el tope `MaxStopLossPips`.
- Take-profit inicial: por defecto es la distancia entre el último máximo y mínimo de oscilación, pero está limitado por `TakeProfitPips` cuando está configurado.
- Objetivo de beneficio fijo: si `FixedTakeProfitPips` es mayor que cero, la posición se cierra una vez que el precio gana al menos esa cantidad de pips desde la entrada.
- Trailing stop: una vez que el cierre de la vela se mueve más de `TrailingStopPips` a favor de la operación, el stop-loss se ajusta para asegurar beneficios.
- Salida de fin de semana: cuando `CloseBeforeWeekend` está habilitado, cualquier posición abierta se cierra después de las 23:50 del viernes.

Todas las salidas se ejecutan con órdenes de mercado para mantener la consistencia con el comportamiento original.

## Riesgo y dimensionamiento

El tamaño del lote se calcula en tres etapas:

1. Intentar arriesgar `RiskPercentage` del valor del portafolio, asumiendo que se conocen tanto el paso de precio del instrumento como el valor monetario del paso.
2. Si el dimensionamiento por riesgo no puede calcularse, recurrir a `StaticVolume`.
3. Limitar el volumen final entre `MinVolume` y `MaxVolume`.

Dado que StockSharp reporta una sola posición neta por instrumento, el límite original de `MaxOpenPosition` se interpreta como una exposición total máxima en lugar de un conteo de tickets independientes.

## Parámetros

| Nombre | Predeterminado | Descripción |
|--------|----------------|-------------|
| `CandleType` | Velas de 30 minutos | Marco temporal principal para análisis y trading. |
| `TrendPreference` | Ambos | Elegir trading solo largo, solo corto o simétrico. |
| `TakeProfitPips` | 800 | Distancia máxima de take-profit en pips (0 desactiva el límite). |
| `MaxStopLossPips` | 200 | Distancia máxima de stop-loss en pips (0 desactiva el límite). |
| `FixedTakeProfitPips` | 0 | Distancia de salida anticipada expresada en pips. |
| `TouchDistanceBuyPips` | 20 | Las entradas largas requieren que el precio ask permanezca dentro de este buffer por encima de la línea de tendencia inferior. |
| `TouchDistanceSellPips` | 20 | Las entradas cortas replican el requisito de buffer cerca de la línea de tendencia superior. |
| `TrailingStopPips` | 50 | Distancia de trailing aplicada después de que la operación se vuelve rentable. |
| `StaticVolume` | 1 | Volumen de orden alternativo cuando el dimensionamiento basado en riesgo no es posible. |
| `MinVolume` / `MaxVolume` | 0.02 / 10 | Límites para el volumen final de la orden. |
| `MaxSpreadPips` | 15 | Spread máximo permitido en pips para nuevas entradas. |
| `RiskPercentage` | 30 | Porcentaje del portafolio arriesgado por operación. Establecer en 0 para usar siempre `StaticVolume`. |
| `MaxOpenPositions` | 1 | Exposición neta máxima (en múltiplos del volumen de orden actual). |
| `UseTimeFilter` | true | Habilita la ventana de trading. |
| `SessionStart` / `SessionEnd` | 00:00 / 06:00 | Ventana de trading. Cuando el inicio es posterior al fin, la ventana cruza la medianoche. |
| `CloseBeforeWeekend` | true | Cerrar cualquier posición después de las 23:50 del viernes. |
| `AtrPeriod` | 14 | Período de lookback del ATR utilizado para el cálculo del stop. |
| `ZigZagDepth` | 5 | Profundidad de lookback del ZigZag. |
| `ZigZagDeviationPips` | 3 | Distancia mínima entre pivotes consecutivos (en pips). |
| `ZigZagBackstep` | 1 | Barras entre pivotes elegibles. |
| `ZigZagHistory` | 10 | Número de pivotes almacenados para la proyección de líneas de tendencia. |

## Notas y limitaciones

- La reconstrucción del ZigZag replica el comportamiento MQL combinando los indicadores `Highest`/`Lowest` con filtros de desviación y backstep. Si el instrumento opera en una sesión personalizada, considere ajustar los parámetros para alinearlos con el indicador original.
- El filtrado de spread requiere cotizaciones bid/ask en tiempo real. Cuando las cotizaciones están ausentes (por ejemplo durante el backtesting con datos solo de velas) el filtro de spread se omite.
- El port opera con posiciones netas. Los entornos que requieren gestión independiente de tickets deben extender la estrategia para rastrear cada ejecución por separado.
- Las cadenas de tiempo de la versión MQL (p. ej., `"24:00"`) se reemplazan con parámetros `TimeSpan`. Para reproducir una sesión nocturna, establezca el inicio posterior al fin, por ejemplo 23:30 a 05:30.

## Uso

1. Adjuntar la estrategia a un instrumento, configurar el tipo de vela y los parámetros, y ejecutarla en modo simulación o en vivo.
2. Asegurarse de que las suscripciones de profundidad de mercado o Level1 estén habilitadas para aplicar el filtro de spread con precisión.
3. Revisar y ajustar los controles de riesgo antes de operar con capital real.
