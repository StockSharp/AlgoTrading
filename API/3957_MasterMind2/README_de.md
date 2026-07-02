# MasterMind 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
MasterMind 2 ist eine Konvertierung des Expertenberaters „TheMasterMind2“ MQL4. Die Strategie wartet auf Extremwerte der Indikatoren Stochastic Oscillator und Williams %R, um Erschöpfungspunkte zu erkennen. Wenn beide Indikatoren extrem überverkaufte Bedingungen anzeigen, wird eine Long-Position eröffnet, und wenn beide Indikatoren extrem überkaufte Bedingungen anzeigen, wird eine Short-Position eröffnet. Die Logik funktioniert nur bei vollständig geschlossenen Kerzen und ahmt das ursprüngliche Verhalten des Expert Advisors nach.

## Indikatoren
- **Stochastic Oszillator** – konfiguriert mit einem langen Lookback, um überkaufte und überverkaufte Niveaus zu messen. Die %D-Signalleitung wird mit Schwellenwerten verglichen.
- **Williams %R** – bestätigt die Stärke des Extrems, indem es Werte nahe -100 für Long-Positionen und nahe 0 für Short-Positionen erfordert.

## Teilnahmebedingungen
1. Warten Sie, bis sich eine Kerze schließt.
2. Berechnen Sie den Stochastic-Oszillator und nehmen Sie seinen %D-Signalwert.
3. Berechnen Sie Williams %R über den konfigurierten Lookback.
4. **Long-Einstieg**: Wenn `%D < 3` und `Williams %R < -99.9`, schließen Sie alle bestehenden Short-Positionen und kaufen Sie.
5. **Short-Einstieg**: Bei `%D > 97` und `Williams %R > -0.1` alle bestehenden Long-Engagements schließen und verkaufen.

## Ausgangsregeln
- Stop-Loss- und Take-Profit-Level werden relativ zum Einstiegspreis mithilfe konfigurierbarer Punktabstände angewendet.
- Ein Trailing-Stop kann den Schutzstopp verschärfen, sobald sich der Preis um die angegebene Stufe günstig bewegt.
- Eine Break-Even-Option verschiebt den Stop-Loss auf das Einstiegsniveau, nachdem der Handel die erforderliche Gewinndistanz erreicht hat.
- Entgegengesetzte Signale schließen sofort die aktuelle Position, bevor sie eine neue eröffnen.

## Parameter
- `Trade Volume` – Vertragsvolumen, das mit jeder Marktorder übermittelt wird.
- `Stochastic Period`, `Stochastic %K`, `Stochastic %D` – Parameter des Stochastic-Oszillators.
- `Williams %R Period` – Lookback-Zeitraum für die Williams %R-Berechnung.
- `Stop Loss`, `Take Profit` – Schutzabstände in Preispunkten.
- `Trailing Stop`, `Trailing Step` – dynamische Stoppverwaltung steuern.
- `Break Even` – Distanz in Punkten, die erforderlich ist, um den Einstiegspreis zu sichern.
- `Candle Type` – Zeitrahmen oder benutzerdefinierter Kerzentyp, der in Berechnungen verwendet wird.

## Notizen
- Die Strategie basiert ausschließlich auf fertigen Kerzen, passend zur ursprünglichen MQL4-Implementierung.
- Alle Aufträge werden zum Marktwert mit einem durch `Trade Volume` definierten Volumen erteilt.
- Aktivieren oder deaktivieren Sie die Schutzfunktionen, indem Sie die Abstandsparameter auf Null setzen.
