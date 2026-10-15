# Genie RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Überkauf- und Überverkaufs-Umkehrungen mit dem Relative Strength Index (RSI). Wenn der RSI über 80 steigt, eröffnet die Strategie eine Short-Position; wenn der RSI unter 20 fällt, eröffnet sie eine Long-Position. Optionale Take-Profit- und Trailing-Stop-Niveaus steuern das Risiko nach dem Einstieg.

Die Strategie ist für schwingende Märkte konzipiert, bei denen sich der Preis häufig zwischen Unterstützung und Widerstand bewegt. Sie funktioniert auf jedem Zeitrahmen, der durch den Parameter `CandleType` definiert wird.

## Details

- **Einstiegskriterien**  
  - **Long**: RSI-Wert fällt bei einer abgeschlossenen Kerze unter 20 und es ist keine Position offen.  
  - **Short**: RSI-Wert steigt bei einer abgeschlossenen Kerze über 80 und es ist keine Position offen.
- **Ausstiegskriterien**  
  - **Long**: RSI steigt über 80, Preis erreicht die Take-Profit-Distanz, oder Preis berührt das Trailing-Stop-Niveau.  
  - **Short**: RSI fällt unter 20, Preis erreicht die Take-Profit-Distanz, oder Preis berührt das Trailing-Stop-Niveau.
- **Indikatoren**: RSI.
- **Parameter**:  
  - `RSI Period` – Länge des RSI-Indikators.  
  - `Take Profit` – Abstand in Preiseinheiten für das Gewinnziel.  
  - `Trailing Stop` – Abstand in Preiseinheiten für den Trailing-Stop.  
  - `Candle Type` – Zeitrahmen der verarbeiteten Kerzen.
- **Positionsverwaltung**: Verwendet Marktorders für Ein- und Ausstiege. Der Trailing-Stop wird bei jeder abgeschlossenen Kerze neu berechnet.
