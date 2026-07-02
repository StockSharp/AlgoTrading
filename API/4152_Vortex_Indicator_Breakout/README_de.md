# Vortex-Indikator-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Experten **Vortex Indicator System.mq4** in die StockSharp-Hochebene API. Die ursprüngliche Idee war
ist in *Technical Analysis of Stocks & Commodities* (Januar 2010) veröffentlicht und basiert auf dem Vortex-Indikator-Crossover zum Armbre
Erteilen Sie Aufträge zum Hoch/Tief der Crossover-Kerze. Die StockSharp-Version behält den gleichen Entscheidungsfluss bei: Ein Crossover schließt die
Die entgegengesetzte Position aktiviert einen Ausbruchsauslöser am Extrempunkt des Crossover-Balkens und die nächste Kerze, die dieses Niveau durchbricht, führt den aus
Marktordnung.

## Wie es funktioniert

1. Ein einzelnes Kerzenabonnement wird gemäß `CandleType` eröffnet. Der resultierende Stream ist an ein `VortexIndicator` Insta gebunden
Sobald `Bind` verwendet wird, erhält die Strategie immer synchronisierte VI+- und VI--Werte für die fertigen Kerzen.
2. Wenn das Aufwärmen des Indikators abgeschlossen ist, verfolgt der Algorithmus die vorherigen VI-Werte, um die gleichen Übergangsbedingungen u zu erkennen
sed im MQL-Experten: `VI+` Kreuzung über `VI-` oder umgekehrt zwischen den letzten beiden geschlossenen Kerzen.
3. **Setup-Phase** – Sobald ein bullischer Crossover erkannt wird, wird jede offene Short-Position sofort geschlossen und das Hoch von th
Die Crossover-Kerze wird zum ausstehenden Long-Trigger. Der entgegengesetzte Crossover schließt eine bestehende Long-Position und speichert das Tief
dieses Balkens als Short-Trigger.
4. **Triggerphase** – bei jeder weiteren abgeschlossenen Kerze prüft die Strategie, ob der aufgezeichnete Triggerpreis berührt wurde („Hi
ghPrice` ≥ long trigger or `LowPrice` ≤ kurzer Trigger). Wenn dies der Fall ist, übermittelt es eine Marktorder, die so dimensioniert ist, dass beide den verbleibenden Gegner glätten
Site-Präsenz (falls die vorherige Bestellung noch nicht abgeschlossen wurde) und eröffnen Sie eine neue Position mit `TradeVolume`.
5. Sobald ein Auftrag ausgelöst wird, wird der entsprechende Auslöser gelöscht. Wenn kein Ausbruch erfolgt, bleibt das Setup bis zu einem neuen Crossove aktiv
r überschreibt es.
6. Ausgänge basieren ausschließlich auf der Crossover-Logik: Das entgegengesetzte Signal glättet sofort die aktuelle Position und aktiviert ein neues b
Reakout-Trigger, der die MetaTrader-Implementierung widerspiegelt.

## Signale

- **Bullisches Setup** – tritt auf, wenn `VI+` bei der vorherigen geschlossenen Kerze unter oder gleich `VI-` war und am längsten darüber steigt
Neustes. Der Long-Trigger ist auf das Hoch dieser Kerze eingestellt.
- **Bullische Ausführung** – die nächste Kerze, deren Hoch den Auslöser erreicht, sendet eine Marktkauforder mit `TradeVolume` (plus etwaiger Vo
(die zum Schließen einer ausstehenden Short-Position erforderliche Leuchtkraft).
- **Bearisches Setup** – tritt auf, wenn `VI-` bei der vorherigen geschlossenen Kerze unter oder gleich `VI+` war und am längsten darüber steigt
Neustes. Der Short-Trigger ist auf das Tief dieser Kerze eingestellt.
- **Bearische Ausführung** – die nächste Kerze, deren Tief den Auslöser berührt, sendet einen Marktverkaufsauftrag mit `TradeVolume` (plus dem Vo
erforderliche Lichtstärke, um eine offene Long-Position abzuflachen).

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `VortexLength` | 14 | Auf den Vortex-Indikator angewendeter Zeitraum. |
| `CandleType` | 1 Stunde | Zeitrahmen für Kerzen und Indikatoraktualisierungen. |
| `TradeVolume` | 1 | Market-Order-Größe, die für Neuzugänge verwendet wird. |

## Hinweise zur Implementierung

- Die Strategie reagiert nur auf **fertige** Kerzen, um den Konvertierungsrichtlinien zu entsprechen. Intrabar-Ausbrüche werden erkannt
Sobald eine Kerze mit einem Hoch/Tief über dem gespeicherten Auslöser schließt.
- Ausstehende Trigger werden am `OnStopped` gelöscht, sodass die Instanz sauber und ohne verbleibenden Status neu gestartet werden kann.
- Bei der Ausführung einer Breakout-Order erhöht der Algorithmus das Volumen, wenn er immer noch eine entgegengesetzte Position hält, und erreicht so das gleiche, z
Wirkung als MetaTrader-Experte, der die aktive Bestellung schloss, bevor die neue geöffnet wurde.
