# RPoint 250 Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **RPoint 250 Reversal Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `e_RPoint_250`. Der ursprüngliche Roboter
basiert auf einem benutzerdefinierten Indikator namens *RPoint*, der das jüngste Swing-Hoch und Swing-Tief hervorhebt. Denn dieser Indikator ist
nicht verfügbar auf StockSharp, die Konvertierung reproduziert das gleiche Verhalten mit den integrierten Indikatoren `Highest` und `Lowest`.
Immer wenn ein neues Extrem das zuvor erkannte ersetzt, kehrt die Strategie die Position sofort um und stellt dieselbe wieder her
Stop-Loss-, Take-Profit- und Trailing-Logik, definiert in der MQL-Version.

## Handelsablauf

1. Abonnieren Sie die durch `CandleType` angegebene Kerzenserie (Standard: 5-Minuten-Kerzen).
2. Verfolgen Sie das gleitende Maximum und Minimum über die letzten `ReversePoint` Balken. Diese Werte stellen die emulierten RPoint-Ebenen dar.
3. Wenn der Preis ein neues Höchsthoch erreicht, schließen Sie alle Long-Positionen und eröffnen Sie eine Short-Position mit einem Volumen von `OrderVolume`.
4. Wenn der Preis ein neues Tief erreicht, schließen Sie alle Short-Positionen und eröffnen Sie eine Long-Position mit einem Volumen von `OrderVolume`.
5. Erteilen Sie Schutzanordnungen mit `StartProtection`. Die Stop-Loss- und Take-Profit-Abstände werden in Preispunkten ausgedrückt
die Parameter `StopLossPoints` und `TakeProfitPoints`.
6. Optional können Sie die Gewinne um `TrailingStopPoints` verzögern. Die nachlaufende Engine misst, wie weit sich der Preis zugunsten des bewegt hat
Position und schließt sie, wenn der Preis um die konfigurierte Anzahl von Punkten zurückgeht.
7. Merken Sie sich die Kerzenzeit des letzten erfolgreichen Einstiegs, um zu vermeiden, dass innerhalb desselben Balkens mehrere Trades eröffnet werden, die mit dem übereinstimmen
`TimeN`-Schutz vor dem MQL-Skript.

Die Strategie behält immer höchstens eine offene Position bei. Es schließt bestehende Geschäfte, bevor es in die entgegengesetzte Richtung einsteigt
skaliert nie ein.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Mit jeder Marktorder gesendetes Volumen. Spiegelt die Eingabe `Lots` in der Version MetaTrader wider. |
| `TakeProfitPoints` | `decimal` | `15` | Abstand zur Take-Profit-Order, gemessen in Preispunkten. Auf `0` setzen, um Gewinnziele zu deaktivieren. |
| `StopLossPoints` | `decimal` | `999` | Abstand zum Schutzstopp, ausgedrückt in Preispunkten. Auf `0` einstellen, um ohne festen Stop zu handeln. |
| `TrailingStopPoints` | `decimal` | `0` | Optionaler Nachlaufabstand in Preispunkten. Bei Null ist die nachgestellte Logik deaktiviert. |
| `ReversePoint` | `int` | `250` | Anzahl der Kerzen, die bei der Suche nach dem letzten Swing-Hoch und Swing-Tief berücksichtigt werden. Größere Werte glätten das Rauschen. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Von der Strategie analysierte Kerzenaggregation. Ändern Sie es so, dass es dem in MetaTrader verwendeten Diagrammzeitrahmen entspricht. |

## Hinweise zur Implementierung

- `Highest` und `Lowest` sind über das übergeordnete `Bind` API an das Kerzenabonnement gebunden, daher gibt es keine manuellen Indikatorwarteschlangen
erforderlich.
- `StartProtection` reproduziert die ursprünglichen Stop-Loss- und Take-Profit-Abstände in absoluten Preiseinheiten. StockSharp kümmert sich um die
Auftragserteilung, sobald eine neue Position erscheint.
- Trailing Stops werden durch die Überwachung jeder abgeschlossenen Kerze implementiert. Wenn der Preis um die konfigurierte Punktezahl abweicht
Um den besten nach Eintritt erzielten Preis zu erreichen, wird die Position mit einer Marktorder geschlossen.
- Die Klasse speichert die zuletzt ausgeführten Umkehrstufen (`_executedHighLevel` und `_executedLowLevel`), um Duplikate zu vermeiden
Einträge. Dies entspricht den Variablen `Reverse_High` / `Reverse_Low` im Code MQL.
- Das Feld `_lastSignalTime` spiegelt die Variable `TimeN` wider und blockiert mehrere Bestellungen innerhalb derselben Kerze, wodurch verhindert wird
versehentliche Doppeleinreichungen auf illiquiden Märkten.

## Nutzungsrichtlinien

1. Hängen Sie die Strategie an ein Portfolio an, das das ausgewählte Instrument und den Kerzentyp unterstützt.
2. Passen Sie `OrderVolume` an die Vertragsgröße und die Risikomanagementregeln Ihres Brokers an.
3. Passen Sie `ReversePoint` an die Volatilität des gehandelten Vermögenswerts an. Höhere Werte führen zu weniger, aber aussagekräftigeren Umkehrungen.
4. Stellen Sie sicher, dass `StopLossPoints`, `TakeProfitPoints` und `TrailingStopPoints` mit dem `PriceStep` des Wertpapiers kompatibel sind.
5. Führen Sie einen Backtest in StockSharp Designer oder Backtester durch, um das Verhalten zu bestätigen, bevor Sie mit Live-Kapital handeln.
6. Überwachen Sie die Protokollausgabe: Informationsmeldungen heben Positionsänderungen hervor und können bei der Validierung der Konvertierung helfen.

Da der RPoint-Indikator durch integrierte Komponenten angenähert wird, bestehen geringfügige Unterschiede zur MetaTrader-Ausführung
möglich bei historischen Daten mit Lücken oder unterschiedlichen Rundungsregeln. Validieren Sie die Ergebnisse immer mit Ihren eigenen Marktdaten-Feeds
bevor man sich auf die Strategie in der Produktion verlässt.
