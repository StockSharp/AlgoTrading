# Estratégia de Impulso de Day Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia DayTrading** é uma conversão C# fiel do clássico MetaTrader 4 consultor especialista "DayTrading" lançado pela NazFunds em 2005. O robô original foi projetado para gráficos Forex de 5 minutos e combina vários indicadores de impulso e acompanhamento de tendências para capturar movimentos direcionais de curto prazo com um alvo fixo modesto e um trailing stop opcional. Esta implementação StockSharp reproduz a lógica de decisão central enquanto expõe cada limite importante como um parâmetro de estratégia para que possa ser otimizado ou adaptado a diferentes instrumentos.

## Pilha de Indicadores

A estratégia avalia quatro indicadores na série de velas selecionada:

- **Parabolic SAR** (`ParabolicSar`) com aceleração, incremento e limite configuráveis. Ele define a direção da tendência da linha de base e precisa virar abaixo/acima do preço para permitir novas entradas.
- **MACD (12, 26, 9)** (`MovingAverageConvergenceDivergenceSignal`). A linha MACD deve estar abaixo da linha de sinal para posições compradas e acima dela para posições vendidas, refletindo a comparação original do histograma/sinal em MQL.
- **Stochastic Oscilador (5, 3, 3)** (`StochasticOscillator`). A linha %K deve permanecer abaixo de 35 para posições longas e acima de 60 para posições curtas para garantir que o mercado esteja saindo de uma zona de sobrevenda/sobrecompra.
- **Momentum (14)** (`Momentum`). Um valor abaixo de 100 desbloqueia negociações longas, enquanto um valor acima de 100 autoriza operações curtas, exatamente como no script MT4.

Todos os indicadores são processados por meio do pipeline `BindEx` de alto nível, portanto, nenhum gerenciamento manual de buffer ou indexação histórica é necessário.

## Regras de negociação

### Condições de Entrada

Uma posição **longa** é aberta quando todas as afirmações a seguir são verdadeiras na última vela finalizada:

1. O ponto Parabolic SAR é impresso no preço de venda atual ou abaixo dele **e** o ponto anterior estava acima do ponto atual (nova SAR mudança para alta).
2. O impulso está abaixo de 100.
3. A linha MACD está abaixo de sua linha de sinal.
4. Stochastic %K está abaixo de 35.

Uma posição **curta** é aberta quando as condições simétricas são satisfeitas:

1. O ponto Parabolic SAR é impresso no preço de oferta atual ou acima dele **e** o ponto anterior estava abaixo do ponto atual (inversão de baixa).
2. O impulso está acima de 100.
3. A linha MACD está acima de sua linha de sinal.
4. Stochastic %K está acima de 60.

Apenas uma posição pode ser aberta por vez. Sempre que um sinal oposto aparece, a posição existente é fechada e nenhuma reentrada acontece na mesma vela - assim como na implementação MetaTrader onde a varredura `OrdersTotal` impede a recarga imediata.

### Gerenciamento de saída

- **Stop Loss/Take Profit:** Distâncias fixas opcionais (em pontos) são convertidas em preços absolutos usando o tamanho do tick do instrumento. Eles são reavaliados em cada vela e fecham a posição se a intrabar for violada.
- **Trailing Stop:** Quando o preço avança pelo número de pontos configurado, um trailing stop é ativado. Para negociações longas, o stop fica abaixo do fechamento; para negociações curtas, ele fica acima do fechamento. O stop nunca recua, portanto o lucro é bloqueado progressivamente.
- **Sinal Oposto:** Uma configuração oposta válida liquida imediatamente a posição atual antes que qualquer nova entrada seja considerada.

Nenhuma lógica adicional de grade, escala ou cobertura é adicionada; a estratégia permanece tão leve e determinística quanto o EA original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `LotSize` | 1 | Volume de cada ordem de mercado. A propriedade `Strategy.Volume` é sincronizada com este valor durante a inicialização. |
| `TrailingStopPoints` | 15 | Distância final em pontos. Defina como zero para desativar o rastreamento. |
| `TakeProfitPoints` | 20 | Distância fixa de lucro em pontos. Defina como zero para remover o alvo. |
| `StopLossPoints` | 0 | Distância de parada protetora em pontos. Zero reproduz o comportamento original de "sem parada". |
| `SlippagePoints` | 3 | Espaço reservado para deslizamento máximo de execução (para compatibilidade com a entrada MT4). Não aplicado automaticamente, mas mantido para fins de integridade. |
| `CandleType` | Período de 5 minutos | Série de velas usada por todos os indicadores. Mantenha-se em M5 para corresponder à recomendação original do EA. |
| `MacdFastPeriod` | 12 | Comprimento EMA rápido no cálculo de MACD. |
| `MacdSlowPeriod` | 26 | Comprimento EMA lento no cálculo MACD. |
| `MacdSignalPeriod` | 9 | Comprimento do sinal EMA no cálculo MACD. |
| `StochasticLength` | 5 | %K comprimento de lookback para o oscilador Stochastic. |
| `StochasticSignal` | 3 | %D comprimento de suavização. |
| `StochasticSlow` | 3 | Desaceleração adicional aplicada à linha %K. |
| `MomentumPeriod` | 14 | Comprimento retrospectivo do momento. |
| `SarAcceleration` | 0,02 | Fator de aceleração inicial para Parabolic SAR. |
| `SarStep` | 0,02 | Incremento aplicado ao fator de aceleração após cada novo extremo. |
| `SarMaximum` | 0,2 | Fator de aceleração máximo para Parabolic SAR. |

Todos os parâmetros numéricos podem ser otimizados por meio do fluxo de trabalho de otimização do StockSharp graças às dicas do `SetCanOptimize(true)`.

## Notas de implementação

- Os preços de compra/venda são derivados de dados em tempo real do Nível 1, quando disponíveis; caso contrário, o fechamento da vela atua como um substituto para que a lógica permaneça robusta nos testes históricos.
- A conversão de pontos depende do `Step`/`PriceStep` do instrumento. Se nenhum for fornecido, um substituto conservador `0.0001` será usado, que corresponde a um pip Forex padrão.
- O gerenciamento de posição reflete o MT4 EA: a estratégia nunca forma pirâmide e nunca mantém as duas direções simultaneamente.
- Os comentários dentro do código estão em inglês de acordo com as diretrizes do projeto, enquanto este README inclui documentação estendida para facilitar a integração.

## Dicas de uso

1. Atribua o par Forex desejado à estratégia, deixe o tipo de vela em 5 minutos e inicie a estratégia. Os indicadores aquecerão automaticamente.
2. Considere ativar um stop loss diferente de zero ao executar dados em tempo real – o script original recomendava negociar sem ele, mas os trailing stops por si só podem não ser suficientes para o controle de risco.
3. Para portfólios algorítmicos, você pode adicionar esta estratégia a um `BasketStrategy` e gerenciar a alocação de capital externamente enquanto ainda se beneficia dos parâmetros expostos para otimização.

Esta documentação, juntamente com as traduções para russo e chinês na mesma pasta, fornece total transparência da lógica convertida.
