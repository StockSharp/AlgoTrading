# Estratégia Martin 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do expert advisor MetaTrader 5 «Martin 1» para a API de estratégia de alto nível do StockSharp. O algoritmo mantém continuamente exposição e usa etapas de martingale estilo hedge para recuperar drawdowns enquanto faz pirâmide em tendências lucrativas.

## Lógica de negociação

1. **Exposição inicial** – quando a estratégia está zerada, ela abre imediatamente uma posição na direção definida por `StartDirection`, independentemente do filtro de tempo. O tamanho base da ordem é retirado de `InitialVolume` após arredondamento para o passo de volume do instrumento.
2. **Filtro de janela de tempo** – quando `UseTradingHours` está habilitado, apenas ações de escala (pirâmide ou hedge) são permitidas entre `StartHour` e `EndHour` inclusive, usando o tempo da corretora contido nos carimbos de tempo das velas.
3. **Pirâmide de vencedores** – cada posição aberta é avaliada em cada vela concluída. Se o lucro flutuante de uma posição comprada exceder a distância de take-profit e permanecer positivo, uma ordem comprada adicional com o volume atual é enviada. Posições vendidas se comportam simetricamente. O preço da nova ordem é assumido como o fechamento da vela atual.
4. **Martingale de hedge** – quando a direção inicial é comprada e uma posição comprada perde mais de `(StopLossPips × tamanho do pip × (índice de multiplicação + 1))`, a estratégia abre uma ordem vendida oposta. Antes de colocar o hedge, o volume é multiplicado por `LotMultiplier`, arredondado para o passo permitido, e o contador de multiplicação é aumentado. A mesma lógica é aplicada em sentido inverso para a direção inicial vendida. O hedge para quando `MaxMultiplications` etapas são alcançadas.
5. **Meta de lucro global** – o lucro não realizado em todas as posições restantes (convertido para dinheiro usando `PriceStep`/`StepPrice`) é somado. Se exceder `MinProfit`, cada posição aberta é fechada emitindo uma ordem a mercado na direção oposta, e o estado do martingale é reiniciado.

## Gestão de riscos e capital

- O tamanho do pip é calculado a partir do passo de preço do instrumento. Cotações de três e cinco dígitos multiplicam o passo por dez para emular o ajuste de pip original do MetaTrader.
- Os volumes são arredondados para baixo para o `VolumeStep` mais próximo. Se o valor arredondado ficar abaixo do passo, a ordem é ignorada.
- O contador de martingale e o volume atual são reiniciados sempre que o livro fica zerado, seja naturalmente ou após atingir a meta de lucro global.
- A estimativa de lucro ignora comissões e swaps, refletindo o comportamento do script original que dependia puramente do PnL flutuante.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de vela que impulsiona todos os cálculos. | Período de 1 minuto |
| `UseTradingHours` | Habilita ou desabilita o filtro de janela de tempo. | `true` |
| `StartHour` | Hora inclusiva em que o filtro de tempo permite novas ações de escala. | 2 |
| `EndHour` | Hora inclusiva em que as ações de escala param. | 21 |
| `LotMultiplier` | Fator aplicado ao volume atual antes de abrir um hedge. | 1.6 |
| `MaxMultiplications` | Número máximo de etapas de hedge que podem ser acionadas. | 5 |
| `StartDirection` | Direção da primeira ordem após a estratégia ficar zerada. | Buy |
| `MinProfit` | Lucro flutuante (em dinheiro) que força o fechamento de todas as posições. | 1.5 |
| `InitialVolume` | Volume base para a primeira ordem e estado de reinicialização. | 0.1 |
| `StopLossPips` | Distância em pips que aciona o próximo hedge de martingale. | 40 |
| `TakeProfitPips` | Distância em pips que aciona uma entrada de pirâmide. | 100 |

## Notas de implementação

- `ProcessCandle` usa o pipeline de subscrição de velas de alto nível (`SubscribeCandles().Bind(...)`) e opera estritamente em velas concluídas, cumprindo as diretrizes da plataforma.
- A exposição hedgeada é rastreada internamente com duas listas FIFO para que a estratégia possa emular o comportamento de hedge do MetaTrader mesmo em contas de compensação.
- A conversão de lucro depende de `Security.PriceStep` e `Security.StepPrice`. Quando esses valores não estão disponíveis, a diferença de preço é multiplicada diretamente pelo volume negociado como alternativa.
- A estratégia continua a negociar continuamente; desabilitar o filtro de tempo ou definir horas amplas fará com que o algoritmo se comporte como o expert advisor original sempre ativo.
