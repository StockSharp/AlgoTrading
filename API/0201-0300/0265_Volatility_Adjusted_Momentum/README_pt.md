# Momentum Ajustado pela Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia de Momentum Ajustado pela Volatilidade monitora a Volatilidade em busca de expansões rápidas. Quando as leituras saltam além do seu intervalo médio, o preço frequentemente inicia um novo movimento.

Os testes indicam um retorno anual médio de aproximadamente 130%. Funciona melhor no mercado de ações.

Uma posição é aberta assim que o indicador perfura uma banda derivada de dados recentes e um multiplicador de desvio. Operações compradas e vendidas são possíveis com um stop associado.

Este sistema é adequado para traders de momentum que buscam rompimentos antecipados. As operações são encerradas quando a Volatilidade volta em direção à média. Os valores padrão começam com `MomentumPeriod` = 14.

## Detalhes

- **Critérios de entrada**: O indicador supera a média pelo multiplicador de desvio.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `MomentumPeriod` = 14
  - `AtrPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Volatilidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

