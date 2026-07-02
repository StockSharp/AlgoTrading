# ACB1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **ACB1-Strategie** ist der StockSharp-Port des MetaTrader-Expertenberaters, der als `MQL/8586/ACB1.MQ4` vertrieben wird. Das ursprüngliche System handelt mit dem EURUSD-Paar und wartet auf starke tägliche Ausbrüche, bevor es in den Markt eintritt. Diese Konvertierung reproduziert den gleichen Entscheidungsprozess mit StockSharp High-Level-Primitiven:

- Tageskerzen (`SignalCandleType`) definieren die Ausbruchsrichtung und stellen die Stop- und Take-Profit-Anker dar.
- H4-Kerzen (`TrailCandleType`) bestimmen die Nachlaufstrecke, die mit `TrailFactor` multipliziert wird.
- Aufträge werden zum Marktwert ausgeführt, sobald die Ausbruchsbedingungen erfüllt sind und die Strategie nur eine Nettoposition behält, was die `OrdersTotal()`-Prüfungen im MQL-Code widerspiegelt.
- Stop-Loss und Take-Profit werden intern verwaltet: Die Strategie überwacht die besten Geld-/Briefkurse und schließt die Position mit Marktaufträgen, wenn die virtuellen Schutzniveaus durchbrochen werden.

## Handelsregeln

1. **Lange Einrichtung**
   - Verwenden Sie die zuvor fertige Tageskerze.
   - Wenn `Close > (High + Low) / 2` *und* der aktuelle Briefkurs über dem vorherigen Höchststand liegt, eröffnen Sie eine Long-Marktposition.
   - Der Stop-Loss wird auf dem vorherigen Tief platziert (gerundet auf die Preisstufe des Instruments).
   - Der Take-Profit entspricht dem Einstiegspreis plus `(High − Low) × TakeFactor`.

2. **Kurze Einrichtung**
   - Wenn `Close < (High + Low) / 2` *und* der aktuelle Geldkurs unter dem vorherigen Tief liegt, eröffnen Sie eine Short-Marktposition.
   - Stop-Loss wird auf das vorherige Hoch gesetzt; Take-Profit subtrahiert `(High − Low) × TakeFactor` vom Einstiegspreis.

3. **Trailing Stop**
   - Die zuletzt fertiggestellte Kerze `TrailCandleType` liefert `(High − Low) × TrailFactor`.
   - Bei Long-Positionen folgt der Stop `Bid − TrailDistance`, während der Preis unter dem Take-Profit abzüglich des Broker-Stop-Levels bleibt.
   - Bei Short-Positionen folgt der Stop `Ask + TrailDistance`, während der Preis über dem Take-Profit plus dem Stop-Level des Brokers bleibt.

4. **Risikowächter**
   - Die Strategie verfolgt das maximal beobachtete Portfolioeigenkapital. Der Handel wird genau wie beim ursprünglichen Berater immer dann unterbrochen, wenn das aktuelle Eigenkapital unter 50 % dieses Höchstwerts fällt.
   - Eine Abklingzeit von fünf Sekunden (`CooldownSeconds`) verhindert, dass neue Bestellungen eingehen oder Aktualisierungen zu häufig gestoppt werden, wodurch die `TimeLocal()`-Drosselung von MQL reproduziert wird.

## Positionsgrößenbestimmung und Risikokontrolle

- Das Volumen pro Trade ergibt sich aus `Portfolio.CurrentValue × RiskFraction`.
- Das monetäre Risiko pro Vertrag wird aus der Stop-Distanz und den Sicherheitsmetadaten (`PriceStep` und `StepPrice`) berechnet.
- Die resultierende Größe wird auf `Security.VolumeStep` ausgerichtet und auf `[Security.MinVolume, Security.MaxVolume]` begrenzt, dann durch den Parameter `MaxVolume` begrenzt (Standard 5 Lose).
- Aufträge werden übersprungen, wenn das normalisierte Volumen Null ist oder wenn die Stoppdistanz gegen `MinStopDistancePoints` verstößt, was die MetaTrader `MODE_STOPLEVEL`-Prüfung emuliert.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `SignalCandleType` | Täglich | Kerzentyp, der zur Ausbruchserkennung verwendet wird. |
| `TrailCandleType` | 4 Stunden | Kerzentyp, der die Trailing-Stop-Distanz liefert. |
| `TakeFactor` | 0,8 | Auf den Tagesbereich angewendeter Multiplikator zur Berechnung des Take-Profits. |
| `TrailFactor` | 10 | Beim Aktualisieren des Stopps wird ein Multiplikator auf den Endbereich angewendet. |
| `RiskFraction` | 0,05 | Anteil des Portfolio-Eigenkapitals, das bei jedem Trade riskiert wird (5 %). |
| `MaxVolume` | 5 | Feste Obergrenze für das endgültige Bestellvolumen. |
| `MinStopDistancePoints` | 0 | Minimale Stop-/Take-Distanz, ausgedrückt in Preispunkten; Legen Sie es auf den Broker `MODE_STOPLEVEL` fest. |
| `CooldownSeconds` | 5 | Minimale Verzögerung zwischen aufeinanderfolgenden Handelsaktionen. |

## Hinweise zur Implementierung

- Die Strategie erfordert die richtigen Instrumentenmetadaten: `Security.PriceStep`, `Security.StepPrice`, `Security.VolumeStep`, `Security.MinVolume` und (falls verfügbar) `Security.MaxVolume`.
- Schutzstufen sind virtuell. StockSharp schließt Positionen über Marktaufträge, wenn Bid/Ask den berechneten Stop-Loss oder Take-Profit berührt.
- Die Eigenkapitalverfolgung verwendet `Portfolio.CurrentValue`. Wenn der Connector dieses Feld nicht bereitstellt, wird der Risikoschutz den Handel so lange deaktiviert lassen, bis es verfügbar ist.
- Es wird nur eine einzige Nettoposition gehalten. Gegensignale während eines aktiven Handels werden ignoriert, bis die Position vollständig geschlossen ist.
- Es ist kein Python-Port enthalten; Dieses Verzeichnis enthält nur die C#-Implementierung und Dokumentation.
