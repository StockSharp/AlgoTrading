# RVI-Histogramm-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie handelt gegen extreme RVI-Werte. Sie arbeitet mit dem Relative Vigor Index (RVI) und eröffnet Positionen, wenn der Indikator überkaufte oder überverkaufte Zonen verlässt oder wenn der RVI seine Signallinie kreuzt. Zwei Signalmodi werden unterstützt:

- **Levels** – reagiert auf RVI-Kreuzungen vordefinierter oberer oder unterer Schwellenwerte.
- **Cross** – reagiert auf RVI-Kreuzungen mit seiner Signallinie.

Die Logik ist konträr: Wenn der RVI über dem hohen Niveau war und dann fällt, wird eine Long-Position eröffnet. Wenn der RVI unter dem niedrigen Niveau war und dann steigt, wird eine Short-Position eröffnet.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `RviPeriod` | RVI-Berechnungszeitraum. |
| `HighLevel` | Oberer Schwellenwert für den RVI. |
| `LowLevel` | Unterer Schwellenwert für den RVI. |
| `Mode` | Signalgenerierungsmodus (`Levels` oder `Cross`). |
| `EnableBuyOpen` | Eröffnung von Long-Positionen erlauben. |
| `EnableSellOpen` | Eröffnung von Short-Positionen erlauben. |
| `EnableBuyClose` | Schließen von Long-Positionen erlauben. |
| `EnableSellClose` | Schließen von Short-Positionen erlauben. |
| `CandleType` | Kerzen-Zeitrahmen. |

## Funktionsweise

1. RVI und sein einfacher gleitender Durchschnitt werden bei jeder abgeschlossenen Kerze berechnet.
2. Je nach ausgewähltem Modus prüft die Strategie:
   - ob der RVI ein extremes Niveau verlässt, oder
   - ob der RVI seine Signallinie kreuzt.
3. Bei einem Long-Signal schließt die Strategie Short-Positionen und eröffnet eine Long-Position. Bei einem Short-Signal werden Long-Positionen geschlossen und eine Short-Position eröffnet.

Der Standard-Zeitrahmen beträgt vier Stunden.

## Hinweise

- Aufträge werden mit Marktaufträgen ausgeführt.
- Stop-Loss- und Take-Profit-Management kann bei Bedarf separat hinzugefügt werden.
