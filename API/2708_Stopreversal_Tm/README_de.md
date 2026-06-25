# Stopreversal Tm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Stopreversal Tm-Strategie ist eine direkte Übersetzung des ursprünglichen MetaTrader 5 Expert Advisors `Exp_Stopreversal_Tm.mq5`. Die Handelsidee folgt dem benutzerdefinierten Indikator Stopreversal, der einen dynamischen Trailing Stop um den Preis herum aufrechthält und Umkehralarme erzeugt, wenn der Preis diese Trailing-Grenze kreuzt. Die Strategie operiert auf einem einzelnen Instrument und einem einzelnen Kerzen-Feed und ist für den Trendumkehr-Handel mit einem benutzerdefinierten Sitzungsfilter ausgelegt.

## Signalgenerierung
Der Stopreversal-Indikator berechnet einen Referenzpreis aus dem gewählten angewendeten Preismodus und passt dann ein Trailing-Stop-Level um `Sensitivity` (den `nPips`-Parameter) an. Wenn der neue angewendete Preis über den Trailing Stop steigt, während sich der vorherige Balken darunter befand, wird ein bullisches Signal erzeugt. Umgekehrt erscheint ein bärisches Signal, wenn der neue Preis unter den Trailing Stop fällt, nachdem er darüber war. Jedes bullische Signal fordert gleichzeitig das Schließen bestehender Short-Positionen und das Öffnen eines neuen Longs, während jedes bärische Signal Longs schließt und Shorts öffnet.

Um das Verhalten der ursprünglichen MetaTrader-Implementierung zu reproduzieren, kann die Strategie die Ausführung von Signalen um mehrere abgeschlossene Balken verzögern (`Signal Bar Delay`). Dies repliziert den `SignalBar`-Eingang des Expert Advisors und verhindert den Handel auf der noch entstehenden Kerze.

## Sitzungsfilter und Positionshandling
Der Expert Advisor erlaubte den Handel nur innerhalb eines bestimmten Zeitfensters. Die konvertierte Strategie behält die gleiche Logik: Wenn das `Use Time Filter`-Flag aktiviert ist, sind Aufträge nur innerhalb der durch `Start Hour/Minute` und `End Hour/Minute` konfigurierten Sitzung erlaubt. Wenn die aktuelle Zeit das erlaubte Fenster verlässt, wird jede offene Position sofort geschlossen. Signalgesteuerte Ausstiege bleiben aktiv, auch wenn die Sitzung deaktiviert ist.

Die Strategie arbeitet mit Netto-Positionen. Eine Schließaktion wird immer vor einem entgegengesetzten Einstieg ausgeführt, wodurch sichergestellt wird, dass die Richtung ohne überlappende Expositionen wechselt.

## Parameter
- **Allow Buy Entries / Allow Sell Entries** – Öffnung neuer Long- oder Short-Positionen aktivieren oder deaktivieren, wenn das entsprechende Signal empfangen wird.
- **Allow Long Exits / Allow Short Exits** – steuert, ob entgegengesetzte Signale bestehende Positionen schließen dürfen.
- **Use Time Filter** – schaltet das Handelssitzungs-Fenster ein.
- **Start Hour / Start Minute / End Hour / End Minute** – definiert den inklusiven Start und das exklusive Ende des Handelsfensters. Der Zeitfilter unterstützt Übernacht-Sitzungen, bei denen die Endzeit früher als die Startzeit liegt.
- **Sensitivity (`nPips`)** – relativer Abstand (ausgedrückt als Multiplikator, z.B. `0.004 = 0.4%`), der verwendet wird, um den Trailing Stop näher oder weiter vom Preis zu bewegen.
- **Signal Bar Delay (`SignalBar`)** – Anzahl abgeschlossener Kerzen, die gewartet werden soll, bevor auf ein Signal reagiert wird. `0` führt sofort bei der Schlusskerze aus, `1` reproduziert das Standardverhalten, auf dem vorherigen Balken zu handeln.
- **Candle Type** – Zeitrahmen der Kerzen-Subskription für Indikatorberechnungen.
- **Applied Price** – Wahl der Preisserie (Schluss, Eröffnung, Medianpreis, Trendfolgemodi, Demark-Preis usw.), die die Trailing-Stop-Berechnung speist.

## Implementierungshinweise
- Der Indikator ist direkt in der Strategie implementiert, ohne sich auf externe Buffer zu verlassen, sodass die `nPips`-Trailing-Stop-Logik dem ursprünglichen MQL5-Code entspricht.
- Sitzungsverwaltung und Signalsequenzierung folgen dem ursprünglichen Experten, einschließlich der Priorität, bestehende Exposition zu schließen, bevor neue Trades eröffnet werden.
- Die Konvertierung konzentriert sich auf die High-Level-StockSharp-API: Kerzen-Subskriptionen, verzögerte Signal-Warteschlange und Marktaufträge (`BuyMarket` / `SellMarket`). Money-Management-Funktionen, die an MetaTrader-Kontometriken gebunden waren, wurden weggelassen, da StockSharp-Strategien bereits mit expliziten Positionsgrößen arbeiten.
