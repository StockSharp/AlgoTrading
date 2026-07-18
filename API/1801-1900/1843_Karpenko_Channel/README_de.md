# Karpenko-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Karpenko-Kanal-Strategie erstellt einen dynamischen Preiskanal mithilfe zweier gleitender Durchschnitte. Die Basislinie ist ein Durchschnitt der Schlusskurse, während die oberen und unteren Grenzen aus dem durchschnittlichen Hoch-Tief-Bereich abgeleitet werden, skaliert mit dem goldenen Verhältnis 1.618. Der Kanal erweitert sich, bis er den aktuellen Balken umschließt.

Ein Signal für Long erscheint, wenn die obere Grenze, die zuvor über der Basislinie lag, diese von oben nach unten kreuzt. Ein Short-Signal entsteht, wenn die obere Grenze die Basislinie von unten nach oben kreuzt, nachdem sie darunter war. Bestehende Positionen in entgegengesetzter Richtung werden bei einem Regimewechsel geschlossen.

Es werden nur abgeschlossene Kerzen verarbeitet. Feste Stop-Loss- und Take-Profit-Niveaus schützen jeden Trade.

## Details

- **Einstiegskriterien:**
  - **Long:** Vorherige obere Grenze über der Basislinie und aktueller Wert darunter oder gleich.
  - **Short:** Vorherige obere Grenze unter der Basislinie und aktueller Wert darüber oder gleich.
- **Ausstiegskriterien:**
  - Long schließen, wenn die vorherige obere Grenze unter der Basislinie war.
  - Short schließen, wenn die vorherige obere Grenze über der Basislinie war.
- **Stops:** Feste Stop-Loss- und Take-Profit-Abstände in Preiseinheiten.
- **Standardwerte:**
  - `Base MA` = 144
  - `History` = 500
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = 4 hour
- **Filter:**
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Custom
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
