# Plan X-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Plan X-Strategie ist ein Ausbruchssystem, das vom ursprünglichen MetaTrader 5 Expert Advisor konvertiert wurde. Es wertet den Abschluss jeder abgeschlossenen Kerze gegen eine Referenzkerze aus, die um eine konfigurierbare Anzahl von Bars verschoben ist. Wenn der aktuellste Schlusskurs den Referenzschlusskurs um eine bestimmte Kanalhöhe überschreitet, öffnet die Strategie eine Position in der Ausbruchsrichtung. Optionale Signalumkehrung ermöglicht den Handel von Ausbrüchen in der entgegengesetzten Richtung.

Die Implementierung verwendet die High-Level-API von StockSharp. Sie unterstützt anpassbaren Stop-Loss, Take-Profit, Trailing-Stop-Logik und einen Trading-Session-Filter.

## Funktionsweise

1. **Kerzenverarbeitung** – die Strategie abonniert den konfigurierten Kerzentyp und verarbeitet nur abgeschlossene Kerzen. Eine kurze Historie von Schlusskursen wird geführt, um den letzten Wert mit einer verschobenen Referenzkerze zu vergleichen.
2. **Ausbruchserkennung** – wenn der letzte Schlusskurs die Referenzkerze um mehr als die Kanalhöhe überschreitet, wird ein Long-Signal erzeugt. Wenn er um denselben Betrag darunter liegt, wird ein Short-Signal generiert. Wenn das Umkehrkennzeichen aktiviert ist, werden die Signale umgekehrt.
3. **Orderausführung** – die Strategie verwendet Marktorders. Bei der Umkehrung aus einer entgegengesetzten Position schließt das Ordervolumen automatisch den absoluten Wert der aktuellen Position ein, um in einem einzigen Vorgang zu glätten und neu einzusteigen.
4. **Risikomanagement** – Stop-Loss- und Take-Profit-Niveaus werden unmittelbar nach dem Einstieg gesetzt. Ein Trailing-Stop kann den ursprünglichen Stop ersetzen, wenn der Preis günstig um mehr als die Trailing-Distanz plus den Trailing-Schritt läuft.
5. **Zeitfilter** – der Handel kann auf eine Start- und Endstunde begrenzt werden. Wenn die Startstunde größer als die Endstunde ist, wird das Fenster als Mitternachtsüberquerung behandelt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Stop Loss (pips)` | Schutz-Stop-Abstand in Pips, in Preiseinheiten basierend auf dem Instrument-Preisschritt konvertiert. |
| `Take Profit (pips)` | Zielabstand in Pips. |
| `Trailing Stop (pips)` | Abstand zwischen Preis und Trailing-Stop. Auf null setzen, um Trailing zu deaktivieren. |
| `Trailing Step (pips)` | Zusätzlicher Gewinn, der benötigt wird, bevor der Trailing-Stop vorgerückt wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| `Channel Height (pips)` | Ausbruchsschwelle in Pips ausgedrückt. |
| `Candle Shift` | Anzahl der Bars zwischen dem letzten Schlusskurs und der Referenzkerze. |
| `Use Time Control` | Aktiviert oder deaktiviert den Trading-Session-Filter. |
| `Start Hour` | Erste Stunde (0–23), wenn Trading erlaubt ist. |
| `End Hour` | Letzte Stunde (0–23), wenn Trading erlaubt ist. |
| `Reverse Signals` | Kehrt die Ausbruchsrichtung um. |
| `Order Volume` | Marktordergröße in Lots/Kontrakten ausgedrückt. |
| `Candle Type` | Datenkerztyp für die Analyse. |

## Signallogik

- **Long-Einstieg** – letzter Schlusskurs ≥ Referenzschlusskurs + Kanalhöhe, Umkehr deaktiviert.
- **Short-Einstieg** – letzter Schlusskurs ≤ Referenzschlusskurs − Kanalhöhe, Umkehr deaktiviert.
- Wenn die Umkehr aktiviert ist, tauscht die Logik die Long- und Short-Bedingungen.

## Trailing-Stop-Logik

- Der Trailing-Stop aktiviert sich, wenn die günstige Bewegung `Trailing Stop + Trailing Step` in Preistermen überschreitet.
- Bei Long-Positionen wird der Stop auf `high − Trailing Stop` verschoben, wenn der neue Wert höher als der bestehende Stop ist.
- Bei Short-Positionen wird der Stop auf `low + Trailing Stop` verschoben, wenn der neue Wert niedriger als der bestehende Stop ist.

## Zusätzliche Hinweise

- Die Pip-Größenberechnung emuliert die MQL-Version, indem der Preisschritt für 3- oder 5-dezimale Instrumente mit 10 multipliziert wird.
- Der Handel außerhalb der erlaubten Session überspringt neue Einträge, verwaltet aber weiterhin offene Positionen.
- Die Strategie ruft `StartProtection()` einmal beim Start auf, um integrierte Portfolio-Schutzdienste zu aktivieren.
