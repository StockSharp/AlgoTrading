# Strategie AK47 A1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port des Experten „AK47_A1“ MetaTrader. Die Strategie kombiniert Bill Williams' Alligator, DeMarker-Oszillator, Williams %R-Filter und fraktale Trigger, um Ausbrüche nur dann zu handeln, wenn der Markt schwankende Bedingungen verlässt.

## Einzelheiten
- **Daten**: Preiskerzen definiert durch `CandleType`.
- **Indikatoren**:
  - Alligator Kiefer/Zähne/Lippen sind 13/8/5-Perioden-SMMAs, die um 8/5/3 Balken verschoben und mit dem Medianpreis gefüttert werden.
  - Der DeMarker mit der Periode 13 muss für Käufe auf der Long-Seite von 0,5 und für Verkäufe unter 0,5 liegen.
  - Williams %R mit Periode 14 wird auf `[0;1]` normalisiert; Der vorherige Balken muss zwischen 0,25 und 0,75 bleiben, um überkaufte/überverkaufte Zustände zu vermeiden.
  - Fractals werden aus den letzten 5 Hochs und Tiefs erkannt und bleiben drei Balken lang gültig.
- **Eintrittskriterien**:
  - Alle drei Alligator-Linien müssen mindestens `SpanGatorPoints` Punkte voneinander entfernt sein (sowohl bei bullischer als auch bei bärischer Ausrichtung).
  - **Lang**: Das jüngste untere Fraktal ist frisch, DeMarker ≥ 0,5 und der Williams %R-Filter genehmigt den Handel.
  - **Short**: Das neueste obere Fraktal ist frisch, DeMarker ≤ 0,5 und der Williams %R-Filter genehmigt den Handel.
  - Gegenüberliegende Positionen werden abgeflacht, bevor eine neue geöffnet wird.
- **Ausstiegskriterien**:
  - Harter Stop-Loss und Take-Profit, definiert durch `StopLossPoints` und `TakeProfitPoints` (über den Instrumentenschritt in absolute Preise umgerechnet).
  - Optionaler Trailing-Stop, der den Schlusskurs um `TrailingStopPoints` Punkte verfolgt, sobald sich die Position positiv bewegt.
  - Wenn ein Rückwärtssignal erscheint, wird die aktuelle Position geschlossen, bevor die neue geöffnet wird.
- **Standardeinstellungen**:
  - `SpanGatorPoints` = 0,5
  - `TakeProfitPoints` = 100
  - `StopLossPoints` = 0 (deaktiviert)
  - `TrailingStopPoints` = 50
  - `CandleType` = 1-Stunden-Kerzen
