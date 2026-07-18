# Negroni Eröffnungsbereich-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche basierend auf dem Vormarkt- oder Eröffnungsbereich, der durch konfigurierbare Zeitfenster definiert wird. Orders sind nur innerhalb der angegebenen Handelssitzung erlaubt und alle offenen Positionen werden am Sitzungsende geschlossen.

## Parameter
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
