# Precipice Martin-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die Precipice Martin-Strategie ist ein mechanischer Gitter-Ansatz, der beim Abschluss jeder verarbeiteten Kerze eine Marktorder eröffnet. Der ursprüngliche MetaTrader 5-Experte erstellte bei jedem neuen Balken eine symmetrische Kauf- und Verkaufsposition und verwaltete Ausstiege mit statischen Stop-Loss- und Take-Profit-Offsets, die in Pips ausgedrückt waren. Verlusttrades erhöhten die nächste Ordergröße um einen Martingale-Multiplikator, während profitable Trades die Positionsgröße auf das Mindestlot zurücksetzten.

Dieser C#-Port folgt derselben High-Level-Logik unter Verwendung der StockSharp High-Level-API. Für jede abgeschlossene Kerze:

1. Aktualisiert die Strategie bestehende Long- und Short-Positionen und schließt sie, wenn die Kerzenbandbreite das konfigurierte Stop-Loss- oder Take-Profit-Niveau durchstochen hat.
2. Wenn flat, wechselt sie zwischen dem Eröffnen einer Long- oder Short-Marktposition (wenn beide Richtungen aktiviert sind), um das Doppeleinstiegsverhalten des Quell-Roboters zu emulieren, während es mit StockSharps Nettopositionen-Abrechnung kompatibel bleibt.
3. Wendet optionales Martingale-Sizing an, sodass aufeinanderfolgende Verlusttrades das Volumen um den konfigurierten Multiplikator erhöhen.
4. Berechnet Stop-Loss- und Take-Profit-Ziele aus benutzerdefinierten Pip-Distanzen, die basierend auf der Tick-Größe des Instruments in absolute Preis-Offsets umgerechnet werden.

## Konvertierungshinweise

* Der ursprüngliche EA eröffnete eine Long- und Short-Position auf jedem neuen Balken, wenn beide Schalter aktiviert waren. Da StockSharp standardmäßig Nettopositionen verwendet, wechselt die C#-Version bei aufeinanderfolgenden Gelegenheiten zwischen Richtungen, um ein sofortiges Ausgleichen der Nettoposition zu vermeiden. Dies stellt sicher, dass beide Marktseiten im Laufe der Zeit gehandelt werden.
* Stop-Loss- und Take-Profit-Management erfolgt intern, indem geprüft wird, ob der Hoch-/Tiefwert einer Kerze das entsprechende Niveau ausgelöst hätte. Wenn ein Niveau erreicht wird, schließt die Strategie die Position mit einer Marktorder und zeichnet den realisierten Gewinn oder Verlust für die Martingale-Logik auf.
* Lot-Validierung repliziert die `LotCheck`-Routine aus MQL5, indem das berechnete Volumen auf den Exchange `VolumeStep` gerundet, Mindest- und Höchstgrenzen eingehalten und die Order storniert wird, wenn der gerundete Wert null wird.
* Die Martingale-Routine spiegelt `CalculateLot`: Jeder nicht profitable Ausstieg multipliziert die nächste Ordergröße mit `MartingaleCoefficient`, während ein profitabler Ausstieg den Multiplikator auf eins zurücksetzt.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| **Use Buy** | Aktiviert das Eröffnen von Long-Positionen. |
| **Buy SL/TP (pips)** | Distanz (in Pips) sowohl für den Stop-Loss als auch für den Take-Profit von Long-Trades. Ein Wert von 0 deaktiviert Ausstiege für diese Seite. |
| **Use Sell** | Aktiviert das Eröffnen von Short-Positionen. |
| **Sell SL/TP (pips)** | Distanz (in Pips) sowohl für den Stop-Loss als auch für den Take-Profit von Short-Trades. |
| **Use Martingale** | Schaltet das Martingale-Positions-Sizing um. Wenn deaktiviert, verwendet jede Order die Mindestlotgröße. |
| **Martingale Coefficient** | Multiplikator, der nach jedem nicht profitablen Trade auf das Mindestlot angewendet wird. |
| **Candle Type** | Zeitrahmen der von der Strategie verarbeiteten Kerzen. Standardmäßig arbeitet die Strategie auf Ein-Minuten-Balken, aber jeder verfügbare Zeitrahmen kann ausgewählt werden. |

## Handelslogik

