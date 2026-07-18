# Estrategia OsMaSter V0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertida del experto de MetaTrader 5 "OsMaSter v0" (archivo MQL `OsMaSter v0.mq5`).
- Usa un patrón de histograma MACD (OsMA) para identificar reversiones de momentum después de una breve consolidación.
- Diseñada para operar en un único instrumento y marco temporal seleccionado por el usuario a través del parámetro `CandleType`.
- Convierte automáticamente la configuración de riesgo basada en pips (stop-loss y take-profit) a offsets de precio absolutos usando el paso de precio del instrumento y la precisión decimal.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `FastPeriod` | 9 | Longitud de la EMA rápida para el histograma MACD. |
| `SlowPeriod` | 26 | Longitud de la EMA lenta para el histograma MACD. |
| `SignalPeriod` | 5 | Longitud de la EMA de señal usada para suavizar el histograma. |
| `StopLossPips` | 30 | Distancia al stop de protección en pips. Poner en `0` para deshabilitar. |
| `TakeProfitPips` | 50 | Distancia al objetivo de ganancia en pips. Poner en `0` para deshabilitar. |
| `TradeVolume` | 1 | Volumen de orden (lotes) usado para entradas de mercado. |
| `CandleType` | Velas de 15 minutos | Marco temporal usado para los cálculos del indicador. |

## Lógica de señales
1. La estrategia mantiene los últimos cuatro valores del histograma MACD (`hist0` = actual, `hist1` = anterior, ..., `hist3` = hace tres velas).
2. **Entrada larga** cuando `hist3 > hist2`, `hist2 < hist1`, y `hist1 < hist0` &mdash; una secuencia ascendente después de un mínimo local.
3. **Entrada corta** cuando `hist3 < hist2`, `hist2 > hist1`, y `hist1 > hist0` &mdash; una secuencia descendente después de un máximo local.
4. Solo una posición puede estar abierta a la vez. La estrategia ignora nuevas señales mientras una operación está activa.

## Gestión de posición
- Las órdenes se envían con `BuyMarket()` o `SellMarket()` usando el `TradeVolume` configurado.
- `StartProtection` adjunta offsets de stop-loss y take-profit basados en las entradas de pips. El tamaño del pip sigue la convención forex (paso de precio × 10 para instrumentos de 3/5 decimales, de lo contrario el propio paso de precio).
- No hay reglas de salida adicionales; las posiciones se gestionan exclusivamente por las órdenes de protección o por intervención manual.

## Notas
- Asegúrese de que el `Security` tiene valores correctos de `PriceStep` y `Decimals` para que la conversión de pips coincida con la especificación del broker.
- Ajuste el marco temporal de velas y el volumen para que coincidan con el horizonte de trading del mercado objetivo.
- Debido a que la estrategia espera la ejecución del stop o del objetivo, las señales consecutivas en la misma dirección se omiten mientras una posición permanece abierta.
