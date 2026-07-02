# Expert Master EURUSD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Expert Master EURUSD-Strategie repliziert den MetaTrader 4 Expert Advisor *Expert Master*.
Es wertet ein Vier-Kerzen-Muster auf den Haupt- und Signalleitungen MACD aus (schneller EMA = 5, langsamer EMA = 15, Signal EMA = 3).
Der Algorithmus geht davon aus, dass der Indikator in eine Richtung Schwung aufbaut, bevor er einen Ausbruch in die entgegengesetzte Richtung auslöst.

## Handelslogik

### Lange Einrichtung
1. Die Signallinie MACD bildet bei den drei vorherigen Kerzen eine absteigende Sequenz und dreht sich bei der aktuellen Kerze nach oben.
2. Die Hauptlinie von MACD bildet eine „V“-Form, wobei der aktuelle Wert über den vorherigen drei Messwerten liegt.
3. Der vorherige Hauptlinienwert liegt unter dem konfigurierbaren unteren Schwellenwert (Standard: −0,00020).
4. Der älteste Hauptlinienwert liegt unter Null, während der aktuelle Wert über dem oberen Schwellenwert liegt (Standard 0,00020).

### Kurze Einrichtung
1. Die Signallinie MACD bildet bei den drei vorherigen Kerzen eine aufsteigende Sequenz und dreht bei der aktuellen Kerze nach unten.
2. Die Hauptlinie von MACD bildet ein umgekehrtes „V“, wobei der aktuelle Wert unter den vorherigen drei Messwerten liegt.
3. Der vorherige Hauptzeilenwert überschreitet den oberen Schwellenwert (Standard 0,00020).
4. Der älteste Hauptleitungswert liegt über Null, während der aktuelle Wert unter den kurzen Schwellenwert fällt (Standard: −0,00035).

## Positionsmanagement

- **Ausstieg bei Momentumverlust:** Eine Long-Position wird geschlossen, wenn der aktuelle MACD-Hauptwert unter den vorherigen fällt.
Short-Positionen werden geschlossen, wenn der aktuelle MACD-Hauptwert über den vorherigen steigt.
- **Trailing Stop:** Nachdem sich der Preis um die konfigurierte Anzahl von Punkten zugunsten des Handels bewegt hat, wird ein Trailing Stop aktiviert.
Der Stop wird bei jeder fertigen Kerze anhand des Kerzenschlusses minus/plus der nachlaufenden Distanz aktualisiert.
Wenn der Preis zum Trailing Stop zurückkehrt, wird die Strategie über eine Marktorder beendet.

## Risikomanagement

- Das Handelsvolumen entspricht standardmäßig der festen Losgröße, kann jedoch über den Parameter **Risikoprozent** dynamisch angepasst werden.
Wenn die Risikodimensionierung aktiviert ist, riskiert die Strategie bei jedem Einstieg einen Bruchteil des Portfoliowerts und ahmt so das ursprüngliche EA-Verhalten nach.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TrailingPoints` | Trailing-Stop-Distanz in Preispunkten. | 25 |
| `FixedVolume` | Fallback-Handelsvolumen, wenn die Risikogröße nicht verfügbar ist. | 1 |
| `RiskPercent` | Prozentsatz des Portfoliowerts, der zur Größenbestimmung von Positionen verwendet wird. | 0,01 |
| `MacdFastPeriod` | Schnelle EMA-Länge für die MACD-Hauptzeile. | 5 |
| `MacdSlowPeriod` | Langsame EMA-Länge für die MACD-Hauptzeile. | 15 |
| `MacdSignalPeriod` | Signallänge von EMA für den Indikator MACD. | 3 |
| `UpperMacdThreshold` | Für Einträge ist ein positiver Schwellenwert von MACD erforderlich. | 0,00020 |
| `LowerMacdThreshold` | Negativer MACD-Schwellenwert, der in langen Signalen verwendet wird. | −0,00020 |
| `ShortCurrentThreshold` | Auf den aktuellen Wert für Shorts wird ein negativer Schwellenwert von MACD angewendet. | −0,00035 |
| `CandleType` | Kerzentyp, der für Indikatorberechnungen verwendet wird. | Zeitrahmen von 1 Minute |

## Notizen

- Handeln Sie nur mit fertigen Kerzen, um auf dem hohen Niveau StockSharp API zu bleiben.
- Die Konvertierung behält die ursprüngliche EA-Logik bei, einschließlich risikobasierter Losgröße und Trailing-Stop-Verhalten, und fügt gleichzeitig eine umfassende Parametrisierung zur einfacheren Optimierung hinzu.
