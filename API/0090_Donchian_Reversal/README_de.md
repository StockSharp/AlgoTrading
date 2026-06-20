# Donchian-Kanal-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Donchian Channels markieren die jüngsten Hochs und Tiefs über einen gewählten Zeitraum. Preise, die diese Grenzen durchbrechen und dann umkehren, können auf Erschöpfung hinweisen. Diese Strategie beobachtet Schlusskurse, die nach einem kurzen Ausbruch wieder in den Kanal zurückkehren.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 157%. Am besten funktioniert die Strategie auf dem Kryptomarkt.

Wenn der vorherige Schlusskurs unterhalb des unteren Bandes lag und der aktuelle Schlusskurs wieder darüber steigt, wird ein Long-Trade eingegangen. Umgekehrt, wenn der vorherige Schlusskurs oberhalb des oberen Bandes war und der Preis wieder innerhalb fällt, wird ein Short eröffnet. In beiden Fällen verwaltet ein prozentualer Stop das Risiko.

Indem nur nach einem gescheiterten Ausbruch gehandelt wird, versucht dieser Ansatz, falsche Bewegungen zu erfassen, die sich schnell wieder zurückziehen.

## Details

- **Einstiegskriterien**: Preis schließt nach dem Durchbrechen des oberen oder unteren Bandes wieder innerhalb des Donchian Channel.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `Period` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Donchian Channel
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

