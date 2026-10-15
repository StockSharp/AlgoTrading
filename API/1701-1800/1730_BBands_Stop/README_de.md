# BBands-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den BBands-Stop-Indikator, der aus Bollinger-Bändern abgeleitet ist, um Markttrends zu folgen. Wenn die Stop-Linie nach oben wechselt, wird eine Short-Position geschlossen und eine Long-Position eröffnet. Ein Abwärtswechsel schließt Long-Positionen und eröffnet Short-Positionen. Parameter steuern die Bollinger-Periode, die Abweichung, den Risiko-Offset und die Berechtigungen für das Ein- oder Aussteigen aus Long- und Short-Positionen.

## Details

- **Einstiegskriterien**:
  - **Long**: Die Aufwärtstrend-Stop-Linie ist aktiv.
  - **Short**: Die Abwärtstrend-Stop-Linie ist aktiv.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetztes Stop-Signal.
- **Stops**: Trailing Stop aus Bollinger-Bändern.
- **Filter**: Keine.
