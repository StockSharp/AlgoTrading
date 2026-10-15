# Psi Proc EMA MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das T4-System aus dem ursprünglichen MQL-Expert `e-PSI@PROC.mq4`. Sie handelt basierend auf der Ausrichtung mehrerer exponentieller gleitender Durchschnitte und einem MACD-Filter.

## Strategie-Logik

1. EMA(200), EMA(50) und EMA(10) bei jeder eingehenden Kerze berechnen.
2. MACD mit Parametern 12, 26, 9 berechnen.
3. Long gehen, wenn:
   - EMA200 steigt und EMA50 > EMA200.
   - EMA50 steigt und EMA10 > EMA50.
   - MACD steigt und über `LimitMACD` liegt.
4. Short gehen, wenn:
   - EMA200 fällt und EMA50 < EMA200.
   - EMA50 fällt und EMA10 < EMA50.
   - MACD fällt und unter `-LimitMACD` liegt.
5. Long verlassen, wenn der Preis unter EMA50 schließt.
6. Short verlassen, wenn der Preis über EMA50 schließt.

Optionale Take-Profit- und Trailing-Stop-Schutzmaßnahmen werden unterstützt.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `LimitMACD` | Minimaler absoluter MACD-Pegel für den Einstieg. |
| `TakeProfitPoints` | Take-Profit-Niveau in Preispunkten. |
| `TrailStopPoints` | Trailing-Stop-Niveau in Preispunkten. |
| `CandleType` | Zeitrahmen der von der Strategie verwendeten Kerzen. |

## Hinweise

- Trades werden mit Marktaufträgen eröffnet.
- Es werden nur abgeschlossene Kerzen verarbeitet.
- Die Strategie operiert auf einem einzelnen Wertpapier.
