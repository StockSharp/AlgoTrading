# AIS1-Handelsroboter (MQL/8700-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der **AIS1 Trading Robot** ist eine direkte C#-Konvertierung des MetaTrader 4 Expert Advisors von `MQL/8700/AIS1.MQ4`. Das ursprüngliche System ist auf tägliche EURUSD-Ausbrüche zugeschnitten und verwendet Multi-Timeframe-Bereiche für Stop-, Ziel- und Trailing-Berechnungen. Diese StockSharp-Implementierung behält die Struktur des Legacy-Robots bei und stellt gleichzeitig jedes konfigurierbare Element als Strategieparameter bereit.

## Handelslogik
- **Zeitrahmen**
  - Primärkerzen: 1-Tages-Balken für Einstiegsbedingungen, Stop-Loss und Take-Profit-Distanzen.
  - Sekundärkerzen: 4-Stunden-Balken für dynamische Trailing-Stop-Berechnungen.
- **Eintrittsbedingungen**
  - Langer Ausbruch: Der gestrige Tagesschluss liegt über der Mitte des Balkens und der aktuelle Brief durchbricht das vorherige Tageshoch.
  - Kurzer Ausbruch: Der gestrige Tagesschluss liegt unter dem Mittelwert und das aktuelle Gebot fällt unter das vorherige Tagestief.
  - Es kann jeweils nur eine Position offen sein; Gegensignale werden ignoriert, bis der aktuelle Handel geschlossen wird.
- **Anfängliches Risiko und Ertrag**
  - Stop-Loss = vorheriges Tageshoch/-tief ± `StopFactor × daily range`.
  - Take Profit = Einstiegspreis ± `TakeFactor × daily range`.
  - Beide Ebenen werden anhand des optionalen `StopBufferTicks` validiert, um die Einschränkungen hinsichtlich der Halteentfernung des Brokers zu berücksichtigen.
- **Trailing Stop**
  - Verwendet den Bereich der letzten 4-Stunden-Kerze multipliziert mit `TrailFactor`.
  - Nachlaufende Aktualisierungen erfordern, dass sich der Preis um mindestens `TrailStepMultiplier × spread` über den bestehenden Stopp hinaus bewegt und um den konfigurierten Puffer vom Ziel entfernt bleibt.
  - Der Drawdown-Schutz deaktiviert nachlaufende Aktualisierungen, wenn das Eigenkapital unter den Reserveschwellenwert fällt.
- **Risikomanagement**
  - Die Losgröße ergibt sich aus `OrderReserve × equity` dividiert durch das monetäre Risiko zwischen Einstieg und Stopp.
  - Die Volumina sind an die Börsenlimits (`MinVolume`, `MaxVolume`, `VolumeStep`) gebunden.
  - Die Eigenkapitalüberwachung verfolgt das laufende Maximum und blockiert neue Einträge, sobald das Eigenkapital unter `AccountReserve - OrderReserve` dieses Höchstwerts fällt.
- **Zeitschutz**
  - Aktionen (Einträge oder nachgestellte Aktualisierungen) werden durch eine obligatorische Pause von fünf Sekunden getrennt, wodurch die ursprüngliche EA-Drosselung reproduziert wird.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `AccountReserve` | 0,20 | Anteil des Eigenkapitals, der unangetastet bleiben muss. Wird zur Berechnung des zulässigen Drawdowns verwendet. |
| `OrderReserve` | 0,04 | Anteil des jedem Trade zugewiesenen Eigenkapitals und Grundlage für die Positionsgröße. |
| `PrimaryCandleType` | Täglich | Kerzentyp, der für Breakout-Logik und statische Ziele verwendet wird. |
| `SecondaryCandleType` | 4 Stunden | Kerzentyp, der zum Ableiten von Nachlaufabständen verwendet wird. |
| `TakeFactor` | 0,8 | Multiplikator der Tagesspanne, die zur Gewinnmitnahme angewendet wird. |
| `StopFactor` | 1,0 | Multiplikator der täglichen Spanne, die auf den Stop-Loss angewendet wird. |
| `TrailFactor` | 5,0 | Multiplikator des 4-Stunden-Bereichs, der auf Trailing Stops angewendet wird. |
| `TrailStepMultiplier` | 1,0 | Spread-Multiplikator, der steuert, um wie viel der Preis steigen muss, bevor ein neuer Trailing Stop gesetzt wird. |
| `StopBufferTicks` | 0 | Zusätzliche Preisschritte wurden als Sicherheitsmargen um Stopps und Ziele hinzugefügt. |

## Nutzungshinweise
1. Weisen Sie das gewünschte **Wertpapier** (standardmäßig EURUSD) und **Portfolio** zu, bevor Sie mit der Strategie beginnen.
2. Stellen Sie sicher, dass sowohl die Tages- als auch die 4-Stunden-Kerzenquelle verfügbar sind. Andernfalls können die Breakout- und Trailing-Module nicht aktiviert werden.
3. Die Strategie abonniert das Orderbuch, um aktuelle Geld-/Briefkurse zu erhalten. In Märkten ohne Tiefenfeed wird der zuletzt gehandelte Preis als Fallback verwendet.
4. Positionsausstiege werden über Marktaufträge durchgeführt, wenn Stopp- oder Zielbedingungen erfüllt sind, was dem Verhalten von MetaTrader EA entspricht, das schützende Aufträge auf der Serverseite geändert hat.
5. Der Drawdown-Limiter, der Pausentimer und die Risikogrößenlogik können alle über die offengelegten Parameter abgestimmt werden, um den Roboter an verschiedene Makler oder Vertragsspezifikationen anzupassen.

## Unterschiede zum Original MQL
- Schutzstopps und -ziele werden durch manuelle Positionsschließungen emuliert, wenn die Preise die gespeicherten Niveaus überschreiten (MT4 hat dies über eine Auftragsänderung gehandhabt).
- Die Risikokonvertierung basiert auf `PriceStep` und `StepPrice` aus dem Objekt `Security`. Wenn solche Metadaten fehlen, greift der Code auf eine 1:1-Geldkonvertierung zurück, daher sollten Benutzer die Vertragsspezifikationen noch einmal überprüfen.
- Aus Gründen der Klarheit und zur besseren Integration in die Optimierungstools von StockSharp wurden ausführliche Kommentare und Parameterbeschreibungen hinzugefügt.

## Anforderungen
- StockSharp High-Level API mit Zugriff auf Kerzenabonnements und Orderbuchdaten.
- Richtig konfigurierte Handelsverbindung für Auftragserteilung und Portfoliobewertung.
