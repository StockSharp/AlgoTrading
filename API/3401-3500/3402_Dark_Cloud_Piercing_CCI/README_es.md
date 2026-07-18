# Estrategia de perforación de nube oscura CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
This strategy is a StockSharp port of the MetaTrader Expert_ADC_PL_CCI advisor. Escanea la acción del precio en busca de reversiones de velas de Piercing Line y Dark Cloud Cover y utiliza el índice del canal de productos básicos (CCI) como confirmación. Una vez que se detecta un patrón válido junto con una lectura extrema de CCI, la estrategia abre una posición de mercado en la dirección de la reversión y luego sale cuando el CCI sale de su zona extrema.

## Indicadores
- **Índice del canal de productos básicos (CCI):** confirma los extremos del impulso y produce las condiciones de salida.
- **Longitud promedio del cuerpo (SMA):** mide el tamaño del cuerpo de la vela para validar velas "largas" dentro de la definición del patrón.
- **Average close price (SMA):** acts as a simple trend filter that mirrors the moving average used in the original MQL logic.

## Reglas de trading
### Entrada
- **Bullish signal (Piercing Line):**
  1. Previous candle must be a long bearish candle that opens above its close.
  2. La última vela debe ser una vela alcista larga que se abra por debajo del mínimo anterior y cierre dentro del cuerpo anterior, por encima de su punto medio pero por debajo de la apertura anterior.
  3. The midpoint of the older candle has to be below the moving average to confirm a short-term downtrend.
  4. The most recent completed CCI value must be less than or equal to `-EntryConfirmationLevel` (default `50`).
  5. If a short position exists it is fully closed before entering long.
- **Señal bajista (Cubierta de nubes oscuras):** lógica reflejada de la señal alcista con una vela alcista larga seguida de una vela bajista larga que se abre, penetra el cuerpo anterior y cierra por debajo de su punto medio mientras CCI es mayor o igual a `EntryConfirmationLevel`.

### Salir
- **Posiciones largas:** se cierran cuando el CCI cruza por debajo de `ExitLevel` o cruza por debajo de `-ExitLevel` desde arriba, lo que indica que el impulso se ha normalizado.
- **Short positions:** closed when the CCI crosses up above `-ExitLevel` or above `ExitLevel` from below.

### Dimensionamiento de posiciones
- Uses the base `Volume` property. Cuando la señal requiere revertir una posición existente, la estrategia agrega automáticamente el tamaño absoluto de la posición actual al volumen de la orden, asegurando un cambio completo.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de vela y período de tiempo utilizado para la detección. | `1H` período de tiempo |
| `CciPeriod` | Longitud retrospectiva del índice del canal de productos básicos. | `49` |
| `AverageBodyPeriod` | Número de velas para la media móvil del tamaño corporal. | `11` |
| `EntryConfirmationLevel` | Absolute CCI level that validates pattern entries. | `50` |
| `ExitLevel` | Absolute CCI level that triggers position exits. | `80` |

## Notas
- The strategy processes only finished candles and ignores partial updates.
- No stop-loss or take-profit orders are set automatically; exits are purely signal based as in the original expert advisor.
- Ensure the instrument has a price step configured because the equality tolerance of the candlestick logic depends on the security settings.
