# Kijun-Sen-Robot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Kijun-Sen-Robot-Strategie** ist eine direkte Konvertierung des MetaTrader 5 Expert Advisors "Kijun Sen Robot" in die StockSharp High-Level-Strategie-API. Sie arbeitet standardmäßig auf 30-Minuten-Kerzen und konzentriert sich auf Ichimoku Kijun-sen-Preiskreuzungen, die durch einen 20-Perioden-linear-gewichteten gleitenden Durchschnitt (LWMA) bestätigt werden. Die Strategie behält die ursprüngliche Idee des Experten bei, nur während der aktivsten Stunden zu handeln und Positionsschutz mit dynamischer Stop-, Breakeven- und Trailing-Logik durchzusetzen.

## Indikatoren und Daten
- **Ichimoku** mit Tenkan, Kijun und Senkou Span B konfiguriert auf 6/12/24 Perioden.
- **Linear-gewichteter gleitender Durchschnitt (LWMA)** über 20 Bars für Steigungsbestätigung und Distanzfilterung.
- **30-Minuten-Kerzen** (Standard) für die Signalerzeugung. Jeder andere Zeitrahmen kann über den Parameter `CandleType` ausgewählt werden.

## Handelslogik
### Long-Einstieg
1. Die Kerze handelt die Kijun-Linie von unten durch. Die Kerze muss entweder unterhalb der Linie öffnen, oberhalb schließen oder sie während der Bar berühren, während der vorherige Schlusskurs ebenfalls unterhalb lag.
2. Das aktuelle Kijun ist flach oder steigend im Vergleich zu zwei Bars zurück.
3. Die LWMA liegt mindestens `MaFilterPips` (in Preiseinheiten umgerechnet) unterhalb des Kijun-Niveaus und hält die Basislinie über dem gleitenden Durchschnitt.
4. Die LWMA-Steigung ist positiv (aktuelle LWMA größer als der vorherige Wert).
5. Die Handelszeit liegt innerhalb von `[TradingStartHour, TradingEndHour)`, Standard 07:00–19:00 Börsenzeitzone.

Wenn alle Bedingungen erfüllt sind und die Strategie nicht bereits netto-long ist, wird eine Markt-Kauforder gesendet (ein vorhandenes Short wird zuerst gedeckt). Der Einstiegspreis ist der Kerzenschlusskurs.

### Short-Einstieg
1. Die Kerze handelt die Kijun-Linie von oben durch (Spiegelung der Long-Logik).
2. Kijun ist flach oder fällt relativ zu zwei Bars zurück.
3. Die LWMA liegt mindestens `MaFilterPips` oberhalb des Kijun-Niveaus.
4. Die LWMA-Steigung ist negativ (aktuelle LWMA niedriger als der vorherige Wert).
5. Der Einstieg erfolgt nur innerhalb des erlaubten Handelsfensters.

Eine Markt-Verkaufsorder wird platziert (vorhandenes Long-Exposure wird geschlossen, bevor ein Short eröffnet wird).

### Positionsmanagement und Ausstiege
- **Initialer Stop-Loss** – `StopLossPips` unterhalb/oberhalb des Einstiegspreises platziert (in Preiseinheiten über den Preisschritt des Instruments umgerechnet). Dies reproduziert den Schutzstop der MQL-Version.
- **Breakeven-Bewegung** – sobald der nicht realisierte Gewinn `BreakEvenPips` überschreitet, wird der Stop auf den Einstiegspreis plus einen Pip (Long) oder minus einen Pip (Short) verschoben. Der Schwellenwert wird mit derselben Pip-Konvertierungslogik gemessen.
- **Trailing-Stop** – nachdem der Kurs um `TrailingStopPips` vorgerückt ist, folgt der Stop dem Kurs auf diesem Abstand, nur in der günstigen Richtung.
- **Fester Take-Profit** – optionales Ziel definiert durch `TakeProfitPips`. Auf null setzen zum Deaktivieren.
- **Kijun-Steigungsausstieg** – wenn die LWMA sich gegen den Trade dreht, bevor der Stop über den Breakeven hinausbewegt wurde, wird die Position sofort geschlossen, entsprechend dem Notausstieg des ursprünglichen Experten.
- **Zeitfilter** – neue Trades werden außerhalb des konfigurierten Fensters ignoriert, aber offene Trades werden weiterhin verwaltet, bis sie durch die oben genannten Regeln geschlossen werden.
- **Orderhandling** – die StockSharp-Strategie verwendet ausschließlich Marktorders; die komplexe Limit-vs-Markt-Einstiegslogik des ursprünglichen EA wird vereinfacht, da Kerzendaten statt Tick-Daten verwendet werden.

