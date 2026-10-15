# Strategie Vorherige-Kerzen-Ausbruch 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie, die den MetaTrader-Experten "Previous Candle Breakdown 2" nachbildet. Der Algorithmus beobachtet die zuletzt abgeschlossene Kerze auf einem konfigurierbaren Zeitrahmen und löst Trades aus, wenn der Preis ihren Hoch- oder Tiefpunkt um einen benutzerdefinierten Pip-Offset durchbricht. Optionale Filterung durch gleitende Durchschnitte, strikte Handelszeiten, Positionsgrößen durch festes Volumen oder prozentuales Risiko sowie mehrschichtige Schutzausstiege replizieren das Verhalten der originalen MQL-Version innerhalb von StockSharp.

## Überblick
- **Einstiegslogik**: Long einsteigen, wenn der Preis den Hoch der vorherigen Kerze plus einem Einzug überschreitet. Short einsteigen, wenn der Preis unter den Tief der vorherigen Kerze minus demselben Einzug bricht.
- **Filter**: Optionale schnelle/langsame gleitende Durchschnitte mit Shift-Parametern erfordern eine Richtungsbestätigung vor dem Handel. Der Handel ist auch auf ein Start-/Endzeitfenster beschränkt.
- **Positionsgrößen**: Wahl zwischen einem festen Ordervolumen oder dynamischer Größenanpassung basierend auf dem Portfoliowert und dem Stop-Loss-Abstand.
- **Risikokontrollen**: Statische Stop-Loss- und Take-Profit-Niveaus in Pips, Trailing Stop mit einem Schrittfilter und ein globales Gewinnziel, das alle Positionen schließt.
- **Skalierung**: Das `MaxPositions`-Limit begrenzt die absolute Netto-Positionsgröße für jede Richtung.

## Standardwerte
- `IndentPips` = 10
- `FastPeriod` = 10, `FastShift` = 3, `SlowPeriod` = 30, `SlowShift` = 0, `MaMethod` = Simple
- `StopLossPips` = 50, `TakeProfitPips` = 150
- `TrailingStopPips` = 15, `TrailingStepPips` = 5
- `ProfitClose` = 100 (Währungseinheiten aus realisiertem + unrealisiertem PnL)
- `MaxPositions` = 10 (absoluter Kontrakt-/Lotanzahl pro Seite)
- `OrderVolume` = 0 (deaktiviert), `RiskPercent` = 5 (verwendet wenn `OrderVolume` null ist und Stop-Loss aktiv ist)
- `StartTime` = 09:09, `EndTime` = 19:19
- `CandleType` = 4-Stunden-Zeitrahmen

## Handelsregeln
1. Die konfigurierte Kerzenserie abonnieren und jede fertige Kerze aufzeichnen.
2. Prüfen, ob die aktuelle Zeit in die erlaubte Handelssitzung fällt. Wenn `ProfitClose` erreicht ist, sofort glätten.
3. Ausbruchsniveaus berechnen, indem der Pip-Einzug zum Hoch und Tief der vorherigen Kerze addiert/subtrahiert wird.
4. Wenn der Preis diese Niveaus durchbricht und die MA-Bedingungen (sofern aktiviert) erfüllt sind, Trades unter Einhaltung des `MaxPositions`-Limits eröffnen.
5. Anfängliche Stop-Loss- und Take-Profit-Abstände vom Eintrittspreis setzen und Trailing Stops aktivieren, sobald sich der Preis mindestens den Trailing-Abstand plus Schritt zugunsten des Trades bewegt hat.
6. Kerzen kontinuierlich überwachen: Stop-Loss-/Take-Profit-Ausstiege beim Berühren auslösen, Stops beim Preisfortschritt nachziehen und Schutzlevel zurücksetzen, sobald Positionen geschlossen sind.

## Hinweise
- Pip-Berechnungen passen sich automatisch für 3 oder 5 Dezimalstellen-Instrumente an, um MetaTraders Punkt-zu-Pip-Konvertierung zu imitieren.
- Bei der Verwendung der prozentualen Risikogrößenanpassung schätzt der Algorithmus das Volumen aus dem aktuellen Portfoliowert und dem konfigurierten Stop-Loss.
- Die Ausbruchsprüfung verwendet fertige Kerzen, daher werden Intrabar-Spitzen auf Kerzenschluss-/Hoch-/Tiefniveaus bewertet.
- `MaxPositions` arbeitet mit der Netto-Position der Strategie. Bei Verwendung von Bruchvolumina stellt der Parameter die maximal erlaubte absolute Nettogröße pro Richtung dar.
- Charts zeigen Kerzen, die aktiven gleitenden Durchschnitte wenn aktiviert und ausgeführte Trades zur visuellen Bestätigung.
