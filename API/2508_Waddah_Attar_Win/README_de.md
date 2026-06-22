# Waddah Attar Win-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie spiegelt den originalen Waddah Attar Win Expert Advisor. Sie pflegt kontinuierlich ein symmetrisches Gitter aus Kauf- und Verkaufslimitorders, die einen festen Punkteabstand vom aktuellen Bid/Ask haben. Wann immer der Marktpreis sich der zuletzt eingereichten Order nähert, stapelt die Strategie ein neues Limit in derselben Distanz mit einem optionalen Volumeninkrement. Der schwebende Gewinn wird bei jeder Orderbuch-Aktualisierung überwacht, und alle Positionen zusammen mit ausstehenden Orders werden geschlossen, sobald das konfigurierte Gewinnziel in der Kontowährung erreicht ist.

## Details

- **Einstiegskriterien**:
  - Initiales Kauf-Limit `Step Points` unterhalb des Bid und Verkaufs-Limit in derselben Distanz oberhalb des Ask platziert.
  - Zusätzliche ausstehende Orders werden hinzugefügt, wenn der Preis innerhalb von fünf Preisschritten der letzten Order auf jeder Seite liegt.
- **Long/Short**: Beide, gehedgtes Gitter.
- **Ausstiegskriterien**:
  - Alle Positionen schließen und Orders stornieren, sobald das Eigenkapital den gespeicherten Saldo um `Min Profit` überschreitet.
- **Stops**: Keine.
- **Standardwerte**:
  - `Step Points` = 20
  - `First Volume` = 0.1
  - `Increment Volume` = 0.0
  - `Min Profit` = 910
- **Hinweise**:
  - Funktioniert mit Hedging-Portfolios, da Long- und Short-Positionen gleichzeitig bestehen können.
  - Verwendet Orderbuchdaten, um sofort auf Bid/Ask-Änderungen zu reagieren.
