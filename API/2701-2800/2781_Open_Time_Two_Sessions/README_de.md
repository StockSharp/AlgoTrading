# Eröffnungszeit Zwei Sitzungen Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Eröffnungszeit Zwei Sitzungen Strategie** automatisiert einen zeitgeplanten Handelsplan, der zwei unabhängige Sitzungen während des Handelstages verwalten kann. Jede Sitzung kann mit ihrer eigenen Richtung, Risikoparametern und optionalem erzwungenem Schließfenster konfiguriert werden. Die Konvertierung folgt der ursprünglichen MetaTrader-Logik, stützt sich jedoch auf die High-Level-APIs von StockSharp, Kerzen und Parameterobjekte für Konfiguration und Optimierung.

## Handelslogik
1. **Sitzungsschließfenster.** Für jedes Intervall kann ein optionales Schließfenster definiert werden. Wenn die Kerzenzeit innerhalb des Fensters liegt (Startzeit plus globale Dauer), schließt die Strategie das entsprechende Intervall zwangsweise und löscht seinen Zustand.
2. **Trailing-Stop-Pflege.** Wenn Trailing-Stop und -Schritt positiv sind, überwacht die Trailing-Logik abgeschlossene Kerzen. Sobald sich der Preis mindestens `(TrailingStop + TrailingStep)` zugunsten der Position bewegt hat, wird der Stop um `TrailingStop` vorgerückt. Aktualisierungen erfordern die Schrittdistanz, um verrauschte Neuberechnungen zu vermeiden.
3. **Stop-Loss- und Take-Profit-Prüfungen.** Jedes Intervall hat unabhängige Stop-Loss- und Take-Profit-Abstände in Pips. Bei jeder abgeschlossenen Kerze werden die Hoch-/Tiefpreise mit diesen Niveaus verglichen und das Intervall sofort geschlossen, wenn ein Niveau durchbrochen wird.
4. **Wochentag-Filter.** Der Handel findet nur an den aktivierten Wochentagen statt. Wenn die aktuelle Kerze zu einem deaktivierten Tag gehört, werden keine neuen Trades eröffnet.
5. **Eröffnungsfenster.** Jedes Intervall hat ein Eröffnungsfenster mit Start- und Endzeiten. Der globale Dauerwert erweitert das Fenster auf der Endseite. Wenn ein Fenster aktiv ist und das Intervall keine offene Position hat, eröffnet die Strategie eine Marktorder in der konfigurierten Richtung.
6. **Positionssynchronisierung.** Aktive Intervalle tragen zu einer Ziel-Nettoposition bei. Die Strategie ruft `BuyMarket` oder `SellMarket` auf, damit die Nettoposition mit der Summe der Intervallexpositionen übereinstimmt. Jedes Intervall führt seinen eigenen Einstiegspreis, Stop/Take-Niveaus und Trailing-Stop-Zustand.

## Parameterreferenz
- **Close Window #1 / Close Window #2** – Aktivieren oder Deaktivieren der dedizierten erzwungenen Schließfenster für jedes Intervall.
- **Close Start #1 / Close Start #2** – Lokale Tageszeit, zu der das Schließfenster für jedes Intervall beginnt.
- **Trailing Stop / Trailing Step** – Abstände in Pips, die von der Trailing-Logik verwendet werden. Beide müssen größer als null sein, um Trailing zu aktivieren.
- **Trade Monday … Trade Friday** – Wochentag-Filter. Mindestens ein Tag muss aktiviert bleiben, um den Handel zu ermöglichen.
- **Open Start #1 / Open End #1 / Open Start #2 / Open End #2** – Eröffnungsfenstergrenzen für jedes Intervall. Die globale Dauer erweitert das Fenster über die Endzeit hinaus.
- **Window Duration** – Zusätzliche Zeitspanne, die zu beiden Eröffnungs- und Schließfenstern hinzugefügt wird.
- **Direction #1 / Direction #2** – Handelsrichtungsflags (`true` für Long, `false` für Short) für jedes Intervall.
- **Trade Volume** – Marktordervolumen für jedes Intervall. Die Strategie nimmt identisches Volumen für beide Intervalle an, wie im ursprünglichen Expert Advisor.
- **Stop Loss #1 / Take Profit #1 / Stop Loss #2 / Take Profit #2** – Abstände in Pips für Stop-Loss- und Take-Profit-Niveaus pro Intervall. Ein Wert von null deaktiviert das entsprechende Niveau.
- **Candle Type** – Kerzenserie, die zum Antreiben der Strategie verwendet wird. Alle Berechnungen, einschließlich Zeitfenster und Risikoprüfungen, werden ausgeführt, wenn diese Kerzen enden.

## Risikomanagement-Details
- Pip-Abstände werden mithilfe des Sicherheitspreisschritts in Preiseinheiten umgerechnet. Wenn das Instrument drei oder fünf Dezimalstellen verwendet, wird der Schritt mit zehn multipliziert, um die MetaTrader-Pip-Definition zu replizieren.
- Die Trailing-Logik wird von beiden Intervallen geteilt, während Stop-Loss- und Take-Profit-Werte unabhängig bleiben.
- Wenn der Stop- oder Trailing-Level auslöst, setzt das Intervall seinen Zustand zurück, damit es innerhalb desselben Fensters wieder öffnen kann, falls die Zeit es erlaubt.

## Einschränkungen und Hinweise
- StockSharp arbeitet mit einem Netting-Positionsmodell. Wenn Intervall #1 und #2 mit entgegengesetzten Richtungen konfiguriert sind, wird die resultierende Nettoposition geglättet, anstatt zwei abgesicherte Trades gleichzeitig offen zu halten. Verwenden Sie ein Hedging-fähiges Portfolio, wenn echtes Hedging erforderlich ist.
- Entscheidungen basieren auf der ausgewählten Kerzenserie. Durch Verwendung eines großen Zeitrahmens können Reaktionen im Vergleich zur Tick-basierten MetaTrader-Implementierung verzögert werden.
- Die Strategie erwartet, dass Exchange- und Terminal-Uhren synchronisiert sind, da Tageszeit-Vergleiche auf Ortszeit basieren.

## Verwendungstipps
- Konfigurieren Sie den Kerzentyp so, dass er der für den Zeitplan verwendeten Zeitgranularität entspricht (z. B. eine Minute für granulare Steuerung).
- Kombinieren Sie den Tagesfilter und Schließfenster, um das Tragen von Positionen über unerwünschte Sitzungen zu vermeiden.
- Optimieren Sie die Parameter durch die eingebauten `StrategyParam`-Objekte – wichtige Felder haben `SetCanOptimize` bereits aktiviert.
