# Universeller Investor Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Universeller Investor Strategie** nutzt das Kreuzen zwischen dem Exponentiellen Gleitenden Durchschnitt (EMA) und dem Linear Gewichteten Gleitenden Durchschnitt (LWMA), um die Marktrichtung zu bestimmen. Die Trendstärke wird bestätigt, indem geprüft wird, dass sich beide Durchschnitte in dieselbe Richtung bewegen.

## Logik

- **Kauf-Einstieg**: LWMA liegt über EMA und beide Durchschnitte steigen.
- **Verkauf-Einstieg**: LWMA liegt unter EMA und beide Durchschnitte fallen.
- **Kauf-Ausstieg**: LWMA kreuzt unter EMA.
- **Verkauf-Ausstieg**: LWMA kreuzt über EMA.

Die Strategie reduziert die Positionsgröße nach aufeinanderfolgenden Verlusttrades, wenn der Reduktionsfaktor aktiviert ist.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `MovingPeriod` | Länge für EMA- und LWMA-Berechnungen. |
| `DecreaseFactor` | Lot-Reduktionsfaktor nach Verlusten (0 deaktiviert die Reduktion). |
| `CandleType` | Kerzendatentyp für Berechnungen. |
| `Volume` | Basis-Handelsvolumen aus den Strategieeinstellungen. |

## Hinweise

- Funktioniert nur mit abgeschlossenen Kerzen.
- Verwendet die High-Level-StockSharp-API mit Indikatorbindung.
- Keine Python-Version vorhanden.
