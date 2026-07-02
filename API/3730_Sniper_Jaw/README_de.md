# Sniper Jaw-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Sniper Jaw Strategy** portiert den MetaTrader 4-Expertenberater `SniperJawEA.mq4` auf die High-Level-Strategie API von StockSharp. Das System analysiert den Bill Williams' Alligator-Indikator für den Kerzenmittelpreis. Ein Handel wird nur eingeleitet, wenn die drei geglätteten gleitenden Durchschnitte (Kiefer, Zähne und Lippen) in streng bullischer oder bärischer Reihenfolge gestapelt sind und alle im Vergleich zur vorherigen fertigen Kerze in die gleiche Richtung vorrücken.

## Handelslogik

1. **Alligator-Rekonstruktion** – drei `SmoothedMovingAverage`-Instanzen berechnen Kiefer, Zähne und Lippen auf dem Kerzenmedian `(High + Low) / 2`. Jede Linie kann um ihre eigene Anzahl von Balken nach vorne verschoben werden, um die Darstellung von MetaTrader widerzuspiegeln.
2. **Trendbestätigung** – ein Long-Bias wird erzeugt, wenn die verschobenen Werte `jaw < teeth < lips` erfüllen **und** jede Linie höher ist als bei der vorherigen Kerze. Eine kurze Tendenz erfordert `jaw > teeth > lips`, wobei sich alle drei Linien im Vergleich zum vorherigen Balken nach unten bewegen.
3. **Einstiegsverwaltung** – die Strategie öffnet jeweils nur eine Position. Wenn `UseEntryToExit` aktiviert ist und ein neues Gegensignal ausgelöst wird, wird die aktuelle Belichtung zuerst abgeflacht und der neue Befehl wird beim nächsten Signal gesendet.
4. **Schutzausstiege** – Stop-Loss- und Take-Profit-Abstände werden in Pips definiert und mithilfe des Wertpapiers `PriceStep` umgerechnet. Sowohl Long- als auch Short-Positionen werden bei jeder fertigen Kerze überwacht und geschlossen, sobald einer der Schwellenwerte erreicht ist.
5. **Signaldrosselung** – das ursprüngliche EA verhinderte doppelte Einträge durch Überprüfung des Zeitstempels der Leiste. Der Port speichert die Zeit der letzten Signalkerze und überspringt weitere Aufträge während desselben Balkens.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Handelsgröße in Lots oder Kontrakten, die an `BuyMarket`/`SellMarket` übergeben wurden. |
| `EnableTrading` | `true` | Hauptschalter, der das Deaktivieren neuer Einträge ermöglicht, während das Risikomanagement aktiv bleibt. |
| `UseEntryToExit` | `true` | Schließt eine bestehende Position, bevor ein Gegensignal aktiviert wird. Spiegelt das „Entry to Exit“-Flag des EA wider. |
| `StopLossPips` | `20` | Abstand des Schutzstopps vom Einstiegspreis. Null deaktiviert den Stopp. |
| `TakeProfitPips` | `50` | Abstand des Gewinnziels vom Einstiegspreis. Null deaktiviert das Ziel. |
| `MinimumBars` | `60` | Erforderliche Anzahl fertiger Kerzen, bevor das erste Signal ausgewertet wird. |
| `JawPeriod` / `TeethPeriod` / `LipsPeriod` | `13 / 8 / 5` | Länge der geglätteten gleitenden Durchschnitte, die die Alligator-Linien bilden. |
| `JawShift` / `TeethShift` / `LipsShift` | `8 / 5 / 3` | Vorwärtsverschiebung (in Balken), die verwendet wird, um die Alligator-Puffer an der MetaTrader-Version auszurichten. |
| `CandleType` | `1 hour time frame` | Abonnement der Primärkerzenserie. Passen Sie es an das in MetaTrader verwendete Diagramm an. |

## Nutzungshinweise

- Die Implementierung wertet nur fertige Kerzen (`CandleStates.Finished`) aus, um unvollständig gebildete Werte zu vermeiden.
- Stopp- und Zielwerte werden intern verfolgt; Die Strategie gibt Marktaufträge aus, um die Position zu glätten, wenn ein Niveau verletzt wird.
- Die Umrechnung von Preisschritten folgt der üblichen Forex-Konvention: 5- und 3-Dezimal-Symbole behandeln einen Pip als zehn Preisschritte.
- Fügen Sie die Strategie zusammen mit einem Connector, einem Portfolio und einer Sicherheitskonfiguration zu einem Schema hinzu. Nach dem Start der Strategie zeigt das Diagrammfeld die Kerzenreihe und die rekonstruierten Alligator-Linien zur schnellen visuellen Validierung an.
