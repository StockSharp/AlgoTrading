# MA Shift Puria Method-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MA Shift Puria Method-Strategie ist eine Implementierung des klassischen "Puria" Expert Advisors, angepasst für StockSharp's High-Level-API. Der Algorithmus kombiniert mehrere exponentielle gleitende Durchschnitte (EMAs) mit einem MACD-Filter und optionaler fraktal-basierter Trailing-Logik. Signale werden nur bei abgeschlossenen Kerzen ausgewertet. Die Positionsverwaltung umfasst feste Stop-Loss- und Take-Profit-Level, konfigurierbare Trailing-Stops und einen optionalen fraktalen Trailing-Modus, der Gewinne in der Nähe des Ziels sichert, wenn ein bestätigter Swing-Punkt erscheint.

## Indikatoren und Berechnungen
- **Schneller EMA (Standard 14)** – erfasst kurzfristiges Momentum und definiert die Neigung des schnellen Durchschnitts.
- **Langsamer EMA (Standard 80)** – repräsentiert die breitere Marktrichtung. Der Abstand zwischen schnellem und langsamem EMA muss einen benutzerdefinierten Pip-Schwellenwert überschreiten, um Signale zu validieren.
- **MACD (schnell 11, langsam 102, Signal 9)** – bestätigt das Richtungsmomentum, indem gefordert wird, dass die Hauptlinie die Nullachse in der Trade-Richtung kreuzt, während sie drei Balken zuvor auf der entgegengesetzten Seite war.
- **Fraktal-Fenster (5 Balken)** – verwendet wenn das Fraktal-Trailing aktiviert ist. Die Strategie leitet Swing-Hochs und -Tiefs aus einem rollenden Fünf-Balken-Puffer ab, entsprechend der MetaTrader-Fraktal-Definition (der Mittelbalken ist das lokale Extrem verglichen mit zwei Balken auf jeder Seite).

## Einstiegslogik
Eine neue Position wird nur eröffnet, wenn die Strategie handeln darf und die folgenden Bedingungen bei der zuletzt abgeschlossenen Kerze zutreffen:

### Long-Einstieg
1. Schneller EMA liegt über dem langsamen EMA.
2. Langsamer EMA tendiert aufwärts verglichen mit seinem Wert vor drei Balken.
3. Schneller EMA hat eine aufwärts gerichtete Neigung (aktueller Wert über dem vorherigen Wert).
4. MACD-Hauptlinie liegt über null und war drei Balken zuvor unter null.
5. Der schnelle EMA stieg zwischen den letzten zwei Balken um mehr als das konfigurierte **Shift Minimum** (in Pips) und beschleunigt entweder weiter oder das vorherige Inkrement war nicht-positiv.

### Short-Einstieg
1. Schneller EMA liegt unter dem langsamen EMA.
2. Langsamer EMA tendiert abwärts verglichen mit drei Balken zuvor.
3. Schneller EMA hat eine abwärts gerichtete Neigung (aktueller Wert unter dem vorherigen Wert).
4. MACD-Hauptlinie liegt unter null und war drei Balken zuvor über null.
5. Der schnelle EMA sank um mehr als den **Shift Minimum**-Schwellenwert und beschleunigt entweder weiter oder das vorherige Inkrement war nicht-negativ.

Die Strategie eröffnet Positionen in festen Inkrementen (manuelles Volumen) oder dynamisch dimensionierten Einheiten basierend auf Portfolio-Risiko, abhängig vom gewählten Modus. Wenn eine entgegengesetzte Position offen ist, schließt der Algorithmus sie und eröffnet eine neue in der aktuellen Richtung in einer einzigen Market-Order.

