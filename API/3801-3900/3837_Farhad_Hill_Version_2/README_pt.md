# Estratégia Farhad Hill Versão 2 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader “Farhad Hill Version 2”.
Ele combina vários filtros de indicadores para negociar reversões de tendências em símbolos Forex. O
a lógica convertida retém a pilha de indicadores original (MACD, Stochastic, Parabolic SAR,
Momentum e cruzamento de média móvel opcional) e sua gestão de dinheiro mais
comportamento.

A estratégia funciona em um único período (velas padrão de 30 minutos) e abre apenas um
posição de cada vez. Stop-loss protetor, take-profit e três estilos de trailing-stop são
suportado para espelhar a versão MQL. Todos os comentários no código são fornecidos em inglês como
solicitado.

## Lógica de negociação
- **MACD filtro** – quando ativado, os longos requerem MACD linha principal abaixo da linha de sinal;
shorts requerem MACD principal acima da linha de sinal.
- **Stochastic filtro de nível** – demanda de posições compradas %K abaixo do limite inferior, demanda de posições vendidas
%K acima do limite superior. Quando o filtro cruzado opcional está ativado, uma tendência de alta
A linha %K/%D (de baixo para cima) é necessária para posições compradas e uma linha de baixa para posições vendidas.
- **Parabolic SAR filtro** – posições compradas exigem SAR abaixo do preço com um degrau descendente
(anterior SAR maior que o atual); shorts exigem SAR acima do preço com um aumento
passo. A conversão usa preços de velas fechadas como referência.
- **Filtro de impulso** – calculado com base nos preços de abertura das velas, correspondendo às configurações de MQL.
Os comprados precisam de impulso abaixo do limite inferior, os vendidos precisam de impulso acima do limite superior
limite.
- **Média móvel cruzada (opcional)** – tipo de MA configurável, preço aplicado e períodos.
Os comprados precisam do MA rápido acima do MA lento; shorts precisam da relação inversa.

A estratégia avalia apenas sinais em velas concluídas e ignora novas entradas quando um
existe posição aberta. As entradas são feitas com ordens de mercado utilizando o lote calculado
tamanho.

## Gerenciamento de posição
- **Stop-loss / Take-profit** – especificado em pips. Um pip é derivado do instrumento
`PriceStep`, voltando para `0.0001` se não estiver disponível.
- **Tipos de trailing stop**
  1. Imediato – quando o preço ultrapassa a distância do stop, o stop segue o preço.
  2. Atrasado – espera que o preço se mova pela distância final da entrada antes
seguindo em um deslocamento fixo.
  3. Três estágios – reproduz a lógica original de três níveis com duas etapas de ponto de equilíbrio
e uma distância final de fuga.
- Ordens de proteção são colocadas com `SellStop`/`BuyStop` (para stop-loss) e
`SellLimit`/`BuyLimit` (para obter lucro) para que permaneçam visíveis na bolsa.

## Gestão de capital
- **Lote Fixo** – negocia o volume fixo configurado. Se `AccountIsMini` estiver ativado, muitos
são convertidos para dimensionamento de minilote com mínimo de 0,1.
- **Risco percentual** – replica a fórmula original
`floor(FreeMargin * percent / 10000) / 10`, limitado pelo limite `MaxLots` e ajustado
para mini contas quando necessário. Se o valor do portfólio não estiver disponível, a estratégia
volta para o lote fixo.

## Parâmetros
Todos os parâmetros são expostos através de objetos `StrategyParam<T>` e podem ser otimizados ou
alterado na IU. Grupos principais:

| Grupo | Parâmetro | Descrição |
| --- | --- | --- |
| Geral | `CandleType` | Prazo das velas usadas para sinais |
| Gestão de dinheiro | `AccountIsMini`, `UseMoneyManagement`, `TradeSizePercent`, `FixedVolume`, `MaxLots` |
| Risco | `StopLossPips`, `TakeProfitPips`, `UseTrailingStop`, `TrailingStopType`, `TrailingStopPips`, `FirstMovePips`, `TrailingStop1`, `SecondMovePips`, `TrailingStop2`, `ThirdMovePips`, `TrailingStop3` |
| Indicadores | `UseMacd`, `UseStochasticLevel`, `UseStochasticCross`, `UseParabolicSar`, `UseMomentum`, `UseMovingAverageCross`, `MacdFast`, `MacdSlow`, `MacdSignal`, `StochasticK`, `StochasticD`, `StochasticSlowing`, `StochasticHigh`, `StochasticLow`, `MomentumPeriod`, `MomentumHigh`, `MomentumLow`, `SlowMaPeriod`, `FastMaPeriod`, `MaMode`, `MaPrice` |

## Notas e suposições
- Parabolic SAR comparações usam o preço de fechamento da vela para aproximar verificações de compra/venda
do MT4. Isso mantém a lógica determinística em dados históricos.
- A gestão de dinheiro requer uma carteira conectada para obter patrimônio atual; caso contrário
o volume fixo é usado.
- As combinações de indicadores são processadas apenas em velas concluídas, evitando
sinais em dados parciais.

## Arquivos
- `CS/FarhadHillVersion2Strategy.cs` – Implementação da estratégia em C#.
- `README.md` – Este documento.
- `README_ru.md` – Tradução russa.
- `README_zh.md` – tradução chinesa.
