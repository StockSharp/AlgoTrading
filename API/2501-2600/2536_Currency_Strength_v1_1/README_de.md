# Währungsstärke v1.1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Währungsstärke v1.1-Strategie repliziert den MetaTrader Expert Advisor *Currency Strength v1.1*. Sie misst die relative Stärke der acht wichtigsten Währungen (USD, EUR, JPY, CAD, AUD, NZD, GBP, CHF) anhand täglicher prozentualer Veränderungen für 26 liquide FX-Paare. Wenn die Stärke zweier Währungen einen konfigurierbaren Schwellenwert überschreitet, öffnet die Strategie eine Position im entsprechenden Währungspaar in Richtung der stärkeren Währung.

## Markt und Daten
- **Instrumentuniversum:** 26 wichtige und Cross-FX-Paare (USDJPY, USDCAD, AUDUSD, USDCHF, GBPUSD, EURUSD, NZDUSD, EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURNZD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, CHFJPY, GBPCHF, GBPAUD, GBPCAD, GBPJPY, CADJPY, NZDJPY, GBPNZD, CADCHF).
- **Datenhäufigkeit:** Tageskerzen (D1). Nur abgeschlossene Kerzen werden verarbeitet, um konsistente Berechnungen aufrechtzuerhalten.
- **Erforderliche Felder:** Eröffnungs-, Hoch-, Tief- und Schlusspreise jeder Kerze.

## Berechnung der Währungsstärke
Die tägliche prozentuale Veränderung für jedes Paar wird berechnet als:

```
(change) = (Close − Open) / Open × 100
```

Diese paarbezogenen Veränderungen werden dann in Währungsstärkeindizes kombiniert:

- **EUR-Stärke** = Durchschnitt von EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURUSD, EURNZD
- **USD-Stärke** = Durchschnitt von USDJPY, USDCAD, –AUDUSD, USDCHF, –GBPUSD, –EURUSD, –NZDUSD
- **JPY-Stärke** = negativer Durchschnitt von USDJPY, EURJPY, AUDJPY, CHFJPY, GBPJPY, CADJPY, NZDJPY
- **CAD-Stärke** = Durchschnitt von CADCHF, CADJPY, –GBPCAD, –AUDCAD, –EURCAD, –USDCAD
- **AUD-Stärke** = Durchschnitt von AUDUSD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, –EURAUD, –GBPAUD
- **NZD-Stärke** = Durchschnitt von NZDUSD, NZDJPY, –EURNZD, –AUDNZD, –GBPNZD
- **GBP-Stärke** = Durchschnitt von GBPUSD, –EURGBP, GBPCHF, GBPAUD, GBPCAD, GBPJPY, GBPNZD
- **CHF-Stärke** = Durchschnitt von CHFJPY, –USDCHF, –EURCHF, –AUDCHF, –GBPCHF, –CADCHF

Jeder Durchschnitt verwendet dieselbe Anzahl von Komponenten wie im ursprünglichen Expert Advisor, um das Gewichtungsschema zu erhalten.

## Handelslogik
1. Nachdem alle 26 Paare eine neue abgeschlossene Tageskerze melden, werden die Stärken neu berechnet.
2. Für jedes Paar vergleicht die Strategie die zwei relevanten Währungsstärken. Wenn die absolute Differenz den Parameter `DifferenceThreshold` überschreitet, wird ein Handelssignal generiert.
3. Die Signalrichtung folgt der stärkeren Währung:
   - Wenn Basiswährungsstärke > Notierungswährungsstärke → Paar kaufen.
   - Wenn Basiswährungsstärke < Notierungswährungsstärke → Paar verkaufen.
4. Trades sind nur erlaubt, wenn die Tageskerze des Paares mit dem Signal übereinstimmt (Schluss über Eröffnung für Käufe, Schluss unter Eröffnung für Verkäufe), was den Trendfilter des ursprünglichen EA widerspiegelt.
5. Bestehende Nettopositionen werden respektiert. Wenn ein Umkehrsignal erscheint, während eine entgegengesetzte Position offen ist, schließt die Strategie die aktuelle Position und dreht in die neue Richtung mit einer einzigen Marktorder.
6. Wenn `TradeOncePerDay` aktiviert ist, kann jedes Paar maximal einmal pro Handelstag Long einsteigen und maximal einmal pro Handelstag Short einsteigen.

## Risikomanagement und Ausstiege
- Die optionale `UseSlTp`-Flag aktiviert Stop-Loss- und Take-Profit-Logik, die auf der Tageskerze jedes Paares ausgeführt wird. Die Abstände werden in Pips definiert (`StopLossPips`, `TakeProfitPips`).
- Die Schutzlogik wertet das Tageshoch/-tief der jüngsten Kerze aus. Wenn diese Extreme die jeweiligen Ziele erreichen, wird die Position beim nächsten Auswertungsschritt zum Marktpreis geschlossen.
- Ohne SL/TP bleiben Positionen offen, bis ein entgegengesetztes Signal eine Umkehrung erzwingt oder die Strategie manuell gestoppt wird, was das Verhalten des Quell-EA widerspiegelt.

## Strategieparameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen für Kerzen (Standard: täglich). |
| `DifferenceThreshold` | Minimale Stärkelücke (in Prozentpunkten), die erforderlich ist, um einen Trade auszulösen. |
| `TradeOncePerDay` | Wenn `true`, begrenzt jedes Paar auf einen Long- und einen Short-Einstieg pro Tag. |
| `UseSlTp` | Aktiviert die tägliche Auswertung von Stop-Loss- und Take-Profit-Niveaus. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips gemessen. |
| `StopLossPips` | Stop-Loss-Abstand in Pips gemessen. |
| Paar-Parameter | Individuelle `Security`-Eingaben für die 26 FX-Paare. Jedes muss vor dem Starten der Strategie zugewiesen werden. |
| `Volume` | Basisklasseneigenschaft, die die Handelsgröße definiert (Standard 0.01 Lot). |

## Implementierungshinweise
- Die Strategie abonniert jedes Paar separat über die High-Level-Kerzenabonnement-API (`SubscribeCandles`).
- Die Kerzenbehandlung ignoriert strikt unvollständige Kerzen und erfüllt die StockSharp-Konvertierungsrichtlinien.
- Stärkeberechnungen und Signalgenerierung laufen nur, wenn alle Paare dasselbe Handelsdatum melden, was synchronisierte Währungskörbe garantiert.
- Interne Wörterbücher verfolgen die letzten Handelsdaten pro Richtung und speichern Einstiegsinformationen für Schutzausstiege.

## Verwendungstipps
1. Alle 26 Wertpapiere vor dem Starten der Strategie zuweisen; fehlende Eingaben werfen eine Ausnahme, um Teilberechnungen zu verhindern.
2. Sicherstellen, dass der Datenprovider Tageskerzen für jedes konfigurierte Paar liefert, damit die Währungsstärken synchron bleiben.
3. `DifferenceThreshold` anpassen, um die Signalhäufigkeit zu kontrollieren. Kleinere Schwellenwerte führen zu häufigeren Trades, aber auch mehr Umkehrungen.
4. Die pip-basierten Stops an die Quotierungsgenauigkeit des eigenen Brokers kalibrieren; der Standard geht von Bruchpip-Preisen aus.
