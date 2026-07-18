# Estrategia Stop Loss Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este port replica el asesor experto MetaTrader «Stop Loss Take Profit». La estrategia lanza una moneda cada vez que la cuenta está plana y abre una orden de mercado en la dirección elegida. Cada posición recibe inmediatamente órdenes de stop-loss y take-profit basadas en pips. Si se toca el stop, la siguiente operación duplica su tamaño (limitado por los límites de volumen del valor). Un take-profit reinicia el volumen a la cantidad inicial. El comportamiento refleja el dimensionamiento de posición estilo martingala original mientras usa la API de alto nivel de StockSharp.

## Lógica de Trading

- **Datos de Mercado**: Usa el parámetro `CandleType` (por defecto marco temporal de 1 minuto) para impulsar los puntos de decisión.
- **Reglas de Entrada**:
  - Cuando `Position == 0` y no hay orden de entrada pendiente, la estrategia genera un booleano pseudoaleatorio.
  - `true` abre una posición larga con `BuyMarket(volume)`; `false` abre una corta con `SellMarket(volume)`.
- **Reglas de Salida**:
  - Las órdenes de stop-loss y take-profit protectores se colocan tan pronto como se recibe el fill de entrada.
  - Una salida por stop duplica el tamaño para la siguiente operación, mientras que un take-profit lo reinicia.
  - Si la distancia de stop o take-profit se establece en `0`, se omite la orden protectora respectiva.
- **Gestión de Dinero**:
  - `InitialVolume` define el tamaño base de la orden.
  - Después de una operación perdedora, el tamaño se duplica pero se recorta a `Security.MaxVolume` cuando ese valor está disponible.
  - El volumen se normaliza al `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento para que las órdenes sean válidas.
- **Manejo de Pips**:
  - Por defecto, la estrategia infiere un pip del `PriceStep` y `Decimals` del instrumento (los símbolos FX de 5 dígitos se asignan a 0.0001).
  - Establezca `PipSize` a un valor positivo para anular la detección automática del tamaño de pip.

## Parámetros

| Nombre | Por defecto | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | Velas de 1 minuto | Marco temporal usado para activar lanzamientos de moneda y entradas. |
| `StopLossPips` | 1 | Distancia de stop-loss expresada en pips. `0` deshabilita el stop. |
| `TakeProfitPips` | 1 | Distancia de take-profit expresada en pips. `0` deshabilita el take-profit. |
| `InitialVolume` | 0.01 | Volumen de operación inicial. Duplicado después de eventos de stop-loss y reiniciado después de ganancias. |
| `PipSize` | 0 (auto) | Anulación opcional del tamaño de pip en unidades de precio absolutas. |

## Notas de Uso

- Funciona tanto en el lado largo como corto y es intencionalmente neutral en dirección.
- Las órdenes protectoras se cancelan siempre que la posición se cierra para evitar órdenes obsoletas.
- El generador aleatorio se siembra con `Environment.TickCount`, lo que significa que cada sesión produce diferentes secuencias de operaciones.
- Adecuado para demostrar la estratificación de riesgo y el comportamiento martingala en lugar de para trading en producción sin controles de riesgo adicionales.
