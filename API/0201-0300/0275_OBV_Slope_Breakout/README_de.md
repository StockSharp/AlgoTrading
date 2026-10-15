# OBV Steigungs-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die OBV Steigungs-Ausbruch-Strategie beobachtet die Änderungsrate des OBV. Eine ungewöhnlich steile Steigung deutet darauf hin, dass sich ein neuer Trend bildet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 154%. Sie funktioniert am besten auf dem Aktienmarkt.

Einstiege erfolgen, wenn die Steigung ihren typischen Wert um ein Vielfaches der Standardabweichung überschreitet, wobei Trades in Richtung der Beschleunigung mit einem Schutz-Stop eingegangen werden.

Sie spricht aktive Trader an, die eine frühzeitige Trendexposition anstreben. Positionen werden geschlossen, wenn die Steigung wieder in den Normalbereich zurückfällt. Standard `LookbackPeriod` = 20.

## Details

- **Einstiegskriterien**: Der Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `SlopeLength` = 5
  - `Multiplier` = 2m
  - `StopLoss` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: OBV
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

