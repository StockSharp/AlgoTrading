# I-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **I-Trend-Strategie** ist ein Trendfolge-Handelsalgorithmus, der vom ursprünglichen MQL5-Experten `Exp_i_Trend` konvertiert wurde. Sie kombiniert einen gleitenden Durchschnitt mit Bollinger Bändern, um Impulsänderungen zu identifizieren. Die Strategie berechnet einen benutzerdefinierten *iTrend*-Wert und eine entsprechende Signallinie und eröffnet oder schließt Positionen bei Kreuzungen.

## Funktionsweise

1. **Indikator-Setup**
   - Berechnet einen Exponentiellen Gleitenden Durchschnitt (EMA) mit konfigurierbarer Periode.
   - Erstellt Bollinger Bänder mit denselben Zeitrahmen- und Abweichungsparametern.
   - Leitet den *iTrend*-Wert als Differenz zwischen dem gewählten Preis und der ausgewählten Bollinger Band-Linie (oben, unten oder mittig) ab.
   - Berechnet eine Signallinie als `2 * MA - (High + Low)`.
2. **Signalerzeugung**
   - Wenn der iTrend die Signallinie **von unten nach oben** kreuzt, schließt die Strategie Short-Positionen und eröffnet eine Long-Position.
   - Wenn der iTrend die Signallinie **von oben nach unten** kreuzt, schließt die Strategie Long-Positionen und eröffnet eine Short-Position.
3. **Orderausführung**
   - Ein- und Ausstiege werden zum Marktpreis ausgeführt.
   - Die Positionsgröße wird durch den Strategieparameter `Volume` definiert.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `MaPeriod` | Periode des in Berechnungen verwendeten gleitenden Durchschnitts. |
| `BbPeriod` | Periode der Bollinger Bänder. |
| `BbDeviation` | Standardabweichung für die Bollinger Bänder. |
| `PriceType` | Preistyp zur Berechnung des iTrend-Werts (Close, Open, High, Low, Median, Typical usw.). |
| `BbMode` | Wählt, welche Bollinger Band-Linie verwendet wird (Upper, Lower, Middle). |
| `CandleType` | Zeitrahmen der an die Strategie gelieferten Kerzen. |
| `Volume` | Ordervolumen für Einstiege. |

## Hinweise

- Die Strategie arbeitet nur mit abgeschlossenen Kerzen; unfertige Kerzen werden ignoriert.
- Sie ist für Bildungszwecke konzipiert und kann Anpassungen für den Echtzeit-Handel erfordern.
