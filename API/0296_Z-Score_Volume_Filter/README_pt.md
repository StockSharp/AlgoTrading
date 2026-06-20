# Z-Score Estratégia com Filtro de Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia Z-Score com Filtro de Volume utiliza o Z-Score junto com filtros de volatilidade. Entra em operações apenas quando as condições especificadas se alinham.

Os sinais exigem que o indicador supere um limiar enquanto a volatilidade atende a critérios predefinidos. As posições podem ser compradas ou vendidas com stops integrados.

Projetada para traders que valorizam o controle de risco, a estratégia sai assim que o indicador reverte à média ou a volatilidade muda. Configuração inicial `LookbackPeriod` = 20.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Z-Score
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
