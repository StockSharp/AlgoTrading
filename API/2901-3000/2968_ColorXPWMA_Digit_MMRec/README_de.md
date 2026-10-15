# Color XPWMA Digit MMRec Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Color XPWMA Digit MMRec Strategie** repliziert den MetaTrader-Expert-Advisor `Exp_ColorXPWMA_Digit_MMRec`. Sie verwendet den ColorXPWMA Digit-Indikator zur Identifizierung von Trend-Inflektionspunkten und umhüllt die ursprüngliche Money-Management-Zählerlogik. Der Indikator erstellt einen potenzgewichteten gleitenden Durchschnitt (PWMA), der optional durch eine ausgewählte gleitende Durchschnittsmethode geglättet wird. Die Steigung der geglätteten Linie wird in diskrete Farben umgewandelt: `2` für aufsteigende Steigung, `0` für absteigende Steigung und `1` wenn die Richtung flach ist.

Handelsentscheidungen werden getroffen, nachdem die Indikatorfarben auf einem konfigurierbaren historischen Balken (`SignalBar`) bewertet wurden. Wenn die vorherige Farbe (`SignalBar + 1`) bullisch (2) war, aber der Balken bei `SignalBar` die bullische Farbe nicht mehr beibehält, schließt die Strategie Short-Positionen und öffnet optional eine neue Long-Position. Die inverse Logik wird angewendet, wenn die historische Farbe bärisch (0) war, aber der neuere Balken diese bärische Farbe nicht mehr beibehält.

## Indikatorlogik
- **Potenzgewichteter gleitender Durchschnitt** – jeder Balken erhält ein Gewicht `(period - index)^power`. Höhere Potenzen betonen die neuesten Stichproben.
- **Glättung** – die gewichtete Serie wird durch einen glättenden gleitenden Durchschnitt geleitet. Unterstützte Methoden umfassen SMA, EMA, SMMA, LWMA, Jurik, T3 und Kaufman AMA. JurX-, Parabolisch- und VIDYA-Optionen werden mit exponentieller Glättung approximiert, da StockSharp keine direkten Implementierungen bereitstellt.
- **Farbkodierung** – das Vorzeichen der geglätteten Steigung definiert den Farb-Puffer, der Einstiege und Ausstiege auslöst.
- **Zifferrundung** – der Endwert kann auf eine feste Anzahl von Stellen gerundet werden, um das ursprüngliche "Digit"-Verhalten zu entsprechen.

## Handelsregeln
1. **Bullisches Fortsetzungsversagen**
   - Bedingung: Farbe bei `SignalBar + 1` gleich `2` (bullisch) und Farbe bei `SignalBar` ungleich `2`.
   - Aktion: Aktive Shorts schließen; wenn Long-Einstiege erlaubt sind, neue Long-Position öffnen, dimensioniert durch den Money-Management-Zähler.
2. **Bärisches Fortsetzungsversagen**
   - Bedingung: Farbe bei `SignalBar + 1` gleich `0` (bärisch) und Farbe bei `SignalBar` ungleich `0`.
   - Aktion: Aktive Longs schließen; wenn Short-Einstiege erlaubt sind, neue Short-Position öffnen, dimensioniert durch den Zähler.

Orders werden immer beim Schlusskurs der Kerze ausgeführt, die das Signal erzeugte. Beim Richtungswechsel schließt die Strategie das entgegengesetzte Engagement und öffnet sofort die neue Position in einer einzigen Marktorder.

## Money-Management-Zähler
Die Strategie führt eine fortlaufende Historie der geschlossenen Trade-Ergebnisse für Longs und Shorts. Bevor ein neuer Trade geöffnet wird, überprüft sie die jüngsten Ergebnisse von `BuyTotalTrigger` oder `SellTotalTrigger`:

- Wenn die Anzahl der Verlust-Trades in diesem Fenster den jeweiligen Verlust-Trigger (`BuyLossTrigger` oder `SellLossTrigger`) erreicht, wird die Positionsgröße auf `ReducedVolume` reduziert.
- Andernfalls wird das Standard-`NormalVolume` verwendet.

Dies reproduziert das Verhalten der ursprünglichen Routinen `BuyTradeMMRecounterS` und `SellTradeMMRecounterS`.

## Parameter
| Gruppe | Parameter | Beschreibung |
| --- | --- | --- |
| Allgemein | `CandleType` | Zeitrahmen für Indikatorberechnungen und Handelsentscheidungen. |
| Indikator | `IndicatorPeriod` | Periode des potenzgewichteten gleitenden Durchschnitts. |
| Indikator | `IndicatorPower` | Exponent für die Gewichte. Höhere Werte betonen die neuesten Balken. |
| Indikator | `SmoothingMethod` | Glättungs-MA-Methode. JurX, ParMa und Vidya verwenden exponentiellen Rückfall. |
| Indikator | `SmoothingLength` | Länge des glättenden gleitenden Durchschnitts. |
| Indikator | `SmoothingPhase` | Phasenparameter für unterstützende Glätter. |
| Indikator | `AppliedPrices` | Quellpreis für den Indikator (Schluss, Eröffnung, Hoch, Tief usw.). |
| Indikator | `RoundingDigits` | Anzahl der Dezimalstellen zur Rundung der Indikatorausgabe. |
| Logik | `SignalBar` | Historischer Versatz (in Balken) beim Lesen des Farb-Puffers. |
| Berechtigungen | `EnableBuyEntries` / `EnableSellEntries` | Long-/Short-Positionen öffnen erlauben. |
| Berechtigungen | `EnableBuyExits` / `EnableSellExits` | Longs/Shorts schließen erlauben. |
| Money Management | `NormalVolume` | Standard-Ordergröße. |
| Money Management | `ReducedVolume` | Ordergröße nach einer Verlustserie. |
| Money Management | `BuyTotalTrigger`, `BuyLossTrigger` | Anzahl der jüngsten Long-Trades und Verlustschwelle für den Wechsel zum reduzierten Volumen. |
| Money Management | `SellTotalTrigger`, `SellLossTrigger` | Gleiche Logik für Short-Trades. |
| Risikomanagement | `StopLossPoints`, `TakeProfitPoints` | Optionale Schutzabstände (Punkte), über `StartProtection` angewendet wenn nicht null. |

## Praktische Hinweise
- Halten Sie `SignalBar = 1`, um das Standard-Expert-Advisor-Verhalten zu imitieren und sicherzustellen, dass Signale auf vollständig abgeschlossenen Kerzen bewertet werden.
- Die Strategie speichert nur die neuesten Ergebnisse, die für den Zähler benötigt werden, um unkontrolliertes Speicherwachstum zu verhindern.
- Da StockSharp Orders asynchron ausführt, geht die Strategie davon aus, dass Fills zum Schlusskurs der Kerze erfolgen, wenn Verlust-Zähler aktualisiert werden. Dies spiegelt wider, wie der ursprüngliche MQL-Experte mit historischen Daten arbeitete.
- JurX-, ParMa- und Vidya-Glättungsoptionen sind Approximationen, die exponentiell glätten. Wenn Sie die ursprünglichen proprietären Filter benötigen, implementieren Sie benutzerdefinierte Indikatorklassen und stecken Sie diese in die Strategie.
