# Estratégia de Temporizador de Stop Global
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Temporizador de Stop Global é uma camada de gerenciamento de risco convertida do especialista MetaTrader `Exp_GStop_Tm`.
Ela monitora continuamente o valor do portfólio em cada vela concluída e interrompe a negociação assim que um objetivo de lucro global
ou limite de perda é atingido. Além disso, pode restringir a negociação a uma janela de tempo definida pelo usuário e forçar o fechamento de todas as posições abertas
sempre que a janela estiver fechada.

## Como funciona

- Quando a estratégia inicia, ela registra o saldo inicial do portfólio como ponto de referência.
- Cada vez que a série de velas assinada fecha, a estratégia lê o valor atual do portfólio e calcula a
  diferença em relação ao saldo inicial.
- Dependendo do `StopCalculationMode` selecionado, a diferença é convertida para porcentagem ou deixada como moeda.
- Se a perda exceder `StopLoss` ou o lucro exceder `TakeProfit`, a estratégia entra no estado parado, registra o
  evento e envia ordens de mercado para fechar qualquer posição restante.
- Quando a janela de negociação opcional está habilitada e o tempo atual sai da janela, a estratégia também tenta
  achatar a posição. Uma vez que o tamanho da posição se torna zero, o sinalizador de stop é redefinido, permitindo que a negociação seja retomada dentro
  da próxima janela válida.

A estratégia nunca abre novas posições por conta própria. É projetada para supervisionar outras estratégias ou trades manuais e para
proteger a conta de drawdown excessivo ou para assegurar lucros em toda a conta.

## Lógica da janela de negociação

A janela de negociação replica a lógica original do especialista:

- Se a hora de início for menor que a hora de fim, a negociação é permitida entre o minuto de início (inclusive) e o minuto de fim (exclusivo) no mesmo dia.
- Se a hora de início e fim forem iguais, a negociação é permitida apenas quando o minuto atual está entre `StartMinute`
  (inclusive) e `EndMinute` (exclusivo).
- Se a hora de início for maior que a hora de fim, a sessão se estende pela meia-noite. A negociação é habilitada desde o início
  até a meia-noite e é retomada da meia-noite até o fim no dia seguinte.

## Parâmetros

- `StopCalculationMode` – escolher entre stops globais baseados em porcentagem ou moeda.
- `StopLoss` – limite de perda global. Tratado como porcentagem quando o modo percentual está ativo, caso contrário como moeda da conta.
- `TakeProfit` – alvo de lucro global. Usa a mesma unidade que `StopLoss`.
- `UseTradingWindow` – habilitar ou desabilitar o filtro de sessão.
- `StartHour` / `StartMinute` – horário de início da janela de negociação permitida.
- `EndHour` / `EndMinute` – horário de fechamento da janela de negociação permitida.
- `CandleType` – série de velas que define com que frequência o estado da conta é avaliado.

## Notas

- Como as verificações de stop ocorrem no fechamento da vela, usar um período pequeno (por exemplo, um minuto) quando reação rápida é
  necessária.
- A estratégia fecha apenas a posição gerenciada por esta instância de estratégia. Executar instâncias separadas se múltiplos
  valores precisam de supervisão individual.
- Usar junto com outras estratégias de negociação, anexando-a como estratégia pai ou executando-a no mesmo instrumento para
  fornecer proteção em nível de conta.
