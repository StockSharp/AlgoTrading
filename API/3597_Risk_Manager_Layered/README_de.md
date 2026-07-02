# Mehrschichtige Risikoschutzstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Layered Risk Protector Strategy** ist eine direkte Umsetzung des MetaTrader Expertenberaters „RiskManager“. Der Algorithmus verfolgt kontinuierlich die Portfolio-Aktienkurve und passt das Marktengagement mithilfe des Commodity Channel Index (CCI), der Average True Range (ATR)-Multiplikatoren und eines mehrschichtigen Positionsgrößenmodells an. Wenn die Risikokennzahlen unter konfigurierbare Schwellenwerte fallen, wechselt die Strategie automatisch in den Absicherungsmodus, schließt Positionen bei Gewinn- oder Drawdown-Ereignissen und kann optional bei Break-Even abflachen.

## Handelslogik
- **Indikatorbedingungen** – Die Strategie abonniert die primäre Kerzenserie (konfigurierbarer Zeitrahmen) und berechnet:
  - CCI unter Verwendung des benutzerdefinierten Zeitraums. Bei Long-Trades muss der CCI unter den negativen Schwellenwert fallen und bei Short-Trades muss er über den positiven Schwellenwert steigen.
  - ATR mit einer festen Periode von 14, um volatilitätsbereinigte Take-Profit- und Stop-Loss-Distanzen für jede geöffnete Schicht abzuleiten.
  - Ein gleitender Durchschnitt der Kerzenvolumina. Der Handel wird nur aktiviert, wenn der gleitende Durchschnitt der letzten 50 abgeschlossenen Kerzen das vorherige Kerzenvolumen übersteigt, wodurch der ursprüngliche „Aktiv“-Filter nachgebildet wird.
- **Ebeneneinträge** – Die maximale Belichtung wird auf eine konfigurierbare Anzahl von Ebenen verteilt. Jede neue Bestellung verwendet das Volumen pro Ebene (`MaxVolume / Layers`). Zusätzliche Einträge werden blockiert, wenn die relative Layer-Nutzung (`Orders / Layers * 100`) den aktuellen Systemzustand überschreitet.
- **Auftragsverwaltung** – Jede geöffnete Ebene speichert ihren Einstiegspreis zusammen mit ATR-basierten Stop-Loss- und Take-Profit-Levels. Bei jeder abgeschlossenen Kerze wird der Hoch-/Tief-Bereich überprüft, um zu entscheiden, ob eine Schicht aufgrund des Erreichens ihrer Schutzniveaus geschlossen werden sollte.
- **Absicherungsmodus** – Wenn `MultiPairTrading` deaktiviert ist und der berechnete Gesundheitsprozentsatz unter `HedgeLevel` fällt, zeichnet die Strategie die aktuelle Auftragsanzahl auf und beginnt mit dem Öffnen der gegenüberliegenden Ebenen, bis die Absicherungsverhältnisanforderung erreicht ist. Die Absicherung wird automatisch deaktiviert, sobald die Gesundheit den Schwellenwert überschreitet.
- **Eigenkapitalkontrollen** – Mehrere Schutzmaßnahmen spiegeln den ursprünglichen Fachberater wider:
  - Harter Aktienstopp definiert durch `RiskLimit` (Anfangskapital minus Risikolimit).
  - Gewinnziel, ausgedrückt als additiver Ausgleich zum Anfangskapital.
  - Rollierendes „Close Equity“-Niveau, das jedes Mal, wenn alle Positionen erfolgreich reduziert werden, `CloseProfitBuffer` zum aktuellen Saldo hinzufügt.
  - Optionaler Break-Even-Exit, der alle Geschäfte schließt, wenn das Eigenkapital das gespeicherte Break-Even-Kapital erreicht.
  - Manueller „Hard Close“-Schalter, der sofort eine flache Position erzwingt.

## Parameter
- `AllowLong` / `AllowShort` – Aktivieren Sie jeweils lange bzw. kurze Einträge.
- `MaxVolume` – Gesamtes Positionsvolumen, das auf allen Ebenen verteilt ist.
- `Layers` – Maximale Anzahl von Ebenen, die gleichzeitig geöffnet werden können.
- `CciLength` / `CciLevel` – Zeitraum und Schwellenwert für den Filter CCI.
- `StopLossMultiple` / `TakeProfitMultiple` – ATR Multiplikatoren, die Schutzstufen für jede Schicht definieren.
- `CloseProfitBuffer` – Gewinn, der dem Saldo hinzugefügt wird, wenn das rollierende Close-Equity-Ziel recycelt wird. Wird auch bei der Berechnung des Break-Even-Kapitals verwendet.
- `ManualCapital` – Überschreibt das Anfangskapital, das für alle Risikoberechnungen verwendet wird (wird auf Null gesetzt, um den Live-Portfolio-Saldo beim Start zu verwenden).
- `RiskLimit` – Maximal tolerierter Abzug vom Anfangskapital.
- `ProfitTarget` – Additives Gewinnziel, das den Handel pausiert, sobald es erreicht ist.
- `MultiPairTrading` – Bei „true“ wird die Absicherung deaktiviert, selbst wenn der Zustand unter den Grenzwert fällt.
- `HedgeLevel` / `HedgeRatio` – Gesundheitsprozentsatz, der mit der Absicherung beginnt, und Verhältnis der zusätzlichen Schichten, die während des Absicherungsmodus erforderlich sind.
- `CloseAtBreakEven` – Aktiviert die Break-Even-Exit-Logik.
- `HardClose` – Erzwingt eine sofortige Abflachung und unterbricht den weiteren Handel, solange der Wert wahr ist.
- `CandleType` – Kerzenserie, die für die Bewertung von Indikatoren und Handelsentscheidungen verwendet wird.

## Notizen
- Die Strategie geht von einer sofortigen Marktauftragsausführung aus. Bei der Ausführung mit historischen Daten hängt das tatsächliche Ausführungsmodell von den Backtesting-Einstellungen in StockSharp ab.
- Eigenkapital- und Saldoinformationen stammen aus dem verbundenen Portfolio (`Portfolio.CurrentValue`, `Portfolio.CurrentBalance`). Stellen Sie sicher, dass das Strategieportfolio mit dem gehandelten Wertpapier synchronisiert ist.
- Durch die Absicherung werden zusätzliche Marktpositionen für dasselbe Instrument eröffnet. Stellen Sie sicher, dass der Broker oder Simulator entgegengesetzte Positionen zulässt, wenn die Absicherung aktiviert ist.
- Beim Break-Even-Tracking wird der Wert `CloseProfitBuffer` wiederverwendet, genau wie in der Originalversion MetaTrader, die mit einem „ClosePL“-Parameter arbeitete.
