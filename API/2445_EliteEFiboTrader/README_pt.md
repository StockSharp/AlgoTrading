# Estratégia Elite eFibo Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Elite eFibo Trader** é uma conversão do expert advisor MQL5 "Elite eFibo Trader". Ela implementa uma grade de médias baseada em Fibonacci que abre uma posição inicial de mercado e camadas ordens stop adicionais a distâncias fixas. A estratégia opera com dados de tick e gerencia automaticamente os trailing stops conforme a grade se expande.

## Como funciona
1. Quando não há posições ou ordens pendentes ativas e o trading está permitido, a estratégia inicia um novo ciclo na direção selecionada (compra ou venda).
2. A primeira ordem é enviada a mercado usando o volume configurado para `LotsLevel1`. Treze ordens stop adicionais são colocadas em múltiplos de `LevelDistance` a partir do preço atual. Seus volumes seguem a sequência de Fibonacci definida por `LotsLevel2` … `LotsLevel14`.
3. Cada ordem executada define um nível de stop individual a `StopLossPoints` do preço de entrada. O mais alto (para posições compradas) ou mais baixo (para posições vendidas) desses stops torna-se o nível de trailing ativo para todas as posições abertas.
4. Se o preço atingir o nível de trailing, a posição inteira é fechada e todas as ordens pendentes restantes são canceladas.
5. O lucro não realizado é monitorado na moeda da conta. Quando atingir `MoneyTakeProfit`, a grade é fechada. Dependendo de `TradeAgainAfterProfit`, a estratégia reinicia automaticamente ou aguarda reativação manual.

A estratégia requer dados de mercado em nível de tick via `SubscribeTrades()` e espera que apenas uma direção (`OpenBuy` xor `OpenSell`) esteja habilitada por vez.

## Parâmetros
- `OpenBuy` – habilita a versão somente comprado da grade.
- `OpenSell` – habilita a versão somente vendido da grade.
- `TradeAgainAfterProfit` – inicia automaticamente um novo ciclo após a realização de lucros.
- `LevelDistance` – espaçamento entre ordens pendentes, medido em passos de preço do instrumento.
- `StopLossPoints` – distância do stop-loss de cada entrada, medida em passos de preço.
- `MoneyTakeProfit` – meta de lucro não realizado expresso em moeda da conta.
- `LotsLevel1` … `LotsLevel14` – volumes individuais para cada nível da grade. Os valores padrão seguem a sequência de Fibonacci (1, 1, 2, 3, 5, …, 377).

## Detalhes da lógica de trading
- Os deslocamentos de preço são calculados com o `PriceStep` do instrumento; se for zero, a estratégia não colocará ordens.
- Apenas um ciclo de trading está ativo por vez. Todas as ordens pendentes são criadas no início do ciclo e permanecem até serem executadas ou explicitamente canceladas.
- Os trailing stops são recalculados sempre que um novo nível da grade é preenchido ou partes da posição são fechadas. Isso garante que todas as ordens compartilhem o melhor nível de proteção disponível.
- O controle de lucros é baseado no PnL flutuante derivado de `Position`, `PositionPrice`, `PriceStep` e `StepPrice`.
- Quando `TradeAgainAfterProfit` está desabilitado, a estratégia permanece inativa após atingir a meta monetária até que o parâmetro seja reativado manualmente.

## Notas de uso
- Configure a direção correta antes de iniciar (comprado ou vendido). Habilitar ambas as direções simultaneamente impede o lançamento da grade.
- Ajuste as distâncias entre níveis e os volumes de acordo com a volatilidade do instrumento e o tamanho do contrato. Volumes grandes de Fibonacci criam escalonamento agressivo e devem ser testados cuidadosamente com dados históricos.
- Certifique-se de que a conta de trading e o corretor suportem ordens stop nos níveis de preço calculados; caso contrário, as ordens podem ser rejeitadas.
