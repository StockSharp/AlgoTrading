# Estratégia de grade semanal RangeEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

RangeEA Weekly Grid Strategy é um sistema de grade de ordem limite convertido do consultor especialista MetaTrader original. O algoritmo identifica a corrente
faixa de negociação semanal e a preenche com um número configurável de ordens com limite pendentes. Cada pedido usa stop-loss dinâmico e
compensações de take-profit escalonadas em relação à distância entre o preço limite e o preço de mercado atual, respeitando também
distâncias mínimas expressas em pontos. Os lucros podem ser bloqueados fechando todo o livro assim que o patrimônio do portfólio crescer
porcentagem predefinida.

A implementação aproveita o API de alto nível de API: velas conduzem a lógica de decisão, pedidos pendentes são gerenciados com o
métodos auxiliares de estratégia e controles de risco são expostos como parâmetros prontos para otimização.

## Lógica de negociação

1. Assine dois streams de velas:
   - Um período de tempo definido pelo usuário (1 hora por padrão) que orienta a manutenção da rede.
   - Velas semanais que são usadas para estimar a faixa de negociação atual.
2. Para cada vela semanal concluída, atualize a máxima mais alta e a mínima mais baixa das últimas duas semanas. A diferença deles torna-se
a faixa de negociação ativa.
3. Em cada vela de negociação finalizada:
   - Respeite a janela de negociação configurada (`StartTradeHour` a `EndTradeHour`).
   - Opcionalmente, redefina a grade no início de cada dia de negociação.
   - Se não existirem ordens com limite pendentes, distribua as novas ordens uniformemente entre o intervalo mínimo e o intervalo máximo.
   - Depois que duas ordens já tiverem sido executadas, substitua o penúltimo preenchimento por uma nova ordem com o mesmo preço quando a grade
diminui para `NumberOfOrders - 2` itens.
   - Monitore continuamente o patrimônio da conta e liquide tudo quando o percentual de lucro configurado for atingido.
4. Quando a janela de negociação fechar e `CloseAllAtEndTrade` estiver ativado, cancele todas as ordens pendentes e saia das posições existentes.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `CandleType` | Prazo de negociação usado para acionar a manutenção da rede. | Velas de 1 hora |
| `WeeklyCandleType` | Período usado para derivar os limites do intervalo. | velas de 1 semana |
| `StartTradeHour` | Hora do dia em que novos pedidos podem ser feitos. | 0 |
| `EndTradeHour` | Hora do dia em que a negociação é interrompida. | 24 |
| `CloseAllAtEndTrade` | Feche todas as ordens e posições fora da janela de negociação. | verdade |
| `MaxOpenOrders` | Número máximo de ordens e posições simultâneas. | 5 |
| `NumberOfOrders` | Número de ordens limitadas na grade. | 10 |
| `OrderVolume` | Volume utilizado para cada pedido. | 0,01 |
| `ResetOrdersDaily` | Reconstrua a grade no início de cada dia de negociação. | verdade |
| `StopLossPoints` | Distância mínima de stop-loss em pontos. | 60 |
| `TakeProfitPoints` | Distância mínima de take-profit em pontos. | 60 |
| `StopLossMultiplier` | Multiplicador aplicado à distância dinâmica de stop-loss. | 3 |
| `TakeProfitMultiplier` | Multiplicador aplicado à distância dinâmica do take-profit. | 1 |
| `TargetPercentage` | Porcentagem de ganho patrimonial que desencadeia a liquidação. | 8 |

## Gestão de risco

- A estratégia respeita o limite `MaxOpenOrders` para manter o número de ordens e posições ativas sob controle.
- Os níveis de stop-loss e take-profit estão sempre a pelo menos o número configurado de pontos de distância da entrada e podem opcionalmente ser
estendido pelos parâmetros do multiplicador.
- A opção de redefinição diária evita que pedidos obsoletos sejam transferidos para uma nova sessão.
- Uma meta de patrimônio no nível do portfólio permite que a estratégia obtenha lucros ao nivelar a carteira.

## Notas

- Certifique-se de que o título selecionado forneça velas semanais; caso contrário, a estratégia não poderá calcular o intervalo.
- Ao usar instrumentos com etapas de preços não padronizadas, ajuste as configurações baseadas em pontos para corresponder ao tamanho do tick subjacente.
- Otimizar `NumberOfOrders`, `OrderVolume` e os multiplicadores stop/take ajuda a adaptar a grade a diferentes níveis de
volatilidade.
