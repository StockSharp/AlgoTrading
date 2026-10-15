# Wedge-Pattern-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Wedge-Pattern-Strategie** ist eine Konvertierung des MetaTrader-Expert-Advisors *Wedge pattern.mq4* in die StockSharp-High-Level-API. Sie sucht nach symmetrischen Keil-Konsolidierungen, die aus Bill-Williams-Fraktalen abgeleitet werden, und handelt Ausbrüche, wenn Trend- und Momentum-Filter übereinstimmen.

Die High-Level-Implementierung ersetzt die ursprüngliche manuelle Orderverwaltung durch StockSharp-Funktionen und bewahrt die Entscheidungslogik:

- **Trendfilter:** vergleicht eine schnelle und eine langsame linear gewichtete gleitende Durchschnittslinie (LWMA), berechnet auf typischen Preisen.
- **Momentum-Filter:** wertet die absolute Entfernung des 14-Perioden-Momentum-Indikators von seinem neutralen Niveau (100) aus. Die letzten drei Momentum-Werte müssen eine konfigurierbare Schwelle überschreiten.
- **MACD-Bestätigung:** verlangt, dass die MACD-Hauptlinie für Longs über der Signallinie (oder für Shorts darunter) liegt.
- **Fraktale Keilerkennung:** sammelt obere und untere Fraktalpunkte, um konvergierende Trendlinien zu bauen. Handelssignale entstehen, wenn der Preis jenseits dieser Linien plus konfigurierbarem Bestätigungspuffer schließt.
- **Risikomanagement:** imitiert die MQL-Implementierung mit festen Stop-Loss- und Take-Profit-Distanzen, automatischer Break-even-Verschiebung und Trailing-Stop-Anpassungen.

## Funktionsweise

1. Einen einzelnen Zeitrahmen abonnieren, der durch `CandleType` definiert ist.
2. Indikatorwerte mit jeder abgeschlossenen Kerze aktualisieren und rollierende Puffer für Hochs und Tiefs zur Fraktalerkennung halten.
3. Keil-Trendlinien aus den zwei jüngsten Hoch- und Tief-Fraktalen bauen. Nur konvergierende Keile (fallende Hochs und steigende Tiefs) gelten als gültige Setups.
4. Ein Long-Trade wird eröffnet, wenn:
   - Schnelle LWMA > langsame LWMA.
   - MACD-Linie > Signallinie.
   - Einer der letzten drei Momentum-Werte die konfigurierte Schwelle überschreitet.
   - Die aktuelle Kerze mindestens um den Ausbruchspuffer über der projizierten oberen Trendlinie schließt.
5. Ein Short-Trade spiegelt die Bedingungen mit invertierten Linien und Schwellen.
6. Nach dem Einstieg platziert die Strategie sofort Stop-Loss- und Take-Profit-Orders. Später kann sie den Stop auf Break-even verschieben und trailen, sobald die Position profitabel wird.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen für Analyse und Orders. |
| `FastMaPeriod` | Länge des schnellen LWMA-Filters. |
| `SlowMaPeriod` | Länge des langsamen LWMA-Filters. |
| `MomentumPeriod` | Rückblickperiode des Momentum-Indikators (Standard 14). |
| `MomentumThreshold` | Mindestabstand von 100, der vom Momentum-Indikator verlangt wird, um den Markt als impulsiv zu betrachten. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Standard-MACD-Konfiguration. |
| `FractalDepth` | Anzahl Bars auf jeder Seite, die zur Bestätigung eines Fraktalhochs oder -tiefs erforderlich sind. |
| `StopLossPips` | Anfängliche Schutzstop-Distanz in Pips. |
| `TakeProfitPips` | Anfängliche Gewinnzieldistanz in Pips. |
| `UseBreakeven`, `BreakevenTriggerPips`, `BreakevenOffsetPips` | Aktiviert und konfiguriert Break-even-Automatisierung. |
| `UseTrailing`, `TrailingActivationPips`, `TrailingDistancePips`, `TrailingStepPips` | Aktiviert und konfiguriert Trailing-Stop-Verhalten. |
| `BreakoutBufferPips` | Zusätzlicher Puffer für die Keil-Ausbruchsbestätigung. |

Alle pipbasierten Einstellungen werden anhand der Tickgröße der Security in Preisdistanzen umgerechnet. Die Standard-Pip-Berechnung berücksichtigt fraktionale Preisstellung (3 oder 5 Dezimalstellen) exakt wie der ursprüngliche Expert Advisor.

## Nutzungsempfehlungen

1. Binden Sie die Strategie an das gewünschte Instrument und wählen Sie den Kerzenzeitrahmen passend zum ursprünglichen Setup (z. B. 15-Minuten-Kerzen).
2. Konfigurieren Sie die Positionsgröße über die Basiseigenschaft `Strategy.Volume`.
3. Passen Sie optional Filter- und Risikoparameter an die Volatilität des Zielmarkts an.
4. Starten Sie die Strategie; sie abonniert Kerzen, zeichnet Chartdaten und handelt automatisch, sobald Keilausbrüche auftreten.

## Unterschiede zur MQL-Version

- Die StockSharp-Version verwendet High-Level-`SubscribeCandles` und Indikator-Binding-APIs und vermeidet manuelle Tickverarbeitung.
- Trailing-Stop- und Break-even-Verwaltung stützen sich auf `SetStopLoss`/`SetTakeProfit` und integrieren sich in das eingebaute Schutzverhalten.
- Es wird jeweils nur eine Position gehalten; das MetaTrader-Skript unterstützte Pyramiding bis zu einer maximalen Tradeanzahl.
- Alert-, Mail- und Benachrichtigungsfunktionen werden ausgelassen; Ereignisbehandlung sollte bei Bedarf extern implementiert werden.

Trotz dieser Anpassungen folgen die zentrale Einstiegslogik und Schutzregeln dem ursprünglichen MetaTrader-Experten eng und nutzen idiomatische StockSharp-Muster.
