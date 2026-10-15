# Grundlegende RSI EA-Vorlagenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Basic RSI EA Template Strategy** repliziert den MetaTrader 4 Expert Advisor „Basic Rsi EA Template.mq4“ (MQL/26750). Es beobachtet den Relative Strength Index (RSI) der ausgewählten Kerzenserie und reagiert, wenn sich die Dynamik in konfigurierbare überkaufte oder überverkaufte Zonen ausdehnt. Die StockSharp-Konvertierung behält den einfachen Ein-Positions-Workflow und die schützende Stop/Take-Logik des ursprünglichen Roboters bei und übernimmt gleichzeitig das High-Level-Abonnement API.

## Strategielogik

### Indikatoren
- **Relative Strength Index (RSI)** mit einem konfigurierbaren Zeitraum, berechnet für den ausgewählten Kerzentyp.

### Teilnahmebedingungen
- **Lange Einrichtung**: Wenn RSI unter `OversoldLevel` fällt und die Strategie keine offene Position hat, sendet sie eine Marktkauforder für den konfigurierten `OrderVolume`.
- **Kurzes Setup**: Wenn RSI über `OverboughtLevel` steigt und die Strategie keine offene Position hat, sendet sie einen Marktverkaufsauftrag für den konfigurierten `OrderVolume`.

Der Algorithmus arbeitet im Netting-Modus: Es kann immer nur eine Position existieren. Wenn eine Long-Position aktiv ist, wartet die Strategie darauf, dass sie geschlossen wird, bevor eine Short-Position eingegangen wird (und umgekehrt).

### Ausstiegsbedingungen
- **Schutzstopp**: `StopLossPips` wird anhand der Tick-Größe des Instruments in einen absoluten Preisabstand umgewandelt. Sobald der Preis um diesen Betrag zurückgeht, schließt die eingebaute Schutzmaschine die Position.
- **Gewinn mitnehmen**: `TakeProfitPips` wird auf die gleiche Weise verarbeitet – wenn sich der Preis um die konfigurierte Distanz zu Ihren Gunsten bewegt, wird die Position mit Gewinn geschlossen.

Es gibt keinen zusätzlichen nachgestellten oder signalbasierten Exit. Die Strategie basiert ausschließlich auf Schutzabständen oder manuellen Eingriffen zum Ausstieg aus Trades und spiegelt das minimalistische Design der ursprünglichen Vorlage wider.

### Umgang mit Risiken und Volumen
- `OrderVolume` definiert den festen Betrag, der mit jeder Market-Order übermittelt wird (standardmäßig 0,01 Lots, entsprechend der MQL-Probe).
- Bei der Strategie handelt es sich weder um eine Pyramide noch um eine Absicherung. Wenn ein schützender Stop oder Take-Profit den aktiven Handel schließt, wird der Algorithmus flach und wartet auf den nächsten RSI-Trigger.

## Parameter
- `CandleType`: Kerzenserie, die zur Signalgenerierung verwendet wird (Standard: 1-Minuten-Zeitrahmen).
- `RsiPeriod`: Anzahl der Balken im Fenster RSI (Standard: 14).
- `OverboughtLevel`: RSI Schwellenwert, der kurze Einträge zulässt (Standard: 70).
- `OversoldLevel`: RSI Schwellenwert, der lange Einträge zulässt (Standard: 30).
- `StopLossPips`: Stop-Distanz in Pips, umgerechnet in absolute Preiseinheiten (Standard: 30 Pips).
- `TakeProfitPips`: Gewinnziel in Pips, umgerechnet in absolute Preiseinheiten (Standard: 20 Pips).
- `OrderVolume`: festes Volumen für Marktaufträge (Standard: 0,01).

## Implementierungshinweise
- Verwendet `SubscribeCandles(...).Bind(rsi, ProcessCandle)`, sodass Indikatorwerte ohne manuelle Pufferverwaltung direkt in die Verarbeitungsmethode einfließen.
- `CreateProtectionUnit` stellt die Pip-Verarbeitung von MQL wieder her: Instrumente mit 3 oder 5 Dezimalstellen verwenden einen 10-fachen Multiplikator, um Pips Preisschritten zuzuordnen.
- Alle Indikatorprüfungen werden für fertige Kerzen ausgeführt, um mehrere Orders auf demselben Balken zu vermeiden.
- Bei der Konvertierung wird im Gegensatz zum Absicherungsmodus von MetaTrader ein Netting-Konto vorausgesetzt. Folglich schließen gegensätzliche Trades die aktuelle Position, anstatt mehrere Tickets zu erstellen.
- Inline-Kommentare und Protokolle sind auf Englisch, um zukünftige Wartungsarbeiten zu erleichtern.

## Nutzungstipps
- Passen Sie `CandleType` an das Instrument und den Zeitrahmen an, mit dem Sie handeln möchten (z. B. wechseln Sie zu stündlichen Kerzen für Swing-Setups).
- Passen Sie `StopLossPips` und `TakeProfitPips` so an, dass sie der Volatilität des Instruments entsprechen. Die Schutzabstände sind für die Risikokontrolle unerlässlich.
- Kombinieren Sie die Strategie mit StockSharp Portfolio- oder Risikomodulen, wenn Sie ein erweitertes Geldmanagement benötigen, das über die Vorlagenlogik hinausgeht.
