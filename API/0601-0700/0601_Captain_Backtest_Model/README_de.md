# Captain-Backtest-Modell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verfolgt die frühe Sitzungspreisrange, um eine tägliche Richtungsneigung zu bestimmen. Handelt Ausbrüche während des Handelsfensters nach einer Korrektur.

## Details

- **Richtungsneigung**: Das Hoch oder Tief der Morgenrange bestimmt die Long- oder Short-Neigung.
- **Einstieg**: Ausbruch über/unter der vorherigen Kerze, sobald die Korrektuربedingungen erfüllt sind.
- **Long/Short**: Beide Richtungen.
- **Ausstieg**: Festes Risiko/Ertrag-Verhältnis oder Ende des Handelsfensters.
- **Stops**: Fester Punktabstand.
- **Standardwerte**:
  - PrevRangeStart = 06:00
  - PrevRangeEnd = 10:00
  - TakeStart = 10:00
  - TakeEnd = 11:15
  - TradeStart = 10:00
  - TradeEnd = 16:00
  - Risk = 25
  - Reward = 75
