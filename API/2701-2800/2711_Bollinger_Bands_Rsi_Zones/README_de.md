# Bollinger Bands RSI Zonen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Multi-Band-Bollinger-Breakout-System, konvertiert aus dem MetaTrader-Experten «Bollinger Bands RSI». Die Strategie leitet drei Bollinger-Hüllen mit identischen Perioden, aber unterschiedlichen Abweichungen ab, um «gelbe», «blaue» und «rote» Bänder zu erstellen. Orders werden ausgelöst, wenn der Preis konfigurierbare Zonen rund um diese Bänder erneut testet, optional bestätigt durch RSI- und Stochastik-Filter.

## Strategielogik
- Das primäre (gelbe) Band verwendet den konfigurierten Abweichungsmultiplikator.
- Das blaue Band halbiert die Abweichung und erstellt eine engere Hülle.
- Das rote Band verdoppelt die Abweichung und produziert eine breite äußere Hülle.
- RSI- und Stochastik-Werte werden auf der vorherigen fertigen Kerze (`Bar Shift`) ausgewertet, um das ursprüngliche EA-Verhalten zu replizieren.
- `Only One Position` kontrolliert, ob neue Orders nur erlaubt sind, wenn die Nettoposition flat ist, oder ob zusätzliche Skalierungsgeschäfte erlaubt sind, sobald der Preis zur Bollinger-Mittellinie zurückkehrt.

## Einstiegskriterien
### Long-Einstiege
1. Der Preis auf der aktuellen Kerze fällt auf oder unter die ausgewählte Long-Einstiegszone (`Entry Mode`):
   - Mittelpunkt zwischen gelb und blau, blau und rot, oder eines der einzelnen Bänder.
2. Optionale Bestätigungen:
   - RSI-Filter: RSI ≤ `100 - RSI Lower`.
   - Stochastik-Filter: %K < `100 - Stochastic Lower`.
3. Positionsvoraussetzungen:
   - Wenn `Only One Position` aktiviert ist, muss die Nettoposition flat sein.
   - Andernfalls werden zusätzliche Long-Orders blockiert, bis die Kerze über dem mittleren (gelben) Band schließt, was die EA-Sperrlogik emuliert.

### Short-Einstiege
1. Der Preis auf der aktuellen Kerze steigt auf oder über die ausgewählte Short-Einstiegszone (spiegelt die Long-Optionen).
2. Optionale Bestätigungen:
   - RSI-Filter: RSI ≥ `RSI Lower`.
   - Stochastik-Filter: %K > `Stochastic Lower`.
3. Positionsvoraussetzungen spiegeln die Long-Logik (flat Position für Einzelhandels-Modus oder entsperrter Zustand, sobald die Kerze unter dem mittleren Band schließt).

## Ausstiegskriterien
- Der Schließmodus wird durch `Closure Mode` bestimmt:
  - `Middle Line`: Longs aussteigen, wenn der Preis das Bollinger-Mittelband erreicht; Shorts aussteigen, wenn der Preis es von oben berührt.
  - `Between Yellow and Blue` / `Between Blue and Red`: Aussteigen an denselben Mittelpunkten, die für Einstiege verwendet werden; standardmäßig an Mittelpunkten zwischen blau und rot, wenn der Eintrittsmodus abweicht.
  - `Yellow Line`, `Blue Line`, `Red Line`: Aussteigen bei direkten Berührungen der entsprechenden oberen/unteren Bänder.
- Sperr-Flags für den Skalierungsmodus werden automatisch zurückgesetzt, wenn die Kerze auf der anderen Seite des mittleren Bandes schließt, und recreaten das EA-Verhalten.

## Risikomanagement
- `Stop Loss`- und `Take Profit`-Parameter werden in Pips ausgedrückt und über `Pip Value` in absolute Preisabstände umgewandelt, wenn `StartProtection` initialisiert wird.
- Stops und Ziele sind optional; lassen Sie den Abstand bei null, um den entsprechenden Schutz zu deaktivieren.
- Das Handelsvolumen wird durch `Order Volume` definiert und auf jede Marktorder angewendet.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Entry Mode` | Wählt die Bollinger-Zone, die Einstiege auslöst. | Zwischen gelb und blau |
| `Closure Mode` | Definiert das gewinnerzielende Band oder den Mittelpunkt. | Zwischen blau und rot |
| `Bands Period` | Periodenlänge, die alle Bollinger Bands teilen. | 140 |
| `Deviation` | Standardabweichungsmultiplikator für das gelbe Band (blau ist halb, rot ist doppelt). | 2.0 |
| `Use RSI Filter` | Aktiviert die RSI-Bestätigungslogik. | false |
| `RSI Period` | RSI-Mittelungsperiode. | 8 |
| `RSI Lower` | Überkauft-Schwelle; überverkauft verwendet `100 - Wert`. | 70 |
| `Use Stochastic Filter` | Aktiviert die %K-Bestätigungslogik. | true |
| `Stochastic Period` | Haupt-%K-Rückblickperiode (Glättung fest auf 3/3 SMA). | 20 |
| `Stochastic Lower` | Überkauft-Schwelle; überverkauft verwendet `100 - Wert`. | 95 |
| `Bar Shift` | Anzahl der fertigen Bars für Indikatorwerte. | 1 |
| `Only One Position` | Wenn aktiviert, werden neue Trades nur geöffnet, wenn keine Position aktiv ist. | true |
| `Order Volume` | Volumen, das mit jeder Marktorder gesendet wird. | 1 |
| `Pip Value` | Absoluter Preiswert eines Pips für die Stop/Ziel-Konvertierung. | 0.0001 |
| `Stop Loss` | Schützender Stop-Abstand in Pips (0 deaktiviert). | 200 |
| `Take Profit` | Schützender Ziel-Abstand in Pips (0 deaktiviert). | 200 |
| `Candle Type` | Datentyp für Berechnungen (Standard-1-Minuten-Kerzen). | 1m Zeitrahmen |

## Hinweise
- Die Strategie verarbeitet nur abgeschlossene Kerzen, daher sollte `Bar Shift` ≥ 1 bleiben, um Referenzen auf unfertige Bars zu vermeiden.
- RSI- und Stochastik-Filter verwenden die %K-Linie; die %D-Linie wird berechnet, aber nicht verwendet, was die ursprüngliche EA-Implementierung widerspiegelt.
- Die Konvertierung hält Kommentare und Signalnamen auf Englisch und folgt den StockSharp-High-Level-API-Richtlinien (Bind-basierte Indikator-Pipeline, kein manueller Pufferzugriff).
