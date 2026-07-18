# Two MA RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Two MA RSI Strategie ist eine Konvertierung des ursprünglichen MetaTrader-Expert Advisors "2MA_RSI". Sie verwendet eine Kreuzung eines schnellen und eines langsamen exponentiellen gleitenden Durchschnitts (EMA), bestätigt durch einen Relative Strength Index (RSI)-Filter. Orders werden mit einem Martingal-ähnlichen Geldverwaltungsblock dimensioniert, der das nächste Ordervolumen nach einem Verlust erhöht. Die StockSharp-Version arbeitet vollständig auf abgeschlossenen Kerzen und reproduziert das ursprüngliche Take-Profit- und Stop-Loss-Verhalten in Preispunkten.

## Daten und Indikatoren
- Die Strategie abonniert eine einzige Kerzenserie, definiert durch `CandleType` (standardmäßig 5-Minuten-Kerzen).
- Drei Indikatoren werden auf jeder abgeschlossenen Bar berechnet:
  - `FastLength` EMA (auf den Kerzenschlusskurs angewendet).
  - `SlowLength` EMA.
  - RSI mit Länge `RsiLength`.
- Historische Indikatorwerte werden intern gespeichert, um EMA-Kreuzungen zu erkennen, ohne Daten aus Indikatorpuffern abzurufen.

## Einstiegslogik
1. Die vorherige Kerze muss abgeschlossen sein, um eine Intrabar-Neubewertung zu vermeiden.
2. Es ist keine aktive Position erlaubt (`Position == 0`).
3. **Long-Einstieg:**
   - Die schnelle EMA kreuzt über die langsame EMA (schnelle EMA auf der aktuellen Bar ist größer als die langsame EMA, während die vorherige Bar schnelle EMA < langsame EMA hatte).
   - Der RSI-Wert liegt unter `RsiOversold`, was einen überverkauften Markt bestätigt.
4. **Short-Einstieg:**
   - Die schnelle EMA kreuzt unter die langsame EMA mit der analogen Bedingung (schnelle EMA jetzt unter langsamer EMA, vorher darüber).
   - RSI liegt über `RsiOverbought`, was einen überkauften Markt signalisiert.
5. Wenn alle Bedingungen erfüllt sind, sendet die Strategie eine Marktorder, die gemäß dem Martingal-Modul dimensioniert ist.

## Ausstiegslogik
- Ein Schutz-Stop-Loss und ein Take-Profit werden unmittelbar nach jedem Einstieg berechnet. Abstände werden in "Punkten" definiert und über den `PriceStep` des Instruments umgerechnet:
  - **Long:**
    - Stop Loss = `Einstiegspreis - StopLossPoints * PriceStep`.
    - Take Profit = `Einstiegspreis + TakeProfitPoints * PriceStep`.
  - **Short:**
    - Stop Loss = `Einstiegspreis + StopLossPoints * PriceStep`.
    - Take Profit = `Einstiegspreis - TakeProfitPoints * PriceStep`.
- Nur diese Schutzlevels schließen einen Trade. Die Strategie wartet auf die nächste Kerze, um zu bestätigen, ob das Tief/Hoch das Ziel oder den Stop berührt hat, und sendet entsprechend eine `ClosePosition()`-Marktorder.
- Die Ausstiegspriorität entspricht dem konservativen Verhalten des ursprünglichen Roboters: Ein Stop-Loss wird vor einem Take-Profit ausgewertet, wenn beide Levels in denselben Kerzenbereich fallen.

## Positionsgrößenbestimmung und Martingal
1. Das Basisvolumen wird bei jedem Einstieg berechnet als `floor(balance / BalanceDivider) * VolumeStep`. Der Wert bleibt immer bei oder über einem Volumenschritt und verwendet `CurrentValue` des Portfolios (fällt auf `BeginValue` zurück, wenn nötig).
2. Nach jedem verlierenden Ausstieg erhöht sich die Martingal-Stufe um eins bis maximal `MaxDoublings`. Das nächste Ordervolumen wird mit `2^stage` multipliziert.
3. Jeder gewinnende Trade oder das Erreichen der maximalen Verdoppelungsanzahl setzt die Stufe auf null zurück und kehrt zum Basisvolumen zurück.
4. Wenn `MaxDoublings` null oder negativ ist, erhöht sich die Größe nie und entspricht dem Basisvolumen.

## Zusätzliches Verhalten
- Die Strategie verfolgt intern frühere EMA-Werte und fragt keine historischen Indikatorwerte ab.
- Orders werden nur ausgeführt, wenn die Strategie online ist, Indikatoren gebildet sind und der Handel erlaubt ist.
- Die Diagrammausgabe zeichnet Preiskerzen, eigene Trades und die drei Indikatoren für die visuelle Analyse.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `FastLength` | Länge des schnellen EMA. | 5 |
| `SlowLength` | Länge des langsamen EMA. | 20 |
| `RsiLength` | Anzahl der Bars, die in der RSI-Berechnung verwendet werden. | 14 |
| `RsiOverbought` | RSI-Niveau, das neue Longs blockiert und Shorts erlaubt. | 70 |
| `RsiOversold` | RSI-Niveau, das Longs erlaubt. | 30 |
| `StopLossPoints` | Stop-Loss-Abstand in Preisschritten ausgedrückt. | 500 |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten. | 1500 |
| `BalanceDivider` | Dividiert den Portfoliowert zur Ermittlung der Basisordergröße. | 1000 |
| `MaxDoublings` | Maximale Anzahl von Martingal-Verdoppelungen nach aufeinanderfolgenden Verlusten. | 1 |
| `CandleType` | Von der Strategie verwendete Kerzenserie. | 5-Minuten-Zeitrahmen |

## Verwendungshinweise
- Ein Portfolio und ein Wertpapier mit gültigen `PriceStep`- und `VolumeStep`-Metadaten bereitstellen, damit das punktebasierte Risikomanagement und die Positionsgrößenbestimmung konsistent bleiben.
- Da Marktorders für Ausstiege verwendet werden, sind Slippage und Spreads im Vergleich zu den Limit-Orders der MetaTrader-Version möglich, aber die Stop/Take-Auswertungslogik ist erhalten.
- Die Strategie erstellt keine Python-Version; nur die C#-Implementierung wird wie angefordert geliefert.
