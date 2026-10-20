# Beta Adjusted Pairs Trading

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversión a la media neutral a beta con dos instrumentos. Deben indicarse `Asset2`, distinto, y `Asset2Portfolio` explícito, que puede ser el principal. Sin TON ni cuenta implícitos; ambas betas positivas. Solo se combinan velas terminadas con `OpenTime` idéntico; faltantes, antiguas o duplicadas no mezclan tiempos.

## Reglas de negociación

`h=BetaAsset1/BetaAsset2`; `B1,B2` son los primeros cierres positivos emparejados. Diferencial `s=ln(P1/B1)-h*ln(P2/B2)`. Media y desviación poblacional de los últimos `LookbackPeriod` diferenciales, incluido el actual: `z=(s-mean)/deviation`. Desviación cero impide entrada. Largo: `z[p]<=-EntryThreshold && -EntryThreshold<z<0`; corto: `z[p]>=EntryThreshold && 0<z<EntryThreshold`. Permanecer fuera del umbral no basta.

Largo compra `Volume` del principal y vende `q2=floor(Volume*P1*h/P2/VolumeStep2)*VolumeStep2` del segundo; corto invierte ambos. Precios de las velas de señal emparejadas; el hedge respeta límites de lote y conserva su cantidad. Solo se entra con ambas posiciones planas, sin órdenes pendientes y separación de índices de entrada estrictamente mayor que `CooldownBars`.

En cada par terminado, precios medios ejecutados `F1,F2` y cantidades firmadas `q1,q2`: `PnL=q1*(P1-F1)+q2*(P2-F2)`, `G=abs(q1)*F1+abs(q2)*F2`. Cerrar ambas posiciones reales si `PnL<=-G*StopLoss/100`; si no, largo sale con `z>=-ExitThreshold`, corto con `z<=ExitThreshold`. Pérdida tiene prioridad; espera de entrada no retrasa salidas. Límite de pérdida del par al cierre, antes de comisiones, no stop intravela de bolsa.

Son órdenes de mercado separadas, no atómicas. Las pendientes bloquean decisiones. Tras finalizar, una pata ausente, rechazada o incompleta hace cerrar los residuos reales en el próximo par coincidente; se reintentan cierres fallidos en los siguientes. BTC/TON archivado comprueba mecánica, no rentabilidad ni relación económica estable.

Las entradas requieren velas terminadas e indicadores preparados. La posición se gestiona hasta su salida; las órdenes de mercado pendientes impiden envíos duplicados.

## Parámetros

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
