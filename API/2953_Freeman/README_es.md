# Estrategia Freeman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Freeman es una estrategia intradía que superpone varios filtros de momentum para escalar en tendencias. Utiliza dos "maestros" RSI impulsados por medias móviles en el marco temporal de trading junto con un filtro de media móvil de mayor marco temporal. El riesgo se controla con objetivos de stop-loss y take-profit basados en ATR más un trailing stop basado en pips.

## Descripción general de la estrategia

- Funciona en cualquier vela del marco temporal seleccionado por el parámetro `CandleType` (15 minutos por defecto).
- Utiliza un filtro horario (`FilterCandleType`) para calificar tendencias antes de que se acepten señales.
- Construye señales largas y cortas a partir de dos bloques RSI que comparan valores actuales y anteriores en combinación con pendientes de medias móviles.
- Permite la piramidación cuando el mercado sigue moviéndose, con la opción de ampliar la siguiente orden después de una salida con pérdidas.

## Lógica de trading

### Condiciones largas

1. El filtro de marco temporal superior es opcional. Cuando está activado, la media móvil horaria debe inclinarse hacia arriba.
2. RSI Maestro #1 está activo cuando:
   - RSI #1 estaba por debajo de `RsiSellLevel` en la barra anterior y sube en la barra actual.
   - La media móvil rápida sube.
   - El RSI horario (período 14) permanece por debajo de `RsiBuyLevel` para confirmar que el marco temporal superior no está sobrecomprado.
3. RSI Maestro #2 está activo cuando:
   - RSI #2 estaba por debajo de `RsiSellLevel2` y gira hacia arriba.
   - La media móvil lenta sube.
   - El RSI horario permanece por debajo de `RsiBuyLevel2`.
4. Se toma una entrada larga cuando al menos un maestro está activo y el filtro de tendencia (si está activado) está de acuerdo.
5. Las entradas largas adicionales requieren que el precio de cierre se mueva más de `DistancePips` (convertido por el paso de precio del instrumento) del último llenado largo. Cuando la última salida larga fue una pérdida, el volumen se multiplica por `LockCoefficient` para imitar el comportamiento de bloqueo de MT5.

### Condiciones cortas

Refleja la lógica larga con comparaciones invertidas:

- La media móvil del marco temporal superior debe declinar cuando el filtro está activado.
- RSI Maestro #1 necesita RSI #1 por encima de `RsiBuyLevel` bajando, la MA rápida cayendo, y el RSI horario por encima de `RsiSellLevel`.
- RSI Maestro #2 necesita RSI #2 por encima de `RsiBuyLevel2` bajando, la MA lenta cayendo, y el RSI horario por encima de `RsiSellLevel2`.
- Las entradas cortas adicionales siguen las mismas reglas de distancia y bloqueo.

## Gestión de posiciones

- El stop-loss y el take-profit se recalculan para cada entrada a partir del valor ATR actual usando `StopLossAtrFactor` y `TakeProfitAtrFactor`.
- El trailing stop se activa una vez que el precio viaja más allá de `TrailingStopPips + TrailingStepPips` y luego bloquea ganancias manteniendo el stop a `TrailingStopPips` del último cierre.
- Las salidas se ejecutan con órdenes de mercado una vez que el máximo/mínimo de la vela supera los niveles de stop o objetivo calculados.
- El parámetro `PositionsMaximum` limita el número total de entradas ejecutadas (largo más corto). Un valor de cero elimina el límite.

## Filtros de tiempo

- El trading los viernes puede desactivarse mediante `TradeOnFriday`.
- `StartHour` y `EndHour` definen una ventana de sesión opcional en tiempo del mercado; los valores cero mantienen el mercado abierto todo el día.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de trading utilizado para la lógica de señal principal. |
| `FilterCandleType` | Marco temporal superior para el filtro de media móvil y RSI (por defecto 1 hora). |
| `FirstMaPeriod` / `SecondMaPeriod` | Períodos para las medias móviles rápidas y lentas que alimentan los maestros RSI. |
| `FilterMaPeriod` | Longitud de la media móvil del marco temporal superior. |
| `MaType` | Tipo de media móvil (SMA, EMA, SMMA o WMA). |
| `RsiFirstPeriod` / `RsiSecondPeriod` | Períodos de los dos maestros RSI. |
| `RsiSellLevel`, `RsiBuyLevel`, `RsiSellLevel2`, `RsiBuyLevel2` | Umbrales RSI que controlan los bloques de maestros. |
| `UseRsiTeacher1`, `UseRsiTeacher2`, `UseTrendFilter` | Interruptores para cada componente. |
| `StopLossAtrFactor`, `TakeProfitAtrFactor` | Multiplicadores ATR para distancias de stop-loss y take-profit. |
| `TrailingStopPips`, `TrailingStepPips` | Offsets en pips para el motor de trailing stop. |
| `PositionsMaximum` | Número máximo de entradas combinadas; cero = ilimitado. |
| `DistancePips` | Distancia mínima en pips antes de agregar a una posición. |
| `TradeOnFriday` | Habilitar o deshabilitar señales los viernes. |
| `StartHour`, `EndHour` | Límites opcionales de la sesión de trading. |
| `LockCoefficient` | Multiplicador de volumen utilizado después de una salida con pérdidas al acumular en la misma dirección. |
| `SignalShift` | Offset aplicado al leer valores de indicadores (0 = barra terminada actual). |

## Notas de implementación

- El porte de StockSharp procesa solo velas terminadas, coincidiendo con el comportamiento "Bars Control" de MT5 incluso cuando el original permitía trading basado en ticks.
- Las distancias de precio expresadas en pips se convierten usando el `PriceStep` del instrumento.
- La lógica de protección (stop, objetivo, trailing) cierra posiciones con órdenes de mercado porque se usan bindings de API de alto nivel en lugar de modificaciones de posición MT5 individuales.
- La estrategia mantiene volúmenes largos y cortos agregados; una vez que un lado se cierra, el seguimiento de pérdidas se reinicia para que la siguiente señal se comporte como las reglas de bloqueo originales.

Use controles de riesgo apropiados y pruebe exhaustivamente antes de implementar en mercados en vivo.
