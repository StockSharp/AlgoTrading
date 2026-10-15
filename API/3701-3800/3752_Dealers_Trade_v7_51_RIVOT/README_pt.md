# Negociação de Revendedores v7.51 RIVOT (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

Dealers Trade v7.51 é uma estratégia de grade estilo martingale que foi originalmente entregue como o MetaTrader 4 consultor especialista `Dealers_Trade_v_7.51_RIVOT.mq4`. O porto mantém a ideia original de negociar longe de um viés direcional baseado em pivô, escalando para o lado dominante sempre que o preço retrocede por uma distância configurável de pip. A implementação StockSharp usa ajudantes de estratégia de alto nível para assinar velas, calcular as zonas de pivô e gerenciar o dimensionamento de posição, risco e saídas.

## Lógica de negociação

1. **Estrutura dinâmica**
   - A estratégia constrói dois preços de referência para cada vela acabada:
     - **Pivot clássico** (`P`) = `(previous high + previous low + previous close + current open) / 4`.
     - **Pivô flutuante** (`FLP`) = `(current high + current low + current close) / 3`.
   - Uma lacuna em pips entre `P` e `FLP` deve ser maior ou igual a `GapThreshold` para permitir a negociação para a barra atual.

2. **Viés direcional**
   - Quando o fechamento da vela está acima de ambos os pivôs e o filtro de gap é satisfeito, a tendência muda para **longo**.
   - Quando o fechamento da vela está abaixo de ambos os pivôs com o gap confirmado, a tendência muda para **curto**.
   - A tendência permanece em vigor até que a série de posições seja totalmente fechada ou a condição oposta apareça após o término da série.

3. **Escalonamento de entradas**
   - Apenas uma série de negociações pode estar ativa por vez.
   - A primeira entrada segue o viés imediatamente.
   - Entradas adicionais são abertas somente quando o preço retrocede em relação à tendência ativa em pelo menos `PipDistance` pips do preenchimento mais recente, emulando a média original do martingale.
   - Cada novo pedido multiplica o tamanho anterior por `VolumeMultiplier` mas nunca excede `MaxVolume`.
   - O número de entradas empilhadas é limitado por `MaxTrades`.

4. **Controles de risco**
   - Um stop loss rígido a `StopLoss` pips da entrada média ponderada por volume fecha a série inteira.
   - Um take-profit fixo de `TakeProfit` pips garante ganhos assim que o preço reverte a favor.
   - Quando ativado, o trailing-stop bloqueia dinamicamente os lucros, aproximando-se do preço cada vez que se move além de `TrailingStop` pips além da entrada média.

5. **Redefinir condições**
   - Qualquer saída completa (stop-loss, take-profit, trailing-stop ou achatamento manual da posição) redefine os contadores martingale e remove a tendência direcional.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Volume` | 1 | Tamanho base do pedido para a primeira entrada. |
| `MaxTrades` | 5 | Número máximo de entradas médias por série. |
| `PipDistance` | 4 | Movimento adverso mínimo (em pips) necessário antes de adicionar uma nova posição. |
| `TakeProfit` | 15 | Distância da entrada média ponderada pelo volume para fechar toda a grade em lucro. |
| `StopLoss` | 90 | Distância da entrada média que aciona uma saída protetora. |
| `TrailingStop` | 15 | Compensação de trailing-stop aplicada quando o preço oscila a favor; definido como zero para desativar o rastreamento. |
| `VolumeMultiplier` | 1,5 | Fator usado para aumentar o tamanho do pedido para cada entrada subsequente. |
| `MaxVolume` | 5 | Limite para o volume de pedido único após aplicação do multiplicador. |
| `GapThreshold` | 7 | Gap mínimo (em pips) entre os pivôs clássico e flutuante necessário para ativar o viés. |
| `CandleType` | Velas com intervalo de tempo de 15 minutos | Tipo de vela usado para cálculos e tomada de decisões. |

Todos os parâmetros são configurados através do `StrategyParam<T>` para que possam ser otimizados dentro do StockSharp Designer ou Strategy Runner.

## Notas de uso

- A estratégia depende apenas de dados de velas; nenhum fluxo direto de oferta/venda em nível de tick é necessário. Certifique-se de que seu provedor de dados possa entregar o `CandleType` selecionado.
- Como StockSharp agrega posições por padrão, a implementação mantém uma média ponderada por volume interno para emular o grid book MT4. Se ocorrerem preenchimentos parciais, a contabilidade de posição integrada mantém os valores consistentes.
- A renderização do gráfico adiciona duas linhas horizontais (`Pivot` e `FloatingPivot`) à área do gráfico quando disponível.
- Não há negociação reversa automática; o sistema espera que a série em andamento termine antes de aceitar uma mudança de tendência.

## Diferenças da versão MQL

- O script original atraiu vários rótulos e comentários no gráfico MT4. A porta mantém apenas a lógica de negociação funcional e substitui os visuais por StockSharp linhas do gráfico.
- Recursos de proteção de conta baseados no total de pedidos abertos, filtragem manual de números mágicos e tabelas de valores de pip específicos de símbolos não são obrigatórios em StockSharp e foram omitidos.
- O fechamento de pedidos com preços exatos de ticks (`Ask == tp`) no código MetaTrader é aproximado com comparações de preços em fechamentos de velas.
- O gerenciamento comercial é implementado com ordens de mercado (`BuyMarket`/`SellMarket`) em vez de loops de tickets MT4. As paradas e saídas finais acontecem nas atualizações das velas.

## Melhores Práticas

- Sempre teste a estratégia em negociações de papel ou simulações históricas com modelos realistas de spread/comissão antes de entrar em operação.
- Considere reduzir `VolumeMultiplier` ou `MaxTrades` em instrumentos altamente voláteis para controlar o rebaixamento.
- Para produtos intradiários, ajuste `CandleType` para corresponder à granularidade dos dados da configuração original (o padrão é 15 minutos, mas o EA era usado com frequência em M15 e H1).

## Arquivos

- `CS/DealersTradeV751RivotStrategy.cs` – Implementação principal de C#.
- `README_zh.md` – documentação chinesa.
- `README_ru.md` – Documentação russa.
