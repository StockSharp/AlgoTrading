# MACD-Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MACD-Ausbruch-Strategie beobachtet den MACD auf plötzliche Expansionen. Wenn die Werte über ihren normalen Bereich hinausspringen, beginnt der Preis oft eine neue Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 94%. Die Strategie funktioniert am besten am Aktienmarkt.

Eine Position wird eröffnet, sobald der Indikator ein Band durchbricht, das aus aktuellen Daten und einem Abweichungsmultiplikator abgeleitet wird. Long- und Short-Trades sind mit einem Stop möglich.

Dieses System eignet sich für Momentum-Trader, die frühe Ausbrüche suchen. Trades schließen, wenn der MACD zur Mitte zurückkehrt. Standardwerte beginnen mit `FastEmaPeriod` = 12.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `SmaPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
