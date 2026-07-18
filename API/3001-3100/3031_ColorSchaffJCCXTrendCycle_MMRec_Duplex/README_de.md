# Color Schaff JCCX Trend Cycle MMRec Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Reproduziert den bidirektionalen Expert Advisor "ColorSchaffJCCXTrendCycle_MMRec_Duplex" aus MetaTrader innerhalb von StockSharp.
- Verwendet zwei unabhängige Schaff Trend Cycle-Stacks auf Basis von Jurik-Moving-Averages zur Erkennung bullischer und bärischer Umkehrungen.
- Implementiert ein vereinfachtes MMRec-Modul (Money-Management-Empfehler), das die Positionsgröße nach wiederholten Verlusten reduziert.
- Wendet separate Parametersätze für Long- und Short-Trades an und ermöglicht asymmetrische Konfigurationen über Zeitrahmen und Preisquellen hinweg.

## Indikator-Pipeline
1. **JCCX-Approximation** – jeder Preis wird durch einen Jurik-Moving-Average verarbeitet, um eine detrendete Reihe zu erhalten. Die detrendete Reihe und ihr Absolutwert werden erneut mit Jurik-Averages geglättet, um den ursprünglichen JCCX-Oszillator zu approximieren.
2. **MACD-Schicht** – die Differenz zwischen schnellen und langsamen JCCX-Ausgaben liefert die Momentum-Basis.
3. **Doppelte stochastische Transformation** – rollende Min/Max-Fenster normalisieren das MACD-Momentum und erzeugen den endgültigen Schaff Trend Cycle (STC)-Wert im Bereich -100..+100.
4. **Phasenkontrolle** – der `Phase`-Parameter moduliert einen internen Glättungsfaktor (0.05–0.95), der nach jedem stochastischen Schritt angewendet wird und das Jurik-"Phasen"-Verhalten emuliert.

Der Indikator-Stack wird zweimal ausgeführt: einmal für den Long-Block und einmal für den Short-Block. Jeder Block kann unterschiedliche Kerzentypen und Preiseingaben verwenden.

## Handelslogik
### Long-Block
- **Einstieg**: wenn der Long-STC die Null nach oben kreuzt (aktueller Wert > 0 und der vorherige verzögerte Wert ≤ 0). Bestehende Short-Positionen werden zuerst geschlossen.
- **Ausstieg**: wenn der Long-STC unter Null fällt und Long-Ausstiege aktiviert sind.
- **Stops**: optionale Stop-Loss- und Take-Profit-Abstände (ausgedrückt in Preisschritten) werden bei jeder abgeschlossenen Kerze anhand von Kerzen-Hochs/-Tiefs bewertet.

### Short-Block
- **Einstieg**: wenn der Short-STC unter Null kreuzt (aktueller Wert < 0 und der verzögerte Wert ≥ 0). Jede bestehende Long-Position wird vor dem Öffnen eines Shorts glattgestellt.
- **Ausstieg**: wenn der Short-STC über Null steigt und Short-Ausstiege aktiviert sind.
- **Stops**: symmetrische Stop-Loss- und Take-Profit-Prüfungen für Short-Trades.

Der `SignalBar`-Parameter definiert, wie viele vollständig geschlossene Kerzen übersprungen werden, bevor Signale ausgewertet werden. Ein Wert von `1` reproduziert das MetaTrader-Verhalten, die vorherige abgeschlossene Kerze zu verwenden.

## Money Management (MMRec)
- Zwei Warteschlangen verfolgen die jüngsten Trade-Ergebnisse für Longs und Shorts.
- `TotalTrigger` begrenzt die Warteschlangenlänge; nur die letzten N Ergebnisse werden berücksichtigt.
- `LossTrigger` definiert, wie viele Verluste innerhalb dieser Warteschlange die Trade-Größe auf `SmallVolume` umschalten.
- Wenn der Verlustschwellenwert nicht überschritten wird, verwendet die Strategie `NormalVolume`.

## Parameter
| Gruppe | Parameter | Beschreibung | Standard |
| --- | --- | --- | --- |
| Long | `LongCandleType` | Kerzentyp (Zeitrahmen) für Long-Berechnungen. | 8-Stunden-Zeitrahmen |
| Long | `LongFastLength` | Schnelle Jurik-Länge in der Long-JCCX-Approximation. | 23 |
| Long | `LongSlowLength` | Langsame Jurik-Länge für die Long-JCCX-Approximation. | 50 |
| Long | `LongSmoothLength` | Jurik-Glättungslänge auf Zähler/Nenner angewendet. | 8 |
| Long | `LongPhase` | Phasenparameter, übersetzt in einen Glättungsfaktor (0.05–0.95). | 100 |
| Long | `LongCycle` | Rollfensterlänge für die stochastischen Transformationen. | 10 |
| Long | `LongSignalBar` | Verzögerung (in Balken) bevor ein Signal ausgewertet wird. | 1 |
| Long | `LongAppliedPrice` | Preisquelle für Long-Berechnungen. | Close |
| Long | `LongAllowOpen` / `LongAllowClose` | Long-Einstiege oder -Ausstiege aktivieren/deaktivieren. | true |
| Long | `LongTotalTrigger` | Anzahl der zuletzt gespeicherten Long-Trades für die MMRec-Warteschlange. | 5 |
| Long | `LongLossTrigger` | Erforderliche Verluste innerhalb der Warteschlange zum Wechsel zu kleinem Volumen. | 3 |
| Long | `LongSmallVolume` / `LongNormalVolume` | Reduzierte und Standard-Long-Trade-Größen. | 0.01 / 0.1 |
| Long | `LongStopLoss` / `LongTakeProfit` | Optionale Stop/Take-Abstände in Preisschritten. | 1000 / 2000 |
| Short | Wie Long (mit Präfix `Short`). | | |

## Risikohinweise
- Preisschritte werden vom aktuellen `Security` abgerufen. Stellen Sie sicher, dass das Instrument einen gültigen `PriceStep` hat oder passen Sie die Parameter entsprechend an.
- Stop-Loss- und Take-Profit-Prüfungen werden bei abgeschlossenen Kerzen ausgewertet; die Ausführungsqualität innerhalb der Kerze hängt von der Kerzenauflösung ab.
- Das MMRec-Modul beruht auf dem Vergleich von Einstiegs- und Ausstiegspreisen. Im Live-Trading kann Slippage das effektive Ergebnis verändern.

## Nutzungstipps
- Beginnen Sie mit identischen Long/Short-Einstellungen, um den ursprünglichen Duplex-EA zu emulieren, und experimentieren Sie dann mit asymmetrischen Zeitrahmen.
- Senken Sie `SignalBar` auf null für schnellere Reaktionen; erhöhen Sie ihn, um Rauschen zu filtern.
- Optimieren Sie `LongPhase`/`ShortPhase` zusammen mit den Glättungslängen, um Reaktionsfähigkeit und Glätte feinzustimmen.
