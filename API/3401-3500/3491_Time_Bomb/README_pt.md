# Estratégia de bomba-relógio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Time Bomb replica o consultor especialista MetaTrader que dispara um único pedido sempre que o preço explode em uma direção dentro de um
janela curta e configurável. A estratégia observa as melhores cotações de compra/venda em tempo real e mede o número de pips cobertos entre
o último preço de referência e a cotação mais recente. Se a distância necessária for percorrida com rapidez suficiente, abre-se uma ordem de mercado em
a direção do rompimento e arma imediatamente os níveis ocultos de stop-loss e take-profit expressos em pips.

A implementação atua apenas quando nenhuma posição está aberta no momento, espelhando a lógica do bloco original que impedia a sobreposição
negócios. As referências de preço são redefinidas quando um sinal é acionado ou quando a janela de observação expira, de modo que cada explosão de
a volatilidade produz no máximo uma única negociação por lado. Os níveis de stop-loss e take-profit são mantidos internamente e aplicados por
a estratégia em si porque StockSharp não coloca automaticamente ordens de proteção para execuções de mercado.

## Detalhes

- **Critérios de entrada**:
  - **Longo**: A melhor venda aumenta em pelo menos `BuyPipsInTime` pips em comparação com o preço de referência armazenado e a movimentação termina
dentro de `BuyTimeToWait` segundos. Uma ordem de compra com tamanho `BuyVolume` é enviada assim que a condição for atendida.
  - **Short**: o melhor lance cai pelo menos `SellPipsInTime` pips em comparação com o preço de referência armazenado e a mudança termina
dentro de `SellTimeToWait` segundos. Uma ordem de venda com tamanho `SellVolume` é enviada assim que a condição for atendida.
- **Longo/Curto**: Ambas as direções são suportadas, mas apenas uma posição pode existir por vez.
- **Critérios de saída**:
  - **Longo**: A posição fecha quando o melhor lance atinge o preço calculado de stop-loss ou take-profit.
  - **Short**: A posição fecha quando a melhor oferta atinge o stop-loss calculado ou a melhor oferta atinge o nível de take-profit.
- **Paradas**: as paradas de proteção ocultas são tratadas pela estratégia. As distâncias são definidas em pips e traduzidas em preços usando
o tamanho do passo do símbolo atual.
- **Valores padrão**:
  - `SellPipsInTime` = 5 pips, `SellTimeToWait` = 10 segundos, `SellVolume` = 0,01 lotes.
  - `SellStopLossPips` = 20 pips, `SellTakeProfitPips` = 20 pips.
  - `BuyPipsInTime` = 5 pips, `BuyTimeToWait` = 10 segundos, `BuyVolume` = 0,01 lotes.
  - `BuyStopLossPips` = 20 pips, `BuyTakeProfitPips` = 20 pips.
- **Filtros**:
  - Categoria: Breakout/momentum.
  - Direção: Simétrica (longa e curta).
  - Indicadores: Apenas movimento de preços brutos, sem osciladores.
  - Paradas: Sim (distâncias fixas de pip por lado).
  - Complexidade: Baixa – detector de fuga único com rastreamento de estado simples.
  - Prazo: intradiário, reage aos impulsos de nível de tick uma vez por segundo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Depende das distâncias de pip configuradas; os incumprimentos correspondem ao risco médio nos principais pares de FX.

## Entradas

| Nome | Descrição |
| --- | --- |
| `SellPipsInTime` | Distância mínima descendente em pips que deve ser percorrida antes de abrir uma posição curta. |
| `SellTimeToWait` | Segundos permitiram que o movimento descendente fosse concluído. |
| `SellVolume` | Volume de negociação para sinais de venda. |
| `SellStopLossPips` | Distância de stop-loss para posições curtas, expressa em pips. |
| `SellTakeProfitPips` | Distância de take-profit para posições curtas, expressa em pips. |
| `BuyPipsInTime` | Distância mínima ascendente em pips que deve ser percorrida antes de abrir uma posição longa. |
| `BuyTimeToWait` | Segundos permitidos para que o movimento ascendente fosse concluído. |
| `BuyVolume` | Volume de negociação para sinais de compra. |
| `BuyStopLossPips` | Distância de stop-loss para posições longas, expressa em pips. |
| `BuyTakeProfitPips` | Distância de take-profit para posições longas, expressa em pips. |

## Notas

- A estratégia depende das melhores atualizações de compra/venda; garantir que o feed de dados forneça cotações precisas de nível um.
- Definir qualquer distância de pip ou janela de tempo para zero desativa o sinal correspondente porque o preço de referência é redefinido em vez de
gerando negócios.
- Como os níveis de proteção são gerenciados internamente, desconexões inesperadas podem deixar posições sem paradas bruscas. Considere
combinando a estratégia com controles de risco externos durante a execução ao vivo.
