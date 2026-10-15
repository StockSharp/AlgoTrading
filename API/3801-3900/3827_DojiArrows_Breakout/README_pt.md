# Estratégia de flechas Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Doji Arrows** é uma versão StockSharp do MetaTrader consultor especialista `Doji_arrows_expert1.mq4`. A ideia de negociação é detectar uma vela doji neutra e negociar imediatamente o rompimento que se segue na próxima barra. Quando o mercado imprime uma vela corporal muito pequena (abertura ≈ fechamento) e a vela subsequente fecha além da máxima ou mínima doji, a estratégia interpreta o movimento como um rompimento direcional e entra nessa direção.

## Lógica de negociação
- **Janela de detecção de sinal** – a estratégia armazena continuamente em buffer as duas velas concluídas anteriormente. A vela mais antiga deve ser um doji, enquanto a vela mais recente confirma o rompimento.
- **Definição de Doji** – uma vela se qualifica como doji quando a diferença absoluta entre abertura e fechamento é menor ou igual a `DojiBodyThresholdSteps * PriceStep`. Com o limite padrão de 1 passo, a barra pode desviar no máximo um tick.
- **Confirmação do intervalo** –
  - Configuração longa: a vela que segue o doji fecha acima da máxima do doji mais o filtro opcional `BreakoutBufferSteps`.
  - Configuração curta: a vela que segue o doji fecha abaixo do mínimo do doji menos o mesmo buffer.
- **Sinalização de disparo único** – a estratégia lembra se a barra anterior já acionou um sinal longo ou curto e reage apenas a um novo rompimento. Esse comportamento reflete o especialista original que gerou uma seta por sequência de breakout.
- **Execução de pedidos** –
  - Se aparecer um rompimento contra uma posição oposta existente, a estratégia primeiro a fecha e depois entra na nova direção com volume `Volume + |Position|` para virar e abrir a nova negociação.
  - No estado neutro, abre uma ordem de mercado na direção do rompimento.

## Gestão de risco
- **Stop-loss inicial** – após cada entrada a estratégia coloca um nível de proteção interno `InitialStopSteps * PriceStep` longe do preço de preenchimento.
- **Take-profit fixo** – sai de parte ou de toda a posição quando o preço atinge `TakeProfitSteps * PriceStep` a partir da entrada.
- **Trailing stop** – quando a negociação se move a favor mais de `TrailingStopSteps * PriceStep`, o nível de stop é seguido vela por vela, bloqueando os lucros e permitindo que o movimento ocorra.
- Todos os cálculos de proteção são feitos em etapas de preços nativos, tornando o instrumento lógico agnóstico.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo/período de vela a ser analisado. | Período de 5 minutos |
| `DojiBodyThresholdSteps` | Corpo doji máximo expresso em etapas de preço. | 1 |
| `BreakoutBufferSteps` | Filtro extra acima/abaixo do extremo doji antes de aceitar um rompimento. | 0 |
| `InitialStopSteps` | Distância inicial do stop-loss desde a entrada em etapas. | 20 |
| `TakeProfitSteps` | Distância de lucro desde a entrada em etapas. | 25 |
| `TrailingStopSteps` | Distância de trailing stop mantida quando a negociação gera lucro. | 10 |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` tornando-os visíveis na IU e prontos para otimização.

## Notas de implementação
- A classe é baseada na assinatura de velas de alto nível API (`SubscribeCandles().Bind(...)`) para permanecer sincronizada com as práticas recomendadas da estrutura.
- O estado entre as chamadas é mantido com `_previousCandle` e `_twoCandlesAgo`, garantindo que apenas velas finalizadas participem da tomada de decisão.
- Os níveis de proteção são armazenados separadamente para posições longas e curtas e são redefinidos quando as posições fecham ou quando os dados de mercado são insuficientes.
- As declarações de registro fornecem informações sobre detecção de sinal, eventos de stop-loss e take-profit, simplificando a depuração durante backtests.

## Dicas de uso
1. Valide os limites de tick padrão em cada instrumento: aumente `DojiBodyThresholdSteps` para mercados voláteis onde impressões doji exatas são raras.
2. Otimize `BreakoutBufferSteps` para filtrar pequenos rompimentos falsos quando spreads ou ruídos forem significativos.
3. Combine a estratégia com sobreposições de risco externo (parada de carteira, filtros de sessão de negociação) se você implementá-la em vários símbolos simultaneamente.
4. Como os sinais dependem de velas concluídas, escolha um tipo de vela compatível com o horizonte de negociação desejado (por exemplo, 1 minuto para scalping, 15 minutos para entradas de swing).
