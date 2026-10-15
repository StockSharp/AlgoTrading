# Eliot-Wave-Strategie (aus MQL4 "Eliot Wave I" portiert)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Eliot-Wave-Strategie** ist eine StockSharp-API-Portierung des ursprünglichen MetaTrader-4 Expert Advisors "Eliot Wave I". Das System kombiniert eine schnelle/langsame Kreuzung linear gewichteter gleitender Durchschnitte (LWMA) mit Multi-Timeframe-Momentum-Bestätigung und einem sehr langsamen MACD-Filter. Ziel ist es, impulsive Bewegungen in Richtung des vorherrschenden Trends zu erkennen und das Risiko zugleich durch integrierte Schutzregeln zu begrenzen.

## Kernindikatoren

- **Schnelle LWMA (Standard 6)** - verfolgt die kurzfristige Richtung anhand des typischen Preises `(High + Low + Close) / 3`.
- **Langsame LWMA (Standard 85)** - misst den breiteren Trend auf demselben Zeitrahmen.
- **Momentum (Standardperiode 14)** - wird auf einem höheren Zeitrahmen bewertet und in eine Abweichung relativ zum neutralen Niveau `100` umgewandelt. Ein Wert über dem konfigurierten Schwellenwert zeigt einen ausreichend starken Impuls.
- **MACD (12, 26, 9)** - wird auf einem sehr langsamen Zeitrahmen berechnet (standardmäßig monatlich) und als langfristiger Filter verwendet. Die Strategie kauft nur, wenn die MACD-Hauptlinie über der Signallinie liegt, und verkauft nur, wenn sie darunter liegt.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `Base Candle` | Primärer Zeitrahmen für die LWMA-Verarbeitung. | 15-Minuten-Kerzen |
| `Momentum Candle` | Höherer Zeitrahmen für die Momentum-Bestätigung. | 1-Stunden-Kerzen |
| `MACD Candle` | Sehr langsamer Zeitrahmen für den MACD-Trendfilter. | 30-Tage-Kerzen |
| `Fast LWMA` | Länge des schnellen linear gewichteten gleitenden Durchschnitts. | 6 |
| `Slow LWMA` | Länge des langsamen linear gewichteten gleitenden Durchschnitts. | 85 |
| `Momentum Period` | Rückblickperiode des Momentum-Indikators auf dem Bestätigungszeitrahmen. | 14 |
| `Momentum Buy Threshold` | Minimale Abweichung über 100, die zur Validierung eines Long-Setups erforderlich ist. | 0.3 |
| `Momentum Sell Threshold` | Minimale Abweichung über 100, die zur Validierung eines Short-Setups erforderlich ist. | 0.3 |
| `Stop Loss (pts)` | Schutz-Stop-Distanz in Instrumentenpunkten. | 20 |
| `Take Profit (pts)` | Zieldistanz in Instrumentenpunkten. | 50 |
| `Trade Volume` | Ordergröße für jeden Einstieg. | 1 Lot |
| `Max Position` | Zulässige absolute Nettoexposure; verhindert, dass die Strategie das `Max_Trades`-Limit des MQL-EA überschreitet. | 10 Lots |

Alle Parameter sind als `StrategyParam<T>` implementiert und können daher direkt in Designer oder Runner optimiert werden.

## Handelsregeln

1. **Trend- und Strukturfilter**
   - Die schnelle LWMA muss über der langsamen LWMA bleiben, damit Long-Trades betrachtet werden.
   - Die schnelle LWMA muss unter der langsamen LWMA bleiben, damit Shorts betrachtet werden.
   - Die letzten zwei abgeschlossenen Kerzen müssen sich überlappen (`Low[2] < High[1]` für Käufe, `Low[1] < High[2]` für Verkäufe), wodurch die Konsolidierungsanforderung des EA repliziert wird.
2. **Momentum-Bestätigung**
   - Das Momentum des höheren Zeitrahmens wird in `abs(momentum - 100)`-Werte transformiert.
   - Überschreitet einer der letzten drei Werte den konfigurierten Schwellenwert, gilt der Impuls als gültig.
3. **Makro-Trendfilter**
   - Kauftrades verlangen, dass die MACD-Hauptlinie auf dem langsamen Zeitrahmen über der Signallinie liegt.
   - Verkaufstrades verlangen, dass die MACD-Hauptlinie unter der Signallinie liegt.
4. **Orderausführung**
   - Wenn alle Bedingungen übereinstimmen, sendet die Strategie eine Marktorder, deren Größe die aktuelle Position umkehrt und das konfigurierte Handelsvolumen hinzufügt.
   - Positionswechsel werden unterstützt, damit das Verhalten der Averaging-Logik des ursprünglichen EA entspricht.

## Risikomanagement

- `StartProtection` wendet Stop-Loss- und Take-Profit-Distanzen in Instrumentenpunkten automatisch an.
- Zusätzliche Ausstiegslogik schließt Long-Positionen, wenn die schnelle LWMA unter die langsame LWMA fällt oder der MACD-Filter bärisch wird (und umgekehrt für Shorts). Dies spiegelt die MQL-Ausstiegsblöcke.
- Der Parameter `Max Position` verhindert, dass die Strategie Exposure über das konfigurierte Limit hinaus ansammelt, und respektiert damit die `Max_Trades`-Beschränkung des EA.

## Unterschiede zum ursprünglichen EA

- Grafische Trendlinienprüfungen und manuelle Trade-Benachrichtigungen wurden entfernt, da sie MetaTrader-spezifisch sind und in StockSharp kein Äquivalent haben.
- Break-even- und komplexe Trailing-Stop-Varianten aus dem MQL-Skript werden durch den einfacheren `StartProtection`-Mechanismus ersetzt. Benutzer können die Strategie erweitern, wenn diese Verhaltensweisen erforderlich sind.
- Geldbasierter Equity-Schutz ist nicht implementiert; Risiko wird über feste Stops und die Positionsobergrenze gesteuert.

## Nutzungshinweise

1. Binden Sie die Strategie an ein liquides Instrument und stellen Sie sicher, dass die drei Kerzenströme verfügbar sind.
2. Setzen Sie `Trade Volume`, Stop-/Zieldistanzen und Schwellenwerte entsprechend der Volatilität des gehandelten Marktes.
3. Optimieren Sie Schwellenwerte getrennt für bullische und bärische Impulse, wenn das Instrument asymmetrisches Verhalten zeigt.
4. Erwägen Sie, die integrierten Chart-Visualisierungen (Kerzen, LWMAs, Trade-Marker) für einfacheres Debugging zu aktivieren.

Diese Portierung konzentriert sich darauf, die Signallogik des ursprünglichen EA mit der High-Level-API von StockSharp zu reproduzieren, während die Implementierung idiomatisch und wartbar bleibt.
