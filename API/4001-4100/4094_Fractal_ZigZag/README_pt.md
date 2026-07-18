# Estratégia Fractal ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta direta do consultor especialista MetaTrader 4 **Fractal ZigZag Expert.mq4**. Ele reconstrói o projeto de lei Williams
sequência fractal e interpreta o extremo confirmado mais recente como a perna ativa do mercado. Quando o último fractal válido é um
oscilar para baixo, o sistema abre uma posição longa; quando uma oscilação alta é confirmada, ele abre uma posição curta. A implementação mantém o
parâmetros originais - profundidade fractal, take-profit, distâncias de stop inicial e trailing stop - enquanto adapta o roteamento do pedido para
o StockSharp API de alto nível.

A estratégia é mais adequada para velas H1, replicando o gráfico padrão usado na versão MetaTrader. No entanto, o
O parâmetro `CandleType` permite alternar para qualquer outro período compatível com o feed de dados. Todas as distâncias são expressas em preço
pontos (etapas de preço do instrumento), que reflete a maneira como MetaTrader usa a constante `Point`.

## Regras de negociação

- **Detecção de sinal**
  - O algoritmo verifica cada vela finalizada e constrói uma janela contínua com elementos `2 * Level + 1`.
  - Um fractal alto é confirmado quando a vela do meio tem a máxima mais alta dentro dessa janela; um fractal baixo requer o menor
baixo.
  - Apenas o último fractal confirmado controla a direção: um mínimo define a tendência interna para `2` (alta), um máximo define-a para
`1` (baixa).
- **Inscrições**
  - Quando a tendência interna é igual a `2` e não há posição aberta, uma compra de mercado é enviada usando o volume `Lots`.
  - Quando a tendência é igual a `1` sem posição, uma venda no mercado é enviada.
  - A estratégia entrará novamente na mesma direção após o fechamento de uma posição, se a tendência não tiver mudado.
- **Saídas e gerenciamento de risco**
  - Cada entrada recebe um stop loss inicial e um takeprofit fixo definido em pontos. Um valor de parada de `0` desativa o
respectiva proteção.
  - O trailing stop opcional (também em pontos) é ativado quando o preço se move pela distância configurada. A parada é então movida para
manter o mesmo deslocamento do preço de fechamento, nunca cruzando o stop de proteção inicial.
  - As ordens de proteção são emuladas monitorando os máximos/mínimos das velas para aproximar os toques intrabarras, correspondendo de perto ao original
Lógica MQL4.

## Parâmetros padrão

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Level` | `2` | Número de velas de cada lado necessárias para confirmar um fractal. |
| `TakeProfitPoints` | `25` | Distância até a meta de lucro em faixas de preço. |
| `InitialStopPoints` | `20` | Distância até o stop loss inicial em pontos de preço. |
| `TrailingStopPoints` | `10` | Distância do trailing stop em faixas de preço (definida como `0` para desativar). |
| `Lots` | `1` | Volume de pedidos usado para entradas no mercado. |
| `CandleType` | `H1` | Prazo de velas processadas pela estratégia. |

## Notas

- A estratégia chama `StartProtection()` uma vez na inicialização para que StockSharp possa gerenciar a liquidação de posição de emergência, se necessário.
- Todos os registros e comentários são fornecidos em inglês, enquanto as descrições seguem o idioma de cada variante README, conforme exigido pelo
diretrizes de conversão.
- A implementação evita buffers de indicadores e imita a abordagem MetaTrader mantendo apenas a janela contínua mínima
necessário para avaliar um fractal.
