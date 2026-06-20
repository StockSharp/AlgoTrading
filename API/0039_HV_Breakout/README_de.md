# Historical Volatility Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Ausbruchmethode verwendet historische Volatilität, um dynamische Schwellenwerte festzulegen. Wenn sich der Preis über einen Referenzpegel hinaus um mehr als die aktuelle Volatilität bewegt, deutet das auf einen potenziellen Trend hin.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 154%. Es funktioniert am besten auf dem Aktienmarkt.

Die Strategie vergleicht den Preis mit Niveaus, die aus Standardabweichung und einem einfachen gleitenden Durchschnitt abgeleitet werden. Ausbrüche über oder unter diesen Niveaus lösen Trades aus.

Ausstiege erfolgen, wenn der Preis zurück durch den gleitenden Durchschnitt kreuzt oder der Stop ausgelöst wird.

## Details

- **Einstiegskriterien**: Preis bricht über oder unter das HV-basierte Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt MA oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `HvPeriod` = 20
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: HV, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

