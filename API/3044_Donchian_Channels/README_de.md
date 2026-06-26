# Donchian-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den klassischen "Donchian Channels" Expert Advisor zur StockSharp High-Level-API. Sie kombiniert einen Multi-Timeframe Donchian-Ausbruch mit gewichteten gleitenden Durchschnitten, Momentum-Bestätigung, MACD-Trendfilterung und umfangreichen Risikokontrollen (Stop-Loss, Take-Profit, Break-Even, Trailing Stop und eigenkapitalbasierter Notfallausstieg).

## Logik-Überblick

- **Marktregime:**
  - Der Donchian-Kanal wird auf einem höheren Zeitrahmen (Standard: 4 Stunden) berechnet, um die vorherrschende Ausbruchsstruktur zu erkennen.
  - Ein MACD, der auf einem konfigurierbaren Trend-Zeitrahmen (standardmäßig täglich) berechnet wird, stellt sicher, dass der höhere Zeitrahmen-Trend mit der Trade-Richtung übereinstimmt.
- **Einstiegsbedingungen:**
  - **Long-Setup:**
    - Das untere Donchian-Band oder die Kanalmedianlinie durchdringt den Kerzenkörper der vorherigen Kerze des höheren Zeitrahmens von unten und signalisiert einen potenziellen Ausbruch.
    - Die letzten beiden Kerzen des Basis-Zeitrahmens bilden einen aufwärts gerichteten Swing (`Low[2] < High[1]`).
    - Die absolute Abweichung des Momentums von 100 auf dem höheren Zeitrahmen überschreitet den Kaufschwellenwert in einer der letzten drei Ablesungen.
    - Die schnelle LWMA bleibt innerhalb der konfigurierten Distanz über der langsamen LWMA, um überdehnte Bewegungen zu vermeiden.
    - Die MACD-Hauptlinie liegt über ihrem Signal (beide positiv oder beide negativ) und bestätigt den bullischen Bias.
  - **Short-Setup:** Symmetrische Regeln gespiegelt für das obere Donchian-Band, Swing-Struktur, bärische Momentum-Abweichung und MACD-Bestätigung.
  - Mehrfache Einstiege (Pyramidisierung) sind bis zur konfigurierten maximalen Trade-Anzahl erlaubt.
- **Ausstiegsbedingungen:**
  - Fester Stop-Loss und Take-Profit in Kursschritten definiert.
  - Optionaler Wechsel zu Break-Even, sobald der Kurs eine konfigurierbare Distanz über den Einstieg hinaus fortschreitet.
  - Trailing Stop, der entweder jüngsten Kerzenextremen folgen kann (mit Polsterung) oder den Kurs mit einem klassischen Trigger/Schritt-Ansatz trailt.
  - Der Equity-Stop überwacht den P&L-Drawdown der Strategie und erzwingt ein Schließen, wenn Verluste das erlaubte Risikobudget überschreiten.

## Parameter

| Gruppe | Name | Beschreibung |
| ------ | ---- | ------------ |
| General | Base Candle | Ausführungs-Zeitrahmen für Einstiege und Risikoprüfungen. |
| General | Donchian Candle | Höherer Zeitrahmen für Donchian-Kanal und Momentum-Filter. |
| General | Trend Candle | Zeitrahmen des MACD-Trendfilters. |
| General | Volume | Ordergröße für jeden Einstieg. |
| Indicators | Donchian Length | Lookback-Periode für den Donchian-Kanal. |
| Indicators | Fast MA / Slow MA | Längen der gewichteten gleitenden Durchschnitte im Handelszeitrahmen. |
| Indicators | MA Distance | Maximal erlaubte Distanz zwischen schneller und langsamer LWMA (in Kursschritten). |
| Indicators | Momentum Period | Lookback für den Momentum-Filter im höheren Zeitrahmen. |
| Filter | Momentum Buy / Sell | Mindest-Absolutabweichung von 100 für bullisches/bärisches Momentum. |
| Risk | Stop Loss / Take Profit | Harte Ausstiege in Kursschritten vom Einstiegspreis. |
| Risk | Use Trailing | Aktiviert Trailing-Stop-Management. |
| Risk | Trailing Trigger / Step | Klassische Trailing-Parameter bei deaktiviertem kerzenbasiertem Trailing. |
| Risk | Candle Trail / Trail Candles | Schaltet kerzenbasiertes Trailing um und legt die Anzahl der verwendeten Kerzen fest. |
| Risk | Trailing Padding | Zusätzlicher Puffer um Kerzenextreme. |
| Risk | Use BreakEven | Aktiviert den Wechsel zu Break-Even. |
| Risk | BreakEven Trigger / Offset | Distanz und Offset beim Verschieben des Stops auf Break-Even. |
| Risk | Use Equity Stop | Aktiviert den drawdown-basierten Notfallausstieg. |
| Risk | Equity Risk | Maximal erlaubter Drawdown vor Schließen der Position. |
| Risk | Max Trades | Maximale Anzahl gleichzeitiger Pyramid-Einstiege. |

## Verwendungstipps

1. **Zeitrahmen:** Den Basiszeitrahmen auf den eigenen Ausführungsstil abstimmen (z.B. 1h/4h) und die Donchian/MACD-Zeitrahmen höher halten, um die Multi-Timeframe-Bestätigungslogik aufrechtzuerhalten.
2. **Momentum-Schwellenwerte:** Der ursprüngliche EA maß Momentum-Abweichungen um 100. Mit kleinen Schwellenwerten (0.3) beginnen und erhöhen, um schwächere Bewegungen auf unruhigen Märkten herauszufiltern.
3. **Risikoeinstellungen:** Pip-basierte Distanzen aus der MQL-Version in instrumentenspezifische Kursschritte umrechnen. Den `Step`-Wert des Wertpapiers beim Konfigurieren von Stops und Trailing-Logik immer überprüfen.
4. **Pyramidisierung:** `Max Trades` auf 1 reduzieren, wenn Einzelpositionsmanagement bevorzugt wird. Schrittweise erhöhen beim Testen des Pyramid-Verhaltens.
5. **Equity-Stop:** Der Equity-Stop überwacht den Strategie-P&L innerhalb von StockSharp. `Equity Risk` anpassen, um den maximalen Drawdown (in Kontowährung) widerzuspiegeln, den man zu tolerieren bereit ist.

## Backtesting

- Funktioniert direkt innerhalb von StockSharp Designer/Backtester nur mit Kerzen-Abonnements (keine Tick-Level-Daten erforderlich).
- Sicherstellen, dass alle ausgewählten Zeitrahmen vom Datenanbieter verfügbar sind, bevor ein Backtest oder eine Live-Session gestartet wird.
- Bei der Optimierung Donchian-Länge, MA-Distanz und Momentum-Schwellenwerte priorisieren — sie haben den stärksten Einfluss auf Gewinnrate und Trade-Häufigkeit.
