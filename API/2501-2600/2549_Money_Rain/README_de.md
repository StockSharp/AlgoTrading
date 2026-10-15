# Money Rain Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des originalen **MoneyRain (barabashkakvn-Edition)** Expertenberaters von MQL5 zur StockSharp High-Level-API.
- Verwendet den DeMarker-Oszillator zur Richtungswahl: Werte über 0.5 lösen Long-Einstiege aus, Werte bei oder unter 0.5 lösen Short-Einstiege aus.
- Handelt jeweils nur eine Position und verlässt sich auf feste Stop-Loss-/Take-Profit-Abstände in Punkten.

## Marktdaten & Indikatoren
- Abonniert den konfigurierbaren `CandleType` (Standard: 30-Minuten-Zeitrahmen).
- Berechnet einen einzelnen `DeMarker`-Indikator mit einstellbarer `DeMarkerPeriod` (Standard: 31).
- Abonniert Level-1-Quotes zur Schätzung des aktuellen Spreads, der für die adaptive Positionsgrößenlogik erforderlich ist.

## Handelslogik
1. Verarbeitet nur abgeschlossene Kerzen, um mit der originalen „neuer Balken"-Logik (`iTime(0)`-Prüfung in MQL) übereinzustimmen.
2. Solange eine Position besteht, überwacht der Hoch/Tief der Kerze gegenüber vorberechneten Stop-Loss- und Take-Profit-Niveaus. Wird eines davon berührt, schließt die Position mit einer Marktorder und markiert das Ergebnis als Verlust oder Gewinn.
3. Wenn keine offene Position vorhanden und das Verlustlimit nicht erreicht ist, wird das Handelsvolumen berechnet.
4. Einstieg Long bei `DeMarker > 0.5`; andernfalls Short. Die Strategie storniert ruhende Aufträge vor dem Senden der Marktorder.

## Geldmanagement
- Repliziert die `getLots()`-Logik der MQL-Version durch Verfolgung von:
  - `_lossesVolume`: kumulatives Volumen der zuletzt verlierenden Trades, skaliert an der Basis-Lotgröße.
  - `_consecutiveLosses` und `_consecutiveProfits`: Streakzähler, die entscheiden, wann der Verlustakkumulator zurückgesetzt wird.
- Wenn nach einer Verlustserie der erste profitable Trade erscheint (`_consecutiveProfits == 0`), wird die nächste Ordergröße nach der Originalformel erhöht:
  \[
  \text{volume} = \text{BaseVolume} \times \frac{_lossesVolume \times (\text{StopLossPoints} + \text{spread})}{\text{TakeProfitPoints} - \text{spread}}
  \]
- Der Spread wird aus besten Geld-/Brief-Kursen (in Punkten) geschätzt und ignoriert, wenn Level-1-Daten noch nicht verfügbar sind.
- `FastOptimize = true` deaktiviert die adaptive Größenanpassung und verwendet immer das Basis-Lot.

## Risikokontrollen
- `StopLossPoints` und `TakeProfitPoints` werden über den Kursschrittpreis des Wertpapiers in absolute Preise umgerechnet, mit einem zusätzlichen 10x-Multiplikator für 3- oder 5-stellige Symbole (spiegelt die `digits_adjust`-Logik aus MQL wider).
- `LossLimit` blockiert weitere Trades, sobald die Anzahl aufeinanderfolgender Verluste den benutzerdefinierten Schwellenwert überschreitet (Standard: praktisch deaktiviert bei 1.000.000).

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `DeMarkerPeriod` | Durchschnittszeitraum des DeMarker-Indikators. | 31 |
| `TakeProfitPoints` | Take-Profit-Abstand in DeMarker-Punkten. | 5 |
| `StopLossPoints` | Stop-Loss-Abstand in DeMarker-Punkten. | 20 |
| `BaseVolume` | Standardordervolumen (Lotgröße). | 0.01 |
| `LossLimit` | Maximal erlaubte aufeinanderfolgende Verluste vor Pause. | 1.000.000 |
| `FastOptimize` | Bei `true` wird die adaptive Positionsgrößenberechnung deaktiviert. | `false` |
| `CandleType` | Kerzendatentyp für Berechnungen. | 30-Minuten-Kerzen |

## Implementierungshinweise
- Stops und Targets werden durch Prüfung der Kerzenextremen emuliert. Die Intrabar-Füllreihenfolge kann nicht wiederhergestellt werden, daher begünstigen gleichzeitige Berührungen den Stop-Loss-Zweig (konservative Annahme).
- `OnOwnTradeReceived` erkennt, wenn eine Schutzausstiegsorder abgeschlossen wurde, damit die Strategie Streak-Zähler und Verlustvolumen-Akkumulator aktualisieren kann.
- Der Code behält Einrückung mit Tabs und verwendet englische Kommentare gemäß den Repository-Richtlinien.

## Dateien
- `CS/MoneyRainStrategy.cs` – Strategieimplementierung.
- `README.md` / `README_ru.md` / `README_zh.md` – mehrsprachige Dokumentation.

## Unterschiede zur MQL-Version
- Brokerseitige Schutzorders werden durch marktbasierte Ausstiege auf Basis von Kerzenbereichen ersetzt.
- Der Spread wird aus Level-1-Quotes statt direkt aus Symbol-Metadaten geschätzt.
- Mailfunktionalität und explizite `IsTradeAllowed`-Prüfungen werden ausgelassen, da die StockSharp-Umgebung die Konnektivität separat verwaltet.
