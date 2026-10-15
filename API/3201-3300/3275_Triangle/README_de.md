# Dreiecksstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader Expert Advisor **Triangle v1** auf die High-Level-API von StockSharp. Der ursprüngliche EA kombinierte Filter mit gewichteten gleitenden Durchschnitten auf einem höheren Zeitrahmen, eine Momentum-Divergenzprüfung und eine sehr langfristige MACD-Bestätigung, bevor er breakoutartige Orders platzierte. Die StockSharp-Version behält die Multi-Timeframe-Logik bei und ersetzt tickbasiertes Money Management durch kerzenbasierte Schutzorders.

## Funktionsweise

1. **Multi-Timeframe-Filter.** Der Arbeitszeitrahmen (`CandleType`, Standard 15 Minuten) wird zur Ausführung von Trades verwendet. Trend- und Momentum-Filter werden auf einem höheren Zeitrahmen (`TrendCandleType`, Standard 1 Stunde) berechnet, um die MQL-Aufrufe zu spiegeln, die `T` referenzierten.
2. **LWMA-Trend-Gate.** Schnelle und langsame gewichtete gleitende Durchschnitte (LWMA-Äquivalent) müssen ausgerichtet sein. Long-Setups verlangen, dass die schnelle LWMA über der langsamen LWMA bleibt; Shorts verlangen die entgegengesetzte Beziehung.
3. **Momentum-Abweichung.** Eine 14-Perioden-Momentum-Reihe auf dem höheren Zeitrahmen muss in einer der letzten drei abgeschlossenen Kerzen mindestens um `MomentumThreshold` vom neutralen Niveau (100) abweichen, wodurch die Prüfungen `MomLevelB/MomLevelS` reproduziert werden.
4. **MACD-Bestätigung.** Ein sehr hoher Zeitrahmen (`MacdCandleType`, Standard 30-Tage-Kerzen ≈ monatlich) muss die MACD-Hauptlinie auf der richtigen Seite der Signallinie zeigen, bevor Trades erlaubt werden; dies kopiert die Bedingung `MacdMAIN0` gegenüber `MacdSIGNAL0`.
5. **Schutzausstiege.** Stop-Loss- und Take-Profit-Distanzen werden in Preisschritten konfiguriert. Wenn eines der Niveaus auf einer abgeschlossenen Bar erreicht wird, schließt die Strategie die Position mit einer Marktorder.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `FastMaPeriod`, `SlowMaPeriod` | Längen der gewichteten gleitenden Durchschnitte des höheren Zeitrahmens. |
| `MomentumPeriod` | Periode für den Momentum-Filter auf dem höheren Zeitrahmen. |
| `MomentumThreshold` | Minimale absolute Abweichung von 100, die in einer der letzten drei Momentum-Messungen erforderlich ist. Auf 0 setzen, um den Filter zu deaktivieren. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD-Parameter, die auf `MacdCandleType` angewendet werden. |
| `StopLossSteps`, `TakeProfitSteps` | Schutz-Stop- und Ziel-Distanzen, gemessen in Instrumenten-Preisschritten (Ticks). 0 verwenden, um zu deaktivieren. |
| `CandleType` | Handelszeitrahmen für die Orderausführung. |
| `TrendCandleType` | Höherer Zeitrahmen, der LWMAs und Momentum speist. |
| `MacdCandleType` | Zeitrahmen für den MACD-Bestätigungsfilter. |

## Verwendung

1. Wählen Sie ein Instrument und konfigurieren Sie `CandleType`, `TrendCandleType` und `MacdCandleType` passend zu den Zeitrahmen, die Sie analysieren möchten.
2. Passen Sie MA-, Momentum- und MACD-Längen an, wenn Sie das System an einen anderen Markt oder ein anderes Volatilitätsregime anpassen möchten.
3. Setzen Sie `StopLossSteps` und `TakeProfitSteps` entsprechend der Tickgröße des Instruments. Die Strategie wandelt die Schrittzahlen automatisch in tatsächliche Preisdistanzen um.
4. Starten Sie die Strategie. Sie abonniert alle erforderlichen Kerzenströme, aktualisiert Indikatoren mit der High-Level-`Bind`-API und verwaltet die Position, wenn Stops oder Ziele getroffen werden.

## Unterschiede zum ursprünglichen EA

- Geldbasierte Ausstiege (`Use_TP_In_Money`, `Use_TP_In_percent`) und der Kontostands-Schutzblock werden nicht nachgebildet, weil StockSharp in Instrumenteneinheiten arbeitet. Ein entsprechendes Verhalten kann durch Abstimmung von `StopLossSteps`/`TakeProfitSteps` erreicht werden.
- Trailing-Stop-, Break-even- und Equity-Stop-Logik aus dem EA beruhten auf Tickverarbeitung und MetaTrader-spezifischen Orderänderungsaufrufen. Die Portierung behält aus Gründen der Klarheit den einfacheren Fixed-Stop-Ansatz bei; Benutzer können `UpdatePositionState` bei Bedarf um Trailing-Regeln erweitern.
- Manuelle Trendlinien (`TREND`/`TRENDLOW`) und Fraktal-Arrays wurden im EA als diskretionäre Filter verwendet. Sie werden bewusst weggelassen, damit die StockSharp-Strategie vollständig systematisch bleibt.
- Die Strategie hält immer höchstens eine Nettoposition, was der typischen Nutzung entspricht, obwohl der EA einen Parameter `Max_Trades` bereitstellte.

Stimmen Sie Schwellenwerte und Zeitrahmenparameter auf das gehandelte Instrument ab. Für volatile Märkte sind meist breitere Werte erforderlich, damit kleine Momentum-Schwankungen nicht alles herausfiltern.
