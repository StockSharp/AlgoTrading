# Color Schaff JCCX Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Konvertierung des MQL5-Experten `Exp_ColorSchaffJCCXTrendCycle`.
Sie verwendet den **Schaff Trend Cycle (STC)**-Oszillator, der auf dem JCCX-Algorithmus aufbaut.

## Handelslogik

* Den Schaff Trend Cycle auf jeder abgeschlossenen Kerze berechnen.
* Wenn der Oszillator nach einer Überschreitung unter das `High Level` fällt, wird eine Long-Position eröffnet und Short-Positionen werden geschlossen.
* Wenn der Oszillator nach einer Unterschreitung über das `Low Level` steigt, wird eine Short-Position eröffnet und Long-Positionen werden geschlossen.

## Parameter

| Name | Beschreibung |
|------|-------------|
| Fast JCCX | Schnelle JCCX-Periode des Indikators. |
| Slow JCCX | Langsame JCCX-Periode des Indikators. |
| Smoothing | JJMA-Glättungsfaktor für JCCX. |
| Phase | JJMA-Phasenwert. |
| Cycle | Zykluslänge für die Schaff-Trend-Berechnung. |
| High Level | Oberes Triggerlevel des Oszillators. |
| Low Level | Unteres Triggerlevel des Oszillators. |
| Open Long | Öffnen von Long-Positionen erlauben. |
| Open Short | Öffnen von Short-Positionen erlauben. |
| Close Long | Schließen bestehender Long-Positionen erlauben. |
| Close Short | Schließen bestehender Short-Positionen erlauben. |

## Hinweise

Die Strategie verwendet die High-Level-API von StockSharp und abonniert Kerzendaten. Sie reagiert nur auf **abgeschlossene** Kerzen. Geldverwaltung und Risikokontrolle sind für Demonstrationszwecke vereinfacht gehalten.
