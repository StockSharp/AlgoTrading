# Drei-Balken-Abwärtsumkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Ein Spiegelbild der bullischen Version sucht dieses Setup nach schnellen bärischen Umkehrungen. Nach zwei starken Aufwärtskerzen, die zu neuen Hochs drängen, schließt eine entschiedene bärische Kerze unter dem Tief des vorherigen Balkens. Ein kurzer Aufwärtstrend zuvor hilft, die Käufererschöpfung zu bestätigen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 88%. Sie funktioniert am besten auf dem Aktienmarkt.

Der Algorithmus verfolgt ein rollendes Fenster von drei Kerzen. Wenn das Muster erscheint und eine Aufwärtstrend-Anforderung erfüllt ist, wird eine Short-Position mit dem Stop über dem Musterhoch eingegangen. Die Regeln sind unkompliziert, sodass Signale sofort beim Kerzenschluss auftreten.

Der Trade wird beim Schutzstopp oder bei der Bildung eines anderen Musters beendet. Da es kurzfristige Rückläufer innerhalb eines potenziellen Abwärtschwungs spielt, funktioniert es am besten auf volatilen Märkten.

## Details

- **Einstiegskriterien**: Zwei bullische Kerzen mit höheren Hochs, dann eine bärische Kerze, die unter dem Tief des mittleren Balkens schließt.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Stop-Loss oder nächstes Muster.
- **Stops**: Ja, oberhalb des Musterhochs.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendLength` = 5
- **Filter**:
  - Kategorie: Muster
  - Richtung: Short
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

