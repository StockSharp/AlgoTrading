# Estratégia de Engolfo de Heiken Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o comportamento dos MetaTrader 5 especialistas **heiken ashi engulf ea buy mt5.mq5** e **heiken ashi engulf sell ea mt5.mq5** combinando ambas as direções dentro de uma única StockSharp estratégia de alto nível. Ele reconstrói velas clássicas de Heiken Ashi a partir do período subscrito, espera por um padrão envolvente, confirma-o com alinhamento de média móvel e dois filtros baseados em RSI e, finalmente, abre uma posição de mercado com stop-loss fixo opcional e distâncias de lucro expressas em MetaTrader pips.

A conversão mantém as configurações originais de “compra” e “venda” separadas para que cada lado possa ser otimizado de forma independente. Um seletor de direção permite que os traders executem apenas os planos de alta, apenas os de baixa ou ambos os manuais ao mesmo tempo.

## Lógica de negociação
### Reconstrução Heiken Ashi
1. Para cada vela concluída, a estratégia constrói valores de abertura, máximo, mínimo e fechamento de Heiken Ashi usando a abertura e fechamento sintético anterior (algoritmo MT padrão).
2. Duas velas históricas de Heiken Ashi (`shift = 1` e `shift = 2`) são armazenadas para emular os parâmetros `Shift` do código MetaTrader.

### Configuração longa
1. Nenhuma posição aberta é permitida (equivalente ao bloco `NoOpenedOrders`).
2. A última vela Heiken Ashi deve ser de alta e a anterior de baixa (`ChosenCandleType = 1`, `PreviousCandleType = 2`).
3. A vela real mais recente deve fechar acima da máxima da vela anterior (`Close[1] > High[2]`), enquanto a vela anterior deve ser de baixa (`Close[2] < Open[2]`).
4. O fechamento de Heiken Ashi da vela mais recente deve permanecer acima da média móvel da linha de base (`iMA` com parâmetros `BuyBaselineMethod/Period`).
5. A MA de tendência rápida deve estar acima da MA de tendência lenta (`BuyFast` vs `BuySlow`).
6. Dois filtros RSI devem manter seus valores dentro dos limites configurados para o número especificado de velas (a mesma lógica do bloco `IndicatorWithinLimits`, incluindo o contador de exceções).
7. Se todas as condições forem aprovadas, a estratégia compra o volume solicitado, converte as distâncias configuradas de stop-loss e take-profit de pips em unidades de preço e define ordens de proteção por meio de `SetStopLoss` / `SetTakeProfit`. Uma mensagem de registro opcional replica o alerta MetaTrader.

### Configuração curta
A lógica curta reflete as regras longas com comparações opostas:
1. Posição plana.
2. A última vela Heiken Ashi é de baixa e a anterior de alta.
3. A vela real mais recente fecha abaixo da mínima da vela anterior (`Close[1] < Low[2]`), e a vela anterior é de alta.
4. O fechamento de Heiken Ashi permanece abaixo do MA de linha de base de baixa, enquanto o MA rápido permanece abaixo do MA lento.
5. Ambos os filtros RSI permanecem entre seus limites, usando sua própria configuração de turno/período/exceção.
6. Uma ordem de venda a mercado é colocada e as distâncias de stop-loss/take-profit para posições vendidas são aplicadas.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | H1 | Prazo usado para todos os indicadores e sinais. |
| `Direction` | Ambos | Qual lado do manual abrangente deve estar ativo (`BuyOnly`, `SellOnly`, `Both`). |
| `BuyVolume` | 0,01 | Tamanho do lote para negociações longas. |
| `BuyStopLossPips` | 50 | MetaTrader pips entre entrada e stop-loss para posições compradas. `0` desativa a parada fixa. |
| `BuyTakeProfitPips` | 50 | MetaTrader pips entre entrada e lucro para posições compradas. `0` desativa o alvo fixo. |
| `BuyBaselinePeriod` / `BuyBaselineMethod` | 20 / Exponencial | MA em comparação com a vela de alta Heiken Ashi (espelhos `inp1_Ro_*`). |
| `BuyFastPeriod` / `BuyFastMethod` | 20 / Exponencial | MA de tendência rápida (`inp12_Lo_*`). |
| `BuySlowPeriod` / `BuySlowMethod` | 30 / Exponencial | Tendência lenta MA (`inp12_Ro_*`). |
| `BuyPrimaryRsi*` | 14, turno 1, janela 2, exceções 0, limites [0;100] | Primeiro filtro RSI (corresponde a `inp13_*`). |
| `BuySecondaryRsi*` | 5, turno 2, janela 3, exceções 0, limites [0;100] | Segundo filtro RSI (`inp14_*`). |
| `SellVolume` | 0,01 | Tamanho do lote para negociações curtas. |
| `SellStopLossPips` | 50 | MetaTrader pips entre entrada e stop-loss para vendas. |
| `SellTakeProfitPips` | 50 | MetaTrader pips entre entrada e lucro para posições vendidas. |
| `SellBaselinePeriod` / `SellBaselineMethod` | 20 / Exponencial | MA de linha de base para configurações de baixa (`inp15_*`). |
| `SellFastPeriod` / `SellFastMethod` | 20 / Exponencial | MA de tendência rápida (`inp26_Lo_*`). |
| `SellSlowPeriod` / `SellSlowMethod` | 30 / Exponencial | Tendência lenta MA (`inp26_Ro_*`). |
| `SellPrimaryRsi*` | 14, turno 1, janela 2, exceções 0, limites [0;100] | Primeiro filtro RSI para shorts (`inp27_*`). |
| `SellSecondaryRsi*` | 5, turno 2, janela 3, exceções 0, limites [0;100] | Segundo filtro RSI para shorts (`inp28_*`). |
| `AlertTitle` | "Mensagem de Alerta" | Texto escrito no log quando uma negociação é aberta. |
| `SendNotification` | verdade | Ativa a mensagem de registro de informações que substitui MetaTrader pop-ups/notificações. |

## Gestão de risco
- As distâncias de stop-loss e take-profit são convertidas de MetaTrader pips em unidades de preço. A conversão dimensiona automaticamente o valor de acordo com o tamanho do tick de segurança (suporte para cotação de 3/5 dígitos incluído).
- Quando uma nova negociação é executada, a posição resultante esperada é passada para `SetStopLoss` / `SetTakeProfit`, imitando a colocação original do stop virtual/real.
- Nenhuma lógica adicional estava presente na fonte EA e, portanto, nenhuma foi introduzida.

## Notas
- Os filtros RSI usam a mesma lógica de “janela com exceções” que o construtor MetaTrader. Se o número de velas disponíveis for insuficiente, o sinal de negociação será ignorado até que um histórico suficiente seja coletado.
- Os valores Heiken Ashi são armazenados em cache por vela para que as mudanças do indicador (`Shift + CandlesShift`) correspondam ao comportamento dos arquivos `.mq5` originais.
- Definir `Direction` como `BuyOnly` ou `SellOnly` desativa completamente o lado oposto sem alterar seus parâmetros, o que ajuda durante a otimização.
