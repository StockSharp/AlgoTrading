# GoldWarrior02b-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Algorithmische Strategie, konvertiert aus dem MetaTrader-Expert Advisor *GoldWarrior02b*.
Sie kombiniert eine Impulsmesseinheit, den Commodity Channel Index (CCI) und einen einfachen ZigZag-Swing-Detektor,
um am Ende jedes 15-Minuten-Blocks zu handeln.

Die Implementierung zielt auf die StockSharp-High-Level-API ab und konzentriert sich auf Nettopositionen.
Mehrstufiges Hedging aus dem ursprünglichen Skript wird nicht unterstützt, da StockSharp mit genetteten Positionen arbeitet.

## Konzept

- Verwenden Sie einen benutzerdefinierten Impulsindikator, der die Differenz zwischen Kerzenöffnungs- und Schlusskursen durchschnittlich berechnet.
- Bewerten Sie CCI-Werte, um überkaufte/überverkaufte Umkehrungen und starke Momentum-Spitzen zu erkennen.
- Leiten Sie eine ZigZag-Swing-Richtung aus den letzten Hochs und Tiefs ab, um nicht gegen die dominante Bewegung zu handeln.
- Bewerten Sie Signale nur in den letzten Sekunden (>= 45s) der Minuten 14, 29, 44 und 59.
- Wenden Sie dynamisches Risikomanagement mit Stop-Loss, Take-Profit, Trailing-Stop und einem globalen Gewinnziel an.

## Einstiegsregeln

Ein Trade wird nur berücksichtigt, wenn keine Position derzeit offen ist und die aktuelle Kerze innerhalb
des oben beschriebenen Zeitfensters schließt.

### Long-Setup
- ZigZag-Swing zeigt nach unten (letztes Tief ist tiefer als das vorherige).
- Entweder:
  - CCI steigt über seinen vorherigen Wert, während der vorherige CCI unter -50 war, aktueller CCI unter -30,
    Impuls wird positiv und der vorherige Impuls war negativ.
  - Oder CCI fällt unter -200, der vorherige CCI war noch niedriger, Impuls bleibt unter dem positiven Schwellenwert
    und der vorherige Impuls ist schwächer als der aktuelle Wert.

### Short-Setup
- ZigZag-Swing zeigt nach oben (letztes Hoch ist höher als das vorherige).
- Entweder:
  - CCI fällt unter seinen vorherigen Wert, während der vorherige CCI über 50 war, aktueller CCI über 30,
    Impuls wird negativ und der vorherige Impuls war positiv.
  - Oder CCI überschreitet 200, der vorherige CCI war höher, Impuls bleibt über dem negativen Schwellenwert
    und der vorherige Impuls ist stärker als der aktuelle Wert.

Wenn der vorherige Impuls zwischen den konfigurierten Kauf- und Verkaufsschwellen liegt, werden Signale ignoriert.

## Ausstiegsregeln

- **Stop-Loss**: schließt die Position, wenn der Preis die Stop-Distanz vom Einstiegspreis kreuzt.
- **Take-Profit**: schließt nach Erreichen der konfigurierten Gewinnsdistanz.
- **Trailing Stop**: sobald der Preis um `(TrailingStop + TrailingStep)` Punkte vorrückt, folgt das Trailing-Niveau dem Preis
  in einem Abstand von `TrailingStop` Punkten. Das Kreuzen des Trailing-Niveaus schließt den Trade.
- **Globales Gewinnsziel**: schließt die Position, wenn der nicht realisierte PnL den angegebenen Betrag (in Kontowährung) überschreitet.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `BaseVolume` | Handelsgröße für Einstiege. | `0.1` |
| `StopLossPoints` | Stop-Distanz in Punkten. | `100` |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. | `150` |
| `TrailingStopPoints` | Basis-Trailing-Stop-Distanz. | `5` |
| `TrailingStepPoints` | Zusätzliche Distanz, bevor der Trailing Stop aktiviert wird. | `5` |
| `ImpulsePeriod` | Periode für CCI- und Impulsberechnungen. | `21` |
| `ZigZagDepth` | Minimale Balken zwischen neuen ZigZag-Swings. | `12` |
| `ZigZagDeviation` | Minimale Preisbewegung (in Punkten) zur Bestätigung eines Swings. | `5` |
| `ZigZagBackstep` | Minimale Balken vor dem Akzeptieren eines neuen Swings. | `3` |
| `ProfitTarget` | Nicht realisierter Gewinnschwelle zum Schließen aller Positionen. | `300` |
| `ImpulseSellThreshold` | Impulsschwelle für Shorts (typischerweise negativ). | `-30` |
| `ImpulseBuyThreshold` | Impulsschwelle für Longs (typischerweise positiv). | `30` |
| `CandleType` | Für Berechnungen verwendeter Zeitrahmen. | `5-Minuten-Zeitrahmen` |

## Hinweise

- Der Impulsindikator ist ein gleitender Durchschnitt der Differenz zwischen Kerzeneröffnungs- und Schlusswerten,
  skaliert durch den Preisschritt des Instruments.
- Trailing- und PnL-Berechnungen basieren auf `PriceStep` und `StepPrice` des Instruments, um
  Punktdistanzen in Kontowährung umzurechnen.
- Der ursprüngliche Expert Advisor skaliert Positionsgrößen und setzt Hedging-Stufen ein.
  Dieser StockSharp-Port hält eine einzelne Nettoposition pro Instrument, entsprechend dem StockSharp-Ausführungsmodell.
- Um das ursprüngliche Verhalten enger zu replizieren, erwägen Sie, ein 15-Minuten-Kerzen-Abonnement zu aktivieren
  und sicherzustellen, dass die Tick-Datenlatenz die Ausführung kurz nach dem Schlusszeitstempel erlaubt.

## Haftungsausschluss

Dieses Muster dient zu Bildungszwecken. Vor dem Betrieb auf Live-Märkten validieren Sie die Strategie unter
realistischen Daten-, Latenz- und Kommissionsbedingungen.
