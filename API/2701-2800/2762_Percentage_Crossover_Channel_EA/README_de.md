# Strategie des prozentualen Crossover-Kanals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie des prozentualen Crossover-Kanals stammt aus dem MetaTrader 5-Expertenberater *Percentage_Crossover_Channel_EA*. Sie basiert auf einem benutzerdefinierten Kanal, der um einen schnellen gleitenden Durchschnitt konstruiert wird, und reagiert entweder auf Bandberührungen oder Kreuzungen der Mittellinie. Diese StockSharp-Implementierung folgt derselben Logik und verwendet die High-Level-API zur Verarbeitung abgeschlossener Kerzen.

## Kanalaufbau
Der zugrunde liegende Indikator baut einen dynamischen Kanal um den ausgewählten Preis (standardmäßig Schluss) auf:

1. Den Basispreis mit dem konfigurierten **Applied Price**-Modus berechnen.
2. Einen 1-Perioden-einfachen gleitenden Durchschnitt anwenden, um den kurzfristigen Referenzpreis zu erhalten.
3. Zwei Grenzen mithilfe des **Percent**-Parameters berechnen (z.B. 50 → ±0,5%).
4. Die vorherige Mittellinie innerhalb der neuen Grenzen begrenzen, um den aktuellen Mittelwert zu erhalten.
5. Die oberen und unteren Bänder sind der begrenzte Mittelwert multipliziert mit den ±Prozent-Faktoren.

Diese Rekursion ermöglicht es dem Kanal, während starker Trends zu verzögern, während er bei Preiskonsolidierung ein enges Envelope beibehält.

## Handelslogik
Zwei verschiedene Signalmodi sind verfügbar:

- **Bandberührungs-Modus (Standard):**
  - Long-Einstieg, wenn das Tief der vorherigen Kerze über dem unteren Band lag und die letzte abgeschlossene Kerze es berührt oder durchbricht.
  - Short-Einstieg, wenn das Hoch der vorherigen Kerze unter dem oberen Band lag und die letzte abgeschlossene Kerze es berührt oder durchbricht.
- **Mittellinie-Kreuzungs-Modus (TradeOnMiddleCross = true):**
  - Long-Einstieg, wenn der Preis die Mittellinie von oben nach unten kreuzt.
  - Short-Einstieg, wenn der Preis die Mittellinie von unten nach oben kreuzt.

Der **ReverseSignals**-Flag tauscht Long- und Short-Regeln aus. Die Strategie schließt und kehrt bestehende Positionen immer um, indem ein einzelner Marktauftrag mit einem Volumen gesendet wird, das dem konfigurierten **OrderVolume** plus dem absoluten Wert der aktuellen Position entspricht.

## Risikomanagement
Optionale Schutzlevel emulieren die ursprünglichen MT5-Stop-Loss- und Take-Profit-Einstellungen:

- **StopLossPoints** – Abstand in Preisschritten, der vom geschätzten Einstiegspreis subtrahiert (Long) oder addiert (Short) wird.
- **TakeProfitPoints** – Abstand in Preisschritten, der zum Einstiegspreis addiert (Long) oder subtrahiert (Short) wird.

Wenn ein Parameter null ist, wird der entsprechende Schutz deaktiviert. Stops werden auf jeder abgeschlossenen Kerze ausgewertet, indem Kerzen-Hochs und -Tiefs mit den gespeicherten Levels verglichen werden. Keine Trailing-Logik wird angewendet.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzen-Datentyp zum Abonnieren (15-Minuten-Zeitrahmen standardmäßig). |
| `Percent` | Kanalbreite in Prozent des Preises (in ±Prozent/100-Faktoren umgerechnet). |
| `PriceMode` | Angewandter Preis für den Kanal. Optionen: Close, Open, High, Low, Median (H+L)/2, Typical (H+L+C)/3, Weighted (H+L+2C)/4, Average (O+H+L+C)/4. |
| `TradeOnMiddleCross` | Umschalten zwischen Bandberührungs-Logik und Mittellinie-Kreuzungs-Logik. |
| `ReverseSignals` | Long- und Short-Bedingungen invertieren. |
| `StopLossPoints` | Schutz-Stop-Abstand in Sicherheits-Preisschritten ausgedrückt. |
| `TakeProfitPoints` | Gewinnziel-Abstand in Sicherheits-Preisschritten ausgedrückt. |
| `OrderVolume` | Basisvolumen für Markteinträge. Die Strategie addiert die absolute offene Position zum Umkehren in einer Transaktion. |

## Implementierungshinweise
- Aufträge werden nur nach Beendigung von Kerzen ausgegeben, was dem MT5-Experten entspricht, der zu Beginn der nächsten Bar mit Daten der vorherigen Bar handelte.
- Der Kanalindikator wird innerhalb der Strategie ohne Speicherung historischer Sammlungen neu erstellt und basiert auf skalaren Zustandsvariablen.
- Schutz-Stops und -Ziele werden manuell überprüft, um die plattformspezifische Auftragsbehandlung aus MT5 zu replizieren.
- Sicherstellen, dass die ausgewählte Sicherheit einen gültigen `PriceStep` aufweist; andernfalls werden Stop-Loss- und Take-Profit-Abstände ignoriert.
