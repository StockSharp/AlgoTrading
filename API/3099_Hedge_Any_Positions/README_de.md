# Strategie zur Absicherung beliebiger Positionen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Strategie zur Absicherung beliebiger Positionen** ist eine direkte Konvertierung des ursprünglichen *Hedge any positions (barabashkakvn's edition)* MQL5-Experts. Die StockSharp-Version hält die Kernidee intakt: Sie überwacht jedes offene Leg, das von der Strategie erstellt wurde, und öffnet sobald ein Leg eine definierte Anzahl von Pips verliert, sofort eine entgegengesetzte Position mit einer verstärkten Losgröße. Die Implementierung basiert auf der High-Level-StockSharp-API, sodass Hedge-Orders über Market-Orders platziert werden und das Positions-Tracking intern ohne benutzerdefinierten Orderrouting-Code behandelt wird.

Die Strategie kann optional einen ersten Trade platzieren, wenn sie startet. Danach reagiert sie einfach auf ungünstige Preisbewegungen und baut eine Treppe von Hedging-Trades auf, wobei jedes Leg als abgesichert markiert wird, damit dieselbe Position nicht mehrere entgegengesetzte Einstiege auslösen kann.

## Hedging-Workflow
1. **Kerzenfeed** – ein konfigurierbarer `CandleType` treibt die Strategie. Es werden nur abgeschlossene Kerzen verarbeitet.
2. **Verlustberechnung** – bei jedem Kerzenschluss prüft die Strategie, ob sich der Schlusskurs gegen ein offenes Leg um mindestens `LosingPips` multipliziert mit der berechneten Pip-Größe bewegt hat.
3. **Hedge-Ausführung** – wenn ein verlierendes Leg gefunden wird, wird eine Market-Order in der entgegengesetzten Richtung gesendet. Das Ordervolumen entspricht dem ursprünglichen Leg-Volumen multipliziert mit `LotCoefficient`, gerundet auf den Instrument-Volumenschritt und auf das erlaubte Mindest-/Maximalvolumen beschränkt.
4. **Zustandsaktualisierung** – sobald eine entgegengesetzte Order versendet wird, wird das ursprüngliche Leg als abgesichert markiert und der neu eröffnete Trade als frisches Leg gespeichert, das selbst später abgesichert werden kann, wenn sich der Preis erneut umkehrt.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Zeitrahmen für die Bewertung von Preisbewegungen und Auslösung von Hedges. | 1-Minuten-Kerzen |
| `LosingPips` | Anzahl der Pips, um die sich der Preis gegen ein Leg bewegen muss, bevor ein Hedge eröffnet wird. | 5 |
| `LotCoefficient` | Multiplikator, der beim Absenden der Hedge-Order auf das ursprüngliche Volumen angewendet wird. | 2.0 |
| `AutoPlaceInitialTrade` | Wenn aktiviert, sendet die Strategie beim Start automatisch den ersten Trade. | Deaktiviert |
| `InitialVolume` | Ordergröße für den optionalen ersten Trade. Gerundet auf den Instrument-Volumenschritt. | 0.10 |
| `InitialDirection` | Seite (Kauf oder Verkauf) für den optionalen ersten Trade. | Kauf |

> **Hinweis:** Die `Strategy.Volume`-Eigenschaft auf die Basisordergröße setzen, die die Strategie verwenden soll. Die obigen Parameter steuern nur Hedging-spezifisches Verhalten.

## Verwendungsrichtlinien
1. Vor dem Starten der Strategie `Security`, `Portfolio` und das gewünschte Basis-`Volume` zuweisen.
2. `LosingPips` und `LotCoefficient` an die Volatilität und Risikotoleranz des ausgewählten Instruments anpassen.
3. `AutoPlaceInitialTrade` aktivieren, wenn die StockSharp-Version die allererste Position automatisch erstellen soll; andernfalls manuell ein erstes Leg öffnen oder eine andere Komponente verwenden.
4. Da die High-Level-StockSharp-API mit Nettopositionen arbeitet, wird die interne Leg-Liste verwendet, um die Hedging-Struktur zu emulieren. Kontoexposure überwachen, wenn auf Netting-Konten läuft.
5. Ausführungsberichte überprüfen: Jeder Hedge wird mit einer Market-Order platziert (`BuyMarket` oder `SellMarket`).

## Unterschiede zum Original Expert
- Margensvalidierung, Slippage-Prüfungen und ausführliche Ergebnisprotokollierung wurden entfernt; StockSharp meldet Ausführungsprobleme bereits über Strategie-Events.
- Die Konvertierung verwendet abgeschlossene Kerzen statt Tick-für-Tick-Daten. Einen ausreichend kleinen Zeitrahmen wählen, wenn schnellere Reaktionszeiten benötigt werden.
- Lot-Rundung basiert jetzt auf `Security.VolumeStep`, `Security.MinVolume` und `Security.MaxVolume`, um die Handelsregeln des Instruments einzuhalten.
- Alerts, Benachrichtigungen und der nur-Tester-Zufalls-Anfangstrade aus der MQL-Version wurden absichtlich weggelassen. Der optionale automatische Einstiegsparameter ersetzt dieses Verhalten.

## Empfohlene Erweiterungen
- Das Hedging-Modul mit einer separaten Einstiegsstrategie kombinieren, die definiert, wann die erste Position erstellt werden soll.
- Eigenkapitalbasierte Abschalteregeln oder maximale Tiefen-Limits hinzufügen, um unbegrenzte Hedging-Ketten zu verhindern.
- Portfolio-Level-Monitoring integrieren, um sicherzustellen, dass Margeanforderungen innerhalb akzeptabler Grenzen bleiben.
