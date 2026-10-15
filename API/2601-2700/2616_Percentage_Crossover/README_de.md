# Prozentuale Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert das Verhalten des ursprünglichen MetaTrader-Experten `Exp_PercentageCrossover`. Sie handelt in Richtung des Percentage Crossover-Indikators, der eine nachziehende Preislinie zeichnet, die sich nur innerhalb eines festen prozentualen Bandes um den aktuellen Schlusskurs bewegen kann. Die Neigung dieser Linie definiert den Marktzustand und löst Trades aus.

## Konzept

1. Bei jeder abgeschlossenen Kerze behält der Indikator den vorherigen Linienwert bei.
2. Eine bullische Aktualisierung erfolgt, wenn der Schlusskurs die nachziehende Linie um mindestens `percent` Prozent des Preises über ihren vorherigen Wert schiebt.
3. Eine bärische Aktualisierung erfolgt, wenn der Schlusskurs die nachziehende Linie um denselben Prozentsatz unter ihren vorherigen Wert zieht.
4. Bleibt der Schlusskurs innerhalb des Bandes, bleibt die Linie flach und behält ihre letzte Farbe.

Die Farbe der Linie wird genauso interpretiert wie in MetaTrader:

- **Farbindex 0 (blau/violett)** – die Linie steigt (bullischer Kontext).
- **Farbindex 1 (orange)** – die Linie fällt (bärischer Kontext).

## Handelsregeln

### Long-Einstiege
- Nur aktiviert, wenn `BuyPosOpen = true`.
- Die durch `SignalBar` ausgewählte Kerze wird ausgewertet (1 bedeutet die letzte geschlossene Kerze).
- Eine Long-Position wird eröffnet, wenn diese Kerze von Farbe 1 auf Farbe 0 wechselt.

### Short-Einstiege
- Nur aktiviert, wenn `SellPosOpen = true`.
- Dieselbe `SignalBar`-Kerze wird ausgewertet.
- Eine Short-Position wird eröffnet, wenn die Kerze von Farbe 0 auf Farbe 1 wechselt.

### Positionsverwaltung
- Wenn `BuyPosClose = true`, wird jede offene Long-Position geschlossen, sobald die aktuelle Kerze (nach Anwendung des `SignalBar`-Versatzes) Farbe 1 hat.
- Wenn `SellPosClose = true`, wird jede offene Short-Position geschlossen, sobald diese Kerze Farbe 0 hat.
- Wenn `UseTimeFilter = true` und die aktuelle Zeit außerhalb des konfigurierten Handelsfensters liegt, verlässt die Strategie sofort die aktive Position und ignoriert neue Signale, bis der Markt das Fenster wieder betritt.
- Aufträge werden mit `BuyMarket()` und `SellMarket()` gesendet. Die tatsächliche Menge stammt aus der `Volume`-Eigenschaft der Strategie.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `Percent` | Prozentband für die nachziehende Linie. Höhere Werte lassen die Linie langsamer reagieren. | `1` |
| `SignalBar` | Welche geschlossene Kerze analysiert wird (1 = letzte geschlossene). Muss positiv bleiben. | `1` |
| `BuyPosOpen` / `SellPosOpen` | Long- bzw. Short-Einstiege aktivieren. | `true` |
| `BuyPosClose` / `SellPosClose` | Schließlogik für Long- bzw. Short-Positionen aktivieren. | `true` |
| `UseTimeFilter` | Das Handelsfenster aktivieren. | `true` |
| `StartHour` / `StartMinute` | Stunde und Minute, die das Handelsfenster öffnen, wenn der Filter aktiv ist. | `0` / `0` |
| `EndHour` / `EndMinute` | Stunde und Minute, die das Handelsfenster schließen. | `23` / `59` |
| `CandleType` | Zeitrahmen der Kerzen, die für den Indikator und die Signale verwendet werden. | `4h` |

## Hinweise

- Der Zeitfilter folgt streng dem ursprünglichen Expert Advisor. Wenn die Startstunde größer als die Endstunde ist, erstellt die Logik ein Übernacht-Fenster, erfordert aber dennoch, dass die Minuten größer oder gleich `StartMinute` sind, bevor die Sitzung aktiv wird.
- `SignalBar` wird nur bei abgeschlossenen Kerzen ausgewertet. Setzen Sie es auf `1`, um die Standard-MetaTrader-Konfiguration zu spiegeln.
- Die Strategie setzt keine Stop-Loss- oder Take-Profit-Niveaus. Die Risikosteuerung muss extern oder durch Anpassung des Prozentsatzes und des Handelsfensters erfolgen.
