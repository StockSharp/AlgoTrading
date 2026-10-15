# Pendulum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-basiertes Martingale-System, das zwischen zwei Preisgrenzen pendelt. Die Strategie eröffnet eine Long-Position, wenn der Preis die obere Grenze des Grids erreicht, und wechselt zu einer Short-Position mit erhöhtem Volumen, wenn der Preis zur unteren Grenze wechselt. Sie wechselt weiterhin die Richtungen (bis zu einer konfigurierbaren Anzahl von Schichten) und erweitert dabei die Ziele und reduziert die Schutzabstände gemäß dem ursprünglichen Pendulum-Expertenberater. Nach der Gewinnmitnahme setzt die Engine das Grid zurück und plant einen neuen Einstieg auf demselben Niveau, um die Pendelbewegung aufrechtzuerhalten.

## Details

- **Einstiegslogik**
  - Richtet das Grid am Kerzen-Schlusskurs unter Verwendung der konfigurierten `StepSize` aus.
  - **Oberes Trigger-Niveau erreicht** → eröffnet eine Long-Position mit dem Basisvolumen.
  - **Unteres Trigger-Niveau erreicht** → eröffnet eine Short-Position mit dem Basisvolumen.
  - Wenn die aktive Position zum entgegengesetzten Trigger wechselt, dreht die Strategie die Richtung um, multipliziert das absolute Volumen mit `Multiplier` und aktualisiert die Take-Profit/Stop-Loss-Abstände wie die MQL-Version.
  - Wiedereinstiege werden nach profitablen Ausstiegen geplant, damit die nächste Kerze sofort auf demselben Grid-Niveau wieder eröffnen kann, sobald die Schließtrades verarbeitet sind.
- **Ausstiegslogik**
  - Jede Schicht definiert einen dedizierten Take-Profit: ein Schritt für die erste Schicht, `Multiplier` Schritte für jede nachfolgende Schicht.
  - Schutz-Stops spiegeln die MQL-Logik wider: Die erste Schicht verwendet einen weiten Stop (`StepSize * Multiplier`), nachfolgende Schichten verwenden einen Ein-Schritt-Stop gegen die neue Richtung.
  - Wenn die maximale Anzahl an Schichten erreicht ist, wartet die Strategie auf Take-Profit oder Stop-Loss, bevor sie zurückgesetzt wird.
- **Positionsmanagement**
  - Verwendet Netting: Der StockSharp-Port schließt und kehrt die aggregierte Position um, anstatt abgesicherte Longs und Shorts zu halten. Dies bewahrt die Exposition des ursprünglichen Experten und bleibt dabei mit StockSharp-Portfolios kompatibel.
  - Das Volumen wird auf den Instrumenten-Volumenschritt gerundet, wenn verfügbar.
- **Daten**
  - Funktioniert mit jedem Symbol und Zeitrahmen. Das Standard-Abonnement verwendet 1-Minuten-Kerzen und verlässt sich auf Kerzen-Schlusskurse für die Grid-Prüfungen.
- **Eingebauter Schutz**
  - `StartProtection()` ist aktiviert, um unerwartete Positionen nach Verbindungsabbrüchen oder manuellen Eingriffen zu schützen.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `StepSize` | `0.001` | Abstand zwischen Grid-Levels. Das Grid rastet immer auf Vielfache dieses Wertes ein. |
| `Multiplier` | `2` | Multipliziert sowohl das Trade-Volumen als auch die erweiterten Ziele, wenn die Richtung auf eine neue Schicht wechselt. Muss größer als 1 sein. |
| `MaxLayers` | `3` | Maximale Anzahl von Martingale-Schichten, bevor die Strategie aufhört, neue Umkehrungen hinzuzufügen. |
| `BaseVolume` | `1` | Basis-Handelsgröße für die erste Schicht. Spätere Schichten skalieren mit `Multiplier`. |
| `CandleType` | `1 Minute TimeFrame` | Kerzentyp für das Abonnement. Kann auf jeden anderen vom Datenprovider unterstützten Zeitrahmen geändert werden. |

## Hinweise

- Die Strategie reproduziert das Verhalten von `Pendulum.mq5` ohne Abhängigkeit von abgesicherten Positionen. Da StockSharp die Exposition konsolidiert, wird die Nettoposition umgekehrt, um die MQL-Grids zu emulieren.
- Take-Profit-Abschlüsse lösen eine verzögerte Order aus, damit die nächste Kerze sofort auf demselben Preisniveau wieder eröffnen kann, sobald der Schließtrade verarbeitet ist.
- Die konfigurierte Schrittgröße mit dem Instrumenten-Preisschritt ausrichten, um übermäßiges Runden der Grid-Levels zu vermeiden.
