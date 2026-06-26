# eInTradePanel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die eInTradePanel-Strategie automatisiert den Arbeitsablauf des ursprünglichen MetaTrader-Handelspanels. Sie ermöglicht dieselben acht Ordermodi (Markt, Stop, Limit und Stop-Limit in beide Richtungen), während sie Auslöse-, Einstiegs-, Stop-Loss- und Take-Profit-Abstände automatisch aus dem aktuellen Spread und einer volatilitätssensitiven ATR-Schätzung berechnet. Schutzorders werden durch Kerzenüberwachung simuliert, damit die Strategie mit Datenanbietern verwendet werden kann, die keine angehängten SL/TP-Orders unterstützen.

## Highlights

- **Ordermodi** – zwischen Buy, Sell, Buy/Sell Stop, Buy/Sell Limit oder Buy/Sell Stop-Limit wählen. Stop-Limit-Orders werden aktiviert, sobald der Preis die Auslösedistanz erreicht, und übermitteln dann den Limit-Einstieg.
- **Dynamische Abstände** – ausstehende Niveaus, Auslöser, Stops und Ziele sind proportional zum Größeren aus aktuellem Spread oder einem ATR-abgeleiteten synthetischen Spread (`ATR × AtrFactor`). Wenn ATR nicht bereit ist, wird eine konfigurierbare Basis-Tick-Distanz verwendet.
- **Volatilitätsanpassung** – die ATR-Länge folgt dem Originalpanel (55), sodass Offsets auf Regimewechsel reagieren ohne zusätzliche Abstimmung.
- **Order-Ablauf** – optionales Stornierungsfenster mit Mindestlebensdauererzwingung (Standard 11 Minuten) hält veraltete ausstehende Orders aus dem Buch heraus.
- **Risikomanagement** – jede offene Position wird bei jeder geschlossenen Kerze überwacht; wenn das Hoch/Tief den berechneten Stop oder das Ziel durchbricht, wird die Position zum Marktpreis geschlossen.
- **Kursbewusstsein** – die Strategie abonniert das Orderbuch, um beste Geld-/Briefkurse für genauere Offset-Berechnungen zu erhalten, und greift auf Kerzenschlüsse zurück, wenn die Tiefe nicht verfügbar ist.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Volume` | Ordergröße für alle Einstiege. |
| `Mode` | Einstiegsmodus (Markt, Stop, Limit oder Stop-Limit). |
| `Candle Type` | Aggregation für ATR und kerzenbasierte Ausführungsprüfungen. |
| `Base Ticks` | Minimale Tick-Distanz wenn ATR-Daten nicht verfügbar sind. |
| `Pending Multiplier` | Multiplikator auf die Basis-Tick-Distanz für ausstehende Order-Offsets. |
| `Trigger Multiplier` | Zusätzlicher Multiplikator für Stop-Limit-Auslösedistanzen. |
| `Stop Multiplier` | Multiplikator für Stop-Loss-Distanz (auf 0 setzen zum Deaktivieren). |
| `Take Multiplier` | Multiplikator für Take-Profit-Distanz (auf 0 setzen zum Deaktivieren). |
| `Use ATR` | Aktiviert ATR-basierte Skalierung aller Abstände. |
| `ATR Factor` | Anteil des ATR, der beim Skalieren als synthetischer Spread behandelt wird. |
| `Expiration` | Minuten bis ausstehende Orders storniert werden (0 hält sie GTC). |
| `Min Expiration` | Mindestlebensdauer ausstehend in Minuten, spiegelt die Panel-Absicherung wider. |

## Handelslogik

1. **Datenvorbereitung** – die Strategie abonniert den konfigurierten Kerzentyp und hält einen 55-Perioden-ATR aktualisiert. Orderbuch-Snapshots aktualisieren das zuletzt gesehene Geld/Brief.
2. **Abstandsberechnung** – jede fertige Kerze berechnet die Basis-Tick-Distanz aus ATR und Spread neu, leitet dann ausstehende, Auslöse-, Stop- und Take-Profit-Preise gemäß dem ausgewählten Modus ab.
3. **Orderübermittlung** –
   - Marktmodi führen sofort auf der nächsten fertigen Kerze aus, während die Strategie flat ist.
   - Stop- und Limit-Modi platzieren die entsprechende ausstehende Order und stornieren sie optional nach dem Ablaufzeitfenster.
   - Stop-Limit-Modi warten, bis der Auslösepreis vom Hoch/Tief der Kerze gedruckt wird, dann übermitteln sie den Limit-Einstieg.
4. **Positionsüberwachung** – sobald eine Position offen ist, prüft die Strategie abgeschlossene Kerzen auf Stop- oder Zielverletzungen und schließt die Position zum Marktpreis, wenn ein Level verletzt wird.
5. **Zustandsreset** – wenn die Strategie flat ist und keine Order aktiv ist, werden Niveaus neu berechnet, damit ein neuer Trade auf der nächsten Kerze vorbereitet werden kann.

Der Ansatz spiegelt das manuelle Panel wider und bleibt dabei kompatibel mit der StockSharp High-Level-API und dem asynchronen Orderfluss.
