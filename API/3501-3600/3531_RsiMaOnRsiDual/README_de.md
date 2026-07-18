# RSI MA auf RSI Dual Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die RSI MA auf RSI Dual-Strategie erstellt den MetaTrader-Expertenberater „RSI_MAonRSI_Dual“ innerhalb von StockSharp neu. Es überwacht zwei Relative-Stärke-Indizes mit unterschiedlichen Lookback-Zeiträumen und wendet einen gemeinsamen gleitenden Durchschnitt über jedem RSI-Stream an. Handelsentscheidungen werden getroffen, wenn die geglätteten RSI-Linien einander kreuzen und dabei auf der gleichen Seite eines konfigurierbaren neutralen Niveaus bleiben.

Die Konvertierung behält das Verhalten des ursprünglichen Roboters bei, einschließlich Zeitfilterung und der Möglichkeit, die Handelsrichtung einzuschränken oder die Signallogik umzukehren.

## Indikatoren

- **Schnell RSI** – Relative-Stärke-Index mit konfigurierbarem Zeitraum.
- **Langsam RSI** – Relative-Stärke-Index mit eigener Periode.
- **Gleitender Durchschnitt auf RSI** – Einfacher gleitender Durchschnitt, der über jedem RSI-Wertstrom berechnet wird. Beide RSIs verwenden die gleiche Glättungslänge.

Für alle drei Indikatoren gilt der gleiche angewandte Preis (standardmäßig Schlusskurs). Die beiden geglätteten RSI-Linien werden zur Überwachung in ein spezielles Diagrammfeld gezeichnet.

## Einreisebestimmungen

1. Warten Sie, bis sich beide geglätteten RSI-Werte auf dem aktuell abgeschlossenen Balken gebildet haben.
2. **Lange Einrichtung**
   - Der schnell geglättete RSI kreuzt **oberhalb** den langsam geglätteten RSI (aktueller Wert oben, vorheriger Wert unten).
   - Beide geglätteten RSIs liegen **unterhalb** des neutralen Niveaus (standardmäßig 50).
3. **Kurze Einrichtung**
   - Der schnell geglättete RSI kreuzt **unterhalb** den langsam geglätteten RSI (aktueller Wert unten, vorheriger Wert oben).
   - Beide geglätteten RSIs liegen **über** dem neutralen Niveau.
4. Optional können Sie die Signalrichtungen mithilfe des Parameters `ReverseSignals` umkehren.
5. Signale, die auf demselben Balken erzeugt werden, werden ignoriert (ein Eintrag pro Balken).

## Positionsmanagement

- `AllowLong` und `AllowShort` steuern, ob die Strategie Positionen in jede Richtung eröffnen darf.
- `CloseOpposite` schließt eine bestehende Position, bevor die Gegenseite betreten wird, und repliziert dabei die ursprüngliche EA-Logik.
- `OnlyOnePosition` verbietet die Eröffnung einer neuen Position, wenn eine Position bereits aktiv ist.
- Marktaufträge werden mit der Strategie `Volume` erteilt.

## Zeitfilter

Aktivieren oder deaktivieren Sie den Handelssitzungsfilter mit `UseTimeFilter`. Wenn diese Option aktiviert ist, sind Trades nur zwischen `SessionStart` und `SessionEnd` zulässig. Sitzungen, die über Mitternacht hinausgehen, werden unterstützt. Die Zeitstempel werden in der Börsenzeitzone ausgewertet, die durch die eingehenden Kerzennachrichten bereitgestellt wird.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Von der Strategie analysierte Kerzenserie. |
| `FastRsiPeriod` | Zeitraum des Fastens RSI. |
| `SlowRsiPeriod` | Zeitraum der langsamen RSI. |
| `MaPeriod` | Länge des gleitenden Durchschnitts, die zum Glätten beider RSI-Streams verwendet wird. |
| `AppliedPrice` | Preistyp, der in die RSI-Berechnungen weitergeleitet wird. |
| `NeutralLevel` | RSI Schwellenwert, der bullische und bärische Zonen trennt. |
| `AllowLong` / `AllowShort` | Handelsrichtung aktivieren oder deaktivieren. |
| `ReverseSignals` | Tauschen Sie Long- und Short-Signale aus. |
| `CloseOpposite` | Schließen Sie die gegenüberliegende Position, bevor Sie eine neue eingeben. |
| `OnlyOnePosition` | Erlauben Sie höchstens eine offene Stelle. |
| `UseTimeFilter` | Aktivieren Sie den Handelssitzungsfilter. |
| `SessionStart` / `SessionEnd` | Grenzen des Handelsfensters. |

## Unterschiede zum Original EA

- Money-Management-, Stop-Loss- und Trailing-Stop-Blöcke des ursprünglichen MQL5-Codes werden nicht reproduziert. Die StockSharp-Strategie platziert Marktaufträge mithilfe des festen `Volume`, der für die Strategie konfiguriert ist.
- Alle Protokollierungs- und Diagnosewarnungen wurden entfernt; Bei Bedarf sollte stattdessen die StockSharp-Protokollierung verwendet werden.
- Die plattformspezifische Transaktionsverfolgung wird durch StockSharp Bestellstatusereignisse ersetzt.

Trotz dieser Unterschiede stimmen die Kerneintragslogik und die Richtungsfilter mit denen des Quellen-Expertenberaters überein.
