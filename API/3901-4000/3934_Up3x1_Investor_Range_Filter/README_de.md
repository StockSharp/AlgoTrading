# Up3x1 Investor Range Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte Portierung des MetaTrader 4 Expertenberaters **up3x1_Investor**. Es handelt ein einzelnes Instrument mit abgeschlossenen Kerzen aus einem konfigurierbaren Zeitrahmen (standardmäßig H1). Der Port repliziert die ursprüngliche Logik mit StockSharp High-Level-APIs und fügt klare Risikomanagementparameter hinzu.

## Handelslogik
- Die Strategie wertet die letzte vollständig geschlossene Kerze aus und prüft Folgendes:
  - Der Kerzenbereich (Hoch minus Tief) überschreitet `0.0060` Preiseinheiten.
  - Der Kerzenkörper (absolute Differenz zwischen Eröffnung und Schluss) überschreitet `0.0050` Preiseinheiten.
- Wenn die Kerze bullisch schloss und die oben genannten Bedingungen erfüllt sind, eröffnet die Strategie eine **Long**-Marktposition.
- Wenn die Kerze bärisch schloss und die Bedingungen erfüllt sind, eröffnet die Strategie eine **Short**-Marktposition.
- Der Handel ist montags vollständig deaktiviert (um den Schutz `DayOfWeek()==1` aus dem Code MQL widerzuspiegeln).

## Positionsmanagement
- Beim Eintritt legt die Strategie interne Ziele anhand der konfigurierten schrittbasierten Distanzen fest:
  - `TakeProfitPoints` → Distanz zum Gewinnziel.
  - `StopLossPoints` → Schutzstoppabstand.
  - `TrailingStopPoints` → Distanz, die verwendet wird, um den Stop zu verfolgen, sobald sich der Preis positiv bewegt.
- Stopps und Ziele werden bei jeder fertigen Kerze ausgewertet:
  - Wenn der Preis das Ziel erreicht, wird die Position zum Zielpreis geschlossen.
  - Wenn der Preis den Stop erreicht, wird die Position geschlossen, um den Verlust zu begrenzen.
  - Sobald der Preis über die Trailing-Distanz hinaus steigt, wird der Stop näher an den Marktpreis verschoben, um einen Gewinn zu sichern.
- Wenn außerdem die für dieselben Kerzen berechneten einfachen gleitenden Durchschnitte über 24 und 60 Perioden gleich werden (innerhalb eines Preisschritts), wird die Position sofort geschlossen. Dies ahmt die MQL-Logik nach, bei der die Order geschlossen wird, wenn beide Durchschnittswerte genau übereinstimmen.

## Volumen- und Risikomanagement
- `BaseVolume` definiert die Fallback-Losgröße, wenn keine kontobasierte Anpassung berechnet werden kann.
- `MaximumRisk` repliziert die ursprüngliche `AccountFreeMargin()*MaximumRisk/1000`-Formel. Wenn der Portfoliowert verfügbar ist, schätzt die Strategie die Position auf `value * MaximumRisk / 1000`, gerundet auf eine Dezimalstelle.
- `DecreaseFactor` imitiert die Loss-Streak-Reduktion: Nach mehr als einem aufeinanderfolgenden Verlust wird die Lautstärke proportional zu `losses / DecreaseFactor` verringert.
- `MinimumVolume` stellt sicher, dass das Volumen niemals unter die kleinste handelbare Größe fällt, die im MQL-Skript verwendet wird (0,1 Lots).

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `BaseVolume` | `0.1` | Basispositionsgröße in Lots, wenn keine Risikoanpassung angewendet wird. |
| `MaximumRisk` | `0.2` | Risikofaktor, der zur Ableitung des Volumens aus dem Kontokapital verwendet wird (identisch mit dem ursprünglichen EA). |
| `DecreaseFactor` | `3` | Reduziert die Positionsgröße nach aufeinanderfolgenden Verlusten. |
| `MinimumVolume` | `0.1` | Kleinstes zulässiges Volumen. |
| `TakeProfitPoints` | `20` | Gewinnzielentfernung gemessen in Preisschritten. |
| `StopLossPoints` | `50` | Stop-Loss-Distanz gemessen in Preisschritten. |
| `TrailingStopPoints` | `10` | Trailing-Stop-Distanz, gemessen in Preisschritten. |
| `SkipMondays` | `true` | Deaktivieren Sie alle Handelsaktivitäten montags. |
| `CandleType` | `1 hour` | Zeitrahmen für das Kerzenabonnement. |

## Notizen
- Die Strategie hält jeweils nur eine Position offen und entspricht dem ursprünglichen `CalculateCurrentOrders`-Guard.
- Die Verfolgung aufeinanderfolgender Verluste erfolgt rein intern, da StockSharp-Broker die Auftragshistorie von MetaTrader nicht offenlegen.
- Es werden keine ausstehenden Bestellungen verwendet; Alle Geschäfte werden als Marktaufträge über `BuyMarket` und `SellMarket` gesendet.
