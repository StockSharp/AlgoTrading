# 5-Minuten-Umschläge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **5Mins Envelopes**-Strategie reproduziert den MetaTrader-Experten, der Fünf-Minuten-Kerzen um einen linear gewichteten Umschlag mit gleitendem Durchschnitt handelt.
Es sucht nach Preisspitzen, die weit über die Bänder hinausgehen, und bewegt sich dann in die Richtung der Mean-Reversion.
Ein Spread-Filter, ein statischer Stop-Loss, ein optionaler Take-Profit und ein Trailing-Stop spiegeln das ursprüngliche Geldmanagement wider.

## Handelslogik
- **Indikator**: Linear Weighted Moving Average (LWMA), berechnet auf Basis des Medianpreises (Hoch+Tief)/2 mit einer Periode von 3.
- **Hüllkurvenbreite**: 0,05 % Abweichung vom LWMA-Wert (oberes und unteres Band).
- **Signalerkennung** (ausgewertet anhand der zuvor abgeschlossenen Kerze und des aktuellen Gebots):
  - **Long**: Das vorherige Kerzentief bleibt mehr als `DistancePoints` unter dem unteren Band **und** das aktuelle Gebot liegt ebenfalls außerhalb dieser Distanz.
  - **Short**: Das vorherige Kerzenhoch bleibt mehr als `DistancePoints` über dem oberen Band **und** das aktuelle Gebot liegt ebenfalls außerhalb dieser Distanz.
- **Filter**:
  - Immer nur eine Position (bei neuen Einträgen muss die aktuelle Position flach sein).
  - Wenn `MaxSpreadPoints` größer als Null ist, muss die Geld-Brief-Spanne unter diesem Schwellenwert bleiben, bevor eine neue Bestellung übermittelt wird.

## Risikomanagement
- **Ordervolumen**: Der Parameter `TradeVolume` steuert die Marktordergröße.
- **Stop-Loss**: `StopLossPoints` wird unter Verwendung der Tick-Größe des Instruments in einen absoluten Preisabstand umgerechnet.
- **Take-Profit**: Optional `TakeProfitPoints`; Zum Deaktivieren auf Null setzen.
- **Trailing Stop**: Optional `TrailingStopPoints`; Zum Deaktivieren auf Null setzen.
- **Schutz**: Der `StartProtection`-Helfer wendet alle Exits mit Marktaufträgen an und entspricht dem MetaTrader-Verhalten.

## Parameter
- `TradeVolume = 1m`
- `DistancePoints = 140`
- `EnvelopePeriod = 3`
- `EnvelopeDeviationPercent = 0.05m`
- `StopLossPoints = 250`
- `TakeProfitPoints = 0`
- `TrailingStopPoints = 120`
- `MaxSpreadPoints = 25`
- `CandleType = TimeFrame(5 minutes)`

## Schlagworte
- Kategorie: Mean Reversion
- Richtung: Beide
- Indikatoren: WeightedMovingAverage
- Stopps: Ja (fest + nachlaufend)
- Zeitrahmen: Intraday (M5)
- Komplexität: Anfänger
- Risikostufe: Mittel
- Saisonalität: Nein
- Neuronale Netze: Nein
- Divergenz: Nein
