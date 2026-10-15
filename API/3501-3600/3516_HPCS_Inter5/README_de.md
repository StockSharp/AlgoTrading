# HPCS Inter5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **HPCS Inter5-Strategie** ist ein Single-Shot-Momentum-Skript, das vom MetaTrader 4-Experten `_HPCS_Inter5_MT4_EA_V01_WE` konvertiert wurde. Wenn die Strategie startet, prüft sie die letzten abgeschlossenen Kerzen und sendet einen Marktkaufauftrag, wenn der Schlusskurs von vor fünf Balken höher ist als der letzte Schlusskurs. Optionale schützende Stop-Loss- und Take-Profit-Distanzen emulieren das Pip-basierte Verhalten des ursprünglichen EA.

## Handelslogik

1. Abonnieren Sie die konfigurierte Kerzenserie und behalten Sie die letzten sechs abgeschlossenen Abschlüsse bei.
2. Nachdem der Puffer gefüllt ist, vergleichen Sie den Schlusskurs von vor fünf Balken mit dem letzten Schlusskurs (`Close[5] > Close[1]` in MetaTrader-Begriffen).
3. Wenn die Bedingung erfüllt ist und noch kein Handel getätigt wurde, senden Sie eine Marktkauforder mit dem konfigurierten Volumen.
4. Schutzbefehle werden einmalig beim Start bis `StartProtection` aktiviert, wobei eine Pip-Umrechnung im MetaTrader-Stil verwendet wird: Instrumente mit 3 oder 5 Dezimalstellen multiplizieren `PriceStep` mit 10, um die Pip-Größe zu bestimmen, andernfalls wird die Rohgröße `PriceStep` verwendet.

Die Strategie eröffnet keine zusätzlichen Trades und ignoriert jedes nachfolgende Signal, sobald die erste Position besetzt ist.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Candle Type` | Zeitrahmen von 1 Minute | Kerzentyp, der zur Erfassung der Schlusskurse verwendet wird. Stellen Sie den Zeitrahmen ein, der Ihrem gewünschten Signalintervall entspricht. |
| `Stop Loss (pips)` | 10 | Abstand für den schützenden Stop-Loss in MetaTrader Pips. Ein Wert von `0` deaktiviert den Stopp. |
| `Take Profit (pips)` | 10 | Abstand für den schützenden Take-Profit in MetaTrader Pips. Ein Wert von `0` deaktiviert den Take-Profit. |
| `Trade Volume` | 1 | Volumen der Market-Order, die bei Auslösung der Eintrittsbedingung übermittelt wird. |

## Implementierungshinweise

- Die Strategie erfordert einen konfigurierten `Security.PriceStep` (oder `Security.Step`), um Pip-Abstände umzuwandeln. Fehlt diese Information, bleiben die Schutzoffsets inaktiv, das Einfahrsignal funktioniert jedoch weiterhin.
- Nur fertige Kerzen (`CandleStates.Finished`) werden verarbeitet, um dem MetaTrader-Verhalten zu entsprechen, das auf `Close[1]` und älteren Werten basiert.
- Der interne Puffer speichert genau sechs Schließungen, ohne die Indikatorhistorie zu verwenden, wobei der minimalistische Charakter der Quelle EA berücksichtigt wird.
- `IsFormedAndOnlineAndAllowTrading()` wird vor dem Senden der Bestellung überprüft, um sicherzustellen, dass die Umgebung zur Ausführung bereit ist.

## Nutzungstipps

1. Weisen Sie ein Forex-Instrument mit den richtigen Preis- und Volumeneinstellungen zu.
2. Passen Sie `Candle Type` an den Zeitrahmen an, den Sie analysieren möchten.
3. Lassen Sie den Stop-Loss oder Take-Profit bei Null, wenn Sie Exits lieber manuell verwalten möchten.
4. Starten Sie die Strategie immer dann neu, wenn Sie die Eintrittsbedingung neu bewerten möchten, da sie nur einmal pro Sitzung ausgelöst wird.
