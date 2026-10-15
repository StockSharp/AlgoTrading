# Estratégia de Temporização EMA BeerGod
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especializado BeerGodEA do MetaTrader dentro do StockSharp. Ela negocia configurações de
reversão à média em um único símbolo monitorando uma média móvel exponencial (EMA) de 60 períodos e comparando a ação do preço
atual com a barra anterior. Os sinais são avaliados apenas uma vez por barra em um deslocamento configurável de minutos após a
abertura da vela, imitando o EA original que aguarda alguns minutos antes de agir.

Quando o preço se afasta temporariamente da EMA enquanto a média está tendendo na direção oposta, a estratégia abre uma posição
a mercado esperando que o movimento reverta. As posições existentes na direção oposta são invertidas imediatamente ajustando o
tamanho da ordem para que os vendidos sejam cobertos antes de estabelecer uma nova posição comprada (e vice-versa).

## Como Funciona

1. Subscrever velas de período (padrão 5 minutos) e construir uma EMA de 60 períodos sobre os preços de fechamento.
2. Rastrear a vela atual em tempo real. No primeiro tick de cada nova barra, armazenar o valor EMA anterior e o fechamento da
   barra anterior para que a estratégia possa compará-los depois.
3. Uma vez transcorrido o número configurado de minutos desde a abertura (padrão 3 minutos), avaliar as seguintes condições
   usando o preço atual e a inclinação da EMA:
   - **Configuração de compra**: preço atual < EMA atual, EMA está abaixo de seu valor anterior (caindo), e preço atual <
     fechamento da barra anterior.
   - **Configuração de venda**: preço atual > EMA atual, EMA está acima de seu valor anterior (subindo), e preço atual >
     fechamento da barra anterior.
4. Se ocorrer uma configuração de compra enquanto não estiver comprado, enviar uma ordem de compra a mercado dimensionada para
   fechar qualquer vendido aberto e estabelecer o volume comprado desejado. A mesma lógica se aplica simetricamente para
   configurações de venda.
5. Após um trade ser acionado, o sinal para aquela vela é considerado processado para evitar entradas duplicadas.

## Parâmetros

- **Volume** – tamanho de ordem em lotes (padrão 1). A estratégia adiciona automaticamente o valor absoluto da posição atual
  quando precisa inverter direções para que a nova ordem feche a exposição antiga e abra o novo trade em uma única transação.
- **EMA Length** – período de lookback para a média móvel exponencial (padrão 60).
- **Trigger Minutes** – número de minutos após a abertura da barra quando as condições de entrada são verificadas (padrão 3).
  Se a janela for perdida, a estratégia aguarda a próxima vela.
- **Candle Type** – tipo de dados de vela usado para cálculos (padrão período de 5 minutos).

## Notas de Trading

- A lógica funciona em qualquer símbolo desde que dados de velas e preços de nível 1 estejam disponíveis. Ajuste a duração da
  vela se o instrumento negociar em sessões diferentes da configuração original do MetaTrader.
- Apenas uma posição (comprada ou vendida) é mantida em qualquer momento. Inverter direções é feito dimensionando a nova ordem
  a mercado para cobrir a posição pendente e abrir o novo trade em um passo.
- Nenhum nível explícito de stop-loss ou take-profit é definido no EA original. O gerenciamento de risco deve ser adicionado
  externamente se necessário.
- A proteção de início está habilitada para que o StockSharp trate automaticamente as saídas de posição de emergência quando
  ocorrem intervenções manuais ou problemas de conexão.
