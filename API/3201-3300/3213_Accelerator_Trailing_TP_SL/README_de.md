# Accelerator Trailing TP & SL-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Accelerator Trailing TP & SL-Strategie portiert den Expertenberater "Accelerator Trailing TP&SL" von MetaTrader in die High-Level-API von StockSharp. Das System kombiniert Bill Williams' Accelerator Oszillator mit Multi-Timeframe-Momentum-Bestätigung und einem monatlichen MACD-Trendfilter. Einstiege werden mit geometrischer Positionsgrößen-Skalierung aufgebaut, während Ausstiege klassische Stop/Ziel-Abstände, adaptives Trailing und Break-Even-Logik kombinieren.

## Trading-Logik
- **Momentum-Filter** – ein 14-Perioden-Momentum-Indikator, der auf einem höheren Zeitrahmen berechnet wird, muss auf jedem der letzten drei abgeschlossenen Bars vom neutralen 100er-Niveau um mindestens den konfigurierten Schwellenwert abweichen.
- **Accelerator Oszillator** – Long-Trades erfordern eine positive Accelerator-Ablesung, Short-Trades eine negative Ablesung auf dem Signal-Zeitrahmen.
- **Gleitende Durchschnitte** – ein schneller linear gewichteter gleitender Durchschnitt (LWMA) muss für Longs über dem langsamen LWMA liegen und für Shorts darunter, was den ursprünglichen schnellen/langsamen Trendfilter annähert.
- **Monatlicher MACD-Trend** – standardmäßig beobachtet der Filter monatliche Kerzen. Long-Trades verlangen, dass die MACD-Linie über der Signallinie liegt (auch wenn beide Werte negativ sind), während Short-Trades die umgekehrte Bedingung erfordern.
- **Gestaffelte Einstiege** – die Strategie kann bis zur konfigurierten maximalen Anzahl von Positionen pro Richtung pyramidieren. Jeder zusätzliche Einstieg wird mit dem Lot-Exponenten multipliziert, was die Martingal-artige Größenbestimmung aus dem MQL-Programm recreiert.

## Risikomanagement
- **Statischer Stop-Loss / Take-Profit** – Pip-Abstände spiegeln die ursprünglichen Stop-Loss- und Take-Profit-Einstellungen wider.
- **Trailing Stop** – wenn aktiviert, verfolgt die Strategie den günstigsten Preis um die konfigurierte Pip-Anzahl.
- **Break-Even-Bewegung** – nachdem ein Trade die Trigger-Distanz erreicht, wird der Stop um den angegebenen Offset vorgeschoben, um aufgelaufene Gewinne zu schützen.
- **MACD-Ausstieg** – wenn der MACD-Filter gegen die aktive Position umschlägt, kann die Strategie sofort alle Positionen schließen, was dem manuellen Ausstiegshilfer im MQL-Code entspricht.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `FastMaLength` / `SlowMaLength` | Perioden der schnellen und langsamen LWMAs auf dem Handels-Zeitrahmen. |
| `MomentumThreshold` | Minimale absolute Abweichung des Momentums vom neutralen 100er-Wert auf dem höheren Zeitrahmen. |
| `StopLossPips` / `TakeProfitPips` | Schutz-Stop- und Zielabstände in Pips. |
| `TrailingStopPips` | Distanz des optionalen Trailing-Stop-Managers. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Definiert wann und wie der Stop auf Break-Even verschoben wird. |
| `MaxTrades` | Maximale Anzahl gestaffelter Einstiege pro Richtung. |
| `BaseVolume` | Volumen der ersten Order in einer Sequenz. |
| `LotExponent` | Multiplikator für jeden zusätzlichen gestaffelten Einstieg. |
| `EnableTrailing` | Aktiviert oder deaktiviert das Trailing-Stop-Management. |
| `UseBreakEven` | Aktiviert oder deaktiviert die Break-Even-Stop-Bewegung. |
| `CloseOnMacdFlip` | Schließt alle Trades, wenn der MACD auf dem höheren Zeitrahmen umkehrt. |
| `CandleType` | Primäre Kerzenserie für Signale (Standard: 15 Minuten). |
| `MomentumCandleType` | Höhere Zeitrahmen-Kerzen für den Momentum-Filter (Standard: 1 Stunde). |
| `MacdCandleType` | Kerzenserie für den MACD-Trendfilter (Standard: monatliche Kerzen). |

## Hinweise
- Die Strategie benötigt das Instrument `PriceStep`, um pip-basierte Risikoeinstellungen in Preisabstände zu konvertieren. Stellen Sie sicher, dass die Wertpapier-Metadaten beim Ausführen der Strategie gefüllt sind.
- Da StockSharp Netto-Positionen verwendet, werden zusätzliche gestaffelte Einstiege durch wiederholtes Senden von Market-Orders geöffnet, bis das konfigurierte Maximum erreicht ist. Ausstiege schließen die gesamte Nettoposition und entsprechen den "Alles schließen"-Routinen im ursprünglichen Experten.
- Der monatliche MACD-Zeitrahmen kann über den `MacdCandleType`-Parameter angepasst werden, um verschiedene Instrumente oder Backtests zu unterstützen.
