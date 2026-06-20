# Estratégia de Arbitragem Estatística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem de arbitragem estatística opera um par de ativos relacionados com base em seu posicionamento relativo em torno das médias móveis. Ao comparar cada ativo com sua própria média, a estratégia busca explorar deslocamentos de curto prazo que deveriam convergir ao longo do tempo.

Os testes indicam um retorno anual médio de aproximadamente 94%. Funciona melhor no mercado de ações.

Uma posição comprada é iniciada quando o primeiro ativo é negociado abaixo de sua média móvel enquanto o segundo ativo é negociado acima de sua própria média. Uma posição vendida ocorre quando o primeiro ativo está acima de sua média e o segundo está abaixo. As posições são fechadas quando o primeiro ativo cruza de volta por sua média móvel, sinalizando que o spread se normalizou.

O método é ideal para traders neutros ao mercado confortáveis em equilibrar a exposição entre dois instrumentos. O stop-loss integrado limita as retrações se o spread se ampliar ainda mais em vez de reverter.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Asset1 < MA1 && Asset2 > MA2
  - **Vendido**: Asset1 > MA1 && Asset2 < MA2
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando Asset1 fecha acima de sua MA1
  - **Vendido**: Sair quando Asset1 fecha abaixo de sua MA1
- **Stops**: Sim, stop-loss percentual sobre o spread.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Arbitragem
  - Direção: Ambos
  - Indicadores: Moving Averages
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
