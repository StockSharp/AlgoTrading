# SVOS EURJPY D1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader 4 Expert Advisors **SVOS_EURJPY_D1**. Es arbeitet mit täglichen Kerzen für EURJPY und
kombiniert einen Regimeklassifikator mit Mustererkennung und Indikatorfiltern. Der Vertikal-Horizontalfilter (VHF) unterscheidet
zwischen trendigen und schwankenden Marktzuständen. Wenn der Markt im Trend liegt, basiert die Strategie auf der Steigung des MACD-Histogramms (OSMA).
während es unter Entfernungsbedingungen auf den Stochastic-Oszillator zurückfällt. Candlestick-Muster wie Engulfing Bars und
Morgen-/Abendsterne werden verwendet, um Positionen aggressiv gegen ungünstige Preisbewegungen zu schließen.

## Handelslogik
- **Regimeerkennung** – der VHF-Wert des Vortages wird mit `VhfThreshold` verglichen. Werte über dem Schwellenwert aktivieren das
Trendfolgeblock, andernfalls wird der Bereichsblock verwendet.
- **Trendbestätigung** – zwei EMAs (5 und 20 Perioden) werden mit einem langsamen EMA (130 Perioden, passend zum Sechsmonatsfilter von) verglichen
das Original EA), um Positionsgrößen zu skalieren. Bei Aufwärtstrends wird das Kaufvolumen mit `RiskBoost` multipliziert; in Abwärtstrends beträgt das Verkaufsvolumen
multipliziert.
- **Indikatorfilter**:
  - Trendregime: Long gehen, wenn OSMA positiv ist und steigt (`OSMA[1] > 0` und `OSMA[1] > OSMA[2]`). Gehen Sie short, wenn der OSMA negativ ist
und fallen.
  - Bereichsregime: Gehen Sie long, wenn die Stochastic-Hauptlinie ihr Signal übersteigt, gehen Sie short, wenn sie darunter kreuzt.
  - Volatilitätsschutz: Die vorherige Standardabweichung muss `StdDevMinimum` überschreiten, bevor ein Signal akzeptiert wird.
- **Preisaktionsfilter** – die zuletzt abgeschlossene Kerze darf kein Doji bilden (Verhältnis `DojiDivisor`) und muss dies bestätigen
Richtung (bullisch für Long-Positionen, bärisch für Short-Positionen). Entgegengesetzte Verschlingungs- oder Sternmuster lösen eine sofortige Liquidation aus
jeweilige Seite.
- **Positionslimits** – die Gesamtzahl der offenen Aufträge ist in Trendmärkten auf `MaxTrendOrders` und in Trendmärkten auf `MaxRangeOrders` begrenzt.
in vielfältigen Märkten.
- **Risikomanagement** – jede Order hat feste Stop-Loss- und Take-Profit-Level (`StopLossPips`, `TakeProfitPips`). Ein Nachlauf
Stop wird aktiviert, wenn der variable Gewinn `TrailingStopPips` überschreitet; Es wird unter Verwendung der Kerzenextreme neu berechnet, um das nachzuahmen
MetaTrader Verhalten.

## Verwendung des Indikators
- **Exponentieller gleitender Durchschnitt (5, 20, 130)** – wird zur Richtungsbestätigung und Volumenskalierung verwendet.
- **Vertikaler horizontaler Filter** – benutzerdefinierter Indikator, der das Verhältnis zwischen Nettobewegung und kumulativem Schlusskurs misst
Änderungen, um Trends im Vergleich zu Bereichen zu erkennen.
- **MACD (OSMA)** – der Unterschied zwischen MACD und seiner Signallinie steuert trendige Ein- und Ausstiege.
- **Stochastic Oszillator** – %K- und %D-Werte liefern Mean-Reversion-Signale für verschiedene Märkte.
- **Standardabweichung** – stellt sicher, dass die Volatilität hoch genug ist, bevor neue Trades zugelassen werden.

