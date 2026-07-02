# e-TurboFx Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **e-TurboFx Momentum Strategy** ist eine direkte Portierung des ursprünglichen MetaTrader 4 Expertenberaters „e-TurboFx“. Das System scannt die zuletzt fertiggestellten Kerzen und sucht nach Richtungsabschnitten, in denen sich die Kerzenkörper weiter ausdehnen. Aufeinanderfolgende bärische Kerzen mit wachsender Körpergröße signalisieren eine mögliche Kapitulation, die mit einem Long-Einstieg abgeschwächt werden kann, während aufeinanderfolgende bullische Kerzen mit expandierenden Körpern auf eine überzogene Rallye hinweisen, die möglicherweise leer verkauft wird. Die StockSharp-Implementierung hält die Logik ereignisgesteuert durch Kerzenabonnements und fügt automatisch optionalen Stop-Loss- und Take-Profit-Schutz hinzu.

## Handelslogik
1. Abonnieren Sie einen konfigurierbaren Kerzentyp (Zeitrahmen) und verarbeiten Sie nur fertige Kerzen.
2. Verfolgen Sie zwei separate Sequenzen: eine für bärische Kerzen und eine für bullische Kerzen.
3. Messen Sie für jede Kerze die absolute Körpergröße (`|Close - Open|`).
4. Setzen Sie die Gegenrichtungssequenz zurück, sobald eine Kerze in die andere Richtung schließt.
5. Innerhalb jeder Sequenz sind streng expandierende Körper erforderlich – jede neue Kerze muss einen größeren Körper haben als die vorherige. Jede Kontraktion startet den Sequenzzähler von 1 neu.
6. Wenn die Anzahl der Kerzen in einer Sequenz `DepthAnalysis` erreicht, lösen Sie einen Markteintritt in die entgegengesetzte Richtung der letzten Sequenz aus (Kauf nach Abwärtstrends, Verkauf nach Aufwärtstrends).
7. Sobald eine Position offen ist, pausieren Sie die Signalerkennung, bis die Strategie zu einer flachen Position zurückkehrt. Das integrierte `StartProtection` verwaltet optionale Stop-Loss- und Take-Profit-Abstände, ausgedrückt in Preisschritten (Ticks).

Dieses Verhalten reproduziert den MQL4-Algorithmus, bei dem der Fachberater die letzten `N` geschlossenen Kerzen überprüfte und bestätigte, dass alle Körper in die gleiche Richtung ausgerichtet waren und dass jeder Körper größer als der Körper der nächstälteren Kerze war.

## Details zur Implementierung
- Verwendet das High-Level-Kerzenabonnement API mit `SubscribeCandles` und `Bind`, um die Projektrichtlinien einzuhalten.
- Behält nur Skalarfelder (`_bearishSequence`, `_bullishSequence`, `_previousBearishBody`, `_previousBullishBody`), um benutzerdefinierte Sammlungen zu vermeiden und sich auf den internen Status zwischen Ereignissen zu verlassen.
- Ruft `StartProtection` nur einmal in `OnStarted` auf, um optionale Stop-Loss- und Take-Profit-Orders in Preisschritten zu konfigurieren. Ein Wert von `0` deaktiviert jede Schutzanordnung, genau wie der ursprüngliche Experte.
- Bietet ausführliche englische Kommentare im Quellcode, einschließlich Erläuterungen zu Zurücksetzungen und Eintragsauslösern.
- Zeichnet Kerzen und eigene Trades in einem Diagrammbereich, wenn es im Designer oder auf der Benutzeroberfläche ausgeführt wird, um das Debuggen zu erleichtern.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `DepthAnalysis` | Anzahl der aufeinanderfolgenden fertigen Kerzen, die in eine Richtung mit expandierenden Körpern erforderlich sind, bevor ein Handel eröffnet wird. | `3` |
| `TakeProfitSteps` | Take-Profit-Distanz gemessen in Börsenpreisschritten (Ticks). Auf `0` setzen, um den Take-Profit zu deaktivieren. | `120` |
| `StopLossSteps` | Stop-Loss-Distanz gemessen in Börsenkursschritten (Ticks). Auf `0` setzen, um den Stop-Loss zu deaktivieren. | `70` |
| `TradeVolume` | Mit jeder Marktorder gesendetes Volumen. Wenn Sie diesen Parameter ändern, wird auch die Basis `Strategy.Volume` aktualisiert. | `0.1` |
| `CandleType` | Für die Analyse abonnierter Kerzendatentyp (Zeitrahmen). | `1 hour` |

Alle numerischen Parameter stellen Optimierungsmetadaten bereit, sodass die Strategie bei Bedarf mit StockSharp Optimierern optimiert werden kann.

## Hinweise und Empfehlungen
- Da die Strategie auf die Expansion des Kerzenkörpers reagiert, beeinflusst der gewählte Zeitrahmen die Signalfrequenz erheblich. Kürzere Intervalle führen zu mehr Trades, erfordern jedoch möglicherweise engere Schutzabstände.
- Stellen Sie sicher, dass die verbundene Sicherheit einen gültigen `PriceStep` definiert. andernfalls können die stufenweisen Schutzabstände nicht in absolute Preise umgerechnet werden.
- Testen Sie den Port im StockSharp-Designer vor der Live-Bereitstellung erneut, um zu überprüfen, wie Stopp und Ziel für das ausgewählte Instrument übersetzt werden.
- Die Strategie behält jeweils eine einzelne offene Position bei. Nach jedem Beenden werden die Zähler zurückgesetzt und das Muster muss von Grund auf neu erstellt werden, um das ursprüngliche MQL4-Verhalten widerzuspiegeln.
