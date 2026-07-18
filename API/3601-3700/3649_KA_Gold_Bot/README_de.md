# KA-Gold Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **KA-Gold Bot-Strategie** ist eine direkte Portierung des MetaTrader Expertenberaters „KA-Gold Bot“. Es handelt mit Ausbrüchen eines benutzerdefinierten Kanals im Keltner-Stil und gleicht Signale mit mittelfristigen Trendfiltern ab. Der Port basiert auf StockSharp High-Level-Kerzenabonnements, Indikatorbindungen und Strategieparametern, sodass das Verhalten über die Benutzeroberfläche konfigurierbar und für die Optimierung bereit bleibt.

## Handelslogik

- Berechnen Sie drei exponentielle gleitende Durchschnitte (EMA):
  - EMA(10) für eine schnelle Momentum-Bestätigung.
  - EMA(200), um den Trend zu höheren Zeitrahmen zu erkennen.
  - EMA(Punkt) als Mittelpunkt des Kanals; Die gleiche Länge wird verwendet, um den Kerzenbereich (Hoch-Tief) zu mitteln.
- Mitteln Sie den Tagesbereich mit einem einfachen gleitenden Durchschnitt, um dynamische Hüllkurven zu bilden:
  - Oberes Band = EMA(Punkt) + SMA(Hoch-Tief, Punkt).
  - Unteres Band = EMA(Periode) − SMA(Hoch-Tief, Periode).
- Ein **langes** Setup erfordert bei der letzten geschlossenen Kerze Folgendes:
  - Schlusskurs über dem oberen Band.
  - Schlusskurs über EMA(200).
  - EMA(10) kreuzte von unterhalb des vorherigen oberen Bandes nach oberhalb des neuesten oberen Bandes.
- Ein **kurzer** Aufbau spiegelt die Regeln wider:
  - Schlusskurs unterhalb des unteren Bandes.
  - Schlusskurs unter EMA(200).
  - EMA(10) hat von oberhalb des vorherigen unteren Bandes bis unterhalb des neuesten unteren Bandes gekreuzt.
- Es kann jeweils nur eine Stelle offen sein; Gegensignale werden ignoriert, bis die Strategie flach ist.

## Positionsgrößen

Es werden zwei Volumenmodelle unterstützt:

1. **Fester Losmodus** – verwenden Sie den Parameter `BaseVolume` direkt.
2. **Risikoprozentsatzmodus** – bei `UseRiskPercent = true` wird der Free-Equity-Proxy (`Portfolio.CurrentValue` oder `Portfolio.BeginValue`) mit `RiskPercent` multipliziert. Das Ergebnis wird mit 100.000 skaliert (Lotkonvention MetaTrader) und auf Vielfache von `BaseVolume` gerundet, wobei `Security.MinVolume`, `Security.MaxVolume` und `Security.VolumeStep` berücksichtigt werden.

## Risikomanagement

- Stop-Loss- und Take-Profit-Offsets werden in Pips definiert. Pips werden mithilfe des Sicherheitsschritts in absolute Preisabstände umgerechnet. Drei- und fünfdezimale Forex-Symbole verwenden die MetaTrader-Regel `pip = step × 10`.
- Erste Schutzaufträge werden unmittelbar nach der ersten Ausführung registriert und mit der aktuellen Positionsgröße synchronisiert.
- Trailing Stops werden aktiviert, sobald der nicht realisierte Gewinn `TrailingTriggerPips` erreicht:
  - Long-Positionen bleiben zurück, indem der Stopp `TrailingStopPips` vom Schluss entfernt bleibt.
  - Short-Positionen nutzen den symmetrischen Abstand über dem Markt.
  - Der Stopp wird nur verschoben, wenn sich der Abstand um mindestens `TrailingStepPips` verbessert, um eine Übertriggerung zu vermeiden.
- Wenn die Position geschlossen wird, werden ausstehende Schutzaufträge automatisch storniert.

## Sitzungs- und Spread-Filter

- Optionales Handelsfenster, gesteuert durch `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour` und `EndMinute` (Inklusiv-Exklusiv-Fenster). Nachtfenster werden unterstützt (Ende früher als Start nach Mitternacht).
- Ein optionaler Spread-Filter lehnt neue Eingaben ab, wenn der aktuelle Spread (Differenz zwischen bestem Brief- und Geldkurs in Preisschritten) `MaxSpreadPoints` überschreitet.

