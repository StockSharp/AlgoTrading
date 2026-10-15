# Mehrwährungs-Überlagerungs-Absicherungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertierung des MetaTrader 4 Expert Advisors **"Multicurrency hedge example EA (overlay hedge)"** in die StockSharp High-Level-API.

## Übersicht
- Arbeitet mit einem vom Benutzer bereitgestellten Universum von Forex-Symbolen und überwacht alle einzigartigen Paare.
- Berechnet rollende Pearson-Korrelation und ATR-Verhältnisse, um zu bestimmen, welche Symbole sich gemeinsam bewegen und wie beide Beine dimensioniert werden.
- Erstellt synthetische Preisüberlagerungen, um zu erkennen, wenn das Hauptinstrument von seinem korrelierten Partner über einen konfigurierbaren Schwellenwert hinaus abweicht.
- Öffnet abgesicherte Blöcke (Kauf/Verkauf, Kauf/Kauf, Verkauf/Kauf, Verkauf/Verkauf) abhängig vom Korrelationsvorzeichen und der Überlagerungsrichtung.
- Schließt den gesamten Block, sobald ein gegenseitiges Take-Profit-Ziel in Punkten oder der Portfoliowährung erreicht wird.

## Arbeitsablauf
1. Abonniere abgeschlossene Kerzen für jedes Wertpapier im Universum und speichere die neuesten High/Low/Close-Werte.
2. Abonniere Level1-Quotes jedes Wertpapiers, um Spread-Filter vor dem Einreichen von Absicherungen durchzusetzen.
3. Einmal täglich (Standard 01:00 Serverzeit) die Liste der handelbaren Paare neu aufbauen:
   - Nur Paare beibehalten, bei denen die absolute Korrelation über dem konfigurierten Schwellenwert liegt.
   - Das ATR-Verhältnis berechnen, um das Volumen des Hauptbeins zu skalieren.
4. Für jede abgeschlossene Kerze die Überlagerungsdistanz prüfen:
   - Positive Korrelation ⇒ Haupt kaufen / Sub verkaufen, wenn die Abweichung unter `-OverlayThreshold` Punkten liegt, Haupt verkaufen / Sub kaufen, wenn sie über `+OverlayThreshold` Punkten liegt.
   - Negative Korrelation ⇒ Beide Beine unterhalb des negativen Schwellenwerts kaufen, Beide Beine oberhalb des positiven Schwellenwerts verkaufen.
5. Offene Absicherungsblöcke verfolgen und schließen, wenn der aggregierte Gewinn eine der Take-Profit-Bedingungen erreicht.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `Universe` | Sammlung von `Security`-Objekten zum Scannen. Benötigt mindestens zwei Einträge. | leer |
| `CandleType` | Kerzen-Datentyp für Berechnungen. | 1-Minuten-Zeitrahmen |
| `RangeLength` | Anzahl der Bars für die Berechnung von Preishüllen. | 400 |
| `CorrelationLookback` | Bars für die Pearson-Korrelation. | 500 |
| `AtrLookback` | Bars für die ATR-Verhältnis-Dimensionierung. | 200 |
| `CorrelationThreshold` | Minimale absolute Korrelation, um ein Paar zu behalten (0–1). | 0.90 |
| `OverlayThreshold` | Überlagerungsabstand in Punkten, gemessen am Schritt des Hauptinstruments. | 100 |
| `TakeProfitByPoints` / `TakeProfitPoints` | Aktiviert und konfiguriert punktbasierten gegenseitigen Take-Profit. | true / 10 |
| `TakeProfitByCurrency` / `TakeProfitCurrency` | Aktiviert und konfiguriert währungsbasierten gegenseitigen Take-Profit. | false / 10 |
| `MaxOpenPairs` | Maximal gleichzeitig offene Absicherungsblöcke. | 10 |
| `BaseVolume` | Volumen des Sekundärbeins (Hauptbein-Volumen = `BaseVolume * ATR ratio`). | 1 |
| `RecalculationHour` | Tagesstunde, zu der Korrelationen neu berechnet werden. | 1 |
| `MaxSpread` | Maximal erlaubter Bid-Ask-Spread pro Bein (in Punkten). | 10 |

## Datenanforderungen
- Historische und Live-Kerzen für jedes Wertpapier in `Universe` mit dem angegebenen `CandleType`.
- Level1-Quote-Updates für jedes Wertpapier zur Spread-Validierung.
- Portfolio-Informationen für die Orderregistrierung.

## Verwendungshinweise
- Die Strategie befüllt das Universum nicht automatisch; übergebe die gewünschten Forex-Symbole vor dem Start.
- Um die MetaTrader-Dimensionierungslogik nachzuahmen, halte `BaseVolume` gleich der Losgröße des Sekundärbeins. Das Hauptbein-Volumen wird automatisch durch das ATR-Verhältnis skaliert.
- Wenn Spread-Daten nicht verfügbar sind, werden neue Einstiege übersprungen, bis der erste Order-Buch-Snapshot eintrifft.
- Die Schließlogik schätzt den gegenseitigen Gewinn durch Kombination der vorzeichenbehafteten Bewegung jedes Beins unter Verwendung des Instrument-Preisschritts und Schrittpreises.

## Unterschiede zum Original-EA
- Verwendet StockSharp-Abonnements (`SubscribeCandles`, `SubscribeLevel1`) anstelle von timer-basiertem Polling.
- Take-Profit-Logik wird mit gemittelten Preisschritt-Informationen implementiert anstatt mit rohem Trade-Gewinn/Provision.
- Erfordert einen expliziten Universum-Parameter, der es der Strategie ermöglicht, auf einer beliebigen Teilmenge von von StockSharp unterstützten Instrumenten zu laufen.
- Die Order-Ausführung erfolgt über StockSharp-Market-Orders mit Pro-Absicherungs-Kommentaren für die Nachverfolgbarkeit.
