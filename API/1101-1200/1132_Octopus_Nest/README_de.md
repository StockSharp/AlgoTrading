# Octopus Nest-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie sucht Squeeze-Ausbrüche mithilfe von Bollinger Bändern und Keltner Kanälen. Die Richtung wird mit EMA und Parabolic SAR bestätigt. Stops werden an jüngsten Hochs/Tiefs mit einem konfigurierbaren Risiko-Ertrags-Verhältnis gesetzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis über EMA und PSAR, außerhalb des Squeeze.
  - **Short**: Preis unter EMA und PSAR, außerhalb des Squeeze.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss an jüngsten Extrempunkten und Take-Profit basierend auf dem Risiko-Ertrags-Verhältnis.
- **Stops**: Ja, fest durch jüngstes Hoch/Tief.
- **Filter**: Bollinger/Keltner-Squeeze, EMA-Trend, PSAR-Richtung.
