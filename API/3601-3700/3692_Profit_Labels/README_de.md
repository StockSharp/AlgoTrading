# Profit-Label-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Profit Labels-Strategie** wandelt den MetaTrader 5 Expert Advisor *Profit Labels (54352)* in den StockSharp High-Level API um. Die Strategie überwacht die Übergänge des Triple Exponential Moving Average (TEMA) zu offenen Positionen und zeichnet Gewinnmarkierungen auf dem Diagramm auf, nachdem eine Position geschlossen wurde. Wenn der Trend nach oben umschlägt, eröffnet der Algorithmus eine Long-Position, und wenn der Trend nach unten umschlägt, eröffnet er eine Short-Position. Wenn noch eine Gegenposition aktiv ist, schließt die Strategie diese zunächst und druckt das Etikett des realisierten Gewinns aus.

Kerzen werden über ein `SubscribeCandles`-Abonnement verarbeitet und der Indikator ist über `Bind` gebunden, um die Implementierung vollständig auf hohem Niveau zu halten. Fertige Kerzen aktualisieren die TEMA-Werte und lösen Handelsentscheidungen aus.

## Handelsregeln

1. **Bullish Crossover**: Wenn der aktuelle TEMA-Wert über den vorherigen Wert steigt, während die älteren Messwerte einen Abwärtstrend zeigen, eröffnet die Strategie eine Long-Position, wenn derzeit keine Long-Position aktiv ist.
2. **Bearish Crossover**: Wenn der TEMA auf die gleiche Weise nach unten dreht, eröffnet er eine Short-Position, sofern kein Short aktiv ist.
3. **Positionsumkehr**: Wenn zum Zeitpunkt eines neuen Signals eine entgegengesetzte Position besteht, schließt die Strategie die offene Position, bevor eine neue Order platziert wird.
4. **Gewinnbezeichnungen**: Sobald die Position vollständig geschlossen ist, wird der realisierte PnL berechnet und mit `DrawText` im Diagramm angezeigt.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Zeitrahmen für das Kerzenabonnement. |
| `TemaPeriod` | `6` | Periode des dreifachen exponentiellen gleitenden Durchschnitts. |
| `TradeVolume` | `0.1` | Mit jeder Market-Order übermitteltes Volumen. |
| `PlacingTrade` | `false` | Aktiviert oder deaktiviert die Live-Auftragserteilung. |
| `LabelOffset` | `0` | Vertikaler Offset, der auf das Gewinnetikett über dem Handelspreis angewendet wird. |

## Notizen

- Die Strategie basiert ausschließlich auf fertigen Kerzen und greift nicht direkt auf Indikatorpuffer zu.
- Schützende Stop-Loss- und Take-Profit-Levels aus der MQL-Version werden nicht repliziert; Die Positionen werden umgekehrt, wenn ein entgegengesetztes Signal eintrifft.
- Gewinnetiketten verwenden die Sicherheitswährung, wann immer diese verfügbar ist, und greifen andernfalls auf Rohwerte zurück.
