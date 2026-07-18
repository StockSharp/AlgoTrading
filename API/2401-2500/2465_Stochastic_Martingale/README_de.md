# Stochastic Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen klassischen Stochastic-Oszillator-Einstieg mit einer Martingale-artigen Mittelung.
Eine Position wird geöffnet, wenn die %K-Linie die %D-Linie kreuzt und der Oszillator über/unter konfigurierbaren Zonen liegt.
Wenn sich der Kurs um einen definierten Schritt gegen die Position bewegt, erhöht die Strategie das Volumen um einen Multiplikator.
Positionen werden geschlossen, wenn der angesammelte Gewinn eine definierte Anzahl von Punkten erreicht.

## Details
- **Einstiegskriterien**
  - Long: %K > %D und %D > ZoneBuy
  - Short: %K < %D und %D < ZoneSell
- **Mittelung**
  - Zusätzliche Orders werden alle `Step` Punkte platziert (oder `Step * Orderanzahl` in Modus 1).
  - Das Volumen jeder neuen Order wird mit `Mult` multipliziert.
- **Ausstiegskriterien**
  - Long: Kurs ≥ letzter Kaufkurs + `ProfitFactor * Orderanzahl` Punkte.
  - Short: Kurs ≤ letzter Verkaufskurs − `ProfitFactor * Orderanzahl` Punkte.
- **Parameter** umfassen Schrittgröße, Schrittmodus, Gewinnfaktor, Multiplikator, Ausgangsvolumina und Stochastic-Perioden.
- **Filter**
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