1. **Pip-Größenberechnung** – die Strategie leitet den Pip-Wert aus der Tick-Größe des Instruments ab. Für Instrumente mit Bruchteil-Pips (5-stellige FX-Symbole) wird der Pip als 10 Ticks betrachtet, entsprechend der MT5-Implementierung.
2. **Einstiegsauswahl** – wenn sowohl `Use Buy` als auch `Use Sell` aktiviert sind, wechselt die Strategie zwischen Long- und Short-Einstiegen, wann immer sie flat ist. Wenn nur eine Richtung aktiviert ist, folgen alle Trades dieser Richtung. Einstiege werden unmittelbar nach Abschluss einer Kerze ausgelöst und die Strategie ist online.
3. **Stop/Take-Niveaus** – wenn ein Trade eröffnet wird, werden Stop-Loss und Take-Profit als absolute Preise relativ zum Einstieg unter Verwendung der gewählten Pip-Distanz gespeichert. Ein Wert von null deaktiviert beide Niveaus für diese Richtung.
4. **Ausstiegshandling** – bei jeder abgeschlossenen Kerze werden die Hoch-/Tiefwerte geprüft. Wenn das Tief den Long-Stop verletzt oder das Hoch das Long-Ziel übertrifft, wird die Long-Position geschlossen. Für Shorts ist die Logik gespiegelt. Ausstiege werden mit Marktorders unter Verwendung des zuletzt aufgezeichneten Volumens für diese Position ausgeführt.
5. **Martingale-Sizing** – das nächste Ordervolumen entspricht dem Mindestlot des Instruments multipliziert mit dem aktuellen Martingale-Multiplikator. Verlusttrades (einschließlich Break-Even-Ergebnisse) multiplizieren den Multiplikator mit `MartingaleCoefficient`; profitable Trades setzen ihn auf eins zurück. Volumenrundung auf den Exchange-Schritt wird vor dem Einreichen der Order angewendet.
6. **Sicherheitsprüfungen** – wenn das gerundete Volumen unter dem Exchange-Mindestlot liegt, wird die Order übersprungen, um "Nicht genug Geld"-Fehler zu vermeiden, die der ursprüngliche EA über `CheckVolume` behandelte.

## Verwendungsrichtlinien

1. Konfigurieren Sie den gewünschten Zeitrahmen in **Candle Type**, um dem in MT5 verwendeten Chartperiode zu entsprechen.
2. Passen Sie die Pip-Distanzen an das gewünschte Stop-Loss- und Take-Profit-Verhalten an. Beachten Sie, dass die Offsets absolute Preise sind, sodass der tatsächliche Stop in Währung vom Symbol abhängt.
3. Aktivieren oder deaktivieren Sie das Martingale-Sizing entsprechend Ihrer Risikobereitschaft. Da das Volumen nach aufeinanderfolgenden Verlusten exponentiell wächst, verwenden Sie konservative Multiplikatoren.
4. Setzen Sie die Strategie auf einem Wertpapier ein, das Echtzeit-Kerzen bereitstellt. Die Strategie benötigt abgeschlossene Balken und wird nicht auf unvollständigen Kerzen handeln.
5. Überwachen Sie die Margennutzung, wenn Martingale aktiv ist. Die StockSharp-Version wechselt absichtlich die Richtungen, wenn beide Seiten aktiviert sind, sodass zu jedem Zeitpunkt nur eine Nettoposition offen ist.

## Unterschiede zur MT5-Implementierung

* **Nettopositionen** – die Wechsellogik ersetzt die gleichzeitigen gehedgten Einstiege des ursprünglichen Algorithmus. Wenn ein echtes Hedging-Konto erforderlich ist, können Sie zwei Instanzen der Strategie ausführen (eine mit `Use Buy`, eine mit `Use Sell`).
* **Order-Platzierung** – Schutzorders werden nicht im Exchange-Buch platziert. Stattdessen werden Ausstiege über Marktorders ausgeführt, wenn die Strategie erkennt, dass das Stop- oder Take-Niveau überschritten wurde.
* **Historien-Scan** – das MT5-Skript berechnete den Martingale-Koeffizienten durch Scannen der gesamten Handelshistorie bei jedem Tick neu. Die C#-Version verwaltet den Multiplikator inkrementell, um den Overhead zu reduzieren und gleichzeitig das Verhalten zu erhalten.

## Risikohinweis

Martingale-basierte Strategien können während Verliererserien sehr große Positionen generieren, die die Kontorisikogrenzen überschreiten können. Testen Sie die Strategie immer auf simulierten Daten vor dem Live-Einsatz und stellen Sie sicher, dass der gewählte Multiplikator und die Pip-Distanzen zur Volatilität des gehandelten Instruments passen.
