# BreakRevert Pro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

BreakRevert Pro ist die StockSharp-Konvertierung des MetaTrader 5 Expertenberaters *BreakRevertPro.mq5*. Die Strategie kombiniert die Ausbruchsbestätigung im Ein-Minuten-Zeitrahmen mit einem breiteren Trend- und Volatilitätskontext aus den 15-Minuten- und 1-Stunden-Charts. Werte im Wahrscheinlichkeitsstil werden durch indikatorgesteuerte Näherungen reproduziert, sodass das Verhalten nahe am ursprünglichen EA bleibt und gleichzeitig StockSharp übergeordneten API-Mustern folgt.

## Kernlogik

1. **Primärer Zeitrahmen (1 Minute)**
   - Der Average True Range (ATR) schätzt die Intraday-Volatilität.
   - Ein gleitender Durchschnitt der Schlusskurse misst die kurzfristige Richtungsverzerrung.
   - Ein zweiter gleitender Durchschnitt verfolgt die Häufigkeit großer Bewegungen von Kerze zu Kerze und stellt die Poisson-Ausbruchswahrscheinlichkeit aus dem MQL-Code dar.
   - Ein exponentieller gleitender Durchschnitt absoluter Preisbewegungen erzeugt die vom ursprünglichen Sicherheitsfilter verwendete exponentielle Wahrscheinlichkeit.
2. **Bestätigungszeitraum (15 Minuten)**
   - Ein einfacher gleitender Durchschnitt misst die mittelfristige Trendrichtung und blockiert Trades gegen den vorherrschenden Fluss.
3. **Kontext-Zeitrahmen (1 Stunde)**
   - Stündliche Kerzen bieten den höheren Zeitrahmentrend und den Volatilitätsbereich, der für die Ausbruchsvalidierung und die Überprüfung der Abflachung der Mittelwertreversion erforderlich ist.

Wenn die Poisson- und Weibull-Proxy-Wahrscheinlichkeiten die Ausbruchsschwelle überschreiten, die 1-Minuten- und 15-Minuten-Trends nach oben ausgerichtet sind und die stündliche Volatilität erhöht ist, geht die Strategie in einen Long-Breakout-Trade ein. Wenn umgekehrt die Wahrscheinlichkeiten unter die Mean-Reversion-Schwelle fallen und der stündliche Trend flach ist, verkauft die Strategie Leerverkäufe und zielt auf Pullbacks zurück in die Spanne. Marktaufträge werden verwendet, um den unmittelbaren Ausführungsstil des ursprünglichen Expertenberaters widerzuspiegeln.

## Risikomanagement

- Eine konfigurierbare Handelsverzögerung verhindert übermäßigen Handel, indem sie eine Pause zwischen aufeinanderfolgenden Einträgen erzwingt.
- `MaxPositions` begrenzt die Anzahl gleichzeitig offener Positionen. Bei der Umkehrung von einem entgegengesetzten Trade schließt die Strategie das aktuelle Engagement und eröffnet die neue Richtung in einer einzigen Marktorder.
- Bei der dynamischen Volumenschätzung werden der Kontostand, die von ATR abgeleitete Stoppdistanz und der Prozentsatz von `RiskPerTrade` verwendet, um eine konservative Losgröße zu ermitteln. Schlägt die Berechnung fehl, wird das minimale Schrittvolumen als sicherer Standardwert verwendet.
- Für Validierungs- oder Testumgebungen, in denen mindestens ein Trade vorhanden sein muss, können optionale Sicherheits-Trades aktiviert werden. Die Richtung des Sicherheitshandels folgt der kombinierten kurz- und mittelfristigen Trendschätzung.
- `StartProtection()` aktiviert den integrierten Schutzblock von StockSharp, sodass unerwartete Verbindungsprobleme nicht dazu führen, dass Positionen nicht verwaltet werden.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `RiskPerTrade` | Risiko pro Trade in Prozent des Portfoliowerts (wird zur dynamischen Lotberechnung verwendet). |
| `LookbackPeriod` | Anzahl der fertigen Kerzen, die für gleitende Durchschnitte und ATR-Berechnungen über alle Zeitrahmen hinweg verwendet werden. |
| `BreakoutThreshold` | Minimale zusammengesetzte Wahrscheinlichkeit, die für einen Ausbruchseinstieg erforderlich ist. |
| `MeanReversionThreshold` | Maximale Wahrscheinlichkeit, die immer noch Mean-Reversion-Shorts zulässt. |
| `TradeDelaySeconds` | Mindestanzahl von Sekunden zwischen aufeinanderfolgenden Einträgen. |
| `MaxPositions` | Maximale gleichzeitige Positionen (sowohl für Long- als auch für Short-Exposure verwendet). |
| `EnableSafetyTrade` | Ermöglicht optionale Validierungssicherheitsgeschäfte, wenn keine Positionen offen sind. |
| `SafetyTradeIntervalSeconds` | Wartezeit zwischen Sicherheitshandelsprüfungen. |
| `CandleType` | Primärer Zeitrahmen, der für das Hauptsignalabonnement verwendet wird (Standard: 1 Minute). |

## Nutzungshinweise

1. Hängen Sie die Strategie an ein Instrument an, das 1-Minuten-Daten unterstützt und 15-Minuten- und 1-Stunden-Kerzen bereitstellt (StockSharp aggregiert automatisch höhere Frames, wenn der Broker Minutenbalken bereitstellt).
2. Legen Sie die Eigenschaft `Volume` fest, wenn eine feste Bestellgröße erforderlich ist. Andernfalls leitet die Strategie eine konservative Größe aus dem Kontostand und ATR ab.
3. Passen Sie Schwellenwerte und Lookback-Längen entsprechend dem Volatilitätsprofil des Zielmarkts an. Paare mit höherer Volatilität können von größeren Schwellenwerten profitieren, um häufige falsche Ausbrüche zu vermeiden.
4. Sicherheitsgeschäfte sind in erster Linie für Validierungsszenarien gedacht, bei denen der ursprüngliche EA auch ohne Signal mindestens einen Handel ausgeführt hat. Deaktivieren Sie sie für normale Live-Handelsumgebungen.

Die Konvertierung behält die ursprüngliche Idee bei, Ausbruchserkennung mit Umkehrschutzmaßnahmen zu kombinieren und verlässt sich dabei auf das High-Level-Indikator-Framework von StockSharp, um effizient und testfreundlich zu bleiben.
