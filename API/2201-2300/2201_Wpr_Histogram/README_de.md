# WPR-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des Verhaltens des Williams %R-Indikators. Sie überwacht, wann der Indikator überkaufte oder überverkaufte Zonen verlässt, und eröffnet Positionen in der entgegengesetzten Richtung.

## Logik

- Wenn Williams %R über das hohe Niveau steigt und dann wieder fällt, wird dies als Signal gewertet, dass der Markt die überkaufte Zone verlässt. Die Strategie eröffnet eine Long-Position.
- Wenn Williams %R unter das niedrige Niveau fällt und dann wieder steigt, verlässt der Markt die überverkaufte Zone. Die Strategie eröffnet eine Short-Position.
- Bestehende entgegengesetzte Positionen werden vor dem Öffnen einer neuen Position geschlossen.

## Parameter

- **WPR Period** – Berechnungszeitraum für Williams %R.
- **High Level** – Schwellenwert für die überkaufte Zone.
- **Low Level** – Schwellenwert für die überverkaufte Zone.
- **Candle Type** – Typ und Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Hinweise

Die Strategie verwendet ausschließlich Marktaufträge und setzt keine Stop-Loss- oder Take-Profit-Niveaus. Die Positionsgröße wird durch die benutzerdefinierte `Volume`-Eigenschaft bestimmt.
