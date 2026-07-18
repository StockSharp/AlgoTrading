# Fractal MFI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Übersetzung des Expert Advisors `Exp_Fractal_MFI.mq5`. Sie verwendet den Money Flow Index (MFI), um Handelssignale zu generieren, wenn der Oszillator vordefinierte obere und untere Niveaus kreuzt.

## Funktionsweise
- Berechnet den MFI über eine konfigurierbare Periode.
- Wenn der vorherige MFI-Wert über dem **Niedrig-Niveau** lag und der aktuelle darunter fällt, wird ein Signal generiert.
  - Im **Direct**-Modus öffnet dies eine Long-Position und schließt optional Shorts.
  - Im **Against**-Modus öffnet dies eine Short-Position und schließt optional Longs.
- Wenn der vorherige MFI-Wert unter dem **Hoch-Niveau** lag und der aktuelle darüber steigt, wird ein weiteres Signal generiert.
  - Im **Direct**-Modus öffnet dies eine Short-Position und schließt optional Longs.
  - Im **Against**-Modus öffnet dies eine Long-Position und schließt optional Shorts.

Nur abgeschlossene Kerzen werden verarbeitet. Die Strategie kann so konfiguriert werden, dass das Öffnen und Schließen von Long- oder Short-Positionen separat aktiviert oder deaktiviert wird.

## Parameter
- `MfiPeriod` – Periode der Money Flow Index-Berechnung.
- `HighLevel` – oberer Schwellenwert für den MFI.
- `LowLevel` – unterer Schwellenwert für den MFI.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.
- `Trend` – `Direct` für den Handel in Indikatorrichtung oder `Against` zum Invertieren der Signale.
- `BuyPosOpen` / `SellPosOpen` – Long- oder Short-Positionen öffnen erlauben.
- `BuyPosClose` / `SellPosClose` – bestehende Positionen bei entgegengesetzten Signalen schließen erlauben.

## Hinweise
Diese C#-Version konzentriert sich auf die High-Level-API-Nutzung und implementiert nicht die originalen Geldmanagement-Regeln oder Stop-Niveaus aus dem MQL-Code.
