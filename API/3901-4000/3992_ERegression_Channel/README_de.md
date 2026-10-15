# E-Regressionskanalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **E-Regression-Channel-Strategie** reproduziert den Expertenberater „e-Regr“ von MetaTrader unter Verwendung der übergeordneten Strategie API von StockSharp. Es passt eine polynomiale Regressionskurve an aktuelle Schlusskurse an, bildet aus der Reststandardabweichung äquidistante Bänder und reagiert, wenn der Preis diese Grenzen durchbricht. Die Strategie ist für den Mean-Reversion-Handel mit optionalen Schutzstopps, einem täglichen Volatilitätsfilter und einem Intraday-Handelsfenster konzipiert.

## Handelslogik
1. Abonnieren Sie den durch `Candle Type` angegebenen Hauptzeitrahmen und berechnen Sie einen polynomialen Regressionskanal für die letzten `Regression Length`-Abschlüsse.
2. Das mittlere Band ist die Regressionsanpassung; Die oberen und unteren Bänder werden um `Std Dev Multiplier` multipliziert mit der Reststandardabweichung verschoben.
3. Schließen Sie alle bestehenden Long-Positionen, wenn der Kerzenschluss das mittlere Band überschreitet; Schließen Sie Short-Positionen, wenn der Schlusskurs darunter fällt.
4. Eröffnen Sie eine Long-Position (nachdem Sie ein bestehendes Short-Engagement geschlossen haben), wenn das aktuelle Kerzentief das untere Band berührt oder durchbricht.
5. Eröffnen Sie eine Short-Position (nach Abflachung der Langzeitexponierung), wenn das aktuelle Kerzenhoch das obere Band berührt oder durchbricht.
6. Verfolgen Sie offene Positionen optional mit `Trailing Activation` und `Trailing Distance`, sobald sich der Preis weit genug zugunsten des Handels bewegt.
7. Überspringen Sie neue Einträge, wenn der Bereich der vorherigen Tageskerze den Schwellenwert `Daily Range Filter` überschreitet oder die aktuelle Zeit außerhalb des Fensters `[Trade Start, Trade End)` liegt.

## Parameter
- `Volume` – Ordergröße, die für jeden Markteintritt verwendet wird (Nettopositionen werden vor der Umkehrung abgeflacht).
- `Trade Start` / `Trade End` – tägliches Handelsfenster, unterstützt Übernachtbereiche (z. B. 21:00–02:00).
- `Regression Length` – Anzahl der Kerzen, die für die polynomiale Regressionsanpassung verwendet werden.
- `Degree` – Polynomgrad (1–6), angewendet auf das Regressionsmodell.
- `Std Dev Multiplier` – Multiplikator, der auf die Regressions-Reststandardabweichung angewendet wird, um die Bänder zu bilden.
- `Enable Trailing` – schaltet die Trailing-Stop-Verwaltung um.
- `Trailing Activation` – Anzahl der Punkte mit günstiger Bewegung, die erforderlich sind, bevor das Nachlaufen beginnt.
- `Trailing Distance` – Trailing-Puffer, der beibehalten wird, sobald Trailing aktiv ist (in Punkten).
- `Stop Loss` – Schutzstoppabstand in Punkten (0 deaktiviert den automatischen Stopp).
- `Take Profit` – Entfernung des Schutzgewinnziels in Punkten (0 deaktiviert das automatische Ziel).
- `Daily Range Filter` – maximal zulässiger Bereich der vorherigen Tageskerze, ausgedrückt in Punkten.
- `Candle Type` – Zeitrahmen für die primäre Preisreihe (standardmäßiger 30-Minuten-Zeitrahmen).

## Standardeinstellungen
- `Volume` = 0,1
- `Trade Start` = 03:00
- `Trade End` = 21:20
- `Regression Length` = 250 Balken
- `Degree` = 3
- `Std Dev Multiplier` = 1,0
- `Enable Trailing` = falsch
- `Trailing Activation` = 30 Punkte
- `Trailing Distance` = 30 Punkte
- `Stop Loss` = 0 Punkte (deaktiviert)
- `Take Profit` = 0 Punkte (deaktiviert)
- `Daily Range Filter` = 150 Punkte
- `Candle Type` = 30-Minuten-Kerzen

## Zusätzliche Hinweise
- Die Strategie verwendet für alle Entscheidungen die zuletzt abgeschlossene Kerze und handelt niemals mehrmals innerhalb desselben Balkens.
- Trailing-Stops schließen Positionen nach Markt, wenn der Preis das intern berechnete Trailing-Niveau berührt.
- Wenn der Vortag zu volatil ist (Bereich über dem konfigurierten Filter), werden bestehende Positionen geschlossen und neue Einträge für den Rest des Balkens ausgesetzt.
- Der Regressionskanal wird bei jeder Aktualisierung im Diagramm neu gezeichnet, um die Visualisierung der mittleren, oberen und unteren Bänder zu erleichtern.
