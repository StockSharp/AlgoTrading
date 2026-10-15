# Estratégia do Corredor de Retração Matinal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia do Corredor de Retração Matinal** replica o comportamento do consultor especialista "3_Otkat_Sys_v1_2" MetaTrader 4. O sistema negocia uma vez por dia durante a sessão da manhã, avaliando a interação entre o preço atual e o corredor de preços formado por velas separadas por 29 barras. Ele reage aos recuos matinais após um forte movimento durante a noite e atribui imediatamente níveis de take-profit assimétricos para posições longas e curtas.

## Lógica de negociação
1. **Filtro de sessão** – as ordens são consideradas apenas dentro do horário de negociação configurado (padrão 05:00 horário da plataforma) e durante os primeiros minutos dessa hora. Segundas e sextas-feiras são excluídas de acordo com o EA original.
2. **Cálculos do corredor de preços** – para cada vela concluída, a estratégia mantém uma janela contínua das barras mais recentes. Ele compara:
   - o preço de abertura 29 barras atrás com o fechamento da vela anterior (`Open[29] - Close[1]`),
   - a vela anterior fecha com o preço de abertura 29 barras atrás (`Close[1] - Open[29]`),
   - a distância do próximo anterior ao mínimo mais baixo dentro da faixa de 29 barras,
   - a distância do máximo mais alto na mesma faixa até o fechamento anterior.
3. **Regras de entrada** – se o movimento noturno exceder o limite `CorridorOpenClosePoints` e o último pullback caber dentro do envelope `PullbackPoints ± CorridorPullbackPoints` configurado, uma posição de mercado será aberta no início da sessão da manhã:
   - Entradas longas requerem um movimento forte para baixo com um recuo superficial ou um movimento para cima com uma continuação estendida acima do corredor.
   - As entradas curtas refletem a lógica das configurações de baixa.
4. **Gerenciamento de posição** – cada negociação recebe:
   - um stop loss em `StopLossPoints * PriceStep` do preço de entrada,
   - um take-profit de `TakeProfitPoints * PriceStep` para posições vendidas e de `(TakeProfitPoints + LongTakeProfitExtraPoints) * PriceStep` para posições longas.
5. **Saída diária** – qualquer posição ainda aberta após o limite de fechamento configurado (padrão após 22h45) é fechada à força para evitar retenção durante a noite.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPoints` | Distância base de take-profit em pontos de instrumento, aplicada a negociações curtas. As negociações longas adicionam `LongTakeProfitExtraPoints`. |
| `StopLossPoints` | Distância de parada protetora nos pontos do instrumento. |
| `PullbackPoints` | Tamanho de retrocesso desejado em torno do qual a estratégia avalia as retrações. |
| `CorridorOpenClosePoints` | Distância mínima entre preços separados por 29 barras para confirmar impulso noturno. |
| `CorridorPullbackPoints` | Tolerância aplicada ao limite de recuo para criar o corredor de entrada. |
| `LongTakeProfitExtraPoints` | Pontos adicionais adicionados à meta de longo lucro. |
| `TradeHour` | Hora (0–23) durante a qual novas entradas são permitidas. |
| `TradeMinuteLimit` | Minuto máximo dentro da hora de negociação para aceitar novos sinais. |
| `CloseHour` | Hora em que a estratégia começa a verificar saídas baseadas em tempo. |
| `CloseMinuteThreshold` | Minuto dentro de `CloseHour` após o qual qualquer posição aberta é fechada. |
| `CandleType` | Período usado para assinaturas de velas (padrão 1 minuto). |

## Notas de implementação
- A estratégia depende de `Security.PriceStep` para converter insumos baseados em pontos em distâncias de preços absolutos. Se o instrumento não fornecer uma etapa de preço válida, a lógica volta para `1.0`.
- Os níveis de stop-loss e take-profit são monitorados em cada vela concluída; a estratégia fecha posições com ordens de mercado quando o nível é ultrapassado dentro dessa faixa de velas.
- A janela contínua contém as últimas 60 velas para cobrir os cálculos necessários de 29 barras e para imitar os auxiliares `Lowest/Highest` usados em MetaTrader.
- A visualização do gráfico (velas e negociações próprias) fica disponível automaticamente quando uma área do gráfico é criada na aplicação host.

## Dicas de uso
- Certifique-se de que o volume da conta de negociação (propriedade `Volume`) esteja definido antes de iniciar a estratégia; o EA nunca dimensiona o tamanho da posição dinamicamente.
- Mantenha o feed de dados alinhado com o fuso horário da sessão esperado pelo consultor especialista original para manter um comportamento idêntico.
- Otimize os parâmetros do corredor ao aplicar a estratégia a mercados com diferentes perfis de volatilidade, porque os limites baseados em pontos foram ajustados para o instrumento original.
