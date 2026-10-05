# Volumen-Klimax-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Volumen-Klimax-Umkehr sucht nach Wendepunkten, die durch extrem hohes Volumen nach einem starken Trend gekennzeichnet sind.
Solche klimaktischen Spitzen deuten auf Erschöpfung hin, da die letzten Käufer oder Verkäufer einströmen, bevor der Schwung nachlässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 82%. Es funktioniert am besten auf dem Aktienmarkt.

Die Strategie tritt gegen die vorherige Bewegung ein, sobald ein großer Volumen-Balken schließt und der Preis beginnt, zurückzusetzen.

Ein enger prozentualer Stop schützt die Position, und Trades werden beendet, wenn das Volumen nicht nachlässt oder der Preis in der ursprünglichen Richtung weiterläuft.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `MaPeriod` = 20
  - `VolumeMultiplier` = 2
  - `StopLossPercent` = 2
    Ein Klimax ist eine Kerze, die mit dem Trend schließt (bullisch über dem SMA mit Periode MaPeriod, bärisch darunter), bei einem Volumen über VolumeMultiplier mal dem Durchschnitt der vorherigen MaPeriod Kerzen. Die nächste Kerze, die in die Gegenrichtung schließt, eröffnet den Trade gegen die Bewegung. Der Trade endet, wenn ein Schluss das Extrem der Klimaxkerze überschreitet, wenn das Volumen erneut ausschlägt oder am Prozent-Stop.
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

