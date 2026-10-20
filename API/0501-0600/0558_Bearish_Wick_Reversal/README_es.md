# Bearish Wick Reversal

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Reversión de mecha inferior, solo larga, sobre velas terminadas de un minuto. Estando sin posición, comprar `Volume` si `Close < Open` y `100 * (Low - Close) / Close <= Threshold`. El valor predeterminado `-1` exige una mecha inferior de al menos el uno por ciento del cierre.

La apertura UTC de la vela debe estar en el intervalo inclusivo `StartTime`–`EndTime`. Con `UseEmaFilter` activo también se requiere `EMA(EmaPeriod)` formada y `Close > EMA`; desactivado, su calentamiento no retrasa entradas.

Cerrar toda la posición larga ejecutada cuando una vela completa cierra por encima del máximo de la vela completa anterior. La ventana de entrada y EMA no bloquean salidas. No hay posiciones cortas, pausas ocultas, take-profit ni stop-loss automáticos. Una orden activa evita duplicados.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Threshold` | `-1` |
| `UseEmaFilter` | `false` |
| `EmaPeriod` | `200` |
| `CandleType` | `1m` |
| `StartTime` | `2014-01-01T00:00:00Z` |
| `EndTime` | `2099-01-01T00:00:00Z` |
