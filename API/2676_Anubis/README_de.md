# Anubis-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Anubis-Strategie kombiniert Multi-Timeframe-Volatilitäts- und Momentum-Filter, um Umkehrungen gegen starke Gegentrend-Spikes zu erfassen. Der ursprüngliche Expert Advisor von MetaTrader 5 verwendete H4-Indikatoren zur Einstiegsselektion und M15-Signale für das Timing. Diese Konvertierung behält dieselbe Struktur bei und passt die Logik an die High-Level-API von StockSharp an, mit umfangreicher Laufzeit-Telemetrie.

## Strategielogik
- **Zeitrahmen**
  - Haupt-Signal-Zeitrahmen: konfigurierbarer Kerzentyp (standardmäßig 15-Minuten-Kerzen).
  - Bestätigung im höheren Zeitrahmen: feste 4-Stunden-Kerzen für CCI und Standardabweichungen.
- **Indikatoren**
  - *Commodity Channel Index (CCI)* im höheren Zeitrahmen erkennt überkaufte/überverkaufte Extreme.
  - *Zwei Standardabweichungen* im höheren Zeitrahmen liefern Volatilitätsmessungen für die Take-Profit-Dimensionierung.
  - *MACD* im Signal-Zeitrahmen liefert Momentum-Kreuzungsbestätigung.
  - *Average True Range (ATR)* im Signal-Zeitrahmen definiert Ausstiege bei abnormaler Kerzenbreite.
- **Einstiegskriterien**
  - **Long:** CCI fällt unter `-CciThreshold`, MACD-Hauptlinie kreuzt die Signallinie nach oben, und das vorherige MACD-Histogramm war negativ.
  - **Short:** CCI steigt über `+CciThreshold`, MACD-Hauptlinie kreuzt die Signallinie nach unten, und das vorherige MACD-Histogramm war positiv.
  - Die Strategie schließt optional eine entgegengesetzte Position, bevor eine neue gestapelt wird, und erzwingt einen Mindestpreisabstand zwischen aufeinanderfolgenden Einstiegen.
- **Positionsverwaltung**
  - Bis zu `MaxLongPositions` oder `MaxShortPositions` gestapelte Einstiege sind erlaubt, jeder mit `TradeVolume` Kontrakten eröffnet.
  - Stop-Loss- und Take-Profit-Abstände werden aus Pip-basierten Einstellungen und der Volatilität des höheren Zeitrahmens abgeleitet.
  - Sobald sich der Preis um `BreakevenPips` bewegt, wird der Schutzstop auf den durchschnittlichen Einstiegspreis angehoben.
- **Ausstiegskriterien**
  - Harte Stops: Stop-Loss- und Take-Profit-Levels werden bei jeder geschlossenen Kerze überwacht.
  - Bereichsausstiege: Positionen schließen, wenn die vorherige Kerzenbreite `CloseAtrMultiplier × ATR` überschreitet.
  - Momentum-Ausstiege: Positionen mit ausreichendem Gewinn schließen, wenn MACD-Momentum gegen den Trade dreht und der Gewinn `ThresholdPips` übersteigt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | 1 | Ordergröße für jeden Einstieg. |
| `CciThreshold` | 80 | Absolutes CCI-Level auf dem 4-Stunden-Chart zur Erkennung von Extremen. |
| `CciPeriod` | 11 | CCI-Rückblicklänge im höheren Zeitrahmen. |
| `StopLossPips` | 100 | Stop-Loss-Abstand in Pips. Auf 0 setzen, um den anfänglichen Stop zu deaktivieren. |
| `BreakevenPips` | 65 | Gewinnabstand in Pips, bevor der Stop auf Breakeven bewegt wird. |
| `ThresholdPips` | 28 | Zusätzliches Gewinnpolster erforderlich, bevor MACD-basierte Ausstiege auslösen. |
| `TakeStdMultiplier` | 2.9 | Multiplikator für die langsame Standardabweichung bei der Berechnung des Take-Profit-Abstands. |
| `CloseAtrMultiplier` | 2 | Multiplikator des Signal-Zeitrahmen-ATR für bereichsbasierte Ausstiege. |
| `SpacingPips` | 20 | Mindestpreisabstand zwischen aufeinanderfolgenden Einstiegen in dieselbe Richtung. |
| `MaxLongPositions` | 2 | Maximale Anzahl simultaner Long-Einstiege. |
| `MaxShortPositions` | 2 | Maximale Anzahl simultaner Short-Einstiege. |
| `MacdFastLength` | 20 | Schnelle EMA-Länge für MACD im Signal-Zeitrahmen. |
| `MacdSlowLength` | 50 | Langsame EMA-Länge für MACD im Signal-Zeitrahmen. |
| `MacdSignalLength` | 2 | Signal-Glättungslänge für MACD. |
| `AtrLength` | 12 | ATR-Rückblickperiode im Signal-Zeitrahmen. |
| `StdFastLength` | 20 | Periode für die schnelle Standardabweichung (für Diagnosen). |
| `StdSlowLength` | 30 | Periode für die langsame Standardabweichung, die den Take-Profit-Abstand antreibt. |
| `CandleType` | 15m-Kerzen | Haupt-Zeitrahmen für MACD- und ATR-Berechnungen. |

## Trading-Hinweise
- Der höhere Zeitrahmen ist auf vier Stunden fixiert; passe `CandleType` an, wenn du den Haupt-Signal-Zeitrahmen mit verschiedenen Märkten synchronisieren möchtest.
- Da StockSharp Nettopositionen standardmäßig aggregiert, werden Long- und Short-Exposure nicht gleichzeitig gehalten; ein entgegengesetztes Signal wird die offene Position schließen, bevor die neue Order platziert wird.
- Die Standardabweichungsberechnung folgt der StockSharp-Implementierung. Die langsame Länge approximiert die EMA-basierte Abweichung aus der ursprünglichen MQL-Version.
- Stelle sicher, dass das ausgewählte Wertpapier einen gültigen `PriceStep` aufweist, damit Pip-basierte Parameter korrekt in Preisabstände übersetzt werden.
