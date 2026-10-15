# Three Breaky-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Three Breaky-Strategie** ist eine vollständige Konvertierung des MetaTrader 4-Expert-Advisors `ThreeBreaky_v1.mq4`. Die StockSharp-Version behält das ursprüngliche Trio der Ausbruchs-Subsysteme, übersetzt ihre kerzenbasierte Logik auf die High-Level-API und fügt eine klare Positionsbuchführung für jedes Modul hinzu. Die Strategie arbeitet auf einem einzigen konfigurierbaren Zeitrahmen und kann jedes Subsystem unabhängig aktivieren oder deaktivieren.

## Handelsmodule

1. **System 1 – ATR-Expansionsausbruch**
   - Verwendet nur die vorherige Kerze.
   - Geht long, wenn die vorherige Kerze bullisch ist und ihr Hoch-Tief-Bereich viermal die 72-Perioden-Average-True-Range überschreitet.
   - Geht short, wenn die vorherige Kerze bearisch ist und dieselbe Bereichsbedingung erfüllt ist.

2. **System 2 – Ichimoku-Cloud-Flip**
   - Beobachtet die Cloud-Grenzen (Senkou Span A und Senkou Span B) mit Standardperioden 9/26/52.
   - Ein Long-Signal löst aus, wenn zwei Kerzen zuvor unterhalb beider Spans geschlossen wurde und die letzte oberhalb beider Spans geschlossen hat (ein bullischer Flip durch die Cloud).
   - Ein Short-Signal löst aus, wenn zwei Kerzen zuvor oberhalb beider Spans geschlossen wurde und die letzte unterhalb beider Spans geschlossen hat.

3. **System 3 – Außergewöhnlicher Körper-Ausbruch**
   - Verfolgt die Körpergröße der letzten 20 abgeschlossenen Kerzen.
   - Ein Long-Setup erfordert, dass die vorherige Kerze bullisch ist und ihr Körper mehr als dreimal den maximalen Körper in dieser 20-Kerzen-Historie beträgt.
   - Ein Short-Setup spiegelt die Bedingung für bearische Körper wider.

Jedes Subsystem handelt eine dedizierte virtuelle Position. Order-Zeitstempel werden gespeichert, um sicherzustellen, dass ein Modul höchstens einen Trade pro Kerze öffnen kann, genau wie die ursprüngliche `buyTag`- und `sellTag`-Logik.

## Exit-Logik

- **Parabolic-SAR-Umkehr**: Alle offenen Positionen teilen einen Parabolic-SAR (0.005/0.2)-Exit. Wenn der Preis zwischen den letzten zwei Kerzen den SAR kreuzt, wird die betroffene Position geschlossen.
- **Risikomanagement**: Optionale Stop-Loss- und Take-Profit-Abstände (in Pips) werden bei jeder abgeschlossenen Kerze ausgewertet. Wenn die konfigurierten Schwellenwerte überschritten werden, wird die relevante Position sofort geschlossen.

## Verwendete Indikatoren

- Average True Range (Periode 72) für die durchschnittliche Volatilitätsbasis.
- Ichimoku Kinko Hyo (9, 26, 52) für den Cloud-Flip-Filter.
- Parabolic SAR (0.005 Beschleunigung, 0.2 Maximum) für Exits und Trailing-Logik.
- Rollender Körpergröße-Puffer (20 Kerzen) zur Reproduktion des MQL-Körpervergleichs.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `UseSystem1` | Aktiviert das ATR-Expansionsausbruch-Modul. |
| `UseSystem2` | Aktiviert das Ichimoku-Cloud-Flip-Modul. |
| `UseSystem3` | Aktiviert das Großer-Körper-Ausbruch-Modul. |
| `OrderVolume` | Volumen für jede Market-Order von einem beliebigen Modul. |
| `StopLossPips` | Schützende Stop-Distanz in Pips. Auf null setzen zum Deaktivieren. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. Auf null setzen zum Deaktivieren. |
| `CandleType` | Zeitrahmen für die Arbeitskerzen (Standard 1 Stunde). |

## Workflow-Zusammenfassung

1. Die konfigurierte Kerzenserie abonnieren und nur fertige Kerzen verarbeiten.
2. ATR-, Ichimoku- und Parabolic-SAR-Indikatoren zusammen mit der rollenden Körperhistorie aktualisieren.
3. Positionen schließen, die Stops, Ziele oder Parabolic-SAR-Umkehrungen treffen.
4. Wenn Handel erlaubt, jedes Subsystem unabhängig auswerten und Market-Orders ausgeben, wenn alle jeweiligen Bedingungen erfüllt sind.
5. Die letzten Indikatorausgaben speichern, damit die nächste Kerze dieselben historischen Werte wie in der ursprünglichen MQL-Implementierung abrufen kann.

## Hinweise

- Die Strategie nimmt einen Pip-Wert basierend auf dem Instrument-Preisschritt an; fünfstellige und dreistellige FX-Kurse werden auf vier und zwei Dezimalstellen-Pip-Größen normalisiert.
- Subsysteme können gleichzeitig laufen. Jedes führt seinen eigenen Einstiegspreis, seine Positionsrichtung und die letzten Signalzeitstempel, um die `MagicNumber+N`-Trennung des Quell-EA widerzuspiegeln.
- Die StockSharp-Implementierung behält das „einmal pro Bar"-Ausführungsmuster bei, indem Kerzen-Öffnungszeiten verwendet werden, um doppelte Orders innerhalb eines einzelnen Balkens zu blockieren.
