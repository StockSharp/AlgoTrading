# Strategy Tester Beispiel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel veranschaulicht, wie Momentum und Trendstärke kombiniert werden können,
um ein einfaches diskretionäres System zu bilden. Eine lineare Regressionsteigung misst
das kurzfristige Momentum, während der Average Directional Index die Persistenz einer
Bewegung bewertet. Zwei unabhängige Regeln lösen Einstiege aus: ein Momentum-Pivot
begleitet von einem ADX-Rückgang, oder ein neues ADX-Hoch mit Momentum, das sich aus
negativen Werten nach oben dreht.

Die Strategie ist bewusst einfach gehalten und konzentriert sich auf Long-Positionen.
Sie dient als Vorlage zum Testen von Ideen wie ATR-basierten Risikoniveaus und optionalen
Ausstiegskontrollen. Entwickler können die Ausstiegslogik erweitern oder Stop-Loss-
Handling hinzufügen, um daraus ein vollständiges Trading-Modell zu machen.

## Details

- **Einstiegskriterien**:
  - Momentum-Pivot-Hoch und ADX im Rückgang.
  - ADX-Pivot-Hoch mit Momentum, das von unterhalb null ansteigt.
- **Long/Short**: Standardmäßig nur Long.
- **Ausstiegskriterien**:
  - Momentum-Pivot-Hoch (wenn Momentum-Ausstieg aktiviert ist).
  - Platzhalter für benutzerdefinierten Strategieausstieg.
- **Stops**: Keine; ATR-Werte sind für externe Verwendung verfügbar.
- **Standardwerte**:
  - Momentum-Länge = 20, DI-Länge = 14.
  - ADX-Schlüsselniveau = 25, ATR-Länge = 14.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Lineare Regression, ADX, ATR
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Kurz/mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja (Momentum-Pivots)
  - Risikolevel: Mittel
