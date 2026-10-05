# Upthrust Reversal Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Upthrust Reversal ist das bearische Gegenstück zum Spring und tritt auf, wenn der Preis kurz über den Widerstand bricht, aber schnell wieder zurückfällt.
Die Bewegung schwächt späte Käufer aus, bevor sie nach unten dreht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 58%. Die Strategie funktioniert am besten am Aktienmarkt.

Diese Strategie geht short, sobald der Preis wieder unter das Ausbruchniveau fällt, in Erwartung, dass das Angebot die Nachfrage überwiegt.

Ein Stop knapp über dem Upthrust-Hoch verwaltet das Risiko, und Positionen werden beendet, wenn der Preis über dieses Niveau zurückkehrt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2
    Der Widerstand ist das höchste Hoch der vorherigen LookbackPeriod Kerzen. Die Strategie verkauft nur: eine bärische Kerze, die den Widerstand überschreitet und wieder darunter schließt. Der Stop liegt StopLossPercent über dem Upthrust-Hoch, und die Position schließt, wenn eine Kerze darüber schließt.
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Wyckoff
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

