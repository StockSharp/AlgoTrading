# Strategie 3103 — ADX EA (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Der originale MetaTrader "ADX EA" kombiniert Average Directional Index-Ausbrüche mit +DI/−DI-Kreuzungen, Momentum-Bestätigung
auf einem höheren Zeitrahmen und einem monatlichen MACD-Filter. Der C#-Port repliziert diesen Multi-Filter-Workflow auf der
StockSharp High-Level-API. Die Strategie abonniert drei Kerzendatenströme:

1. **Primärer Zeitrahmen** (Standard 5 Minuten) — treibt ADX, lineare gewichtete gleitende Durchschnitte, Preisstruktur-
   prüfungen und Volumenfilter an.
2. **Momentum-Zeitrahmen** (Standard 15 Minuten) — erzeugt die Momentum-Abweichungen um die 100-Basislinie, die Einträge
   kontrollieren.
3. **MACD-Zeitrahmen** (Standard 30 Tage) — spiegelt den monatlichen MACD wider, der Positionsausstiege steuert.

## Handelslogik
- **Ausbruchsmodul** – Wenn aktiviert, erfordern Long-Trades:
  - ADX oder +DI über `EntryLevel` und die Lücke zwischen +DI und −DI größer als `MinDirectionalDifference`.
  - Die schnelle LWMA über der langsamen LWMA, bullische Kerzenstruktur (`Low[2] < High[1]`) und wachsendes Momentum
    (`Momentum[1] > Momentum[2]`).
  - Mindestens eine der letzten drei Momentum-Ablesungen auf dem höheren Zeitrahmen weicht von 100 um mehr als
    `MomentumBuyThreshold` ab.
  - Steigendes Volumen auf dem primären Zeitrahmen (`Volume[1] > Volume[2]` oder `Volume[1] > Volume[3]`).
  - MACD auf dem monatlichen Zeitrahmen bullisch (`MacdMain[1] > MacdSignal[1]`).
  - ADX über `ExitLevel` zur Bestätigung der allgemeinen Trendstärke.

  Short-Ausbrüche wenden die symmetrische Logik mit −DI-Dominanz, bärischer Struktur (`Low[1] < High[2]`), Momentum unter 100
  um `MomentumSellThreshold` und einem bärischen MACD-Vergleich an.

- **Kreuzungsmodul** – Wenn aktiv, sucht nach +DI-Kreuzung über −DI (Longs) oder −DI-Kreuzung über +DI (Shorts). Optionale
  Filter spiegeln den Original-EA wider:
  - `RequireAdxSlope` erfordert, dass ADX höher als die vorherige Ablesung ist.
  - `ConfirmCrossOnBreakout` fügt dieselben Ausbruchsschwellenprüfungen auf der Kreuzungsbar hinzu.
  - `MinAdxMainLine` erzwingt eine Mindest-ADX-Stärke während der Kreuzung.
  - LWMA-Ausrichtung, Momentum-Steigung, Volumenexpansion und MACD-Polarität müssen noch mit der beabsichtigten Richtung
    übereinstimmen.

- **Pyramidisierung** – Jede neue Order fügt Volumen gemäß `LotExponent` hinzu. Die Strategie behandelt `TradeVolume` als
  Basis-Lotgröße und erhöht sie um `LotExponent^n`, wobei `n` die Anzahl der bereits geöffneten Stufen ist. `MaxTrades`
  begrenzt die Menge des Netto-Volumens, das angesammelt werden kann.

## Risikomanagement
- **Schutzaufträge** – `TakeProfitSteps` und `StopLossSteps` werden an `StartProtection` übergeben und in Preisschritten des
  Instruments ausgedrückt.
- **Trailing Stop** – `TrailingStopSteps` pflegt eine manuelle Trailing-Barriere jenseits des besten Schlusskurses.
- **Break-Even** – Wenn `UseBreakEven` aktiviert ist, wird der Stop nach Preisfortschritt von `BreakEvenTrigger` Schritten
  angezogen und kann den Stop um `BreakEvenOffset` Schritte versetzen.
- **MACD-Ausstieg** – Wenn `EnableMacdExit` wahr ist, schließt die monatliche MACD-Beziehung Longs, wenn MACD unter sein
  Signal fällt (und umgekehrt für Shorts), passend zu den `Close_BUY`/`Close_SELL`-Routinen des EA.
- **Eigenkapital-Stop** – `UseEquityStop` verfolgt die Kurve des schwebenden Gewinns und liquidiert Positionen, sobald der
  Drawdown `TotalEquityRisk` Prozent erreicht.

