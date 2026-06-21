# Trend Trader Remastered-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Parabolic-SAR-Indikator, um Trends zu folgen. Eine Kauforder wird gesendet, wenn der Preis über den SAR kreuzt, und eine Verkaufsorder, wenn der Preis darunter kreuzt. Ein entgegengesetzter Kreuzungspunkt schließt die aktuelle Position.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis kreuzt über PSAR.
  - **Short**: Preis kreuzt unter PSAR.
- **Ausstiege**: Entgegengesetzter PSAR-Kreuzungspunkt schließt den Trade.
- **Stops**: Keine zusätzlichen Stops.
- **Standardwerte**:
  - `Start` = 0.02
  - `Increment` = 0.02
  - `Max` = 0.2
