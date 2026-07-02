# Envelope MA Short-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Envelope MA Short Strategy** ist eine C#-Portierung des MetaTrader Expert Advisors `EnvelopeMA.mq4` (ID 9533). Es stellt die ursprüngliche Short-Only-Breakout-Logik bei 15-Minuten-Kerzen wieder her, indem es einen exponentiellen gleitenden Durchschnittsumschlag mit zwei zusätzlichen EMAs und einem Trio von Parabolic SAR-Filtern kombiniert. Die Strategie achtet auf Preisrückgänge und den schnellen EMA in die untere Hälfte des Umschlags und löst dann an der unteren Grenze des Umschlags eine ausstehende Verkaufsstopp-Order aus. Wenn die Order ausgeführt wird, verwaltet sie die Short-Position mit festen Stop-Loss- und Take-Profit-Levels sowie indikatorbasierten Ausstiegsregeln.

## Indikatoren und Signale
- **Hüllkurvenbasis:** Exponentieller gleitender Durchschnitt der Kerzenhöchststände (`EnvelopePeriod`, Standard 280). Das untere Band ist der Eintrittsauslöser und wird mit einem Abweichungsprozentsatz (`EnvelopeDeviation`, Standard 0,08 %) berechnet.
- **Schneller EMA:** Exponentieller gleitender Durchschnitt der Kerzentiefs (`FastMaPeriod`, Standard 6), der zur Bestätigung des Momentums vor dem Aktivieren des Short-Einstiegs verwendet wird.
- **Langsamer EMA (verschoben):** Exponentieller gleitender Durchschnitt der Kerzentiefs mit einer Verzögerung von einem Balken (`SlowMaPeriod`, Standard 18). Der verzögerte Wert spiegelt den Verschiebungsparameter `iMA` von MetaTrader wider und wird sowohl für die Eingangsbestätigung als auch für Ausgangsentscheidungen verwendet.
- **Parabolic SAR Trio:** Drei Parabolic SAR Instanzen mit unterschiedlichen Beschleunigungsfaktoren (0,03/0,5, 0,015/0,6 und 0,02/0,2), die alle über dem aktuellen Preis liegen müssen, bevor die Strategie einen indikatorbasierten Ausstieg ermöglicht.

Die Strategie wartet auf abgeschlossene Kerzen. Wenn der schnelle EMA, der verschobene langsame EMA und der Kerzenschluss zwischen den Hüllkurvengrenzen (über dem unteren Band und unter dem oberen Band) bleiben, wird am unteren Hüllkurvenband eine Verkaufsstopp-Order übermittelt. Ausstehende Aufträge verfallen nach etwa fünf Kerzenintervallen, wenn sie nicht ausgeführt werden.

## Handelsmanagement
- **Schutzniveaus:** Beim Einstieg setzt die Strategie interne Stop-Loss- und Take-Profit-Ziele, die aus den konfigurierten Pip-Abständen abgeleitet werden. Preisbewegungen außerhalb der Kerzenspanne werden anhand der Höchst- und Tiefstwerte jedes fertigen Balkens angenähert.
- **Indikator-Ausstieg:** Eine Short-Position wird vorzeitig geschlossen, wenn beide EMAs und der Schlusskurs unter dem Einstiegspreis liegen, alle drei SAR-Werte über dem Preis bleiben und der schnelle EMA wieder über den verzögerten langsamen EMA kreuzt – was das MetaTrader-Verhalten nachahmt.
- **Trailing-Anpassung:** Wenn nach mindestens vier Balken das höchste Kerzenhoch seit dem Einstieg mindestens drei Preisschritte unter den Einstiegspreis gefallen ist und der Schlusskurs unter dem unteren Band des Umschlags liegt, wird der Stop-Loss auf dieses untere Band verschärft.

## Risk controls
- **Aktienschutz:** Der Parameter `LiquidityThreshold` schließt alle offenen Short-Positionen und hebt ausstehende Verkaufsstopps auf, wenn das Verhältnis zwischen Portfolio-Eigenkapital und Startguthaben unter den konfigurierten Wert fällt (Standard 0,58).
- **Auftragsablauf:** Nicht ausgeführte ausstehende Aufträge werden nach Ablauf ihrer Fünf-Balken-Lebensdauer automatisch storniert, um veraltete Signale zu vermeiden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Von der Strategie verarbeiteter Kerzentyp/Zeitrahmen. | 15-minütiger Zeitrahmen |
| `EnvelopePeriod` | Die Länge EMA wird als Umschlagbasis verwendet. | 280 |
| `EnvelopeDeviation` | Umschlagbreite ausgedrückt in Prozent. | 0,08 |
| `FastMaPeriod` | Schnelle EMA-Periode, berechnet auf Tiefstständen. | 6 |
| `SlowMaPeriod` | Langsamer Zeitraum von EMA (ausgewertet mit einer Verzögerung von einem Takt). | 18 |
| `StopLossPips` | Stop-Loss-Abstand in Pips vom Einstiegspreis. | 25 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips vom Einstiegspreis. | 25 |
| `TradeVolume` | Für Pending- und Market-Orders verwendetes Volumen. | 1 |
| `LiquidityThreshold` | Mindesteigenkapitalquote; Shorts werden bei Verletzung liquidiert. | 0,58 |

## Konvertierungshinweise
- Die Lotgröße von MetaTrader basierend auf Saldo, Marge oder Gegen-Pips wurde durch einen direkten Parameter `TradeVolume` ersetzt, um dem Ausführungsmodell von StockSharp zu entsprechen.
- Der Ablaufzeitstempel für ausstehende Orders wird innerhalb der Strategieschleife verarbeitet, da StockSharp-Bestellungen nicht dasselbe Ablauffeld wie MetaTrader aufweisen.
- Stop-Loss- und Take-Profit-Niveaus werden anhand der Kerzenhochs und -tiefs bewertet, um die Auslöser innerhalb eines Balkens anzunähern, was dem Verhalten des MQL-Experten entspricht, der die Preise für abgeschlossene Balken überwacht.
