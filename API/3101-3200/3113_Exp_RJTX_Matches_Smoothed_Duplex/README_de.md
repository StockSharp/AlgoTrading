# Exp RJTX Übereinstimmungen Geglättet Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie recreiert das Verhalten des MetaTrader 5-Expertenberaters `Exp_RJTX_Matches_Smoothed_Duplex.mq5`. Zwei unabhängige RJTX-Signalblöcke analysieren geglättete Eröffnungs- und Schlusskurse auf ihren jeweiligen Zeitrahmen. Jeder Block klassifiziert jede abgeschlossene Kerze als bullish oder bearish, je nachdem ob der geglättete Schluss über die geglättete Eröffnung von vor `Period` Bars steigt. Bullische "Matches" lösen Einstiege für das Long-Modul aus, während bearische Matches das Short-Modul verwalten.

## Signalerzeugung
1. **Glättung** – beide Blöcke speisen Kerzen-Eröffnungen und -Schlusskurse in den ausgewählten Glättungsalgorithmus ein. Dieselbe Methode wird auf Eröffnungs- und Schlussströme angewendet, aber separate Instanzen werden verwendet, um die internen Puffer unabhängig zu halten.
2. **Vergleich** – sobald genug Historie verfügbar ist, wird der aktuelle geglättete Schluss mit der geglätteten Eröffnung verglichen, die `Period` Bars früher aufgezeichnet wurde.
3. **Match-Erkennung** – wenn der Schluss größer ist, erhält die Kerze ein bullisches Match; andernfalls wird sie bearisch. Signale werden nach einer Verschiebung um `SignalBar` geschlossene Kerzen ausgewertet, genau wie der MT5-Pufferzugriff.

## Positionsmanagement
- Der **Long-Block** öffnet eine Long-Position (schließt dabei ggf. ein bestehendes Short) wenn ein bullisches Match das Auswertungsfenster erreicht. Ein bearisches Match schließt die Long-Position, wenn Long-Exits aktiviert sind.
- Der **Short-Block** spiegelt diese Logik: ein bearisches Match öffnet einen Short-Trade (schließt Long-Exposure wenn erlaubt) und ein bullisches Match schließt den Short.
- StockSharp-Strategien sind genettert. Daher schließen entgegengesetzte Module die aktuelle Position, bevor sie eine neue öffnen, anstatt wie die MT5-Version zwei unabhängige gehedgte Positionen aufrechtzuerhalten. Deaktivieren Sie den entsprechenden `Allow ... Close`-Parameter, um die automatische Abdeckung zu verbieten.

## Risikomanagement
Stops und Gewinnziele werden in Preisschritten ausgedrückt (`PriceStep × points`). Für jede abgeschlossene Kerze prüft die Strategie, ob der Barrange den aktiven Stop-Loss- oder Take-Profit-Level berührt und schließt die entsprechende Position sofort. Dies emuliert das Verhalten von MT5-Schutzorders ohne auf broker-verwaltete Orders zurückzugreifen.

## Parameter
| Abschnitt | Parameter | Standard | Beschreibung |
| --- | --- | --- | --- |
| Long | `LongCandleType` | H4 | Zeitrahmen des Long-RJTX-Blocks. |
| Long | `LongVolume` | 0.1 | Volumen bei Ausführung eines Long-Signals. |
| Long | `LongAllowOpen` | `true` | Long-Positionen öffnen erlauben. |
| Long | `LongAllowClose` | `true` | Long-Positionen bei bearischen Matches schließen erlauben. |
| Long | `LongStopLossPoints` | 1000 | Stop-Loss-Abstand für Long-Trades in Preisschritten (0 deaktiviert die Prüfung). |
| Long | `LongTakeProfitPoints` | 2000 | Take-Profit-Abstand für Long-Trades in Preisschritten (0 deaktiviert die Prüfung). |
| Long | `LongSignalBar` | 1 | Verschiebung beim Lesen von RJTX-Puffern (`0` = aktuelle geschlossene Kerze). |
| Long | `LongPeriod` | 10 | Anzahl Bars zwischen aktuellem geglättetem Schluss und historischer geglätteter Eröffnung. |
| Long | `LongMethod` | `Sma` | Glättungsalgorithmus für den Long-Block (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`). |
| Long | `LongLength` | 12 | Länge des Glättungsfilters für Eröffnungs-/Schlussreihen. |
| Long | `LongPhase` | 15 | Phasenparameter für Jurik-Stil-Filter (für Kompatibilität beibehalten). |
| Short | `ShortCandleType` | H4 | Zeitrahmen des Short-RJTX-Blocks. |
| Short | `ShortVolume` | 0.1 | Volumen bei Ausführung eines Short-Signals. |
| Short | `ShortAllowOpen` | `true` | Short-Positionen öffnen erlauben. |
| Short | `ShortAllowClose` | `true` | Short-Positionen bei bullischen Matches schließen erlauben. |
| Short | `ShortStopLossPoints` | 1000 | Stop-Loss-Abstand für Short-Trades in Preisschritten (0 deaktiviert die Prüfung). |
| Short | `ShortTakeProfitPoints` | 2000 | Take-Profit-Abstand für Short-Trades in Preisschritten (0 deaktiviert die Prüfung). |
| Short | `ShortSignalBar` | 1 | Verschiebung beim Lesen von RJTX-Puffern für den Short-Block. |
| Short | `ShortPeriod` | 10 | Anzahl Bars zwischen aktuellem geglättetem Schluss und historischer geglätteter Eröffnung. |
| Short | `ShortMethod` | `Sma` | Glättungsalgorithmus für den Short-Block. |
| Short | `ShortLength` | 12 | Länge des Glättungsfilters für Short-Signale. |
| Short | `ShortPhase` | 15 | Phasenparameter für Jurik-Stil-Filter im Short-Block. |

## Hinweise
- `Jjma` entspricht dem Jurik Moving Average. `Jurx`, `Parma` und `Vidya` werden mit Zero-Lag EMA, Arnaud Legoux MA bzw. EMA approximiert, da StockSharp keine identischen Filter aus der SmoothAlgorithms-Bibliothek bereitstellt.
- Die Stop-Loss/Take-Profit-Logik wird anhand der Kerzenextrema ausgewertet. Intrabar-Spikes kürzer als Hoch/Tief der Kerze lösen keine Exits aus.
- Signale werden nur bei abgeschlossenen Kerzen verarbeitet; Intrabar-Matches werden entsprechend dem MT5-`IsNewBar`-Verhalten ignoriert.
