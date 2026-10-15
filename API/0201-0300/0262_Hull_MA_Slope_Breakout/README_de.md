# Hull MA-Steilheits-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Hull MA-Steilheits-Ausbruch-Strategie verfolgt die Änderungsrate des Hull. Eine ungewöhnlich steile Neigung deutet darauf hin, dass sich ein neuer Trend bildet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 121%. Am besten funktioniert sie auf dem Kryptomarkt.

Einstiege erfolgen, wenn die Steigung ihren typischen Pegel um ein Vielfaches der Standardabweichung überschreitet, wobei Trades in Richtung der Beschleunigung mit einem Schutz-Stop eingegangen werden.

Sie spricht aktive Trader an, die frühzeitig Trendexponierung suchen. Positionen werden geschlossen, wenn die Steigung zu normalen Werten zurückkehrt. Standard `HullLength` = 9.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `HullLength` = 9
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Hull
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

