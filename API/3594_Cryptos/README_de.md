# Krypto-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Cryptos-Strategie** ist eine High-Level-StockSharp-Portierung des ursprünglichen MetaTrader4-Expertenberaters `cryptos.mq4`. Es konzentriert sich auf das ETH/USD-Paar und kombiniert Bollinger-Bänder mit einem linear gewichteten gleitenden Durchschnitt (LWMA), um Ausbrüche aus der Volatilitätskompression zu erfassen. Die Strategie verfolgt Swing-Hochs und -Tiefs über eine konfigurierbare Anzahl von Kerzen und berechnet dynamisch Take-Profit-Ziele als Vielfaches der erkannten Spanne.

## Handelslogik

1. **Trenderkennung** – wenn der Schlusskurs das obere Bollinger-Band berührt, wechselt die Strategie in eine Short-Tendenz, und wenn das untere Band berührt wird, wechselt sie in eine Long-Tendenz. Die Bandberührung friert außerdem die aktuellen Swing-Werte ein, indem die automatische Hoch-/Tiefaktualisierung deaktiviert wird.
2. **Eintrittsbedingungen** –
   - Eröffnen Sie eine Short-Position, wenn der Schlusskurs unter den LWMA fällt, die Tendenz Short ist und keine aktive Short-Position besteht.
   - Eröffnen Sie eine Long-Position, wenn der Schlusskurs über den LWMA steigt, die Tendenz long ist und keine aktive Long-Position besteht.
3. **Bereichsprojektion** – Swing-Hochs und -Tiefs (entweder automatisch oder manuell eingefroren) definieren den Abstand vom LWMA. Dieser in Ticks ausgedrückte Abstand wird mit der Take-Profit-Quote multipliziert, um Gewinnziele und die risikobasierte Positionsgröße zu berechnen.
4. **Risikokontrolle** – die Strategie legt Take-Profit- und Stop-Loss-Niveaus pro Trade fest. Bei Long-Positionen wird der Stop unterhalb des Swing-Tiefs platziert; für Shorts, oberhalb der Swing-Höhe. Stopps und Ziele werden für jeden Eintrag neu berechnet und innerhalb der Strategieschleife durchgesetzt.
5. **Trailing-Exits** – wenn eine Long-Position unterhalb des unteren Bollinger-Bandes (oder eine Short-Position über dem oberen Band) schließt, wird die offene Position sofort abgeflacht, wodurch das Trailing-Verhalten des ursprünglichen EA nachgeahmt wird.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Datentyp der Kerzenserie, der für alle Indikatorberechnungen verwendet wird. |
| `BollingerPeriod`, `BollingerWidth` | Länge und Standardabweichungsmultiplikator der Bollinger-Bänder. |
| `MaPeriod` | Zeitraum des linear gewichteten gleitenden Durchschnitts basierend auf Medianpreisen. |
| `LookbackCandles` | Anzahl der untersuchten Kerzen, um den automatischen Hoch-/Tiefschwung zu bestimmen. |
| `TakeProfitRatio` | Bereichsmultiplikator, der für Gewinnziele beim Handel mit ETH/USD verwendet wird. |
| `AlternativeTakeProfitRatio` | Bereichsmultiplikator wird auf alle anderen Symbole angewendet. |
| `RiskPerTrade` | Kapitalbetrag (in Quotierungswährung), den der Volumenrechner bei jedem Trade zu riskieren versucht. |
| `ValueIndex`, `CryptoValueIndex` | Multiplikatoren, die das Risiko in Volumen für Nicht-Krypto- bzw. Krypto-Symbole umwandeln. |
| `MinVolume`, `MaxVolume` | Feste Grenzen für die Positionsgröße nach der Ausrichtung, um Volumenschritte auszutauschen. |
| `MinRangeTicks` | Minimal zulässiger projizierter Bereich in Ticks, um Null-Distanz-Stopps zu vermeiden. |
| `SpreadPoints` | Manuelle Überschreibung des Spreads in Ticks (wird automatisch anhand des besten Geld-/Briefkurses ermittelt, sofern verfügbar). |
| `GlobalTrend` | Manuelle Bias-Überschreibung: `1` erzwingt eine kurze Einrichtung, `2` erzwingt eine lange Einrichtung, `0` lässt die Strategie entscheiden. |
| `AutoHighLow` | Wenn diese Option aktiviert ist, werden die Swing-Punkte bei jeder Kerze neu berechnet; Wenn sie deaktiviert sind, bleiben sie bis zur nächsten Bandberührung eingefroren. |
| `ManualBuyTrigger`, `ManualSellTrigger` | Auf `true` setzen, um einen sofortigen Long- oder Short-Eintrag anzufordern (nach der Ausführung zurückgesetzt). |
| `SkipBuys`, `SkipSells` | Deaktivieren Sie die Eröffnung neuer Long- oder Short-Positionen. |

## Positionsgrößen

Die Strategie repliziert die MT4-Logik: `volume = RiskPerTrade / rangeTicks * valueIndex`. Das Ergebnis wird an `VolumeStep` angepasst und dann zwischen `MinVolume`/`MaxVolume` und den durch die Börse auferlegten Limits des Instruments begrenzt.

## Nutzungshinweise

- Die Strategie prüft den Portfoliowert beim Start. Wenn der Kontostand unter `RiskPerTrade * 3` liegt, wird der Handel deaktiviert und eine Warnung wird protokolliert, die der Sicherheitsüberprüfung von EA entspricht.
- Manuelle Trigger und Bias-Kontrollen ermöglichen die Synchronisierung mit Ermessensentscheidungen während des Live-Handels.
- ETH/USD verwendet automatisch `CryptoValueIndex` und `TakeProfitRatio`; andere Instrumente greifen auf die alternativen Parameter zurück.
- Stopps und Ziele werden innerhalb der Strategieschleife durchgesetzt, sodass kein zusätzliches Schutzmodul erforderlich ist.
