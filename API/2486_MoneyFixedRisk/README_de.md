# Strategie mit festem Risiko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie mit festem Risiko ist ein direkter Port des MetaTrader 5 Expert Advisors **Money Fixed Risk.mq5**. Das ursprüngliche Skript berechnet periodisch die maximale Positionsgröße, die das Risiko unter einem festen Prozentsatz des Kontokapitals hält, und öffnet dann einen Marktkauf, der mit symmetrischen Stop-Loss- und Take-Profit-Orders abgesichert ist. Diese StockSharp-Version bewahrt dasselbe Verhalten mithilfe der High-Level-Tick-Abonnement-API und der vom Framework bereitgestellten Risikokontrollen.

Die Strategie hört auf jeden Trade (Tick) des ausgewählten Instruments. Nach einer konfigurierbaren Anzahl von Ticks wertet sie den aktuellen Portfoliowert aus, wandelt die konfigurierte Stop-Distanz in Pips in Preiseinheiten um und berechnet das größte Volumen, das das Risiko innerhalb des angegebenen Kapitalanteils hält. Wenn das berechnete Volumen gültig ist, öffnet die Strategie eine Long-Marktorder und weist Stop-Loss- und Take-Profit-Niveaus genau im Stop-Abstand vom Ausführungspreis zu. Stop und Ziel werden auf jedem folgenden Tick überwacht und die Position wird geschlossen, sobald eine der Grenzen berührt wird.

## Datenanforderungen
- Tick-(Trade-)Daten sind erforderlich, weil die Einstiegsbedingung einzelne Ticks zählt. Kerzendaten werden nicht verwendet.
- `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` und das optionale `MaxVolume` müssen für das Instrument korrekt konfiguriert sein, damit die Positionsgrößenformel den Broker-Kontraktspezifikationen entspricht.

## Funktionsweise der Strategie
1. Auf Tick-Updates über `SubscribeTrades()` warten.
2. Den zuletzt gehandelten Preis verfolgen und einen internen Zähler erhöhen.
3. Immer wenn der Tick-Zähler das **Ticks Interval** erreicht, den Zähler zurücksetzen und:
   - Die Pip-Größe aus `PriceStep` und `Decimals` bestimmen (5- und 3-stellige Kurse werden automatisch um 10 skaliert).
   - Die konfigurierte Stop-Loss-Distanz von Pips in Preiseinheiten umrechnen.
   - Das aktuelle Kontokapital bestimmen (versucht `Portfolio.CurrentValue`, fällt auf `CurrentBalance` zurück, dann `BeginValue`).
   - Das monetäre Risiko pro Kontrakt mithilfe der Stop-Distanz und `StepPrice` berechnen.
   - Das maximale Volumen ableiten, das das monetäre Risiko unter `Risk %` des Kapitals hält, und es auf den Exchange-Volumenschritt und -Grenzen normalisieren.
4. Wenn das berechnete Volumen positiv ist, eine Kauf-Marktorder senden, die jedes bestehende Short-Engagement ausgleicht und eine neue Long-Position öffnet.
5. Die Stop-Loss- und Take-Profit-Preise rund um den Einstiegspreis aufzeichnen. Auf jedem folgenden Tick den Handelspreis überwachen und die Position schließen, wenn ein Niveau verletzt wird.

## Parameter
- **Stop Loss (pips)** – Stop-Loss-Distanz in Pips. Der Take-Profit wird in entgegengesetzter Richtung im gleichen Abstand platziert.
- **Risk %** – Prozentsatz des Portfolio-Kapitals, das pro Trade riskiert wird.
- **Ticks Interval** – Anzahl der Ticks, die gewartet werden, bevor eine neue Position reevaluiert und potenziell geöffnet wird.

Alle Parameter unterstützen Optimierung und Validierung (müssen größer als null sein).

## Details zum Money Management
- Risikobetrag = `Equity * (Risk % / 100)`.
- Stop-Distanz in Preiseinheiten = `Stop Loss (pips) * pip size`, wobei pip size für 3- und 5-Dezimalinstrumente `PriceStep * 10` entspricht, sonst `PriceStep`.
- Monetäres Risiko pro Kontrakt = `(stop distance / PriceStep) * StepPrice`.
- Positionsgröße = `Risk amount / monetary risk per contract`, abgerundet auf den nächsten `VolumeStep` und begrenzt durch `MinVolume`/`MaxVolume`. Orders werden übersprungen, wenn die normalisierte Größe unter dem Mindestvolumen liegt.

## Unterschiede zum Original-Expert-Advisor
- Läuft vollständig innerhalb von StockSharp ohne MetaTrader-Bibliotheken aufzurufen.
- Verwendet `StartProtection()`, damit plattformseitige Schutzmaßnahmen aktiv bleiben.
- Stützt sich auf das Strategie-Portfolio für aktuelle Kapitalinformationen statt Terminal-Saldoobjekte abzufragen.
- Nutzt kontinuierliche Tick-Überwachung zum Schließen von Positionen, was die Notwendigkeit expliziter Stop-Orders in diesem Lehrbeispiel entfernt.

## Verwendungshinweise
- Dieses Beispiel öffnet nur Long-Positionen genau wie die ursprüngliche Datei. `ProcessTrade` erweitern, wenn Short-Trades erforderlich sind.
- Beim Backtesting sicherstellen, dass die Tick-Daten genug Tiefe haben, um das konfigurierte Tick-Intervall zu erreichen; andernfalls werden keine Trades ausgelöst.
- Da die Positionsgrößenbestimmung von Broker-Metadaten abhängt, die Korrektheit von `PriceStep`, `StepPrice` und Volumenbeschränkungen vor dem Livebetrieb überprüfen.
- Die Implementierung vermeidet die Verwendung von Indikator-Sammlungen, um die Konvertierungsrichtlinien einzuhalten, und hält die Logik durch private Felder zustandsbehaftet.
