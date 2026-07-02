# MultiMartinEstratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`MultiMartinStrategy` é a conversão StockSharp do consultor especialista MQL5 **MultiMartin**. O robô original é um martingale multimoedas que alterna negociações longas e curtas em sinais de reversão e aumenta o tamanho do pedido após perder negócios. Esta porta mantém a lógica central de gerenciamento de dinheiro enquanto usa o StockSharp de alto nível do API para roteamento de pedidos, monitoramento de posição, trailing stops opcionais e tratamento de rejeição do corretor.

A estratégia abre continuamente uma posição de mercado única no instrumento configurado. Após cada saída, ele mantém a direção (se a negociação for lucrativa) ou inverte a direção (se a negociação perder dinheiro). As negociações perdidas acionam uma etapa de martingale que multiplica o próximo volume de pedido até que um teto configurável seja atingido.

## Lógica de negociação

1. **Seleção de entrada**
   - A estratégia utiliza um filtro de tempo para limitar a negociação a uma janela intradiária. Fora desta janela nenhuma nova entrada será enviada.
   - Quando nenhuma posição está aberta e a corretora não está em estado de espera, a estratégia envia uma ordem de mercado na direção atual. A primeira direção é definida pelo usuário (compra ou venda).
2. **Martingale dimensionamento**
   - Após cada perda, o próximo volume do pedido é multiplicado pelo parâmetro `Factor`.
   - A multiplicação é limitada por `Limit`, que define o número máximo de duplicações consecutivas. Quando o limite é excedido, o volume é redefinido para a base `Volume`.
   - As negociações lucrativas sempre redefinem o volume para o tamanho base e mantêm a direção da negociação.
3. **Gerenciamento de saídas**
   - As distâncias de stop-loss e take-profit são expressas em pontos de preço e convertidas em distâncias absolutas usando o instrumento `PriceStep`.
   - Os modos de rastreamento opcionais movem o stop loss para o ponto de equilíbrio ou o acompanham linearmente atrás do preço.
   - As saídas são tratadas por ordens de mercado quando os extremos da vela ultrapassam o limite de stop ou take.
4. **Tratamento de rejeição do corretor**
   - Se um pedido for rejeitado, a estratégia entra em um período de espera controlado por `SkipBadTime`. Durante o tempo de espera, nenhuma nova entrada é tentada. A opção `Forever` desativa a negociação pelo restante da sessão.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `UseTimeFilter` | Habilite ou desabilite a janela de negociação intradiária. |
| `HourStart` | Hora inclusiva (0-23) quando a negociação se torna ativa. |
| `HourEnd` | Hora exclusiva (1-24) quando a negociação é interrompida. Suporta janelas noturnas (por exemplo, 22-2). |
| `Volume` | Volume base do pedido em lotes ou contratos. |
| `Factor` | Multiplicador aplicado ao próximo volume de pedido após uma negociação perdida. |
| `Limit` | Número máximo de multiplicações consecutivas antes do volume ser reiniciado. |
| `StopLossPoints` | Distância de stop-loss expressa em pontos do instrumento. Defina como 0 para desativar a parada. |
| `TakeProfitPoints` | Distância de take-profit expressa em pontos de instrumento. Defina como 0 para desabilitar o alvo. |
| `StartDirection` | Primeira direção comercial (`Buy` ou `Sell`). |
| `SkipBadTime` | Intervalo de espera aplicado após uma ordem de mercado rejeitada. `Forever` bloqueia outras entradas. |
| `TrailMode` | Modo de rastreamento: `None`, `Breakeven` ou `Straight` (rastreamento linear). |
| `CandleType` | Série de velas usada para gerenciar saídas e filtragem de tempo. |

## Diferenças em relação à versão MQL5

- A porta StockSharp negocia um único título por instância de estratégia. Inicie várias instâncias para cobrir vários símbolos.
- A gestão de stop-loss e take-profit é baseada em velas; os preenchimentos são executados com ordens de mercado assim que o intervalo da vela atinge os limites.
- As rejeições do corretor usam o retorno de chamada `OnOrderFailed` de `OnOrderFailed` para acionar o resfriamento de `SkipBadTime` em vez do cronômetro global de MQL5.
- As opções de trailing stop foram reimplementadas usando lógica de nível de estratégia em vez de chamadas diretas de modificação de ordem.

## Notas de uso

- Configure o `Security` e o `Portfolio` antes de iniciar a estratégia.
- Certifique-se de que `Volume` seja compatível com o tamanho do lote do instrumento e as regras de volume fracionário.
- Defina `StopLossPoints`/`TakeProfitPoints` como zero para desativar as respectivas ordens de proteção.
- Ao fazer backtesting, escolha um tipo de vela que corresponda ao conjunto de dados históricos (por exemplo, velas de 1 minuto para pares forex).
- Para simular o comportamento original de vários símbolos, implemente múltiplas instâncias de estratégia com diferentes valores mobiliários e parâmetros.

## Avisos de risco

Martingale a gestão de dinheiro é inerentemente arriscada. Sequências de derrotas podem aumentar exponencialmente a exposição e consumir a margem disponível rapidamente. Use configurações de volume conservadoras, teste dados históricos e aplique controles de risco rigorosos antes de usar a estratégia em produção.
