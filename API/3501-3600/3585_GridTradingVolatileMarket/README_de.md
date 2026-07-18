# Grid-Handel am volatilen Markt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader-Experten „Gridtrading_at_volatile_market.mq4“ unter Verwendung der StockSharp-Hochebene API. Es handelt um Donchian Kanalgrenzen, die in einem längeren Zeitrahmen erkannt wurden, und bestätigt gleichzeitig Einträge mit verschlingenden Mustern im Handelszeitrahmen. Sobald ein Raster aktiv ist, fügt die Strategie durchschnittliche Aufträge hinzu, wenn der Preis um ein Vielfaches des höheren Zeitrahmens ATR steigt, und endet, wenn die Portfoliogewinn- oder Drawdown-Ziele erreicht sind.

## Wie es funktioniert
1. Es werden zwei Candle-Streams verwendet: der vom Benutzer ausgewählte Handelszeitrahmen und ein automatisch daraus abgeleiteter höherer Zeitrahmen (M1→M5→M15→M30→H1→H4→D1).
2. Auf dem höheren Zeitrahmen berechnet die Strategie:
   - `ATR(20)` zur Größe des Rasterabstands.
   - `SMA(SlowMaLength)`, um den Trend zusammen mit RSI zu filtern.
   - `DonchianChannels(20)` für Unterstützungs- und Widerstandsniveaus.
3. Im Handelszeitraum werden die letzten beiden abgeschlossenen Kerzen verfolgt, um bullische oder bärische Engulfing-Muster zu erkennen.
4. Ein langes Raster beginnt, wenn die vorherige Kerze das untere Band von Donchian berührt, ein bullisches Engulfing-Muster bildet und RSI überverkaufte Bedingungen bestätigt (`RSI < 35`, während der Preis über dem höheren Zeitrahmen SMA liegt). Ein kurzes Gitter spiegelt diese Regeln im oberen Band mit `RSI > 65` wider.
5. Nach der ersten Marktorder behält die Strategie den Anfangspreis als Anker bei. Wenn sich der Preis im aktuellen Rasterschritt um `2 * ATR` gegenüber der Position bewegt, wird eine weitere Order mit einem Volumen multipliziert mit `GridMultiplier` hinzugefügt.
6. Das Raster wird geschlossen und alle Bestellungen werden storniert, wenn:
   - Der kombinierte (realisierte + nicht realisierte) PnL übersteigt `TakeProfitFactor * total grid volume`.
   - Der Drawdown fällt unter `-MaxDrawdownFraction * initial portfolio value`.

## Parameter
- **TakeProfitFactor** – Gewinnmultiplikator des gesamten Netzvolumens, das zum Schließen des Netzes erforderlich ist (Standard `0.1`).
- **SlowMaLength** – Zeitraum des höheren Zeitrahmens SMA, der zum Filtern verwendet wird (Standard `50`).
- **GridMultiplier** – geometrischer Faktor, der auf jede zusätzliche Mittelungsreihenfolge angewendet wird (Standard `1.5`).
- **BaseOrderVolume** – Volumen der ersten Ordnung im Raster (Standard `0.1`).
- **MaxDrawdownFraction** – maximaler Verlust im Verhältnis zum anfänglichen Portfoliowert, bevor das Raster zwangsweise geschlossen wird (Standard `0.8`).
- **CandleType** – Handelszeitraum. Der höhere Zeitrahmen wird automatisch abgeleitet.

## Notizen
- Es werden nur geschlossene Kerzen verarbeitet, um ein Nachlackieren zu vermeiden.
- Die Strategie stützt sich auf verfügbare Geld-/Briefkurse, um den offenen PnL zu bewerten; Wenn nur die letzten Handelspreise angegeben werden, ist die Näherung möglicherweise weniger genau.
- Wenn die Portfolioinformationen nicht verfügbar sind, wird der Drawdown-Schutz übersprungen, sodass das Raster weiterläuft, bis das Gewinnziel erreicht ist oder die Position manuell geschlossen wird.
