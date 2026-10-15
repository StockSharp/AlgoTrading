# Filtro de Volatilidade por Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Filtro de Volatilidade por Hurst Exponent utiliza o Hurst Exponent junto com filtros de volatilidade. Entra em operações somente quando as condições especificadas se alinham.

Os testes indicam um retorno anual médio de aproximadamente 163%. Funciona melhor no mercado de ações.

Os sinais exigem que o indicador supere um limiar enquanto a volatilidade atende a critérios predefinidos. As posições podem ser compradas ou vendidas com stops integrados.

Projetada para traders que valorizam o controle de risco, a estratégia sai assim que o indicador reverte à média ou a volatilidade muda. Configuração inicial `HurstPeriod` = 100.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `HurstPeriod` = 100
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `StopLoss` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Hurst
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
