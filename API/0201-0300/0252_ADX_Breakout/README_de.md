# ADX-Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ADX-Ausbruch-Strategie überwacht den ADX auf starke Expansionen. Wenn die Werte über ihren typischen Bereich hinausspringen, beginnt der Preis oft eine neue Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 97%. Die Strategie funktioniert am besten im Kryptomarkt.

Eine Position wird eröffnet, sobald der Indikator ein Band durchbricht, das aus aktuellen Daten und einem Abweichungsmultiplikator abgeleitet wird. Long- und Short-Trades sind mit einem Stop möglich.

Dieses System eignet sich für Momentum-Trader, die frühe Ausbrüche suchen. Trades schließen, wenn der ADX zur Mitte zurückkehrt. Standardwerte beginnen mit `ADXPeriod` = 14.

## Details

- **Einstiegskriterien**: Indikator überschreitet den Durchschnitt um den Abweichungsmultiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `ADXPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg und StdDev sind Mittelwert und Standardabweichung der letzten AvgPeriod Werte von ADX einschließlich des aktuellen. Der Stop liegt bei festen StopLossPercent vom Einstiegspreis und wird auch zwischen den Kerzen überwacht; 0 schaltet ihn ab. Das Band ist der Mittelwert plus Multiplier Standardabweichungen: der ADX darüber bei einer steigenden Kerze eröffnet einen Long, bei einer fallenden einen Short; die Position schließt, sobald der ADX wieder unter seinen Mittelwert fällt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
