# Exemplo de estratégia de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O **SampleTrailingStopStrategy** é uma porta C# direta do consultor especialista MetaTrader `SampleTrailingstop.mq4`. A estratégia não gera entradas próprias; em vez disso, observa continuamente a posição atual e mantém ordens protetoras de stop-loss e take-profit. A lógica reflete o EA original, respeitando os níveis de stop e congelamento impostos pelo corretor ao aplicar um trailing stop medido em faixas de preço.

Sempre que uma posição longa se torna lucrativa e a melhor oferta se afasta o suficiente do preço de entrada, a estratégia primeiro move o stop-loss logo abaixo da oferta pela distância mínima permitida. As atualizações subsequentes acompanham o stop atrás do lance pelo número configurado de pontos mais os buffers do corretor. As posições curtas são processadas simetricamente, com o stop acima do pedido. As metas opcionais de lucro são recalculadas em cada evento final.

## Fluxo de dados

* Assina as atualizações do Level1 para receber as melhores cotações de compra/venda.
* Rastreia o preço da posição atual por meio da base `Strategy` API.
* Registra novamente ordens de stop e limite de proteção quando novos preços são calculados.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TrailingStopPoints` | `200` | Distância entre o mercado e o trailing stop medido em pontos de preço. Este valor é adicionado aos buffers do corretor durante os cálculos finais. |
| `TakeProfitPoints` | `1000` | Distância opcional de take-profit em pontos. Defina como `0` para desativar o gerenciamento de lucro. |
| `StopLevelPoints` | `0` | Restrição de nível de stop da corretora expressa em pontos. É adicionado à distância final para manter as ordens de parada válidas. |
| `FreezeLevelPoints` | `0` | Restrição de nível de congelamento do corretor expressa em pontos. O trailing espera até que o mercado ultrapasse esse buffer em relação ao preço de entrada. |

Todas as distâncias são traduzidas em valores de preço com o tamanho do tick do instrumento para emular o comportamento `_Point` de MetaTrader.

## Algoritmo de trilha

1. **Validação de posição** – A estratégia ignora o trailing até que exista uma posição e a melhor oferta/venda seja conhecida.
2. **Verificação de lucro** – O trailing é ativado somente quando a posição é lucrativa (`bid > entry` para posições longas, `ask < entry` para posições curtas) e o buffer de congelamento foi limpo.
3. **Colocação de stop inicial** – Se nenhum trailing stop estiver ativo ainda, o stop é movido para a distância mínima permitida do mercado (oferta menos buffers para posições compradas, venda mais buffers para posições vendidas) assim que o preço percorrer pelo menos a distância final da entrada.
4. **Atualizações de trailing** – Enquanto a posição permanece lucrativa, o stop é aprofundado usando a distância de trailing configurada mais os buffers da corretora. Os níveis de lucro são recalculados em cada atualização quando ativados.
5. **Manutenção de ordens** – As ordens de proteção são criadas, atualizadas ou canceladas automaticamente por meio de métodos auxiliares de alto nível para que a corretora sempre veja os valores mais recentes de stop-loss e take-profit.

## Notas de uso

* Inicie a estratégia junto com outro componente que abre posições, ou utilize ordens manuais; este módulo gerencia apenas saídas.
* Certifique-se de que os metadados do instrumento contenham etapas adequadas de preço e volume. A estratégia normaliza todos os preços e valores gerados para satisfazer as restrições cambiais.
* Quando a direção da posição muda, quaisquer ordens de proteção herdadas são canceladas antes dos reinícios do novo lado.
