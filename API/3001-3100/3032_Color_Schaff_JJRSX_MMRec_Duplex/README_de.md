# Color Schaff JJRSX MMRec Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader-Expert Advisors `Exp_ColorSchaffJJRSXTrendCycle_MMRec_Duplex`. Der ursprüngliche Roboter kombiniert zwei Schaff Trend Cycle-Oszillatoren, die von JJRSX-Momentum angetrieben werden, und ein MMRec-Modul (Money Management Recalculation), das die Risikoexposition nach einer Verlustserie reduziert. Die C#-Konvertierung bewahrt das duale Long/Short-Layout und spiegelt die einstellbaren Risikokontrollen wider, während der nicht verfügbare JJRSX-Indikator durch eine robuste plattforminterne Approximation ersetzt wird.

## Handelslogik
- Zwei unabhängige Oszillatoren werden auf benutzergewählten Zeitrahmen berechnet: einer steuert Long-Einstiege, der andere steuert Short-Einstiege. Jeder Oszillator verwendet schnelle und langsame RSX-Stil-Momentum-Linien, die mit einer Schaff Trend Cycle-Pipeline geglättet und normalisiert werden, um Werte im Bereich [-100, 100] zu erzeugen.
- Eine Long-Position wird eröffnet, wenn der Long-Oszillator durch null nach unten kreuzt (`previous > 0` und `current <= 0`). Der ursprüngliche Expert markiert diese Ereignisse als bullische Momentum-Umkehrungen. Long-Ausstiege werden ausgelöst, wenn der Indikatorwert einen Bar zuvor negativ ist.
- Eine Short-Position wird eröffnet, wenn der Short-Oszillator durch null nach oben kreuzt (`previous < 0` und `current >= 0`). Short-Ausstiege werden ausgelöst, wenn der Indikatorwert einen Bar zuvor positiv ist.
- Die `SignalBar`-Einstellung reproduziert das MetaTrader-Verhalten der Signalauswertung auf historischen Bars. Zum Beispiel inspiziert `SignalBar = 1` die letzte vollständig geschlossene Kerze und die Kerze davor. Die Strategie pflegt rollende Indikatorhistorien, um die `CopyBuffer`-Aufrufe aus dem MQL-Code zu emulieren.

## Money Management (MMRec)
- Separate Money-Management-Blöcke werden für Long- und Short-Trades gepflegt. Das Basisvolumen entspricht `Strategy.Volume * MM`, wobei `MM` der konfigurierbare normale Multiplikator ist (`LongMm`/`ShortMm`).
- Nach jedem geschlossenen Trade zeichnet die Strategie auf, ob das Ergebnis profitabel war oder nicht (basierend auf den Einstiegs-/Ausstiegs-Kerzenpreisen, identisch mit der EA-Logik, die den Verlauf über `HistorySelect` verfolgt).
- Wenn die letzten `TotalTrigger` Trades mindestens `LossTrigger` Verlierer enthalten, wechselt die nächste Order für diese Seite zum reduzierten Multiplikator (`SmallMm`). Wenn die Verlustbedingung verschwindet, wird der Basis-Multiplikator automatisch wiederhergestellt.
- Positionsumkehrungen respektieren die MMRec-Regeln: Das Wechseln von Long zu Short (oder umgekehrt) schließt zunächst das Ergebnis des bestehenden Trades ab und aktualisiert die Verlustzähler, bevor die neue Order dimensioniert wird.

