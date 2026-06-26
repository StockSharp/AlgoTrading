# Exp XBullsBearsEyes Vol Direkt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Umsetzung des MetaTrader-Experten **Exp_XBullsBearsEyes_Vol_Direct**. Sie rekonstruiert den benutzerdefinierten
Oszillator aus Bulls Power und Bears Power, multipliziert ihn mit einer konfigurierbaren Volumenquelle und wendet die gleiche adaptive
Glättung wie der Original-Indikator an. Handelsentscheidungen werden ausschließlich durch den Richtungspuffer des Indikators gesteuert:
Der Algorithmus reagiert auf Momentum-Schwankungen statt auf Niveaukreuzungen und öffnet oder schließt Positionen, wenn das geglättete
Histogramm seine Steigung ändert.

Anders als viele Konvertierungen behält die StockSharp-Version die Volumengewichtungsstufe und den vierstufigen Gamma-Filter des
MQL-Codes bei. Der Oszillator wird zweimal mit derselben Methode der gleitenden Durchschnitte geglättet — einmal für das Histogramm
und einmal für den Volumenstrom —, sodass Signale erst erscheinen, wenn beide Komponenten vollständig gebildet sind. Die Strategie
verarbeitet nur abgeschlossene Kerzen und unterstützt Tick-Volumen oder tatsächlich gehandeltes Volumen, was sie über verschiedene
Märkte hinweg portierbar macht.

## Indikatorlogik
1. Berechnung von Bulls Power und Bears Power mit einem exponentiellen gleitenden Durchschnitt des Schlusskurses über `Period` Kerzen.
2. Anwendung des originalen vierstufigen Gamma-Filters (Parameter `Gamma`, `L0`–`L3`), um die beiden Kräfte in ein normalisiertes
   Histogramm zwischen -50 und +50 zu kombinieren.
3. Multiplikation des Histogramms mit der gewählten Volumenquelle (Tick-Anzahl oder gehandeltes Volumen).
4. Glättung des Histogramms und des Rohvolumens mit derselben gleitenden Durchschnittsfamilie (`Method`, `SmoothingLength`, `SmoothingPhase`).
5. Ableitung eines Richtungspuffers: Farbe `0`, wenn das geglättete Histogramm steigt, Farbe `1`, wenn es fällt. Dies imitiert den
   `ColorDirectBuffer` aus der MetaTrader-Implementierung.

Die oberen/unteren Schwellenpuffer des Indikators werden intern berechnet, aber nicht für Handelsfilter verwendet, was dem Verhalten
des Original-Experten entspricht, der nur auf Richtungswechsel reagierte.

## Handelsregeln
- **Shorts schließen**, wenn die Richtung der vorherigen Kerze bullisch war (`olderColor = 0`).
- **Longs öffnen**, wenn Long-Einträge erlaubt sind, einer bullischen Kerze eine bärische folgt (`currentColor = 1`), und die Strategie
  nicht bereits long ist.
- **Longs schließen**, wenn die Richtung der vorherigen Kerze bärisch war (`olderColor = 1`).
- **Shorts öffnen**, wenn Short-Einträge erlaubt sind, einer bärischen Kerze eine bullische folgt (`currentColor = 0`), und keine Long-Position
  aktiv ist.
- Positionsumkehrungen schließen zuerst die Gegenseite und senden dann eine Marktorder mit dem konfigurierten `OrderVolume`.

Signale werden mit einem konfigurierbaren Balkenversatz (`SignalBar`) ausgewertet. Der Standardwert von `1` emuliert den MQL-Experten,
der auf eine vollständig geschlossene Kerze wartete, bevor er auf die Richtungsänderung reagierte.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `CandleType` | Kerzentyp/Zeitrahmen, den die Strategie abonniert (Standard: 2-Stunden-Kerzen). |
| `Period` | Lookback-Periode für Bulls/Bears Power. |
| `Gamma` | Glättungsfaktor (0…1) des adaptiven Gamma-Filters. |
| `VolumeMode` | Volumenquelle: Tick-Anzahl oder gehandeltes Volumen. |
| `Method` | Gleitende-Durchschnitt-Familie zur Glättung von Histogramm und Volumen (SMA, EMA, SMMA, LWMA, Jurik; nicht unterstützte Legacy-Typen fallen auf SMA zurück). |
| `SmoothingLength` | Länge beider Glättungsstufen. |
| `SmoothingPhase` | Jurik-Phasenparameter (aus Kompatibilitätsgründen beibehalten). |
| `SignalBar` | Anzahl der Balken zurück, die beim Auswerten des Richtungspuffers gelesen werden. |
| `AllowBuyOpen` / `AllowSellOpen` | Öffnen von Long-/Short-Positionen aktivieren oder deaktivieren. |
| `AllowBuyClose` / `AllowSellClose` | Erzwungene Ausstiege bei gegensätzlichen Signalen aktivieren oder deaktivieren. |
| `OrderVolume` | Marktordergröße für neue Einstiege. |
| `StopLossPoints` | Optionaler Schutz-Stop in Preisschritten (0 deaktiviert den Stop). |
| `TakeProfitPoints` | Optionales Schutzziel in Preisschritten (0 deaktiviert das Ziel). |

## Verwendungshinweise
- Die Strategie arbeitet mit einem einzelnen Wertpapier, das von `GetWorkingSecurities()` zurückgegeben wird, und funktioniert am
  besten bei Symbolen mit einem stabilen Volumenstrom.
- Tick-Volumen wird für Spot-FX-Symbole empfohlen, bei denen kein tatsächlich gehandeltes Volumen verfügbar ist. Setzen Sie
  `VolumeMode` auf `Real` für Börsen, die ausgeführtes Volumen veröffentlichen.
- Stop-Loss- und Take-Profit-Abstände werden in Preisschritten angegeben und über den `PriceStep` des Wertpapiers in absolute
  Preiseinheiten umgerechnet.
- Da die Logik auf Richtungswechseln basiert, behalten aufeinanderfolgend gleiche Histogrammwerte die vorherige Richtung bei, bis
  eine neue Steigung erscheint — genau wie in der MetaTrader-Version.
- Die Chartausgabe zeigt standardmäßig nur Preiskerzen. Sie können benutzerdefinierte Plots für das Histogramm hinzufügen, wenn
  eine visuelle Bestätigung erforderlich ist.
