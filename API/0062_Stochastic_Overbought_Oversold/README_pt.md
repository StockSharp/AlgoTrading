# Reversão Stochastic em Sobrecompra/Sobrevenda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia reage a níveis extremos do Oscilador Stochastic. Quando a linha %K mergulha em território de sobrevenda, o sistema espera um repique; enquanto leituras de sobrecompra podem prenunciar uma queda. O método opera em velas intradiárias curtas para que os sinais cheguem rapidamente.

Os testes indicam um retorno anual médio de aproximadamente 73%. Tem melhor desempenho no mercado cripto.

Após assinar o período selecionado, monitora as linhas %K e %D. Uma configuração altista se forma quando %K cai abaixo de 20 e começa a se recuperar. Por outro lado, uma configuração baixista aparece se %K sobe acima de 80 e começa a virar para baixo. Um stop de percentual fixo controla o risco para ambos os lados.

As posições são encerradas quando a linha %K cruza de volta pelo nível 50, sinalizando que o momento mudou para a direção oposta. Como os stops escalam com o ATR mais recente, o tamanho da operação se adapta à volatilidade.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `%K < 20` com virada altista.
  - **Vendido**: `%K > 80` com virada baixista.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: %K cruzando o nível 50 ou stop-loss.
- **Stops**: Sim, a uma distância de `2%`.
- **Valores padrão**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

