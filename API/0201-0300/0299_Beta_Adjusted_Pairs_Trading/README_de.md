# Beta Adjusted Pairs Trading

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Beta-neutrale Mittelwertrückkehr mit zwei Instrumenten. Ein anderes `Asset2` und ein explizites `Asset2Portfolio` sind erforderlich; das Portfolio darf identisch sein. Kein implizites TON oder Konto; beide Betas positiv. Nur abgeschlossene Kerzen mit exakt gleichem `OpenTime` werden verbunden; fehlende, alte oder doppelte Daten mischen keine Zeitpunkte.

## Handelsregeln

`h=BetaAsset1/BetaAsset2`, `B1,B2` sind die ersten passenden positiven Schlusskurse. Spread `s=ln(P1/B1)-h*ln(P2/B2)`. Mittelwert und Populationsabweichung der letzten `LookbackPeriod` Spreads einschließlich des aktuellen ergeben `z=(s-mean)/deviation`. Nullabweichung sperrt Einstieg. Long: `z[p]<=-EntryThreshold && -EntryThreshold<z<0`; Short: `z[p]>=EntryThreshold && 0<z<EntryThreshold`. Nur jenseits der Schwelle zu bleiben genügt nicht.

Long kauft `Volume` des ersten Instruments und verkauft `q2=floor(Volume*P1*h/P2/VolumeStep2)*VolumeStep2` des zweiten; Short kehrt beide Seiten um. Kurse stammen aus passenden Signalkerzen; Hedge-Lotgrenzen werden geprüft, die Menge bleibt während der Position konstant. Einstieg nur bei beiden Positionen null, ohne ausstehende Orders und mit strikt mehr als `CooldownBars` Abstand der Einstiegsindizes.

Je abgeschlossenem Kerzenpaar gelten tatsächliche gewichtete Einstiegskurse `F1,F2` und vorzeichenbehaftete Mengen `q1,q2`: `PnL=q1*(P1-F1)+q2*(P2-F2)`, `G=abs(q1)*F1+abs(q2)*F2`. Beide tatsächlichen Positionen schließen bei `PnL<=-G*StopLoss/100`, sonst Long bei `z>=-ExitThreshold`, Short bei `z<=ExitThreshold`. Verlust hat Vorrang; Einstiegspause verzögert keinen Ausstieg. Verlustlimit auf Paarschlusskursen vor Gebühren, kein Intrabar-Börsenstop.

Beine sind separate Marktorders, nicht atomar. Ausstehende Orders blockieren Entscheidungen. Nach Abschluss bewirkt ein fehlendes, abgelehntes oder unvollständiges Bein die Glattstellung tatsächlicher Restpositionen beim nächsten passenden Kerzenpaar; fehlgeschlagene Abschlüsse werden dort erneut versucht. BTC/TON-Archiv prüft Mechanik, nicht Rentabilität oder stabile ökonomische Beziehung.

Einstiege verwenden nur abgeschlossene Kerzen und vollständig gebildete Indikatoren. Offene Positionen werden bis zum Ausstieg verwaltet; ausstehende Marktorders verhindern doppelte Aufträge.

## Parameter

| Parameter | Default |
|---|---|
| `Asset2` | `required` |
| `Asset2Portfolio` | `required` |
| `BetaAsset1` | `1` |
| `BetaAsset2` | `1` |
| `LookbackPeriod` | `20` |
| `EntryThreshold` | `2` |
| `ExitThreshold` | `0.15` |
| `StopLoss` | `2 (%)` |
| `CooldownBars` | `120` |
| `CandleType` | `5m` |
