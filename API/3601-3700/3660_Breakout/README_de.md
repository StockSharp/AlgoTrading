# Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Breakout-Strategie ist ein Donchian-Kanal-Breakout-System, das vom ursprünglichen MetaTrader 5-Expertenberater `BreakoutStrategy.mq5` abgeleitet wurde. Bei jedem abgeschlossenen Balken überwacht die Strategie das höchste Hoch und das niedrigste Tief über ein konfigurierbares Lookback-Fenster und geht Geschäfte ein, sobald der Preis diese Grenzen durchbricht. Offene Positionen werden durch einen nachgestellten Kanal geschützt, der aus einer zweiten Donchian-Berechnung abgeleitet wird und die im Quellexperten verwendete nachgestellte Logik widerspiegelt.

## Handelslogik

1. **Einstiegskanal** – Höchste und niedrigste Preise über `EntryPeriod` Balken werden um `EntryShift` Balken verzögert, um zu vermeiden, dass der aktuelle Balken bei der Ausbruchsberechnung verwendet wird.
2. **Breakout-Erkennung** – Ein langer Ausbruch wird ausgelöst, wenn das Hoch des Balkens das verschobene obere Band plus einen Preisschritt berührt. Ein kurzer Ausbruch wird ausgelöst, wenn das Balkentief das verschobene untere Band minus einem Preisschritt berührt.
3. **Ausgangskanal** – Höchste und niedrigste Preise über `ExitPeriod` Balken werden um `ExitShift` Balken verzögert. Die optionale Mittellinie kann den Trailing Stop verschärfen, indem sie das Maximum (für Long-Positionen) oder das Minimum (für Short-Positionen) zwischen dem äußeren und dem mittleren Band auswählt und so die Option „Mittellinie verwenden“ aus EA repliziert.
4. **Positionsverwaltung** – Die Strategie schließt eine bestehende Long-Position, wenn das Tief des Balkens das abschließende Niveau durchbricht, und schließt eine Short-Position, wenn das Hoch des Balkens das abschließende Short-Niveau berührt. Entgegengesetzte Signale glätten jegliche vorhandene Belichtung, bevor sie in die neue Richtung eintreten.
5. **Risikogröße** – Die Positionsgröße wird von `RiskPerTrade` abgeleitet. Die Strategie erhält das Portfolio-Eigenkapital, wandelt die Stop-Distanz mithilfe der Instrumente `PriceStep` und `StepPrice` in Geld um und fordert das größte zulässige Volumen an, das den Verlust in der Nähe des konfigurierten Prozentsatzes hält. Die Lautstärken sind auf die Instrumente `VolumeStep`, `VolumeMin` und `VolumeMax` abgestimmt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Datentyp, der die von der Strategie verwendete Kerzenserie beschreibt. Der Standardwert sind 1-Stunden-Kerzen. |
| `EntryPeriod` | Lookback-Fenster für den Breakout-Kanal. |
| `EntryShift` | Anzahl der abgeschlossenen Balken, die bei der Auswertung des Kanals als Offset verwendet werden. `1` reproduziert das ursprüngliche EA-Verhalten. |
| `ExitPeriod` | Lookback-Fenster für den Trailing-Exit-Kanal. |
| `ExitShift` | Versatz in Balken, der auf den nachlaufenden Kanal angewendet wird. |
| `UseMiddleLine` | Wenn diese Option aktiviert ist, nimmt die Mittellinie Donchian an der Trailing-Stop-Berechnung teil und entspricht der Option MQL5. |
| `RiskPerTrade` | Anteil des pro Trade riskierten Portfolio-Eigenkapitals (z. B. `0.01` für 1 %). |

## Notizen

- Alle Kommentare innerhalb der C#-Implementierung sind gemäß den Repository-Richtlinien auf Englisch verfasst.
- Die Strategie nutzt StockSharp hochrangige API-Funktionen: Kerzenabonnements, Donchian-Kanäle (`Highest`/`Lowest`-Indikatoren) und Schichtindikatoren, um manuelle Puffer zu vermeiden.
- Für diese Konvertierung sind keine automatisierten Tests vorgesehen; Bitte validieren Sie das Verhalten in Ihrer eigenen Umgebung, bevor Sie es in der Produktion bereitstellen.
