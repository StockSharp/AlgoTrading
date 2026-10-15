# Rabbit M2 Regime Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Rabbit M2 ist ein diskretionärer Expertenberater, der ursprünglich von Peter Byrom für MetaTrader 4 codiert wurde. Der Algorithmus wechselt zwischen
bullische und bärische Regime, die durch stündliche exponentielle gleitende Durchschnitte bestimmt werden. Innerhalb des aktiven Regimes wartet es auf Williams %R
Momentumschwankungen, die vom Commodity Channel Index bestätigt werden, bevor Marktaufträge übermittelt werden. Die Schutzlogik spiegelt das wider
Quelle EA durch Anhängen von Stop-Loss- und Take-Profit-Levels mit fester Distanz und durch Schließen von Positionen, wenn der Preis gegen die festgelegten Werte verstößt
gegenüber der Kanalgrenze Donchian. Ein einfaches Geldverwaltungsmodul erhöht die Basislosgröße nach jedem hochprofitablen Spiel
trade and doubles the profit target required for the next scale-up.

## Marktdaten und Indikatoren
- **Primärer Zeitrahmen** (Standard: 1-Minuten-Kerzen) liefert Eingaben für Williams %R, CCI und den Kanal Donchian.
- **Stündlicher Zeitrahmen** berechnet das schnelle (40) und langsame (80) EMA-Paar, das die Handelsrichtung steuert.
- **Williams %R (50)** fungiert als Impulsauslöser, wenn es die -20/-80-Bänder überschreitet.
- **Commodity Channel Index (14)** filtert Trades nach überkauften oder überverkauften Werten.
- **Donchian Kanal (100)** bietet Breakout-Ausgänge basierend auf dem vorherigen Hoch/Tief-Bereich.
- **Statischer Stop-Loss und Take-Profit** werden mithilfe des Sicherheitsticks aus Punktabständen (Standard 50) in Preis-Offsets umgewandelt
Größe, angepasst für 3 und 5 Dezimalinstrumente.

## Handelslogik
### Regimemanagement
1. Wenn der 40-Perioden-EMA im stündlichen Feed unter den 80-Perioden-EMA fällt, werden alle Long-Positionen geschlossen und nur Short-Setups vorgenommen
sind erlaubt.
2. Wenn der 40-Perioden-EMA über den 80-Perioden-EMA steigt, werden Short-Positionen liquidiert und die Strategie lässt nur Long-Trades zu.

### Einreisebestimmungen
- **Kurze Einträge** erfordern:
  - Williams %R bewegt sich von der Zone -20..0 in den überverkauften Bereich (< -20).
  - CCI to exceed the configurable sell threshold (default 101).
  - Netto-Short-Engagement unterhalb des `MaxTrades`-Limits (jeder Trade fügt eine Basisvolumeneinheit hinzu).
- **Lange Einträge** erfordern:
  - Williams %R, um aus der Zone -100..-80 herauszuklettern und einen Wert über -80 auszugeben.
  - CCI muss unter den Kaufschwellenwert fallen (Standard 99).
  - Netto-Long-Engagement unter der Obergrenze von `MaxTrades`.

Jede Bestellung wird mit dem aktuellen Basisvolumen versendet. Der StockSharp-Port verwendet Netting-Positionen, sodass sich wiederholende Signale einfach zunehmen
das Netto-Exposure bis zum Erreichen des konfigurierten Limits.

### Ausgangsregeln
1. Stop-Loss- und Take-Profit-Level werden bei jeder fertigen Kerze überwacht. Sobald der Preis ein Niveau überschreitet, ist die Position
mit einer Marktorder geschlossen.
2. Independently of stop/target levels, a long position is closed when the close falls below the previous Donchian lower band;
Ein Short wird geschlossen, wenn der Schlusskurs über das vorherige obere Band von Donchian steigt.
3. Ein durch den stündlichen EMA-Crossover verursachter Regimewechsel löst sofort Positionen auf, die der neuen Richtung entgegenstehen.

### Geldmanagement
- Die Basisauftragsgröße beginnt bei `InitialVolume` (Standard 0,01) und berücksichtigt den Sicherheitsvolumenschritt, das Minimum und das Maximum.
- Nach jedem realisierten Gewinn größer als `BigWinTarget` (Standard 15 Währungseinheiten) erhöht sich das Basisvolumen um
`VolumeIncrement` (Standard 0,01) und der Gewinnschwellenwert verdoppelt sich entsprechend dem Kaskadenverhalten der MetaTrader-Version.
- Wenn die Strategie flach ist, werden alle ausstehenden Stop/Take-Platzhalter zurückgesetzt, um veraltete Werte zu vermeiden.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CciSellLevel` | 101 | Mindestwert CCI, der ein kurzes Signal bestätigt. |
| `CciBuyLevel` | 99 | Maximaler CCI-Wert, der ein langes Signal bestätigt. |
| `CciPeriod` | 14 | Lookback-Länge des Commodity Channel Index. |
| `DonchianPeriod` | 100 | Donchian channel period used for breakout exits. |
| `MaxTrades` | 1 | Maximale Anzahl an Basisvolumeneinheiten, die in der Nettoposition zulässig sind. |
| `BigWinTarget` | 15 | Realisierter Gewinn, der vor der Erhöhung des Basisvolumens erforderlich ist. |
| `VolumeIncrement` | 0,01 | Zusätzliches Volumen nach einem Qualifikationssieg hinzugefügt. |
| `WprPeriod` | 50 | Williams %R Berechnungszeitraum. |
| `FastEmaPeriod` | 40 | Schneller Zeitraum von EMA im stündlichen Trend-Feed. |
| `SlowEmaPeriod` | 80 | Langsamer Zeitraum von EMA im stündlichen Trend-Feed. |
| `TakeProfitPoints` | 50 | Nehmen Sie die Gewinndistanz in Preispunkten. |
| `StopLossPoints` | 50 | Stop-Loss-Distanz in Preispunkten. |
| `InitialVolume` | 0,01 | Ausgangsbasis-Ordergröße. |
| `CandleType` | 1-Minuten-Kerzen | Primärer Zeitrahmen für Momentum- und Exit-Berechnungen. |

## Hinweise zur Implementierung
- Stop-Loss- und Take-Profit-Level werden innerhalb der Strategie bewertet und nicht als separate Aufträge übermittelt, um sie zu reproduzieren
Verhalten der `OrderSend`-Parameter von MetaTrader.
- Volumenanpassungen basieren auf dem von StockSharp gemeldeten realisierten PnL. Stellen Sie sicher, dass die Strategie Handelsbestätigungen von erhält
Broker-Verbindung, damit die Skalierungslogik aktiviert wird.
- Die Hilfsmethode `CalculatePriceOffset` vergrößert die Punktgröße für Forex-Symbole mit 3 und 5 Dezimalstellen und reproduziert so die Methode `Point`.
Konstante von der ursprünglichen Plattform.
