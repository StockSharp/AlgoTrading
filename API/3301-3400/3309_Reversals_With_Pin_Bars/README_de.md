# Reversals-With-Pin-Bars-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein C#-Port des MetaTrader-Expert-Advisors **"Reversals With Pin Bars"**. Der ursprüngliche EA sucht nach Ablehnungskerzen mit langen Dochten (Pin Bars) und bestätigt sie mit einem Momentum-Filter, einer Trendprüfung über eine linear gewichtete gleitende Durchschnittslinie (LWMA) auf höherem Zeitrahmen und einem MACD-Richtungsfilter. Der Port behält diese Multi-Zeitrahmen-Struktur bei, nutzt ausschließlich StockSharp-Indikatoren und stellt die wichtigsten Risikosteuerungen als Parameter bereit.

Die Implementierung konzentriert sich auf die StockSharp-High-Level-API: Kerzen des primären Zeitrahmens treiben Einstiege an, während zusätzliche Abonnements Indikatoren höherer Zeitrahmen speisen. Risikomanagement wird in Pips ausgedrückt und unterstützt optionale Trailing-Stop- und Break-even-Automatisierung.

## Einstiegslogik
- **Pin-Bar-Erkennung**: Die vorherige abgeschlossene Kerze muss einen Docht haben, der mindestens 50% ihrer gesamten Spanne ausmacht.
  - Long-Setup: Der obere Schatten dominiert (entspricht der ursprünglichen "Hanging Man"-Prüfung).
  - Short-Setup: Der untere Schatten dominiert.
- **Trendfilter**: Die schnelle LWMA (Länge = `FastMaPeriod`) muss im höheren Zeitrahmen über/unter der langsamen LWMA (`SlowMaPeriod`) liegen.
- **Momentum-Filter**: Die absolute Entfernung des Momentum-Werts von 100 auf einer der letzten drei Bars des höheren Zeitrahmens muss `MomentumThreshold` überschreiten.
- **MACD-Filter**: Die MACD-Hauptlinie muss im MACD-Zeitrahmen über/unter der Signallinie liegen.
- **Positionslimits**: Die Netto-Exposure darf `MaxTrades * Volume` nicht überschreiten. Neue Trades verwenden die ausgerichtete Einstellung `Volume`.

## Risikomanagement
- **Stop-Loss / Take-Profit**: Feste Distanzen in Pips (`StopLossPips`, `TakeProfitPips`) vom Einstiegsschlusskurs.
- **Break-even**: Wenn aktiviert, bewegt sich der Stop auf `entry +/- BreakEvenOffsetPips`, sobald der Preis um `BreakEvenTriggerPips` voranschreitet.
- **Trailing Stop**: Wenn aktiviert, hält der Trail eine Distanz von `TrailingStopPips` zum letzten Schlusskurs.
- **Automatisches Glattstellen**: Das Erreichen des berechneten Stops oder Ziels beendet die gesamte Position mit einer Marktorder.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `TradeVolume` | Volumen für jeden neuen Einstieg, am Instrumentenschritt ausgerichtet. |
| `MaxTrades` | Maximale Anzahl gleichgerichteter Einstiege (aggregiertes Volumenlimit). |
| `StopLossPips` | Stop-Loss-Distanz in Pips. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. |
| `EnableTrailing` / `TrailingStopPips` | Aktiviert und konfiguriert die Trailing-Stop-Distanz. |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Break-even-Aktivierung und Puffereinstellungen. |
| `FastMaPeriod` / `SlowMaPeriod` | Längen der LWMAs des höheren Zeitrahmens. |
| `MomentumPeriod` / `MomentumThreshold` | Momentum-Länge und minimale absolute Entfernung von 100. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD-Konfiguration für den langfristigen Filter. |
| `CandleType` | Primäre Kerzenserie für die Pin-Bar-Erkennung. |
| `HigherCandleType` | Kerzenserie für LWMAs und Momentum. |
| `MacdCandleType` | Kerzenserie für MACD. |

## Unterschiede zur MetaTrader-Version
- Geldbasierte Take-Profit-, Trailing- und Equity-Stop-Optionen wurden ausgelassen; Risiko wird über Pips ausgedrückt.
- Fraktallinien-Bestätigungen, die Chartobjekte erforderten, wurden durch indikatorbasierte Bedingungen ersetzt.
- Alle Benachrichtigungen (Alerts, E-Mails, Push-Nachrichten) wurden entfernt; die StockSharp-Version konzentriert sich auf Handelslogik.

## Nutzungshinweise
1. Weisen Sie die Strategie einem Portfolio und Instrument zu und passen Sie dann die drei Kerzentypen an Ihr gewünschtes Multi-Zeitrahmen-Setup an.
2. Stellen Sie sicher, dass der Preisschritt des Instruments der Pip-Definition entspricht (Standard-Fallback ist 0.0001).
3. Starten Sie die Strategie; Stops, Ziele, Trailing und Break-even-Verwaltung erfolgen automatisch beim Kerzenschluss.
4. Überwachen Sie die Ergebnisse; passen Sie Momentum- und LWMA-Längen an das Volatilitätsprofil des Instruments an.
