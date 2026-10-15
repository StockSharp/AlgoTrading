# Exp Blau CSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Konvertierung des MetaTrader 5-Experten `Exp_BlauCSI`. Sie handelt auf dem Blau Candle Stochastic Index (CSI), der auf einer ausgewählten Kerzenserie berechnet wird. Die Strategie kann entweder auf Nulllinien-Durchbrüche oder auf Richtungsänderungen im Indikator reagieren und unterstützt konfigurierbare Stop-Loss- und Take-Profit-Niveaus, gemessen in Preisschritten.

## Handelslogik

Der Blau CSI vergleicht eine Momentum-Komponente mit der Hoch-Tief-Spanne der letzten Kerzen. Beide Teile werden dreimal mit einem ausgewählten gleitenden Durchschnittstyp geglättet.

* **Breakdown-Modus** – öffnet eine Long-Position, wenn der Indikator unter null kreuzt und alle Shorts schließt, während der vorherige Wert positiv war. Öffnet eine Short-Position bei einem Kreuz über null und schließt alle Longs, während der vorherige Wert negativ war.
* **Twist-Modus** – öffnet eine Long-Position, wenn der Indikator nach oben dreht (Wert steigt im Vergleich zum vorherigen Balken nach einem Rückgang). Öffnet eine Short-Position, wenn der Indikator nach unten dreht. Die Richtung des vorherigen Balkens wird immer verwendet, um bestehende Positionen auf der entgegengesetzten Seite zu schließen.

Signale werden auf einem konfigurierbaren historischen Balken (`Signal Bar`) ausgewertet, um die Bestätigung auf vollständig geschlossenen Kerzen sicherzustellen.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Entry Mode` | Wählt die `Breakdown`- oder `Twist`-Logik. |
| `Smoothing Method` | Gleitender Durchschnittstyp, der innerhalb des Blau CSI verwendet wird (Einfach, Exponentiell, Geglättet, LinearWeighted oder Jurik). |
| `Momentum Length` | Anzahl der Balken zur Berechnung der Momentum- und Bereichskomponenten. |
| `First/Second/Third Smoothing` | Tiefe der drei auf Momentum und Bereich angewendeten Glättungsstufen. |
| `Smoothing Phase` | Phasenparameter für Jurik-Glättung (von anderen Methoden ignoriert). |
| `Momentum Price` / `Reference Price` | Angewendete Preiskonstanten für die führenden und nacheilenden Momentum-Werte (Schluss, Eröffnung, Hoch, Tief, Median, Typical, Gewichtet, Einfach, Viertel, Trendfolge oder Demark). |
| `Signal Bar` | Offset (in Balken) bei der Auswertung des Blau CSI-Puffers. Standard `1` bedeutet den vorherigen geschlossenen Balken. |
| `Stop Loss (pts)` | Stop-Loss-Distanz in Preisschritten (`0` deaktiviert). |
| `Take Profit (pts)` | Take-Profit-Distanz in Preisschritten (`0` deaktiviert). |
| `Allow Long/Short Entries` | Eröffnen von Positionen für jede Richtung aktivieren oder deaktivieren. |
| `Allow Long/Short Exits` | Ausstiegssignale für bestehende Positionen aktivieren oder deaktivieren. |
| `Candle Type` | Datentyp für das Abonnement (Standard: 4-Stunden-Zeitrahmen). |
| `Start Date` / `End Date` | Datumfilter für Handelsaktivitäten. |
| `Order Volume` | Marktorder-Volumen. |

## Risikomanagement

Wenn eine neue Position eröffnet wird, berechnet die Strategie Stop-Loss- und Take-Profit-Niveaus unter Verwendung des Instrument-`PriceStep`. Wenn das Instrument keinen Schritt bereitstellt, werden Stops automatisch deaktiviert. Trailing wird nicht durchgeführt; jede Position behält die anfänglichen Schutzlevel, bis sie durch ein Signal oder das Erreichen eines Ziels geschlossen wird.

## Verwendungshinweise

1. Die Strategie an ein Wertpapier anhängen, das Kerzendaten für den ausgewählten `Candle Type` bereitstellt.
2. Den Indikatormodus und Glättungsparameter entsprechend Ihrem Handelsplan wählen.
3. Sicherstellen, dass das Instrument bei Verwendung von Stop-Loss- oder Take-Profit-Distanzen einen gültigen `PriceStep` hat.
4. Optionally den Handel auf einen Zeitbereich mit `Start Date` und `End Date` einschränken.

## Unterschiede im Vergleich zur ursprünglichen MT5-Version

* Die Implementierung verwendet StockSharp-Indikatoren und C#-Strategie-APIs anstelle von MetaTrader-Handelsfunktionen.
* Das Lot-Größen-Management ist vereinfacht: Das Ordervolumen wird direkt aus dem Parameter `Order Volume` entnommen.
* Nur die von StockSharp bereitgestellten Glättungsmethoden werden unterstützt (Einfach, Exponentiell, Geglättet, LinearWeighted, Jurik). Nicht unterstützte MT5-Modi fallen auf exponentielle Glättung zurück.
* Handelsrichtungs-Toggles und Stop-Management bleiben mit dem ursprünglichen Verhalten kompatibel.

Die Strategie ist bereit für Backtesting in StockSharp Designer, Shell, Runner oder jeder benutzerdefinierten StockSharp-Host-Anwendung.
