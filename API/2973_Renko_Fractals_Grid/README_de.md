# Renko Fractals Grid Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Renko Fractals Grid ist ein direkter Port des MetaTrader 4 Expert Advisors "RENKO FRACTALS GRID". Die Strategie handelt Ausbrüche aus aktuellen Bill-Williams-Fractals, die durch einen Renko-artigen Volatilitätsfilter, einen gewichteten gleitenden Durchschnitt als Trendbias und die Momentumstärke aus dem Rate-of-Change-Indikator bestätigt werden. Die StockSharp-Version behält das gitterartige Positionsmanagement des Original-Roboters bei, einschließlich Martingale-Positionssizing, Break-Even-Handling, Trailing Stops, Equity-Schutz und optionalem Trailing von Floating-Profit in Währungseinheiten.

## Handelslogik
- **Fractal-Ausbruch:** Ein Long-Setup erfordert, dass das jüngste bullische Fractal von der letzten abgeschlossenen Kerze gebrochen wird, während mindestens einer der drei vorherigen Closes unter diesem Niveau blieb. Short-Trades spiegeln dieses Verhalten mit bärischen Fractals wider.
- **Renko-Filter:** Die Strategie prüft den High/Low-Bereich der letzten _CandlesToRetrace_ Balken. Ein Ausbruch ist nur gültig, wenn der aktuelle Close mindestens eine Renko-"Box" (entweder ein fester Pip-Abstand oder der letzte ATR-Wert) von diesen Extremen entfernt ist.
- **Trendfilter:** Schneller und langsamer gewichteter gleitender Durchschnitt müssen ausgerichtet sein (schnell über langsam für Longs und darunter für Shorts).
- **Momentum-Prüfung:** Die absolute Abweichung der letzten drei Rate-of-Change-Werte von 100 muss die konfigurierten Schwellenwerte überschreiten. Dies ahmt den MQL-Momentum-Filter basierend auf `iMomentum` nach.
- **MACD-Bestätigung:** Trades sind nur erlaubt, wenn die MACD-Hauptlinie auf der richtigen Seite ihrer Signallinie ist. Die gleiche Prüfung wird für das Exit-Timing verwendet.

## Risikomanagement
- **Martingale-Gitter:** Jede zusätzliche Position multipliziert das Basisvolumen mit _LotExponent_, während die Anzahl gleichzeitiger Trades durch _MaxTrades_ begrenzt ist.
- **Stop-Loss und Take-Profit:** Statische Preisoffsets in Pips werden vom durchschnittlichen Eintrittspreis angewendet.
- **Break-Even:** Wenn der Preis um _BreakEvenTriggerPips_ vorrückt, bewegt sich der Stop auf den Einstieg plus _BreakEvenOffsetPips_.
- **Trailing Stop:** Ein kerzenbasierter Trailing Stop hält die beste seit dem Einstieg beobachtete Kursauslenkung.
- **Geldtrailing:** Optionales Floating-Profit-Management schließt alle Trades nach einem Rückzug von _MoneyStopLoss_, sobald der offene Gewinn _MoneyTakeProfit_ übersteigt.
- **Equity-Stop:** Die Strategie verfolgt den laufenden Equity-Peak (basierend auf dem Portfolio-Wert und offenem PnL). Wenn der Drawdown _EquityRiskPercent_ überschreitet, wird die gesamte Position liquidiert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Primärer Kerzentyp für alle Indikatoren. |
| `FastMaLength` / `SlowMaLength` | Perioden der gewichteten gleitenden Durchschnitte, die die Trendrichtung definieren. |
| `MomentumLength` | Rate-of-Change-Lookback für den Momentum-Filter. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Minimale absolute Abweichung von 100, die für Einstiege erforderlich ist. |
| `UseAtrFilter` | ATR statt einem festen Pip-Abstand für die Renko-Bestätigung verwenden. |
| `BoxSizePips` | Größe der synthetischen Renko-Box, wenn ATR-Filterung deaktiviert ist. |
| `CandlesToRetrace` | Anzahl der Kerzen, die bei der Messung von letzten Hochs und Tiefs untersucht werden. |
| `BaseVolume` | Anfangs-Handelsvolumen vor Anwendung des Martingale-Multiplikators. |
| `LotExponent` | Multiplikator auf jede neue Position im Gitter. |
| `MaxTrades` | Maximale Anzahl gleichzeitiger Positionen pro Richtung. |
| `StopLossPips` / `TakeProfitPips` | Statische Schutz-Stop- und Zielabstände. |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (auf null setzen zum Deaktivieren). |
| `UseBreakEven` | Aktivieren Sie das Bewegen des Stops auf Break-Even. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Erforderliche Distanz vor der Break-Even-Aktivierung und der danach angewendete Offset. |
| `UseMoneyTarget` | Floating-Profit-Trailing in Währungseinheiten aktivieren. |
| `MoneyTakeProfit` / `MoneyStopLoss` | Gewinnschwelle, die das Geldtrailing aktiviert, und der maximal zulässige Rückzug. |
| `UseEquityStop` | Equity-basierten globalen Stop-Out aktivieren. |
| `EquityRiskPercent` | Maximal zulässiger Drawdown vom Equity-Peak, bevor alle Trades geschlossen werden. |

## Implementierungshinweise
- Der ursprüngliche EA wertet MACD auf dem monatlichen Zeitrahmen aus. Der StockSharp-Port verwendet dieselbe Indikatorkonfiguration auf dem Arbeitszeitrahmen, da Multi-Zeitrahmen-Daten standardmäßig nicht verfügbar sind.
- Alle Preisoffsets, die von "Pips" in MQL stammen, werden über den Preisschritt des Instruments konvertiert, um mit gebrochenen Pip-Quotierungen zu arbeiten.
- Die Tracking von realisierten Gewinnen wird über gefüllte Orderevents approximiert, was für die Equity-Drawdown-Logik in Abwesenheit von broker-bereitgestellten Kontostatistiken ausreichend ist.
- Die Strategie verwendet High-Level-Kerzenabonnements mit Indikatorbindung gemäß den Projektrichtlinien und hält jeden Inline-Kommentar auf Englisch wie angefordert.
