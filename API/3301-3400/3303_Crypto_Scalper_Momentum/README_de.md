# Crypto-Scalper-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Crypto-Scalper-Momentum-Strategie** repliziert den ursprünglichen MetaTrader-Expert-Advisor "Crypto Scalper", indem sie Money Flow Index, Momentum und Multi-Zeitrahmen-MACD-Filter kombiniert. Sie arbeitet auf einem primären Intraday-Zeitrahmen, bestätigt kurzfristiges Momentum auf einem höheren Zeitrahmen und respektiert einen Makrotrendfilter aus einem langsamen MACD. Mehrere Risikomanagement-Funktionen der MQL-Implementierung wurden beibehalten, darunter währungsbasierte Basket-Ziele, Geld-Trailing, Break-even-Stops und Equity-Drawdown-Schutz.

## Handelslogik

1. **Primäre Indikatoren**
   - Money Flow Index (MFI) im Hauptzeitrahmen mit Standardwert 14 Perioden.
   - MACD im Hauptzeitrahmen (EMA-Konfiguration 12/26/9).
2. **Momentum des höheren Zeitrahmens**
   - Momentum-Indikator auf einer separaten Kerzenserie. Die absolute Entfernung von der MetaTrader-Basislinie (100) muss eine konfigurierbare Schwelle überschreiten.
3. **Makrotrendfilter**
   - Ein langsamer MACD auf einem Makrozeitrahmen (standardmäßig täglich) verhindert Trades gegen den höheren Trend und erzwingt Liquidation bei Umkehr.
4. **Einstiegsregeln**
   - **Longs**: Mindestens einer der letzten drei MFI-Werte liegt unter der Überverkauft-Schwelle, die Momentum-Abweichung überschreitet die Schwelle, die primäre MACD-Linie liegt über der Signallinie und der Makro-MACD ist bullisch.
   - **Shorts**: gespiegelte Bedingungen mit Überkauft-Schwellen und bärischen MACD-Bestätigungen.
5. **Ausstiegsregeln**
   - Fester Stop-Loss und Take-Profit in Pips.
   - Optionaler Trailing Stop entweder über Kerzenextreme oder als klassischer distanzbasierter Trail.
   - Break-even-Verschiebung nach einer konfigurierbaren günstigen Bewegung.
   - Eine Umkehr des Makro-MACD schließt bestehende Exposure.
   - Währungsziele, Prozentziele und Trailing-Gewinn in Geld replizieren die MQL-Funktionen.
   - Ein Equity-Drawdown-Wächter schließt alle Trades, wenn das Konto um einen vorgegebenen Prozentsatz vom Hoch zurückläuft.

## Risikomanagement

- **Stops/Ziele**: konfigurierbare Pip-Distanzen mit optionaler Aktivierung.
- **Trailing**: kerzenbasiert (niedrigstes Tief/höchstes Hoch der jüngsten Kerzen) oder klassisches Pip-Trailing.
- **Break-even**: verschiebt Stops, um Gewinne zu sichern, sobald die Triggerdistanz erreicht ist.
- **Geldmanagement**: Basket-Take-Profit in Währung, Prozent der Anfangsequity und Trailing-Gewinn in Geld.
- **Equity-Stop**: überwacht die höchste beobachtete Equity und schließt Trades, sobald der Drawdown den erlaubten Prozentsatz überschreitet.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `CandleType` | Primäre Kerzenserie für Einstiege. |
| `MomentumCandleType` | Kerzen des höheren Zeitrahmens für den Momentum-Indikator. |
| `MacroCandleType` | Kerzen des langsamen Zeitrahmens für den Makro-MACD-Filter. |
| `MfiPeriod` | Länge des Money Flow Index. |
| `MfiOversold` / `MfiOverbought` | Oszillatorschwellen (Standard 30 / 70). |
| `MomentumPeriod` | Momentum-Länge im höheren Zeitrahmen. |
| `MomentumThreshold` | Mindestabweichung von der 100-Linie, die der Momentum-Filter verlangt. |
| `MomentumReference` | Basiswert (MetaTrader-Standard ist 100). |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD-Parameter im Handelszeitrahmen. |
| `MacroMacdFastPeriod` / `MacroMacdSlowPeriod` / `MacroMacdSignalPeriod` | MACD-Parameter im Makrozeitrahmen. |
| `TradeVolume` | Volumen jeder Marktorder (Lots). |
| `MaxTrades` | Maximale gleichzeitige Trades pro Richtung (0 = unbegrenzt). |
| `UseStopLoss` / `StopLossPips` | Aktiviert und konfiguriert den Schutzstop. |
| `UseTakeProfit` / `TakeProfitPips` | Aktiviert und konfiguriert das Schutzziel. |
| `UseTrailingStop` | Hauptschalter für die Trailing-Logik. |
| `UseCandleTrail` | Wechsel zwischen Kerzenextrem-Trailing und klassischem Trailing. |
| `TrailTriggerPips` / `TrailAmountPips` | Triggerdistanz und vom klassischen Trailing Stop gehaltene Distanz. |
| `CandleTrailLength` / `CandleTrailBufferPips` | Anzahl Kerzen und zusätzlicher Puffer für kerzenbasiertes Trailing. |
| `UseBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Break-even-Aktivierungsdistanz und gesicherter Gewinn. |
| `UseMoneyTakeProfit` / `MoneyTakeProfit` | Basket-Take-Profit in Kontowährung. |
| `UsePercentTakeProfit` / `PercentTakeProfit` | Basket-Take-Profit in Prozent der Anfangsequity. |
| `EnableMoneyTrailing` / `MoneyTrailTarget` / `MoneyTrailStop` | Trailing des schwebenden Gewinns in Währung. |
| `UseEquityStop` / `EquityRiskPercent` | Equity-Drawdown-Schutz relativ zum beobachteten Hoch. |
| `ForceExit` | Stellt Positionen beim nächsten Kerzenschluss sofort glatt. |

## Hinweise

- Pip-Distanzen werden mit `PriceStep` des Instruments umgerechnet. Ein Fallback von `0.0001` wird verwendet, wenn der Broker keinen Preisschritt liefert, entsprechend der Punktbehandlung in MetaTrader.
- Das Makro-MACD-Abonnement kann auf Monatskerzen zeigen, um den ursprünglichen EA nachzuahmen. Tageskerzen sind Standard, weil Monatsbalken nicht in allen Datenfeeds verfügbar sind.
- Alle Kommentare im Code sind absichtlich auf Englisch geschrieben, um die Repository-Regeln einzuhalten.
