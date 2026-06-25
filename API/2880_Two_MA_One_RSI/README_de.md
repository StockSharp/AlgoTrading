# Two MA One RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader 5-Experten "Two MA one RSI" nach StockSharp. Sie kombiniert einen Kreuzungs-Crossover eines schnellen und langsamen gleitenden Durchschnitts mit einer RSI-Bestätigung, die auf der vorherigen geschlossenen Kerze ausgewertet wird. Flexible Schalter ermöglichen es, jeden Vergleich in eine "größer als"- oder "kleiner als"-Regel umzuwandeln, sodass die Konfiguration ohne Codeänderungen invertiert werden kann.

## Details
- **Einstiegskriterien**:
  - Long-Signale erfordern, dass die schnelle MA vor zwei Bars unter der langsamen MA lag, die schnelle MA auf der zuletzt geschlossenen Bar über der langsamen MA liegt, und der RSI der vorherigen Bar über dem oberen Schwellenwert liegt. Jeder Vergleich kann durch boolesche Parameter umgekehrt werden.
  - Short-Signale spiegeln die Logik und prüfen die entgegengesetzten MA-Verhältnisse zusammen mit dem RSI, der unter den unteren Schwellenwert fällt.
  - Beide MAs verwenden denselben Durchschnittstyp; die langsame Periode ist immer `FastMaPeriod * SlowPeriodMultiplier`. Optionale horizontale Verschiebungen reproduzieren das MT5-Verhalten, bei dem Indikatorwerte mehrere Kerzen zurück gelesen werden.
- **Long/Short**: Die Strategie kann Positionen in beide Richtungen eröffnen. `CloseOppositePositions` steuert, ob ein neues Signal die Gegenseite schließt, bevor eingestiegen wird.
- **Ausstiegskriterien**:
  - Konfigurierbarer Stop-Loss und Take-Profit in Pips.
  - Optionaler Trailing Stop, der sich nur bewegt, nachdem der Preis mindestens `TrailingStopPips + TrailingStepPips` über den Einstieg hinaus vorangeschritten ist.
  - `ProfitClose` überwacht schwebende P&L (unter Verwendung des Instrumentenpreisschritts) und schließt alle Positionen, sobald der Zielwährungsbetrag erreicht wird.
- **Stops**: Wenn `StopLossPips` null ist, verlässt sich die Strategie ausschließlich auf das Trailing-Stop-Modul (falls aktiviert). `TrailingStopPips` erfordert ein positives `TrailingStepPips`, passend zur Validierung des Original-Experten.
- **Standardwerte**:
  - `FastMaPeriod = 10`, `SlowPeriodMultiplier = 2`.
  - `FastMaShift = 3`, `SlowMaShift = 0`.
  - `RsiPeriod = 10`, `RsiUpperLevel = 70`, `RsiLowerLevel = 30`.
  - `StopLossPips = 50`, `TakeProfitPips = 150`, `TrailingStopPips = 15`, `TrailingStepPips = 5`.
  - `MaxPositions = 10`, `ProfitClose = 100`, `TradeVolume = 1`.
- **Filter**: Sechs boolesche Schalter (`BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper`, `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower`) ermöglichen es dem Benutzer, den Sinn jedes Vergleichs sofort zu ändern.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen (oder anderer Kerzentyp) für die Analyse. |
| `MaType` | Gleitender-Durchschnitt-Typ (einfach, exponentiell, geglättet, gewichtet, volumengewichtet). |
| `FastMaPeriod` | Periode des schnellen MA. |
| `SlowPeriodMultiplier` | Periodenmultiplikator für den langsamen MA (`langsam = schnell * Multiplikator`). |
| `FastMaShift`, `SlowMaShift` | Horizontale Verschiebungen in Kerzen bei der Auswertung von MA-Werten. |
| `RsiPeriod` | RSI-Länge (verwendet die vorherige abgeschlossene Kerze). |
| `RsiUpperLevel`, `RsiLowerLevel` | RSI-Schwellenwerte für Long- und Short-Bestätigungen. |
| `BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper` | Vergleiche für Long-Signale ein-/ausschalten. |
| `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower` | Vergleiche für Short-Signale ein-/ausschalten. |
| `StopLossPips`, `TakeProfitPips` | Schutzstop und Ziel in Pips (Pip-Größe aus dem Preisschritt des Instruments abgeleitet). |
| `TrailingStopPips`, `TrailingStepPips` | Trailing-Stop-Abstand und minimale Verbesserung. |
| `MaxPositions` | Maximale Anzahl gleichzeitiger Einstiege pro Richtung (`0` = unbegrenzt). |
| `ProfitClose` | Währungsgewinnziel, das alle Positionen bei Erreichen schließt. |
| `CloseOppositePositions` | Ob die Gegenseite vor dem Öffnen eines neuen Handels geschlossen werden soll. |
| `TradeVolume` | Basisordergröße; synchronisiert sich auch mit der Strategie-Eigenschaft `Volume`. |

## Implementierungshinweise
- Alle Entscheidungen verwenden nur abgeschlossene Kerzen und entsprechen der "neuer Bar"-Logik des MT5-Experten.
- Die Pip-Größe entspricht dem Instrumentenpreisschritt. Wenn Ihr Markt fraktionale Pip-Preise verwendet, passen Sie die Instrumenteneinstellungen entsprechend an, damit die Pip-Übersetzung der ursprünglichen `digits_adjust`-Logik des Experten entspricht.
- Trailing Stops beginnen erst, nachdem der Preis um `TrailingStopPips + TrailingStepPips` vorgerückt ist; der Stop wird dann `TrailingStopPips` vom Schlusskurs entfernt verankert und bewegt sich nur, wenn er sich um mindestens `TrailingStepPips` verbessert.
- `ProfitClose` berechnet schwebenden Gewinn mit `PriceStep` und `StepPrice` des Instruments. Stellen Sie sicher, dass diese Felder für korrekte Währungsergebnisse konfiguriert sind.
