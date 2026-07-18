# Estratégia de Rompimento de Intervalo Big Dog
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Big Dog** procura uma janela de consolidação estreita dentro da sessão matinal de Londres e opera rompimentos dessa caixa. O consultor especializado MQL original colocava ordens stop quando o intervalo de preços entre as `StartHour` e `StopHour` especificadas permanecia dentro de um número configurável de pontos. O port do StockSharp mantém a mesma ideia e usa ordens de mercado quando ocorre o rompimento, acompanhadas de níveis dinâmicos de stop-loss e take-profit derivados dos extremos da consolidação.

## Lógica de trading

1. Coletar velas terminadas entre `StartHour` (inclusive) e `StopHour` (exclusivo por padrão) para construir o intervalo diário.
2. Ignorar a sessão se a diferença entre o máximo e mínimo da sessão exceder `MaxRangePoints` (convertido em unidades de preço usando o tamanho de ponto ajustado).
3. Após o fechamento da sessão, verificar a distância entre o melhor ask/bid atual e os níveis de rompimento. Um setup é ativado apenas se o mercado estiver pelo menos `DistancePoints` afastado do máximo (para entradas compradas) ou do mínimo (para entradas vendidas).
4. Quando o preço rompe através do máximo ou mínimo preparado em uma vela subsequente, entrar com uma ordem de mercado dimensionada por `OrderVolume` (compensando automaticamente qualquer posição contrária).
5. Atribuir imediatamente as saídas:
   - Operações compradas usam um stop-loss no mínimo da sessão registrado e um take-profit colocado `TakeProfitPoints` acima do nível de entrada.
   - Operações vendidas usam um stop-loss no máximo da sessão registrado e um take-profit colocado `TakeProfitPoints` abaixo do nível de entrada.
6. Em cada vela terminada, a estratégia monitora o máximo/mínimo para decidir se o stop-loss ou take-profit foi atingido e fecha a posição adequadamente.
7. No início de um novo dia de trading, todos os níveis em cache são reiniciados para evitar ordens remanescentes da sessão anterior.

> **Pontos ajustados.** A estratégia converte entradas baseadas em pontos em distâncias de preço reais multiplicando-as pelo `PriceStep` do instrumento. Quando o ativo tem 3 ou 5 casas decimais, o valor é adicionalmente escalado por 10 para imitar a lógica de pip usada no EA original.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `StartHour` | Hora do dia (0-23) quando a janela de consolidação começa. | `14` |
| `StopHour` | Hora do dia (0-23) quando a janela de consolidação termina. | `16` |
| `MaxRangePoints` | Altura máxima da caixa de sessão medida em pontos ajustados. | `50` |
| `TakeProfitPoints` | Distância de take-profit em pontos ajustados a partir do preço de rompimento. | `50` |
| `DistancePoints` | Distância mínima entre o preço atual e o nível de rompimento antes de ativar ordens. | `20` |
| `OrderVolume` | Volume de cada operação de rompimento (também aplicado ao `Volume` da estratégia). | `1` |
| `CandleType` | Tipo de vela usado para construir a caixa de sessão. Período de uma hora por padrão. | `1h` |

## Notas de implementação

- A estratégia assina tanto velas quanto o livro de ordens. Os melhores valores de bid/ask são usados para avaliar os filtros de distância, recorrendo ao último fechamento de vela se não houver profundidade disponível.
- As entradas são executadas com ordens de mercado. Isso reflete o comportamento das ordens stop pendentes originais enquanto permanece dentro da API de alto nível.
- As decisões de stop-loss e take-profit são realizadas nos fechamentos de velas com base nos máximos e mínimos intrabarra, o que emula os níveis protetores da versão MQL sem registrar ordens filho adicionais.
- O gerenciamento de estado diário cancela quaisquer ordens ativas e reinicia os máximos/mínimos em cache quando a data do calendário muda.
