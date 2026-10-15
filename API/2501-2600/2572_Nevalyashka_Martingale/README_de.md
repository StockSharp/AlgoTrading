# Nevalyashka Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Nevalyashka Martingale-Strategie ist eine direkte Portierung des MetaTrader 5-Expertenberaters "Nevalyashka3_1". Sie führt ein Einzelsymbol-Martingale aus, das nach Verlustgeschäften zwischen Kaufen und Verkaufen wechselt. Die Strategie beginnt immer mit dem Verkaufen und misst das Kontokapital, um zu entscheiden, ob der vorherige Handelszyklus mit Gewinn oder Verlust endete. Ein Gewinn setzt das Volumen auf die Basislotgröße zurück und behält die Richtung unverändert bei, während ein Verlust die Lotgröße multipliziert und die Richtung in einem Versuch wechselt, den Drawdown zu erholen.

## Funktionsweise
- **Erster Trade** – eine Short-Position wird auf der ersten abgeschlossenen Kerze mit der Basislotgröße eröffnet.
- **Kapitalverfolgung** – die Strategie speichert den höchsten beobachteten Kapitalwert. Wenn keine Position offen ist, vergleicht sie das aktuelle Kapital mit dem gespeicherten Höchststand.
  - Wenn das Kapital ein neues Hoch markierte, verwendet der nächste Trade die Basislotgröße und wiederholt die letzte Richtung.
  - Wenn das Kapital kein neues Hoch markierte, erhöht der nächste Trade den Lot mit dem Multiplikator und wechselt die Richtung.
- **Stop Loss / Take Profit** – jede Order verwendet feste Abstände, die in "Punkten" (Instrumentenschritte) definiert sind. Der Take Profit spiegelt den ursprünglichen Experten: Der Stop liegt `StopLossPoints` vom Einstieg entfernt und das Ziel `TakeProfitPoints`.
- **Trailing** – sobald sich der Preis um `MoveProfitPoints` bewegt, wird der Stop enger gezogen. Jede Bewegung erfordert einen zusätzlichen `MoveStepPoints`-Puffer, damit der Stop nur dann vorrückt, wenn der Markt weiter drückt. Wenn der Stop über den Einstiegspreis hinausgeht, wird das geplante Volumen durch den Multiplikator geteilt, um den nächsten Trade wieder in Richtung Basislot zu skalieren.
- **Positionsausgang** – die Position schließt sofort, wenn das Kerzenhoch/-tief den Stop- oder Take-Level erreicht. Nach dem Schließen bewertet die Strategie das Kapital und bereitet das nächste Signal vor.

## Parameter
- `BaseVolume` – Lotgröße für den Ersthandel und alle profitablen Zyklen (Standard 0.1).
- `VolumeMultiplier` – Faktor, der nach einem Verlust angewendet wird, um den nächsten Lot zu erhöhen (Standard 1.1).
- `TakeProfitPoints` – Take-Profit-Abstand gemessen in Preispunkten (Standard 94).
- `MoveProfitPoints` – minimale günstige Exkursion, bevor der Trailing-Stop aktiviert wird (Standard 25).
- `MoveStepPoints` – extra Puffer, der zwischen aufeinanderfolgenden Trailing-Anpassungen benötigt wird (Standard 11).
- `StopLossPoints` – anfänglicher Stop-Loss-Abstand gemessen in Preispunkten (Standard 70).
- `CandleType` – Zeitrahmen für das Trade-Management. Standard sind 5-Minuten-Kerzen.

## Details zur Positionsverwaltung
- Die Strategie hält `_plannedVolume`, um die ursprüngliche "Lot"-Variable zu spiegeln. Es ändert sich nur nach dem Schließen eines Trades oder wenn der Stop das Break-Even überschreitet.
- `AdjustVolume` respektiert Börsenregeln, indem die Lotgröße an `VolumeStep` ausgerichtet und `MinVolume`/`MaxVolume` erzwungen wird.
- `GetPointValue` repliziert die MT5-Logik für "adjusted point": Für Instrumente mit 3 oder 5 Dezimalstellen wird die Punktgröße mit 10 multipliziert, um mit ganzen Pips zu arbeiten.
- `HandleLongPosition` und `HandleShortPosition` verwenden Kerzenhochs und -tiefs, um die Stop-Modifikation und das Ausstiegsverhalten von MT5 zu emulieren, ohne auf den Indikatorverlauf angewiesen zu sein.

## Verwendungshinweise
- Die Strategie geht davon aus, dass sie ein einziges Wertpapier handelt. Fügen Sie sie der Strategie hinzu und setzen Sie `Security`/`Portfolio` vor dem Start.
- Da es sich um ein Martingale handelt, wächst das Risiko nach einer Verlustserie schnell. Passen Sie `BaseVolume` und `VolumeMultiplier` sorgfältig an und testen Sie mit realistischen Margensanforderungen.
- Die Stop- und Take-Profit-Abstände werden in Instrumentenpunkten definiert. Stellen Sie sicher, dass die Sicherheitsmetadaten (`PriceStep`, `VolumeStep`, `MinVolume`) ausgefüllt sind, damit Offsets und Lotberechnungen mit Ihrem Broker übereinstimmen.
- Die Trailing-Logik wirkt auf abgeschlossenen Kerzen. Intrabar-Stop-Treffer können im Live-Trading früher auftreten, abhängig vom Preisweg.
