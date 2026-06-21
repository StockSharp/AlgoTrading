# Up3x1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Up3x1 Strategie verwendet drei einfache gleitende Durchschnitte, um Trendänderungen zu erfassen:

- **Schneller SMA**: reagiert schnell auf Preisänderungen.
- **Mittlerer SMA**: liefert eine zusätzliche Trendbestätigung.
- **Langsamer SMA**: definiert die globale Marktrichtung.

### Einstiegsregeln

- **Kauf**, wenn der schnelle SMA den mittleren SMA nach oben kreuzt und beide unter dem langsamen SMA liegen.
- **Verkauf**, wenn der schnelle SMA den mittleren SMA nach unten kreuzt und beide über dem langsamen SMA liegen.

### Ausstiegsregeln

- Auf jede Position wird ein fester Take Profit und Stop Loss angewendet.
- Ein optionaler Trailing Stop kann Gewinne schützen, indem er dem Preis nach dem Einstieg folgt.

### Parameter

- `Volume` – Auftragsgröße.
- `TakeProfit` – Gewinnziel in Preiseinheiten.
- `StopLoss` – Verlustlimit in Preiseinheiten.
- `TrailingStop` – Trailing-Abstand; auf 0 setzen zum Deaktivieren.
- `FastPeriod`, `MiddlePeriod`, `SlowPeriod` – Längen der gleitenden Durchschnitte.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.

Die Strategie ist für Demonstrationszwecke konzipiert und kann für spezifische Handelsinstrumente oder -bedingungen weiter angepasst werden.
