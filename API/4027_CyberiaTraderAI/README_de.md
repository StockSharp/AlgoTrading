# Cyberia Trader KI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Konvertierung des Expert Advisors **CyberiaTrader.mq4 (Build 8553)**. Das ursprüngliche MQL-Programm mischt a
Wahrscheinlichkeits-Engine mit einer Sammlung optionaler Trendfilter. Der C#-Port behält die gleiche Struktur bei: Ein Wahrscheinlichkeitsmodell sucht
für den zuverlässigsten Stichprobenzeitraum und dann können optionale MACD-, EMA- und Umkehrfilter Trades ablehnen.

## Indikatoren und internes Modell

- **Probability Engine** – iteriert Kandidaten-Stichprobenzeiträume (`MaxPeriod`) und wertet `SamplesPerPeriod` historische Segmente aus.
Für jede Periode berechnet die Engine:
  - Entscheidungsrichtung (Kauf/Verkauf/Flat) basierend auf aufeinanderfolgenden bullischen/bärischen Ein-Minuten-Kerzen im Abstand des Abtastzeitraums.
  - Durchschnittliche „Möglichkeits“-Amplituden für Kauf, Verkauf und undefinierte Ergebnisse sowie der Anteil der oben genannten erfolgreichen Ergebnisse
`SpreadThreshold`.
  - Erfolgsquoten, die den Zeitraum mit der besten Leistung auswählen.
- **EMA Trendfilter** – optionaler exponentieller gleitender Durchschnitt (`EnableMa`), der Trades gegen die aktuelle Steigung blockiert.
- **MACD-Filter** – optionale Konvergenz/Divergenz des gleitenden Durchschnitts (`EnableMacd`), die den Handel gegen das Momentum verbietet.
- **Reversal Detector** – optionaler Spike-Detektor (`EnableReversalDetector`), der Berechtigungen umkehrt, wenn die Wahrscheinlichkeiten darüber steigen
`ReversalFactor` Vielfache ihrer Durchschnittswerte.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `MaxPeriod` | Größter Stichprobenschritt, der von der Wahrscheinlichkeits-Engine überprüft wird. |
| `SamplesPerPeriod` | Anzahl der pro Periodenkandidaten verarbeiteten Segmente (spiegelt MQL `ValuesPeriodCount` wider). |
| `SpreadThreshold` | Minimale Amplitude, die als erfolgreiches Wahrscheinlichkeitsergebnis gilt. |
| `EnableCyberiaLogic` | Aktiviert die Cyberia-Wahrscheinlichkeitsschalter, die Käufe oder Verkäufe deaktivieren können. |
| `EnableMacd` | Aktiviert den Momentumfilter MACD. |
| `EnableMa` | Aktiviert den Steigungsfilter EMA. |
| `EnableReversalDetector` | Aktiviert das Umschalten der Berechtigungen des Umkehrdetektors bei extremen Spitzen. |
| `MaPeriod` | EMA Länge, die vom Trendfilter verwendet wird. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD schneller EMA, langsamer EMA und Signalperioden. |
| `ReversalFactor` | Multiplikator, der den Umkehrdetektor auslöst. |
| `CandleType` | Vom Modell verarbeiteter Kerzendatentyp (Standard 1 Minute). |
| `TakeProfitPercent` | Optionaler schützender Take-Profit, ausgedrückt in Prozent. |
| `StopLossPercent` | Optionaler schützender Stop-Loss, ausgedrückt in Prozent. |

## Handelslogik

1. Jede abgeschlossene Kerze aktualisiert die lokale Verlaufswarteschlange und berechnet die Wahrscheinlichkeitsstatistik für jeden Zeitraum von 1 bis neu
`MaxPeriod`. Der Zeitraum mit der höchsten Erfolgsquote wird zur aktiven Konfiguration.
2. Die Cyberia-Logik setzt `DisableBuy`/`DisableSell`-Flags unter Verwendung derselben Vergleiche wie der MQL-Code:
   - Vergleicht durchschnittliche Kauf-/Verkaufsmöglichkeiten und ihre erfolgsgewichteten Varianten, wenn der Zeitraum zunimmt oder abnimmt.
   - Deaktiviert Einträge, wenn neue Möglichkeiten das Doppelte ihres Erfolgsdurchschnitts überschreiten.
3. Optionale Filter werden in der Reihenfolge angewendet: MACD, EMA Steigung, dann der Umkehrdetektor.
4. Wenn keine Position offen ist, kommt die Strategie zum Tragen, wenn die aktuelle Entscheidung Kauf (oder Verkauf) ist und die entsprechende Möglichkeit größer ist
sein erfolgreicher Durchschnitt, während die Gegenrichtung deaktiviert ist.
5. Während eine Position vorhanden ist, prüft der Code die gleichen Bedingungen, um sie zu schließen, wenn die Wahrscheinlichkeitsmaschine umkippt oder wenn Filter dies verbieten
aktuelle Richtung.
6. `StartProtection` reproduziert die ursprünglichen Geldverwaltungsblöcke, wenn Risikoparameter ungleich Null angegeben werden.

## Hinweise zur Konvertierung

- Der Port behält die statistischen Berechnungen bei, ersetzt jedoch die Tick-basierte Spread-Prüfung durch den konfigurierbaren `SpreadThreshold`.
- Die automatische Losgrößen- und Bilanzdiagnose aus dem MQL-Skript ist nicht implementiert. Die Lautstärke von StockSharp wird über `Volume` gesteuert.
- Die Module MoneyTrain und Pipsator sind in der oben beschriebenen einheitlichen Ein-/Ausstiegslogik zusammengefasst, um der Verwendung von API auf hoher Ebene gerecht zu werden.
- Die Strategie fügt Diagrammzeichnungen für Kerzen, EMA und MACD hinzu, um die Validierung im Designer zu erleichtern.
