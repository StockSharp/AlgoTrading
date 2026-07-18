# Estratégia Simples de Pivot Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta C# de alto nível do MetaTrader 4 Expert Advisor armazenado em `MQL/7610/Simplepivot_www_forex-instruments_info.mq4`. O programa original verifica o preço de abertura de cada nova vela em relação ao intervalo de velas anterior e alterna entre posições de mercado longas e curtas. A versão StockSharp mantém o mesmo comportamento confiando exclusivamente em ajudantes de alto nível, como `SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket` e `ClosePosition`.

A lógica convertida:

1. Espera que uma vela finalizada obtenha os valores de abertura, máximo e mínimo.
2. Usa o intervalo de velas anterior para construir um pivô simples no ponto médio.
3. Abre uma nova posição longa quando a vela atual abre na metade inferior do intervalo ou fica acima da máxima anterior.
4. Abre uma nova posição curta quando a vela atual abre na metade superior do intervalo.
5. Sempre fecha a posição existente antes de entrar na direção oposta, replicando o comportamento do bilhete único da versão MQL.

Nenhum nível de stop-loss ou take-profit é implementado no Expert Advisor original, portanto a posição é revertida apenas quando uma nova vela dita uma direção diferente.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `OrderVolume` | 1 | Volume de ordem de mercado usado ao entrar em uma posição. |
| `CandleType` | Período de 1 minuto | Tipo de vela solicitado no feed de dados. |

## Detalhes da lógica de negociação
1. A primeira vela acabada é armazenada e usada como referência para a próxima decisão. Nenhum pedido é enviado até que haja uma vela completa para analisar.
2. Para cada vela concluída subsequente:
   - Calcule `pivot = (previousHigh + previousLow) / 2`.
   - Se `Open < previousHigh` **e** `Open > pivot`, a estratégia prepara uma entrada curta.
   - Caso contrário, prepara uma entrada longa (isso cobre aberturas na metade inferior, aberturas iguais ao pivô e quaisquer lacunas acima da máxima anterior ou abaixo da mínima anterior).
3. Se a estratégia já mantém uma posição na direção escolhida, o sinal é ignorado para evitar pagar o spread duas vezes – espelhando o retorno antecipado encontrado no código MQL.
4. Se a direção mudar, a posição atual será fechada via `ClosePosition()` e uma nova ordem de mercado será enviada usando `OrderVolume`.
5. O buffer máximo/mínimo anterior é atualizado com a última vela concluída para conduzir a próxima decisão.

## Gestão de risco
O algoritmo convertido não inclui stops ou metas de lucro. O dimensionamento da posição é controlado apenas pelo parâmetro `OrderVolume`, portanto o risco deve ser gerenciado externamente (por exemplo, ajustando o volume ou combinando a estratégia com proteções no nível da conta).

## Visualização
Quando uma área do gráfico está disponível, a estratégia traça as velas solicitadas e as negociações executadas, o que ajuda a validar os pivôs durante os backtests.
