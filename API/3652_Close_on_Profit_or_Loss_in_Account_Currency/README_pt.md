# Fechar com lucro ou perda na moeda da conta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista MetaTrader *Close_on_PROFIT_or_LOSS_inAccont_Currency*. Ele monitora continuamente o patrimônio do portfólio ao qual a estratégia está vinculada e, uma vez atingida uma meta de lucro configurada ou piso de rebaixamento, liquida todas as posições abertas e cancela todas as ordens pendentes gerenciadas pela estratégia. A classe depende do alto nível de StockSharp API: uma assinatura de vela fornece o batimento cardíaco, `CancelActiveOrders()` remove ordens de trabalho e `ClosePosition()` nivela a exposição por meio de ordens de mercado.

## Como funciona

1. A estratégia continua pesquisando o patrimônio atual (`Portfolio.CurrentValue`) sempre que uma vela de pulsação fecha.
2. Se o patrimônio for maior ou igual a **Fecho Positivo**, a estratégia envia uma solicitação de fechamento completo.
3. Se o patrimônio for menor ou igual a **Fecho Negativo**, a mesma rotina de liquidação é executada para limitar as perdas.
4. Durante a liquidação, a estratégia cancela todas as ordens pendentes, envia ordens de mercado para fechar todas as posições ativas e, finalmente, para (espelhando a chamada `ExpertRemove()` do EA original).

> **Importante:** defina os limites na moeda da conta. Para emular o comportamento original, escolha um valor de **Fecho Positivo** acima do patrimônio atual e um valor de **Fecho Negativo** abaixo dele; caso contrário, a saída será acionada imediatamente na partida.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `PositiveClosureInAccountCurrency` | Nível de patrimônio que desencadeia uma liquidação total quando excedido. | `0` |
| `NegativeClosureInAccountCurrency` | Piso patrimonial que força a liquidação quando atingido. | `0` |
| `CandleType` | Período usado para as velas de pulsação que orientam as verificações de patrimônio. Reduza-o para reações mais rápidas. | `1 minute` |

## Notas

- `StartProtection()` é ativado na inicialização para copiar o comportamento de segurança original.
- A estratégia interage apenas com posições e ordens que administra; anexe-o à carteira que contém as negociações que você deseja proteger.
- Não há entrada separada de spread/slippage porque StockSharp ordens de mercado já contabilizam custos de execução específicos do conector.
