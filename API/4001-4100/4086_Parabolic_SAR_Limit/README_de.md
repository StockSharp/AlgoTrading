# Strategie Parabolic SAR Limit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Parabolic SAR Limit ist eine direkte Portierung des MT4 Expert Advisors **ytg_Parabolic_exp.mq4**. Das System hält kontinuierlich Kauf- und Verkaufs-Limit-Orders an den Parabolic SAR-Wert gebunden und lässt den Markt die Order in einen Handel ziehen. Sobald sie gefüllt ist, überwacht die Strategie die offene Position und führt Stop-Loss- oder Take-Profit-Exits mithilfe von Candle-Extremen durch, was das ursprüngliche MQL-Verhalten widerspiegelt.

## Strategielogik

1. Die Strategie abonniert eine konfigurierbare Kerzenserie (standardmäßig 4-Stunden-Zeitrahmen) und berechnet den Indikator Parabolic SAR mit der gleichen Schrittweite und den gleichen Maximalwerten wie das MT4-Skript.
2. Auf jeder fertigen Kerze:
   - Wenn der SAR-Punkt *unter* dem Tiefststand des Balkens liegt und das beste Gebot mindestens `MinOrderDistancePoints` über dem SAR-Preis liegt, wird eine Kauf-Limit-Order genau auf den SAR-Wert platziert (oder neu ausgerichtet).
   - Wenn der SAR-Punkt *über* dem Hoch des Balkens liegt und der beste Brief mindestens `MinOrderDistancePoints` unter dem SAR-Preis liegt, wird eine Verkaufslimitorder zu diesem SAR-Preis platziert (oder neu ausgerichtet).
   - Es wird nur eine ausstehende Bestellung pro Seite verwaltet. Wenn sich der SAR verschiebt, wird die aktive ausstehende Bestellung storniert und eine neue auf der aktualisierten Ebene übermittelt.
3. Wenn eine ausstehende Order ausgeführt wird, werden die Stop-Loss- und Take-Profit-Distanzen (ausgedrückt in Punkten) mithilfe der Sicherheitspreisstufe in absolute Preise umgewandelt. Diese Ebenen werden als virtuelle Schutzgrenzen gespeichert.
4. Jede neue Kerze überprüft die aufgezeichneten Grenzen. Wenn die Kerzenspanne das Stop- oder Take-Level berührt, schließt die Strategie die entsprechende Position sofort und setzt den Schutzzustand zurück.

## Parameter

- **CandleType** – Zeitrahmen für Signalkerzen. Standardmäßig werden 4-Stunden-Kerzen verwendet, um dem MT4-Eingabeparameter `timeframe` zu entsprechen.
- **SarStep** – Parabolic SAR Beschleunigungsfaktor (`step` in MT4). Steuert, wie schnell der SAR mit dem Preis mithalten kann.
- **SarMaximum** – maximale Beschleunigung (`maximum` in MT4). Begrenzt die SAR-Geschwindigkeit.
- **StopLossPoints** – Abstand in Punkten zwischen dem Einstiegspreis und dem Stop-Level. Zum Deaktivieren auf `0` setzen.
- **TakeProfitPoints** – Abstand in Punkten zwischen dem Einstiegspreis und dem Take-Profit-Level. Zum Deaktivieren auf `0` setzen.
- **MinOrderDistancePoints** – ahmt `MODE_STOPLEVEL` in MT4 nach. Ausstehende Aufträge werden nur übermittelt, wenn der Marktpreis weiter als dieser Abstand vom SAR-Wert entfernt ist.
- **OrderVolume** – Lose (Volumen) für jede ausstehende Bestellung. Richten Sie es am `VolumeStep` des Instruments aus.

Alle punktbasierten Entfernungen werden mithilfe des Instruments `PriceStep` in Preise umgewandelt, sodass das Verhalten auf allen Märkten konsistent bleibt.

## Handelsverhalten

- Funktioniert in beide Richtungen gleichzeitig: Eine Kauf- und eine Verkaufs-Limit-Order können nebeneinander bestehen, wenn der SAR den Preis umkehrt.
- Ausstehende Bestellungen werden immer auf den letzten SAR-Wert ausgerichtet; Veraltete Bestellungen werden storniert, bevor eine neue registriert wird.
- Stop-Loss- und Take-Profit-Ausstiege werden praktisch über Kerzenhochs und -tiefs abgewickelt, da hochrangige StockSharp-Strategien SL/TP nicht direkt an ausstehende Aufträge anhängen.
- Die Strategie basiert auf den besten Geld-/Briefdaten, sofern verfügbar; andernfalls wird der Schlusskurs der Kerze als Fallback zur Bewertung der Distanzbedingungen verwendet.

## Portierungshinweise

- `MinOrderDistancePoints` ist standardmäßig `0`, aber Sie können es auf das Stop-Level des Brokers setzen, wenn der Handelsplatz einen Mindestabstand vorschreibt.
- Die Schutzniveaus werden automatisch zurückgesetzt, wenn die Position geschlossen oder die ausstehende Order storniert wird, wobei die Logik mit der des MT4-Experten identisch bleibt.
- Kommentare im C#-Code erläutern die Verwendung von API auf hoher Ebene, die Indikatorbindung und den Bestelllebenszyklus für eine einfachere Wartung.

## Nutzungstipps

- Bereitstellung von Angeboten der Stufe 1 für eine präzise Entfernungsprüfung; Andernfalls stellen Sie sicher, dass der Schlusskurs der Kerze ein guter Indikator für den aktuellen Marktpreis ist.
- Überprüfen Sie die `PriceStep` und `VolumeStep` Ihres Symbols, damit die Punktabstände und das Bestellvolumen in gültige Preise und Mengen umgewandelt werden können.
- Da Exits anhand abgeschlossener Kerzen bewertet werden, sollten Sie kürzere Zeitrahmen verwenden, wenn Sie eine feinere Granularität für die Stop-Loss-Überwachung benötigen.
