# Auto RXD v1.67 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Auto RXD v1.67 ist eine regelbasierte Strategie, die den gleichnamigen Expert Advisor MetaTrader emuliert. Der Ansatz verwendet drei lineare Perzeptrone: einen Supervisor, der entscheidet, ob nach bullischen oder bärischen Signalen gesucht wird, sowie ein eigenes Perzeptron für jede Richtung. Jedes Perzeptron arbeitet mit linear gewichteten gleitenden Durchschnitten (LWMAs), die aus Kerzenschluss und Robbie Ruans „gewichteten Preis“-Eingaben (Hoch + Tief + 2 × Schluss) berechnet werden. Der Port StockSharp wird nur bei abgeschlossenen Kerzen ausgeführt und verwendet den High-Level-Datenfluss `BindEx`, um die Indikatorberechnungen mit der Handelsschleife synchronisiert zu halten.

## Marktdaten und Indikatoren
- **Kerzen** – Der Standardzeitraum beträgt 30-Minuten-Kerzen. Der Zeitrahmen kann über den Parameter `CandleType` geändert werden.
- **Average True Range (ATR)** – Bietet sowohl adaptive Take-Profit- als auch Stop-Loss-Distanzen, wenn `UseAtrTargets` aktiviert ist. Der Zeitraum von ATR wird von `AtrPeriod` gesteuert.
- **Relative Strength Index (RSI)** – Optionaler Filter, der Long-Trades über der neutralen 50-Marke und Short-Trades unter 50 erzwingt, wenn `UseRsiFilter` wahr ist.
- **Commodity Channel Index (CCI)** – Optionaler Trendfilter, der Werte über +100 für Long-Positionen und unter -100 für Short-Positionen erfordert, wenn `UseCciFilter` aktiv ist.
- **Moving Average Convergence Divergence (MACD)** – Optionale Impulsbestätigung. Lange Einträge erfordern die Linie MACD über der Signallinie, während kurze Einträge die Linie MACD unter der Signallinie benötigen, wenn `UseMacdFilter` wahr ist.
- **Durchschnittlicher Richtungsindex (ADX)** – Optionaler Stärkefilter, der prüft, ob ADX über dem konfigurierten Schwellenwert liegt und ob +DI gegenüber -DI mit der gewünschten Richtung übereinstimmt, wenn `UseAdxFilter` aktiviert ist.

## Handelslogik
1. **Perceptron-Datenvorbereitung** – Für jede Kerze aktualisiert die Strategie die Puffer mit den neuesten Schluss- und gewichteten Preisen. Die Puffer füttern LWMA-Snapshots und erzeugen vier verzögerte Features, getrennt durch die konfigurierten `Step`-Werte für kurze, lange und Supervisor-Perzeptrone.
2. **Supervisor-Entscheidung** – Das Supervisor-Perzeptron bewertet die verzögerten Deltas anhand der Gewichtsparameter `SupervisorX1…X4` und `SupervisorThreshold`. Eine positive Punktzahl schaltet das lange Perzeptron frei; Ein negativer Wert schaltet das kurze Perzeptron frei. Wenn der Supervisor-Score Null ist oder nicht verfügbar ist (nicht genügend Daten), wird die Kerze übersprungen.
3. **Richtungsspezialisten** – Das passende Perzeptron (lang oder kurz) validiert seine eigene Punktzahl unter Verwendung desselben LWMA-Funktionssatzes und richtungsspezifischer Gewichtungen (`LongX*` oder `ShortX*`). Ein positiver Wert löst die nächste Validierungsstufe aus.
4. **Indikatorfilter** – Wenn `UseIndicatorFilters` falsch ist, handelt die Strategie ausschließlich auf dem Perzeptronsignal. Wenn „true“, muss jeder aktivierte Filter (RSI, CCI, MACD, ADX) mit der vorgeschlagenen Richtung übereinstimmen. Fehlende Indikatordaten oder fehlerhafte Bedingungen löschen das Signal.
5. **Auftragsausführung** – Die Strategie stellt sicher, dass es keine aktiven Aufträge gibt, glättet jegliches gegenteiliges Risiko und geht mit Marktaufträgen der Größe `OrderVolume` ein. Die Einstiegspreise entsprechen standardmäßig dem besten Angebot, sofern verfügbar, andernfalls schließt die Kerze.

