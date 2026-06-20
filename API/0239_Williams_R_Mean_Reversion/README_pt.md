# Estratégia de Reversão à Média com Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Williams %R oscila entre 0 e -100 para mostrar quando o preço fecha perto dos extremos de seu intervalo recente. Esta estratégia opera contra esses extremos uma vez que o indicador se afasta muito de sua própria média.

Os testes indicam um retorno anual médio de aproximadamente 154%. Funciona melhor no mercado de ações.

Uma operação comprada é ativada quando Williams %R cai abaixo da média menos `DeviationMultiplier` vezes o desvio padrão. Uma operação vendida é tomada quando sobe acima da média mais esse multiplicador. As saídas ocorrem quando Williams %R volta em direção ao seu nível médio.

A abordagem é adequada para traders que dependem do esgotamento do momentum para temporizar entradas. Um stop-loss protetor limita o risco se o preço continuar se movendo para novos extremos.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %R < Avg - DeviationMultiplier * StdDev
  - **Vendido**: %R > Avg + DeviationMultiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando %R > Avg
  - **Vendido**: Sair quando %R < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `WilliamsRPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: Williams %R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

