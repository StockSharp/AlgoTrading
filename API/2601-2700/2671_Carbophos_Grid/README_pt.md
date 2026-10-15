# Estratégia Carbophos Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A Estratégia Carbophos Grid é uma conversão direta do assessor especialista MetaTrader 5 "Carbophos". Ela mantém continuamente uma escada simétrica de ordens limit de compra e venda ao redor dos preços bid/ask atuais. A estratégia monitora o lucro flutuante agregado de toda a grade e fecha toda a exposição assim que o alvo de lucro desejado ou o drawdown máximo tolerado é atingido. Depois que a posição é aplainada e não restam ordens pendentes, a escada é reconstruída automaticamente.

## Lógica de Trading
1. Quando a estratégia inicia e não há ordens ativas nem posições abertas, ela calcula o espaçamento da grade em unidades de preço com base no passo configurado em pips e a precisão de preço do instrumento. Cinco (configurável) ordens sell limit são colocadas acima do melhor bid e o mesmo número de ordens buy limit são colocadas abaixo do melhor ask.
2. Se alguma ordem for preenchida, a posição resultante é monitorada tick a tick usando dados de Level1. O PnL flutuante é calculado a partir da diferença entre o preço de saída atual (bid para posições compradas, ask para posições vendidas) e o preço médio de entrada ponderado por volume.
3. Uma vez que o lucro flutuante excede o alvo configurado, ou a perda flutuante viola o limiar de proteção, a estratégia envia uma ordem de mercado para fechar a posição aberta e cancela todas as ordens limit restantes. O indicador de estado é limpo para que a escada seja reconstruída na próxima atualização de preço.
4. Se todas as ordens forem preenchidas mas a posição líquida retornar a zero (por exemplo, porque o mercado se reverte através da grade), a próxima atualização de Level1 aciona uma nova colocação de escada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `ProfitTarget` | Lucro flutuante (dinheiro) que desencadeia o fechamento de toda a grade. |
| `MaxLoss` | Perda flutuante (dinheiro) que força uma saída de emergência. |
| `StepPips` | Distância entre níveis consecutivos da grade expressa em pips. Convertida internamente em unidades de preço usando o tamanho do tick e a precisão decimal do símbolo. |
| `OrdersPerSide` | Número de ordens limit colocadas acima e abaixo do preço atual do mercado. |
| `OrderVolume` | Volume para cada ordem de grade. |

Todos os parâmetros suportam intervalos de otimização para simplificar a experimentação no otimizador do StockSharp.

## Gestão de Risco e Proteções
A estratégia usa o gancho embutido `StartProtection()` e aplica níveis monetários duros de stop/profit no nível da estratégia. O cálculo do PnL flutuante depende das configurações `PriceStep` e `StepPrice` do instrumento. Quando qualquer um dos limiares é atingido, a estratégia fecha a posição com uma ordem de mercado e cancela cada ordem limit ativa antes de redefinir o indicador interno da grade.

## Notas de Conversão
- O assessor especialista MQL5 original ajustava os valores de pip para símbolos Forex de três e cinco decimais. O port StockSharp replica esse comportamento multiplicando o `PriceStep` da exchange por 10 quando o instrumento expõe três ou cinco decimais.
- MetaTrader agrega o lucro de posição, comissão e swap por número mágico. No StockSharp o PnL flutuante é recalculado a partir do preço médio de entrada e o preço bid/ask atual, então o tratamento explícito de comissões não é necessário.
- A colocação de ordens, cancelamento e gestão de posições são implementados via API `Strategy` de alto nível (`BuyLimit`, `SellLimit`, `CancelActiveOrders`, `BuyMarket`, `SellMarket`) conforme exigido pelas diretrizes do projeto.
- A grade é atualizada exclusivamente a partir de atualizações de Level1, replicando o comportamento "OnTick" do código original sem introduzir timers personalizados ou coleções.

## Uso
1. Atribua a `Security` e o `Portfolio` desejados à instância da estratégia antes de iniciá-la.
2. Opcionalmente ajuste os parâmetros para corresponder à volatilidade do instrumento alvo e à tolerância ao risco.
3. Inicie a estratégia. Ela imediatamente subscreve os dados de Level1, constrói a primeira grade assim que os preços bid e ask estiverem disponíveis, e continua gerenciando a exposição automaticamente.
4. Monitore o registro de mensagens como "Profit target reached" ou "Maximum loss reached" para saber quando a grade foi redefinida.

Certifique-se de que o instrumento selecionado fornece atualizações de Level1 com os melhores preços bid e ask; caso contrário a escada não será construída.
