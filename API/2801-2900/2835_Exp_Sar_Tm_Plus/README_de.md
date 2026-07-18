# Strategie Exp Sar Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

High-Level StockSharp-Port des Expert Advisors **Exp_Sar_Tm_Plus**. Die Strategie überwacht Parabolic SAR-Umkehrungen auf einem konfigurierbaren Zeitrahmen und repliziert die ursprünglichen Geldverwaltungs- und Timeout-Funktionen, während die Logik mit der StockSharp High-Level-API kompatibel bleibt.

## Handelslogik

- Kerzen werden aus dem Parameter `CandleType` abonniert (Standard: 4-Stunden-Zeitrahmen). Der Parabolic SAR-Indikator wird mit den vom Benutzer definierten Koeffizienten `SarStep` und `SarMaximum` berechnet.
- Für jede fertige Kerze puffert der Algorithmus Schlusskurse und SAR-Werte. Der Parameter `SignalBar` wählt aus, welche geschlossene Kerze ausgewertet wird (Standard: die zuletzt geschlossene Kerze) und vergleicht sie mit der vorherigen Kerze, um eine Änderung der SAR-Richtung zu erkennen.
- Eine **Long**-Position wird geöffnet, wenn der Preis den SAR nach **oben** kreuzt (vorherige Kerze unterhalb SAR, ausgewählte Kerze oberhalb SAR) und Long-Trading aktiviert ist. Bestehende Short-Exposition wird vor dem Richtungswechsel automatisch geschlossen.
- Eine **Short**-Position wird geöffnet, wenn der Preis den SAR nach **unten** kreuzt (vorherige Kerze oberhalb SAR, ausgewählte Kerze unterhalb SAR) und Short-Trading aktiviert ist. Bestehende Long-Exposition wird zuerst aufgelöst.
- Positionen werden geschlossen, wenn sich der SAR gegen sie bewegt (`AllowLongExit` / `AllowShortExit`), wenn optionale Stop-Loss / Take-Profit-Niveaus verletzt werden, oder wenn die maximale Haltezeit (`UseTimeExit` + `HoldingMinutes`) abläuft.
- Stop-Loss- und Take-Profit-Niveaus werden bei jedem Einstieg mit dem Instrument-`PriceStep` neu berechnet. Beide Niveaus sind optional und werden ignoriert, wenn der entsprechende Wert null ist.

## Parameter

- `MoneyManagement` – Bruchteil des Basis-`Volume`, der bei jedem Einstieg gehandelt wird. Werte ≤ 0 fallen auf den einfachen `Volume`-Wert zurück. Normalisiert auf den Instrument-`VolumeStep`.
- `ManagementMode` – Enumeration, die vom ursprünglichen Experten beibehalten wird. Alle Modi verhalten sich derzeit wie `Lot` (fixes Volumen) in diesem Port.
- `StopLossPoints` / `TakeProfitPoints` – Abstand in Preisschritten zur Festlegung von Schutzniveaus um den Einstiegspreis. Auf null setzen zum Deaktivieren.
- `DeviationPoints` – originale Slippage-Einstellung. Wird der Vollständigkeit halber beibehalten, aber die High-Level-API führt Marktorders ohne Verwendung dieses Werts aus.
- `AllowLongEntry`, `AllowShortEntry` – Schalter für das Öffnen von Long/Short-Positionen.
- `AllowLongExit`, `AllowShortExit` – Schalter für das Schließen von Positionen, wenn der Preis den SAR in die entgegengesetzte Richtung kreuzt.
- `UseTimeExit` – aktiviert die Positionsliquidation nach `HoldingMinutes` Minuten im Markt.
- `HoldingMinutes` – Dauer für das zeitbasierte Ausstiegsfenster.
- `CandleType` – Kerzendatentyp für SAR-Analyse.
- `SarStep`, `SarMaximum` – Parabolic SAR-Konfiguration.
- `SignalBar` – Anzahl der geschlossenen Kerzen zur Verschiebung der Signalauswertung (0 = aktuelle fertige Kerze, 1 = vorherige, usw.).

## Risikomanagement und Hinweise

- Die Strategie ruft beim Start `StartProtection()` auf und aktiviert damit die integrierten Schutzdienste von StockSharp.
- Zeitbasierte Ausstiege verlassen sich auf die `CloseTime` der Kerze (Fallback auf `OpenTime` wenn nicht verfügbar), um den Haltezeitraum genau zu messen.
- Es wird jederzeit nur eine Nettoposition gehalten. Positionsumkehrungen schließen die entgegengesetzte Seite automatisch, bevor eine neue Position eingegangen wird.
- Die Implementierung behält den Parametersatz des ursprünglichen MQL5-Experten bei. Einige Optionen (wie Nicht-`Lot`-Geldverwaltungsmodi oder Order-`DeviationPoints`) sind Platzhalter, weil die High-Level-API Broker-seitige Mechanismen abstrahiert.