Funktionen, die auf Kontowährungszielen basierten ("Take Profit in Money", "Trailing Profit in Money" usw.) sind nicht portiert,
da StockSharp-Strategien typischerweise die Schutzlogik durch Stop-Abstände und den eingebauten Schutzdienst verwalten. Alle
anderen Entscheidungspunkte des EA werden mit Indikator-Äquivalenten beibehalten.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `TradeVolume` | 0.01 | Basis-Lotgröße für den ersten Eintrag. |
| `CandleType` | 5m-Zeitrahmen | Primäre Kerzenreihe für ADX/LWMA-Logik. |
| `MomentumCandleType` | 15m-Zeitrahmen | Höherer Zeitrahmen für den Momentum-Abweichungsfilter. |
| `MacdCandleType` | 30-Tage-Zeitrahmen | Zeitrahmen, der den MACD-Ausstiegsfilter speist. |
| `FastMaPeriod` | 6 | Schnelle lineare gewichtete gleitende Durchschnittslänge. |
| `SlowMaPeriod` | 85 | Langsame lineare gewichtete gleitende Durchschnittslänge. |
| `AdxPeriod` | 14 | Average Directional Index-Periode. |
| `MomentumPeriod` | 14 | Momentum-Indikatorperiode auf dem höheren Zeitrahmen. |
| `MacdFastPeriod` | 12 | Schnelle EMA-Periode im MACD-Ausstiegsfilter. |
| `MacdSlowPeriod` | 26 | Langsame EMA-Periode im MACD-Ausstiegsfilter. |
| `MacdSignalPeriod` | 9 | Signal-SMA-Periode im MACD-Ausstiegsfilter. |
| `EnableBreakoutStrategy` | true | Umschalter für den ADX-Ausbruchszweig. |
| `EnableCrossStrategy` | true | Umschalter für den DI-Kreuzungszweig. |
| `UseTrendFilter` | true | Erzwingt +DI-Dominanz für Longs und −DI-Dominanz für Shorts bei Ausbrüchen. |
| `RequireAdxSlope` | true | Erfordert ADX-Anstieg bei der Bewertung von DI-Kreuzungen. |
| `ConfirmCrossOnBreakout` | true | Fügt Ausbruchsschwellen zum Kreuzungsmodul hinzu. |
| `EnableMacdExit` | true | Aktiviert die MACD-basierte Ausstiegsroutine. |
| `EntryLevel` | 10 | Minimales ADX/+DI/−DI-Niveau für Ausbrüche. |
| `ExitLevel` | 10 | Minimale ADX-Stärke, die neue Einträge erlaubt. |
| `MinDirectionalDifference` | 10 | Erforderliche Lücke zwischen +DI und −DI. |
| `MinAdxMainLine` | 10 | Minimales ADX-Niveau bei DI-Kreuzungen. |
| `MomentumBuyThreshold` | 0.3 | Erforderliche Abweichung von 100 für bullische Momentum-Bestätigung. |
| `MomentumSellThreshold` | 0.3 | Erforderliche Abweichung von 100 für bärische Momentum-Bestätigung. |
| `MaxTrades` | 10 | Maximale Anzahl von Pyramidisierungsstufen. |
| `LotExponent` | 1.44 | Volumen-Multiplikator für jede zusätzliche Stufe. |
| `TakeProfitSteps` | 50 | Abstand in Preisschritten für den Take-Profit-Auftrag. |
| `StopLossSteps` | 20 | Abstand in Preisschritten für den Stop-Loss-Auftrag. |
| `TrailingStopSteps` | 40 | Manueller Trailing-Stop-Abstand in Preisschritten. |
| `UseBreakEven` | true | Aktiviert die Break-Even-Umsetzungslogik. |
| `BreakEvenTrigger` | 30 | Schritte günstiger Bewegung, die vor dem Aktivieren von Break-Even erforderlich sind. |
| `BreakEvenOffset` | 30 | Zusätzliche Schritte, die beim Verschieben des Stops zum Eintrittspreis hinzugefügt werden. |
| `UseEquityStop` | true | Aktiviert den drawdown-basierten Notausstieg. |
| `TotalEquityRisk` | 1 | Erlaubter prozentualer Drawdown vor dem Schließen aller Positionen. |

## Verwendungshinweise
- Richten Sie `MomentumCandleType` und `MacdCandleType` auf Ihren primären Zeitrahmen aus, um das ursprüngliche Zeitrahmen-
  Mapping nachzuahmen (z. B. 5-Minuten-Chart → 15-Minuten-Momentum → monatlicher MACD).
- Stimmen Sie `EntryLevel`, `MinDirectionalDifference` und `MinAdxMainLine` zusammen ab; alle drei zu senken lockert den
  Ausbruchsfilter erheblich.
- `LotExponent` größer als 1.0 repliziert die martingalähnliche Skalierung des EA. Setzen Sie ihn auf 1.0, um die
  Positionsgrößen konstant zu halten.
