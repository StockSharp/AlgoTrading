# BandOsMaCustom-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine direkte Portierung des MetaTrader 5-Expertenberaters unter
`MQL/45596/mql5/Experts/MQL5Book/p7/BandOsMACustom.mq5`. Der ursprüngliche Roboter
kombiniert das MACD-Histogramm (auch bekannt als OsMA) mit Bollinger-Bändern und a
gleitender Durchschnitt, der auf die Histogrammwerte anstelle der Rohpreise angewendet wird.
Immer wenn das Histogramm das untere Band durchbricht, eröffnet der Experte einen Long-Trade.
während Berührungen des oberen Bandes kurze Eingaben auslösen. Das Histogramm kreuzt a
Ein separater gleitender Durchschnitt schließt die Position. Ein Schutzstopp und a
Trailing-Stop-Schritte (entspricht einem Fünfzigstel des Stops) halten das Risiko unter Kontrolle.

Die StockSharp-Implementierung behält dieses Verhalten mithilfe der übergeordneten API bei.
So bleibt die Handelslogik innerhalb des Frameworks lesbar und debuggbar.

## Conversion-Highlights

* Das MACD-Histogramm wird implementiert durch
`MovingAverageConvergenceDivergenceHistogram`, gefüttert mit dem Kerzenpreis, der
entspricht dem vom `AppliedPrice` ausgewählten Modus MetaTrader `PRICE_*`
Parameter.
* Bollinger Bänder und der gleitende Ausstiegsdurchschnitt verarbeiten eher die OsMA-Ausgabe
als Preisdaten. Ein kompakter Verlaufspuffer reproduziert die MetaTrader `shift`
Argumente für beide Indikatoren.
* Die Strategie behält die ursprüngliche Long/Short-Signalisierung bei: Kreuzungen unterhalb der
Start-Longs des unteren Bandes, Kreuzungen über dem Start-Shorts des oberen Bandes und die
OsMA überschreitet seinen gleitenden Durchschnitt und schließt den Handel.
* `StartProtection` spiegelt den Stop-Loss-Plus-Trailing-Stop-Block MetaTrader wider.
Der abschließende Schritt wird als `StopLossPoints / 50` berechnet, genau wie der MQL
Klasse `TrailingStop` hat es getan.

## Indikatoren

| Indikator | Zweck |
| --- | --- |
| `MovingAverageConvergenceDivergenceHistogram` | Erstellt die `iOsMA`-Ausgabe von MetaTrader neu. |
| `BollingerBands` | Berechnet die oberen und unteren Schwellenwerte für das Histogramm. |
| Gleitender Durchschnitt (SMA/EMA/SMMA/LWMA) | Der Filter wird beendet, wenn das Histogramm ihn überschreitet. |

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 1-stündiger Zeitrahmen | Primärer Zeitrahmen, der für alle Indikatorberechnungen verwendet wird. |
| `FastOsmaPeriod` | 12 | Schnelle EMA-Länge aus der OsMA-Berechnung. |
| `SlowOsmaPeriod` | 26 | Langsame EMA-Länge aus der OsMA-Berechnung. |
| `SignalPeriod` | 9 | Signallänge SMA aus der OsMA-Berechnung. |
| `AppliedPrice` | Typisch | Angewandter Preis im MetaTrader-Stil, der das Histogramm speist. |
| `BandsPeriod` | 26 | Länge der Bollinger-Bänder, die auf den Histogrammwerten gezeichnet werden. |
| `BandsShift` | 0 | Rechtsverschiebung (in Balken), angewendet auf die Bollinger-Werte. |
| `BandsDeviation` | 2,0 | Standardabweichungsmultiplikator für die Bänder. |
| `MaPeriod` | 10 | Länge des im Histogramm berechneten gleitenden Ausgangsdurchschnitts. |
| `MaShift` | 0 | Rechtsverschiebung (in Balken), angewendet auf den gleitenden Ausgangsdurchschnitt. |
| `MaMethod` | Einfach | Methode des gleitenden Durchschnitts (SMA, EMA, SMMA, LWMA). |
| `StopLossPoints` | 1000 | Schutzstoppabstand, ausgedrückt in Preisschritten. |
| `OrderVolume` | 0,01 | Handelsvolumen, identisch mit der Eingabe MetaTrader „Lots“. |

## Handelsregeln

1. Abonnieren Sie die ausgewählte Kerzenserie und füttern Sie den gewählten Preis
in das MACD-Histogramm ein.
2. Übergeben Sie jeden Histogrammwert an die Bollinger-Bänder und den gleitenden Ausgangsdurchschnitt.
3. Erkennen Sie Signale mithilfe der verschobenen Puffer:
   * Wenn das Histogramm durch das untere Band fällt, setzen Sie ein bullisches Signal.
   * Wenn das Histogramm durch das obere Band steigt, setzen Sie ein bärisches Signal.
   * Wenn das Histogramm den gleitenden Ausgangsdurchschnitt kreuzt, löschen Sie den aktiven Wert
Signal, das das Schließen der Position ermöglicht.
4. Positionen verwalten:
   * Schließen Sie bestehende Long-Positionen, wenn das bullische Signal verschwindet; enge Shorts
wenn das bärische Signal verschwindet.
   * Eröffnen Sie eine Long-Position, wenn das bullische Signal aktiv ist und es keine Eröffnung gibt
Position; Eröffnen Sie einen Short, wenn das rückläufige Signal aktiv ist und die Position aktiv ist
flach.
5. Wenden Sie `StartProtection` mit der konfigurierten Stop-Loss-Distanz und einem Trailing an
Schritt entspricht `StopLossPoints / 50` Preisschritten.

## Notizen

* Alle Kommentare im Quellcode sind auf Englisch, um dem Repository zu entsprechen
Richtlinien.
* Die Verlaufspuffer garantieren, dass die Version StockSharp MetaTrader berücksichtigt
Parameter `BandsShift` und `MaShift`, ohne Indikatorwerte anzufordern
Index.
* Die Strategie entspricht den allgemeinen API-Konventionen: `SubscribeCandles`
fördert die Aktualisierung von Indikatoren und direkte Aufrufe zur Nachahmung von `BuyMarket`/`SellMarket`
die Auftragserteilung des ursprünglichen Sachverständigen.
