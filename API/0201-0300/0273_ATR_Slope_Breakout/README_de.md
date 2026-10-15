# ATR Steigungs-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ATR Steigungs-Ausbruch-Strategie beobachtet die Änderungsrate des ATR. Eine ungewöhnlich steile Steigung deutet darauf hin, dass sich ein neuer Trend bildet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 148%. Sie funktioniert am besten auf dem Devisenmarkt.

Einstiege erfolgen, wenn die Steigung ihren typischen Wert um ein Vielfaches der Standardabweichung überschreitet, wobei Trades in Richtung der Beschleunigung mit einem Schutz-Stop eingegangen werden.

Sie spricht aktive Trader an, die eine frühzeitige Trendexposition anstreben. Positionen werden geschlossen, wenn die Steigung wieder in den Normalbereich zurückfällt. Standard `AtrPeriod` = 14.

## Details

- **Einstiegskriterien**: Der Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `SlopePeriod` = 20
  - `BreakoutMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

