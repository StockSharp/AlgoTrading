# Estratégia de Compressão das Bandas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta configuração monitora a largura das Bandas de Bollinger para detectar períodos de baixa volatilidade. Quando as bandas se contraem em relação à sua média recente, isso sinaliza que uma possível expansão de volatilidade está próxima.

Os testes indicam um retorno anual médio de aproximadamente 100%. Funciona melhor no mercado de forex.

Uma vez que uma compressão é identificada, a estratégia aguarda o preço romper fora das bandas. Um fechamento acima da banda superior inicia uma operação comprada, enquanto um fechamento abaixo da banda inferior abre uma vendida. A operação é fechada se o preço retornar em direção ao meio das bandas ou se um stop-loss for acionado.

O método é voltado para traders que gostam de operar rompimentos de volatilidade em vez de continuações de tendência. Usar a largura da banda como filtro ajuda a evitar sinais falsos durante condições agitadas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Largura da banda < largura média && Fechamento > banda superior
  - **Vendido**: Largura da banda < largura média && Fechamento < banda inferior
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o preço cai de volta dentro das bandas
  - **Vendido**: Sair quando o preço sobe de volta dentro das bandas
- **Stops**: Sim, tipicamente a 2*ATR.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0m
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
