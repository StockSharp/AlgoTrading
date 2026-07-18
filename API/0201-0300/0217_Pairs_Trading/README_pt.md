# Estratégia Pairs
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia de pairs trading monitora o spread de preços entre dois instrumentos correlacionados. Ao comparar o spread com sua média histórica e desvio padrão, o sistema tenta explorar divergências temporárias que eventualmente revertam.

Os testes indicam um retorno anual médio de aproximadamente 88%. Funciona melhor no mercado de ações.

Um spread comprado é aberto quando o spread cai abaixo de sua média por mais do que o multiplicador de desvio especificado. Isso significa comprar o primeiro ativo e vender o segundo. Um spread vendido faz o oposto quando o spread sobe acima da média pelo mesmo valor. As posições são fechadas assim que o spread retorna ao nível médio.

O pairs trading atrai traders neutros ao mercado que preferem oportunidades de valor relativo em vez de direção pura. Como ambas as pontas são cobertas, a volatilidade tende a ser menor, embora a estratégia ainda use um stop-loss no spread para gerenciar o risco.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Spread < Mean - Multiplier * StdDev
  - **Vendido**: Spread > Mean + Multiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o spread reverter à média
  - **Vendido**: Sair quando o spread reverter à média
- **Stops**: Sim, stop percentual baseado no valor do spread.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Arbitragem
  - Direção: Ambos
  - Indicadores: Spread statistics
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