## Implementierungshinweise

- Kerzen werden über `SubscribeCandles().Bind(...)` verarbeitet; Die Werte EMA(10) und EMA(200) kommen über die Bindung an, während der Kanal EMA und der Bereichsdurchschnitt innerhalb des Handlers ohne Verwendung von `GetValue` aktualisiert werden.
- Der Indikatorstatus wird nur durch Skalarfelder gespeichert, die die Verschiebungslogik MetaTrader `iClose` und `CopyBuffer` widerspiegeln, wobei die Anforderung zum Vergleich der letzten beiden geschlossenen Balken erhalten bleibt.
- Die Schutz- und Nachfolgelogik verwendet High-Level-Reihenfolgehelfer (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`), um die `PositionModify`-Aufrufe von MetaTrader zu spiegeln.
- Die Portfoliogröße hängt von den verfügbaren Aktieninformationen in StockSharp ab; Fehlt es, greift die Strategie auf das Festvolumen zurück.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `KeltnerPeriod` | Zeitraum für den Kanal EMA und Bereichsglättung. | 50 |
| `FastEmaPeriod` | Länge des schnellen EMA-Filters. | 10 |
| `SlowEmaPeriod` | Länge des langsamen EMA-Trendfilters. | 200 |
| `BaseVolume` | Mindestbestellmenge (Losgröße). | 0,01 |
| `UseRiskPercent` | Aktivieren Sie die ausgleichsbasierte Positionsgrößenbestimmung. | wahr |
| `RiskPercent` | Prozentsatz des pro Trade verwendeten Eigenkapitals, wenn die Risikogrößenbestimmung aktiv ist. | 1 |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | 500 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips (0 deaktiviert). | 500 |
| `TrailingTriggerPips` | Gewinnschwelle, um den Trailing Stop zu aktivieren. | 300 |
| `TrailingStopPips` | Der Abstand wird durch den Trailing Stop aufrechterhalten, sobald er aktiviert ist. | 300 |
| `TrailingStepPips` | Minimale Verbesserung, bevor der Stopp verschoben wird. | 100 |
| `UseTimeFilter` | Schalten Sie den Handelssitzungsfilter um. | wahr |
| `StartHour`, `StartMinute` | Startzeit der Sitzung. | 02:30 |
| `EndHour`, `EndMinute` | Endzeit der Sitzung (exklusiv). | 21:00 |
| `MaxSpreadPoints` | Maximal zulässiger Spread in Preisschritten (0 = deaktiviert). | 65 |
| `CandleType` | Für Signalkerzen verwendeter Zeitrahmen. | 5-Minuten-Kerzen |

## Unterschiede im Vergleich zur MetaTrader-Version

- Die Trailing-Stop-Implementierung erstellt die `PositionModify`-Sequenz mithilfe von StockSharp-Stop-Orders neu; Die Funktionalität ist gleichwertig, basiert jedoch auf von der Börse bestätigten Bestellungen.
- MetaTrader berechnete Kanalbreite aus dem durchschnittlichen Hoch-Tief-Bereich; Der Port reproduziert die gleiche Mittelung mit einem einfachen gleitenden Durchschnitt, um Ausbrüche identisch zu halten.
- Bei der Risikodimensionierung wird auf Portfolioeigenkapital statt auf freie Marge zurückgegriffen. Diese Annäherung entspricht der Absicht (Prozentsatz des Kapitals), kann jedoch abweichen, wenn keine Leverage-spezifischen Margin-Daten verfügbar sind.
- Spread-Prüfungen verwenden `Security.BestAskPrice` und `Security.BestBidPrice`. Wenn keine Tiefe verfügbar ist, wird der Filter übersprungen und spiegelt die Option „Floating Spread“ im Original-Experten wider.

## Nutzungstipps

- Hängen Sie die Strategie an Instrumente an, bei denen die Pip-Definition den Forex-Konventionen (3 oder 5 Dezimalstellen) folgt, um die Risikoparameter mit dem ursprünglichen Experten in Einklang zu bringen.
- Optimieren Sie die EMA-Zeiträume und die Kanallänge für Nicht-Gold-Instrumente, da die Quellstrategie auf XAUUSD abgestimmt wurde.
- Überwachen Sie das Portfoliofenster, um sicherzustellen, dass Aktienwerte ausgefüllt sind, wenn `UseRiskPercent` aktiviert ist.
