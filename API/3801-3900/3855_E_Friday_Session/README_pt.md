# Estratégia de sessão de sexta-feira eletrônica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de sessão E-Friday replica o clássico consultor especialista MetaTrader que negocia apenas às sextas-feiras. Observa a vela diária anterior e abre uma posição em uma hora configurada no início da sessão de sexta-feira. A direção é contrária: se o dia anterior fechou abaixo da abertura (vela de baixa), a estratégia compra; se o dia anterior fechou acima de sua abertura (vela de alta), a estratégia vende. As posições são gerenciadas intradiariamente e podem ser fechadas automaticamente após uma hora configurável ou por paradas de proteção.

## Regras de negociação
1. Colete velas diárias (padrão: 1 dia) para obter a abertura e o fechamento do dia anterior.
2. Às sextas-feiras, monitore as velas intradiárias (padrão: 1 minuto) para detectar a hora de entrada configurada.
3. Na primeira vela da hora de entrada:
   - Opere comprado quando o dia anterior foi de baixa.
   - Opere vendido quando o dia anterior foi de alta.
   - Ignore a negociação se o dia anterior tiver sido um doji (abertura é igual a fechamento).
4. Opcionalmente, feche a posição automaticamente quando a hora de saída configurada for atingida.
5. Gerencie saídas usando stop-loss, take-profit e lógica opcional de trailing stop que imita o Expert Advisor original, incluindo a ativação de lucro e limites de trailing step.

## Notas de implementação
- Usa StockSharp assinaturas de vela de alto nível para contexto diário e tempo intradiário.
- Converte controles de risco baseados em pontos da versão MQL em compensações de preço absoluto usando a etapa de preço do título.
- Mantém trailing stops no código, atualizando-os em cada vela finalizada e fechando a posição quando os preços extremos são violados.
- Garante apenas uma negociação por sexta-feira, rastreando o estado diário.
- Suporta entradas longas e curtas, respeitando o controle original do número mágico, negociando um único símbolo por instância de estratégia.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Tamanho da negociação em lotes/contratos. | `0.1` |
| `StopLossPoints` | Distância de stop-loss em etapas de preço (0 desativa). | `75` |
| `TakeProfitPoints` | Distância de lucro em etapas de preço (0 desativa). | `0` |
| `HourOpen` | Hora do dia (0-23) para abertura da posição. | `7` |
| `UseClosePositions` | Habilite o fechamento automático após a hora de saída. | `true` |
| `HourClose` | Hora do dia (0-23) para fechar a posição, se habilitada. | `19` |
| `UseTrailing` | Habilite ajustes de trailing stop. | `true` |
| `ProfitTrailing` | Exija que o lucro exceda a distância de trilha antes que a trilha seja ativada. | `true` |
| `TrailingStopPoints` | Distância do trailing stop em etapas de preço. | `60` |
| `TrailingStepPoints` | Pontos adicionais necessários antes de apertar o batente móvel. | `5` |
| `IntradayCandleType` | Tipo de vela para tempo intradiário (velas padrão de 1 minuto). | `TimeSpan.FromMinutes(1)` |
| `DailyCandleType` | Tipo de vela para detecção diária de sentimento (velas padrão de 1 dia). | `TimeSpan.FromDays(1)` |

## Dicas de uso
- Alinhe o pregão do instrumento para que o horário de entrada de sexta-feira corresponda à abertura desejada do mercado.
- Ao configurar os valores de stop-loss e trailing, expresse-os nos mesmos "pontos" usados pela etapa de preço do símbolo para reproduzir o comportamento MetaTrader.
- A estratégia é projetada para uma única negociação por sexta-feira. Para negociar vários símbolos, execute instâncias de estratégia separadas por símbolo.

## Diferenças do original EA
- Usa dados de fechamento de velas para tomada de decisão, considerando os preços originais pesquisados por tick.
- As saídas de proteção são executadas por meio de ordens de mercado quando as velas indicam que os níveis de parada ou meta foram atingidos dentro do intervalo.
- Os parâmetros de estratégia são expostos por meio do sistema `StrategyParam` de StockSharp, suportando otimização e vinculação de UI.
