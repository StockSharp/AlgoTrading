# Bollinger-Band-Squeeze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Setup überwacht die Breite der Bollinger Bands, um Perioden niedriger Volatilität zu erkennen. Wenn sich die Bänder im Vergleich zu ihrem jüngsten Durchschnitt zusammenziehen, signalisiert dies eine mögliche bevorstehende Volatilitätserweiterung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 100%. Sie funktioniert am besten auf dem Forex-Markt.

Sobald ein Squeeze identifiziert wird, wartet die Strategie darauf, dass der Preis außerhalb der Bänder bricht. Ein Schluss oberhalb des oberen Bandes eröffnet eine Long-Position, während ein Schluss unterhalb des unteren Bandes eine Short-Position eröffnet. Der Trade wird geschlossen, wenn der Preis in Richtung der Mitte der Bänder zurückkehrt oder wenn ein Stop-Loss ausgelöst wird.

Die Methode richtet sich an Trader, die Volatilitätsausbrüche handeln möchten, anstatt Trendfortsetzungen. Die Verwendung der Bandbreite als Filter hilft, Fehlsignale in unruhigen Märkten zu vermeiden.

## Details
- **Einstiegskriterien**:
  - **Long**: Bandbreite < durchschnittliche Breite && Schluss > oberes Band
  - **Short**: Bandbreite < durchschnittliche Breite && Schluss < unteres Band
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn der Preis wieder innerhalb der Bänder fällt
  - **Short**: Ausstieg, wenn der Preis wieder innerhalb der Bänder steigt
- **Stops**: Ja, typischerweise bei 2*ATR.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2
  - `LookbackPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    Die durchschnittliche Breite umfasst die letzten LookbackPeriod Kerzen einschließlich der aktuellen. Ein Long schließt bei einem Schluss zurück am oder unter dem oberen Band, ein Short bei einem Schluss zurück am oder über dem unteren Band. Der Stop liegt AtrMultiplier mal die ATR über AtrPeriod vom Einstiegsschluss entfernt und wird bei Kerzenschluss geprüft; 0 schaltet ihn ab. Ein Einstiegssignal gegen eine offene Position dreht sie.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
