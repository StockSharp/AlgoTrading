# Kompasslinienstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den CompassLine-Experten, indem sie zwei komplementäre Filter zusammenführt:

* **Linie folgen** – ein Ausbruchspfad der Bänder um Bollinger, optional um ATR verschoben. Wenn der Preis außerhalb der Bänder schließt, verlängert sich die Spur in Richtung des Ausbruchs und zieht sich nie zurück, solange der Trend anhält.
* **Kompass** – eine logistische Transformation des Medianpreises im Verhältnis zum höchsten Hoch und niedrigsten Tief über dem Fenster des gleitenden Durchschnitts. Das Rohsignal wird doppelt geglättet (dreieckige Mittelung), um einen stabilen bullischen/bärischen Zustand zu erzeugen.

Eine Position wird nur eröffnet, wenn beide Filter mit dem Trend übereinstimmen. Optionale Zeitfilterung und Schutzstopps spiegeln die MQL-Logik wider.

## Einzelheiten

- **Eintrittskriterien**:
  - Die Folgelinie muss bei Long-Positionen nach oben (aktueller Schlusskurs über dem oberen Band) oder bei Shorts nach unten (aktueller Schlusskurs unter dem unteren Band) zeigen. Die Verschiebung um ATR kann mit `UseAtrFilter` umgeschaltet werden.
  - Der Kompassstatus (basierend auf `CompassPeriod`) muss nach der doppelten Glättungsphase für Long-Positionen positiv oder für Short-Positionen negativ sein.
  - Der Handel wird nur ausgeführt, wenn der optionale Sitzungsfilter (`UseTimeFilter` mit `Session` in HHmm-HHmm) dies zulässt.
- **Lang/Kurz**: Beide Richtungen werden unterstützt.
- **Ausstiegskriterien**:
  - `CloseMode = None` behält die Position bei, bis ein Gegeneinstieg oder ein Schutzstopp erfolgt.
  - `CloseMode = BothIndicators` wird geschlossen, wenn sowohl „Linie folgen“ als auch „Kompass“ gleichzeitig die Richtung umkehren.
  - `CloseMode = FollowLineOnly` wird beendet, wenn Follow Line gegen die Position kippt.
  - `CloseMode = CompassOnly` wird beendet, wenn der Kompass die Polarität ändert.
- **Stopps**: Die Distanzen `TakeProfit` und `StopLoss` (in Sicherheitsschritten) werden nach jeder Eingabe angewendet, wenn sie größer als Null sind.
- **Standardwerte**:
  - `FollowBbPeriod` = 21
  - `FollowBbDeviation` = 1
  - `FollowAtrPeriod` = 5
  - `UseAtrFilter` = falsch
  - `CompassPeriod` = 30 (Glättungslänge = Round(CompassPeriod / 3))
  - `CloseMode` = Keine
  - `UseTimeFilter` = falsch
  - `Session` = „0000-2400“
  - `TakeProfit` = 0
  - `StopLoss` = 0
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger-Bänder, ATR, dreieckiger gleitender Durchschnitt
  - Stopps: Optional
  - Komplexität: Mittelschwer
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikostufe: Mittel

## Zusätzliche Hinweise

- Die Compass-Glättung verwendet ein dreieckiges Fenster gleich rund(`CompassPeriod` / 3), was der ursprünglichen Indikatorimplementierung sehr nahe kommt.
- Sitzungszeichenfolgen wie `0930-1600` beschränken den Handel auf das angegebene Fenster, während die Indikatorzustände weiterhin außerhalb der Sitzung aktualisiert werden.
- Bei Schutzanordnungen werden die High-Level-Helfer von StockSharp wiederverwendet, sodass die Logik mit den Portfolio-Risikomanagementmodulen kompatibel ist.
