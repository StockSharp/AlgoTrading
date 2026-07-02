# TCPivotStop Floor Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **TCPivotStop Floor Breakout Strategy** ist eine direkte Portierung des MetaTrader-Expertenberaters `gpfTCPivotStop`. Die Logik dreht sich um
klassische Floor-Pivot-Berechnungen, die am vorherigen Handelstag durchgeführt wurden. Zu Beginn jeder neuen täglichen Sitzung lautet die Strategie:

1. Fasst die Höchst-, Tiefst- und Schlusskurse des Vortages zusammen, um den Pivotpunkt sowie die ersten drei Unterstützungs- und Widerstandsstufen zu berechnen.
2. Überprüft, ob der letzte abgeschlossene Stundenbalken den Pivot von oben oder unten gekreuzt hat.
3. Eröffnet eine Marktorder in Richtung des Crossovers und fügt gleichzeitig Stop-Loss- und Take-Profit-Levels hinzu, die diese widerspiegeln
Verhalten des ursprünglichen Experten.

Es kann jeweils nur eine Position aktiv sein. Die optionale Sitzungsverwaltung ermöglicht eine Reduzierung der Belichtung, wenn ein neuer Tag beginnt.

## Handelsregeln

- **Zeitrahmen** – Entwickelt für 1-Stunden-Kerzen (konfigurierbar).
- **Pivot-Berechnung** – Verwendet den Höchst-, Tiefst- und Schlusskurs des Vortages, um `Pivot`, `R1`, `R2`, `R3`, `S1`, `S2`, `S3` zu berechnen.
- **Eintrittsbedingungen**
  - Geben Sie *short* ein, wenn der letzte abgeschlossene Balken unterhalb des Pivots schloss, während der vorherige Balken darüber schloss.
  - Geben Sie *long* ein, wenn der letzte abgeschlossene Balken über dem Pivot schloss, während der vorherige Balken darunter schloss.
- **Positionsgröße** – Feste Losgröße, definiert durch den Parameter `OrderVolume`.
- **Ausstiegsbedingungen**
  - Stop-Loss- und Take-Profit-Preise werden den klassischen Pivot-Levels zugeordnet.
  - Wenn das Flag `CloseAtSessionEnd` aktiviert ist, liquidiert die Strategie offene Trades, bevor die nächste Sitzung beginnt.
  - Schutzniveaus werden auf Kerzenhochs/-tiefs überwacht und bei Erreichen mit Marktaufträgen ausgeführt.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `OrderVolume` | Handelsgröße für Markteintritte. | `0.1` |
| `TakeProfitTarget` | Wählt aus, welche Pivot-Stufe als Gewinnziel fungiert (`1` = am nächsten, `3` = am weitesten entfernt). | `1` |
| `CloseAtSessionEnd` | Schließen Sie alle offenen Positionen, sobald eine neue tägliche Sitzung beginnt. | `false` |
| `CandleType` | Für alle Berechnungen verwendeter Zeitrahmen (standardmäßig stündlich). | `H1` |

## Notizen

- Die Strategie führt Aufträge nur einmal pro Tag aus, wenn ein neuer Pivot-Satz verfügbar ist, genau wie die Quelle EA, die auf dem ausgelöst wird
erster Tick der täglichen Sitzung.
- Die MetaTrader-Version berechnete die Losgrößen anhand der Kontomargenhistorie neu. Dieser Port hält die Positionsgröße fest und
delegiert die Geldverwaltung bei Bedarf an andere Komponenten.
- Schutzaufträge werden durch die Überwachung von Kerzenextremen und das Senden von Marktaufträgen nachgeahmt, sobald ein Schwellenwert überschritten wird.

## Dateien

- `CS/TcpFloorPivotBreakoutStrategy.cs` – C#-Implementierung der Handelslogik.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Vereinfachte chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.
