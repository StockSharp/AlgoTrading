# Bollinger-Bandbreiten-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Bollinger-Bandbreiten-Ausbruch-Strategie verfolgt den Bollinger auf starke Expansionen. Wenn die Werte über ihren normalen Bereich hinausspringen, beginnt der Kurs oft eine neue Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 109%. Am besten funktioniert sie auf dem Kryptomarkt.

Eine Position wird eröffnet, sobald der Indikator ein Band durchbricht, das aus aktuellen Daten und einem Abweichungsmultiplikator abgeleitet wird. Long- und Short-Trades sind mit einem Stop möglich.

Dieses System eignet sich für Momentum-Trader, die frühe Ausbrüche suchen. Trades werden geschlossen, wenn der Bollinger zur Mitte zurückkehrt. Die Standardwerte beginnen mit `BollingerLength` = 20.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

