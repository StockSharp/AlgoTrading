# Waddah Attar Win Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Waddah Attar Win Grid Strategy** repliziert den MetaTrader 4 Expert Advisor aus dem `MQL/8210`-Skript. Es wird kontinuierlich eine symmetrische Leiter von Kauf- und Verkaufslimitaufträgen rund um das aktuelle Geld/Brief aufrecht erhalten. Wenn der Preis in Richtung der aktuellsten Rasterebene driftet, stapelt die Strategie automatisch eine neue ausstehende Order einen Schritt weiter entfernt und erhöht optional das Volumen jeder zusätzlichen Order. Der variable Gewinn wird bei jeder Auftragsbuchaktualisierung überwacht und sobald der konfigurierte Eigenkapitalgewinn erreicht ist, werden alle Positionen und Arbeitsaufträge gleichzeitig geschlossen.

## Wie es funktioniert

1. **Initialisierung**
   - Abonniert Orderbuchaktualisierungen, um sofort auf Geld-/Briefänderungen reagieren zu können.
   - Zeichnet den aktuellen Portfoliowert auf, der als Basis-Aktienreferenz verwendet werden soll.
   - Startet das integrierte Risikoschutz-Subsystem von StockSharp.
2. **Basismanagement**
   - Wenn keine aktiven Aufträge vorliegen und die Nettoposition unverändert ist, wird der aktuelle Portfoliowert zum neuen Referenzsaldo. Dies spiegelt den ursprünglichen Expert Advisor wider, der bei jedem Tick den aktuellen Kontostand speicherte.
3. **Erste Rasterplatzierung**
   - Sobald der Handel erlaubt ist und keine Orders aktiv sind, platziert die Strategie zwei ausstehende Orders:
     - Ein Kauflimit `Step Points` unter dem aktuellen Briefkurs.
     - Ein Verkaufslimit `Step Points` über dem aktuellen Gebotspreis.
   - Beide Bestellungen verwenden den Wert `First Volume`.
4. **Neue Bestellungen stapeln**
   - Wenn sich der Briefkurs innerhalb von fünf Preisschritten um das letzte Kauflimit bewegt, setzt die Strategie ein neues Kauflimit, das einen ganzen Schritt unter dem vorherigen Niveau liegt.
   - Wenn sich der Geldkurs innerhalb von fünf Preisschritten um das letzte Verkaufslimit bewegt, setzt die Strategie ein neues Verkaufslimit, das einen ganzen Schritt über dem vorherigen Niveau liegt.
   - Jede neue ausstehende Bestellung erhöht das Volumen um `Increment Volume` und ermöglicht auf Wunsch eine Pyramidenbildung im Martingal-Stil.
5. **Gewinnerfassung**
   - Der variable Gewinn wird als Differenz zwischen dem aktuellen Portfolio-Eigenkapital und dem hinterlegten Referenzsaldo berechnet.
   - Sobald dieser Gewinn `Min Profit` übersteigt, wird jede aktive Order storniert und alle offenen Positionen werden mit einem einzigen Aufruf von `CloseAll` reduziert.
   - Das Grundeigenkapital wird aktualisiert, sodass das Raster mit einer sauberen Weste neu gestartet werden kann.

## Strategiemerkmale

- **Marktdaten**: Funktioniert ausschließlich auf Orderbuch-Snapshots der Ebene 1 (bester Geld-/Briefkurs).
- **Order-Typen**: verwendet nur Limit-Orders; Es werden keine Stopps oder Markteintritte automatisch generiert.
- **Engagement**: Kann gleichzeitig Long- und Short-Positionen in absicherungsfähigen Portfolios halten.
- **Risikokontrolle**: es fehlen harte Stop-Losses; setzt auf das variable Gewinnziel und externe Risikoregeln.
- **Neueingabe**: Nach der Reduzierung oder manuellen Stornierung von Aufträgen wird das ursprüngliche Raster automatisch neu erstellt, wenn die Marktdatenschleife das nächste Mal ausgeführt wird.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Step Points` | `120` | Abstand zwischen aufeinanderfolgenden Rasterebenen, ausgedrückt in Preispunkten (Preisschritt-Vielfaches). |
| `First Volume` | `0.1` | Volumen, das für das allererste Paar ausstehender Aufträge verwendet wird. |
| `Increment Volume` | `0.0` | Jeder neu gestapelten Bestellung wird zusätzliches Volumen hinzugefügt. auf Null setzen, um alle Bestellungen gleich groß zu halten. |
| `Min Profit` | `450` | Variabler Gewinn (in Kontowährung), der zum Schließen aller offenen Positionen und ausstehenden Aufträge erforderlich ist. |

## Hinweise und Einschränkungen

- Stellen Sie sicher, dass `PriceStep` des Instruments richtig eingestellt ist; Die Strategie multipliziert `Step Points` mit `PriceStep`, um tatsächliche Preise abzuleiten.
- Da der Algorithmus Aufträge häufig storniert und ersetzt, sollten Broker- oder Börsenlimits für die Anzahl ausstehender Aufträge berücksichtigt werden.
- Es gibt keinen integrierten Drawdown-Schutz – erwägen Sie eine Kombination der Strategie mit externem Risikomanagement oder Stopps auf Portfolioebene.
- Das Netz kann bei starker Preisentwicklung unbegrenzt erweitert werden, ohne dass das Gewinnziel erreicht wird; Wählen Sie `Increment Volume` sorgfältig aus, um die Margin-Nutzung zu kontrollieren.

## Dateien

- `CS/WaddahAttarWinGridStrategy.cs` – C#-Implementierung der Handelslogik.
- `README.md` – diese Dokumentation (Englisch).
- `README_ru.md` – Russische Übersetzung mit identischem Inhalt.
- `README_zh.md` – Chinesische Übersetzung mit identischem Inhalt.
