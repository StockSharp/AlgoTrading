# Wöchentliche Rebound-Korridor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Weekly Rebound Corridor-Strategie repliziert das Verhalten des MetaTrader 4 Expert Advisors `2_Otkat_Sys_v1_1`. Das System sucht nach einer starken Lücke zwischen dem Schlusskurs der vorherigen Sitzung und dem Eröffnungspreis, der 24 Kerzen zuvor aufgetreten ist. Wenn die erkannte Lücke einen konfigurierbaren Korridorschwellenwert überschreitet und es sich um den angegebenen Handelstag der Woche handelt, kommt die Strategie in den ersten Minuten des neuen Handelstages auf den Markt. Es werden schützende Stop-Loss- und Take-Profit-Level angewendet und alle offenen Positionen werden kurz vor Ende der Handelssitzung zwangsweise geschlossen.

## Handelslogik
1. **Datenvorbereitung**
   - Verwendet standardmäßig Minutenkerzen. Der Kerzentyp ist für andere Bargrößen konfigurierbar.
   - Verfolgt den Schlusskurs der vorherigen Kerze und unterhält einen Ringpuffer, der den vor 24 Kerzen beobachteten Eröffnungspreis zurückgibt.
2. **Signalerzeugung**
   - Am angegebenen Handelstag der Woche (MetaTrader-Format: `0 = Sunday`, `6 = Saturday`) wertet die Strategie fertige Kerzen aus, deren Ortszeit zwischen 00:00 und 00:03 liegt.
   - Berechnet die Differenz zwischen der historischen Eröffnungskerze (vor 24 Kerzen) und der letzten geschlossenen Kerze. Wenn die Differenz den konfigurierten Korridorschwellenwert überschreitet, wird eine Marktorder gesendet:
     - **Long-Setup**: Der historische Eröffnungskurs minus der vorherige Schlusskurs liegt über dem Korridorschwellenwert.
     - **Short-Setup**: Der vorherige Schlusskurs minus der historische Eröffnungskurs liegt über dem Korridorschwellenwert.
   - Jeder Handelstag kann höchstens einen Eintrag auslösen.
3. **Handelsmanagement**
   - Stop-Loss- und Take-Profit-Level werden in Punkten ausgedrückt. Die Tick-Größe des Instruments wandelt die Punktwerte in tatsächliche Preisversätze um.
   - Long-Trades addieren den ursprünglichen MT4-Offset von drei Extrapunkten zur Take-Profit-Distanz.
   - Die Strategie überwacht kontinuierlich die Höchst- und Tiefststände der Kerzen, um Stop-Loss- oder Take-Profit-Treffer zu erkennen, und schließt die offene Position bei Auslösung mit einer Marktorder.
   - Alle verbleibenden offenen Positionen werden nach 22:45 Uhr Ortszeit geschlossen, um die End-of-Day-Flat-Regel des ursprünglichen Expert Advisors zu emulieren.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. Long-Trades fügen drei zusätzliche Punkte hinzu, wie im MT4-Skript definiert. | `5` |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten. | `49` |
| `TradeVolume` | Mit Marktaufträgen übermitteltes Volumen. Der Wert wird automatisch an den Lautstärkeschritt des Instruments angepasst. | `1` |
| `CorridorPoints` | Erforderliche Mindestlücke zwischen dem historischen Eröffnungskurs und dem letzten Schlusskurs. | `10` |
| `TradeDayOfWeek` | Handelstag in der Nummerierung MetaTrader (`0 = Sunday` … `6 = Saturday`). | `5` (Freitag) |
| `CandleType` | Für die Analyse verwendeter Kerzendatentyp. | `1 minute` |

## Notizen
- Die Strategie basiert ausschließlich auf fertigen Kerzen, um sie an den Projektrichtlinien auszurichten.
- Stellen Sie sicher, dass das ausgewählte Instrument genügend historische Daten bereitstellt, um den 24-Kerzen-Puffer aufzubauen, bevor Einträge erwartet werden.
- Die volumen- und punktbasierten Parameter sollten an die Instrumentenspezifikation (Tickgröße, Lotschritt, Handelsplan) angepasst werden.
