# Strategie für Rastervorlagen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters **Grid_Template**. Es wird ein symmetrisches Gitter aus ausstehenden Sto erstellt
p-Orders rund um den aktuellen Geld-/Briefkurs, sodass der Händler benutzerdefinierte Einstiegsfilter integrieren oder ihn als reine Breakout-Vorlage ausführen kann. Einmal
Sobald alle Grid-Aufträge ausgeführt wurden oder abgelaufen sind, bereitet die Engine sofort das nächste Grid vor. Die Implementierung bewahrt t
Die optionale Money-Management-Formel und die Möglichkeit, veraltete ausstehende Aufträge nach einer konfigurierbaren Anzahl von automatisch zu entfernen
Stunden.

## Handelslogik
- Abonnieren Sie Kurse der Stufe 1, um kontinuierlich die besten Geld-/Briefkurse zu verfolgen. Es sind keine Kerzen oder Indikatoren erforderlich.
- Wenn das Konto keine offene Position und keine aktiven Strategieaufträge aufweist, platzieren Sie `GridOrders` Kauf-Stop-Aufträge über dem Briefkurs und „G
Die Verkaufsstopp-Orders von RidOrders liegen unter dem Gebot.
- Die erste Gitterebene ist um `PriceDistancePips` vom aktuellen Marktpreis versetzt; Jede weitere Ebene fügt `GridStepPips` m hinzu
Erzentfernung.
- Jeder Eintrag verwendet das gleiche feste Volumen (oder die vom Geld verwaltete Größe) und die gleichen Stop-Loss- und Take-Profit-Abstände, ausgedrückt in S
ips.
- Sobald eine ausstehende Order ausgeführt wird, registriert die Strategie die entsprechenden Schutzorder (Stop-Loss und Take-Profit) als
unabhängige Stop-/Limit-Orders. Diese erben denselben Kommentar, um sie leichter identifizieren zu können.
- Wenn vor Ablauf des Ablaufzeit-Timers kein Auftrag ausgelöst wird, storniert die Vorlage alle noch ausstehenden Aufträge und aktiviert den g erneut
los.

## Geldmanagement
- Wenn `UseMoneyManagement` deaktiviert ist, verwenden alle Bestellungen den festen Parameter `StaticVolume`.
- Wenn diese Option aktiviert ist, wird die Losgröße aus der ursprünglichen Vorlagenformel abgeleitet: `freeMargin * RiskPercent / 100000`, gerundet auf n
Empfänger `VolumeStep` und zwischen `VolumeMin` und `VolumeMax` eingeklemmt. Der aktuelle Wert des Portfolios wird als Ersatz für MT4 verwendet
freie Marge.
- Das berechnete Volumen wird durch die Börsenvertragseinstellungen normalisiert; wenn es unter die festgelegte minimale handelbare Größe fällt
Null, wodurch die Auftragsübermittlung verhindert wird.

## Auftrags- und Risikomanagement
- Kaufstopp-Orders werden bei `ask + PriceDistancePips + GridStepPips * level` platziert. Sell-Stop-Orders spiegeln die Logik des Bid-Si wider
de.
- Schutzstopps (`SellStop`/`BuyStop`) und Ziele (`SellLimit`/`BuyLimit`) werden erst registriert, nachdem ein ausstehender Eintrag gefüllt ist
. Dies ahmt das MT4-Verhalten nach, bei dem Stop-Loss und Take-Profit zum selben Ticket gehören.
- `PendingExpirationHours` definiert, wie lange ausstehende Eintrittsaufträge aktiv bleiben. Ein Nullwert hält sie, bis sie gefüllt sind oder ma sind
endgültig abgesagt.
- Wenn die Nettoposition auf Null zurückkehrt, storniert die Strategie auch alle noch aktiven Schutzaufträge, um eine saubere Weste zu gewährleisten.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OrderComment` | Text, der jeder vom Raster generierten Bestellung zugewiesen ist und mit dem ursprünglichen EA-Kommentar übereinstimmt. |
| `StaticVolume` | Feste Losgröße, die verwendet wird, wenn die Geldverwaltung deaktiviert ist. |
| `UseMoneyManagement` | Aktiviert die ausgleichsbasierte Größenbestimmungsroutine. |
| `RiskPercent` | Von der Money-Management-Formel verwendeter Prozentsatz; Wird ignoriert, wenn `UseMoneyManagement` falsch ist. |
| `TakeProfitPips` | Auf jeden Rastereintrag wird eine Take-Profit-Distanz angewendet. |
| `StopLossPips` | Stop-Loss-Distanz gilt für jeden Rastereintrag. |
| `PriceDistancePips` | Anfängliche Lücke (in Pips) zwischen dem Marktpreis und der ersten Grid-Order. |
| `GridStepPips` | Zusätzlicher Abstand (in Pips), der zwischen aufeinanderfolgenden Rasterebenen hinzugefügt wird. |
| `GridOrders` | Anzahl der auf jeder Seite des Preises erstellten ausstehenden Aufträge. |
| `PendingExpirationHours` | Lebensdauer des ausstehenden Rasters vor der Stornierung. |

## Notizen
- Die Vorlage schreibt keine indikatorbasierten Filter vor; Händler können die Klasse erweitern und `TryPlaceGrid` überschreiben, um custo hinzuzufügen
m Bedingungen.
- Da Schutzstopps und -ziele als separate Aufträge implementiert werden, kann die Ausführung auf Brokerseite geringfügig vom MT4-Tik abweichen
Stop-Loss/Take-Profit-Management im T-Stil, insbesondere bei Teilfüllungen.
- Stellen Sie immer sicher, dass die von der Börse abgeleitete Pip-Größe (`PriceStep` und `Decimals`) mit dem gehandelten Instrument übereinstimmt
Bevor Sie die Strategie auf einem Live-Konto ausführen.
