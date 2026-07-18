# Estratégia de Trading de Pares Ajustada por Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Trading de Pares Ajustada por Beta utiliza o Beta junto com filtros de volatilidade. Entra em operações somente quando as condições especificadas se alinham.

Os sinais exigem que o indicador supere um limiar enquanto a volatilidade atende a critérios predefinidos. As posições podem ser compradas ou vendidas com stops integrados.

Projetada para traders que valorizam o controle de risco, a estratégia sai assim que o indicador reverte à média ou a volatilidade muda. Configuração inicial `Asset2` = (Security.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `Asset2` = (Security
  - `Asset2Portfolio` = (Portfolio
  - `BetaAsset1` = 1.0m
  - `BetaAsset2` = 1.0m
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2.0m
  - `StopLoss` = 2.0m
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Beta
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
