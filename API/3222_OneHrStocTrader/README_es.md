# Estrategia de OneHrStocTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia **OneHrStocTrader** replica el asesor experto de MetaTrader 4 *OneHrStocTrader.mq4* dentro de la API de alto nivel de StockSharp. Opera un único instrumento en velas horarias y combina el oscilador estocástico con un filtro de ancho de Bandas de Bollinger. Una operación se abre solo cuando la volatilidad (medida por la distancia entre las Bandas de Bollinger) se encuentra dentro del rango configurado y el oscilador estocástico abandona una zona extrema exactamente a la hora configurada.

## Lógica de trading

1. **Datos**
   - Trabaja con velas horarias por defecto (configurable).
   - Utiliza los valores de la vela *completada* más reciente para coincidir con el comportamiento de MetaTrader.
2. **Filtro de Bandas de Bollinger**
   - Calcula la diferencia entre las bandas superior e inferior en pips.
   - Las señales de trading se ignoran cuando la diferencia cae fuera del rango `[BollingerSpreadLower, BollingerSpreadUpper]`.
3. **Disparador del oscilador estocástico**
   - Referencia las dos últimas velas completadas de la línea %K estocástica.
   - **Compra**: %K actual por debajo de `StochasticLower`, %K anterior subiendo (`prev < current`) y la nueva barra comienza en `BuyHourStart`.
   - **Venta**: %K actual por encima de `StochasticUpper`, %K anterior bajando (`prev > current`) y la nueva barra comienza en `SellHourStart`.
4. **Gestión de órdenes**
   - Cierra una posición opuesta antes de abrir una nueva.
   - Limita entradas consecutivas en la misma dirección mediante `MaxOrdersPerDirection`.
5. **Gestión de riesgo**
   - Distancias fijas de take-profit y stop-loss definidas en pips.
   - Trailing stop opcional que se mueve en incrementos de pip una vez que el precio avanza más allá de la distancia configurada.
   - Los niveles de protección internos se monitorean en cada vela completada; cuando se alcanzan, la estrategia cierra la posición a mercado.

## Parámetros

| Nombre | Descripción | Valor predeterminado |
|------|-------------|---------|
| `TradeVolume` | Tamaño de orden en lotes. | `0.01` |
| `CandleType` | Marco temporal utilizado para todos los cálculos. | `1h` |
| `BollingerPeriod` | Período de retroceso de las Bandas de Bollinger. | `20` |
| `BollingerSigma` | Multiplicador sigma de las Bandas de Bollinger. | `2.0` |
| `BollingerSpreadLower` | Diferencia mínima de banda en pips requerida para operar. | `56` |
| `BollingerSpreadUpper` | Diferencia máxima de banda en pips permitida para operar. | `158` |
| `BuyHourStart` | Hora (0-23) cuando se evalúan las entradas largas. | `4` |
| `SellHourStart` | Hora (0-23) cuando se evalúan las entradas cortas. | `0` |
| `StochasticKPeriod` | Período %K estocástico. | `5` |
| `StochasticDPeriod` | Período %D estocástico. | `3` |
| `StochasticSlowing` | Factor de ralentización estocástico. | `5` |
| `StochasticLower` | Umbral de sobreventa. | `36` |
| `StochasticUpper` | Umbral de sobrecompra. | `70` |
| `TakeProfitPips` | Distancia de take-profit en pips. | `200` |
| `StopLossPips` | Distancia de stop-loss en pips. | `95` |
| `TrailingStopPips` | Distancia de trailing stop en pips (0 = desactivado). | `40` |
| `MaxOrdersPerDirection` | Máximo de entradas consecutivas por dirección. | `1` |

## Gráficos

Cuando hay una superficie de gráfico disponible, la estrategia dibuja:
- Velas de precio.
- Bandas de Bollinger.
- Oscilador estocástico en un panel separado.
- Operaciones ejecutadas para validación visual rápida.

## Notas

- El tamaño del pip se deriva del paso de precio del instrumento y la precisión decimal, reflejando la lógica de multiplicador de MetaTrader.
- Los niveles de protección se recalculan usando `Security.ShrinkPrice` para asegurar el redondeo de precios conforme al exchange.
- Los ajustes del trailing stop imitan el EA original ajustando el stop solo cuando el precio avanza al menos un pip más allá del stop anterior.
- La implementación no crea órdenes pendientes; todas las entradas y salidas usan órdenes a mercado exactamente como el asesor experto fuente.
