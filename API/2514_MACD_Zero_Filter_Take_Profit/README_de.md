# MACD Nulllinie-Filter Take-Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den originalen MetaTrader 5-Experten "Robot_MACD", der MACD-Signallinienkruzungen mit zusätzlichen Nulllinienfiltern handelt. Sie operiert auf einem einzelnen Instrument und sucht nach Momentum-Umkehrungen, die durch die Position der MACD-Linie relativ zur Null bestätigt werden. An jede Order wird ein Take-Profit mit festem Abstand angehängt, der das punktbasierte Gewinnziel der ursprünglichen Implementierung widerspiegelt.

## Daten und Indikatoren
- **Primärdaten**: Einzelne Kerzensubskription (Standard-Zeitrahmen 5 Minuten). Der Zeitrahmen kann mit dem Parameter `CandleType` geändert werden, um sich dem gehandelten Markt anzupassen.
- **Indikatoren**:
  - `MovingAverageConvergenceDivergenceSignal` (MACD + Signal + Histogramm). Die Standardwerte sind 12/26-EMAs mit einer 9-Perioden-Signallinie, passend zu den MQL-Parametern.

## Handelslogik
1. Warten, bis die MACD-Berechnung sowohl aktuelle als auch vorherige Werte der MACD- und Signallinie liefert.
2. Bullische und bärische Kreuzungen identifizieren:
   - **Bullische Kreuzung**: vorheriger MACD ≤ vorherige Signal **und** aktueller MACD > aktuelle Signal.
   - **Bärische Kreuzung**: vorheriger MACD ≥ vorherige Signal **und** aktueller MACD < aktuelle Signal.
3. **Positionsmanagement**:
   - Eine Long-Position schließen, wenn eine bärische Kreuzung erscheint.
   - Eine Short-Position schließen, wenn eine bullische Kreuzung erscheint.
4. **Einstiegskriterien** (nur wenn keine Position offen ist und ausreichend Kapital vorhanden):
   - Long einsteigen bei einer bullischen Kreuzung **während sowohl MACD als auch Signal unter null bleiben**.
   - Short einsteigen bei einer bärischen Kreuzung **während sowohl MACD als auch Signal über null bleiben**.
5. Einen festen Take-Profit zum Zeitpunkt der Orderregistrierung anhängen, indem `StartProtection` mit einem absoluten Abstand in Preispunkten aufgerufen wird. Der Abstand entspricht dem konfigurierten Punktwert multipliziert mit dem Preisschritt des Wertpapiers.

## Risikomanagement
- Jede Order hat einen angehängten Take-Profit, der durch `TakeProfitPoints` definiert wird. Es gibt keinen Stop-Loss in der Basislogik, um die Parität mit dem Quell-EA zu erhalten.
- Die Strategie prüft, ob der Portfoliowert mindestens `MinimumCapitalPerVolume * VolumePerTrade` beträgt, bevor eine neue Order platziert wird. Dies emuliert die Free-Margin-Absicherung (`FreeMargin() < 1000 * Lots`) aus der MQL-Version.

## Parameter
| Parameter | Beschreibung | Standardwert |
|-----------|--------------|--------------|
| `MacdFast` | Schnelle EMA-Periode für MACD. | 12 |
| `MacdSlow` | Langsame EMA-Periode für MACD. | 26 |
| `MacdSignal` | Glättungsperiode der Signallinie. | 9 |
| `TakeProfitPoints` | Take-Profit-Abstand in Preispunkten. | 300 |
| `VolumePerTrade` | Handelsvolumen (Lots) für jeden Einstieg. | 1 |
| `MinimumCapitalPerVolume` | Mindestportfoliowert je gehandeltem Lot. | 1000 |
| `CandleType` | Kerzentyp (Zeitrahmen) für den MACD-Indikator. | 5-Minuten-Kerzen |

## Implementierungshinweise
- Orders werden mit `BuyMarket`/`SellMarket` ausgeführt, identisch mit dem EA, der Marktorders via `CTrade` nutzte.
- Die Nulllinienfilter verhindern Einstiege in der entgegengesetzten Hälfte des MACD-Histogramms, genau wie im MQL-Skript.
- Die Portfoliowertprüfung basiert auf `Portfolio.CurrentValue`. Wenn die Handelsumgebung diesen Wert nicht liefert, wird die Prüfung automatisch bestanden, was die Strategie für simulierte Konten nutzbar hält.
- Der Zeichnungsbereich im Chart stellt Kerzen, den MACD-Indikator und Handelsmarkierungen dar, wenn ein Chartbereich in der Host-Anwendung verfügbar ist.
