# Estratégia Clássica MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia MA2CCI transporta o clássico consultor especialista MetaTrader construído em torno da interação de duas médias móveis simples (SMA) e o índice de canal de commodities (CCI). Ele filtra as negociações usando a linha zero CCI e aplica paradas de proteção derivadas do Average True Range (ATR). O sistema foi projetado para entradas de acompanhamento de tendências com reação rápida a reversões.

A versão StockSharp mantém a lógica de negociação original enquanto adapta o gerenciamento de risco ao ambiente .NET. O dimensionamento da posição segue uma regra de risco por mil com um fator de redução adicional que reduz o tamanho da negociação após perdas consecutivas. Cada entrada anexa um stop orientado à volatilidade que reflete a distância ATR usada na implementação MQL.

## Lógica de negociação

- **Indicadores**
  - Rápido SMA com comprimento padrão 4.
  - Lento SMA com comprimento padrão 8.
  - Filtro CCI usando lookback de 4 períodos.
  - ATR com período 4 para colocação de stop.
- **Condições de entrada**
  - **Longa**: o SMA rápido cruza acima do SMA lento e a barra finalizada anterior mostra CCI subindo até zero (de negativo para positivo).
  - **Curto**: o rápido SMA cruza abaixo do lento SMA e a barra anterior mostra CCI caindo de zero (de positivo para negativo).
- **Exit Conditions**
  - O cruzamento oposto SMA fecha posições abertas mesmo que nenhuma nova negociação seja iniciada.
  - stop ATR: as posições longas saem quando o preço cai para `entry - ATR`; as posições curtas saem quando o preço sobe para `entry + ATR`.

## Gestão de risco

- O volume base do pedido é configurável; por padrão 0,1 lote (ou equivalente cambial).
- O dimensionamento dinâmico opcional dimensiona o volume para `free capital * MaxRiskPerThousand / 1000` quando os dados do portfólio estão disponíveis.
- Após mais de uma perda consecutiva, o tamanho da posição é reduzido linearmente em `losses / DecreaseFactor` do volume calculado.
- As paradas de volatilidade dependem da vela finalizada mais recente; picos intrabar além dos níveis de stop desencadeiam uma saída do mercado no próximo tick da estratégia.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo de trabalho para todos os indicadores. | Velas de 1 hora |
| `OrderVolume` | Tamanho mínimo de negociação quando o dimensionamento baseado em risco não estiver disponível. | 0,1 |
| `FastMaPeriod` | Período do jejum SMA. | 4 |
| `SlowMaPeriod` | Período da lentidão SMA. | 8 |
| `CciPeriod` | Período do filtro CCI. | 4 |
| `AtrPeriod` | ATR comprimento para cálculo de parada. | 4 |
| `MaxRiskPerThousand` | Fração de capital livre alocado por negociação (por 1.000 unidades). | 0,02 |
| `DecreaseFactor` | Divisor usado para diminuir o volume após sequências perdidas. | 3 |

## Notas

1. A estratégia processa apenas velas finalizadas, garantindo uma decisão por barra semelhante ao EA original que usava `Volume[0] > 1` como portão.
2. Os níveis de stop são simulados internamente em vez de registrar ordens de stop de câmbio; isso corresponde ao comportamento da versão MetaTrader que dependia do fechamento do mercado quando os limites de ATR foram atingidos.
3. Ative gráficos dentro do StockSharp Designer para visualizar SMA, CCI e negociações executadas usando os auxiliares de desenho integrados.
