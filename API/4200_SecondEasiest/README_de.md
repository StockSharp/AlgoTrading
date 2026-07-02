# Zweiteinfachste Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die zweiteinfachste Strategie ist der StockSharp-Port des MetaTrader-Experten *Second_Easiest.mq4*. Der ursprüngliche Roboter scannt die
Tageskerze der aktuellen Handelssitzung und eröffnet eine einzelne Intraday-Position, sobald der Preis zeigt, dass er von der Kerze abweicht
Tag ist geöffnet. Wenn der Markt schließt, liquidiert der Experte alle Engagements und bereitet sich so auf die nächste Sitzung vor. Der StockSharp
Die Version behält dieses Intraday-Breakout-Verhalten bei und nutzt gleichzeitig die High-Level-API des Frameworks für Kerzen
Abonnements, Auftragsverwaltung und Positionsverfolgung.

Im Gegensatz zu Momentum-Strategien, die mehrere Indikatoren erfordern, benötigt Second Easiest nur die laufende Eröffnung, das Hoch und das Tief der Indikatoren
aktueller Tag. Dadurch ist es sehr leichtgewichtig und reagiert dennoch auf die ersten Anzeichen einer Richtungsüberzeugung. Der Code bleibt erhalten
eine Position nach der anderen und kehrt niemals sofort um; Der neue Handel kann erst eröffnet werden, nachdem der vorherige geschlossen wurde.

## Handelslogik
1. Abonnieren Sie die durch `CandleType` definierte Intraday-Kerzenserie. Der Standardwert ist ein Zeitrahmen von einer Minute, was einen frühen Zeitpunkt darstellt
Sicht auf Tagesextreme und bleibt gleichzeitig mit der Tageslogik des Originals EA kompatibel.
2. Aktualisieren Sie für jede fertige Kerze den In-Memory-Datensatz der Eröffnungs-, Höchst- und Tiefstkurse der Sitzung. Die erste Kerze wurde verarbeitet
an einem neuen Handelstag alle drei Werte definiert; Nachfolgende Kerzen erweitern nur das Hoch oder Tief, wenn ein neues Extrem erreicht wird.
3. Ignorieren Sie neue Setups, sobald die Uhr `EntryCutoffHour` erreicht. Der MetaTrader-Code stoppt die Eröffnung von Trades um 16:00 Uhr Serverzeit und
Der Hafen folgt der gleichen Regel.
4. Eine Long-Position ist nur zulässig, wenn der aktuelle Schlusskurs über dem Tageseröffnungskurs liegt **und** der Abstand zwischen dem Eröffnungskurs und dem Tageseröffnungskurs liegt
Tagestiefstwert übersteigt `RangePointsThreshold`. Dies reproduziert die Bedingungen „Bid > open“ und „open – low > 15 Punkte“ aus MQL.
5. Eine Short-Position ist nur zulässig, wenn der aktuelle Schlusskurs unter dem täglichen Eröffnungskurs **und** dem Abstand zwischen dem Tageshöchststand und dem Tageshöchstkurs liegt
die Öffnung überschreitet den gleichen Schwellenwert.
6. Wenn ein Einstiegssignal erscheint und keine Position offen ist, senden Sie eine Marktorder mit `TradeVolume` Lots. Die Hilfsmethoden von
Die Basisklasse `Strategy` kümmert sich um die Auswahl der richtigen Seite.
7. Sobald der Markt `MarketCloseHour` erreicht, glätten Sie das bestehende Risiko, indem Sie `ClosePosition()` aufrufen. Es werden keine neuen Geschäfte platziert
nach dieser Unterbrechung bis zum Beginn der nächsten Sitzung.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 1 Minute | Primäre Intraday-Kerzen steuern die Ein- und Ausstiegslogik. |
| `TradeVolume` | `decimal` | `1` | Für jede Marktorder verwendete Losgröße. |
| `EntryCutoffHour` | `int` | `16` | Stunde (0-23), nach der die Strategie die Eröffnung neuer Positionen verweigert. |
| `MarketCloseHour` | `int` | `20` | Stunde (0-23), wenn eine offene Position zwangsweise geschlossen wird. |
| `RangePointsThreshold` | `decimal` | `15` | Mindestabstand, ausgedrückt in Broker-Punkten, zwischen der Tageseröffnung und dem nächsten Extrem. |

## Unterschiede zur MetaTrader-Version
- Die StockSharp-Version verfolgt Positionen saldiert. Das Verhalten ist identisch mit der ursprünglichen Single-Order-Logik
weil jeweils nur ein Trade offen sein kann und die Position abgeflacht wird, bevor neue Einträge bewertet werden.
- MetaTrader ruft die laufenden Eröffnungs-, Höchst- und Tiefststände bis `iOpen/iHigh/iLow` Anrufe im täglichen Zeitrahmen ab. Der Hafen wird neu aufgebaut
die gleichen Informationen von Intraday-Kerzen, wodurch verbotene Indikatoraufrufe vermieden werden und sichergestellt wird, dass die Daten auch dann verfügbar bleiben, wenn
Der Brokerage-Feed stellt keine täglichen Balken bereit.
- Der Auftragsabschluss erfolgt über `ClosePosition()`, anstatt die Ticket-IDs in einer Schleife zu durchlaufen. Das Endergebnis ist das gleiche:
Die offene Belichtung wird entfernt, sobald die konfigurierte Schließzeit erreicht ist.
- Wenn der `PriceStep` des Wertpapiers nicht angegeben ist, behandelt die Konvertierung den `RangePointsThreshold` als absolute Preisdistanz.
Dieser Sicherheitsfallback hält das System auf Instrumenten betriebsbereit, die Preise ohne Schrittmetadaten melden.

## Nutzungshinweise
- `Volume` ist in `OnStarted` auf `TradeVolume` gesetzt, daher wirkt sich eine Änderung des Parameters sofort auf nachfolgende Bestellungen ohne aus
den Rest des Codes ändern.
- Stellen Sie bei der Auswahl eines anderen `CandleType` sicher, dass dieser immer noch genügend Granularität bietet, um die Intraday-Eröffnungs-/Höchst-/Tiefstwerte zu verfolgen
genau. Fünf-Minuten-Kerzen funktionieren beispielsweise gut, stündliche Balken können jedoch die Erkennung von Tagesextremen verzögern.
- Erhöhen Sie `RangePointsThreshold`, um Sitzungen mit geringer Volatilität herauszufiltern. Wenn Sie ihn verringern, kann die Strategie auch dann ausgelöst werden, wenn
Der frühe Bereich ist klein.
- Da der Algorithmus alle Positionen am Ende des Tages schließt, ist keine Übernachtmarge erforderlich. Makler, die durchsetzen
Bei Sitzungsunterbrechungen werden auch die internen Range-Zähler automatisch zurückgesetzt, wenn der Handel wieder aufgenommen wird.
