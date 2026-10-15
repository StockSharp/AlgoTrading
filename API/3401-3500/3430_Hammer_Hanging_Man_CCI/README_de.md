# Hammer und hängender Mann mit CCI-Bestätigung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den Experten MetaTrader „AH HM CCI“ in StockSharp erneut. Es achtet auf Hammer und hängenden Kerzenleuchter
Muster und erfordert eine Bestätigung durch den Commodity Channel Index (CCI), bevor ein Handel abgeschlossen wird. Die zusätzlichen Bestätigungsfilter
erkennt schwache Muster und hilft dabei, Einträge an der durch CCI signalisierten Momentumverschiebung auszurichten.

Die Logik läuft nur auf abgeschlossenen Kerzen und verwendet einen kurzen einfachen gleitenden Durchschnitt (SMA), um den vorherrschenden Trend zu definieren. Der Vorherige
Kerze muss ein Hammer in einem Abwärtstrend mit überverkauftem CCI zum Kaufen oder ein hängender Mann in einem Aufwärtstrend mit überkauftem CCI zum Verkauf sein. Ausgänge
werden verwaltet, wenn CCI konfigurierbare Auslöseschwellen überschreitet, wodurch die abstimmungsbasierte Exit-Logik des ursprünglichen Experten repliziert wird.

## Handelslogik

1. **Trendfilter** – Der Mittelpunkt der vorherigen Kerze muss unter (für Long-Positionen) oder über (für Short-Positionen) einem berechneten Wert von SMA liegen
Schlusskurse. Dies ahmt die Trendprüfung des gleitenden Durchschnitts des ursprünglichen Assistenten nach.
2. **Mustererkennung** – Die Strategie wertet den vorherigen Balken aus und prüft:
   - Körper vollständig im oberen Drittel des Kerzenbereichs.
   - Lücke zwischen dem Öffnen/Schließen der vorherigen Kerze und der Kerze davor.
   - Richtungskontext (Hammer für einen Abwärtstrend, hängender Mann für einen Aufwärtstrend).
3. **CCI-Bestätigung** – Der CCI des vorherigen Balkens muss unter dem Long-Schwellenwert oder über dem Short-Schwellenwert liegen. Die Standardwerte
entsprechen der Vorlage MetaTrader (40 für Long-Positionen und 60 für Short-Positionen).
4. **Positionsausstiege** – Bestehende Positionen werden geschlossen, wenn CCI entweder die untere oder obere Ausstiegsschwelle überschreitet. Überquerung von
unten schließt Longs; Überqueren von oben schließt Shorts.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Kerzentyp und Zeitrahmen, die zur Mustererkennung verwendet werden. | `TimeSpan.FromMinutes(15)` |
| `CciPeriod` | Anzahl der vom Commodity Channel Index verwendeten Balken. | `11` |
| `MaPeriod` | Anzahl der Balken im Trendfilter SMA. | `5` |
| `LongConfirmationThreshold` | Maximal zulässiger CCI-Wert für ein Hammersignal. | `40` |
| `ShortConfirmationThreshold` | Zulässiger Mindestwert CCI für ein Hängender-Mann-Signal. | `60` |
| `ExitUpperThreshold` | CCI-Ebene, die nach einem Aufwärtsübergang Ausstiege auslöst. | `70` |
| `ExitLowerThreshold` | Sekundäre Ausstiegsebene für Frühsignale. | `30` |

Alle Parameter stehen zur Optimierung zur Verfügung. Die Schwellenwerte akzeptieren negative Werte, sodass Sie die Strategie an andere anpassen können
Märkte oder Lärmpegel durch Anziehen oder Lösen der Filter anpassen.

## Orderverwaltung

- Bei **Eingaben** werden Marktaufträge mit einer Größe von `Volume + |Position|` verwendet, um sicherzustellen, dass Umkehrungen in einem einzigen Trade ausgeführt werden.
- **Ausgänge** verlassen sich ausschließlich auf die CCI-Kreuze, um in der Nähe des MetaTrader-Experten zu bleiben. Fügen Sie bei Bedarf `StartProtection` Anrufe hinzu
explizite Stop-Loss- oder Take-Profit-Level.

## Nutzungstipps

- Wenden Sie die Strategie auf liquide Instrumente an, bei denen Candlestick-Gaps und -Tails informativ sind.
- Experimentieren Sie mit längeren `CciPeriod`- und `MaPeriod`-Werten, um Störungen beim Handel mit höheren Zeitrahmen auszugleichen.
- Eine Senkung von `LongConfirmationThreshold` oder eine Erhöhung von `ShortConfirmationThreshold` verringert die Anzahl der Trades, verbessert sich jedoch
Selektivität.
