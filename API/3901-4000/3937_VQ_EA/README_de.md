# Strategie VQ EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader-Experten „VQ_EA“, der mit dem Indikator „Volatilitätsqualität“ (VQ) handelt.
- Die StockSharp-Version nähert sich der VQ-Linie mit einem geglätteten Medianpreis an, um die Logik innerhalb des oberen API zu halten.
- Positions are opened on direction changes of the smoothed line and managed with optional protective orders.

## Ursprüngliches MQL-Verhalten
1. Fordert Kauf- oder Verkaufssignale vom benutzerdefinierten VQ-Indikator an (Puffer 3 und 4).
2. Opens a new market position when a fresh signal appears and no trade is active in that direction.
3. Schließt die Gegenposition sofort bei einem Gegensignal.
4. Optionale Geldverwaltungsfunktionen: feste Lots, Teillots, Break-Even, Trailing Stop, manuelle Protokollausgabe und Alarm-/E-Mail-Benachrichtigungen.

## StockSharp-Implementierung
- Anstelle des proprietären VQ-Indikators wendet die Strategie einen einfachen gleitenden Durchschnitt auf den Medianpreis an und glättet ihn optional noch einmal.
- Die Steigung der geglätteten Reihe spielt die Rolle der ursprünglichen Farbänderung der VQ-Linie.
- Ein konfigurierbarer, in Punkten ausgedrückter Filter verhindert Signale, die durch geringfügige Schwankungen verursacht werden.
- Marktaufträge werden für Ein- und Ausstiege verwendet und spiegeln das ursprüngliche EA-Verhalten wider.

### Signalerzeugung
1. Abonnieren Sie den ausgewählten Kerzentyp und berechnen Sie den Medianpreis für jede fertige Kerze.
2. Wenden Sie den gleitenden Basisdurchschnitt (`Length`) und, falls gewünscht, eine zusätzliche Glättung (`Smoothing`) an.
3. Vergleichen Sie den aktuellen geglätteten Wert mit dem vorherigen. Wenn die absolute Änderung `FilterPoints` (umgerechnet in Preiseinheiten) überschreitet, markieren Sie die Richtung als steigend oder fallend.
4. Wenn die Richtung von unten nach oben wechselt, wird ein langer Eintrag ausgegeben. Ein Flip von oben nach unten führt zu einem kurzen Einstieg. Bestehende Positionen werden durch Addition des absoluten Positionsvolumens zur Ordergröße rückgängig gemacht.

### Risikomanagement
- `StopLossPoints`, `TakeProfitPoints` und `TrailingStopPoints` werden durch Multiplikation mit der Instrumentenpreisstufe in absolute Preise umgerechnet.
- Wenn mindestens einer dieser Schutzmaßnahmen aktiviert ist, wird `StartProtection` mit Market-Order-Anpassungen aufgerufen, sodass Stopps der Position folgen, wie im MQL-Experten.
- Der optionale Trailing Stop wird nur aktiviert, wenn `UseTrailing` den Wert `true` hat und die Trailing-Distanz größer als Null ist.

## Parameter
- `Length` – Basis-Glättungszeitraum des Medianpreises. Standard: 5.
- `Smoothing` – sekundärer Glättungszeitraum. Standard: 1 (deaktiviert).
- `FilterPoints` – minimale Punktverschiebung, die erforderlich ist, um zu bestätigen, dass sich die Steigung geändert hat. Default: 5.
- `StopLossPoints` – schützender Stop-Loss in Punkten. Standard: 60 (0 deaktiviert es).
- `TakeProfitPoints` – schützender Take-Profit in Punkten. Standard: 0 (deaktiviert).
- `UseTrailing` – Trailing Stops aktivieren oder deaktivieren. Default: false.
- `TrailingStopPoints` – Nachlaufdistanz in Punkten. Standard: 0 (wird ignoriert, wenn `UseTrailing` falsch ist).
- `CandleType` – für Berechnungen verwendeter Zeitrahmen. Standard: 1-Stunden-Kerzen.
- `Volume` – geerbt von `Strategy.Volume`, standardmäßig 1 Vertrag und wird für jeden neuen Eintrag verwendet.

## Unterschiede zum ursprünglichen Experten
- Die genauen VQ-Pufferwerte werden durch geglättete Medianpreise angenähert; Der Indikator ist nicht eins zu eins portiert.
- Erweiterte Funktionen wie Break-Even-Schichten, Planung von Warntönen, manuelle Protokollausgabe und Teilmengen-Geldverwaltung werden nicht reproduziert.
- Die Handhabung von Trailing-Steps wird durch den integrierten Trailing-Stop-Manager von StockSharp vereinfacht.

## Nutzungshinweise
- Signale werden nur bei fertigen Kerzen generiert und entsprechen dem Modus „Handel bei Bar-Schluss“ des ursprünglichen EA.
- Stellen Sie sicher, dass das Instrument über einen ordnungsgemäßen `PriceStep` verfügt. Andernfalls greift die Strategie bei der Konvertierung punktbasierter Parameter auf einen Schritt von 1,0 zurück.
- Die Strategie ist zu Demonstrationszwecken gedacht und kann bei Bedarf um weitere Money-Management-Regeln erweitert werden.
