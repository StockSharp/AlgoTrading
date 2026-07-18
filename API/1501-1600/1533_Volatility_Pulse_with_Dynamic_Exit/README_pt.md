# Estratégia de Pulso de Volatilidade com Saída Dinâmica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em momentum que detecta expansão de volatilidade. Entra na direção do momentum quando o ATR sobe acima da sua média e sai usando stop e take profit baseados em ATR após um período de manutenção.

## Detalhes

- **Critérios de entrada**: Expansão de volatilidade ATR com confirmação de momentum
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss e take profit definidos após o período de manutenção
- **Stops**: Stop baseado em ATR, take profit pela razão risco-recompensa
- **Valores padrão**:
  - `AtrLength` = 14
  - `MomentumLength` = 20
  - `VolThreshold` = 0.5
  - `MinVolatility` = 1.0
  - `ExitBars` = 42
  - `RiskReward` = 2
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: ATR, SMA, Momentum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
