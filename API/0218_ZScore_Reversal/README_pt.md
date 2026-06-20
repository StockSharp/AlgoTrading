# Estratégia de Reversão Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão Z-Score mede o quanto o preço se desvia de uma média móvel em termos de desvios padrão. O Z-Score resultante destaca condições estatisticamente estendidas que podem se reverter em direção à média.

Os testes indicam um retorno anual médio de aproximadamente 91%. Funciona melhor no mercado de ações.

Uma operação comprada é aberta quando o Z-Score cai abaixo de um limiar negativo, sinalizando um mercado sobrevendido. Uma operação vendida é realizada quando o Z-Score sobe acima do limiar positivo. A posição é fechada assim que o Z-Score cruza de volta por zero, indicando que o preço se normalizou.

Esta técnica é atraente para traders de reversão à média que preferem níveis de entrada objetivos. O percentual de stop-loss mantém os movimentos adversos gerenciáveis enquanto se aguarda a reversão.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Z-Score < -Limiar
  - **Vendido**: Z-Score > Limiar
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o Z-Score cruza acima de 0
  - **Vendido**: Sair quando o Z-Score cruza abaixo de 0
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(10)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Z-Score
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
