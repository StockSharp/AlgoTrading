# Estrategia semanal de Contrarian Trade MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema contrario semanal convertido del asesor experto MQL "Contrarian_trade_MA" original. La estrategia analiza los extremos de las velas semanales junto con una media móvil simple para atenuar los movimientos extendidos al comienzo de una nueva semana.

## Lógica de trading

- **Fuente de datos**: velas semanales proporcionadas por el parámetro `CandleType` (el valor predeterminado es un período de tiempo de 7 días).
- **Extremos históricos**: Los indicadores `Highest` y `Lowest` rastrean los máximos y mínimos de las `CalcPeriod` semanas completas anteriores, excluyendo la vela evaluada actualmente.
- **Filtro de media móvil**: una media móvil simple de longitud `MaPeriod` aplicada a los cierres semanales actúa como un filtro direccional.
- **Reglas de entrada**:
  - **Compre** cuando el cierre de la semana anterior sea superior al máximo registrado (`highest < previousClose`) o cuando el promedio móvil esté por encima de la apertura semanal actual.
  - **Vender** cuando el cierre de la semana anterior sea inferior al mínimo registrado (`lowest > previousClose`) o cuando el promedio móvil esté por debajo de la apertura semanal actual.
  - Sólo puede haber una posición abierta en cualquier momento; Las señales opuestas se ignoran hasta que se cierra la operación existente.
- **Reglas de salida**:
  - La posición se cierra después de mantenerse durante siete días (604.800 segundos) independientemente de la dirección.
  - Se evalúa una parada protectora en cada vela semanal completa. La distancia de parada se calcula a partir de `StopLossPoints * PriceStep` (vuelve a `1` si los metadatos del instrumento no especifican un paso).

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CalcPeriod` | `4` | Número de semanas completadas utilizadas para calcular el máximo más alto y el mínimo más bajo. |
| `MaPeriod` | `7` | Periodo de la media móvil simple aplicada a los cierres semanales. |
| `StopLossPoints` | `300` | Distancia desde el precio de entrada hasta el stop-loss, medida en incrementos de precio. Establezca en `0` para desactivar la parada. |
| `Volume` | `0.5` | Tamaño del pedido en lotes enviados por `BuyMarket`/`SellMarket`. |
| `CandleType` | `7 days` | Plazo de las velas que impulsan todos los cálculos. |

## Notas adicionales

- La estrategia recupera automáticamente el paso de precio de `Security.PriceStep`. Proporcione este valor en los metadatos del instrumento para una colocación precisa del stop-loss.
- `StartProtection()` está habilitado para rastrear cambios de posición inesperados realizados fuera de la estrategia.
- Debido a que la lógica opera en velas semanales completadas, los llenados se simulan en el cierre semanal de la barra de señal cuando se ejecuta en modo de prueba.
