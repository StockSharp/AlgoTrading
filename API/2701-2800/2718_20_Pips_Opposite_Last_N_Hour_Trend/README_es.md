# Estrategia de 20 Pips Opuesta a la Tendencia de las Últimas N Horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de StockSharp es un puerto de alto nivel del Asesor Experto MetaTrader
**«20 Pips Opposite Last N Hour Trend»**. Observa velas horarias, mide
cómo se comportó el precio durante las `N` horas anteriores, y luego abre una posición en
la dirección opuesta cuando finaliza la hora de trading configurada. La operación se
gestiona usando un objetivo fijo de take-profit de 20 pips y un tiempo de espera horario, mientras
se aplica una escala de volumen estilo martingala después de pérdidas consecutivas.

La implementación usa las suscripciones de velas de StockSharp, el sistema de parámetros,
y los helpers de órdenes (`BuyMarket`, `SellMarket`) para que pueda ejecutarse sin cambios dentro de
Designer, API, Runner o Shell.

## Lógica de trading

- La estrategia se suscribe al tipo de vela seleccionado (predeterminado: barras de 1 hora).
- Para cada vela terminada mantiene el precio de cierre dentro de un historial interno.
- Cuando una vela con `OpenTime.Hour == TradingHour` se completa y hay suficiente
  historial disponible:
  - Compare el cierre que ocurrió hace `HoursToCheckTrend` barras con el
    cierre anterior (1 barra atrás).
  - Si el precio disminuyó en esa ventana (deriva bajista) la estrategia compra;
    si el precio aumentó (deriva alcista) vende. Cierres iguales omiten el trading.
- Solo se abre una operación por día y exclusivamente en la hora de trading configurada.
  Todas las demás velas se usan puramente para gestión.

## Gestión de posición

- Un objetivo de 20 pips (ajustado para símbolos de 3/5 dígitos) se calcula justo después de la
  entrada. Cuando cualquier vela terminada muestra que el máximo/mínimo tocó el objetivo, la
  posición se cierra en ese nivel.
- Si el objetivo no se alcanza durante la siguiente hora, la posición se cierra al
  final de la siguiente vela para evitar exposición nocturna.
- Los contadores diarios se reinician automáticamente cuando comienza un nuevo día de trading, para que
  la próxima señal elegible pueda dispararse en la siguiente sesión.

## Gestión de dinero

- `Volume` establece el tamaño base de la orden. `MaxVolume` limita el tamaño resultante de cualquier
  paso de martingala.
- Después de una salida perdedora la estrategia aumenta la siguiente posición por el
  multiplicador apropiado: primera pérdida → `FirstMultiplier`, segunda pérdida →
  `SecondMultiplier`, etc. Las rachas perdedoras más allá de cinco operaciones reutilizan el quinto
  multiplicador. Cualquier cierre rentable o en punto de equilibrio reinicia la secuencia.
- Los cálculos de volumen dependen del último precio de posición ejecutado, por lo que la detección de ganancia/pérdida
  permanece determinista incluso sin datos completos de PnL del broker.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `MaxPositions` | 9 | Máximo de operaciones permitidas por día. Establezca en 0 para deshabilitar el trading. |
| `Volume` | 0.1 | Volumen base para la primera operación de una racha. |
| `MaxVolume` | 5 | Límite máximo para el volumen ajustado después de multiplicadores. |
| `TakeProfitPips` | 20 | Distancia de take-profit en pips. 0 deshabilita el TP. |
| `TradingHour` | 7 | Hora del día (0-23) habilitada para abrir una posición. |
| `HoursToCheckTrend` | 24 | Número de cierres horarios usados para medir la tendencia previa. |
| `FirstMultiplier` | 2 | Multiplicador aplicado después de la primera pérdida consecutiva. |
| `SecondMultiplier` | 4 | Multiplicador aplicado después de la segunda pérdida consecutiva. |
| `ThirdMultiplier` | 8 | Multiplicador aplicado después de la tercera pérdida consecutiva. |
| `FourthMultiplier` | 16 | Multiplicador aplicado después de la cuarta pérdida consecutiva. |
| `FifthMultiplier` | 32 | Multiplicador aplicado desde la quinta pérdida en adelante. |
| `CandleType` | H1 | Tipo de datos de vela usado para la generación de señales y gestión. |

## Notas adicionales

- El tamaño del pip se calcula a partir de `Security.PriceStep` y el número de decimales para que
  el objetivo de 20 pips funcione correctamente en símbolos FX de 4 y 5 dígitos.
- `StartProtection()` se invoca cuando la estrategia comienza, habilitando las protecciones integradas
  de StockSharp (stop automático para posiciones sin límite, reinicios de cartera).
- La lógica usa solo velas terminadas y nunca lee valores de indicadores
  directamente, cumpliendo con las directrices de `AGENTS.md`.

> **Descargo de responsabilidad de riesgo:** El dimensionamiento de posición estilo martingala puede llevar a
> drawdowns sustanciales. Siempre pruebe los parámetros en datos históricos y use límites de riesgo prudentes
> antes de implementar en trading en vivo.
