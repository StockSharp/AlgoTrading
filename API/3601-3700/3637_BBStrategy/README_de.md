# BBStrategy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

BBStrategy ist ein Bollinger-Band-Breakout-System, das aus dem MetaTrader-Expertenberater „BBStrategy“ abgeleitet wurde. Die Strategie verfolgt zwei Sätze von Bollinger-Bändern mit demselben Zeitraum, aber unterschiedlichen Abweichungsmultiplikatoren. Wenn der Preis das äußere Band durchbricht, aktiviert der Algorithmus einen Handel, ein tatsächlicher Einstieg wird jedoch verschoben, bis der Preis wieder in das innere Band zurückkehrt. Durch dieses Verhalten wird versucht, den Kauf überzogener Ausbrüche oder den Verkauf stark überverkaufter Bedingungen zu vermeiden und gleichzeitig die Fortsetzungsbewegung nach einer Volatilitätsexpansion zu erfassen.

## Kernlogik

1. Abonnieren Sie Kerzen und berechnen Sie zwei Bollinger-Bänder:
   - **Äußeres Band** verwendet einen konfigurierbaren Abweichungsmultiplikator (Standard 3,0).
   - **Inneres Band** verwendet eine geringere Abweichung (Standard 2,0).
2. Erkennen Sie, wann der Schlusskurs außerhalb des äußeren Bandes endet:
   - Über den oberen äußeren Bandarmen befindet sich ein langer Aufbau.
   - Unterhalb der unteren äußeren Bandarme ein kurzer Aufbau.
3. Steigen Sie nur dann ein, wenn die nächste abgeschlossene Kerze wieder innerhalb des inneren Bandes in Richtung des Ausbruchs schließt. Während der Preis darauf wartet, wieder einzusteigen, bleibt die Strategie im „Wartezustand“ auf die entsprechende Richtung.
4. Senden Sie eine einzelne Marktorder, wenn die Bedingungen übereinstimmen und keine offenen Positionen oder aktiven Orders vorhanden sind. Bestehende Gegenpositionen werden durch Erhöhung des Volumens der Market Order geschlossen.
5. Optionale Take-Profit- und Stop-Loss-Distanzen (ausgedrückt in Punkten) werden in absolute Preis-Offsets umgewandelt und über den integrierten Schutzhelfer verwaltet.

## Parameter

| Name | Beschreibung |
|------|-------------|
| **Bestellvolumen** | Handelsgröße für jede Position. |
| **Bollinger Zeitraum** | Anzahl der Kerzen, die für beide Bollinger-Bandberechnungen verwendet werden. |
| **Innere Abweichung** | Abweichungsmultiplikator für das innere Band, der Pullbacks validiert. |
| **Äußere Abweichung** | Abweichungsmultiplikator für das äußere Band, das Ausbrüche erkennt. |
| **Stop-Loss-Punkte** | Abstand des Schutzstopps in Punkten (0 deaktiviert den Stopp). |
| **Take-Profit-Punkte** | Take-Profit-Distanz in Punkten (0 deaktiviert das Ziel). |
| **Kerzentyp** | Kerzenzeitrahmen für Berechnungen. |

## Notizen

- Die Strategie handelt jeweils nur eine Position und ignoriert neue Signale, solange Aufträge aktiv sind.
- Für das Risikomanagement wandelt der Helfer MetaTrader „Punkte“ in tatsächliche Preiserhöhungen basierend auf der Tick-Größe des Instruments um.
- Diagrammzeichnungen umfassen Kerzen, beide Bollinger-Bänder und die eigenen Trades der Strategie, um das visuelle Debuggen zu erleichtern.
