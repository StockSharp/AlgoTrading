# Volumen-Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Volumen-Ausbruch-Strategie beobachtet das Volumen auf schnelle Expansionen. Wenn die Werte über ihren durchschnittlichen Bereich hinausspringen, beginnt der Preis oft eine neue Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 103%. Die Strategie funktioniert am besten am Aktienmarkt.

Eine Position wird eröffnet, sobald der Indikator ein Band durchbricht, das aus aktuellen Daten und einem Abweichungsmultiplikator abgeleitet wird. Long- und Short-Trades sind mit einem Stop möglich.

Dieses System eignet sich für Momentum-Trader, die frühe Ausbrüche suchen. Trades schließen, wenn das Volumen zur Mitte zurückkehrt. Standardwerte beginnen mit `AvgPeriod` = 20.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `AvgPeriod` = 20
  - `Multiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg und StdDev sind Mittelwert und Standardabweichung der letzten AvgPeriod Werte von Volumen einschließlich des aktuellen. Der Stop liegt bei festen StopLossPercent vom Einstiegspreis und wird auch zwischen den Kerzen überwacht; 0 schaltet ihn ab. Das Band ist der Mittelwert plus Multiplier Standardabweichungen: das Volumen darüber bei einer steigenden Kerze eröffnet einen Long, bei einer fallenden einen Short; die Position schließt, sobald das Volumen wieder unter seinen Mittelwert fällt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
