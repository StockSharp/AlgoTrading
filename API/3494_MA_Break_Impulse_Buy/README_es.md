# Estrategia de compra por impulso de ruptura MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el asesor experto "M.A break mt4 buy" utilizando el API de alto nivel de StockSharp. Se centra en identificar fuertes rupturas alcistas después de una consolidación silenciosa. La lógica de entrada busca una secuencia de filtros de media móvil exponencial (EMA), una fase de mercado tranquila y luego una poderosa vela de impulso alcista que interactúa con una ruptura EMA. La estrategia abre únicamente posiciones **largas**.

## Lógica de trading
1. **EMA Filtros de tendencias**
   - Se evalúan dos pares EMA en la vela completada anterior (`shift = 1`).
   - `EMA(FirstFastPeriod)` debe ser mayor que `EMA(FirstSlowPeriod)`.
   - `EMA(SecondFastPeriod)` debe ser mayor que `EMA(SecondSlowPeriod)`.
2. **Selección de velas de impulso**
   - La vela de impulso es la última barra completa (desplazamiento 1).
   - Su precio de apertura debe estar por encima del `TrendMaPeriod` EMA.
   - Su mínimo debe tocar o caer por debajo del `BreakoutMaPeriod` EMA.
   - La vela debe ser alcista (`Close > Open`).
   - El rango de la vela debe estar entre `CandleMinSize` y `CandleMaxSize` (convertido de pips usando `Security.PriceStep`).
   - La mecha superior no debe exceder el `UpperWickLimit` por ciento del rango de la vela. La mecha inferior debe ser al menos el `LowerWickFloor` por ciento del rango.
3. **Barras silenciosas y fuerza de impulso**
   - La estrategia escanea `QuietBarsCount` velas que preceden a la vela de impulso (desplazamientos ≥ 2) y registra el rango máximo-mínimo.
   - Este rango silencioso debe ser mayor que `QuietBarsMinRange` (pips → precio).
   - El cuerpo de la vela de impulso (`Close - Open`) debe ser al menos `ImpulseStrength × quietRange`.
4. **Gestión de posiciones**
   - Se envía una orden de compra de mercado cuando se cumplen todas las condiciones y no hay ninguna posición abierta actualmente.
   - Las órdenes protectoras de stop-loss y take-profit se gestionan a través de `StartProtection`, utilizando entradas de pips convertidas a través de `Security.PriceStep`.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `FirstFastPeriod` | 20 | EMA rápida utilizado en el primer filtro de tendencias. |
| `FirstSlowPeriod` | 30 | EMA lenta utilizado en el primer filtro de tendencia. |
| `SecondFastPeriod` | 30 | EMA rápida para el segundo filtro de tendencias. |
| `SecondSlowPeriod` | 50 | EMA lenta para el segundo filtro de tendencia. |
| `TrendMaPeriod` | 30 | EMA que debe exceder la apertura de la vela de impulso. |
| `BreakoutMaPeriod` | 20 | EMA que debe tocar el mínimo de la vela de impulso. |
| `QuietBarsCount` | 2 | Número de velas en calma antes de que se evalúe el impulso. |
| `QuietBarsMinRange` | 0.0 | Rango mínimo de silencio (pips). |
| `ImpulseStrength` | 1.1 | Multiplicador aplicado al rango silencioso para validar el tamaño del cuerpo del impulso. |
| `UpperWickLimit` | 100.0 | Mecha superior máxima como porcentaje del rango de la vela. |
| `LowerWickFloor` | 0.0 | Mecha inferior mínima como porcentaje del rango de la vela. |
| `CandleMinSize` | 0.0 | Rango mínimo permitido de la vela de impulso en pips. |
| `CandleMaxSize` | 100.0 | Rango máximo permitido de la vela de impulso en pips. |
| `VolumeSize` | 0,01 | Volumen comercial enviado con `BuyMarket`. Normalizado para intercambiar `VolumeStep`. |
| `StopLossPips` | 20.0 | Distancia de stop-loss en pips (convertida con `PriceStep`). |
| `TakeProfitPips` | 20.0 | Distancia de obtención de beneficios en pips (convertida con `PriceStep`). |
| `CandleType` | plazo de 15 minutos | Tipo de datos de vela solicitados desde el conector. |

## Notas de implementación
- La estrategia utiliza StockSharp suscripciones de alto nivel `Bind` para mantener los cálculos de los indicadores sincronizados con las actualizaciones de las velas.
- Todos los cálculos se basan únicamente en velas terminadas (`CandleStates.Finished`).
- Los filtros de rango silencioso y de tamaño de vela convierten internamente los valores de pips en unidades de precio usando `Security.PriceStep`. Si el instrumento no informa `PriceStep`, se utiliza un respaldo de `1`, que coincide con la lógica MQL de multiplicar por el valor del pip.
- `StartProtection` se activa una vez durante `OnStarted`, por lo que cada nueva posición recibe el stop-loss y el take-profit configurados.
- El búfer del historial de velas mantiene solo las últimas `QuietBarsCount + 3` entradas para evaluar el período de calma e impulsar la vela de manera eficiente.

## Consejos de uso
- Asegúrese de que el instrumento conectado proporcione `PriceStep`, `VolumeStep` y límites de volumen para que las conversiones de pips y volumen sigan siendo precisas.
- Ajusta EMA períodos y parámetros de impulso a la volatilidad del instrumento. Un `ImpulseStrength` más bajo reaccionará a rupturas más pequeñas, mientras que un valor más alto filtra solo los movimientos más fuertes.
- La estrategia está diseñada para una posición abierta a la vez. Las posiciones externas sobre el mismo valor pueden impedir nuevas entradas.
