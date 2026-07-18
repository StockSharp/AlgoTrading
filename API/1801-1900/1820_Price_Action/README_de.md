# Price Action Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Price Action Strategie** wechselt zwischen Long- und Short-Marktorders, sobald die vorherige Position geschlossen wird.
Sie wendet eine feste Stop-Loss-Distanz, ein hebelbasiertes Take-Profit-Ziel und einen optionalen Trailing-Stop an, der dem Markt mit einem konfigurierbaren Schritt folgt.

## Details
- **Einstiegskriterien:** Keine offene Position. Die Richtung wechselt nach jedem Trade zwischen Kauf und Verkauf.
- **Long/Short:** Beide.
- **Ausstiegskriterien:** Der Kurs erreicht den Trailing-Stop, den anfänglichen Stop oder das Take-Profit-Niveau.
- **Stops:** Feste Stop-Distanz mit optionalem Trailing (der Schritt definiert die minimale Kursbewegung für eine Aktualisierung).
- **Standardwerte:** `Volume = 1`, `TP = 100`, `Leverage = 5`, `TrailingStop = 0`, `TrailingStep = 0`, `InitialDirection = Buy`, `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`.
