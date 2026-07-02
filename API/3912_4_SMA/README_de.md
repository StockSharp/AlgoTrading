# 4 SMA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die 4-SMA-Strategie repliziert den MetaTrader-Expertenberater **4 SMA.mq4**. Es arbeitet mit 30-Minuten-Kerzen, die mit Durchschnittspreisen berechnet werden, und vergleicht vier einfache gleitende Durchschnitte (5, 20, 40 und 60 Perioden), um Momentumausbrüche zu erkennen. Der StockSharp-Port behält das Einzelpositionsverhalten des ursprünglichen Codes bei und verwendet hochrangige API-Helfer für Markteintritte und Risikomanagement.

## Handelslogik
- Berechnen Sie den Medianpreis `(high + low) / 2` für jede fertige Kerze und geben Sie ihn in die vier SMAs ein.
- **Long-Einstieg** erfolgt, wenn der schnelle SMA über dem mittleren SMA liegt, der mittlere SMA über dem langsamen SMA liegt, der langsame SMA um mindestens eine Preisstufe über dem sehr langsamen SMA liegt und der vorherige langsame SMA unter oder gleich dem sehr langsamen SMA lag. Es kann jeweils nur eine Long-Position aktiv sein.
- **Kurzer Eintrag** ist die Spiegelbedingung: Der schnelle SMA liegt unter dem mittleren SMA, der mittlere SMA liegt unter dem langsamen SMA, der sehr langsame SMA liegt um mindestens eine Preisstufe über dem langsamen SMA und der vorherige langsame SMA lag über oder gleich dem sehr langsamen SMA. Es kann jeweils nur eine Short-Position aktiv sein.

## Positionsmanagement
- Die Strategie schließt Long-Positionen, wenn der langsame SMA den sehr langsamen SMA unterschreitet, und schließt Short-Positionen, wenn der langsame SMA den sehr langsamen SMA überschreitet.
- Die Schutzstufen werden nach jedem Eintrag vorberechnet. Stop-Loss- und Take-Profit-Distanzen folgen den ursprünglichen punktbasierten Einstellungen und stützen sich auf den Wertpapierpreisschritt.
- Trailing-Stops werden aktiviert, sobald sich der Preis über die konfigurierte Trailing-Distanz hinaus bewegt. Der Stopp wird Kerze für Kerze nachgezogen und niemals gelöst.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| CandleType | Für Berechnungen verwendete Kerzenserie (standardmäßig 30 Minuten). | Zeitrahmen M30 |
| TakeProfit | Take-Profit-Distanz in Punkten. | 50 |
| StopLoss | Stop-Loss-Distanz in Punkten. | 50 |
| TrailingStop | Trailing-Stop-Distanz in Punkten. | 11 |
| FastLength | Länge des Fastens SMA. | 5 |
| Mittlere Länge | Länge des Mediums SMA. | 20 |
| SlowLength | Länge des langsamen SMA. | 40 |
| VerySlowLength | Länge des sehr langsamen SMA. | 60 |

Alle numerischen Parameter werden zur Optimierung über die Parameter-Benutzeroberfläche StockSharp verfügbar gemacht.

## Unterschiede zur MQL-Version
- Der ursprüngliche Trailing Stop manipulierte MT4-Aufträge direkt; Der Hafen berechnet die Ausstiegspreise neu und erteilt Marktaufträge, wenn die Niveaus überschritten werden.
- Preisschrittorientierte Berechnungen ermöglichen die Anwendung der Strategie auf Instrumenten mit Nicht-Forex-Tick-Größen.
- Die StockSharp-Implementierung basiert auf `SubscribeCandles`-Bindungen und Strategieparametern auf hoher Ebene und bleibt dabei den Best Practices des Frameworks treu.
