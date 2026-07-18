# Estrategia de Canal Plano (2684)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión en C# del asesor experto de MetaTrader 5 *Flat Channel (edición barabashkakvn)*. Detecta períodos de baja volatilidad (un canal "plano") usando el indicador de Desviación Estándar y coloca órdenes stop de ruptura en los límites del canal. Cuando el precio rompe el rango plano, la orden stop correspondiente se activa, mientras que la opuesta se cancela para evitar quedar atrapado en ambos lados del mercado.

## Lógica principal

1. **Filtro de volatilidad** – la estrategia se suscribe a velas y calcula la Desviación Estándar del precio mediano. Una fase plana se confirma cuando el valor sigue cayendo durante al menos `FlatBars` velas consecutivas.
2. **Construcción del canal** – una vez confirmada la fase plana, se rastrean el máximo más alto y el mínimo más bajo del rango plano. El ancho del canal debe mantenerse entre `ChannelMinPips` y `ChannelMaxPips` (convertidos a unidades de precio mediante el tamaño de tick del instrumento).
3. **Órdenes de entrada** – mientras el precio opera dentro del canal, la estrategia coloca:
   - Un buy stop en el máximo del canal con stop-loss `2 × ancho del canal` por debajo de la entrada y take-profit `1 × ancho del canal` por encima.
   - Un sell stop en el mínimo del canal con las distancias simétricas de stop-loss/take-profit.
4. **Vida útil de la orden** – las órdenes stop pendientes expiran después de `OrderLifetimeSeconds`. Cuando transcurre el tiempo, se cancelan y pueden recrearse si las condiciones planas se mantienen.
5. **Gestión de posición** – después de que una orden de entrada se ejecuta, la orden stop opuesta se cancela y se registran órdenes de protección nuevas (stop-loss y take-profit). La lógica opcional de punto de equilibrio mueve el stop-loss al precio de entrada una vez que el precio recorre una fracción Fibonacci (`FiboTrail`) de la distancia hacia el objetivo de take-profit.
6. **Ventana de trading** – el filtro `UseTradingHours` restringe la actividad por día de la semana y por horas específicas del lunes/viernes, emulando los controles de horario del EA original.

## Indicadores

- **StandardDeviation** (precio mediano, longitud = `StdDevPeriod`) – detecta la caída de la volatilidad.
- **DonchianChannels** (longitud = `FlatBars`) – proporciona los límites iniciales de máximo/mínimo para el canal plano.

## Riesgo y gestión de capital

- `FixedVolume` define el tamaño del lote cuando `UseMoneyManagement` está deshabilitado.
- Cuando `UseMoneyManagement` está habilitado, el tamaño de la posición se estima a partir de `RiskPercent` del valor actual del portafolio dividido por la distancia del stop-loss expresada en dinero usando `PriceStep` y `StepPrice`.
- Después de una operación perdedora, la siguiente posición usa `FixedVolume × 4`, replicando el comportamiento de recuperación del EA original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `UseTradingHours` | Habilitar o deshabilitar el filtro de horario por día de la semana/hora. |
| `TradeTuesday`, `TradeWednesday`, `TradeThursday` | Permitir el trading en días individuales entre semana (el lunes y el viernes siempre están permitidos pero controlados por los límites horarios). |
| `MondayStartHour`, `FridayStopHour` | Hora de inicio el lunes y hora de corte el viernes (reloj de 24h). |
| `UseMoneyManagement`, `RiskPercent`, `FixedVolume` | Opciones de gestión de capital descritas anteriormente. |
| `OrderLifetimeSeconds` | Tiempo de vencimiento para las órdenes de entrada pendientes (0 = sin vencimiento). |
| `StdDevPeriod`, `FlatBars` | Configuraciones del indicador que controlan la detección de la fase plana. |
| `ChannelMinPips`, `ChannelMaxPips` | Ancho de canal permitido expresado en pips (convertido usando el tamaño de tick del instrumento). |
| `UseBreakeven`, `FiboTrail` | Habilitar la lógica de punto de equilibrio y establecer el multiplicador Fibonacci usado para activar el ajuste del stop. |
| `CandleType` | Tipo de datos de velas o marco temporal usado para los cálculos. |

## Notas

- La estrategia espera instrumentos que expongan `PriceStep` y `StepPrice` para que los umbrales basados en pips puedan convertirse a precios reales.
- Las órdenes pendientes se recrean sólo cuando la volatilidad continúa cayendo. Si la volatilidad sube, el estado plano se reinicia y todas las órdenes de entrada se cancelan.
- Las órdenes protectoras de stop y take-profit se cancelan automáticamente cuando la posición se cierra.

## Descargo de responsabilidad

Este ejemplo se proporciona sólo con fines educativos. El rendimiento pasado de la estrategia original no garantiza resultados futuros. Prueba y ajusta los parámetros exhaustivamente antes de desplegar en mercados reales.
