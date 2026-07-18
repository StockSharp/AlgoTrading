# Arbitragem Spot-Futuros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Arbitra a diferença de preço entre um ativo spot e seu contrato de futuros.
Entra comprado em spot/vendido em futuros quando o futuro negocia acima do spot por um limiar, e o oposto quando abaixo.
Os limiares podem ser dinâmicos com base na média e desvio padrão do spread, e as operações são fechadas quando o spread reverte ou após um tempo máximo de manutenção.

## Parâmetros
- **Spot** — ativo spot.
- **Future** — ativo de futuros.
- **CandleType** — período do candle.
- **MinSpreadPct** — percentual mínimo de spread para entrar.
- **LookbackPeriod** — período para estatísticas do spread.
- **AdaptiveThreshold** — ativar limiares dinâmicos.
- **MaxHoldHours** — tempo máximo de manutenção da posição em horas.
