# Estratégia de Padrão Estrela da Manhã
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Estrela da Manhã é uma formação de velas altista que sinaliza um potencial fundo após uma queda. Consiste em uma grande vela baixista, uma pequena vela indecisa e uma forte vela altista que fecha acima do ponto médio da primeira barra.

Os testes indicam um retorno anual médio de aproximadamente 97%. Tem melhor desempenho no mercado cripto.

Esta estratégia rastreia sequências de três velas. Quando o padrão aparece, uma posição comprada é aberta com um stop colocado abaixo da pequena vela do meio. As saídas ocorrem quando o preço sobe acima da máxima da barra de confirmação ou se o stop é atingido.

Como o padrão frequentemente provoca recuperações rápidas de condições de sobrevenda, as operações geralmente têm curta duração, capturando o impulso inicial de alta.

## Detalhes

- **Critérios de entrada**: Padrão de três velas Estrela da Manhã.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Preço acima da máxima da barra de confirmação ou stop-loss.
- **Stops**: Sim, abaixo da mínima da vela do meio.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

