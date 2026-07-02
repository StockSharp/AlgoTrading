# Estratégia de pivô L3H3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **L3H3 Pivot Strategy** é uma versão StockSharp do MetaTrader especialista "L3_H3_Expert". O script original constrói uma estrutura dinâmica diária e implanta duas ordens pendentes para negociar possíveis rompimentos ou retrocessos em torno da máxima e da mínima da sessão anterior. A versão StockSharp mantém a mesma ideia: ela recalcula os níveis de pivô após cada vela de período superior concluída (diariamente por padrão) e decide entre entradas de stop ou limite com base em onde o mercado atualmente negocia em relação ao intervalo de ontem.

## Lógica de negociação

1. **Estatísticas da sessão**
   - Após cada vela pivô concluída (padrão: diariamente), a estratégia captura os valores de abertura, máximo, mínimo e fechamento da sessão anterior.
   - O nível de pivô clássico é calculado como `(High + Low + Close) / 3`.
   - Esses níveis permanecem ativos durante toda a próxima sessão.

2. **Configuração de entrada**
   - O preço de entrada de compra está ancorado ligeiramente acima da mínima anterior. O deslocamento é igual ao parâmetro `EntryOffsetPips` expresso em múltiplos de tamanho pip.
   - Um preço de entrada de venda está ancorado na máxima anterior (espelhando o especialista original que usou a máxima bruta sem qualquer buffer adicional).
   - Para cada novo dia de negociação (detectado através da assinatura da vela principal), a estratégia coloca novas ordens pendentes:
     - Se o mercado for negociado **abaixo** da mínima de ontem, um **stop de compra** será colocado para capturar um rompimento de alta.
     - Se o mercado for negociado **acima** da máxima de ontem, um **stop de venda** será colocado para negociar uma reversão negativa.
     - Caso contrário, o algoritmo prefere ordens de **limite** nos mesmos níveis de preço para comprar quedas ou vender altas de volta ao intervalo.
   - As ordens de stop loss são posicionadas `StopLossPips` longe da referência mínima/máxima, exatamente como a versão MQL fixou um buffer de stop de 16 pontos.
   - O take-profit de ambas as ordens pendentes está alinhado com o nível pivô, replicando o posicionamento alvo encontrado no código-fonte.

3. **Gerenciamento de pedidos**
   - Cada vez que um novo pivô é calculado, quaisquer ordens pendentes em funcionamento são canceladas e recalculadas com os novos níveis.
   - A estratégia também cancela ordens pendentes desatualizadas quando uma nova sessão começa, evitando o acúmulo de ordens inativas.
   - Quando um pedido é atendido, sua referência interna é apagada automaticamente para evitar cancelamentos duplicados.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `EntryCandleType` | Série de velas usada para monitorar a sessão atual e acionar a colocação de pedidos. | Período de 5 minutos |
| `PivotCandleType` | Vela de prazo mais alto usada para medir as estatísticas da sessão anterior. | Período diário |
| `EntryOffsetPips` | Distância (em pips) adicionada acima da mínima anterior para entradas longas. | 2 |
| `StopLossPips` | Distância (em pips) aplicada além da referência baixa/máxima para posicionar stop loss. | 16 |

## Diferenças do especialista MQL

- O script MetaTrader selecionou diferentes sessões de negociação (Ásia, Londres, Nova York) por meio de números mágicos e janelas de tempo. A versão StockSharp consolida o comportamento usando uma vela configurável de prazo mais alto (diariamente por padrão) para derivar os níveis de pivô, o que torna a lógica mais fácil de auditar e adaptar entre corretores.
- MetaTrader confiou no bid/ask atual para decidir entre ordens stop e limit. A implementação StockSharp usa a vela finalizada mais recente da série `EntryCandleType` para essa comparação para manter o fluxo de trabalho orientado a eventos.
- Os comentários dos pedidos e os números mágicos eram específicos da plataforma no MT4. Eles são omitidos intencionalmente aqui; em vez disso, a estratégia mantém referências diretas às suas ordens pendentes.

## Notas de uso

- Certifique-se de que a segurança subjacente exponha um `PriceStep` válido. A estratégia lança uma exceção no início se a conexão do corretor não fornecer informações sobre o tamanho do pip.
- Para replicar o comportamento original mais de perto, defina `PivotCandleType` para uma série de velas horárias agregadas na sessão desejada e ajuste os parâmetros de deslocamento/parada de acordo.
- Como acontece com qualquer estratégia de ordem pendente, considere a distância mínima da corretora e as políticas de expiração de ordem pendente ao implantar ao vivo.