## Indikator-Approximation
Der ursprüngliche Roboter basiert auf einem maßgeschneiderten `ColorSchaffJJRSXTrendCycle`-Indikator, der auf dem JJRSX-Oszillator und Jurik-Glättungsbibliotheken aufbaut. StockSharp enthält diese Komponenten nicht, daher implementiert die Konvertierung `ColorSchaffJjrsxTrendCycleIndicator`:
- Eine leichtgewichtige RSI-Approximation (`SimpleRsi`) berechnet die Momentum-Basislinie mit exponentieller Glättung, identisch mit dem Glättungszeitraum des EA.
- Schnelle und langsame RSI-Kurven werden subtrahiert, um eine MACD-ähnliche Reihe zu erhalten, die dann über ein zyklisches Fenster normalisiert und mit einem konfigurierbaren Faktor (Standard 0.5) doppelt geglättet wird, um das Schaff Trend Cycle-Verhalten zu imitieren.
- Der Indikator akzeptiert dieselben Preisquellen (Schlusskurs, Eröffnung, Hoch, Tief, Median, Typical, Weighted usw.) und behält die Zyklus-/Längenparameter bei, damit Optimierungs-Workflows der Quellstrategie treu bleiben.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Long | `LongCandleType` | Kerzentyp oder Zeitrahmen für den Long-Indikator. |
| Long | `LongTotalTrigger` | Anzahl der abgeschlossenen Long-Trades, die bei der Auswertung des Verlustzählers untersucht werden. |
| Long | `LongLossTrigger` | Mindestanzahl von Verlusten im untersuchten Fenster, die den reduzierten Multiplikator aktiviert. |
| Long | `LongSmallMm` | Reduzierter Volumen-Multiplikator nach wiederholten Verlusten. |
| Long | `LongMm` | Standard-Long-Volumen-Multiplikator. |
| Long | `LongEnableOpen` | Aktiviert Long-Einstiege. |
| Long | `LongEnableClose` | Aktiviert Long-Ausstiege. |
| Long | `LongFastLength` | Schnelle JJRSX-Periodenannäherung. |
| Long | `LongSlowLength` | Langsame JJRSX-Periodenannäherung. |
| Long | `LongSmooth` | Exponentielle Glättungslänge vor der Schaff-Normalisierung. |
| Long | `LongCycleLength` | Zyklusfenster für Min/Max-Normalisierung. |
| Long | `LongSignalBar` | Historischer Versatz bei der Analyse von Long-Signalen. |
| Long | `LongAppliedPrice` | Preisquelle für den Long-Indikator. |
| Short | `ShortCandleType` | Kerzentyp oder Zeitrahmen für den Short-Indikator. |
| Short | `ShortTotalTrigger` | Anzahl der abgeschlossenen Short-Trades bei der Verlustzählerauswertung. |
| Short | `ShortLossTrigger` | Mindestanzahl von Verlusten im untersuchten Fenster (Short-Seite). |
| Short | `ShortSmallMm` | Reduzierter Volumen-Multiplikator nach wiederholten Verlusten (Short). |
| Short | `ShortMm` | Standard-Short-Volumen-Multiplikator. |
| Short | `ShortEnableOpen` | Aktiviert Short-Einstiege. |
| Short | `ShortEnableClose` | Aktiviert Short-Ausstiege. |
| Short | `ShortFastLength` | Schnelle JJRSX-Periodenannäherung für Shorts. |
| Short | `ShortSlowLength` | Langsame JJRSX-Periodenannäherung für Shorts. |
| Short | `ShortSmooth` | Exponentielle Glättungslänge vor der Schaff-Normalisierung (Short). |
| Short | `ShortCycleLength` | Zyklusfenster für Min/Max-Normalisierung (Short-Seite). |
| Short | `ShortSignalBar` | Historischer Versatz bei der Analyse von Short-Signalen. |
| Short | `ShortAppliedPrice` | Preisquelle für den Short-Indikator. |

## Implementierungshinweise
- Die Strategie verwendet StockSharp's High-Level-Kerzenabonnements und vermeidet direkten Zugriff auf Indikatorpuffer gemäß den Konvertierungsrichtlinien.
- Schutzstops (`StopLoss`/`TakeProfit`) aus der MQL-Version werden nicht portiert, da MetaTrader punktbasierte Abstände verwendet; Benutzer können `StartProtection` oder benutzerdefinierte Risikomodule bei Bedarf anhängen.
- Trade-Historie wird mit Kerzen-Schlusskursen bewertet, was die Abhängigkeit des EA von historischen Deal-Aufzeichnungen spiegelt und gleichzeitig die Logik innerhalb von StockSharp deterministisch hält.
- Der benutzerdefinierte Indikator exponiert `IsFormed`, damit die Strategie erst reagiert, wenn genügend Daten angesammelt wurden, um vorzeitige Signale während der Aufwärmphase zu verhindern.

## Haftungsausschluss
Dieser Port repliziert die logische Struktur der MetaTrader-Strategie, aber die Performance kann aufgrund von Daten-Feeds, Ausführungsrichtlinien und der JJRSX-Approximation abweichen. Validieren Sie das Verhalten immer auf Demo-Daten, bevor Sie es live einsetzen.
