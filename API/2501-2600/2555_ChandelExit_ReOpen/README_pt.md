# Estratégia ChandelExit de Reabertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o especialista do MetaTrader "Exp_ChandelExitSign_ReOpen" para a API de alto nível do StockSharp. Opera rompimentos usando as bandas do Chandelier Exit e reabre posições automaticamente quando a tendência continua. O sistema reage a sinais de indicadores calculados em um período superior configurável enquanto gerencia o risco com stops baseados em ATR e níveis opcionais de take-profit.

A ideia central é tratar o Chandelier Exit tanto como filtro de tendência quanto como barreira de trailing dinâmico. Quando a banda inferior cruza acima da superior, um impulso de alta é detectado; quando o oposto acontece, um impulso de baixa aparece. A estratégia pode funcionar simetricamente nos lados comprado e vendido, e cada sinal pode ser habilitado ou desabilitado individualmente por parâmetros. Uma vez em posição, o preço deve avançar um número de passos de preço (`PriceStepPoints`) antes que uma ordem adicional seja permitida. Os acréscimos imitam o comportamento do consultor especialista original e são limitados por `MaxAdditions` para evitar tamanhos de posição descontrolados.

## Lógica de trading

- **Cálculo de sinais**
  - `RangePeriod` barras (deslocadas por `Shift`) definem a máxima mais alta e a mínima mais baixa usadas pelas bandas do Chandelier Exit.
  - `AtrPeriod` junto com `AtrMultiplier` produzem um buffer de volatilidade que afasta as bandas de saída do preço.
  - `SignalBar` (padrão 1) atrasa a execução para que a estratégia atue na vela finalizada anterior, replicando a implementação do MT5.
- **Entradas**
  - **Comprado**: acionado quando a banda inferior cruza acima da superior (`IsUpSignal`). Requer `EnableBuyEntries = true`. Se uma posição vendida existe, a estratégia primeiro tenta zerar quando `EnableSellExits = true`.
  - **Vendido**: acionado quando as bandas cruzam na direção oposta (`IsDownSignal`) e `EnableSellEntries = true`. Posições compradas existentes são fechadas apenas se `EnableBuyExits = true`.
- **Saídas**
  - Posições **compradas** fecham em sinais de baixa quando `EnableBuyExits = true`, ou quando stops/alvos protetores são atingidos.
  - Posições **vendidas** fecham em sinais de alta quando `EnableSellExits = true`, ou através de níveis protetores.
  - A estratégia também verifica valores mais antigos do indicador quando ambos os toggles de entrada e saída estão habilitados para garantir que um sinal de fechamento esteja disponível mesmo que a vela mais recente tenha produzido apenas uma entrada.
- **Reentrada / escalonamento**
  - Após cada entrada, o último preço de preenchimento é armazenado. Quando o preço se move a favor por pelo menos `PriceStepPoints * PriceStep`, uma ordem adicional de tamanho `Volume` é enviada, até `MaxAdditions` vezes.
  - Cada acréscimo redefine os cálculos de stop/take para o preenchimento mais recente para que a proteção permaneça próxima à exposição mais recente.
- **Gestão de risco**
  - `StopLossPoints` e `TakeProfitPoints` expressam distâncias em passos de preço a partir do último preenchimento. Stops e alvos são opcionais; definir como zero para desativar.
  - Todas as verificações protetoras são executadas em cada vela finalizada. Se o preço viola um stop ou alvo intrabar, a posição é fechada a mercado.

## Parâmetros padrão

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | `TimeSpan.FromHours(4).TimeFrame()` | Período usado para cálculos do indicador. |
| `RangePeriod` | 15 | Janela de observação para a máxima mais alta / mínima mais baixa. |
| `Shift` | 1 | Número de barras recentes ignoradas antes de calcular o intervalo. |
| `AtrPeriod` | 14 | Comprimento ATR para o buffer de volatilidade. |
| `AtrMultiplier` | 4 | Multiplicador ATR aplicado ao buffer. |
| `SignalBar` | 1 | Quantas barras completadas atrás ler o sinal. |
| `PriceStepPoints` | 300 | Movimento favorável mínimo em passos de preço antes de adicionar à operação. |
| `MaxAdditions` | 10 | Número máximo de ordens adicionais após a entrada inicial. |
| `StopLossPoints` | 1000 | Distância do stop-loss em passos de preço. |
| `TakeProfitPoints` | 2000 | Distância do take-profit em passos de preço. |
| `EnableBuyEntries` / `EnableSellEntries` | `true` | Permitir abertura de operações compradas/vendidas em sinais. |
| `EnableBuyExits` / `EnableSellExits` | `true` | Permitir fechamento de operações compradas/vendidas em sinais opostos. |

## Notas práticas

- A estratégia depende de `Volume` para definir o tamanho base da ordem. Operações adicionais reutilizam o mesmo tamanho. Ajustar `Volume` ou `MaxAdditions` para atender aos limites de risco.
- Como as reentradas requerem um movimento expresso em passos de preço, garantir que os metadados do instrumento (`PriceStep`) estejam configurados corretamente. Instrumentos com grandes valores de ponto podem precisar de padrões diferentes.
- `SignalBar` pode ser definido como zero para atuar na vela completa mais recente, mas o especialista original usou um atraso de uma barra para evitar agir na vela que gerou o sinal.
- Iniciar a estratégia em uma combinação de símbolo/portfólio que suporte operações compradas e vendidas. Usar os toggles de parâmetros integrados para restringi-la a uma direção se necessário.
- Helpers de gráfico (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) ativam-se automaticamente quando uma área de gráfico está disponível, facilitando a visualização de bandas e preenchimentos.

## Exemplo de fluxo de trabalho

1. Aguardar um cruzamento de alta: a banda inferior rompe acima da banda superior na vela de período superior.
2. Se não existe posição e as entradas compradas estão habilitadas, colocar uma ordem de compra a mercado de tamanho `Volume`. Stops e alvos são definidos em relação ao preço de preenchimento.
3. Se o preço sobe pelo menos `PriceStepPoints` * `PriceStep`, enviar uma ordem de compra adicional (respeitando `MaxAdditions`).
4. Fechar todo o comprado quando um sinal de baixa aparecer, quando o stop-loss for atingido ou quando o take-profit for alcançado. O processo é simétrico para operações vendidas.

Esta documentação reflete a estratégia MT5 original enquanto adota as convenções do StockSharp, como parâmetros de estratégia, assinaturas de velas de alto nível e gerenciamento explícito de posições.
