# Loco-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den "Loco"-Indikator, der ursprünglich in MQL5 geschrieben wurde. Der Indikator analysiert Kerzenpreise und weist eine Farbe zu (grün oder magenta). Ein Farbwechsel signalisiert eine Trendumkehr.

## Logik
- Der Indikator berechnet eine Reihe anhand eines konfigurierbaren Preises (standardmäßig Schlusskurs) und einer Rückblicklänge.
- Wenn die Farbe von Magenta auf Grün wechselt, schließt die Strategie jede Short-Position und öffnet eine Long-Position.
- Wenn die Farbe von Grün auf Magenta wechselt, schließt die Strategie jede Long-Position und öffnet eine Short-Position.

## Parameter
- **Candle Type** – Typ der in der Strategie verwendeten Kerzen.
- **Length** – Anzahl der Balken zum Preisvergleich.
- **Price Type** – Preis, der bei der Indikatorberechnung verwendet wird.

## Hinweise
Die Strategie verwendet eine eigene Implementierung des Loco-Indikators. Eine Python-Version ist nicht vorhanden.
