# Roulette-Spiel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Roulette-Spielstrategie bildet den Casino-ähnlichen Expertenberater von MetaTrader in StockSharp nach. Es behandelt jede fertige Kerze als eine neue Drehung des Rades, wählt eine zufällige Richtung und skaliert seine Ordergröße nach Verlusten mithilfe einer Progression im Martingale-Stil. Die Implementierung verfolgt eine virtuelle Bankroll und begrenzt das Risiko durch konfigurierbare Obergrenzen.

Jede Runde beginnt mit dem Abflachen einer vorhandenen Position, dem Werfen einer virtuellen Münze, die Rot oder Schwarz darstellt, und dem Senden einer Marktorder in die ausgewählte Richtung. Wenn die nächste Kerze schließt, prüft die Strategie, ob sich der Schlusskurs zugunsten der Wette bewegt hat. Bei Gewinnen wird der Einsatz auf das Basisvolumen zurückgesetzt, bei Verlusten vervielfacht sich der Einsatz bis zu einer definierten Obergrenze. Ein maximaler Pechsträhneschutz erzwingt ein Zurücksetzen, bevor die Gefährdung extrem wird. Zwischen den Runden können optional Abklingkerzen eingefügt werden, um das Wetttempo zu verlangsamen.

Diese Konvertierung konzentriert sich auf das vom ursprünglichen Experten vorgestellte, vom Glücksspiel inspirierte Geldmanagement anstelle von Indikatorsignalen. Es zeigt, wie man zeitbasierte Runden orchestriert, den internen Status aufrechterhält und über Kerzenabonnements mit StockSharps übergeordnetem API interagiert.

## Einzelheiten

- **Eintrittskriterien**: Kein technischer Filter. Die Richtung wird am Ende einer fertigen Kerze zufällig ausgewählt.
- **Lang/Kurz**: Beide Richtungen, in jeder Runde zufällig ausgewählt.
- **Ausstiegskriterien**: Die Position wird bei der nächsten abgeschlossenen Kerze geschlossen, wobei bewertet wird, ob der Preis über oder unter dem Einstiegspunkt schloss.
- **Haltestellen**: Keine herkömmlichen Haltestellen. Das Risiko wird durch Einsatzobergrenzen und Streak-Limits gesteuert.
- **Standardwerte**:
  - `BaseVolume` = 1m
  - `LossMultiplier` = 2m
  - `MaxMultiplier` = 16m
  - `RoundCooldown` = 1
  - `MaxLosingStreak` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Geldmanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stopps: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikostufe: Hoch

## Notizen

- Die Größe der Market Orders richtet sich nach dem multiplikatorbereinigten Einsatz und wird auf die Volumenstufe des Instruments gerundet.
- Bei Gewinnen wird der Einsatz auf das Basisvolumen zurückgesetzt; Verluste skalieren mit dem Multiplikator, bis der maximale Multiplikator oder das Maximum für die Pechsträhne erreicht ist.
- Abklingbalken verhindern den sofortigen Wiedereintritt und ermöglichen die Synchronisierung mit langsameren Datenfeeds.
