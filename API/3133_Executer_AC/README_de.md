# Executer AC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Executer AC-Strategie** ist ein originalgetreuer StockSharp-Port des MetaTrader 5-Expertenberaters „Executer AC". Der ursprüngliche EA handelt auf dem **Accelerator Oscillator (AC)** von Bill Williams und kombiniert seine Momentum-Schwingungen mit einem festen Stop/Limit-Framework und einem Trailing-Stop-Modul. Diese Konvertierung behält das Verhalten der MQL5-Version bei und bietet benutzerfreundliche Parameter, die sich in die StockSharp High-Level-API integrieren.

## Handelslogik

Die Strategie operiert mit fertigen Kerzen des ausgewählten Zeitrahmens und stützt sich auf die letzten vier Accelerator Oscillator-Werte:

- `AC[0]` – zuletzt abgeschlossene Kerze (im Originalcode als `ac[1]` bezeichnet).
- `AC[1]`, `AC[2]`, `AC[3]` – progressiv ältere Werte für die Mustererkennung.

Der Entscheidungsbaum ist identisch mit dem EA:

1. **Positionsmanagement**
   - Long-Positionen werden geschlossen wenn `AC[0] < AC[1]` (Momentum abnehmend).
   - Short-Positionen werden geschlossen wenn `AC[0] > AC[1]` (Momentum zunehmend).
   - Eine Trailing-Stop-Routine zieht den Schutz-Stop dynamisch enger, sobald der Preis die konfigurierte Distanz plus den Trailing-Schritt überschreitet.
2. **Einstiegsregeln bei flacher Position**
   - **Bullische Beschleunigung über null:** wenn `AC[0] > 0` und `AC[1] > 0` und `AC[0] > AC[1] > AC[2]`, wird ein Markt-Kauf ausgelöst.
   - **Bärische Beschleunigung über null:** wenn `AC[0] > 0` und `AC[1] > 0` und `AC[0] < AC[1] < AC[2] < AC[3]`, wird ein Markt-Verkauf ausgelöst.
   - **Bullische Beschleunigung unter null:** wenn `AC[0] < 0` und `AC[1] < 0` und `AC[0] > AC[1] > AC[2] > AC[3]`, wird ein Markt-Kauf ausgelöst.
   - **Bärische Beschleunigung unter null:** wenn `AC[0] < 0` und `AC[1] < 0` und `AC[0] < AC[1] < AC[2]`, wird ein Markt-Verkauf ausgelöst.
   - **Null-Linien-Kreuzungen:** ein Abwärts-Kreuz (`AC[0] > 0` und `AC[1] < 0`) löst einen Kauf aus; ein Aufwärts-Kreuz (`AC[0] < 0` und `AC[1] > 0`) löst einen Verkauf aus.

Signale werden erst nach Bestätigung evaluiert, dass Kerzen abgeschlossen, Indikatorwerte geformt und der Handel aktiviert ist.

## Risikomanagement

- **Stop-Loss und Take-Profit:** konfigurierbare Abstände (in Pips) werden in Preiseinheiten mithilfe des Instrument-Steps umgerechnet. Stops werden bei jedem neuen Einstieg neu berechnet und bleiben unverändert, bis sie getroffen oder durch den Trailing-Stop ersetzt werden.
- **Trailing-Stop:** spiegelt die EA-Logik wider. Wenn der unrealisierte Gewinn `TrailingStop + TrailingStep` (beide in Pips) überschreitet, wird der Stop-Preis auf `Close - TrailingStop` für Long-Positionen und `Close + TrailingStop` für Short-Positionen verschoben, wobei die erforderliche Verbesserung vor jedem Schritt durchgesetzt wird.
- **Positionsschutz:** der eingebaute `StartProtection()`-Helper wird aufgerufen, damit StockSharp gegen unerwartete Verbindungsunterbrechungen schützt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Ordervolumen für alle Markteintritte. Wird gemäß Volumen-Step und Grenzen des Instruments normalisiert. |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Ein Wert von null deaktiviert den Stop-Loss. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Ein Wert von null deaktiviert den Take-Profit. |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Null zum Deaktivieren des Trailing setzen. |
| `TrailingStepPips` | Minimaler zusätzlicher Gewinn (in Pips), bevor der Trailing-Stop erneut bewegt wird. |
| `CandleType` | Zeitrahmen der Kerzen für die Accelerator Oscillator-Berechnung. |

## Implementierungshinweise

- Die Preisnormalisierung berücksichtigt sowohl die Tick-Größe der Börse als auch Drei/Fünf-Stellen-Forex-Symbole, indem die Punktgröße bei Bedarf mit zehn multipliziert wird.
- Die Indikatorgeschichte wird in einem fest dimensionierten Puffer gespeichert, um die ursprünglichen `ac[1] … ac[4]`-Vergleiche zu replizieren, ohne auf aufwändige Collections oder Historienabfragen zurückzugreifen.
- Die Strategie verlässt immer die aktuelle Position, bevor neue Eintritte auf derselben Kerze bewertet werden, was dem Kontrollfluss des MQL5-EA entspricht, wo `return`-Anweisungen sofortigen Wiedereinstieg verhindern.
- Trailing-Stop-Werte aktualisieren sowohl den internen Trailing-Zustand als auch den effektiven Stop-Preis für Stop-Loss-Prüfungen und gewährleisten Konsistenz mit dem `PositionModify`-Verhalten des EA.

## Nutzungshinweise

1. Wählen Sie einen Kerzen-Zeitrahmen, der zum gehandelten Markt passt (das Originalskript wurde häufig auf Intraday-Forex-Charts verwendet).
2. Passen Sie Stop-Loss-, Take-Profit- und Trailing-Abstände an die Volatilität des gewählten Instruments an; extrem enge Werte können zu häufigen Whipsaws führen.
3. Aktivieren Sie Risikokontrollen auf der Broker-Seite wenn möglich, da die Strategie auf Software-seitige Ausstiege angewiesen ist.
4. Kombinieren Sie mit portfolioweitem Money-Management, wenn Sie beabsichtigen, mehrere Strategien gleichzeitig zu betreiben.
