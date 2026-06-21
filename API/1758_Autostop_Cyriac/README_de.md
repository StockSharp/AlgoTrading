# Autostop Cyriac-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Hilfsstrategie fügt jedem Trade automatisch einen Take-Profit und einen Stop-Loss hinzu, während sie aktiv ist. Sie erstellt selbst keine Ein- oder Ausstiege und kann mit manuellem Trading oder anderen Strategien kombiniert werden.

## Details

- **Einstiegskriterien**: Keine. Positionen werden manuell oder durch externe Logik eröffnet.
- **Long/Short**: Sowohl Long- als auch Short-Positionen werden unterstützt.
- **Ausstiegskriterien**: Positionen werden durch den angehängten Take-Profit oder Stop-Loss geschlossen.
- **Stops**: Ja. Absolute Preisabstände für Take-Profit und Stop-Loss über `StartProtection`.
- **Filter**: Keine.

Die Strategie bietet zwei Parameter:

- `TakeProfit` – Abstand zum Take-Profit in Preiseinheiten.
- `StopLoss` – Abstand zum Stop-Loss in Preiseinheiten.
