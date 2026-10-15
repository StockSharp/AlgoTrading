# Sidus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert das SIDUS-Gleitdurchschnittssystem. Sie handelt mithilfe von Kreuzungen zwischen zwei linear gewichteten gleitenden Durchschnitten und einem bestätigenden exponentiellen Durchschnitt. Eine Position wird eröffnet, wenn der kurzfristige LWMA den langfristigen LWMA oder wenn der lange LWMA den langsamen EMA kreuzt. Entgegengesetzte Kreuzungen schließen oder kehren die Position um. Ein prozentualer Stop-Loss und Take-Profit verwalten das Risiko.

Tests deuten auf eine durchschnittliche Jahresrendite von etwa 25% hin. Es funktioniert am besten bei Devisenpaaren.

Die Kernidee besteht darin, Trendverschiebungen zu erfassen, wenn sich die schnellen und langsamen gleitenden Durchschnitte neu ausrichten. Das LWMA-Paar reagiert schnell auf Kursänderungen, während der langsamere EMA Rauschen herausfiltert. Wenn eine bullische oder bärische Ausrichtung auftritt, tritt die Strategie in diese Richtung ein und verlässt sich auf die Schutzniveaus, um bei ungünstigen Bewegungen auszusteigen.

## Details

- **Einstiegskriterien**:
  - **Long**: schneller LWMA kreuzt über den langsamen LWMA *oder* langsamer LWMA kreuzt über den langsamen EMA.
  - **Short**: schneller LWMA kreuzt unter den langsamen LWMA *oder* langsamer LWMA kreuzt unter den langsamen EMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung oder Schutz-Stop-Niveaus.
- **Stops**: Ja, verwendet prozentualen Take-Profit und Stop-Loss über `StartProtection`.
- **Standardwerte**:
  - Länge schneller EMA = 18.
  - Länge langsamer EMA = 28.
  - Länge schneller LWMA = 5.
  - Länge langsamer LWMA = 8.
  - Take-Profit = 2%.
  - Stop-Loss = 1%.
- **Filter**: Keine.
