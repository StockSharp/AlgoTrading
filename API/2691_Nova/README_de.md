# Nova-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des MetaTrader 5-Expertenberaters "Nova", der das Kursmomentum über eine feste Anzahl von Sekunden überwacht.
- Funktioniert mit jedem über den Parameter `CandleType` ausgewählten Kerzentyp und wertet die Logik nur bei abgeschlossenen Kerzen aus.
- Verfolgt die besten Kauf- und Verkaufspreise mit Level1-Daten und speichert deren Werte von `SecondsAgo` Sekunden zuvor.
- Eröffnet eine **Long**-Position, wenn die vorherige Kerze bullish ist und der aktuelle Ask höher als der gespeicherte Ask um mindestens `StepPips` ist.
- Eröffnet eine **Short**-Position, wenn die vorherige Kerze bärisch ist und der aktuelle Bid niedriger als der gespeicherte Ask um mindestens `StepPips` ist.
- Wendet automatische Stop-Loss- und Take-Profit-Level unter Verwendung des StockSharp-Schutzes an, wenn die entsprechenden Parameter größer als null sind.
- Nach einem Verlust (Stop-Loss-Aktivierung) wird das Volumen des nächsten Trades mit `LossCoefficient` multipliziert; nach einem profitablen Ausstieg wird das Volumen auf `BaseVolume` zurückgesetzt.

## Parameter
- `SecondsAgo` – Anzahl der Sekunden zwischen dem Referenz-Preis-Snapshot und dem aktuellen Auswertungsmoment.
- `StepPips` – Breakout-Filter in Pips; in Preiseinheiten umgerechnet unter Verwendung des Wertpapierpreisschritts (3/5-Dezimalinstrumente werden mit ×10 angepasst).
- `BaseVolume` – anfängliche Trade-Größe; auf den Börsenvolumenschritt und Min/Max-Limits normalisiert.
- `StopLossPips` – Abstand in Pips für den Schutz-Stop-Loss (0 deaktiviert ihn).
- `TakeProfitPips` – Abstand in Pips für den Schutz-Take-Profit (0 deaktiviert ihn).
- `LossCoefficient` – Multiplikator, der nach einem Verlust-Trade auf das zuletzt ausgeführte Volumen angewendet wird.
- `CandleType` – Kerzenquelle für Signale (Zeitrahmen, Tick, Range usw.).

## Zusätzliche Hinweise
- Die Strategie erfordert Level1-Daten (bester Bid/Ask), um das ursprüngliche MT5-Verhalten zu replizieren; Kerzen stellen einen Fallback mit ihrem Schlusskurs bereit, wenn Level1 nicht verfügbar ist.
- Die Volumen-Neuberechnung respektiert `Security.VolumeStep`, `Security.MinVolume` und `Security.MaxVolume`, um ungültige Orders zu vermeiden.
- Preiskonvertierungen basieren auf `Security.PriceStep` und `Security.Decimals`, damit sich die Strategie sowohl an 4/5-stellige Forex-Symbole als auch an andere Instrumente anpasst.
- Für diese Strategie wird keine Python-Version bereitgestellt.
