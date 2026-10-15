# Move Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie demonstriert eine vereinfachte Konvertierung des ursprünglichen `move_cross.mq4`-Skripts. Sie verwendet den RAVI-Indikator (Range Action Verification Index), der aus zwei einfachen gleitenden Durchschnitten berechnet wird, um die Trendrichtung zu bestimmen.

Die Strategie vergleicht stündliche und tägliche RAVI-Werte:

- **Kaufen** wenn der stündliche RAVI negativ ist, während der tägliche RAVI positiv und steigend ist.
- **Verkaufen** wenn der stündliche RAVI positiv ist, während der tägliche RAVI negativ und fallend ist.

Positionen werden zum Marktpreis mit optionalem Gewinnziel und Stop-Loss eröffnet.

## Parameter

| Name       | Beschreibung                          | Standard |
|------------|---------------------------------------|----------|
| TakeProfit | Gewinnziel in Punkten                  | 50       |
| StopLoss   | Verlustlimit in Punkten                | 100      |

## Hinweise

- Die Strategie verwendet zwei SMA-Paare (Perioden 2 und 24), um RAVI auf stündlichen und täglichen Kerzen zu berechnen.
- Sie ist für Bildungszwecke gedacht und erfordert möglicherweise weitere Anpassungen für den Live-Handel.
