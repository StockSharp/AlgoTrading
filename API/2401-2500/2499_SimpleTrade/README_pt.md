# Estratégia SimpleTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do consultor especializado MetaTrader 5 "SimpleTrade (edição de barabashkakvn)". Compara o preço de abertura da barra atual com o preço de abertura três barras atrás. Se a abertura atual for maior, a estratégia vai comprada; caso contrário vai vendida. Cada posição é mantida por apenas uma vela completada e é protegida com uma distância de stop-loss fixo expressa em pips.

A implementação no StockSharp assina a série de velas selecionada através da API de alto nível e reage apenas às barras terminadas, garantindo que as decisões sejam baseadas em dados de preço completos. As posições são fechadas na próxima transição de barra ou antes se o nível de stop for tocado dentro do intervalo da barra.

## Lógica de trading
- **Entrada**
  - Em cada barra completada, armazenar seu preço de abertura e manter um histórico deslizante das últimas quatro aberturas.
  - Quando não há posição aberta e há pelo menos quatro preços de abertura disponíveis, comparar a abertura mais recente com a registrada três barras atrás.
  - Entrar comprado se a abertura atual estiver acima da abertura de três barras atrás; caso contrário entrar vendido.
- **Saída**
  - Cada operação é protegida por um nível de stop calculado como *StopLossPips × tamanho do pip* desde o preço de abertura de entrada.
  - Na barra seguinte a posição é fechada independentemente do resultado, replicando o consultor especializado original que nunca mantém uma operação por mais de uma vela.
  - Se o máximo da barra (para vendidas) ou o mínimo (para compradas) penetrar o nível de stop, a estratégia tenta fechar a posição imediatamente a mercado.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `StopLossPips` | 120 | Distância do preço de abertura de entrada ao stop protetor, medida em pips. O código reproduz o comportamento do MetaTrader multiplicando o passo de preço por 10 para símbolos cotados com 3 ou 5 decimais. |
| `TradeVolume` | 1 | Volume de ordem usado para entradas de mercado. Ajuste para alinhar com o tamanho do contrato do instrumento negociado. |
| `CandleType` | Período de 1 hora | Especifica qual série de velas a estratégia assina. Selecione o período que corresponde ao gráfico usado no MetaTrader. |

Todos os parâmetros são expostos como objetos `StrategyParam<T>` para que possam ser otimizados ou alterados através da interface gráfica.

## Notas de implementação
- O histórico deslizante de quatro preços de abertura é mantido sem coleções para cumprir as diretrizes do repositório.
- Os stops não são enviados como ordens separadas; em vez disso, a lógica verifica os intervalos das velas e emite uma saída a mercado quando o nível de stop teria sido acionado.
- Como o StockSharp processa posições de forma assíncrona, a estratégia sai de uma operação existente antes de avaliar um novo sinal de entrada. No trading ao vivo, isso reflete a sequência original de "fechar e reabrir" enquanto evita ordens sobrepostas.
- O tamanho do pip é derivado de `Security.PriceStep`. Para símbolos de 5 ou 3 dígitos, o passo é multiplicado por dez para que um pip corresponda à definição do MetaTrader.

## Dicas de uso
- Execute a estratégia em instrumentos com tamanhos de tick consistentes onde stops baseados em pips sejam significativos (por exemplo, pares de Forex principais).
- Otimize o valor de `StopLossPips` por instrumento; valores grandes ampliam o buffer protetor, enquanto valores menores tornam a estratégia mais sensível ao ruído intrabarra.
- Garanta que a conexão com o corretor envie atualizações de velas com estados finais para que a estratégia receba os preços de abertura corretos.

## Riscos e limitações
- Manter operações por apenas uma barra significa que a estratégia depende muito do período escolhido. É essencial fazer backtesting com diferentes durações de velas.
- Usar os extremos da vela para emular a execução de stops introduz slippage em mercados voláteis comparado às ordens stop nativas.
- A estratégia permanece sempre no mercado (comprada ou vendida) após as primeiras quatro barras de dados, o que pode gerar operações frequentes em mercados laterais.
