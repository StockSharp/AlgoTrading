# Feinabstimmungs-MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht die Steigung eines einfachen gleitenden Durchschnitts. Nach zwei aufeinanderfolgenden Balken in eine Richtung löst eine Richtungsumkehr des gleitenden Durchschnitts einen Einstieg aus. Ein Aufwärtsschwenk nach einem Rückgang eröffnet eine Long-Position, während ein Abwärtsschwenk nach einem Anstieg eine Short-Position eröffnet. Entgegengesetzte Signale schließen bestehende Trades.

Das System wurde aus dem MQL-Expert-Advisor "Exp_FineTuningMA" konvertiert und ersetzt den originalen benutzerdefinierten Indikator durch einen standardmäßigen einfachen gleitenden Durchschnitt für mehr Übersichtlichkeit.

## Details

- **Einstiegskriterien**: MA ändert nach zwei Balken die Richtung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `MaLength` = 10
  - `TakeProfitPercent` = 1
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Swing / H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
