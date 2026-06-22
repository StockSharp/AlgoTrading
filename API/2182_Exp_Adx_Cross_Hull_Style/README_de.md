# Exp ADX Cross Hull Style-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Average Directional Index (ADX)-Kreuzungssignale mit einem Hull Moving Average (HMA)-Filter. Einstiege erfolgen, wenn die +DI-Linie über die -DI-Linie für Long-Positionen oder darunter für Short-Positionen kreuzt. Ausstiege werden durch ein Paar Hull-Gleitender Durchschnitte gesteuert: der schnelle Durchschnitt kreuzt den langsamen und schließt damit Positionen. Die Strategie arbeitet standardmäßig auf dem 4-Stunden-Zeitrahmen.

## Details
- **Einstiegskriterien**  
  - **Long**: +DI kreuzt über -DI.  
  - **Short**: -DI kreuzt über +DI.
- **Ausstiegskriterien**  
  - **Long**: schnelle HMA fällt unter langsame HMA.  
  - **Short**: schnelle HMA steigt über langsame HMA.
- **Indikatoren**  
  - AverageDirectionalIndex (Periode 14).  
  - HullMovingAverage schnelle Länge 20.  
  - HullMovingAverage langsame Länge 50.
- **Zeitrahmen**: 4-Stunden-Kerzen (konfigurierbar).
- **Stops**: standardmäßig keine.
- **Richtung**: Long und Short.

Die Strategie basiert nicht auf historischen Sammlungen; sie reagiert auf Echtzeit-Kerzendaten. Parameter können für verschiedene Märkte optimiert werden. Die Chartausgabe zeigt Preiskerzen mit beiden Hull-Gleitenden Durchschnitten und Handelsmarkierungen.
