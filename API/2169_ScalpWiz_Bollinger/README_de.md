# ScalpWiz Bollinger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **ScalpWiz Bollinger-Strategie** ist ein Gegentrendsystem, das Bollinger-Bänder verwendet, um überdehnte Preise zu erkennen. Wenn sich der Schlusskurs weit über das obere oder unter das untere Band bewegt, eröffnet die Strategie eine Position in entgegengesetzter Richtung und erwartet eine Rückkehr.

Vier Distanzebenen werden geprüft. Jede Ebene entspricht einer anderen Signalstärke und multipliziert das Handelsvolumen. Die Positionsgröße wird außerdem durch einen Risikoprozentsatz des aktuellen Portfoliowerts skaliert.

## Parameter

- `BandsPeriod` – Anzahl der Kerzen für die Berechnung der Bollinger-Bänder.
- `BandsDeviation` – Standardabweichungs-Multiplikator für die Bänder.
- `Level1Pips` … `Level4Pips` – Abstand vom Band in Pips, der ein Level-1–4-Signal auslöst.
- `StrengthLevel1Multiplier` … `StrengthLevel4Multiplier` – Volumenmultiplikatoren für jede Ebene.
- `RiskPercent` – Prozentsatz des Portfoliowerts, der pro Signal riskiert wird.
- `CandleType` – Kerzen-Zeitrahmen für die Berechnungen.

## Handelslogik

1. Abonnieren von Kerzen des ausgewählten Zeitrahmens und Berechnung der Bollinger-Bänder.
2. Bei jeder abgeschlossenen Kerze:
   - Liegt der Schlusskurs um eine konfigurierte Levelentfernung über dem oberen Band, wird eine Short-Position eröffnet.
   - Liegt der Schlusskurs um eine konfigurierte Levelentfernung unter dem unteren Band, wird eine Long-Position eröffnet.
3. Das Volumen wird aus dem Risikoprozentsatz und dem Signalstärke-Multiplikator berechnet.

Die Strategie wurde durch das originale MQL-Skript `mcb.scalpwiz.9001.mq4` inspiriert.
