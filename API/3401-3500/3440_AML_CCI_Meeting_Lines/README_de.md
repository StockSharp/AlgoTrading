# AML CCI Meeting Lines-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader 5 Experten „Expert_AML_CCI“ innerhalb des StockSharp High-Level-Frameworks. Der ursprüngliche Roboter
kombiniert das japanische Candlestick-Muster „Meeting Lines“ mit einem Commodity Channel Index (CCI)-Filter und nutzt den Expert Advisor
Motor zur Gewichtung bullischer und bärischer Stimmen. Der Port StockSharp behält die gleiche Bestätigungslogik bei und übersetzt den Candlestick
Mustererkennung in reine Kerzenarithmetik umwandelt und alle Schwellenwerte als optimiererfreundliche Parameter offenlegt.

## Wie es funktioniert
* **Datenquelle** – Die Strategie abonniert eine konfigurierbare Zeitrahmen-Kerzenserie (standardmäßig 30-Minuten-Kerzen) mit
`SubscribeCandles`. Jede fertige Kerze wird zusammen mit dem synchronisierten CCI-Wert über den High-Level `Bind` versendet.
Pipeline, sodass keine manuelle Indikatorenverwaltung erforderlich ist.
* **Kernindikator** – Ein einzelner `CommodityChannelIndex` mit der Periode `CciPeriod` spiegelt den MetaTrader-Oszillator wider. Seine Werte sind
Wird intern zwischengespeichert, um die beiden letzten abgeschlossenen Messwerte zu vergleichen und die Aufrufe `CCI(1)` und `CCI(2)` von MQL zu replizieren.
* **Candlestick-Logik** – Die Hilfsmethoden bauen die Prüfungen „Bullish Meeting Lines“ und „Bearish Meeting Lines“ neu auf. Sie berechnen
der gleitende Durchschnitt der Körperlängen über `AverageBodyPeriod` Kerzen (Standard 3) und erzwingt den langen Körper und den gleichen Schlusskurs
Anforderungen aus dem ursprünglichen `CML_CCI`-Filter. Da StockSharp fertige Kerzen liefert, wird das Muster genau ausgewertet
wenn die zweite Kerze des Musters schließt – im selben Moment gibt der MQL-Experte seine 80-Punkte-Stimme ab.
* **Eintrittsregeln** –
  * Long-Positionen erfordern eine bullische Meeting-Lines-Formation und den letzten abgeschlossenen CCI-Wert, um darunter oder gleich zu bleiben
`LongEntryCciLevel` (standardmäßig −50). Wenn ein entgegengesetzter Short offen ist, umfasst die Ordergröße automatisch den absoluten Wert
der aktuellen Position, um die Richtung umzukehren, was dem Verhalten von EA entspricht.
  * Short-Positionen spiegeln die Logik wider: ein rückläufiges Meeting-Lines-Muster plus einen CCI-Wert über oder gleich `ShortEntryCciLevel`
(+50 standardmäßig).
* **Ausstiegsregeln** – Anstelle der Abstimmungsgewichte des Expert Advisors verwendet der Port explizite Flattening Orders. Positionen sind geschlossen
wenn CCI das durch `ExtremeCciLevel` definierte Extremband überschreitet (standardmäßig 80):
  * Shorts werden beendet, wenn der CCI durch −Extreme nach oben springt oder wieder unter +Extreme fällt.
  * Long-Positionen werden beendet, wenn CCI unter +Extreme fällt oder unter −Extreme fällt.
Diese Regeln spiegeln den Abstimmungszweig `40` innerhalb von `LongCondition` und `ShortCondition` in der Signalklasse MQL wider.
* **Risikomanagement** – Die Strategie überlässt Schutzstopps dem Anrufer. Es ist kompatibel mit StockSharps `StartProtection`
Helfer, wenn ein Stop-Loss oder Take-Profit extern angebracht werden muss.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Zeitrahmen der Quellkerzen. | 30-minütiger Zeitrahmen |
| `CciPeriod` | Länge des Commodity Channel Index. | 18 |
| `AverageBodyPeriod` | Anzahl der Kerzen, die zur Berechnung der durchschnittlichen Körpergröße für die Mustervalidierung verwendet werden. | 3 |
| `LongEntryCciLevel` | Überverkauftes Niveau, das bullische Meeting-Linien bestätigt. | −50 |
| `ShortEntryCciLevel` | Überkauftes Niveau, das die rückläufigen Meeting-Linien bestätigt. | +50 |
| `ExtremeCciLevel` | Absolutes Extremband für CCI Exit-Crossovers. | 80 |

Alle numerischen Parameter stellen Optimierungsbereiche bereit, die mit den EA-Standardwerten identisch sind, sodass die Strategie durch StockSharp optimiert werden kann
Optimierungstools.

## Nutzungshinweise
1. Hängen Sie die Strategie an ein Wertpapier an und stellen Sie den gewünschten `Volume` ein, bevor Sie beginnen.
2. Passen Sie optional die Schwellenwerte an, um sie an das ursprüngliche Geldverwaltungsprofil anzupassen oder um die Empfindlichkeit anzupassen.
3. Die Chart-Integration zeichnet Kerzen, die CCI-Kurve und ausgeführte Trades für eine schnelle visuelle Validierung der Mustererkennung.

Durch die Fokussierung auf die gleiche Candlestick+CCI-Kombination liefert diese StockSharp-Implementierung eine originalgetreue Portierung des Experten
Berater unter Einhaltung des empfohlenen High-Level-API-Stils.
