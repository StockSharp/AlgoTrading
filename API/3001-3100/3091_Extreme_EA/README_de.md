# Extreme EA (StockSharp-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Extreme EA** Strategie ist ein Trendfolge-Experte, der ursprünglich für MetaTrader geschrieben wurde. Sie kombiniert zwei gleitende Durchschnitte mit einem Commodity Channel Index (CCI) Filter und einem adaptiven Geldverwaltungsmodul. Dieser Port hält die Handelslogik intakt, während alle wichtigen Parameter über die High-Level-API von StockSharp zugänglich gemacht werden. Die Strategie operiert nur auf abgeschlossenen Kerzen und ist mit mehreren Zeitrahmen kompatibel, indem die gleitenden Durchschnitte und der CCI auf unabhängigen Kerzenabonnements ausgeführt werden.

## Strategie-Überblick

1. **Trendfilter:** Zwei gleitende Durchschnitte werden auf dem konfigurierbaren `MaCandleType` berechnet. Der schnelle Durchschnitt verfolgt kurzfristiges Momentum, während der langsame Durchschnitt die dominante Trendsteigung definiert. Die Strategie prüft die Steigung des langsamen Durchschnitts mit den zwei vorherigen Werten, um die ursprünglichen `CopyBuffer`-Array-Offsets aus dem MQL-Code nachzuahmen.
2. **Momentum-Filter:** Der CCI wird auf seinem eigenen Zeitrahmen (`CciCandleType`) und seiner Preisquelle ausgewertet. Der letzte abgeschlossene Wert wird zwischengespeichert und wiederverwendet, bis eine neue CCI-Kerze erscheint, was dem Verhalten der MetaTrader-Puffer entspricht.
3. **Einstiegsregeln:**
   - Long eintreten, wenn der langsame MA steigt, der schnelle MA steigt und der CCI unter das untere Niveau fällt.
   - Short eintreten, wenn der langsame MA fällt, der schnelle MA fällt und der CCI über das obere Niveau steigt.
4. **Ausstiegsregeln:**
   - Alle Longs schließen, wenn der langsame MA aufhört zu steigen.
   - Alle Shorts schließen, wenn der langsame MA aufhört zu fallen.

## Risikomanagement

- **MaximumRisk** steuert die Zielpositionsgröße basierend auf dem aktuellen Portfolio-Eigenkapital und dem letzten Preis. Wenn das berechnete Volumen null ist oder die Portfolio-Werte nicht verfügbar sind, greift die Strategie auf das konfigurierte `Volume` oder das Börsenminimum zurück.
- **DecreaseFactor** reduziert das berechnete Volumen nach zwei oder mehr aufeinanderfolgenden Verlust-Trades. Die Reduktion spiegelt die ursprüngliche Formel `lot = lot - lot * losses / DecreaseFactor` wider.
- **HistoryDays** begrenzt, wie lange eine Verluststrähne erinnert wird. Wenn ein Schließungshandel nach der angegebenen Anzahl von Tagen stattfindet, wird die Strähne vor der Anwendung der Reduktion zurückgesetzt.
- **MaxPositions** begrenzt die Pyramidisierung, indem die Nettoexposition pro Richtung begrenzt wird. Wenn das Limit erreicht wird, werden neue Einstiege unterdrückt, bis die Exposition sinkt.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `MaximumRisk` | `decimal` | `0.05` | Eigenkapitalanteil für die Dimensionierung jedes neuen Trades. |
| `DecreaseFactor` | `decimal` | `6` | Verluststrähnen-Reduktionsfaktor. Auf `0` setzen, um zu deaktivieren. |
| `HistoryDays` | `int` | `60` | Anzahl der Tage, die beim Zählen aufeinanderfolgender Verluste aufbewahrt werden. |
| `MaxPositions` | `int` | `3` | Maximale gleichzeitige Einstiege pro Richtung. |
| `FastMaPeriod` | `int` | `15` | Periode für den schnellen gleitenden Durchschnitt. |
| `SlowMaPeriod` | `int` | `75` | Periode für den langsamen gleitenden Durchschnitt. |
| `CciPeriod` | `int` | `12` | Lookback-Länge für den CCI. |
| `CciUpperLevel` | `decimal` | `50` | Oberer CCI-Schwellenwert für Shorts. |
| `CciLowerLevel` | `decimal` | `-50` | Unterer CCI-Schwellenwert für Longs. |
| `MaCandleType` | `DataType` | `15m` | Zeitrahmen für beide gleitenden Durchschnitte und Ausführung. |
| `CciCandleType` | `DataType` | `30m` | Zeitrahmen für den CCI-Filter. |
| `MaMethod` | `MaMethod` | `Exponential` | Glättungsmethode (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaPriceMode` | `AppliedPriceMode` | `Median` | Preiseingabe für die gleitenden Durchschnitte. |
| `CciPriceMode` | `AppliedPriceMode` | `Typical` | Preiseingabe für den CCI. |

## Implementierungshinweise

- Die Strategie abonniert den Zeitrahmen der gleitenden Durchschnitte einmal und optional ein zweites Abonnement für den CCI. Wenn beide Zeitrahmen übereinstimmen, speist ein einzelnes Abonnement beide Komponenten und reproduziert den ursprünglichen Einzel-Chart-Workflow.
- Vorherige Indikatorwerte werden in privaten Feldern zwischengespeichert, um die Vergleiche `ma_slow_array[1]`, `ma_slow_array[2]` und `ma_fast_array[0]` ohne manuelle Indikatorpuffer zu emulieren.
- Die Positionsgrößenbestimmung wird gegen den Instrument-Volumenschritt, das Minimum und Maximum normalisiert, um abgelehnte Orders zu vermeiden.
- Das Risikomodul zeichnet Einstiegs- und Ausstiegspreise auf, um den realisierten PnL pro abgeschlossener Position zu schätzen, was die in MetaTrader verwendete `HistoryDealGet`-Schleife ersetzt.

## Unterschiede zur MQL-Version

- MetaTrader-spezifische Funktionen wie `FreeMarginCheck`, `MarginCheck` und `HistorySelect` werden mit StockSharp-Portfolio-Metriken und dem internen Verluststrähnen-Tracker approximiert.
- Der StockSharp-Port operiert auf Nettopositionen. Schließungsorders glätten die gesamte Exposition in der relevanten Richtung, was mit dem konsolidierten Positionsmodell übereinstimmt.
- Protokollierungsroutinen aus dem Original-EA wurden zugunsten der integrierten Diagnose von StockSharp weggelassen.
