# Estratégia Freeman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Freeman é uma estratégia intradiária que combina vários filtros de momentum para escalar em tendências. Ela usa dois "mestres" RSI impulsionados por médias móveis no período de trading junto com um filtro de média móvel de período superior. O risco é controlado com alvos de stop-loss e take-profit baseados em ATR mais um trailing stop baseado em pips.

## Visão geral da estratégia

- Funciona em qualquer vela do período selecionado pelo parâmetro `CandleType` (15 minutos por padrão).
- Usa um filtro horário (`FilterCandleType`) para qualificar tendências antes de aceitar sinais.
- Constrói sinais comprados e vendidos a partir de dois blocos RSI que comparam valores atuais e anteriores em combinação com inclinações de médias móveis.
- Permite piramidação quando o mercado continua se movendo, com a opção de ampliar a próxima ordem após uma saída com prejuízo.

## Lógica de trading

### Condições comprado

1. O filtro de período superior é opcional. Quando habilitado, a média móvel horária deve inclinar para cima.
2. RSI Mestre #1 está ativo quando:
   - RSI #1 estava abaixo de `RsiSellLevel` na barra anterior e sobe na barra atual.
   - A média móvel rápida sobe.
   - O RSI horário (período 14) permanece abaixo de `RsiBuyLevel` para confirmar que o período superior não está sobrecomprado.
3. RSI Mestre #2 está ativo quando:
   - RSI #2 estava abaixo de `RsiSellLevel2` e gira para cima.
   - A média móvel lenta sobe.
   - O RSI horário permanece abaixo de `RsiBuyLevel2`.
4. Uma entrada comprada é tomada quando pelo menos um mestre está ativo e o filtro de tendência (se habilitado) concorda.
5. Entradas compradas adicionais requerem que o preço de fechamento se mova mais de `DistancePips` (convertido pelo passo de preço do instrumento) do último preenchimento comprado. Quando a última saída comprada foi um prejuízo, o volume é multiplicado por `LockCoefficient` para imitar o comportamento de bloqueio do MT5.

### Condições vendido

Espelha a lógica comprada com comparações invertidas:

- A média móvel do período superior deve declinar quando o filtro está habilitado.
- RSI Mestre #1 precisa de RSI #1 acima de `RsiBuyLevel` caindo, a MA rápida caindo e o RSI horário acima de `RsiSellLevel`.
- RSI Mestre #2 precisa de RSI #2 acima de `RsiBuyLevel2` caindo, a MA lenta caindo e o RSI horário acima de `RsiSellLevel2`.
- Entradas vendidas adicionais seguem as mesmas regras de distância e bloqueio.

## Gestão de posições

- Stop-loss e take-profit são recalculados para cada entrada a partir do valor ATR atual usando `StopLossAtrFactor` e `TakeProfitAtrFactor`.
- O trailing stop se ativa uma vez que o preço viaja além de `TrailingStopPips + TrailingStepPips` e então bloqueia lucros mantendo o stop a `TrailingStopPips` do último fechamento.
- As saídas são executadas com ordens de mercado quando o máximo/mínimo da vela ultrapassa os níveis de stop ou alvo calculados.
- O parâmetro `PositionsMaximum` limita o número total de entradas preenchidas (comprado mais vendido). Um valor de zero remove o limite.

## Filtros de tempo

- O trading às sextas-feiras pode ser desabilitado através de `TradeOnFriday`.
- `StartHour` e `EndHour` definem uma janela de sessão opcional em tempo de bolsa; valores zero mantêm o mercado aberto o dia todo.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Período de trading usado para a lógica de sinal principal. |
| `FilterCandleType` | Período superior para o filtro de média móvel e RSI (padrão 1 hora). |
| `FirstMaPeriod` / `SecondMaPeriod` | Períodos para as médias móveis rápidas e lentas que alimentam os mestres RSI. |
| `FilterMaPeriod` | Comprimento da média móvel do período superior. |
| `MaType` | Tipo de média móvel (SMA, EMA, SMMA ou WMA). |
| `RsiFirstPeriod` / `RsiSecondPeriod` | Períodos dos dois mestres RSI. |
| `RsiSellLevel`, `RsiBuyLevel`, `RsiSellLevel2`, `RsiBuyLevel2` | Limites RSI que controlam os blocos de mestres. |
| `UseRsiTeacher1`, `UseRsiTeacher2`, `UseTrendFilter` | Alternadores para cada componente. |
| `StopLossAtrFactor`, `TakeProfitAtrFactor` | Multiplicadores ATR para distâncias de stop-loss e take-profit. |
| `TrailingStopPips`, `TrailingStepPips` | Offsets em pips para o motor de trailing stop. |
| `PositionsMaximum` | Número máximo de entradas combinadas; zero = ilimitado. |
| `DistancePips` | Distância mínima em pips antes de adicionar a uma posição. |
| `TradeOnFriday` | Habilitar ou desabilitar sinais às sextas-feiras. |
| `StartHour`, `EndHour` | Limites opcionais da sessão de trading. |
| `LockCoefficient` | Multiplicador de volume usado após uma saída com prejuízo ao acumular na mesma direção. |
| `SignalShift` | Offset aplicado ao ler valores de indicadores (0 = barra terminada atual). |

## Notas de implementação

- O porte do StockSharp processa apenas velas terminadas, correspondendo ao comportamento "Bars Control" do MT5, mesmo quando o original permitia trading baseado em ticks.
- Distâncias de preço expressas em pips são convertidas usando o `PriceStep` do instrumento.
- A lógica de proteção (stop, alvo, trailing) fecha posições com ordens de mercado porque são usados bindings de API de alto nível em vez de modificações de posição MT5 individuais.
- A estratégia mantém volumes comprados e vendidos agregados; uma vez que um lado é fechado, o rastreamento de perdas é reiniciado para que o próximo sinal se comporte como as regras de bloqueio originais.

Use controles de risco apropriados e teste exaustivamente antes de implantar em mercados ao vivo.
