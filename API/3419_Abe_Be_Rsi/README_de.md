# ABE BE RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **ABE BE RSI-Strategie** ist eine Portierung des MetaTrader-Expertenberaters `Expert_ABE_BE_RSI`. Das System kombiniert klassische Candlestick-Umkehrmuster mit der Momentum-Bestätigung durch den Relative Strength Index (RSI). Zwei aufeinanderfolgende Kerzen müssen ein bullisches oder bärisches Engulfing-Muster bilden, und die zuletzt abgeschlossene Kerze muss einen RSI-Wert innerhalb vordefinierter Schwellenwerte aufweisen. Zusätzliche RSI-Kreuzregeln werden angewendet, um bestehende Positionen abzuflachen oder umzukehren, was die Entscheidungslogik der ursprünglichen MQL-Implementierung weitgehend widerspiegelt.

## Handelslogik

1. **Erkennung verschlingender Muster**
Die Strategie bewertet die beiden zuletzt abgeschlossenen Kerzen. Ein bullisches Signal erfordert:
   - Kerze *t-2* schließt tiefer als sie öffnet (bärischer Körper).
   - Kerze *t-1* schließt höher als sie öffnet (bullischer Körper).
   - Die Körpergröße der Kerze *t-1* übersteigt den gleitenden Durchschnitt der jüngsten Körpergrößen (Standard: fünf Balken).
   - Kerze *t-1* schließt über dem Eröffnungskurs von Kerze *t-2* und öffnet unter ihrem Schlusskurs, was ein echtes Engulfing-Ereignis gewährleistet.
   - Der Mittelpunkt der Kerze *t-2* liegt unter dem gleitenden Durchschnitt der Schlusskurse, was einen kurzfristigen Abwärtstrend bestätigt.

Ein bärisches Engulfing-Signal nutzt die symmetrischen Bedingungen: Die ältere Kerze ist bullisch, die neuere Kerze ist bärisch mit einem überdurchschnittlich großen Körper und die neuere Kerze umhüllt den vorherigen Körper vollständig, während der Mittelpunkt des älteren Balkens über dem gleitenden Durchschnitt liegt, um eine Erschöpfung des Abwärtstrends zu bestätigen.

2. **RSI Bestätigung**
   - Für Long-Einträge muss der RSI der zuletzt geschlossenen Kerze unter dem konfigurierten bullischen Einstiegsniveau liegen (Standard 40).
   - Für Short-Einstiege muss der RSI der zuletzt geschlossenen Kerze über dem rückläufigen Einstiegsniveau liegen (Standard 60).

3. **Exit-Management**
RSI Crossovers über zwei Ebenen werden überwacht, um bestehende Positionen zu schließen:
   - Short-Positionen werden gedeckt, wenn RSI entweder über die untere (Standard 30) oder obere (Standard 70) Ausstiegsschwelle steigt, nachdem sie bei der vorherigen Kerze darunter lag.
   - Long-Positionen werden geschlossen, wenn RSI unter einen der Schwellenwerte fällt, nachdem er bei der vorherigen Kerze darüber lag.

4. **Auftragsausführung**
Marktaufträge werden sowohl für Ein- als auch für Ausstiege verwendet. Bei der Umkehrung schließt die Strategie zunächst das aktuelle Exposure und steigt dann in die neue Richtung mit dem konfigurierten Basisvolumen ein. Die Positionsgrößenbestimmung ahmt das Fixed-Lot-Modell des MQL-Experten nach.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Auftragsgröße in Verträgen. | `0.1` |
| `RsiPeriod` | Anzahl der vom RSI-Filter verwendeten Balken. | `11` |
| `MovingAveragePeriod` | Zeitraum für die Größe des Kerzenkörpers und die gleitenden Durchschnitte des Schlusskurses. | `5` |
| `BullishEntryLevel` | Maximaler RSI-Wert, der immer noch einen bullischen Engulfing-Einstieg bestätigt. | `40` |
| `BearishEntryLevel` | Mindestwert RSI erforderlich für einen bärischen Engulfing-Einstieg. | `60` |
| `ExitLowerLevel` | Unteres Kreuzungsniveau RSI für Abflachungspositionen. | `30` |
| `ExitUpperLevel` | Oberes Kreuzungsniveau RSI für Abflachungspositionen. | `70` |
| `CandleType` | Von der Strategie verarbeitete Kerzenserie. | `1 hour time frame` |

Dank der `StrategyParam`-Wrapper können alle Parameter innerhalb von Designer oder Runner optimiert werden.

## Indikatorpipeline

- **Relative Strength Index (RSI)** – berechnet die Dynamik über den konfigurierbaren `RsiPeriod` und liefert Einstiegs-/Ausstiegsschwellen.
- **Einfacher gleitender Durchschnitt der Schlusskurse** – bietet einen Trendkontext zur Validierung verschlingender Muster.
- **Einfacher gleitender Durchschnitt der Kerzenkörpergrößen** – stellt sicher, dass die umschließende Kerze größer ist als die durchschnittliche Körpergröße der letzten `MovingAveragePeriod` Balken.

## Nutzungshinweise

- Die Strategie wirkt nur auf vollständig abgeschlossene Kerzen (`CandleStates.Finished`). Teilbalkendaten werden ignoriert, um vorzeitige Signale zu vermeiden.
- Der Kerzenverlauf wird intern gespeichert, um Engulfing-Bedingungen zu bewerten, ohne große Sammlungen zu durchlaufen, und dabei die projektweiten Konvertierungsrichtlinien zu berücksichtigen.
- `StartProtection()` ist aktiviert, sodass die grundlegenden StockSharp-Schutzmechanismen aktiv werden, wenn die Positionsexposition ungleich Null ist.

## Unterschiede zum ursprünglichen Expert Advisor

- Der ursprüngliche Expert Advisor basiert auf dem Signal-Voting-System von MetaTrader. In diesem Port werden die Stimmen in direkte Ein- und Austrittsaktionen umgesetzt, die dieselben Bedingungen nachbilden.
- Die Geldverwaltung wird auf einen einzigen `Volume`-Parameter vereinfacht, der die vom Quellenexperten verwendete feste Losgröße (`Money_FixLot_Lots`) widerspiegelt.
- Trailing-Stop-Unterstützung ist nicht enthalten, da die MT5-Version ein „No Trailing“-Modul verwendet.

## Empfohlene Tests

1. Hängen Sie die Strategie an ein Diagramm in Designer oder API Runner mit einem Symbol an, das in der Vergangenheit auf umfassende Umkehrungen (z. B. große FX-Paare) reagiert.
2. Überprüfen Sie die Parameter RSI und gleitenden Durchschnitt, bevor Sie Live-Sitzungen ausführen. Die Standardeinstellungen reproduzieren die veröffentlichten Expert Advisor-Einstellungen.
3. Nutzen Sie die integrierten Optimierungsfunktionen, um alternative RSI-Schwellenwerte oder Durchschnittszeiträume für verschiedene Märkte zu erkunden.
