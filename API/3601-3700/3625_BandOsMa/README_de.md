# BandOsMa-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **BandOsMa-Strategie** wandelt den MetaTrader 5 „BandOsMA“-Expertenberater in eine StockSharp-Strategie um. Es wertet das MACD-Histogramm (OsMA) mithilfe von Bollinger-Bändern aus, die direkt auf den Histogrammwerten basieren. Ausbrüche über oder unter den Bändern erzeugen Einstiegssignale, während ein zusätzlicher gleitender Durchschnitt des Histogramms Signalausgänge verwaltet.

Die Strategie basiert auf einem einzelnen Symbol und einem vom Benutzer ausgewählten Zeitrahmen. Die Indikatorwerte werden für fertige Kerzen mithilfe der High-Level-Kerzenabonnements von StockSharp berechnet.

## Handelslogik
1. **Indikatoren**
   - `MovingAverageConvergenceDivergenceSignal` stellt das MACD-Histogramm (OsMA) bereit.
   - `BollingerBands` wird auf die OsMA-Sequenz angewendet, um extreme Abweichungen zu erkennen.
   - Ein konfigurierbarer gleitender Durchschnitt glättet das Histogramm und fungiert als Ausgangsfilter.
2. **Eintrag**
   - Ein **Long-Signal** erscheint, wenn der aktuelle OsMA unterhalb des unteren Bandes schließt, während der vorherige Balken darüber blieb.
   - Ein **Short-Signal** erscheint, wenn der aktuelle OsMA über dem oberen Band schließt, während der vorherige Balken darunter blieb.
3. **Ausstieg**
   - Signale werden gelöscht, wenn das Histogramm den gleitenden Durchschnitt in die entgegengesetzte Richtung kreuzt.
   - Wenn eine offene Position nicht mehr mit dem aktiven Signal übereinstimmt, wird die Position sofort geschlossen.
   - Jeder Position ist ein Pip-basierter Stop-Loss zugeordnet. Der Stopp fungiert auch als Trailing-Stop mit derselben Distanz und einem Trailing-Schritt gleich `StopLossPoints / 50` (Spiegelung der Hilfsklasse MetaTrader).

## Positionsmanagement
- **Stop Loss & Trailing**: Die Stop-Distanz wird in MetaTrader Punkten ausgedrückt und mithilfe des `PriceStep` des Instruments in Preiseinheiten umgerechnet. Der gleiche Abstand wird für den Trailing Stop verwendet, der sich nach vorne bewegt, sobald sich der Schlusskurs um mindestens den Trailing Step verbessert.
- **Eine Position nach der anderen**: Es wird nur eine Nettoposition beibehalten. Entgegengesetzte Signale schließen die aktuelle Position, bevor ein neuer Eintrag in Betracht gezogen wird.

## Parameter
| Gruppe | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Allgemein | `CandleType` | Zeitrahmen für das Kerzenabonnement und die Indikatorberechnung. | `H1` |
| Risiko | `LotSize` | Handelsvolumen in Lots. | `0.01` |
| Risiko | `StopLossPoints` | Stop-Loss-Distanz, ausgedrückt in MetaTrader Punkten (wird auch für Trailing verwendet). | `1000` |
| Indikatoren | `MacdFastPeriod` | Schnelle Länge von EMA in MACD. | `12` |
| Indikatoren | `MacdSlowPeriod` | Langsame EMA-Länge in MACD. | `26` |
| Indikatoren | `MacdSignalPeriod` | Signallänge von EMA in MACD. | `9` |
| Indikatoren | `PriceType` | Angewandter Preis für MACD-Eingabe (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Typical` |
| Indikatoren | `BollingerPeriod` | Periode von Bollinger Bändern über der OsMA-Sequenz. | `26` |
| Indikatoren | `BollingerShift` | Verschiebung wird auf Bollinger Puffer angewendet (nicht negativ). | `0` |
| Indikatoren | `BollingerDeviation` | Standardabweichungsmultiplikator für Bollinger-Bänder. | `2` |
| Indikatoren | `MovingAveragePeriod` | Länge des auf OsMA angewendeten gleitenden Durchschnitts. | `10` |
| Indikatoren | `MovingAverageShift` | Auf den gleitenden Durchschnittspuffer angewendete Verschiebung (nicht negativ). | `0` |
| Indikatoren | `MovingAverageMethod` | Typ des gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |

## Implementierungshinweise
- Die Kerzenverarbeitung verwendet `WhenCandlesFinished`, um sicherzustellen, dass nur die letzten Balken die Logik steuern.
- Indikatorwerte werden in Verlaufspuffern gespeichert, um Pufferverschiebungen im MetaTrader-Stil zu emulieren. Negative Verschiebungen werden nicht unterstützt; Verwenden Sie Nullwerte oder positive Werte wie in den ursprünglichen Expertenstandards.
- Trailing-Stops basieren auf Kerzenschließungen und nicht auf Tick-für-Tick-Aktualisierungen. Passen Sie den Pip-Abstand an, wenn ein präzises Nachlaufen auf Tick-Ebene erforderlich ist.

## Nutzung
1. Wählen Sie das gewünschte Symbol und den gewünschten Zeitrahmen in StockSharp aus.
2. Konfigurieren Sie die Parameter, insbesondere `CandleType`, `LotSize` und Indikatorzeiträume.
3. Starten Sie die Strategie; Es abonniert Kerzen, berechnet die Indikatoren und führt Geschäfte gemäß der beschriebenen Logik aus.
