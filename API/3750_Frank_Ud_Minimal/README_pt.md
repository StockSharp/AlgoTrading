# Estratégia Mínima de Frank Ud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo transporta o consultor especialista clássico **Frank Ud** MetaTrader para StockSharp usando a estratégia de alto nível API. O script MQL original executa uma grade martingale protegida que continua adicionando posições sempre que o preço se move em relação à entrada mais recente. Os lucros são bloqueados quando a ordem mais recente (e, portanto, maior) ganha um número fixo de pips, após o qual *todas* as negociações desse lado são fechadas simultaneamente.

## Lógica central

1. **Hedge simétrico.** A estratégia mantém duas escadas independentes de posições de mercado: uma escada longa e uma escada curta. Portanto, é possível manter posições longas e curtas ao mesmo tempo, como no modo de hedge de MetaTrader.
2. **Martingale progressão.** A primeira ordem de qualquer lado usa `InitialVolume` (padrão 0,1 lote). Cada entrada subsequente no mesmo lado duplica o maior volume atualmente aberto. Os ajustes de volume respeitam as restrições `MinVolume`, `MaxVolume` e `VolumeStep` do instrumento.
3. **Espaçamento de entrada.** Uma nova posição é adicionada somente quando o preço se moveu pelo menos `ReEntryPips` (padrão 41 pips) além do melhor preço de entrada da escada existente. A escada longa espera que os preços de venda caiam abaixo de `lowest_buy - ReEntryPips`, enquanto a escada curta espera que os preços de compra subam acima de `highest_sell + ReEntryPips`.
4. **Coleta de lucros.** Para cada escada, a negociação com o maior volume atua como a ordem de "gatilho". Quando seu lucro excede `TakeProfitPips` (padrão 65 pips), ou quando o preço atinge o nível de lucro implícito `(TakeProfitPips + 25)` usado pela versão MQL, cada posição desse lado é achatada com uma única ordem de mercado.
5. **Proteção de margem.** Antes de enviar qualquer nova entrada a estratégia verifica se a margem livre informada pela carteira (`CurrentValue - BlockedValue`) permanece acima de `Balance × MinimumFreeMarginRatio` (padrão 0,5). Se a corretora não reportar estatísticas da carteira, a verificação volta ao comportamento de volume fixo do especialista original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Limite de lucro do pip medido no maior e mais recente pedido. Uma vez ultrapassado, todas as posições desse lado serão fechadas. |
| `ReEntryPips` | Distância mínima de pip entre a melhor entrada existente e a oferta/venda atual antes de uma nova ordem de martingale ser adicionada. |
| `InitialVolume` | Tamanho base do lote para a primeira ordem de cada escada. Os pedidos subsequentes dobram o maior volume ativo. |
| `MinimumFreeMarginRatio` | Proporção necessária entre margem livre e saldo antes que novas entradas sejam permitidas. Defina como 0 para desativar a verificação. |

## Notas de implementação

- A estratégia depende exclusivamente de cotações de nível 1: as atualizações de lances impulsionam a lógica de escada curta e as atualizações de pedido impulsionam a lógica de escada longa.
- As intenções do pedido são rastreadas em um dicionário interno para que `OnNewMyTrade` saiba se um preenchimento abriu ou fechou uma escada. Isso imita a escrituração explícita de tickets na fonte MQL.
- A contabilidade de posição armazena cada preenchimento (preço e volume) em listas em vez de consultar estatísticas cumulativas, preservando o comportamento das matrizes MQL que foram usadas para localizar o maior lote e seu preço de entrada.
- O buffer extra de 25 pips que o especialista original colocou em cada ordem de realização de lucro é retido como uma condição de saída adicional.

> **Nota:** A porta Python foi omitida intencionalmente por enquanto, conforme solicitado. A pasta contém apenas a implementação do C# e a documentação multilíngue.