Wenn sowohl der Stop-Loss- als auch der Take-Profit-Level innerhalb derselben Bar verletzt werden würden, hat der Stop-Loss Vorrang, um ohne Intrabar-Information konservativ zu bleiben.

## Parameter
| Parameter | Standardwerte | Beschreibung |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Ichimoku Tenkan-sen-Länge. |
| `KijunPeriod` | 12 | Ichimoku Kijun-sen-Länge. |
| `SenkouSpanBPeriod` | 24 | Ichimoku Senkou Span B-Länge. |
| `LwmaPeriod` | 20 | Länge des LWMA-Bestätigungsfilters. |
| `MaFilterPips` | 6 | Minimaler LWMA-zu-Kijun-Abstand in Pips. |
| `StopLossPips` | 50 | Initialer Schutzstop-Abstand. |
| `BreakEvenPips` | 9 | Benötigter Gewinn, um den Stop auf Breakeven zu verschieben. |
| `TrailingStopPips` | 10 | Abstand für Trailing-Stop-Bewegung. |
| `TakeProfitPips` | 120 | Optionaler fester Take-Profit-Abstand. |
| `TradingStartHour` | 7 | Erste erlaubte Handelsstunde (inklusive). |
| `TradingEndHour` | 19 | Letzte erlaubte Handelsstunde (exklusive). |
| `CandleType` | 30-Minuten-Zeitrahmen | Für die Signalauswertung verwendeter Datentyp. |

Alle pip-basierten Parameter werden in Preiseinheiten unter Verwendung des Instrument-`PriceStep` übersetzt. Instrumente mit 3 oder 5 Dezimalstellen erhalten automatisch einen Faktor von 10, um die klassische FX-Pip-Größe zu replizieren.

## Implementierungshinweise
- Die Konvertierung hält die zustandsbehafteten Strategievariablen (Verhalten `longcross`, `shortcross`) über `_pendingLongLevel` und `_pendingShortLevel`, wodurch neue Positionen eine frische Kijun-Kreuzung erfordern.
- Intrabar-Prüfungen wie "letzter Bid/Ask" aus der MT5-Version werden mit Bedingungen auf Kerzenebene (`Open`, `Close`, `High`, `Low`) angenähert. Dies macht die Logik deterministisch für Backtesting in StockSharp.
- Der Positionsschutz verwendet `ClosePosition()` und manuelles Stop-Tracking anstelle von MT5-Ordermodifikationen. Die Breakeven- und Trailing-Anpassungen werden einmal pro abgeschlossener Kerze ausgeführt.
- Die Hilfsmethode `ConvertPips` führt die Pip-zu-Preis-Konvertierung mit `Security.PriceStep` oder `Security.MinPriceStep` durch und wendet einen 10×-Multiplikator für Tick-Größen mit 3 oder 5 Dezimalstellen an, um die MT5-`digits_adjust`-Regel zu emulieren.
- Da die Strategie an die High-Level-API gebunden ist, werden Indikatoren über `SubscribeCandles().BindEx(...)` gebunden, und Chart-Zeichnungen werden automatisch konfiguriert (Kerzen, Ichimoku, LWMA, eigene Trades).

## Verwendungsrichtlinien
1. Die Strategie an ein Wertpapier anhängen, das 30-Minuten-Kerzen unterstützt (oder einen anderen `CandleType` einstellen).
2. `Volume` auf der Strategieinstanz auf die gewünschte Ordergröße konfigurieren, bevor gestartet wird.
3. Optional pip-basierte Parameter anpassen, um die Instrumentvolatilität zu berücksichtigen oder optimierte Einstellungen für spezifische Währungspaare zu reproduzieren.
4. Im High-Level-Backtester oder in der Live-Umgebung ausführen; die Strategie wird dasselbe Handelsfenster, dieselben Stop- und Trailing-Regeln wie der ursprüngliche Experte durchsetzen.
5. Log oder Chart überwachen, um Breakeven- und Trailing-Updates zu sehen. Alle Kommentare im Code sind auf Englisch für Klarheit, wie angefordert.

Die Python-Version wird absichtlich weggelassen; nur die C#-Implementierung wird in diesem Ordner bereitgestellt.
