# VIDYA N-Bars-Ränder Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die ursprüngliche MetaTrader-Strategie kombiniert den Kanalindikator „VIDYA N Bars Borders“ mit einem Martingal-Positionsgrößenmodul. Der StockSharp-Port behält die Idee bei, zu kaufen, wenn der Preis unter das adaptive untere Band fällt, und zu verkaufen, wenn der Preis über das obere Band steigt. Die Kanalmitte wird durch einen adaptiven gleitenden Durchschnitt (VIDYA-Analog) erzeugt und seine Breite wird durch eine Average True Range-Hüllkurve gesteuert. Ein Money-Management-Block erhöht die Handelsgröße nach verlorenen Geschäften unter Einhaltung maximaler Positions- und Risikogrenzen.

## Handelslogik
1. Abonnieren Sie die ausgewählten Zeitrahmenkerzen.
2. Berechnen Sie einen Kaufman Adaptive Moving Average als VIDYA-Ersatz und einen ATR-Kanal um ihn herum.
3. Wenn der Schlusskurs einer fertigen Kerze das untere Band unterschreitet, öffnen oder kehren Sie in eine Long-Position um (es sei denn, die Flagge `Reverse` ist aktiviert, in diesem Fall wird ein Short eröffnet).
4. Wenn der Schlusskurs das obere Band überschreitet, öffnen Sie oder gehen Sie eine Short-Position ein (oder eine Long-Position, wenn `Reverse` wahr ist).
5. Erzwingen Sie einen Mindestpreisabstand zwischen aufeinanderfolgenden Einträgen, um einen erneuten Einstieg zu nahe an der vorherigen Füllung zu vermeiden.
6. Wenn der variable Gewinn über die offene Position das festgelegte Geldziel erreicht, glätten Sie alles und warten Sie auf das nächste Signal.
7. Nach jedem abgeschlossenen Trade wird das nächste Basisvolumen entweder auf die ursprüngliche Größe zurückgesetzt (nach einem profitablen Trade) oder mit dem Martingal-Verhältnis multipliziert (nach einem verlustbringenden Trade). Das resultierende Volumen wird an den Instrumentenschritt angepasst, und es werden sowohl Obergrenzen pro Trade als auch Gesamtvolumen angewendet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Candle Type` | Datentyp der zu handelnden Kerzen. |
| `CMO Period` | Effizienzverhältnisfenster für den adaptiven gleitenden Durchschnitt. |
| `EMA Period` | Glättungszeitraum des adaptiven gleitenden Durchschnitts. |
| `ATR Period` | Anzahl der Balken für die Kanalhalbbreite ATR. |
| `Profit Target` | Geldgewinnschwelle, die einen vollständigen Ausstieg auslöst. |
| `Increase Ratio` | Der Multiplikator wird auf das nächste Handelsvolumen nach einem Verlusthandel angewendet. |
| `Max Position Volume` | Harte Obergrenze für ein einzelnes Order-/Positionsvolumen. |
| `Max Total Volume` | Obergrenze für das durch die Strategie eröffnete Gesamtengagement. |
| `Max Positions` | Maximale Anzahl gleichzeitiger Positionen (der Port behält eine Nettoposition). |
| `Minimum Step` | Mindestabstand zwischen zwei aufeinanderfolgenden Einträgen, gemessen in Punkten. |
| `Base Volume` | Ausgangsordergröße vor Martingal-Anpassungen. |
| `Reverse Signals` | Kehrt die Long/Short-Interpretation des Kanalausbruchs um. |

## Hinweise zur Implementierung
- StockSharp beinhaltet keine direkte VIDYA-Implementierung. Die Strategie nutzt `KaufmanAdaptiveMovingAverage` mit konfigurierbarer Effizienz und Glättungsfenstern, um das adaptive Verhalten von VIDYA nachzuahmen. Dadurch bleibt die Reaktionsfähigkeit nahe an der Originalanzeige, während auf eingebaute Komponenten zurückgegriffen wird.
- Es wird jeweils nur eine Nettoposition verwaltet. Die Version MetaTrader hat mehrere ausstehende Einträge in die Warteschlange gestellt. In StockSharp öffnet jedes Signal entweder eine neue Position oder kehrt die aktuelle um. Die Martingale-Skalierung wird auf die nächste Eintragsgröße angewendet, anstatt sofort neue Ebenen hinzuzufügen.
- Die Ausrichtung von Mindestschritt und Lautstärke hängt von den Instrumentenmetadaten ab (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`). Geben Sie diese Werte an, wenn Sie die Strategie für genaue Ausführungslimits konfigurieren.
- Die Gewinnverfolgung basiert auf der Strategie `PnL` und dem letzten Kerzenschluss, was für Backtests auf hohem Niveau ausreichend ist. Für den Live-Handel verbinden Sie die Strategie mit einem Portfolio, das die realisierten PnL-Werte aktualisiert.

## Dateien
- `CS/VidyaNBarsBordersMartingaleStrategy.cs` – C#-Implementierung der Strategie.
