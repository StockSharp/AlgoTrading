# MACD 1 MIN SCALPER-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein C#-Port des MetaTrader-Expertenberaters **"MACD 1 MIN SCALPER"**. Sie kombiniert gewichtete gleitende Durchschnitte mit Multi-Timeframe-MACD-Bestätigungen und einem Momentum-Filter vor der Eröffnung von Trades. Das Ziel ist, in Trendrichtung zu handeln, wenn Indikatoren auf niedrigerem und höherem Zeitrahmen ausgerichtet sind und das Kurs-Momentum ausreichend stark ist.

## Handelslogik

1. **Basis-Zeitrahmen** – konfigurierbar (Standard M1). Zwei gewichtete gleitende Durchschnitte (WMA) mit den Perioden 50 und 200, berechnet auf dem typischen Preis `(Hoch + Tief + Schluss) / 3`, definieren den kurzfristigen Trend.
2. **Trend-Filter höherer Zeitrahmen** – WMAs mit denselben Perioden werden auf dem H1-Zeitrahmen berechnet. Long-Setups erfordern, dass beide schnellen WMAs über ihren langsamen Pendants liegen, Shorts erfordern das Gegenteil. Wenn der Arbeits-Zeitrahmen bereits H1 ist, werden die Basis-WMAs wiederverwendet.
3. **MACD-Bestätigungen** – der MACD (12, 26, 9) muss seine Hauptlinie über der Signallinie auf dem Basis-Zeitrahmen, dem H1-Zeitrahmen und einem monatlichen Zeitrahmen (ca. 43200 Minuten) haben. Short-Einstiege erfordern, dass alle drei MACDs unter ihren Signalen liegen.
4. **Momentum-Filter** – ein Momentum-Indikator mit Periode 14 operiert auf einem höheren Zeitrahmen, der aus dem MetaTrader-Basis-Zeitrahmen abgeleitet wird (M1→M15, M5→M30, …). Die absolute Abweichung von 100 muss einen konfigurierbaren Schwellenwert auf mindestens einem der letzten drei abgeschlossenen Bars überschreiten.
5. **Einstiegsregeln** – eine Long-Position wird eröffnet, wenn alle bullischen Bedingungen erfüllt sind und die Strategie aktuell keine Long-Exposition hat. Eine Short-Position erfordert die gespiegelten bärischen Bedingungen. Wenn eine entgegengesetzte Position offen ist, schließt die Ordergröße automatisch die zum Schließen benötigte Menge ein.
6. **Risikomanagement** – optionale Stop-Loss- und Take-Profit-Abstände werden in Pips angegeben und beim Start in Instrument-Punkte umgerechnet. Trailing-, Breakeven- und Geldverwaltungsfunktionen aus dem Originalskript werden in diesem High-Level-Port bewusst weggelassen.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Arbeitszeitrahmen für die Basisindikatoren. |
| `OrderVolume` | Volumen, das bei jedem Markteinstieg gesendet wird. Auch zum Schließen/Umkehren von Positionen verwendet. |
| `FastMaPeriod` / `SlowMaPeriod` | Längen der schnellen und langsamen gewichteten gleitenden Durchschnitte. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | EMA-Perioden für den MACD-Indikator. |
| `MomentumPeriod` | Momentum-Indikatorlänge auf dem Bestätigungs-Zeitrahmen. |
| `MomentumThreshold` | Minimale absolute Abweichung von 100, die erforderlich ist, um Momentum zu akzeptieren. |
| `TakeProfitPips` / `StopLossPips` | Optionale Schutz-Niveaus in Pips. |

## Implementierungshinweise

- Die Strategie stützt sich auf StockSharpss High-Level-Kerzenabonnements (`SubscribeCandles`) und Indikatoranbindung (`Bind` / `BindEx`). Keine manuellen Indikatorberechnungen oder historischen Puffer werden verwendet.
- Der Momentum-Zeitrahmen wird aus dem MetaTrader-Mapping abgeleitet: `[1,5,15,30,60,240,1440,10080,43200]`. Wenn ein Wert außerhalb dieser Liste liegt, wird ein 4×-Multiplikator des Basis-Zeitrahmens als Fallback verwendet.
- `StartProtection` wird nur gestartet, wenn mindestens einer der Risikoparameter größer als null ist. In diesem Port gibt es keine Trailing-Stop-Implementierung.
- Diagramm-Rendering ist für die Basis-Kerzen, beide WMAs und den MACD aktiviert, um visuelle Inspektion während des Debuggings oder Live-Tradings zu erleichtern.

## Nutzungstipps

- Den Parameter `OrderVolume` entsprechend der Mindestlosgröße des Instruments einstellen. Der Helfer passt automatisch das gesendete Volumen an, um es dem Schritt- und Min/Max-Constraints des Symbols anzupassen.
- Sicherstellen, dass Daten höherer Zeitrahmen (H1 und monatlich) im Datenfeed verfügbar sind. Ohne diese Kerzen wird die Strategie keine Positionen eröffnen, da die Bestätigungssignale unvollständig bleiben.
- Momentum-Filterung ist sensitiv gegenüber dem gewählten Schwellenwert. Höhere Werte erfordern stärkere Momentum-Schübe, während niedrigere Werte zu häufigeren Trades führen.
