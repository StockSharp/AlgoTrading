# Estratégia Precipice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Precipice é uma conversão direta do assessor especialista MetaTrader *Precipice (barabashkakvn's edition)*. O sistema não analisa a estrutura do mercado nem usa indicadores; em vez disso, aguarda o fechamento da posição anterior e então joga uma moeda para decidir se entra comprado ou vendido. Se o trader habilitar ambas as direções, cada vela completada tem 50% de chance de gerar uma nova posição desde que a conta esteja flat. Ordens de proteção opcionais espelham o comportamento do MetaTrader anexando a mesma distância de stop-loss e take-profit em "pips" a cada operação.

A implementação no StockSharp mantém a natureza aleatória do código original e reproduz suas configurações de gerenciamento de capital. Converte automaticamente a distância de pip do MetaTrader para o passo de preço nativo do instrumento para que o stop-loss e o take-profit permaneçam simétricos independentemente do número de casas decimais usadas pelo ativo.

## Lógica de trading
1. Subscrever a série de velas primária definida por `CandleType` e processar apenas velas completadas para que o timing da operação corresponda à lógica `OnTick` do MetaTrader após o fechamento da barra.
2. Ignorar todos os sinais enquanto uma posição estiver aberta. O especialista coloca no máximo uma operação de cada vez.
3. Quando a estratégia está flat, gerar um número aleatório para o ramo de compra. Se `UseBuy` estiver habilitado e o resultado for inferior a 0.5, enviar uma ordem de compra a mercado com `TradeVolume` lotes.
4. Se nenhuma posição comprada foi aberta, gerar outro número aleatório para o ramo de venda. Quando `UseSell` estiver habilitado e o resultado exceder 0.5, enviar uma ordem de venda a mercado.
5. Após uma entrada, anexar ordens opcionais de stop-loss e take-profit a `StopLossTakeProfitPips` pips do MetaTrader do fechamento da vela. As ordens de proteção são canceladas automaticamente quando a posição retorna a zero.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Período de tempo primário processado pela estratégia. |
| `TradeVolume` | `decimal` | `1` | Tamanho da ordem usado para cada entrada a mercado. |
| `StopLossTakeProfitPips` | `int` | `100` | Distância (em pips do MetaTrader) entre o preço de entrada e ambas as ordens de proteção. Defina como `0` para desabilitar o stop-loss e take-profit. |
| `UseBuy` | `bool` | `true` | Habilitar entradas compradas aleatórias. |
| `UseSell` | `bool` | `true` | Habilitar entradas vendidas aleatórias. |

## Diferenças do especialista original do MetaTrader
- O MetaTrader expõe os níveis de freeze e stop do instrumento; o porte do StockSharp emula apenas a conversão de distância em pips e depende do broker para rejeitar distâncias de stop inválidas se necessário.
- O EA original usa as cotações atuais de Bid/Ask. A estratégia do StockSharp baseia as ordens de proteção no preço de fechamento da vela porque a API de alto nível recebe dados de vela agregados; efeitos de slippage e spread devem ser tratados externamente.
- O MetaTrader trabalha com tickets individuais, enquanto o StockSharp gerencia posições líquidas. A conversão mantém no máximo uma posição líquida e remove as ordens de proteção assim que a exposição volta a zero.

## Dicas de uso
- Escolha um `TradeVolume` realista que corresponda ao passo de lote do ativo. O construtor também aplica esse valor a `Strategy.Volume` para que os métodos auxiliares enviem ordens com a quantidade pretendida.
- Ajuste `StopLossTakeProfitPips` à volatilidade do instrumento. A estratégia multiplica pips pelo passo de preço do ativo (escalado para cotações de 3/5 dígitos) para obter uma distância de preço nativa.
- Habilite apenas `UseBuy` ou `UseSell` se quiser que o gerador aleatório abra operações em uma única direção.
- Como as entradas são aleatórias, monitore a estratégia com limites de risco adicionais ou uma duração máxima de posição se precisar de condições de saída deterministas.

## Indicadores
- Nenhum. A estratégia depende puramente de geração aleatória de operações e ordens de proteção opcionais.
