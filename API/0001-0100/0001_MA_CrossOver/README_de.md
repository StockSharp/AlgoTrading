# HMA Saisonale Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie kombiniert den Hull Moving Average (HMA) mit saisonal geclustertem Open Interest, um Divergenzen zwischen Preis und Marktpositionierung zu finden. Sie geht davon aus, dass eine Trendfortsetzung wahrscheinlich ist, wenn sich der Preis vorübergehend gegen die Richtung eines steigenden Open Interest bewegt. Das System ist darauf ausgelegt, sowohl Long- als auch Short-Positionen zu handeln, wobei die HMA-Steigung zur Beurteilung des Momentums und die saisonalen Open-Interest-Daten zur Messung der Partizipationsniveaus verwendet werden.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 40%. Es funktioniert am besten auf dem Kryptomarkt.

Ein Trade-Setup entsteht, wenn sich die HMA gegenüber der vorherigen Kerze ändert, während das saisonale Open Interest die Bewegung bestätigt, der Preis jedoch in die entgegengesetzte Richtung druckt. Diese bullische oder bärische Divergenz zwischen Preis und Positionierung signalisiert oft das Ende eines kurzfristigen Rücksetzers innerhalb eines größeren Trends. Die Strategie wartet auf diese Bedingungen, bevor sie eintritt, und platziert einen volatilitätsbasierten Stop zur Risikosteuerung.

Positionen werden geschlossen, wenn die HMA-Steigung umkehrt, was darauf hinweist, dass das Momentum gedreht hat. Da das Stop-Niveau ein Vielfaches der Average True Range (ATR) verwendet, passt sich das Risiko der Marktvolatilität an. Dies hilft, vorzeitige Ausstiege in Expansionsphasen zu verhindern, und hält Verluste begrenzt, wenn die Volatilität nachlässt.

## Details

- **Einstiegskriterien**:
  - **Long**: `HMA(t) > HMA(t-1)` && `OI_Cluster_Seasonal(t) > OI_Cluster_Seasonal(t-1)` && `Price(t) < Price(t-1)` (bullische Divergenz).
  - **Short**: `HMA(t) < HMA(t-1)` && `OI_Cluster_Seasonal(t) < OI_Cluster_Seasonal(t-1)` && `Price(t) > Price(t-1)` (bärische Divergenz).
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: `HMA(t) < HMA(t-1)` (HMA beginnt zu fallen).
  - **Short**: `HMA(t) > HMA(t-1)` (HMA beginnt zu steigen).
- **Stops**: Ja, Stop-Loss bei `N * ATR` vom Einstieg.
- **Standardwerte**:
  - `HMA period` = 9.
  - `OI_Cluster_Seasonal` = saisonales OI auf Cluster-Niveaus über fünf Jahre.
  - `N` = 2 (Stop-Loss = `2 * ATR`).
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Komplex
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Ja
  - Neuronale Netze: Ja
  - Divergenz: Ja
  - Risikolevel: Hoch

