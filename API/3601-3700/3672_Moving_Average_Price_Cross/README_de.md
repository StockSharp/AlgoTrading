# Cross-Strategien für den gleitenden Durchschnittspreis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Dieses Paket enthält zwei C#-Strategieports der MetaTrader 5 Beispiele, die sich in `MQL/50198` befinden:

* **`MovingAveragePriceCrossStrategy`** – ein minimalistisches Crossover-System mit gleitendem Durchschnitt und Preis, das jeweils eine einzelne Position handelt.
* **`MovingAverageMartingaleStrategy`** – eine erweiterte Version, die die Positionsgröße im Martingal-Stil nach Verlusten anwendet und gleichzeitig die gleiche Preis-/Durchschnitts-Crossover-Logik beibehält.

Beide Implementierungen basieren auf dem übergeordneten StockSharp API, verwenden Kerzenabonnements für die Signalauswertung und stellen MetaTrader-kompatible Parameter für Stop-Loss- und Take-Profit-Abstände bereit.

## Dateien

| Datei | Beschreibung |
| --- | --- |
| `CS/MovingAveragePriceCrossStrategy.cs` | Basispreis/MA-Crossover mit festem Volumen und statischen Schutzaufträgen. |
| `CS/MovingAverageMartingaleStrategy.cs` | Martingale-Variante, die Volumen und Schutzabstände nach verlorenen Trades skaliert. |

## Handelslogik

### MovingAveragePriceCrossStrategy

1. Abonniert Kerzen des konfigurierten Zeitrahmens und berechnet einen einfachen gleitenden Durchschnitt (`SMA`).
2. Wertet Signale nur bei fertigen Kerzen aus, um das MT5-Expertenverhalten nachzuahmen.
3. Erkennt Überschneidungen zwischen dem SMA und dem Schlusskurs der Kerze anhand der letzten beiden abgeschlossenen Kerzen:
   * **Verkaufen**, wenn der gleitende Durchschnitt über den Kerzenschluss steigt (der Preis hat den Durchschnitt unterschritten).
   * **Kaufen**, wenn der gleitende Durchschnitt unter den Kerzenschluss fällt (der Preis hat den Durchschnitt überschritten).
4. Platziert eine einzelne Marktorder pro Signal, wenn derzeit keine Position offen ist.
5. Wendet automatischen Schutz über `StartProtection` an, wobei MetaTrader Punktabstände in absolute Preisversätze umgewandelt werden.

### MovingAverageMartingaleStrategie

1. Hat das gleiche Kerzenabonnement und die gleiche SMA-Signalgenerierung wie die Basisstrategie.
2. Verfolgt den realisierten PnL nach jeder geschlossenen Position und speichert das letzte Handelsergebnis.
3. Wenn ein neues Crossover-Signal erscheint und keine Position offen ist:
   * Wenn der letzte Trade **verlustbringend** war, multipliziert das nächste Handelsvolumen mit `VolumeMultiplier` (begrenzt auf `MaxVolume`) und vergrößert die Stop-Loss- und Take-Profit-Distanzen um `TargetMultiplier`.
   * Wenn der letzte Handel **profitabel** war, werden das Handelsvolumen und die Schutzabstände auf ihre ursprünglichen Werte zurückgesetzt.
4. Wendet `StartProtection` mit den dynamisch angepassten Offsets unmittelbar vor dem Senden der Marktorder an.
5. Handelt weiterhin jeweils nur eine Position, entsprechend der ursprünglichen Expert-Advisor-Logik.

## Risikomanagement

* Schutzniveaus werden in MetaTrader Punkten ausgedrückt und anhand der erkannten Pip-Größe (`PriceStep` angepasst an 3/5 Dezimal-FX-Symbole) automatisch in absolute Preisversätze übersetzt.
* Die Martingale-Strategie hält die Stop-Loss- und Take-Profit-Multiplikatoren begrenzt, um außer Kontrolle geratene Distanzen zu verhindern.
* Das Positionsvolumen wird an `VolumeStep`, `MinVolume` und optional an `MaxVolume` des Instruments angepasst, um ungültige Aufträge zu vermeiden.

## Parameter

### Gemeinsame Eingaben

| Parameter | Strategie | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | beides | `1 minute` | Kerzendatentyp, der für die Signalberechnung verwendet wird. |
| `MaPeriod` | beides | `50` | Länge des einfachen gleitenden Durchschnitts. |

### MovingAveragePriceCrossStrategy

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | `1` | Auftragsvolumen auf den Instrumentenschritt abgestimmt. |
| `TakeProfitPoints` | `150` | Take-Profit-Distanz in MetaTrader Punkten (0 Deaktivierungen). |
| `StopLossPoints` | `150` | Stop-Loss-Distanz in MetaTrader Punkten (0 deaktiviert). |

### MovingAverageMartingaleStrategie

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `StartingVolume` | `1` | Nach profitablen Trades wird das Basisvolumen wiederhergestellt. |
| `MaxVolume` | `5` | Maximale Lautstärke nach Anwendung von Multiplikatoren. |
| `TakeProfitPoints` | `100` | Anfängliche Take-Profit-Distanz in MetaTrader Punkten. |
| `StopLossPoints` | `300` | Anfängliche Stop-Loss-Distanz in MetaTrader Punkten. |
| `VolumeMultiplier` | `2` | Faktor, der nach einem Verlust auf das nächste Auftragsvolumen angewendet wird. |
| `TargetMultiplier` | `2` | Faktor, der auf Stop-Loss- und Take-Profit-Distanzen nach einem Verlust angewendet wird. |

## Nutzungshinweise

* MetaTrader „Punkte“ entsprechen bei den meisten Instrumenten einem `PriceStep`; Die Strategien multiplizieren sich automatisch mit 10 für FX-Symbole mit 3 oder 5 Dezimalstellen, um dem MT5-Verhalten zu entsprechen.
* Beide Strategien erfordern nur ein Wertpapier und ignorieren Signale, während eine Position offen ist, wodurch die ursprüngliche `PositionsTotal()`-Abwehr der Experten reproduziert wird.
* Aktivieren Sie die Optimierung der bereitgestellten Parameter im StockSharp-Designer, um die MT5-Eingabeoptimierung zu replizieren.
