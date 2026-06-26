# EMA LWMA RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **EMA LWMA RSI-Strategie** reproduziert den MetaTrader-Expertenberater "EMA LWMA RSI" in StockSharp. Sie vergleicht zwei gleitende Durchschnitte, die denselben angewendeten Preis und optional eine Vorwärtsverschiebung verwenden, während ein Relative-Stärke-Index-Filter das Momentum bestätigt. Der Algorithmus reagiert nur auf neu abgeschlossene Kerzen des konfigurierten Zeitrahmens und handelt eine einzelne Nettoposition: Er schließt jede entgegengesetzte Exposition, bevor er eine neue Order in der signalisierten Richtung eröffnet. Stop-Loss- und Take-Profit-Abstände werden in Pips konfiguriert und automatisch auf die Tick-Größe des Instruments skaliert.

## Handelslogik
1. Einen exponentiellen gleitenden Durchschnitt (EMA) und einen linear gewichteten gleitenden Durchschnitt (LWMA) mit individuellen Längen, aber demselben angewendeten Preis berechnen. Wenn `MaShift` größer als null ist, werden beide Durchschnitte um die angegebene Anzahl von Bars nach vorne verschoben, um das MetaTrader-"Shift"-Argument zu spiegeln.
2. Einen RSI mit seinem eigenen angewendeten Preis verarbeiten. Die Strategie verwendet den klassischen 50-Schwellenwert, um bullisches und bärisches Momentum zu unterscheiden.
3. Wenn eine abgeschlossene Kerze ankommt:
   - Ein **Kauf**-Signal wird generiert, wenn EMA **über** LWMA kreuzt (vorheriger EMA war größer als vorheriger LWMA, aber aktueller EMA ist kleiner als aktueller LWMA) und der RSI-Wert **über 50** liegt.
   - Ein **Verkauf**-Signal wird generiert, wenn EMA **unter** LWMA kreuzt (vorheriger EMA war kleiner als vorheriger LWMA, aber aktueller EMA ist größer als aktueller LWMA) und der RSI-Wert **unter 50** liegt.
4. Signale setzen interne ausstehende Flags. Vor dem Umkehren schließt die Strategie zuerst die bestehende Position mit `ClosePosition()`. Nach der Bestätigung des Fills wird sofort eine Marktorder in der angeforderten Richtung gesendet.
5. Schutz-Orders werden über `StartProtection` gestartet. Wenn ein Stop-Loss oder Take-Profit deaktiviert ist (auf null gesetzt), wird diese Seite weggelassen, entsprechend dem MQL-Verhalten.

## Implementierungshinweise
- Die Auswahl des angewendeten Preises unterstützt die MetaTrader-Optionen (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet, Durchschnitt). Der gewichtete Preis wird als `(Hoch + Tief + 2 * Schluss) / 4` berechnet, identisch mit `PRICE_WEIGHTED`.
- Pip-Sizing multipliziert den `PriceStep` des Instruments automatisch mit 10 für 3/5-stellige Forex-Symbole, sodass ein Pip 10 Punkte auf Bruchkurs entspricht.
- Indikatoranbindungen stützen sich auf StockSharpss High-Level-Kerzenabonnement. Die Shift-Behandlung verwendet `Shift`-Indikatoren statt manueller Pufferindizierung.
- Der Code hält boolesche Flags für ausstehende Kauf-/Verkaufsanfragen. Sie verhindern doppelte Orders, während der vorherige Befehl noch aussteht.
- Chart-Helfer zeichnen beide gleitenden Durchschnitte auf dem Preisfeld und den RSI auf einem separaten Bereich zur visuellen Inspektion.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `1h TimeFrame` | Kerzenreihe, die von der Strategie verarbeitet wird. |
| `StopLossPips` | `int` | `150` | Stop-Loss-Abstand in Pips. `0` deaktiviert den Stop. |
| `TakeProfitPips` | `int` | `150` | Take-Profit-Abstand in Pips. `0` deaktiviert das Ziel. |
| `EmaPeriod` | `int` | `28` | Periode des exponentiellen gleitenden Durchschnitts. |
| `LwmaPeriod` | `int` | `8` | Periode des linear gewichteten gleitenden Durchschnitts. |
| `MaShift` | `int` | `0` | Vorwärtsverschiebung (Bars), auf beide gleitende Durchschnitte angewendet. |
| `RsiPeriod` | `int` | `14` | Mittelungsperiode des RSI. |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Angewendeter Preis für EMA und LWMA. |
| `RsiAppliedPrice` | `AppliedPriceType` | `Weighted` | Angewendeter Preis für den RSI. |

## Verwendung
1. Die Strategie an das gewünschte Instrument anhängen und `CandleType` auf den in MetaTrader verwendeten Zeitrahmen einstellen.
2. Pip-basierte Schutzmaßnahmen und Indikatoreinstellungen anpassen, wenn der Broker andere Standards verwendet.
3. Handel aktivieren, sobald das Abonnement live ist. Die Strategie verwaltet jeweils eine Position und verwendet `ClosePosition()` vor dem Richtungswechsel.

Für diese Strategie ist noch keine Python-Übersetzung verfügbar.
