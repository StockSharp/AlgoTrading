# MA RSI EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MA RSI EA-Strategie** reproduziert die Logik des ursprünglichen MetaTrader-Expertenberaters, der einen schnellen gleitenden Durchschnitt mit einem RSI-Filter kurzer Periode kombiniert. Die Strategie handelt auf der ausgewählten Kerzen-Serie, bewertet neue Orders nur bei fertigen Balken und verwendet dynamisches Positionsgrößen-Sizing basierend auf Kontostand oder Eigenkapital. Wenn der schwebende Gewinn aller offenen Positionen positiv wird, wird jede Position sofort geschlossen, um den Gewinn zu sichern.

## Indikatoren
- **Moving Average** – konfigurierbarer Methode (einfach, exponentiell, geglättet, linear gewichtet) mit Preisquellen-Auswahl und optionalem Shift.
- **Relative Strength Index (RSI)** – kurzfristiger Oszillator, der aus derselben Kerzenpreisfamilie wie in der MQL-Version liest.

## Handelslogik
1. Für jede abgeschlossene Kerze berechnet die Strategie die gleitenden Durchschnitts- und RSI-Werte unter Verwendung der konfigurierten Preisquellen.
2. Der aktuellste gleitende Durchschnittswert kann um eine benutzerdefinierte Anzahl von Balken verschoben werden, um das MQL-Verhalten zu entsprechen.
3. Es wertet das schwebende PnL der aktuellen Netto-Position aus:
   - Wenn das schwebende Ergebnis aller offenen Positionen **größer als null** ist, schließt die Strategie die gesamte Position, um den Gewinn zu realisieren.
   - Wenn das schwebende Ergebnis **negativ** ist, wird die Seite mit dem kleineren Verlust (Kauf-Seite vs. Verkauf-Seite) durch Eröffnen eines zusätzlichen Trades in dieser Richtung verstärkt.
4. Wenn kein Mittelungssignal vorhanden ist, wird der RSI + MA-Filter angewendet:
   - **Short-Einstieg** – RSI ≥ `RsiOverbought` und der Kerzen-Eröffnungspreis liegt unter dem verschobenen gleitenden Durchschnitt.
   - **Long-Einstieg** – RSI ≤ `RsiOversold` und der Kerzen-Eröffnungspreis liegt über dem verschobenen gleitenden Durchschnitt.

## Ausstiegslogik
- Positives schwebendes PnL löst `CloseAllPositions` aus und flattert die Strategie sofort.
- Manuelle Umkehrsignale aus der Mittelungslogik schließen das entgegengesetzte Exposure, da StockSharp mit Netto-Positionen arbeitet.

## Positionsgrößen
`LotSizingModes` spiegelt die `OptLot`-Auswahl aus dem EA:
- **Fixed** – sendet immer `LotSize`-Volumen.
- **Balance** – konvertiert `PercentOfBalance` des Portfoliowerts in Volumen unter Verwendung des Kerzen-Schlusskurses.
- **Equity** – konvertiert `PercentOfEquity` des aktuellen Portfolio-Eigenkapitals in Volumen.

Das berechnete Volumen wird auf das nächste `Security.VolumeStep` gerundet (wenn verfügbar), damit Orders der Lot-Größe des Instruments entsprechen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `LotOption` | Volumen-Berechnungsmodus (`Fixed`, `Balance`, `Equity`). | `Balance` |
| `LotSize` | Fester Lot-Wert für den `Fixed`-Modus. | `0.01` |
| `PercentOfBalance` | Prozentsatz des Kontostands im `Balance`-Modus. | `2` |
| `PercentOfEquity` | Prozentsatz des Eigenkapitals im `Equity`-Modus. | `3` |
| `FastMaPeriod` | Länge des gleitenden Durchschnitts. | `4` |
| `FastMaShift` | Shift auf das Ergebnis des gleitenden Durchschnitts. | `0` |
| `FastMaMethod` | Berechnungsmethode des gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `FastMaPrice` | Kerzenpreisquelle für den gleitenden Durchschnitt. | `Open` |
| `RsiPeriod` | RSI-Länge. | `4` |
| `RsiPrice` | Kerzenpreisquelle für den RSI. | `Open` |
| `RsiOverbought` | RSI-Level, der einen überkauften Markt definiert. | `80` |
| `RsiOversold` | RSI-Level, der einen überverkauften Markt definiert. | `20` |
| `CandleType` | Von der Strategie verwendete Kerzen-Serie. | `15-Minuten-Zeitrahmen` |

## Kerzenpreisquellen
`CandlePriceSources` repliziert die MQL-Applied-Price-Liste:
- `Open`, `High`, `Low`, `Close`
- `Median` = (High + Low) / 2
- `Typical` = (High + Low + Close) / 3
- `Weighted` = (High + Low + Close + Close) / 4

## Hinweise
- Orders werden nur generiert, wenn die Strategie online ist und die Kerze beendet ist, was dem ursprünglichen EA entspricht, der bei neuen Balken auslöst.
- Da StockSharp eine Netto-Position hält, reduzieren oder drehen Mittelungssignale automatisch das aktuelle Exposure, anstatt Hedge-Positionen zu erstellen.
- Python-Implementierung wird auf Anfrage absichtlich weggelassen.
