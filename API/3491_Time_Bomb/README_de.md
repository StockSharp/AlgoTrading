# Zeitbombenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Time Bomb repliziert den Expertenberater MetaTrader, der eine einzelne Order auslöst, wenn der Preis innerhalb eines Zeitraums in eine Richtung explodiert
kurzes, konfigurierbares Fenster. Die Strategie überwacht in Echtzeit die besten Geld-/Briefkurse und misst die Anzahl der zwischen ihnen abgedeckten Pips
der letzte Referenzpreis und das neueste Angebot. Wenn die erforderliche Distanz schnell genug zurückgelegt wird, wird eine Marktorder eröffnet
die Richtung des Ausbruchs und aktiviert sofort versteckte Stop-Loss- und Take-Profit-Level, ausgedrückt in Pips.

Die Implementierung wirkt nur, wenn derzeit keine Position offen ist, und spiegelt die ursprüngliche Blocklogik wider, die Überlappungen verhindert hat
Geschäfte. Preisreferenzen werden entweder zurückgesetzt, sobald ein Signal ausgelöst wird oder wenn das Beobachtungsfenster abläuft, also bei jedem Ausbruch von
Volatilität führt höchstens zu einem einzigen Trade pro Seite. Stop-Loss- und Take-Profit-Level werden intern verwaltet und durchgesetzt
die Strategie selbst, da StockSharp nicht automatisch Schutzaufträge für Marktausführungen platziert.

## Einzelheiten

- **Eintrittskriterien**:
  - **Long**: Der beste Brief steigt um mindestens `BuyPipsInTime` Pips im Vergleich zum gespeicherten Referenzpreis und die Bewegung wird beendet
innerhalb von `BuyTimeToWait` Sekunden. Sobald die Bedingung erfüllt ist, wird ein Kaufauftrag mit der Größe `BuyVolume` gesendet.
  - **Short**: Das beste Gebot fällt im Vergleich zum gespeicherten Referenzpreis um mindestens `SellPipsInTime` Pips und die Bewegung wird beendet
innerhalb von `SellTimeToWait` Sekunden. Sobald die Bedingung erfüllt ist, wird ein Verkaufsauftrag mit der Größe `SellVolume` gesendet.
- **Long/Short**: Beide Richtungen werden unterstützt, es kann jedoch jeweils nur eine Position vorhanden sein.
- **Ausstiegskriterien**:
  - **Long**: Die Position wird geschlossen, wenn das beste Gebot den berechneten Stop-Loss- oder Take-Profit-Preis berührt.
  - **Short**: Die Position wird geschlossen, wenn der beste Brief den berechneten Stop-Loss erreicht oder der beste Kauf das Take-Profit-Niveau erreicht.
- **Stopps**: Versteckte Schutzstopps werden von der Strategie behandelt. Entfernungen werden in Pips definiert und mit in Preise umgerechnet
die aktuelle Schrittgröße des Symbols.
- **Standardwerte**:
  - `SellPipsInTime` = 5 Pips, `SellTimeToWait` = 10 Sekunden, `SellVolume` = 0,01 Lots.
  - `SellStopLossPips` = 20 Pips, `SellTakeProfitPips` = 20 Pips.
  - `BuyPipsInTime` = 5 Pips, `BuyTimeToWait` = 10 Sekunden, `BuyVolume` = 0,01 Lots.
  - `BuyStopLossPips` = 20 Pips, `BuyTakeProfitPips` = 20 Pips.
- **Filter**:
  - Kategorie: Ausbruch / Momentum.
  - Richtung: Symmetrisch (lang und kurz).
  - Indikatoren: Nur reine Preisbewegung, keine Oszillatoren.
  - Stopper: Ja (feste Zackenabstände pro Seite).
  - Komplexität: Niedrig – einzelner Breakout-Detektor mit einfacher Zustandsverfolgung.
  - Zeitrahmen: Intraday, reagiert einmal pro Sekunde auf Impulse auf Tick-Ebene.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikostufe: Hängt von den konfigurierten Pip-Abständen ab; Ausfälle entsprechen einem mittleren Risiko bei den wichtigsten Devisenpaaren.

## Eingaben

| Name | Beschreibung |
| --- | --- |
| `SellPipsInTime` | Mindestabstand nach unten in Pips, der zurückgelegt werden muss, bevor eine Short-Position eröffnet wird. |
| `SellTimeToWait` | Es dauerte Sekunden, bis die Abwärtsbewegung abgeschlossen war. |
| `SellVolume` | Handelsvolumen für Verkaufssignale. |
| `SellStopLossPips` | Stop-Loss-Distanz für Short-Positionen, ausgedrückt in Pips. |
| `SellTakeProfitPips` | Take-Profit-Distanz für Short-Positionen, ausgedrückt in Pips. |
| `BuyPipsInTime` | Mindestabstand nach oben in Pips, der abgedeckt werden muss, bevor eine Long-Position eröffnet wird. |
| `BuyTimeToWait` | Es dauerte Sekunden, bis die Aufwärtsbewegung abgeschlossen war. |
| `BuyVolume` | Handelsvolumen für Kaufsignale. |
| `BuyStopLossPips` | Stop-Loss-Distanz für Long-Positionen, ausgedrückt in Pips. |
| `BuyTakeProfitPips` | Take-Profit-Distanz für Long-Positionen, ausgedrückt in Pips. |

## Notizen

- Die Strategie basiert auf den besten Bid/Ask-Aktualisierungen; Stellen Sie sicher, dass der Datenfeed genaue Level-1-Angebote liefert.
- Wenn Sie einen Pip-Abstand oder ein Zeitfenster auf Null setzen, wird das entsprechende Signal deaktiviert, da stattdessen der Referenzpreis zurückgesetzt wird
Generierung von Trades.
- Da die Schutzniveaus intern verwaltet werden, können unerwartete Unterbrechungen Positionen ohne harte Stopps verlassen. Überlegen Sie
Kombination der Strategie mit externen Risikokontrollen im Live-Betrieb.
