# MA-Steilheits-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die MA-Steilheits-Ausbruch-Strategie beobachtet die Änderungsrate der MA. Eine ungewöhnlich steile Neigung deutet darauf hin, dass sich ein neuer Trend bildet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 124%. Am besten funktioniert sie auf dem Forex-Markt.

Einstiege erfolgen, wenn die Steigung ihren typischen Pegel um ein Vielfaches der Standardabweichung überschreitet, wobei Trades in Richtung der Beschleunigung mit einem Schutz-Stop eingegangen werden.

Sie spricht aktive Trader an, die frühzeitig Trendexponierung suchen. Positionen werden geschlossen, wenn die Steigung zu normalen Werten zurückkehrt. Standard `MaLength` = 20.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `MaLength` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

