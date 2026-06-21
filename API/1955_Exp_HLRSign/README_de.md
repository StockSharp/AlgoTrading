# Exp HLRSign-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert die HLRSign-Indikatorlogik in StockSharp.
Sie öffnet und schließt Positionen, wenn das High-Low Ratio (HLR) vordefinierte Niveaus kreuzt.

## Funktionsweise

- Berechnet Donchian-Kanal-Werte über einen konfigurierbaren Bereich.
- Berechnet den HLR-Wert als prozentuale Position des Mittelkurses innerhalb des Kanals.
- Erzeugt Kauf- oder Verkaufssignale, wenn das HLR die oberen oder unteren Schwellenwerte kreuzt, abhängig vom ausgewählten Modus:
  - **ModeIn** – kaufen beim Kreuzen über das obere Niveau und verkaufen beim Kreuzen unter das untere Niveau.
  - **ModeOut** – verkaufen beim Kreuzen unter das obere Niveau und kaufen beim Kreuzen über das untere Niveau.
- Ermöglicht das separate Aktivieren oder Deaktivieren des Öffnens und Schließens von Long- und Short-Positionen.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Mode` | Indikator-Betriebsmodus (ModeIn oder ModeOut). |
| `Range` | Rückblickzeitraum für höchste und niedrigste Preise. |
| `UpLevel` | Oberer Schwellenwert in Prozent für HLR. |
| `DnLevel` | Unterer Schwellenwert in Prozent für HLR. |
| `CandleType` | Zeitrahmen der für Berechnungen verwendeten Kerzen. |
| `BuyOpen` | Öffnen von Long-Positionen erlauben. |
| `SellOpen` | Öffnen von Short-Positionen erlauben. |
| `BuyClose` | Schließen von Long-Positionen erlauben. |
| `SellClose` | Schließen von Short-Positionen erlauben. |

## Hinweise

- Die Strategie verwendet die High-Level-API mit dem `DonchianChannels`-Indikator.
- Es werden nur abgeschlossene Kerzen verarbeitet und Positionsberechtigungen vor dem Handel geprüft.
- Es sind keine Stop-Loss- oder Take-Profit-Niveaus definiert; der Positionsschutz kann manuell hinzugefügt werden.
