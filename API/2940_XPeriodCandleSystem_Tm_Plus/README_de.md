# XPeriod Candle System TM Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters `Exp_XPeriodCandleSystem_Tm_Plus`. Der ursprüngliche Roboter basiert auf dem benutzerdefinierten Indikator *XPeriod Candle System*, der Kerzendaten glättet und Balken entsprechend Bollinger-Band-Ausbrüchen einfärbt. Die übersetzte Version reproduziert dieses Verhalten durch Anwendung exponentieller Glättung auf die OHLC-Reihe, Mapping derselben angewandten Preismodi und Steuerung von Trades aus den resultierenden Farbzuständen. Ein zeitbasierter Ausstieg und konfigurierbare Schutzorders ergänzen die Ausbruchslogik.

## Handelslogik

1. **Geglättete Kerzen** – Exponentielle gleitende Durchschnitte mit konfigurierbarer Länge erstellen synthetische Eröffnungs-, Hoch-, Tief- und Schlusskurswerte, die den Quellindikator annähern.
2. **Angewandter Preis** – Der Benutzer kann eine der zwölf Preisformeln (Schluss, Eröffnung, Median, Trendfolge-Variationen, Demark usw.) auswählen, bevor Daten in die Bollinger Bänder gespeist werden.
3. **Bandanalyse** – Ein Bollinger-Bänder-Indikator (Länge und Abweichung konfigurierbar) verarbeitet die geglättete Preisreihe. Abgeschlossene Bänder sind erforderlich, bevor Signale ausgewertet werden.
4. **Farbzustände** –
   - Bullischer Balken oberhalb des oberen Bandes → Farbe `0` (Ausbruch nach oben).
   - Bärischer Balken unterhalb des unteren Bandes → Farbe `4` (Ausbruch nach unten).
   - Andere bullische Balken → Farbe `1`; andere bärische Balken → Farbe `3`.
   - Ein konfigurierbarer Ausbruchs-Offset (bei Möglichkeit in Preiseinheiten unter Verwendung der Tick-Größe des Symbols umgerechnet) vermeidet Fehlauslöser.
5. **Einstiege** – Die Strategie betrachtet die durch `SignalBar` definierte Kerze und ihren Vorgänger:
   - Long eröffnen, wenn der vorherige Balken ein bullischer Ausbruch (`0`) war und der Signalbalken es nicht ist.
   - Short eröffnen, wenn der vorherige Balken ein bärischer Ausbruch (`4`) war und der Signalbalken es nicht ist.
6. **Ausstiege** –
   - Longs schließen, wenn der Referenzbalken bärisch ist (`> 2`).
   - Shorts schließen, wenn der Referenzbalken bullisch ist (`< 2`).
   - Ein optionaler Haltetimer (`TimeTrade` und `HoldingMinutes`) schließt Positionen nach den angegebenen Minuten.
7. **Risiko** – `StartProtection` setzt optionale absolute Take-Profit- und Stop-Loss-Abstände für jeden Trade ein.

## Parameter

| Parameter | Beschreibung | Standardwert |
|-----------|--------------|--------------|
| `OrderVolume` | Basisauftragsgröße für Markteinstiege. | 0.1 |
| `BuyPosOpen` / `SellPosOpen` | Long- oder Short-Einstiege aktivieren/deaktivieren. | `true` |
| `BuyPosClose` / `SellPosClose` | Long- oder Short-Positionsausstiege erlauben. | `true` |
| `TimeTrade` | Aktiviert den zeitbasierten Ausstiegsfilter. | `true` |
| `HoldingMinutes` | Maximale Haltezeit, bevor der Zeitfilter eine Position schließt. | 960 |
| `CandleType` | Kerzendatentyp (Zeitrahmen), der vom Markt angefordert wird. | 4 Stunden |
| `Period` | Länge der exponentiellen gleitenden Durchschnitte zur Glättung. | 5 |
| `BollingerLength` | Anzahl geglätteter Balken im Bollinger-Berechnungsfenster. | 20 |
| `BandsDeviation` | Bandbreiten-Multiplikator. | 1.001 |
| `AppliedPriceMode` | Preistransformation vor dem Bollinger-Indikator (Schluss, Eröffnung, Median, Trendfolge, Demark usw.). | Close |
| `SignalBar` | Index des Balkens für die Signalauswertung (1 = letzter geschlossener Balken). | 1 |
| `StopLoss` / `TakeProfit` | Absolute Abstände (in Preiseinheiten) für die Schutz-Engine. | 1000 / 2000 |
| `Deviation` | Zusätzlicher Ausbruchs-Offset, der über/unter die Bollinger Bänder addiert wird. | 10 |

## Verwendungshinweise

- Der Glättungsschritt verwendet exponentielle gleitende Durchschnitte zur Replikation der proprietären XPeriod-Berechnung. Kleinere Perioden halten die synthetischen Kerzen näher an den Marktpreisen, während größere Perioden die Trendstruktur betonen.
- `SignalBar` muss innerhalb des gespeicherten Verlaufs bleiben (bis zu 14 Positionen nach dem aktuellen Balken). Werte größer als der verfügbare Verlauf überspringen das Trading automatisch.
- Der Ausbruchs-Offset wird mit `PriceStep` multipliziert, wenn das Wertpapier eine Tick-Größe offenlegt. Dies hält das Verhalten ähnlich zur MetaTrader-Version, wo `Deviation` in Punkten definiert ist.
- `StopLoss` und `TakeProfit` werden in absoluten Preiseinheiten angegeben. Auf null setzen, um Schutzorders zu deaktivieren, während die Verwaltungsinfrastruktur aktiv bleibt.
- Noch keine Python-Übersetzung vorhanden; dieser Ordner enthält nur die C#-Implementierung.
