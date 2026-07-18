# Strategie Three White Soldiers
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Drei Weiße Soldaten-Muster ist eine klassische bullische Umkehr, bestehend aus drei aufeinanderfolgenden starken Aufwärtskerzen. Nach einem Abwärtstrend markiert diese Sequenz oft den Beginn einer anhaltenden Aufwärtsbewegung, da der Kaufdruck die Verkäufer überwältigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 175%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie steigt long ein, sobald der dritte Soldat geformt ist, und erwartet eine Fortsetzung des Schwunganstiegs. Short-Trades werden nicht eingegangen, da das Setup rein bullisch ist, aber das System erlaubt das Schließen von Short-Positionen, die mit anderen Methoden eingegangen wurden.

Stops werden knapp unterhalb des Musters platziert, um gegen Fehlsignale zu schützen, und Positionen werden beendet, wenn der Preis wieder unter dieses Niveau schließt.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
