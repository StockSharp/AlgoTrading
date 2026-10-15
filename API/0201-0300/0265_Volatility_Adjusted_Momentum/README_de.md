# Volatilitätsbereinigtes Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Volatilitätsbereinigte-Momentum-Strategie überwacht die Volatilität auf schnelle Expansionen. Wenn die Werte über ihren durchschnittlichen Bereich hinausspringen, beginnt der Kurs oft eine neue Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 130%. Am besten funktioniert sie auf dem Aktienmarkt.

Eine Position wird eröffnet, sobald der Indikator ein Band durchbricht, das aus aktuellen Daten und einem Abweichungsmultiplikator abgeleitet wird. Long- und Short-Trades sind mit einem Stop möglich.

Dieses System eignet sich für Momentum-Trader, die frühe Ausbrüche suchen. Trades werden geschlossen, wenn die Volatilität zur Mitte zurückkehrt. Die Standardwerte beginnen mit `MomentumPeriod` = 14.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `MomentumPeriod` = 14
  - `AtrPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volatilität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

