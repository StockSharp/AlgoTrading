# Estratégia Surefirething
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Surefirething recria o clássico assessor especialista MetaTrader 5 que coloca ordens limite simétricas de compra e venda ao redor do fechamento da vela mais recente. O sistema reconstrói constantemente a grade após cada vela completada, gerencia stops de proteção em unidades de pip e força uma posição completamente plana dez minutos antes da meia-noite, horário do servidor.

## Processamento de velas
- Funciona com um tipo de vela configurável (padrão: período de 1 hora).
- Após cada vela terminada, a estratégia calcula um intervalo amplificado: `range = (high - low) * 1.1`.
- Ela deriva dois níveis de rompimento desse intervalo:
  - `L4 = close - range / 2` para a ordem limite de compra.
  - `H4 = close + range / 2` para a ordem limite de venda.
- As ordens pendentes existentes são canceladas antes de publicar a nova grade, de modo que apenas uma ordem limite de compra e uma de venda permanecem ativas.

## Gerenciamento de ordens
- A ordem limite de compra em `L4` e a ordem limite de venda em `H4` são registradas com o volume de ordem configurado.
- Uma vez que uma posição é aberta, a ordem pendente oposta é cancelada imediatamente.
- Todos os dias às **23:50** (horário do servidor), a estratégia:
  - Cancela quaisquer ordens pendentes restantes.
  - Fecha a posição aberta a mercado, se houver.
  - Redefine todos os rastreadores de stop/take-profit para começar a próxima sessão de forma limpa.

## Gerenciamento de risco
- As distâncias de stop-loss e take-profit são definidas em pips e traduzidas em preços usando o passo de preço do instrumento (símbolos de 5 dígitos e 3 dígitos são ajustados automaticamente para unidades pip clássicas).
- Um trailing stop (também em pips) pode ser habilitado. Cada vez que o preço se move além de `TrailingStopPips + TrailingStepPips`, o stop avança para `preço atual - TrailingStopPips` para posições compradas ou `preço atual + TrailingStopPips` para posições vendidas.
- Ambos os níveis de proteção são monitorados em cada vela. Se a vela operar através do stop ou do alvo, a estratégia sai da posição usando ordens de mercado.

## Parâmetros
- `OrderVolume` – volume base para ambas as ordens limite (padrão: `0.1`).
- `StopLossPips` – distância do stop-loss em pips (padrão: `50`).
- `TakeProfitPips` – distância do take-profit em pips (padrão: `50`).
- `TrailingStopPips` – distância do trailing stop em pips (padrão: `25`).
- `TrailingStepPips` – movimento adicional em pips necessário antes que o trailing stop se mova (padrão: `1`). Deve ser maior que zero quando um trailing stop está habilitado.
- `CandleType` – tipo de dados de vela usado para cálculos (padrão: período de 1 hora).

## Notas
- A implementação corresponde à lógica MQL original ao garantir que o passo de trailing seja diferente de zero sempre que o trailing estiver ativo.
- Nenhuma implementação Python é fornecida para esta estratégia.
