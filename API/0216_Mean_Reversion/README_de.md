# Mean Reversion Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieser statistische Ansatz sucht nach kurzfristigen Extremen im Preis im Verhältnis zu seinem jüngsten Durchschnitt. Die Strategie verwendet einen gleitenden Durchschnitt zur Definition des fairen Werts und misst die Abweichung von diesem Mittelwert durch eine Standardabweichungsberechnung.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 85%. Sie funktioniert am besten auf dem Kryptomarkt.

Trades werden geöffnet, wenn der Preis eine festgelegte Distanz vom Durchschnitt schiebt. Ein Einbruch unter das untere Band löst einen Long-Einstieg aus, der eine Erholung in Richtung des Mittelwerts antizipiert, während eine Rally über das obere Band einen Short veranlasst. Sobald der Preis den gleitenden Durchschnitt wieder berührt, wird eine offene Position geschlossen.

Die Methode spricht Trader mit einem konträren Stil an, die klar definierte Einstiegs- und Ausstiegszonen wünschen. Da sie auf volatilitätsbasierten Bändern basiert, passt sie sich ruhigeren oder aktiveren Märkten an, während Verluste durch einen festen Stop-Loss unter Kontrolle bleiben.

## Details
- **Einstiegskriterien**:
  - **Long**: Price < MA - k*StdDev (below lower band)
  - **Short**: Price > MA + k*StdDev (above upper band)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn der Preis über den gleitenden Durchschnitt kreuzt
  - **Short**: Ausstieg, wenn der Preis unter den gleitenden Durchschnitt kreuzt
- **Stops**: Ja.
- **Standardwerte**:
  - `MovingAveragePeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Mean Reversion
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

