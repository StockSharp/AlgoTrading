# Universelle Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Kernidee des originalen MQL4-Skripts `cm_universal_trailing_stop.mq4`. Sie generiert keine Einstiegssignale, sondern verwaltet eine bestehende Position, indem sie den Stop-Loss in Richtung des Gewinns verschiebt.

Der Algorithmus hält einen Abstand vom aktuellen Preis und verschiebt den Stop jedes Mal, wenn sich der Markt um einen konfigurierbaren Schritt bewegt. Sobald die minimale Gewinnschwelle erreicht ist, wird der Trailing-Stop aktiv und folgt dem Preis automatisch für Long- und Short-Positionen.

## Details

- **Einstiegskriterien**: keine. Die Position sollte manuell oder durch eine andere Strategie eröffnet werden.
- **Long/Short**: beide.
- **Ausstiegskriterien**: Stop-Order ausgelöst, wenn der Preis um den konfigurierten Abstand zurückläuft.
- **Stops**: Trailing-Stop basierend auf Punkten.
- **Parameter**:
  - `Delta` – Abstand vom Preis zum Stop in Punkten.
  - `Step` – minimale Preisbewegung in Punkten zum Verschieben des Stops.
  - `StartProfit` – Gewinn in Punkten, der zum Aktivieren des Trailings erforderlich ist.
  - `CandleType` – Zeitrahmen für die Trailing-Berechnungen.
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Trailing
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