## Risikomanagement
- **Schutzaufträge** – Nach dem Ausfüllen eines Eintrags berechnet die Strategie sofort Take-Profit- und Stop-Loss-Abstände bis `CalculateProtectiveDistances`. Wenn `UseAtrTargets` wahr ist, werden die Entfernungen um ATR mit den konfigurierten Multiplikatoren (`AtrTakeProfitFactor`, `AtrStopLossFactor`) und mit den ursprünglichen MQL punktbasierten TP/SL-Größen skaliert. Wenn das ATR-Targeting deaktiviert ist, werden Festpunktentfernungen in Preisschritte umgewandelt.
- **Auftragsverwaltung** – Der Helfer `SetProtectiveOrders` übersetzt Rohdistanzen in Preisschrittzahlen und registriert Stop-Loss- und Take-Profit-Aufträge im Verhältnis zum Einstiegspreis. Die Strategie vermeidet doppelte Aufträge, indem sie `HasActiveOrders()` überprüft, bevor neue Geschäfte übermittelt werden.
- **Schutz starten** – `StartProtection()` wird einmal in `OnStarted` aufgerufen und aktiviert die integrierte Schutzbehandlung des Frameworks, wenn die Position ungleich Null wird.

## Parameter
Die StockSharp-Implementierung stellt den vollständigen MQL-Parametersatz gruppiert zur Optimierung und Klarheit der Benutzeroberfläche bereit. Zu den wichtigsten Parametern gehören:

### Handel
- `OrderVolume` – Losgröße für neue Positionen.
- `CandleType` – Kerzendatentyp, der für die Bindung verwendet wird.

### Risiko
- `UseAtrTargets` – Umschalten zwischen ATR-basierten und Festpunkt-Schutzabständen.
- `AtrPeriod`, `AtrTakeProfitFactor`, `AtrStopLossFactor` – ATR Konfiguration für adaptive Ziele.
- `LongTakeProfitPoints`, `LongStopLossPoints`, `ShortTakeProfitPoints`, `ShortStopLossPoints` – Punktbasierte TP/SL-Referenzen, die sowohl im ATR- als auch im festen Modus wiederverwendet werden.

### Indikatorfilter
- `UseIndicatorFilters` – Hauptschalter für alle Filter.
- `UseAdxFilter`, `AdxPeriod`, `AdxThreshold` – ADX Bestätigungseinstellungen.
- `UseMacdFilter`, `MacdFast`, `MacdSlow`, `MacdSignal` – MACD Bestätigungseinstellungen.
- `UseRsiFilter`, `RsiPeriod` – RSI Bestätigungseinstellungen.
- `UseCciFilter`, `CciPeriod` – CCI Bestätigungseinstellungen.

### Perceptron-Spezialisten
- `ShortMaPeriod`, `ShortStep`, `ShortX1…ShortX4`, `ShortThreshold` – Kurze Perzeptronkonfiguration.
- `LongMaPeriod`, `LongStep`, `LongX1…LongX4`, `LongThreshold` – Lange Perzeptronkonfiguration.
- `SupervisorMaPeriod`, `SupervisorStep`, `SupervisorX1…SupervisorX4`, `SupervisorThreshold` – Supervisor-Perzeptron-Konfiguration.

Alle numerischen Parameter spiegeln die MQL-Standardwerte wider und ermöglichen ein vergleichbares Verhalten zwischen dem ursprünglichen Expert Advisor und diesem StockSharp-Port, während die Konfiguration über das `StrategyParam`-System für Optimierungskampagnen verfügbar gemacht wird.