## Ausstieg und Risikomanagement
- **Stop Loss** – in Pips relativ zum Einstiegspreis gesetzt. Wenn das Tief/Hoch der Kerze das Schutzniveau berührt, wird die Position sofort geschlossen.
- **Take Profit** – ebenfalls in Pips ausgedrückt. Das Erreichen des Ziels schließt die gesamte Position.
- **Trailing Stop** – wenn aktiviert, verfolgt der Stop-Level den Preis um die konfigurierte Distanz, nachdem die Gewinne die Trailing-Distanz plus den Trailing-Step überschreiten. Die Logik spiegelt den originalen MQL-Experten wider und aktualisiert nur, wenn der Stop sich mindestens um den Trailing-Step bewegen kann.
- **Fraktal-Trailing** – optional. Sobald der Preis 95% der Take-Profit-Distanz zurückgelegt hat, kann der Stop auf das letzte Swing-Tief (Long) oder Swing-Hoch (Short) verschoben werden, das durch das Fünf-Balken-Fraktal-Muster identifiziert wurde, was das Risiko enger hält und trotzdem Raum für einen Ausbruch lässt.
- **Risikobasiertes Sizing** – wenn manuelles Volumen deaktiviert ist, riskiert die Strategie einen festen Prozentsatz des Portfolios pro Trade. Sie teilt das Risikokapital durch die monetäre Stop-Distanz und rundet das Ergebnis auf den nächsten erlaubten Volumen-Schritt innerhalb der Exchange-Grenzen.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `UseManualVolume` | Umschalten zwischen festem Volumen und risikobasiertem Sizing. | `true` |
| `ManualVolume` | Volumen pro Trade wenn manuelles Sizing aktiv ist. | `0.1` |
| `RiskPercent` | Prozent des Eigenkapitals pro Trade (verwendet wenn `UseManualVolume` false ist). | `9` |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | `45` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `75` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. | `15` |
| `TrailingStepPips` | Minimale Pip-Bewegung vor Aktualisierung des Trailing-Stops. | `5` |
| `MaxPositions` | Maximale Anzahl von Positionseinheiten, die in einer Richtung angesammelt werden können. | `1` |
| `ShiftMinPips` | Minimale EMA-Neigung in Pips für ein gültiges Signal. | `20` |
| `FastLength` | Schnelle EMA-Länge. | `14` |
| `SlowLength` | Langsame EMA-Länge. | `80` |
| `MacdFast` | MACD-Schnellperiode. | `11` |
| `MacdSlow` | MACD-Langsamperiode. | `102` |
| `UseFractalTrailing` | Fraktal-Trailing-Stop-Anpassungen aktivieren/deaktivieren. | `false` |
| `CandleType` | Kerzentyp (Zeitrahmen) für Berechnungen. | `15 Minuten` |

## Implementierungshinweise
- Die Strategie abonniert einen Kerzenstrom und bindet EMA- und MACD-Indikatoren über `SubscribeCandles().Bind(...)`, wodurch Indikatorwerte im Signal-Handler ohne manuelle Buffer-Abfragen empfangen werden.
- Der interne Zustand verfolgt die letzten drei EMA- und MACD-Werte, um das `shift`-Indexing von MQL zu imitieren, das von der originalen Logik benötigt wird.
- Fraktale werden lokal mit einem Fünf-Balken-Rolling-Window berechnet, entsprechend dem MetaTrader-Verhalten, ohne `GetValue` auf dem Indikator aufzurufen.
- Stop- und Take-Profit-Verwaltung wird mit Market-Exits durchgeführt, wenn Preisniveaus verletzt werden, was den Effekt der originalen Positionsmodifikationen widerspiegelt.
- Der `StartProtection()`-Aufruf aktiviert die integrierte StockSharp-Positionsüberwachung für Resilienz bei unerwarteten Verbindungsunterbrechungen.

## Verwendungsempfehlungen
1. Wählen Sie einen geeigneten Kerzentyp (z. B. 15-Minuten-Balken für wichtige FX-Paare), um das ursprüngliche Puria-Setup widerzuspiegeln.
2. Passen Sie die pip-basierten Parameter an den Punktwert des Instruments an. Der Helper skaliert automatisch auf fünfstellige Quotes, aber exotische Instrumente könnten benutzerdefinierte Anpassungen erfordern.
3. Beim Aktivieren des risikobasierten Sizings überprüfen Sie die Portfolio-Bewertung und Volumen-Schritt-Einschränkungen, um sicherzustellen, dass das berechnete Volumen handelbar ist.
4. Kombinieren Sie bei Bedarf mit Portfolio-Level-Geldmanagement oder Session-Filtern; die Strategie konzentriert sich strikt auf Signal- und Trailing-Logik des originalen MQL-Experten.
