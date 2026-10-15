# Bollinger Bands-Sitzungsumkehr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Portierung des MetaTrader-Expertenberaters **BollingerBandsEA (Version 3.0)**. Es werden Mean-Reversion-Setups gehandelt, die auftreten, nachdem der Preis während der aktiven Handelssitzung über die Bollinger-Bänder hinausgeht.

## Handelslogik

1. Abonnieren Sie die primäre Intraday-Kerzenserie (standardmäßig 15-Minuten-Kerzen) und eine tägliche Kerzenserie, die zum Erstellen des Trendfilters verwendet wird.
2. Berechnen Sie Bollinger-Bänder (Länge 20, Breite 2,0) für die Intraday-Serie und ein 100-Perioden-SMA für Tagesabschlüsse.
3. Verfolgen Sie die aktuellen und vorherigen Tageshochs/-tiefs und behalten Sie die vorherigen Bollinger-Bandwerte für die Signalauswertung bei.
4. Erlauben Sie nur Eingaben innerhalb des Handelssitzungsfensters: von `SessionStartOffsetMinutes` nach der Eröffnung des Handelstages bis `SessionEndOffsetMinutes` vor dem Ende des Handelstages.
5. Überspringen Sie den Handel, sobald der kumulierte PnL für den aktuellen Tag positiv wird, und ahmen Sie so den täglichen Stop von EA nach.
6. Geben Sie eine Short-Position ein, wenn die vorherige Kerze bärisch ist, über dem oberen Band schließt, der aktuelle Schlusskurs über diesem Band bleibt, die Bandbreite breit genug ist, der Preis unter dem täglichen SMA liegt und der Preis über dem aktuellen oder vorherigen Tageshoch gehandelt wird.
7. Geben Sie eine Long-Position ein, wenn die vorherige Kerze bullisch ist, unter dem unteren Band schließt, der aktuelle Schlusskurs unter diesem Band bleibt, die Bandbreite breit genug ist, der Preis über dem täglichen SMA liegt und der Preis unter dem aktuellen oder vorherigen Tagestief liegt.
8. Die Positionsgröße wird entweder durch das konfigurierte feste Volumen oder durch eine risikobasierte Größenbestimmung bestimmt, die den Abstand zum Stop-Loss in Punkten verwendet.
9. Ausstiege werden durch die Überprüfung von Stop-Loss, Take-Profit, optionalem Schließen auf dem mittleren Band, einem optionalen Trailing-Stop und der optionalen Break-Even-Logik durchgeführt. Verlierergeschäfte können auch nach einer konfigurierbaren Haltezeit liquidiert werden.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Für den Handel verwendete Intraday-Kerzenserien. |
| `BollingerLength` | Zeitraum des gleitenden Durchschnitts der Bollinger-Bänder. |
| `BollingerWidth` | Breitenmultiplikator der Bollinger-Bänder. |
| `DailyMaLength` | Länge des täglichen SMA-Filters. |
| `StopLossPoints` | Stop-Loss-Distanz, ausgedrückt in Instrumentenpunkten. |
| `UseRiskVolume` | Ermöglicht eine risikobasierte Positionsgrößenbestimmung. |
| `RiskPercent` | Kontoprozentsatz, der für die risikobasierte Dimensionierung verwendet wird. |
| `FixedVolume` | Festes Fallback-Volumen, wenn die Risikogrößenbestimmung deaktiviert oder nicht möglich ist. |
| `SessionStartOffsetMinutes` | Minuten nach Beginn der Sitzung, bevor Einträge zulässig sind. |
| `SessionEndOffsetMinutes` | Minuten vor Sitzungsende, wenn Einträge blockiert sind. |
| `CloseOnMiddleBand` | Ausstiegsposition, wenn der Preis das mittlere Band von Bollinger überschreitet. |
| `EnableTrailing` | Ermöglicht Trailing-Stop-Anpassungen. |
| `TrailingFactor` | Distanzmultiplikator erforderlich, bevor der Stopp nachgestellt wird. |
| `EnableBreakEven` | Ermöglicht die Verschiebung des Stops auf den Einstiegspreis. |
| `BreakEvenFactor` | Gewinnmultiplikator erforderlich, um den Stop auf die Gewinnschwelle zu bringen. |
| `CloseLosingAfterMinutes` | Schließt verlustbringende Trades, nachdem sie für die angegebene Minute gehalten wurden. |

## Notizen

- Schützende Stop-Loss- und Take-Profit-Orders werden simuliert, indem bei jeder Aktualisierung die Candle-Extreme überprüft werden. Passen Sie diesen Abschnitt an, wenn börsenseitige Schutzanordnungen erforderlich sind.
- Die risikobasierte Größenbestimmung hängt von `Security.Step` und `Security.StepPrice` ab. Fehlen diese Werte, greift die Strategie auf das Festvolumen zurück.
- Der tägliche Gewinnstopp nutzt die Strategie PnL, daher müssen realisierte und variable PnL in derselben Währung wie das Portfolio erfolgen.
