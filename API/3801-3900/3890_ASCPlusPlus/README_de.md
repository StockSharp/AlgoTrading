# ASCPlusPlus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **ASC++ Williams Breakout-Strategie** portiert den Legacy-Experten MQL4 „ASC++.mq4“ auf die übergeordnete API von StockSharp. Die Logik sucht nach engen Handelsspannen, die durch den Williams %R-Oszillator bestätigt werden, und platziert dann Stop-Orders leicht über den Kerzenextremen. Nach der Auslösung sorgt das integrierte Risikomanagement dafür, dass die Position durch automatische Gewinnmitnahme, Stop-Loss und optionales Trailing-Verhalten geschützt bleibt.

## Wie die Strategie funktioniert

1. **Indikatorvorbereitung**
   - Schnelle und langsame Williams %R-Oszillatoren (Standard 9 und 54 Perioden) messen den kurzfristigen Impuls.
   - Ein 10-Perioden Average True Range glättet die „ASC“-gewichtete Range-Berechnung.
   - Die dynamischen Schwellenwerte `x1 = 67 + RiskLevel` und `x2 = 33 - RiskLevel` ahmen die ursprünglichen adaptiven Überkauft/Überverkauft-Bänder nach.
2. **Signalbewertung**
   - Jede fertige Kerze berechnet `value2 = 100 - |%R_fast|`. Werte unter `x2` deuten auf ein überverkauftes Umfeld mit Druck nach oben hin; Werte über `x1` weisen auf einen überkauften Zustand hin, der nach unten ausbrechen kann.
   - Aufeinanderfolgende Kerzen, die innerhalb derselben extremen Erhöhung bleiben, erhöhen die Bestätigungszähler. Ein Handel ist erst nach `SignalConfirmation` aufeinanderfolgenden Balken (Standard 5) zulässig, um die ursprünglichen `SigVal` Timer anzunähern.
3. **Auftragserteilung**
   - Wenn der Bereichsfilter (`ATR < EntryRange`) eine Konsolidierung bestätigt und das Momentum übereinstimmt (`%R_fast` über/unter `%R_slow`), platziert die Strategie eine Stop-Order:
     - Kaufstopp bei `High + ATR * 0.5 + EntryStopLevel * PriceStep` für bullische Ausbrüche.
     - Verkaufsstopp bei `Low - ATR * 0.5 - EntryStopLevel * PriceStep` für rückläufige Ausbrüche.
   - Ausstehende Aufträge der Gegenseite werden storniert, um Konflikte zu vermeiden.
4. **Positionsverwaltung**
   - Schutzaufträge werden über `StartProtection` konfiguriert (Take-Profit und Stop-Loss ausgedrückt in Punkten, optionales Trailing aktiviert, wenn `TrailingStopPoints > 0`).
   - Wenn ein neues Signal mit einer bestehenden Position in Konflikt steht (z. B. ein zinsbullischer Ausbruch während einer Short-Position), glättet die Engine sofort das gegnerische Engagement, bevor sie den Ausbruchsbefehl in die Warteschlange stellt, genau wie die Quelle EA.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 15-minütiger Zeitrahmen | Für die Berechnungen verwendete Basiskerzenquelle. |
| `FastLength` | 9 | Williams %R-Länge, die für den schnellen Impulsdetektor verwendet wird. |
| `SlowLength` | 54 | Williams %R Länge, die für den Bestätigungsoszillator verwendet wird. |
| `RangeLength` | 10 | ATR Glättungsfenster ersetzt die manuelle gewichtete Bereichsschleife. |
| `EntryStopLevel` | 10 Punkte | Zusätzlicher Offset (in Preisschritten) zu Breakout-Stop-Orders hinzugefügt. |
| `EntryRange` | 27 Punkte | Maximal zulässige durchschnittliche Reichweite vor Annahme eines Setups. |
| `RiskLevel` | 3 | Passt die Schwellenwerte `x1`/`x2` an, wodurch die Bestätigungsbänder enger oder breiter werden. |
| `SignalConfirmation` | 5 Takte | Anzahl aufeinanderfolgender Kerzen, die im gleichen Extrem bleiben müssen, bevor ein Befehl aktiviert wird. |
| `TakeProfitPoints` | 100 Punkte | Distanz der automatischen Take-Profit-Order. |
| `StopLossPoints` | 40 Punkte | Distanz der automatischen Stop-Loss-Order. |
| `TrailingStopPoints` | 20 Punkte | Aktiviert das Nachlaufverhalten, wenn es größer als Null ist. |

## Konvertierungshinweise

- Das ursprüngliche EA hat manuell ein gewichtetes ATR erstellt; Der StockSharp-Port verwendet den nativen `AverageTrueRange`-Indikator mit demselben 10-Perioden-Lookback. Dies entspricht der Glättungsabsicht und vermeidet gleichzeitig benutzerdefinierte Puffer.
- Die Timer `SigValBuy` und `SigValSell` im Code MQL hingen von minutenbasierten Zählern ab. Die C#-Version emuliert sie mit `SignalConfirmation` aufeinanderfolgenden Kerzenprüfungen, um den Eintragsrhythmus konsistent zu halten, ohne auf winzige Zeitstempel zuzugreifen.
- Ausstehende Eingabebefehle werden mit `BuyStop`/`SellStop`-Helfern umgesetzt. Bevor eine neue Bestellung aufgegeben wird, wird die Gegenseite storniert, was der alten `OrderDelete`-Logik entspricht.
- Das Stop-Management basiert auf `StartProtection`, das automatisch Take-Profit, Stop-Loss und Trailing verarbeitet. Dies deckt die nachgestellte Leiter MQL (`TSLevel1`, `TSLevel2`) auf vereinfachte, aber robuste Weise ab.
- Der gesamte Indikatorzugriff erfolgt über High-Level-Abonnements und Bindungen gemäß den Projektrichtlinien – keine manuellen `GetValue`-Aufrufe oder benutzerdefinierten Indikator-Caches.

## Anwendungstipps

- Die Strategie erwartet Instrumente mit einem definierten `PriceStep`; andernfalls wird standardmäßig `1` verwendet. Passen Sie `EntryStopLevel`, `EntryRange` und die Risikoparameter an die Tick-Größe des Instruments an.
- Reduzieren Sie `SignalConfirmation` für einen aggressiveren Handel in kürzeren Zeitrahmen oder erhöhen Sie ihn, um nur ausgeprägte Konsolidierungen zu handeln.
- Erwägen Sie die Aktivierung der Diagrammzeichnung in einer Host-Anwendung, um die Stop-Orders zu visualisieren und zu bestätigen, dass die Ausbruchsniveaus mit den jüngsten Höchst-/Tiefstständen übereinstimmen.
- Testen Sie immer anhand historischer Daten, da die Strategie sehr empfindlich auf Spread-, Slippage- und Broker-spezifische Preisschrittdefinitionen reagiert.
