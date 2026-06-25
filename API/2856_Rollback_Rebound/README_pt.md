# Estratégia de Retrocesso e Rebote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Retrocesso e Rebote é uma conversão C# do assessor especialista MQL5 "TST (barabashkakvn's edition)". Ela monitora um único instrumento no período especificado pelo parâmetro `CandleType` e busca movimentos fortes que retrocedam de volta para dentro do intervalo da barra. Quando uma barra de alta recua a partir de sua máxima por mais do que o limiar de retrocesso, a estratégia compra, enquanto um retrocesso baixista equivalente desencadeia uma venda. A implementação usa a API de subscrição de velas de alto nível do StockSharp e gerencia todas as ordens protetoras em unidades de pip que são convertidas em deslocamentos de preço absolutos.

As distâncias em pip são calculadas a partir do `PriceStep` do instrumento. Para símbolos que cotam com três ou cinco decimais, a estratégia automaticamente multiplica o passo por dez para corresponder à definição de pip do MetaTrader. Todo o dimensionamento de posição é retirado da propriedade base `Volume` da estratégia.

## Lógica de entrada
- Processar apenas velas terminadas da série `CandleType` configurada.
- Com `ReverseSignal = false` (padrão):
  - **Configuração comprada:** a vela fecha abaixo de sua abertura e a diferença entre a máxima da vela e o fechamento excede `RollbackRatePips` (convertido para preço). Isso indica que o preço se expandiu para cima e então retrocedeu profundamente o suficiente para qualificar para uma entrada contrária comprada.
  - **Configuração vendida:** a vela fecha acima de sua abertura e a diferença entre o fechamento e a mínima da vela excede `RollbackRatePips`. Isso reflete a lógica comprada no lado baixista.
- Quando `ReverseSignal = true`, os papéis das condições comprada e vendida são trocados, permitindo ao trader mudar a direção sem alterar os outros parâmetros.
- Novas entradas só são colocadas quando a posição atual está plana ou na direção oposta. O volume executado é igual a `Volume + |Position|` para que uma posição oposta seja fechada antes de estabelecer o novo trade.

## Lógica de saída
- Na entrada, a estratégia armazena os níveis de stop-loss e take-profit com base nos deslocamentos de pip configurados. Quando o intervalo da vela toca um nível, a posição é fechada com uma ordem de mercado.
- `StopLossPips = 0` ou `TakeProfitPips = 0` desabilita o nível de proteção correspondente.
- A lógica de trailing é ativada uma vez que o lucro flutuante excede `TrailingStopPips + TrailingStepPips` (em termos de preço).
  - Para trades comprados, o stop se desloca para `preço mais alto - TrailingStopPips` sempre que o novo nível estiver pelo menos `TrailingStepPips` acima do stop anterior.
  - Para trades vendidos, o stop se desloca para `preço mais baixo + TrailingStopPips` quando o novo nível estiver pelo menos `TrailingStepPips` abaixo do stop anterior.
  - Se o mercado se reverter e cruzar o trailing stop, a posição é encerrada imediatamente.
- Quando nenhuma posição está aberta, todas as variáveis de estado internas são limpas para evitar dados obsoletos.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usada para cálculo de sinais. | Período de 15 minutos |
| `StopLossPips` | Distância do stop de proteção em pips. Definir como zero para desabilitar. | 30 |
| `TakeProfitPips` | Distância do take-profit em pips. Definir como zero para desabilitar. | 90 |
| `TrailingStopPips` | Offset do trailing stop em pips. Definir como zero para desabilitar o trailing. | 1 |
| `TrailingStepPips` | Lucro extra (em pips) necessário antes que o trailing stop possa se mover novamente. Deve ser positivo quando o trailing está habilitado. | 15 |
| `RollbackRatePips` | Recuo mínimo do extremo da barra que valida um sinal. | 15 |
| `ReverseSignal` | Inverte a direção de entrada (sinais comprados tornam-se vendidos e vice-versa). | false |

## Notas de uso
- Definir a propriedade `Volume` antes de iniciar a estratégia; ela define a quantidade negociada para cada ordem.
- O trailing requer `TrailingStopPips > 0` e `TrailingStepPips > 0`. A estratégia lança um erro na inicialização se essa relação for violada.
- Como o especialista original avaliava ticks dentro da barra ativa, o porto C# usa a máxima/mínima/fechamento da vela terminada para aproximar o mesmo comportamento. A diferença é insignificante para a maioria dos backtests e mantém a implementação alinhada com a API de alto nível do StockSharp.
- A estratégia funciona com um único instrumento. Para operar múltiplos instrumentos, criar instâncias de estratégia separadas.
