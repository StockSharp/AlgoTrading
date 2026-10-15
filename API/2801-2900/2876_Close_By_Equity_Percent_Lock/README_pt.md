# Estratégia de Bloqueio de Capital por Percentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- **Categoria**: Gestão de risco / automação em nível de conta.
- **Fonte original**: Consultor especialista MQL5 "Close by Equity Percent" (#20880).
- **Objetivo**: Monitorar o capital da conta em relação ao último saldo plano e liquidar todas as posições abertas quando o capital crescer para um múltiplo configurável desse saldo.
- **Instrumentos**: Quaisquer títulos já negociados por outras estratégias ou traders manuais dentro do mesmo portfólio.

## Ideia central
O consultor especialista MQL original compara o capital atual da conta com o saldo da conta (que só muda após as posições estarem planas). Quando o capital atinge ou excede `Balance * EquityPercentFromBalance`, o script fecha todas as posições abertas para garantir ganhos. Este port do StockSharp mantém a mesma lógica de proteção de conta enquanto se integra com a API de estratégia de alto nível.

## Como funciona
1. Quando a estratégia inicia, ela faz uma foto do valor atual do portfólio. Isso serve como referência de "saldo" enquanto a conta está plana.
2. A estratégia se inscreve em velas de 1 minuto (configurável através de `CandleType`) no `Security` configurado. O fluxo de velas é usado apenas como temporizador para acionar verificações de capital.
3. Em cada vela concluída:
   - Se todas as posições estiverem planas, o snapshot do saldo é atualizado para o valor mais recente do portfólio.
   - O capital atual (`Portfolio.CurrentValue`) é comparado com `balanceSnapshot * EquityPercentFromBalance`.
   - Quando o capital atinge ou excede o limite, cada posição aberta no portfólio é fechada via `ClosePosition(position.Security)`.
4. O snapshot do saldo é atualizado novamente após todas as posições serem fechadas, permitindo que o ciclo reinicie.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------ | --------- |
| `EquityPercentFromBalance` | decimal | 1.20 | Múltiplo de capital que deve ser atingido antes de liquidar todas as posições. O valor `1.20` significa "fechar tudo quando o capital for 120% do último saldo plano". |
| `CandleType` | `DataType` | Vela de período de 1 minuto | Fluxo de dados usado apenas para acionar verificações periódicas de capital. Ajuste para corresponder à cadência que você prefere para monitorar o capital. |

## Notas de implementação
- Usa `Strategy.ClosePosition(Security)` para cada posição aberta, espelhando o loop `PositionClose` na versão MQL.
- Rastreia o snapshot do saldo apenas após todas as posições estarem planas, reproduzindo como o script MQL dependia de `AccountBalance` (que se atualiza após as posições serem fechadas).
- A estratégia é em nível de conta: ela não abre posições por conta própria e tentará fechar **todas** as posições no portfólio conectado independentemente do símbolo.
- Requer que tanto `Portfolio` quanto `Security` estejam atribuídos antes de iniciar. O instrumento é usado apenas para se inscrever em velas que fornecem eventos de temporização.

## Diretrizes de uso
1. Anexe a estratégia ao portfólio que deseja proteger e defina o `Security` cujo fluxo de velas você deseja usar como temporizador (por exemplo, um instrumento altamente líquido).
2. Ajuste `EquityPercentFromBalance` para o múltiplo de realização de lucro que se adapta ao seu plano de risco.
3. Inicie a estratégia. Sempre que o capital atingir o múltiplo especificado do último saldo plano, todas as posições abertas no portfólio serão fechadas automaticamente.
4. Após a liquidação, o snapshot do saldo é atualizado, então o próximo ciclo de lucro aguardará novamente que o capital cresça pela porcentagem configurada antes de acionar outro fechamento.

## Exemplo prático
- Snapshot de saldo inicial = 10.000 USD.
- `EquityPercentFromBalance = 1.2` → capital alvo = 12.000 USD.
- Posições abertas se valorizam até o capital atingir 12.050 USD.
- Estratégia fecha todas as posições abertas; snapshot do saldo é atualizado quando o portfólio estiver plano (por exemplo, para 12.000 USD).
- O próximo ciclo aguarda o capital superar 12.000 * 1.2 = 14.400 USD antes de agir novamente.