## Auftragsverwaltung
- Aufträge werden mit `BuyMarket`/`SellMarket` ausgeführt und intern gespeichert, sodass einzelne Stopps und Ziele simuliert werden können
Die Netting-Umgebung von StockSharp.
- Wenn Stop-Loss- oder Take-Profit-Level innerhalb der Kerzenspanne erreicht werden, wird der entsprechende Teil der Position geschlossen.
- Der Trailing Stop folgt dem Kerzenhoch (für Longs) oder dem Tief (für Shorts) und behält dabei den konfigurierten Abstand bei.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `LotSize` | Basisauftragsgröße, ausgedrückt in Losen. | `0.1` |
| `RiskBoost` | Auf die Losgröße angewendeter Multiplikator, wenn der Trendfilter EMA ausgerichtet ist. | `3` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `350` |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | `90` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips (immer aktiv). | `150` |
| `StochKPeriod` | %K Länge des Stochastic-Oszillators. | `8` |
| `StochDPeriod` | %D Länge des Stochastic-Oszillators. | `3` |
| `StochSlowing` | Auf %K angewendeter Glättungsfaktor. | `3` |
| `StdDevPeriod` | Lookback-Fenster für den Standardabweichungsfilter. | `20` |
| `StdDevMinimum` | Minimale Standardabweichung erforderlich, bevor neue Geschäfte eröffnet werden können. | `0.3` |
| `VhfPeriod` | Länge des vertikalen horizontalen Filters. | `20` |
| `VhfThreshold` | Regimeschwelle; Höhere Werte kennzeichnen Trendmärkte. | `0.4` |
| `MaxTrendOrders` | Maximale Anzahl gleichzeitig offener Aufträge während Trends. | `4` |
| `MaxRangeOrders` | Maximale Anzahl gleichzeitig offener Orders während Ranges. | `2` |
| `MacdFastLength` | Schnelle Länge von EMA innerhalb von MACD. | `10` |
| `MacdSlowLength` | Langsame Länge von EMA innerhalb von MACD. | `25` |
| `MacdSignalLength` | Signallänge EMA für MACD. | `5` |
| `DojiDivisor` | Verhältnis zur Kennzeichnung von Doji-Kerzen (Körper kleiner als Bereich/Divisor). | `8.5` |
| `CandleType` | Für die Analyse verwendeter Kerzentyp (standardmäßig täglich). | `1 day` |
| `PipSizeOverride` | Optionale Überschreibung der Pip-Größe; `0` ermöglicht die automatische Erkennung von `Security.PriceStep`. | `0` |

## Hinweise zur Implementierung
- Der ursprüngliche EA bezog sich auf einen sechsmonatigen EMA aus einem monatlichen Zeitrahmen. Der Hafen berechnet einen 130-Perioden-EMA bei täglichen Schließungen von
Reproduzieren Sie die gleiche Glättung und behalten Sie dabei ein einziges Datenabonnement bei.
- Stops, Ziele und Trailing-Logik werden innerhalb der Strategie reproduziert, da StockSharp standardmäßig Nettopositionen ermittelt. Jeder Eintrag ist
werden einzeln verfolgt, um das Verhalten von MetaTrader zu berücksichtigen.
- Trailing-Stop-Updates nutzen Kerzenhochs/-tiefs, um Intraday-Preisbewegungen zu approximieren. Die Ergebnisse können geringfügig von denen auf Zeckenbasis abweichen
Nachlaufen in MetaTrader, wenn große Intraday-Umkehrungen auftreten.
- Die Pip-Größe wird aus `Security.PriceStep` berechnet; Verwenden Sie `PipSizeOverride`, wenn der Broker einen nicht standardmäßigen Schritt für JPY-Paare verwendet.

## Nutzung
1. Hängen Sie die Strategie an die täglichen EURJPY-Daten an oder aktualisieren Sie `CandleType`, wenn ein anderer Zeitrahmen gewünscht wird.
2. Stellen Sie sicher, dass die Pip-Größe korrekt erkannt wird. Passen Sie `PipSizeOverride` bei Bedarf an.
3. Konfigurieren Sie die Geldverwaltungsparameter (`LotSize`, `RiskBoost`) entsprechend den Kontobeschränkungen.
4. Führen Sie die Strategie im StockSharp Designer oder API Runner aus, um das Verhalten vor dem Live-Handel zu validieren.
