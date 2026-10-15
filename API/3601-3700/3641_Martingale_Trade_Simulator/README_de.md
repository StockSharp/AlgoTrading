# Martingale Handelssimulator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`MartingaleTradeSimulatorStrategy` erstellt den Fachberater „Martingale Trade Simulator“ von MetaTrader innerhalb des StockSharp-Frameworks neu. Bei der Strategie handelt es sich um ein manuelles Handelspanel, das es einem Händler ermöglicht, sofortige Marktaufträge zu senden, eine Mittelwertbildung im Martingal-Stil anzuwenden und den Trailing-Schutz zu verwalten, ohne zusätzliche Skripte zu automatisieren. Er reagiert in Echtzeit auf Parameteränderungen und eignet sich daher genau wie der ursprüngliche MQL-Roboter für Strategietester-Experimente.

## Wie es funktioniert

### Manuelle Marktschaltflächen
- Die Parameter `Buy` und `Sell` fungieren als virtuelle Schaltflächen. Wenn einer der Parameter auf `true` gesetzt ist, sendet die Strategie eine Marktorder mit dem Volumen `Order Volume` und setzt den Parameter dann automatisch auf `false` zurück.
- Es werden keine ausstehenden Aufträge verwendet – die Strategie basiert ausschließlich auf Marktausführungen und spiegelt das Simulatorverhalten im visuellen Tester von MetaTrader wider.

### Martingale Mittelung
- Durch die Aktivierung von `Enable Martingale` kann das Panel Durchschnittsaufträge erteilen, wenn der Parameter `Martingale` auf `true` umgeschaltet wird.
- Die Strategie prüft die aktive Position:
  - **Long-Position:** Wenn der aktuelle Briefkurs mindestens `Martingale Step (points)` unter dem niedrigsten ausgefüllten Kaufpreis liegt, wird ein neuer Kaufauftrag gesendet.
  - **Short-Position:** Wenn der aktuelle Geldkurs mindestens `Martingale Step (points)` über dem höchsten eingegebenen Verkaufspreis liegt, wird ein neuer Verkaufsauftrag erteilt.
- Jedes durchschnittliche Auftragsvolumen entspricht `Order Volume × Martingale Multiplier^N`, wobei `N` die Anzahl der aufeinanderfolgenden Einträge in der aktuellen Richtung ist.
- Wenn Martingal aktiv ist, wird das Take-Profit-Ziel auf den gewichteten durchschnittlichen Einstiegspreis plus/minus `Martingale TP Offset (points)` neu berechnet, um den kumulierten Drawdown abzudecken.

### Trailing-Stop-Modul
- `Enable Trailing` aktiviert einen schützenden Trailing Stop, der dem aktuellsten besten Preis folgt.
- Der Trailing Stop beginnt bei `Trailing Stop (points)` vom Marktpreis entfernt und bewegt sich erst vorwärts, nachdem sich der Preis um mindestens `Trailing Step (points)` verbessert hat.
- Wenn der Marktpreis das Trailing-Level überschreitet, schließt die Strategie sofort die gesamte Position mit einer entgegengesetzten Marktorder.

### Stop-Loss und Take-Profit
- `Stop Loss (points)` und `Take Profit (points)` reproduzieren die grundlegenden Risikokontrollen des ursprünglichen Fachberaters.
- Bei Long-Positionen wird der Stop unter dem durchschnittlichen Einstiegspreis platziert, während der Take-Profit darüber liegt. Bei Short-Positionen werden beide Level gespiegelt.
- Schutzexits werden mit Marktaufträgen ausgeführt, sodass die Strategie mit jedem von StockSharp unterstützten Connector kompatibel bleibt.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Order Volume` | Basisgröße für manuelle Marktaufträge. | `1` |
| `Stop Loss (points)` | Abstand zum Schutzanschlag. Null deaktiviert den Stop-Loss. | `500` |
| `Take Profit (points)` | Entfernung zum Schutzziel. Null deaktiviert den Take-Profit. | `500` |
| `Enable Trailing` | Schaltet das Trailing-Stop-Modul ein/aus. | `true` |
| `Trailing Stop (points)` | Abstand zwischen Preis und Trailing Stop. | `50` |
| `Trailing Step (points)` | Es ist nur eine minimale günstige Bewegung erforderlich, um den Trailing Stop voranzutreiben. | `20` |
| `Enable Martingale` | Ermöglicht die Mittelung von Aufträgen, die über die Schaltfläche `Martingale` gesteuert werden. | `true` |
| `Martingale Multiplier` | Volumenmultiplikator, der für jeden weiteren Durchschnittshandel verwendet wird. | `1.2` |
| `Martingale Step (points)` | Erforderliche Gegenbewegung, bevor eine Mittelungsanordnung zulässig ist. | `150` |
| `Martingale TP Offset (points)` | Zusätzlicher Ausgleich wird auf das durchschnittliche Take-Profit-Niveau angewendet. | `50` |
| `Buy` | Auf `true` setzen, um eine Marktkauforder zu senden (automatische Zurücksetzung). | `false` |
| `Sell` | Auf `true` setzen, um einen Marktverkaufsauftrag zu senden (automatische Zurücksetzung). | `false` |
| `Martingale` | Auf `true` setzen, um einen Mittelungsauftrag auszuwerten und zu erteilen (automatische Zurücksetzung). | `false` |

## Anwendungstipps

1. Hängen Sie die Strategie an ein Instrument an, legen Sie `Order Volume` fest und starten Sie sie im Tester- oder Live-Modus.
2. Verwenden Sie die Umschalter `Buy` / `Sell`, um Schaltflächenklicks im Bedienfeld MetaTrader zu simulieren.
3. Lösen Sie nach dem ersten Handel den `Martingale`-Umschalter aus, wenn sich der Preis entgegen der Position bewegt. Die Strategie überprüft den Preisabstand und erhöht das Volumen, wenn die Bedingungen erfüllt sind.
4. Passen Sie die Trailing- und Risikoparameter an, um das Verhalten des ursprünglichen EA zu reproduzieren oder mit alternativen Einstellungen zu experimentieren.

## Notizen

- Die Strategie basiert auf Level-1-Daten (bester Geld-/Briefkurs und letzter Handel), um die Marktbedingungen zu bewerten.
- Alle Kommentare im C#-Code sind auf Englisch, um die Konsistenz mit den Repository-Richtlinien zu wahren.
