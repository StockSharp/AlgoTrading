# Vortex Indicator Duplex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MetaTrader-Experten **Exp_VortexIndicator_Duplex** in die StockSharp High-Level-API. Es werden zwei unabhängige Vortex-Indikatorströme geführt: einer steuert Long-Trades, der andere Short-Trades. Jeder Strom kann seinen eigenen Zeitrahmen, Indikatorlänge und Balkenversatz verwenden, was asymmetrisches Verhalten zwischen bullischen und bärischen Setups ermöglicht.

## Funktionsweise

1. Zwei Kerzenabonnements werden gemäß `LongCandleType` und `ShortCandleType` geöffnet. Jeder Feed aktualisiert seine eigene `VortexIndicator`-Instanz.
2. Bei jeder abgeschlossenen Kerze zeichnet die Strategie die neuesten VI+- und VI--Werte auf. Die Parameter `LongSignalBar`/`ShortSignalBar` legen fest, wie viele geschlossene Kerzen zurück für die Signalauswertung verwendet werden sollen, entsprechend dem MetaTrader-Eingabewert `SignalBar`.
3. **Long-Einstieg** – erlaubt wenn `AllowLongEntries = true`. Eine Kauforder wird gesendet, wenn der aktuelle VI+-Wert des Long-Stroms über VI- liegt, während der zuvor gesampelte Wert VI+ kleiner oder gleich VI- hatte. Jedes bestehende Short-Engagement wird vor der neuen Long-Position aufgelöst.
4. **Long-Ausstieg** – aktiviert durch `AllowLongExits`. Die Long-Position wird geschlossen, wenn der VI--Wert des Long-Stroms über VI+ steigt. Zusätzlich werden schützende Stop-Loss- und Take-Profit-Niveaus in Preisschritten (`LongStopLossSteps`, `LongTakeProfitSteps`) bei jeder Kerze überwacht; das Erreichen einer der Schwellen schließt den Trade ebenfalls.
5. **Short-Einstieg** – gesteuert durch `AllowShortEntries`. Eine Verkaufsorder wird platziert, wenn der VI+ des Short-Stroms unter VI- fällt, nachdem er zuvor darüber lag. Das bestehende Long-Engagement wird während der Umkehr aufgelöst.
6. **Short-Ausstieg** – kontrolliert durch `AllowShortExits`. Die Short-Position wird gedeckt, wenn VI+ wieder über VI- steigt. Schutzabstände (`ShortStopLossSteps`, `ShortTakeProfitSteps`) schließen den Trade, wenn sie erreicht werden.
7. Die Positionsgröße verwendet den Parameter `TradeVolume`. Die Strategie stützt sich auf den `PriceStep` des Instruments, um Schrittzählungen in absolute Preisabstände umzurechnen; das Setzen eines Schrittparameters auf null deaktiviert die entsprechende Schutzregel.

Die Stop/Take-Prüfungen werden bei jeder abgeschlossenen Kerze beider Zeitrahmen ausgewertet. Hat das Konto keine Position, werden zwischengespeicherte Einstiegsdaten gelöscht, um die MetaTrader-Implementierung zu spiegeln.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `LongCandleType` | H4 | Zeitrahmen für den Long-seitigen Vortex-Indikator. |
| `ShortCandleType` | H4 | Zeitrahmen für den Short-seitigen Indikator. |
| `LongLength` | 14 | VI-Periode angewendet auf den Long-Strom. |
| `ShortLength` | 14 | VI-Periode angewendet auf den Short-Strom. |
| `LongSignalBar` | 1 | Geschlossener-Kerzen-Versatz für die Long-Auswertung (0 = aktuelle abgeschlossene Kerze). |
| `ShortSignalBar` | 1 | Geschlossener-Kerzen-Versatz für die Short-Auswertung. |
| `AllowLongEntries` | true | Aktiviert das Öffnen von Long-Positionen. |
| `AllowLongExits` | true | Aktiviert das Schließen von Long-Positionen. |
| `AllowShortEntries` | true | Aktiviert das Öffnen von Short-Positionen. |
| `AllowShortExits` | true | Aktiviert das Schließen von Short-Positionen. |
| `LongStopLossSteps` | 1000 | Stop-Loss-Abstand für Long-Trades, ausgedrückt in Preisschritten. |
| `LongTakeProfitSteps` | 2000 | Take-Profit-Abstand für Long-Trades, ausgedrückt in Preisschritten. |
| `ShortStopLossSteps` | 1000 | Stop-Loss-Abstand für Short-Trades, ausgedrückt in Preisschritten. |
| `ShortTakeProfitSteps` | 2000 | Take-Profit-Abstand für Short-Trades, ausgedrückt in Preisschritten. |
| `TradeVolume` | 1 | Basis-Marktordergröße beim Einstieg in eine Position. |

## Ausführungshinweise

- Die Strategie schließt jede entgegengesetzte Position, bevor eine neue geöffnet wird, und reproduziert damit das MT5-Verhalten, bei dem separate Magic Numbers Long- und Short-Signale verwalteten.
- Schutzabstände werden über `distance = steps * Security.PriceStep` umgerechnet. Stellen Sie sicher, dass das Instrument einen gültigen Preisschritt hat; andernfalls fällt die Strategie auf 1.0 zurück.
- Setzen Sie einen Stop/Take-Parameter auf null, um diesen Schutzpfad zu deaktivieren, während signalbasierte Ausstiege aktiv bleiben.
- Da beide Zeitrahmen das Risikomanagement auslösen können, wählen Sie `TradeVolume` sorgfältig, um wiederholte Umkehrungen auf dünnen Märkten zu vermeiden.
