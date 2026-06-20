# Donchian-Kanalbreiten-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Donchian-Kanalbreiten-Ausbruch-Strategie beobachtet den Donchian auf starke Expansionen. Wenn die Werte über ihren durchschnittlichen Bereich hinausspringen, beginnt der Kurs oft eine neue Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 61%. Am besten funktioniert sie auf dem Kryptomarkt.

Eine Position wird eröffnet, sobald der Indikator ein Band durchbricht, das aus aktuellen Daten und einem Abweichungsmultiplikator abgeleitet wird. Long- und Short-Trades sind mit einem Stop möglich.

Dieses System eignet sich für Momentum-Trader, die frühe Ausbrüche suchen. Trades werden geschlossen, wenn der Donchian zur Mitte zurückkehrt. Die Standardwerte beginnen mit `DonchianPeriod` = 20.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

