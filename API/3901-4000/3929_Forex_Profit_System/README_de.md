# Forex Profit System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert den klassischen MetaTrader-Expertenberater „Forex Profit System“ innerhalb des StockSharp-High-Level-API. Es nutzt
drei exponentielle gleitende Durchschnitte (EMA 10, 25 und 50) für den Kerzenmittelpreis kombiniert mit einem Parabolic SAR-Filter. Die
Die Kombination erkennt kurzlebige Impulsausbrüche, die auftreten, nachdem der schnelle Durchschnitt die langsame Trendlinie kreuzt, während der Parabolic
SAR ist bereits auf die gleiche Seite wie der Preis gekippt.

## Handelslogik

1. **Indikatorstapel**
   - Der aus der fertigen Kerze abgeleitete Medianpreis bestimmt alle Indikatoren, sodass die Ergebnisse mit dem ursprünglichen MetaTrader „PRICE_MEDIAN“ übereinstimmen.
Eingabe.
   - Fast EMA (Länge 10) reagiert schnell auf kurzfristige Impulsverschiebungen.
   - Mittleres EMA (Länge 25) und langsames EMA (Länge 50) definieren die Richtungsneigung.
   - Parabolic SAR mit Schritt 0,02 und Maximum 0,2 bestätigt, dass der Preis bereits auf die neue Seite des Trends durchgebrochen ist.
2. **Langer Eintrag**
   - EMA(10) ist größer als EMA(25) und EMA(50).
   - EMA(10) lag bei der vorherigen geschlossenen Kerze unter EMA(50) (Cross-Up-Bestätigung).
   - Der Wert von Parabolic SAR liegt unter dem Schlusskurs der Kerze, was bedeutet, dass die Punkte in den bullischen Modus gewechselt sind.
   - Es gibt keine offene Position und die Strategie darf gehandelt werden (online + Berechtigungen).
3. **Kurzer Eintrag**
   - EMA(10) ist niedriger als sowohl EMA(25) als auch EMA(50).
   - EMA(10) lag über EMA(50) bei der vorherigen geschlossenen Kerze (Cross-Down-Bestätigung).
   - Parabolic SAR liegt über dem Kerzenschluss.
4. **Exit-Management**
   - Harter Stop-Loss und Take-Profit werden unmittelbar nach dem Einstieg mit asymmetrischen Einstellungen für Long- und Short-Trades angewendet.
   - Ein Trailing Stop wird aktiviert, sobald sich der Preis weit genug zugunsten der Position bewegt. Der Stopp wird auf `current price -/+ trailing` gezogen
Entfernung je nach Richtung.
   - Ein vorzeitiger Ausstieg erfolgt, wenn EMA(10) die Richtung umkehrt (bei Long-Positionen unter seinen vorherigen Wert fällt oder bei Short-Positionen darüber steigt) und der
Der offene Gewinn überschreitet einen Mindestauslöseabstand.

## Standardparameterwerte

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | Zeitrahmen von 15 Minuten | Von der Strategie verarbeitete Kerzenserie. |
| `FastEmaLength` | 10 | Periode des schnellen EMA. |
| `MediumEmaLength` | 25 | Zeitraum des Mediums EMA. |
| `SlowEmaLength` | 50 | Zeitraum der langsamen EMA. |
| `SarStep` | 0,02 | Anfängliche Beschleunigung für Parabolic SAR. |
| `SarMax` | 0,2 | Maximale Beschleunigung für Parabolic SAR. |
| `Volume` | 0,1 | Handelsvolumen in Lots/Kontrakten. |
| `LongTakeProfitPoints` | 50 | Take-Profit-Distanz für Long-Trades, gemessen in Preispunkten. |
| `ShortTakeProfitPoints` | 50 | Take-Profit-Distanz für Short-Trades, gemessen in Preispunkten. |
| `LongStopLossPoints` | 30 | Stop-Loss-Distanz für Long-Trades, gemessen in Preispunkten. |
| `ShortStopLossPoints` | 30 | Stop-Loss-Distanz für Short-Trades, gemessen in Preispunkten. |
| `LongTrailingStopPoints` | 10 | Trailing-Stop-Trigger-Distanz für Long-Trades. |
| `ShortTrailingStopPoints` | 10 | Trailing-Stop-Trigger-Distanz für Short-Trades. |
| `LongProfitTriggerPoints` | 10 | Mindesteröffnungsgewinn (Punkte), der erforderlich ist, bevor ein Long-Trade bei einer EMA-Umkehr geschlossen werden kann. |
| `ShortProfitTriggerPoints` | 5 | Mindesteröffnungsgewinn (Punkte), der erforderlich ist, bevor ein Short-Trade bei einer EMA-Umkehr geschlossen werden kann. |

## Implementierungshinweise

- Die Strategie verwendet Kerzenabonnements und Indikatorbindung auf der oberen Ebene API und behält dabei die gesamte Risikokontrolle innerhalb der
Strategieklasse. Es ist kein Low-Level-Orderbuchzugriff erforderlich.
- Alle Handelsmanagemententfernungen werden mithilfe des Instruments `PriceStep` von Punkten in tatsächliche Preisversätze umgerechnet. Wenn `PriceStep`
Ist der Wert nicht verfügbar, wird der rohe Punktwert verwendet, sodass der Algorithmus weiterhin auf synthetischen Instrumenten funktioniert.
- Schutzstopps (`SetStopLoss`, `SetTakeProfit`) werden anhand der resultierenden Position gesetzt, nachdem die Marktorder gesendet wurde, um darin zu bleiben
Synchronisierung mit möglichen Teilfüllungen.
- Der interne Status verfolgt den letzten Einstiegspreis pro Richtung, sodass nachfolgende und EMA-basierte Ausstiege den realisierten Preis auswerten können
Fortschritt präzise.
- Da die gesamte Logik auf fertigen Kerzen ausgeführt wird, besteht kein Neuzeichnungsrisiko und die Signale spiegeln das ursprüngliche MetaTrader-Verhalten wider
Alles wurde anhand von `start()` Schlusskursen berechnet.

## Empfohlene Verwendung

- Die Methode eignet sich für liquide FX-Paare auf Intraday-Charts (15-Minuten-Standard). Durch Anpassen können höhere Zeitrahmen verwendet werden
EMA Zeiträume und Handelsmanagemententfernungen entsprechend.
- Passen Sie für Vermögenswerte mit unterschiedlichen Tick-Größen oder Volatilitätsniveaus die punktbasierten Parameter an (`StopLoss`, `TakeProfit`,
`TrailingStop`, `ProfitTrigger`), sodass die Entfernungen dem Instrumentenprofil entsprechen.
- Kombinieren Sie es mit Spread- oder Session-Filtern, wenn der Veranstaltungsort zu bestimmten Zeiten große Spreads aufweist; Die Strategie erwartet vernünftig
Ausführung, um die kurzfristigen Impulsausbrüche zu realisieren.
