# RSI Bollinger Fraktal-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den MetaTrader "RSI and Bollinger Bands" Expert Advisor in StockSharp. Sie wendet Bollinger Bänder auf den RSI-Oszillator an, wartet auf ein aktuelles Fraktal-Ausbruchsniveau und platziert Stop-Orders jenseits dieses Niveaus mit konfigurierbaren Abständen. Ein Parabolic SAR Trailing-Filter zieht Stops dynamisch nach, sobald eine Position geöffnet ist.

## Indikatoren und Signale
- **RSI** (Standard 8 Perioden) – der Hauptoszillator. Überkauft- und Überverkauft-Schwellen werden verwendet, um ausstehende Orders zu stornieren.
- **Bollinger Bänder auf RSI** (Standard 14 Perioden, Abweichung 1.0) – Einstiege werden nur ausgelöst, wenn der RSI außerhalb des oberen oder unteren Bandes schließt, entsprechend dem ursprünglichen Skriptverhalten, bei dem Bollinger mit RSI-Werten gespeist wird.
- **Bill Williams Fraktale** – die Strategie scannt die letzten bestätigten Auf- und Ab-Fraktale (5-Kerzen-Muster) und verwendet deren Preise als Basis-Ausbruchsniveaus.
- **Parabolic SAR** (Schritt 0.003, Max 0.2) – liefert eine Trailing-Stop-Referenz, sobald eine Position aktiv ist.

## Einstiegslogik
1. Die Arbeit wird auf abgeschlossenen Kerzen des ausgewählten Zeitrahmens durchgeführt (Standard 4 Stunden).
2. Wenn ein **Aufwärts-Fraktal** erscheint und der RSI über dem **oberen Bollinger Band** schließt, während der vorherige Schlusskurs unter dem Fraktal bleibt, wird ein **Buy-Stop** platziert:
   - Eintrittspreis = Fraktal-Hoch + Indent (standardmäßig 15 Pips).
   - Optionaler Stop-Loss = Eintritt − StopLossPips.
   - Optionaler Take-Profit = Eintritt + TakeProfitPips.
3. Symmetrisch wird, wenn ein **Abwärts-Fraktal** entsteht und der RSI unter dem **unteren Bollinger Band** schließt, während der vorherige Schlusskurs über dem Fraktal bleibt, ein **Sell-Stop** unter dem Fraktal platziert.
4. Der RSI, der innerhalb des Kanals zurückkehrt, storniert ausstehende Orders:
   - RSI < untere Schwelle storniert Buy-Stops.
   - RSI > obere Schwelle storniert Sell-Stops.

## Ausstieg und Risikomanagement
- Feste Stop-Loss- und Take-Profit-Abstände (in Pips) replizieren die MQL-Eingaben. Einen Abstand auf `0` zu setzen deaktiviert diesen Schutz.
- Die Parabolic SAR Trailing-Logik erfordert, dass der SAR mindestens `SarTrailingPips` vom aktuellen Preis entfernt ist und bewegt den Stop nur in die günstige Richtung.
- Wenn der Trailing-Stop den Preis kreuzt oder der Preis den festen Take-Profit erreicht, wird die Position mit einer Marktorder geschlossen.
- Das Eröffnen einer Position löscht automatisch die entgegengesetzte ausstehende Order und speichert die beabsichtigten Schutzlevel.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `RsiPeriod` | RSI-Glättungslänge. | 8 |
| `BandsPeriod` | RSI-Bollinger-Zeitraum. | 14 |
| `BandsDeviation` | Standardabweichungsmultiplikator für Bollinger auf RSI. | 1.0 |
| `SarStep` | Parabolic SAR Beschleunigungsschritt. | 0.003 |
| `SarMax` | Maximale Parabolic SAR Beschleunigung. | 0.2 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | 50 |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | 135 |
| `IndentPips` | Abstand jenseits eines Fraktals vor der Platzierung der Stop-Order. | 15 |
| `RsiUpper` | RSI-Schwelle, die Sell-Stops storniert. | 70 |
| `RsiLower` | RSI-Schwelle, die Buy-Stops storniert. | 30 |
| `SarTrailingPips` | Mindestabstand (in Pips) zwischen Preis und SAR vor dem Trailing. | 10 |
| `CandleType` | Datentyp / Zeitrahmen für die Verarbeitung. | 4-Stunden-Kerzen |

## Hinweise
- Die Python-Version wird absichtlich weggelassen, wie angefordert.
- Verwenden Sie `Volume` in der Basisklasse, um die Lotgröße zu konfigurieren (Standard 1, wenn nicht angegeben).
- Die Strategie sollte auf demselben Zeitrahmen wie die ursprüngliche EA-Konfiguration ausgeführt werden (EURUSD H4 gemäß der bereitgestellten `.set`-Datei).
