# KWAN RDP Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader-Experten `Exp_KWAN_RDP`. Die Logik berechnet den KWAN RDP-Oszillator durch Kombination dreier Standardindikatoren und Glättung ihres Produkts:

1. **DeMarker** — misst die Beziehung zwischen zuletzt aufgetretenen Hochs und Tiefs, um die Momentum-Erschöpfung zu beurteilen.
2. **Money Flow Index** — bewertet Preis und Volumen, um überkaufte oder überverkaufte Bedingungen zu erkennen.
3. **Momentum** — erfasst die Geschwindigkeit von Preisänderungen mit dem gewählten Zeitraum.
4. Der Rohwert `100 * DeMarker * MFI / Momentum` wird mit einem konfigurierbaren gleitenden Durchschnitt (SMA, EMA, SMMA, WMA oder Jurik) geglättet.

Die Steigung des geglätteten Oszillators erzeugt Handelssignale:

- **Bullische Wende (steigende Steigung)**: Short-Positionen schließen und optional eine Long-Position eröffnen.
- **Bärische Wende (fallende Steigung)**: Long-Positionen schließen und optional eine Short-Position eröffnen.
- Neutrale Balken (flache Steigung) lösen keine Aktionen aus.

## Parameter

- `CandleType` — Kerzenserie für Indikatorberechnungen (Standard: H1-Zeitrahmen).
- `DeMarkerPeriod` — Periode des DeMarker-Indikators.
- `MfiPeriod` — Periode des Money Flow Index.
- `MomentumPeriod` — Periode des Momentum-Indikators.
- `SmoothingLength` — Länge des glättenden gleitenden Durchschnitts.
- `Smoothing` — Glättungsmethode (Simple, Exponential, Smoothed, Weighted, Jurik).
- `EnableLongEntries` / `EnableShortEntries` — Eröffnung von Long- oder Short-Positionen erlauben.
- `CloseLongsOnReverse` / `CloseShortsOnReverse` — Gegenpositionen schließen, wenn ein Umkehrsignal erscheint.
- `TakeProfitPercent` / `StopLossPercent` — optionaler prozentualer Schutz, angewendet über `StartProtection`.

## Handelsregeln

1. Die konfigurierte Kerzenserie abonnieren und DeMarker, MFI, Momentum und den geglätteten KWAN-Wert auf jeder abgeschlossenen Kerze berechnen.
2. Die Steigungsrichtung des neuesten Oszillatorwerts gegenüber dem vorherigen erkennen.
3. Wenn die Steigung aufwärts dreht, Shorts schließen (wenn aktiviert) und ein Long eröffnen, wenn Long-Trading erlaubt ist und keine Long-Position aktiv ist.
4. Wenn die Steigung abwärts dreht, Longs schließen (wenn aktiviert) und ein Short eröffnen, wenn Short-Trading erlaubt ist und keine Short-Position aktiv ist.
5. Die optionalen Stop-Loss- und Take-Profit-Prozentsätze verwenden, um Positionen mit Platform-Schutz abzusichern.

## Hinweise

- Signale werden nur auf abgeschlossenen Kerzen verarbeitet, um Intrabar-Rauschen zu vermeiden.
- Die DeMarker-Berechnung verwendet interne Glättung, um der MetaTrader-Implementierung zu entsprechen.
- Alle Kommentare im C#-Code sind gemäß den Projektrichtlinien auf Englisch verfasst.
