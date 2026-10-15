# Estratégia de Stoploss Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o comportamento do especialista MetaTrader **Dual StopLoss.mq4**. Atua como uma camada de gestão de risco: monitoriza as ordens protetoras de stop-loss associadas às posições abertas e fecha essas posições alguns pontos antes de o stop ser acionado. A saída antecipada foi projetada para evitar derrapagens negativas em movimentos altamente voláteis, ao mesmo tempo que respeita a colocação inicial do stop do trader.

## Como funciona

1. A estratégia assina os dados do Nível 1 para rastrear o melhor lance/venda atual e a distância `StopLevel` (ou equivalente) publicada pela corretora.
2. Cada vez que novos preços chegam ou ordens/negociações mudam, ele procura a ordem stop ativa mais próxima que pertence ao título gerenciado.
3. A distância entre o preço de mercado e esse stop de proteção é comparada com um limite configurável:
   - Limite = `WhenToClosePoints × pointValue + stopLevelDistance`.
   - `pointValue` corresponde ao MetaTrader de `Point` (0,0001 para a maioria dos pares FX, detectado automaticamente nas configurações de segurança).
   - `stopLevelDistance` vem de campos de Nível 1 (`StopLevel`, `MinStopPrice`, `StopPrice` ou `StopDistance`) quando disponíveis, caso contrário, zero.
4. Quando a distância restante é menor ou igual ao limite, a posição é fechada imediatamente através de uma ordem de mercado.

A lógica cobre posições longas e curtas. Para posições longas, a melhor oferta é comparada com o preço stop de venda; para posições curtas, o melhor preço de venda é comparado com o preço stop de compra. Apenas são consideradas ordens stop e stop-limit no estado ativo.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **QuandoToClosePoints** | Distância (em MetaTrader pontos) do nível de stop que deverá desencadear a saída antecipada. Padrão: 10. Defina como zero para contar apenas com a distância mínima do nível de stop da corretora. |

## Notas e limitações

- A estratégia **não** abre posições por si só; gerencia apenas posições que já existem e possuem ordens de stop de proteção.
- Certifique-se de que o conector/corretor subjacente forneça valores de nível de parada por meio de dados de Nível 1 se você quiser levar em conta as distâncias mínimas impostas pelo corretor. Se essa informação estiver faltando, a estratégia ainda funciona usando apenas a distância do ponto configurada.
- A chamada `StartProtection()` ativa os guardas de segurança integrados de StockSharp para que as saídas de emergência permaneçam ativas depois que a estratégia for iniciada.
- As paradas são detectadas na coleção `Orders` da estratégia. Certifique-se de que as paradas de proteção sejam registradas através da mesma instância de estratégia para que apareçam nesta lista.
- Quando existem múltiplas ordens de stop para a mesma direção, é utilizada aquela mais próxima do mercado.

## Dicas de uso

1. Anexe a estratégia a uma carteira/título onde as posições são abertas manualmente ou por outro sistema, mas as paradas de proteção são colocadas no mesmo contexto estratégico.
2. Configure `WhenToClosePoints` para corresponder à quantidade de amortecimento necessária antes da parada. Este valor é interpretado exatamente como em MetaTrader (pontos, não unidades de preço).
3. Inicie a estratégia e monitore o log. Quando o preço de mercado se aproximar do stop, a estratégia emitirá uma ordem de mercado para fechar a posição de forma proativa.
4. Combine este módulo com outras estratégias de entrada ou dimensionamento de posição para criar um fluxo de trabalho de negociação completo.
