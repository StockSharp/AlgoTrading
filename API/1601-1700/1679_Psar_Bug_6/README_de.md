# Psar Bug 6-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem MQL4-Skript "psar_bug_6".

## Logik
- Verwendet den Parabolic SAR-Indikator mit konfigurierbarem Schritt und maximaler Beschleunigung.
- Kauft, wenn der Preis über dem SAR schließt und zuvor darunter war.
- Verkauft, wenn der Preis unter dem SAR schließt und zuvor darüber war.
- Der optionale Umkehrparameter invertiert die Kauf-/Verkaufssignale.
- Die Option `SarClose` schließt die bestehende Position, wenn der SAR auf die entgegengesetzte Seite wechselt.
- Feste Take-Profit- und Stop-Loss-Abstände in Preiseinheiten. Trailing Stop kann aktiviert werden.

## Parameter
- `SarStep` – Schritt des Beschleunigungsfaktors.
- `SarMax` – maximaler Beschleunigungsfaktor.
- `StopLoss` – anfänglicher Stop-Loss-Abstand.
- `TakeProfit` – Take-Profit-Abstand.
- `Trailing` – Trailing Stop aktivieren.
- `TrailStop` – Trailing-Stop-Abstand bei aktiviertem Trailing.
- `SarClose` – Position bei SAR-Umkehr schließen.
- `Reverse` – Trading-Signale invertieren.
- `CandleType` – Kerzentyp für Berechnungen.

## Hinweise
Die Strategie verwendet die High-Level-API mit Kerzenabonnements und Indikator-Bindung. Der Schutz wird mit optionalem Trailing Stop gestartet, Ausstiege erfolgen per Marktorder.
