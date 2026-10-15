# Estratégia Duplicadora com Hedge e Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Duplicadora com Hedge e Trailing** é uma conversão direta para a API de alto nível do StockSharp do expert advisor do MetaTrader 5 `Doubler.mq5`. O algoritmo abre imediatamente uma posição de mercado comprada e vendida simétricas sempre que não existe exposição, em seguida gerencia ambas as pernas com regras independentes de stop-loss, take-profit e trailing stop. A conversão preserva o comportamento de hedging do programa MQL original enquanto adapta o gerenciamento de risco para as primitivas do StockSharp (ordens a mercado, assinaturas Level1 e parâmetros de estratégia).

Ao contrário das estratégias direcionais, o sistema mantém ambas as direções ativas até que cada perna saia por sua própria lógica protetora. Uma vez que *ambas* as pernas estejam zeradas, a estratégia recria o hedge, mantendo continuamente a exposição emparelhada.

## Características principais
- **Hedging automático** – abre uma ordem de compra e venda com o mesmo volume sempre que a estratégia não tem posições ativas.
- **Controles de risco baseados em pips** – stop-loss, take-profit e offsets de trailing são configurados em pips e convertidos internamente para passos de preço inspecionando o passo de preço do ativo e a precisão decimal (instrumentos de 3/5 decimais são automaticamente dimensionados por um fator de 10).
- **Trailing independente por perna** – cada perna rastreia o melhor bid/ask atual. Quando o preço se move mais de `TrailingStopPips + TrailingStepPips` a favor, o nível de stop é avançado por `TrailingStopPips` respeitando a condição do passo de trailing, espelhando exatamente a lógica do EA original.
- **Validação de volume** – o volume da ordem é validado contra `MinVolume`, `MaxVolume` e `VolumeStep`, gerando uma exceção quando o tamanho solicitado viola as restrições da bolsa.
- **Diagnósticos opcionais** – o sinalizador `LogTradeDetails` habilita mensagens informativas detalhadas (entradas, saídas, ajustes de trailing) que ajudam durante os testes ou o monitoramento ao vivo.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-----------|--------|-------|
| `OrderVolume` | Volume de cada perna do hedge (ordens de compra e venda). | `1` | Deve respeitar os limites de volume da bolsa; normalizado para o `VolumeStep` mais próximo. |
| `StopLossPips` | Distância do stop-loss em pips. | `150` | `0` desativa o stop-loss. |
| `TakeProfitPips` | Distância do take-profit em pips. | `300` | `0` desativa o take-profit. |
| `TrailingStopPips` | Distância do trailing stop em pips. | `5` | Se maior que zero, `TrailingStepPips` também deve ser positivo. |
| `TrailingStepPips` | Movimento adicional mínimo antes que o trailing stop avance. | `5` | Barreira que evita que o stop se mova com muita frequência. |
| `LogTradeDetails` | Habilita logging detalhado de execuções e atualizações de trailing. | `false` | Definir como `true` para execuções de depuração. |

## Lógica de trading
### Entrada
1. Assinar atualizações Level1 (melhor bid/ask).
2. Quando tanto `_longPosition` quanto `_shortPosition` são nulos e não há ordens de entrada pendentes, registrar duas ordens a mercado: uma de compra e uma de venda com `OrderVolume` cada.
3. Após confirmação das execuções, a estratégia registra os preços de entrada, os níveis iniciais de stop/take e reinicia os rastreadores de trailing.

### Gestão de risco
- **Stop-loss** – para cada perna, o stop inicial é colocado a `StopLossPips` de distância do preço de entrada. Uma distância de stop de `0` desativa completamente o stop protetor.
- **Take-profit** – take-profit simétrico em `TakeProfitPips`. Um valor de `0` desativa os alvos de lucro.
- **Fechamento forçado** – se `NormalizeVolume` detectar um tamanho inválido (muito pequeno/grande ou não correspondendo ao `VolumeStep`), a estratégia lança uma exceção para evitar o envio de uma ordem inválida.

### Comportamento do trailing stop
1. Quando o preço se move favoravelmente pelo menos `TrailingStopPips + TrailingStepPips`, o stop avança para `currentPrice ± TrailingStopPips`.
2. A verificação do passo de trailing reproduz a condição MQL: o stop só se move se o novo nível estiver pelo menos `TrailingStepPips` mais próximo do preço do que o stop existente, ou se ainda não existir stop.
3. Para posições compradas é usado o melhor bid como preço de referência; para posições vendidas é usado o melhor ask para que os níveis de saída reflitam preços de execução realistas.

### Saída
- Cada perna sai de forma independente sempre que sua condição de stop-loss, trailing stop ou take-profit for atendida. As ordens de saída são registradas como ordens a mercado, e uma vez que uma perna esteja zerada, seu estado interno é limpo.
- Depois que ambas as pernas são fechadas, a próxima atualização Level1 aciona um novo par com hedge.

## Requisitos de dados
- **Level1 (melhor bid/ask)** – necessário para snapshots do preço de entrada, cálculos de trailing e gatilhos de saída.
- Nenhuma assinatura de velas ou trades é necessária; a estratégia reage exclusivamente a atualizações Level1.

## Notas sobre a conversão
- As distâncias em pips são convertidas em offsets de preço absolutos multiplicando pelo `PriceStep` do ativo. Instrumentos cotados com 3 ou 5 decimais recebem automaticamente um ajuste ×10, correspondendo à definição de pip usada no expert do MetaTrader.
- A estratégia depende dos métodos `Strategy` de alto nível do StockSharp (`RegisterOrder`, `StartProtection`, `SubscribeLevel1`) e evita operações de conector de baixo nível.
- O hedging é implementado por meio de objetos `PositionState` internos para que as pernas compradas e vendidas sejam rastreadas mesmo quando o corretor/portfólio usa posições líquidas.
- A conversão é autossuficiente e não modifica nem requer nenhum harness de teste do repositório.
