# Estratégia de Rompimento Pendente de Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o assessor especialista de MetaTrader "Bands 2" sobre a API de alto nível do StockSharp. Monitora velas terminadas, verifica se a hora atual está dentro da janela de trading configurada e se o preço está operando dentro do canal de Bollinger. Quando essas condições são atendidas, coloca uma grade simétrica de três ordens stop de compra e três stop de venda ao redor do envelope de Bollinger. Cada ordem carrega suas próprias distâncias de stop-loss e take-profit, e qualquer execução remove as demais ordens pendentes.

A abordagem é projetada para rompimentos das bandas de Bollinger. A referência do stop-loss pode ser alternada entre a banda oposta ou a média móvel central. Um módulo de trailing stop separado ajusta continuamente o stop protetor uma vez que a posição se move em lucro por um passo configurável.

## Detalhes

- **Dados de mercado**: Funciona com qualquer instrumento/tipo de vela fornecido através do StockSharp.
- **Horário de trading**: Usa `HourStart`/`HourEnd` para restringir a colocação de ordens. As ordens são atualizadas em cada vela terminada dentro dessa janela.
- **Lógica de entrada**:
  - Aguardar uma vela terminada com preço de fechamento estritamente entre as bandas de Bollinger deslocadas superior e inferior.
  - Excluir ordens pendentes sobrantes da barra anterior e colocar três stops de compra acima da banda superior e três stops de venda abaixo da banda inferior.
  - Cada nível é separado por `StepPips` convertido em ticks.
- **Modos de Stop-Loss**:
  - *BollingerBands*: O stop-loss usa a banda oposta deslocada pela mesma distância de passo que a ordem de entrada.
  - *MovingAverage*: O stop-loss usa o valor da média móvel mais/menos a distância de passo (usa o preço aplicado e método configurados).
  - *None*: Nenhum stop inicial é definido; o trailing stop pode ainda ser ativado depois.
- **Lógica de Take-Profit**:
  - O primeiro nível usa `FirstTakeProfitPips` para ordens de compra e venda.
  - As ordens de compra segunda e terceira usam distâncias de take-profit `Second`/`Third`, enquanto as ordens de venda seguem o comportamento do script MQL original e sempre reutilizam a primeira distância de take-profit.
- **Gestão de ordens**:
  - Quando qualquer ordem pendente é executada, a estratégia cancela todas as outras ordens de entrada e cria ordens protetoras independentes do mercado (stop + limit) para o volume executado.
  - O módulo trailing move a ordem stop em direção ao mercado uma vez que o preço se move por `TrailingStopPips + TrailingStepPips` desde a entrada.
  - As ordens protetoras de stop/limit são canceladas automaticamente quando a posição vai a zero.
- **Normalização de preços**: Todos os níveis de preço são arredondados para o tamanho de tick do instrumento e a conversão ponto-a-pip imita o tratamento original de 3/5 dígitos.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Volume para cada ordem pendente (mesmo volume para todas as seis ordens). |
| `CandleType` | Período/tipo de dados usado para cálculos do indicador. |
| `HourStart`, `HourEnd` | Horas inclusivas/exclusivas (0-24) que permitem colocar novas ordens pendentes. `HourEnd` deve ser maior que `HourStart`. |
| `StopLossModes` | Referência de posicionamento para o stop-loss inicial (`BollingerBands`, `MovingAverage`, `None`). |
| `FirstTakeProfitPips`, `SecondTakeProfitPips`, `ThirdTakeProfitPips` | Distâncias de take-profit (em pips) convertidas em offsets de preço para as entradas primeira, segunda e terceira. |
| `TrailingStopPips`, `TrailingStepPips` | Distância do trailing stop e o passo adicional necessário antes de avançar o stop. Zero para desativar trailing. |
| `StepPips` | Espaçamento entre ordens pendentes consecutivas (convertido em preço). |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Configuração de média móvel usada para a entrada de Bollinger e opcionalmente para o posicionamento de stop quando `StopLossModes` é `MovingAverage`. O `MaShift` emula o deslocamento para frente do EA original. |
| `BandsPeriod`, `BandsShift`, `BandsDeviation`, `BandsPriceType` | Configurações das bandas de Bollinger (período, deslocamento, multiplicador de desvio e preço aplicado). |

## Resumo do comportamento

1. Assinar velas terminadas do período selecionado.
2. Em cada vela terminada dentro da janela de trading, calcular as bandas de Bollinger deslocadas e a média móvel usando os preços aplicados selecionados.
3. Garantir que o fechamento da vela está dentro do canal de bandas, então colocar a grade de stop de compra/venda ao redor das bordas do canal com stops e alvos individuais.
4. Quando uma ordem é executada, cancelar as ordens de entrada restantes, enviar ordens protetoras de stop/limit e iniciar trailing de acordo com os parâmetros configurados.
5. Fechar as ordens protetoras quando a posição sai, pronto para a próxima oportunidade de rompimento.
