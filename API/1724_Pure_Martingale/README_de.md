# Reine Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein einfaches Martingale-System. Sie eröffnet Trades in einer zufälligen Richtung und verdoppelt nach jedem Verlust-Trade die Positionsgröße sowie die Stop/Take-Distanz. Nach einem Gewinn-Trade wird auf das ursprüngliche Volumen und die ursprüngliche Distanz zurückgesetzt.

Der Ansatz setzt darauf, dass der Preis letztendlich zur Profitabilität zurückkehrt, aber das Risiko wächst exponentiell. Nur auf liquiden Instrumenten mit engen Spreads verwenden.

## Details

- **Einstiegskriterien**:
  - Keine offene Position: zufällig kaufen oder verkaufen beim Kerzenschluss.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Schließen, wenn sich der Preis um die konfigurierte Distanz zugunsten oder gegen die Position bewegt.
- **Stops**: Virtuelle Stop-Loss- und Take-Profit-Aufträge, die von der Strategie verwaltet werden.
- **Filter**:
  - Keine.
