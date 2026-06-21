# WellMartin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **WellMartin**-Strategie ist ein Mean-Reversion-System, das Bollinger-Bänder und den Average Directional Index (ADX) kombiniert. Es werden Long-Positionen eingegangen, wenn der Preis bei niedriger Trendstärke unter das untere Bollinger-Band fällt, und Short-Positionen, wenn der Preis unter denselben Bedingungen über das obere Band steigt. Positionen werden geschlossen, wenn der Preis das entgegengesetzte Band erreicht oder die konfigurierten Take-Profit- oder Stop-Loss-Niveaus trifft.

## Parameter

- **CandleType** – Kerzenserie für Berechnungen.
- **BollingerPeriod** – Periode für Bollinger-Bänder.
- **BollingerWidth** – Standardabweichungsmultiplikator für Bollinger-Bänder.
- **AdxPeriod** – Periode für den ADX-Indikator.
- **AdxLevel** – ADX-Schwellenwert; Trades werden nur ausgeführt, wenn der ADX-Wert unter diesem Niveau liegt.
- **Volume** – Handelsvolumen für jeden Einstieg.
- **TakeProfit** – Gewinnziel in Preiseinheiten.
- **StopLoss** – Verlustlimit in Preiseinheiten.

## Logik

1. Kerzendaten abonnieren und Bollinger-Bänder und ADX berechnen.
2. Wenn keine offene Position vorhanden ist:
   - **Kaufen**, wenn der Schlusskurs unter dem unteren Band liegt und ADX unter dem Schwellenwert.
   - **Verkaufen**, wenn der Schlusskurs über dem oberen Band liegt und ADX unter dem Schwellenwert.
3. Die Seite des zuletzt ausgeführten Trades verfolgen und Einstiege nur in derselben Richtung oder bei keinen bisher getätigten Trades erlauben.
4. Bei einer Long-Position:
   - Aussteigen, wenn der Preis das obere Band berührt, das Take-Profit-Ziel erreicht oder den Stop-Loss trifft.
5. Bei einer Short-Position:
   - Aussteigen, wenn der Preis das untere Band berührt, das Take-Profit-Ziel erreicht oder den Stop-Loss trifft.

## Hinweise

Diese Implementierung verwendet ein festes Handelsvolumen. Die ursprüngliche MQL-Version erhöhte das Volumen nach einem Verlusttrade; dieses Verhalten kann bei Bedarf später hinzugefügt werden.
