# Estratégia TakeProfitTimeGuardStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`TakeProfitTimeGuardStrategy` emula o comportamento do especialista MetaTrader `Exp_GTakeProfit_Tm` supervisionando o lucro no nível da conta e forçando um estado de posição plana fora de um horário de negociação configurável. A estratégia não abre posições por conta própria. Em vez disso, serve como uma camada de gerenciamento de risco sobreposta que fecha automaticamente qualquer exposição existente assim que o objetivo de lucro é atingido ou quando a negociação deve ser interrompida fora do intervalo de tempo permitido.

## Lógica principal

- Assina um fluxo de velas configurável (padrão 1 minuto) para avaliar o PnL realizado e não realizado usando o preço de fechamento mais recente.
- Calcula o **lucro total** como a soma do PnL realizado (`Strategy.PnL`) e o PnL flutuante derivado do preço médio de posição atual.
- Ignora perdas enquanto a janela de negociação está aberta, espelhando o comportamento original do consultor especializado.
- Uma vez que o **alvo de take-profit** é atingido, define um sinalizador de stop interno e liquida repetidamente qualquer posição restante até que a conta esteja plana. O sinalizador de stop é redefinido após o portfólio retornar à posição zero.
- Quando a **janela de negociação** opcional está habilitada, a estratégia fecha todas as posições sempre que o tempo atual cai fora do intervalo permitido, também aguardando até que o livro esteja plano antes de reabilitar a negociação.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `CandleType` | `DataType` | período de 1 minuto | Série de velas usada para avaliar a lógica de lucro e horário. |
| `TargetMode` | `ProfitTargetModes` (`Percent`/`Currency`) | `Percent` | Seleciona se `TakeProfitValue` é interpretado como porcentagem do capital da conta ou como valor absoluto em moeda. |
| `TakeProfitValue` | `decimal` | `100` | Limite do alvo de lucro. Interpretado de acordo com `TargetMode`. Deve ser maior que zero. |
| `UseTradingWindow` | `bool` | `true` | Habilita ou desabilita o filtro de tempo. |
| `StartTime` | `TimeSpan` | `00:00:00` | Início da janela de negociação permitida (inclusive). |
| `EndTime` | `TimeSpan` | `23:59:00` | Fim da janela de negociação permitida. Quando o horário de início é maior que o horário de fim, a janela abrange a meia-noite. |

## Notas de comportamento

1. O valor inicial do portfólio é capturado quando a estratégia inicia (ou na primeira atualização se o valor era zero) e é usado como referência para o alvo percentual.
2. A estratégia calcula o PnL flutuante usando o preço de fechamento do eixo de vela mais recente; os resultados dependem da granularidade de vela selecionada.
3. Se o alvo de lucro for atingido, a estratégia continua enviando ordens de mercado para achatar a posição até que o livro esteja vazio. Registra o motivo do fechamento do livro.
4. Quando `UseTradingWindow` está habilitado e o relógio está fora da janela, a mesma rotina de achatamento é executada mesmo que o alvo de lucro não tenha sido atingido.
5. O sinalizador de stop (`_stop`) só é limpo após a posição retornar a zero, permitindo que a negociação seja retomada quando as condições permitirem.

## Diferenças em relação à estratégia MQL original

- Usa a API de alto nível do StockSharp (`SubscribeCandles`) em vez de manipuladores por tick.
- Calcula o lucro flutuante a partir do preço médio de posição exposto por `Strategy.PositionPrice`.
- Registra eventos de take-profit para monitoramento mais fácil.
- A comparação de tempo é baseada em `DateTimeOffset.CloseTime` das velas assinadas.

## Dicas de uso

- Anexe a estratégia a um portfólio que já executa outra estratégia de negociação para atuar como camada de guarda.
- Escolha um período de vela que corresponda à capacidade de resposta necessária para avaliação de lucros (por exemplo, 1 minuto para controle rápido).
- Certifique-se de que as informações do portfólio (especialmente `CurrentValue`) estejam disponíveis; caso contrário, defina um saldo inicial explícito antes de executar alvos percentuais.
- A estratégia pode ser combinada com `StartProtection()` em outra estratégia primária para adicionar controles de risco adicionais.
