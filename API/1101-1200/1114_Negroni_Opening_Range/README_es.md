# Estrategia de Rango de Apertura Negroni
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera rupturas basadas en el rango de premercado o de apertura definido por ventanas de tiempo configurables. Las órdenes solo se permiten dentro de la sesión de trading especificada y cualquier posición abierta se cierra al final de la sesión.

## Parámetros
- `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- `MaxTradesPerDay` = 3
- `Direction` = TradeDirection.LongShort
- `SessionStart` = new TimeSpan(9, 30, 0)
- `SessionEnd` = new TimeSpan(14, 0, 0)
- `CloseTime` = new TimeSpan(16, 0, 0)
- `UsePreMarketRange` = true
- `PreMarketStart` = new TimeSpan(8, 0, 0)
- `PreMarketEnd` = new TimeSpan(9, 0, 0)
- `OpenRangeStart` = new TimeSpan(9, 5, 0)
- `OpenRangeEnd` = new TimeSpan(9, 30, 0)
