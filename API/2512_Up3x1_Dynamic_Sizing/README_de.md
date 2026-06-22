# Up3x1 Strategie mit dynamischer Positionsgrößenbestimmung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 5 Expert Advisors `up3x1.mq5` in die StockSharp High-Level-API.
- Handelt einen Triple-EMA-Crossover (exponentieller gleitender Durchschnitt) mit Stop-Loss-, Take-Profit- und Trailing-Stop-Management.
- Verarbeitet nur abgeschlossene Kerzen, um den ursprünglichen `iTickVolume(0) > 1`-Schutz zu emulieren, der eine Entscheidung pro Bar erzwang.
- Standard-Kerzenserie ist 1 Stunde, aber der Zeitrahmen ist über den `CandleType`-Parameter konfigurierbar.

## Handelslogik
1. **Indikatoren**
   - Schnelle EMA (`FastPeriod`, Standard 24).
   - Mittlere EMA (`MediumPeriod`, Standard 60).
   - Langsame EMA (`SlowPeriod`, Standard 120).
2. **Long-Einstieg**
   - Vorherige Bar: schnelle EMA unterhalb der mittleren EMA und mittlere unterhalb der langsamen (`EMAfast₍t-1₎ < EMAmedium₍t-1₎ < EMAslow₍t-1₎`).
   - Aktuelle Bar: mittlere EMA unterhalb der schnellen EMA, während die schnelle unterhalb der langsamen bleibt (`EMAmedium₍t₎ < EMAfast₍t₎ < EMAslow₍t₎`).
3. **Short-Einstieg**
   - Vorherige Bar: schnelle EMA oberhalb der mittleren EMA und mittlere oberhalb der langsamen (`EMAfast₍t-1₎ > EMAmedium₍t-1₎ > EMAslow₍t-1₎`).
   - Aktuelle Bar: mittlere EMA kreuzt über die schnelle EMA, während beide über der langsamen EMA bleiben (`EMAmedium₍t₎ > EMAfast₍t₎ > EMAslow₍t₎`).
4. **Ausstiegslogik für beide Richtungen**
   - Take Profit, wenn der Preis `TakeProfitOffset` vom Einstieg voranzieht (Kerzenhoch für Longs, -tief für Shorts).
   - Stop Loss, wenn der Preis `StopLossOffset` vom Einstieg zurückzieht (Kerzentief für Longs, -hoch für Shorts).
   - Trailing Stop aktiviert sich, sobald sich die Position um mehr als `TrailingStopOffset` günstig bewegt, und folgt dem Preis in diesem festen Abstand, bewertet an Kerzenextremen.
   - Fallback-Ausstieg, wenn die schnelle EMA wieder unter die mittlere EMA kreuzt, während beide über der langsamen EMA bleiben (spiegelt die `ma_one_1 > ma_two_1 > ma_three_1`-Prüfung der MQL-Version wider).

## Positionsgrößenbestimmung und Risikomanagement
- `RiskFraction` (Standard 0.02) multipliziert den aktuellen Portfoliowert, um die ursprüngliche `FreeMargin * 0.02 / 1000`-Lot-Größenbestimmung zu approximieren.
- `BaseVolume` (Standard 0.1) dient als Fallback, wenn Portfolio-Daten nicht verfügbar sind oder die berechnete Größe nicht positiv wird.
- Nach mehr als einem verlierenden Ausstieg wird das Volumen um `volume * losses / 3` reduziert, was den kumulativen `losses`-Zähler des Skripts imitiert (der Zähler wird nach profitablen Trades nicht zurückgesetzt, wie im Originalcode).
- Volumen wird auf `Security.VolumeStep` abgerundet, durch `Security.MinVolume` / `Security.MaxVolume` begrenzt und auf null gesetzt, wenn das Instrumentminimum nicht erfüllt werden kann.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|--------------|
| `FastPeriod` | 24 | Länge der schnellsten EMA. |
| `MediumPeriod` | 60 | Länge der mittleren EMA. |
| `SlowPeriod` | 120 | Länge der langsamen EMA als Langzeittrend-Filter. |
| `TakeProfitOffset` | 0.015 | Absoluter Preisabstand für die Take-Profit-Order (an Instrument-Quotierung anpassen). |
| `StopLossOffset` | 0.01 | Absoluter Preisabstand für die Stop-Loss-Order. |
| `TrailingStopOffset` | 0.004 | Trailing-Distanz, die Gewinne sichert, sobald der Preis ausreichend voranzieht; auf 0 setzen zum Deaktivieren. |
| `BaseVolume` | 0.1 | Fallback-Handelsgröße, wenn dynamische Größenbestimmung nicht berechnet werden kann. |
| `RiskFraction` | 0.02 | Anteil des Portfoliowerts, der auf die dynamische Größenformel angewendet wird. |
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzenserie für Indikatorberechnungen und Entscheidungsfindung. |

## Konvertierungshinweise
- Trailing Stop und Schutzausstiege verwenden Kerzenhochs/-tiefs statt roher Ticks, da die High-Level-API abgeschlossene Kerzen verarbeitet; dies hält das Verhalten über Backtests und Live-Runs deterministisch.
- Stop Loss und Take Profit werden über Markt-Flattening-Befehle am bewerteten Schwellenwert ausgeführt statt durch separate Schutzorders, was Kompatibilität mit dem High-Level-Strategiefluss sicherstellt.
- Dynamische Positionsgrößenbestimmung hängt von `Portfolio.CurrentValue` ab. Wenn nicht verfügbar, fällt die Strategie auf `BaseVolume` zurück, ähnlich dem ursprünglichen `LotCheck`-Fallback auf den manuellen `Lots`-Eingang.
- Der `losses`-Zähler ist absichtlich kumulativ (wird nie bei gewinnenden Trades zurückgesetzt), um der MQL-Implementierung zu folgen.
- Alle Kommentare sind auf Englisch gemäß Projektrichtlinien.

## Verwendungstipps
1. Hängen Sie die Strategie an ein Instrument und Portfolio, dann konfigurieren Sie `CandleType` entsprechend der Chart-Auflösung, die Sie von MT5 emulieren möchten.
2. Überprüfen Sie die Preisoffsets, damit sie die Tick-Größe Ihres Instruments widerspiegeln (z.B. entspricht für ein 5-stelliges Forex-Paar 0.015 150 Punkten wie im Quell-Expert).
3. Stimmen Sie `RiskFraction` / `BaseVolume` ab, um realistische Positionsgrößen relativ zu Ihrem Konto zu erreichen.
4. Optional: Trailing deaktivieren durch Setzen von `TrailingStopOffset` auf null.
5. Überwachen Sie Protokolle auf Nachrichten wie "Enter long" oder "Exit short", die die MetaTrader `Print`-Diagnosen spiegeln.

## Repository-Struktur
```
API/2512_Up3x1/
├── CS/Up3x1DynamicSizingStrategy.cs      # Konvertierte C#-Strategie
├── README.md                # Englische Dokumentation (diese Datei)
├── README_zh.md             # Chinesische Übersetzung
└── README_ru.md             # Russische Übersetzung
```

## Haftungsausschluss
Der Handel birgt erhebliche Risiken. Dieses Beispiel dient nur zu Bildungszwecken und sollte an historischen und simulierten Daten validiert werden, bevor es live eingesetzt wird.
