# Reverse Day Fractal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Reverse Day Fractal ist eine Preisaktionsstrategie, die nach scharfen Umkehrungen nach einem Intraday-Ausbruch sucht. Der Algorithmus analysiert die letzten drei fertigen Kerzen. Wenn der aktuelle Balken ein neues Extrem jenseits der vorherigen zwei Kerzen bildet und zurück in die entgegengesetzte Richtung schließt, behandelt er dies als gescheiterten Ausbruch und tritt in einen Umkehrtrade ein. Schutzorders werden durch konfigurierbare Take-Profit-, Stop-Loss- und Trailing-Stop-Abstände in Preisschritten verwaltet.

## Handelslogik
- **Bullisches Setup**:
  - Die aktuelle fertige Kerze bildet ein *tieferes Tief* als jede der zwei vorherigen Kerzen.
  - Die Kerze schließt *über* ihrem Eröffnungspreis, was eine bullische Ablehnung des neuen Tiefs anzeigt.
  - Wenn diese Bedingungen erfüllt sind und die Strategie handeln darf, öffnet sie eine Long-Position. Optional kann zunächst ein bestehender Short geschlossen werden.
- **Bärisches Setup**:
  - Die aktuelle fertige Kerze bildet ein *höheres Hoch* als jede der zwei vorherigen Kerzen.
  - Die Kerze schließt *unter* ihrem Eröffnungspreis, was eine bärische Ablehnung des neuen Hochs anzeigt.
  - Wenn diese Bedingungen erfüllt sind, öffnet sie eine Short-Position, optional mit vorherigem Schließen eines bestehenden Longs.
- **Positionsverwaltung**: Die Strategie kann so konfiguriert werden, dass nur eine offene Position gleichzeitig erlaubt ist (Standardverhalten). Wenn deaktiviert, kehrt sie eine bestehende Position um, indem das erforderliche Volumen zum Richtungswechsel hinzugefügt wird.
- **Risikokontrollen**: Beim Start ruft die Strategie `StartProtection` auf, um Take-Profit-, Stop-Loss- und Trailing-Stop-Schutz mit den konfigurierten Punktabständen anzuwenden. Wenn ein Trailing Stop aktiviert ist, folgt der Schutzstop dem Preis in diskreten Schritten.

## Parameter
- `Trade Volume` – Ordervolumen für neue Einstiege.
- `Take Profit` – Abstand zum Gewinnziel in Preisschritten. Null zum Deaktivieren.
- `Stop Loss` – Abstand zum Schutzstop in Preisschritten. Null zum Deaktivieren.
- `Trailing Stop` – Trailing-Stop-Abstand in Preisschritten. Null zum Deaktivieren.
- `Trailing Step` – Mindestbewegung (in Schritten), bevor der Trailing Stop angepasst wird.
- `Only One Position` – Wenn aktiviert, ignoriert die Strategie neue Signale, während eine Position offen ist.
- `Candle Type` – Für Berechnungen verwendeter Kerzendatentyp (Standard: 1-Stunden-Zeitrahmen).

## Hinweise
- Signale werden nur auf fertigen Kerzen erzeugt, die durch das konfigurierte Abonnement geliefert werden.
- Die Strategie hält die zwei jüngsten Kerzenextrema im Speicher; daher benötigt sie mindestens zwei abgeschlossene Kerzen nach dem Start, bevor sie ein Signal erzeugen kann.
- Standard-Parameterwerte replizieren den ursprünglichen MQL4-Expertenberater: 0.01 Lot Volumen, 20-Punkt Stop-Loss, 10-Punkt Take-Profit, 25-Punkt Trailing Stop und 5-Punkt Trailing Step.
