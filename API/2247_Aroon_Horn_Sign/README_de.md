# Aroon Horn Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Aroon Horn Sign**-Strategie sucht mithilfe des Aroon-Indikators nach Trendumkehrungen.
Sie überwacht die Aroon-Up- und Aroon-Down-Linien auf Kerzen mit höherem Zeitrahmen. Wenn die
Aroon-Up-Linie über die Aroon-Down-Linie kreuzt und über dem Niveau 50 bleibt,
signalisiert dies eine potenzielle bullische Umkehr. Die Strategie schließt alle Short-Positionen
und eröffnet eine neue Long-Position. Umgekehrt gilt: Wenn Aroon Down über 50 dominiert,
wird jede bestehende Long-Position geschlossen und eine Short-Position eröffnet.

Der Ansatz verwendet feste Take-Profit- und Stop-Loss-Niveaus, die in Preiseinheiten ausgedrückt werden.
Diese Niveaus werden durch das integrierte Risikoschutzmodu aktiviert.
Da die Logik nur auf Aroon-Werten basiert, funktioniert sie auf verschiedenen
Märkten und Zeitrahmen ohne zusätzliche Filter.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: `Aroon Up` > `Aroon Down` und `Aroon Up` >= 50.
  - **Short**: `Aroon Down` > `Aroon Up` und `Aroon Down` >= 50.
- **Ausstiegskriterien**:
  - Long-Positionen schließen, wenn eine Short-Einstiegsbedingung erscheint.
  - Short-Positionen schließen, wenn eine Long-Einstiegsbedingung erscheint.
- **Stops**: Fester Stop-Loss und Take-Profit mit `StartProtection`.
- **Standardwerte**:
  - `AroonPeriod` = 9
  - `CandleType` = 4‑Stunden-Kerzen
  - `TakeProfit` = 2000 (Preiseinheiten)
  - `StopLoss` = 1000 (Preiseinheiten)
- **Filter**:
  - Kategorie: Trendumkehr
  - Richtung: Long und Short
  - Indikatoren: Aroon
  - Komplexität: Einfach
  - Risikolevel: Mittel
