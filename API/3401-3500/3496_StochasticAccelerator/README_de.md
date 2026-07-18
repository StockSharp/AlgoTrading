# Stochastic Accelerator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Stochastic Accelerator-Strategie ist eine Umsetzung des MetaTrader 5 expert *#2 stoch mt5*. Der ursprüngliche Roboter wertet drei aus
Stochastische Oszillatoren zusammen mit Bill Williams' Accelerator Oscillator und dem Awesome Oscillator. Eine Long-Position wird eröffnet
Nur wenn sich alle stochastischen Filter auf ein bullisches Momentum einigen und der Accelerator-Oszillator eine Empfindlichkeitsschwelle überschreitet.
Short-Positionen nutzen die symmetrischen Regeln. Sobald ein Handel läuft, überwacht der Awesome Oscillator die Momentumumkehr bis zum Abschluss
die Belichtung. Der StockSharp-Port reproduziert diese Mechanismen und stützt sich dabei auf das High-Level-Kerzenabonnement API und
Indikatorbindungen.

Die Strategie behält das Geldverwaltungsprofil vom EA bei. Die Einträge werden mit einem festen Losbetrag, während Stop-Loss und
Take-Profit-Entfernungen werden in MetaTrader Pips ausgedrückt. Die StockSharp-Implementierung verwendet `StartProtection`, also die konfigurierte
Risikolimits werden automatisch mit jeder neuen Position verknüpft. Preisschritte werden in MetaTrader Pip-Einheiten umgewandelt, um dies beizubehalten
gleiche Schutzabstände zwischen Maklern.

## Handelslogik
1. Abonnieren Sie die durch `CandleType` definierte primäre Kerzenserie und verarbeiten Sie nur fertige Kerzen, die das Original EA widerspiegeln.
2. Füttere drei `StochasticOscillator`-Instanzen:
   - Die **Signalstochastik** prüft, ob %K über oder unter %D liegt.
   - Die **Einstiegsstochastik** bestätigt, dass bullische Signale über `EntryLevel` bleiben (oder unter `100 - EntryLevel` für Shorts).
   - Der **Filter Stochastik** stellt sicher, dass bullische Setups unter `FilterLevel` (oder über `100 - FilterLevel` für Shorts) bleiben.
3. Verfolgen Sie den Accelerator-Oszillator und verlangen Sie, dass er `AcceleratorLevel` überschreitet, um lange Eingaben zu bestätigen. Shorts erfordern ein
Kreuzen Sie unten `-AcceleratorLevel` an.
4. Schließen Sie alle offenen Positionen, wenn der Awesome Oscillator das `AwesomeLevel`-Band in die entgegengesetzte Richtung durchquert.
5. Öffnen Sie nach dem Abflachen eine neue Position, wenn genau eine Seite alle Eingabefilter erfüllt. Die Lautstärke wird an das Wertpapier angepasst
Losschritt, damit die Anfrage für echte Makler weiterhin gültig bleibt.
6. Wenden Sie Stop-Loss- und Take-Profit-Abstände mit `StartProtection` an und behalten Sie dabei die gleichen Pip-basierten Risikokontrollen wie bei MetaTrader bei.
Experte.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 4 Stunden | Von der Strategie verarbeitete Primärkerzen. |
| `TradeVolume` | `decimal` | `0.01` | Für Neuzugänge (Lots) verwendetes Volumen. |
| `StopLossPips` | `decimal` | `40` | Stop-Loss-Distanz in MetaTrader Pips. |
| `TakeProfitPips` | `decimal` | `70` | Take-Profit-Distanz in MetaTrader Pips. |
| `SignalKPeriod` | `int` | `40` | %K Periode der Bestätigungsstochastik. |
| `SignalDPeriod` | `int` | `10` | %D Glättung der Bestätigungsstochastik. |
| `SignalSlowing` | `int` | `10` | Zusätzliche Glättung für die Bestätigungsstochastik. |
| `EntryKPeriod` | `int` | `40` | %K Periode des stochastischen Eintrags. |
| `EntryDPeriod` | `int` | `10` | %D Glättung des Eingangs stochastisch. |
| `EntrySlowing` | `int` | `10` | Zusätzliche Glättung für die Eingangsstochastik. |
| `EntryLevel` | `decimal` | `20` | Unterer Schwellenwert, der ein bullisches Momentum bestätigt (Shorts verwenden `100 - EntryLevel`). |
| `FilterKPeriod` | `int` | `40` | %K Periode des stochastischen Filters. |
| `FilterDPeriod` | `int` | `10` | %D Glättung des Filters stochastisch. |
| `FilterSlowing` | `int` | `10` | Zusätzliche Glättung für den Filter Stochastik. |
| `FilterLevel` | `decimal` | `75` | Oberer Schwellenwert, der bullische Setups begrenzt (Shorts verwenden `100 - FilterLevel`). |
| `AcceleratorLevel` | `decimal` | `0.0002` | Mindestamplitude des Beschleunigeroszillators, die für Einträge erforderlich ist. |
| `AwesomeLevel` | `decimal` | `0.0013` | Tolles Oszillatorband, das Handelsausstiege auslöst. |

## Unterschiede zum ursprünglichen MetaTrader-Experten
- Der StockSharp-Port verwendet Kerzenabonnements mit Indikatorbindungen anstelle wiederholter `CopyBuffer`-Aufrufe.
- Die Auftragsverwaltung erfolgt im Nettopositionsmodus. Wenn der EA sofort umkehren würde, schließt die Konvertierung zuerst den
das aktuelle Engagement und gibt dann auf der Gegenseite eine neue Marktorder aus.
- Stop-Loss- und Take-Profit-Abstände werden über `StartProtection` angehängt, wobei Pip-Größenberechnungen verwendet werden, die daraus abgeleitet werden
Preisschritt des Instruments. Dadurch werden manuelle Ticketänderungen vermieden, während die Entfernungen mit MetaTrader Punkten identisch bleiben.
- Volumenanfragen werden auf die Werte `VolumeStep`, `MinVolume` und `MaxVolume` des Wertpapiers normalisiert, sodass der Code live einsatzbereit ist
Handelsumgebungen.

## Anwendungstipps
- Passen Sie `TradeVolume` an den Mindestlotschritt des Instruments an, bevor Sie die Strategie ausführen.
- Passen Sie die stochastischen Pegel (`EntryLevel` und `FilterLevel`) zusammen mit den Oszillatorschwellen fein an, um den Filter anzupassen
Strenge für Ihren Markt.
- Aktivieren Sie die Diagrammzeichnung, sofern verfügbar, um die drei stochastischen Oszillatoren, den Accelerator Oscillator und den Awesome, zu visualisieren
Oszillator und ausgeführte Trades.
- Da die Logik auf fertige Kerzen wartet, erscheinen am Ende jedes Balkens Signale; Verwenden Sie einen Backtester mit demselben Zeitrahmen
für konsistente Ergebnisse.

## Indikatoren
- Drei `StochasticOscillator`-Instanzen mit unabhängigen Glättungs- und Schwellenwerteinstellungen.
- `AcceleratorOscillator` für die Einreisebestätigung.
- `AwesomeOscillator` für Exit-Timing.
