# Estratégia Ordens Pendentes Por Horário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o clássico expert MetaTrader "Pending orders by time" para StockSharp. Funciona com uma agenda discreta: todos os dias coloca ordens stop simétricas ao redor do mercado quando uma nova hora de sessão começa, e limpa todas as ordens mais as posições abertas em uma hora de fechamento especificada. A implementação mantém as entradas originais baseadas em pips, converte-as em unidades de preço nativas e usa a API de alto nível para gerenciar o risco.

## Como funciona

1. **Gatilho baseado em tempo** – Quando uma vela que termina na hora de abertura configurada é recebida, a estratégia envia um buy stop acima do ask e um sell stop abaixo do bid. Ambas as ordens são deslocadas pelo parâmetro `Distance (pips)` convertido em unidades de preço.
2. **Ordens protetoras** – `StartProtection` anexa automaticamente proteção de stop-loss e take-profit usando as distâncias em pips definidas nos parâmetros. `ManageRisk` também atua como salvaguarda, fechando qualquer posição residual se uma vela completada mostrar que os limites foram cruzados.
3. **Encerramento de sessão** – Quando a hora de fechamento chega, a estratégia cancela quaisquer ordens pendentes restantes e sai forcosamente de operações abertas independentemente do lucro ou perda. Isso reproduz o comportamento do expert original de redefinir no final da sessão.
4. **Tamanho de pip com reconhecimento de dígitos** – O multiplicador de pip emula a implementação MetaTrader multiplicando o passo de preço por dez para símbolos cotados com três ou cinco casas decimais (p. ex., JPY ou pares FX de 5 dígitos). Isso mantém as entradas legadas consistentes entre brokers.

O tipo de vela padrão são barras de 30 minutos para permanecer abaixo da restrição original de períodos menores que H1. Qualquer outro período pode ser usado, desde que os registros de hora resultantes correspondam às horas de sessão desejadas.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Opening Hour` | Hora (0-23) quando a estratégia colocará o par de ordens stop. | 9 |
| `Closing Hour` | Hora (0-23) quando todas as ordens são canceladas e posições são fechadas. | 2 |
| `Distance (pips)` | Deslocamento, em pips, entre o preço atual e as entradas stop pendentes. | 20 |
| `Stop Loss (pips)` | Distância em pips para o stop protetor assim que uma posição estiver aberta. | 20 |
| `Take Profit (pips)` | Distância em pips para o alvo de lucro assim que uma posição estiver aberta. | 500 |
| `Order Volume` | Quantidade usada ao colocar cada ordem stop pendente. | 0.1 |
| `Candle Type` | Período que impulsiona a agenda horária. | Período de 30 minutos |

Todos os parâmetros podem ser otimizados. As entradas baseadas em pips são convertidas internamente usando o passo de preço do instrumento para que permaneçam portáteis entre símbolos FX com diferente precisão decimal.

## Fluxo de trabalho diário

1. **A cada fechamento de vela** a estratégia verifica se a distância de stop-loss ou take-profit foi atingida. Se sim, fecha a posição ativa a mercado.
2. **Quando a hora de fechamento é alcançada** cancela quaisquer ordens pendentes não preenchidas e sai da posição, garantindo que o livro esteja plano antes da próxima sessão.
3. **Quando a hora de abertura é alcançada** (e a estratégia está plana) cancela ordens antigas por precaução e envia um novo sell stop abaixo do bid e um buy stop acima do ask. As ordens são espelhadas ao redor do spread para que qualquer rompimento possa ser capturado.
4. **Ao longo da sessão** a proteção a nível de plataforma criada por `StartProtection` mantém um stop-loss e take-profit anexados, agindo imediatamente se a ação do preço intrabar atingir os limites.

## Notas de uso

- Use instrumentos cujo tamanho de tick representa um único "ponto" para que o ajuste de pip reflita o expert original. Tamanhos de tick exóticos podem exigir ajuste manual dos parâmetros de distância.
- A lógica assume um ciclo de trading por dia. Se usar dados intradiários com múltiplas correspondências de abertura/fechamento, ajuste as horas de acordo.
- Como todas as ações ocorrem no fechamento da vela, selecione um tamanho de vela que corresponda à frequência com que deseja avaliar a agenda. Por exemplo, velas horárias fornecem a mesma cadência que a versão MetaTrader.
- A estratégia só coloca novas ordens pendentes quando a posição está plana, evitando sobreexposição se uma operação de rompimento ainda estiver ativa durante a próxima hora de abertura.

## Diferenças da versão MQL

- As saídas protetoras são tratadas via `StartProtection` mais verificações explícitas, aproveitando a API de alto nível do StockSharp em vez da atribuição direta de stop-loss no ticket da ordem pendente.
- Os preços bid/ask são lidos de `Security.BestBid` e `Security.BestAsk`. Se essas cotações estiverem indisponíveis, o fechamento da vela é usado como referência de fallback.
- As ordens de mercado são usadas para liquidar posições na hora de fechamento por simplicidade e para evitar comportamentos específicos do broker.
