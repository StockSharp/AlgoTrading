# Force DiverSign-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Force DiverSign-Strategie handelt auf der Basis von Divergenzsignalen zwischen zwei Force-Index-Indikatoren, die mit unterschiedlichen Glättungsperioden berechnet werden.
Sie sucht nach einem Drei-Kerzen-Umkehrmuster zusammen mit entgegengesetzten Schwankungen in den schnellen und langsamen Force-Werten. Bei einer bullischen Divergenz
kauft die Strategie; bei einer bärischen Divergenz verkauft sie.

## Parameter
- `Period1` – Periode für den schnellen Force Index.
- `Period2` – Periode für den langsamen Force Index.
- `MaType1` – gleitender Durchschnittstyp zur Glättung des schnellen Force Index.
- `MaType2` – gleitender Durchschnittstyp zur Glättung des langsamen Force Index.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.

## Handelslogik
1. Rohen Force Index als Volumen multipliziert mit der Änderung des Schlusskurses berechnen.
2. Den Rohwert mit zwei gleitenden Durchschnitten glätten, um schnelle und langsame Force-Reihen zu erhalten.
3. Die letzten fünf Force-Werte und die letzten vier Kerzen speichern.
4. **Kaufen** wenn:
   - Die drei vorherigen Kerzen ein Auf–Ab–Auf-Muster bilden.
   - Beide Force-Reihen ein lokales Tief bilden und dann steigen.
   - Schneller und langsamer Force sich zwischen der ersten und dritten Kerze in entgegengesetzte Richtungen bewegen.
5. **Verkaufen** wenn:
   - Die drei vorherigen Kerzen ein Ab–Auf–Ab-Muster bilden.
   - Beide Force-Reihen ein lokales Hoch bilden und dann fallen.
   - Schneller und langsamer Force sich zwischen der ersten und dritten Kerze in entgegengesetzte Richtungen bewegen.

Positionen werden bei jedem Signal umgekehrt: Ein Kauf schließt eine bestehende Short-Position und ein Verkauf schließt eine Long-Position.
