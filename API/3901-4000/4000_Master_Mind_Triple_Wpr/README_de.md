# Master Mind Triple WPR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 4 Expert Advisors `MasterMind3CE` (Ordner `MQL/8458`).
- Verwendet vier Williams %R-Indikatoren mit den Perioden 26, 27, 29 und 30, um extreme überkaufte/überverkaufte Bedingungen zu erkennen.
- Konzipiert für Mean-Reversion-Einstiege: Kaufen nach einem tiefen Ausverkauf, Verkaufen nach einer überzogenen Rallye.
- Beinhaltet konfigurierbare Stop-Loss-, Take-Profit- und optionale Trailing-Stop-Logik, ausgedrückt in Instrumentenpreisschritten.
- Funktioniert in jedem Zeitrahmen, der vom verbundenen StockSharp-Terminal unterstützt wird; Der Standardwert sind 15-Minuten-Kerzen.

## Handelslogik
### Indikatoren
- `WilliamsR(26)` – extrem schneller Oszillator.
- `WilliamsR(27)` – schneller Oszillator zur Bestätigung.
- `WilliamsR(29)` – mittlerer Oszillator, der das Signal glättet.
- `WilliamsR(30)` – langsamer Oszillator, der extreme Werte über mehrere Lookbacks hinweg erfordert.

Alle vier Oszillatoren müssen gebildet werden. Das Abonnement verarbeitet nur fertige Kerzen, die dem `TradeAtCloseBar = true`-Verhalten des ursprünglichen Experten entsprechen.

### Teilnahmebedingungen
- **Langer Eintrag**: Alle vier Williams %R-Werte liegen unter oder gleich `OversoldLevel` (Standardwert `-99.99`). Die Strategie zielt auf eine Long-Position von `TradeVolume` ab. Wenn eine Short-Position offen ist, wird sie geschlossen und in einer einzigen Marktorder, die so dimensioniert ist, dass sie das Zielengagement erreicht, in eine Long-Position umgewandelt.
- **Kurzer Eintrag**: Alle vier Williams %R-Werte liegen über oder gleich `OverboughtLevel` (Standardwert `-0.01`). Die Strategie zielt auf eine Short-Position von `TradeVolume` ab und schließt zunächst alle bestehenden Long-Positionen.

### Ausstiegsbedingungen
- **Signalbasierter Ausstieg**: Wenn eine Long-Position offen ist und eine Short-Einstiegsbedingung auftritt, schließt/dreht die Strategie die Position (und umgekehrt).
- **Schützender Stop-Loss**: Optionaler Preisschrittabstand vom durchschnittlichen Einstiegspreis. Ein Treffer auf das Hoch/Tief der Kerze löst einen Marktausstieg aus.
- **Take-Profit**: Optionales Preissprungziel vom durchschnittlichen Einstiegspreis. Sobald die Kerze erreicht ist, wird die Position geschlossen.
- **Trailing-Stop**: Optionale Trailing-Logik, die startet, sobald sich der Preis um `TrailingStopSteps + TrailingStepSteps` zu seinen Gunsten bewegt. Der Stop wird dann `TrailingStopSteps` vom letzten Schlusskurs entfernt gehalten und erhöht sich erst, wenn er um mindestens `TrailingStepSteps` verbessert wird.

## Risikomanagement
Preisabstände werden in den *Preisschritten* des Instruments angegeben. Bei `PriceStep = 0.0001` und `StopLossSteps = 2000` wird der Stopp beispielsweise 0,2000 vom Eingang entfernt platziert. Die Strategie berechnet den durchschnittlichen Einstiegspreis neu, wenn sie in die gleiche Richtung skaliert, um das Risikoniveau konstant zu halten. Trailing Stops sind deaktiviert, es sei denn, sowohl `TrailingStopSteps` als auch `TrailingStepSteps` sind positiv.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Angestrebte Nettopositionsgröße (Lots/Kontrakte). | `1` |
| `OversoldLevel` | Williams %R Schwellenwert, der überverkaufte Bedingungen bestätigt. | `-99.99` |
| `OverboughtLevel` | Williams %R-Schwellenwert, der überkaufte Bedingungen bestätigt. | `-0.01` |
| `StopLossSteps` | Stop-Loss-Distanz in `PriceStep` Einheiten. Stellen Sie `0` auf Deaktivieren ein. | `2000` |
| `TakeProfitSteps` | Take-Profit-Distanz in `PriceStep` Einheiten. Stellen Sie `0` auf Deaktivieren ein. | `0` |
| `TrailingStopSteps` | Trailing-Stop-Distanz in `PriceStep` Einheiten. Erfordert `TrailingStepSteps > 0`. | `0` |
| `TrailingStepSteps` | Minimale Verbesserung, bevor der Trailing Stop verschoben wird (in `PriceStep` Einheiten). | `1` |
| `CandleType` | Kerzendatentyp/Zeitrahmen, der von der Strategie verarbeitet wird. | `TimeFrame(15m)` |

## Konvertierungshinweise
- Auf Warnungen, akustische Benachrichtigungen, Protokollierung in Dateien und E-Mail-Funktionen des MQL-Experten wird bewusst verzichtet; Stattdessen können StockSharp-Protokolle verwendet werden.
- Der ursprüngliche Berater erlaubte den Handel vor Börsenschluss. Der Port behält die standardmäßige „Trade on Close“-Logik bei, indem er nur fertige Kerzen verarbeitet.
- Magische Zahlen, wiederholte Bestellwiederholungen und manuelle Objektzeichnung waren spezifisch für MetaTrader und haben keine direkten StockSharp-Entsprechungen, daher wurden sie entfernt.
- Das Risikomanagement wird innerhalb der Strategie konsolidiert, anstatt externe Auftragsänderungsschleifen zu verwenden. Stop/Take-Checks werden für jede Kerze ausgewertet.

## Nutzung
1. Konfigurieren Sie das gewünschte Instrument und den gewünschten Zeitrahmen, passend zum Diagramm, dem der Experte ursprünglich zugeordnet war.
2. Passen Sie Schwellenwerte oder Risikoparameter an, wenn das Instrument ein anderes Volatilitätsprofil aufweist.
3. Starten Sie die Strategie; Es abonniert die angegebene Kerzenserie, überwacht Williams %R-Extreme und verwaltet die Positionen entsprechend.
