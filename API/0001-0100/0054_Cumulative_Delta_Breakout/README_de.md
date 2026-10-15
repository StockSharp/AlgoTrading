# Kumulativer Delta-Ausbruch (Cumulative Delta Breakout)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Cumulative Delta summiert die Differenz zwischen Kauf- und Verkaufsvolumen. Diese Strategie überwacht den laufenden Gesamtwert und handelt, wenn er seinen höchsten Wert überschreitet oder seinen niedrigsten Wert innerhalb des Rückblickzeitraums unterschreitet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 49 %. Die Strategie eignet sich am besten für den Kryptomarkt.

Ein Ausbruch des kumulativen Deltas geht oft einer Kursbewegung voraus. Die Strategie schließt Trades, wenn das Delta wieder durch null kreuzt oder ein Stop-Loss-Level erreicht wird.

## Details

- **Einstiegskriterien**: Das kumulative Delta überschreitet den höchsten oder unterschreitet den niedrigsten Wert im Rückblickzeitraum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Das Delta kreuzt null oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Cumulative Delta
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
