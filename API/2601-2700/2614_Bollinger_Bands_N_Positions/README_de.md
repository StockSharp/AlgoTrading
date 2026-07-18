# Bollinger Bands N Positionen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors **Bollinger Bands N positions**. Sie überwacht Schlusskurse relativ zu einem Bollinger-Bänder-Envelope und tritt in eine Position ein, wann immer der Markt eine Kerze außerhalb des Kanals schließt. Das Positionsmanagement repliziert den ursprünglichen Experten, indem es eine Obergrenze für das Gesamtengagement durchsetzt, feste Stop-Loss- und Take-Profit-Abstände platziert und einen Trailing-Stop aktiviert, sobald der Trade ausreichend im Gewinn ist.

## Handelslogik

1. Den konfigurierten Kerzentyp abonnieren und Bollinger Bänder mit dem ausgewählten Zeitraum und der Breite berechnen.
2. Bei jeder abgeschlossenen Kerze prüft die Strategie zunächst, ob eine bestehende Position geschlossen werden muss:
   - Long-Positionen steigen aus, wenn der Preis den festen Stop-Loss, den festen Take-Profit oder den Trailing-Stop-Level berührt.
   - Short-Positionen wenden die symmetrische Logik an.
3. Wenn Trading erlaubt ist und auf der aktuellen Kerze kein Ausstieg erfolgte, werden Einstiegssignale bewertet:
   - Wenn der Schlusskurs über dem oberen Band liegt, schließt die Strategie Short-Engagements und öffnet, wenn innerhalb der Positionsobergrenze, eine neue Long-Position mit dem angeforderten Volumen.
   - Wenn der Schlusskurs unter dem unteren Band liegt, schließt sie Long-Engagements und öffnet auf die gleiche Weise eine Short-Position.
4. Trailing-Stops bewegen sich in Schritten, die durch den Trailing-Schritt-Parameter definiert sind, sobald der Trade um die Trailing-Distanz plus den Trailing-Schritt voraus liegt. Das Trailing-Level bleibt um die Trailing-Distanz hinter dem Preis und rückt nur vor, wenn der Gewinn um mindestens einen Trailing-Schritt zunimmt.

## Positionsmanagement

- **Max Positions** definiert das maximale Nettoengagement, gemessen als `MaxPositions × Volume`. Da StockSharp im Netting-Modus arbeitet, kann die Strategie nur eine Nettoposition gleichzeitig halten. Der Parameter wirkt daher als Sicherheitsobergrenze, die verhindert, dass die Strategie erneut einsteigt, wenn die aktuelle absolute Position das konfigurierte Limit bereits erreicht.
- Stop-Loss- und Take-Profit-Abstände werden in Pips angegeben. Die Strategie konvertiert sie mithilfe des `PriceStep` der Sicherheit in Preise. Wenn das Instrument Bruchteil-Pip-Preise verwendet, müssen die Werte möglicherweise entsprechend angepasst werden.
- Trailing-Stops erfordern, dass sowohl der Abstand als auch der Schritt positiv sind. Wenn der Trailing-Stop-Abstand auf null gesetzt wird, wird das Trailing-Modul deaktiviert.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Volume` | Ordergröße in Lots für jeden Einstieg. | `0.1` |
| `MaxPositions` | Nettopositionsobergrenze in Vielfachen von `Volume`. | `9` |
| `BollingerPeriod` | Rückblicklänge für den Bollinger-Gleitenden-Durchschnitt. | `20` |
| `BollingerWidth` | Standardabweichungsmultiplikator für die Bollinger Bänder. | `2` |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | `50` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Auf `0` setzen zum Deaktivieren. | `5` |
| `TrailingStepPips` | Mindestgewinninkrement, das benötigt wird, bevor der Trailing-Stop vorrückt. | `5` |
| `CandleType` | Zeitrahmen oder benutzerdefinierter Kerzentyp für die Bollinger-Bänder. | `1-Minuten-Zeitrahmen` |

## Unterschiede zum MQL5-Experten

- Der ursprüngliche Experte arbeitet im Hedging-Modus von MetaTrader und kann gleichzeitig Long- und Short-Positionen halten. StockSharp-Strategien sind genettete, daher schließt dieser Port entgegengesetzte Engagements, bevor er in einen neuen Trade eintritt. Der Parameter `MaxPositions` begrenzt daher die absolute Größe der Nettoposition statt der Anzahl unabhängiger Tickets.
- Orderstops werden innerhalb der Strategie simuliert, anstatt als angehängte Stop-Orders gesendet zu werden. Dies stimmt mit der Trailing-Logik der MQL-Implementierung überein, bedeutet aber, dass Ausstiege auf der nächsten abgeschlossenen Kerze erfolgen.
- Die Trailing-Konfiguration wird beim Start validiert. Das Aktivieren eines Trailing-Stops mit einem null Trailing-Schritt wirft eine Ausnahme, um die ursprüngliche Initialisierungsprüfung nachzuahmen.

## Verwendungshinweise

1. `Volume`, `MaxPositions` und die Risikoparameter so konfigurieren, dass sie zur Kontraktgröße und dem Tick-Wert des Instruments passen.
2. Sicherstellen, dass die Sicherheit einen gültigen `PriceStep` exponiert. Wenn der Schritt null oder fehlt, fällt die Strategie auf `1` zurück, was möglicherweise nicht für alle Märkte passt.
3. Die Strategie erst starten, nachdem die Indikator-Aufwärmperiode (Bollinger-Periode) abgeschlossen ist, um nicht auf unvollständigen Daten zu agieren.
4. Protokolle auf Trailing-Schritt-Validierungsfehler überwachen, wenn die Risikoeinstellungen angepasst werden.
